using System;
using System.Collections.Generic;
using System.IO;
using Google.Apis.AndroidPublisher.v3;
using Google.Apis.AndroidPublisher.v3.Data;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Upload;

namespace ConsoleGPlayAPITool
{
    class Program
    {
        static void Main(string[] args)
        {
            var configFilePath = args[0];
            Console.WriteLine($"Loading Config File from path {configFilePath}");

            BundleSettings configs = BundleSettings.FromFilePath(configFilePath);
            
            if (configs == null)
            {
                throw new Exception("Cannot load a valid BundleConfig");
            }
            
            //Create publisherService
            var androidPublisherService = CreateGoogleConsoleAPIService(configs);

            // Create a new edit to make changes to your listing.
            var edit = CreateAnEditObject(androidPublisherService, configs);
            
            var uploadAab = configs.ApkPath.Trim().EndsWith(".aab");

            var result = uploadAab ? CreateUploadAabFileObject(configs, androidPublisherService, edit).Upload() : 
                CreateUploadApkFileObject(configs, androidPublisherService, edit).Upload();

            if (result.Exception != null)
            {
                throw new Exception(result.Exception.Message);
            }

            if (result.Status != UploadStatus.Completed)
            {
                throw new Exception("File upload failed. Reason: unknown :(");
            }

            Console.WriteLine("File uploaded, bytes sent: " + result.BytesSent);
        }

        private static void UploadObbFilesOnApk(AndroidPublisherService service, AppEdit edit, Apk apk,
            BundleSettings configs, string[] obbs)
        {
            foreach (var obbPath in obbs)
            {
                var upload = service.Edits.Expansionfiles.Upload(
                    configs.PackageName,
                    edit.Id,
                    apk.VersionCode.Value,
                    EditsResource.ExpansionfilesResource.UploadMediaUpload.ExpansionFileTypeEnum.Main,
                    new FileStream(obbPath, FileMode.Open),
                    "application/octet-stream"
                );
                Console.WriteLine($"Starting Uploading Obb:{obbPath}");
                upload.ResponseReceived += response =>
                {
                    if (response == null)
                    {
                        throw new Exception("Failed Upload " + obbPath);
                    }
                    else
                    {
                        Console.WriteLine("Success Upload " + obbPath);
                    }
                };
                var result = upload.Upload();
                if (result.Exception != null)
                {
                     throw new Exception("Error: " + result.Exception.Message);
                }

                if (result.Status != UploadStatus.Completed)
                {
                    throw new Exception("Obb not uploaded");
                }
                Console.WriteLine($"Finish Uploading Obb:{obbPath}");
            }
        }
        
        private static void UploadObbFiles(AndroidPublisherService service, AppEdit edit, Nullable<int> versionCode,
            BundleSettings configs, string[] obbs)
        {
            foreach (var obbPath in obbs)
            {
                var upload = service.Edits.Expansionfiles.Upload(
                    configs.PackageName,
                    edit.Id,
                    versionCode.Value,
                    EditsResource.ExpansionfilesResource.UploadMediaUpload.ExpansionFileTypeEnum.Main,
                    new FileStream(obbPath, FileMode.Open),
                    "application/octet-stream"
                );
                Console.WriteLine($"Starting Uploading Obb:{obbPath}");
                upload.ResponseReceived += response =>
                {
                    if (response == null)
                    {
                        throw new Exception("Failed Upload " + obbPath);
                    }
                    else
                    {
                        Console.WriteLine("Success Upload " + obbPath);
                    }
                };
                var result = upload.Upload();
                if (result.Exception != null)
                {
                    throw new Exception("Error: " + result.Exception.Message);
                }

                if (result.Status != UploadStatus.Completed)
                {
                    throw new Exception("Obb not uploaded");
                }
                Console.WriteLine($"Finish Uploading Obb:{obbPath}");
            }
        }

        private static void CommitChangesToGooglePlay(AndroidPublisherService androidPublisherService,
            BundleSettings configs,
            AppEdit edit)
        {
            var commitRequest = androidPublisherService.Edits.Commit(configs.PackageName, edit.Id);
            var appEdit = commitRequest.Execute();
            Console.WriteLine("App edit with id " + appEdit.Id + " has been comitted");
        }

        private static bool CheckIfNeedProcessObb(BundleSettings configs, out string[] f)
        {
            var apkFolder = Directory.GetParent(configs.ApkPath);
            Console.WriteLine($"Trying find obb on Path: {apkFolder}");
            var boolNeedProcessObb = false;
            var tempList = new List<string>();

            var files = apkFolder.GetFiles();
            foreach (var fileInfo in files)
            {
                if (fileInfo.Extension == ".obb")
                {
                    boolNeedProcessObb = true;
                    tempList.Add(fileInfo.FullName);
                }
            }

            f = tempList.ToArray();
            Console.WriteLine($"Need Upload Obb:{boolNeedProcessObb}");
            return boolNeedProcessObb;
        }

        private static void UpdateTrackInformation(Nullable<int> versionCode, Track track, BundleSettings configs)
        {
            var apkVersionCodes = new List<long?> {versionCode};
            var release = new TrackRelease
            {
                Name = configs.ReleaseName,
                ReleaseNotes = new List<LocalizedText>()
                {
                    new LocalizedText()
                    {
                        Language = configs.RecentChangesLang,
                        Text = configs.RecentChanges,
                    }
                },
                Status = configs.TrackStatus,
                VersionCodes = apkVersionCodes,
            };

            if (configs.TrackStatus == "completed")
            {
                track.Releases.Clear();
            }
            else 
            {
                release.UserFraction = configs.UserFraction;
            }
            track.Releases.Add(release);
            
            
            //var completedStatus = "inProgress";
            // var haltedStatus = "halted";
            // if (configs.TrackStatus == completedStatus)
            // {
            //     foreach (var trackRelease in track.Releases)
            //     {
            //         if (trackRelease.Status == completedStatus)
            //         {
            //             trackRelease.Status = haltedStatus;
            //             trackRelease.UserFraction = 1;
            //         }
            //     }
            // }

            Console.WriteLine("Update Track information (Without Commit).");
        }

        private static Track LoadTrackBranch(AndroidPublisherService androidPublisherService, BundleSettings configs,
            AppEdit edit)
        {
            var track = androidPublisherService.Edits.Tracks.Get(configs.PackageName, edit.Id, configs.TrackBranch)
                .Execute();
            Console.WriteLine($"Load TrackBranch:{track.TrackValue}");
            return track;
        }
        
        private static EditsResource.ApksResource.UploadMediaUpload CreateUploadApkFileObject(BundleSettings configs, AndroidPublisherService androidPublisherService, AppEdit edit)
        {
            var upload = UploadApkFile(configs, androidPublisherService, edit);
            
            //Verify if exist any obb
            var needUploadExtensionsFiles = CheckIfNeedProcessObb(configs, out string[] obbs);
            
            upload.ResponseReceived += (apk) =>
            {
                if (apk == null)
                    return;
                var track = LoadTrackBranch(androidPublisherService, configs, edit);

                UpdateTrackInformation(apk.VersionCode, track, configs);

                if (needUploadExtensionsFiles)
                    UploadObbFiles(androidPublisherService, edit, apk.VersionCode, configs, obbs);

                var updatedTrack = androidPublisherService.Edits.Tracks
                    .Update(track, configs.PackageName, edit.Id, track.TrackValue).Execute();
                Console.WriteLine("Track " + updatedTrack.TrackValue + " has been updated.");

                CommitChangesToGooglePlay(androidPublisherService, configs, edit);
            };

            return upload;
        }
        
        private static EditsResource.BundlesResource.UploadMediaUpload CreateUploadAabFileObject(BundleSettings configs, AndroidPublisherService androidPublisherService, AppEdit edit)
        {
            var upload = UploadAabFile(configs, androidPublisherService, edit);
            
            //Verify if exist any obb
            var needUploadExtensionsFiles = CheckIfNeedProcessObb(configs, out string[] obbs);
            upload.ResponseReceived += (aab) =>
            {
                if (aab == null)
                    return;
                var track = LoadTrackBranch(androidPublisherService, configs, edit);

                UpdateTrackInformation(aab.VersionCode, track, configs);

                if (needUploadExtensionsFiles)
                    UploadObbFiles(androidPublisherService, edit, aab.VersionCode, configs, obbs);

                var updatedTrack = androidPublisherService.Edits.Tracks
                    .Update(track, configs.PackageName, edit.Id, track.TrackValue).Execute();
                Console.WriteLine("Track " + updatedTrack.TrackValue + " has been updated.");

                CommitChangesToGooglePlay(androidPublisherService, configs, edit);
            };

            return upload;
        }

        private static EditsResource.ApksResource.UploadMediaUpload UploadApkFile(BundleSettings configs,
            AndroidPublisherService androidPublisherService,
            AppEdit edit)
        {
            Console.WriteLine("Upload started for apk: " + Path.GetFileName(configs.ApkPath));
            var upload = androidPublisherService.Edits.Apks.Upload(
                configs.PackageName,
                edit.Id,
                new FileStream(configs.ApkPath, FileMode.Open),
                "application/octet-stream"
            );
            return upload;
        }
        
        private static EditsResource.BundlesResource.UploadMediaUpload UploadAabFile(BundleSettings configs,
            AndroidPublisherService androidPublisherService,
            AppEdit edit)
        {
            Console.WriteLine("Upload started for aab: " + Path.GetFileName(configs.ApkPath));
            var upload = androidPublisherService.Edits.Bundles.Upload(
                configs.PackageName,
                edit.Id,
                new FileStream(configs.ApkPath, FileMode.Open),
                "application/octet-stream"
            );
            return upload;
        }

        private static AppEdit CreateAnEditObject(AndroidPublisherService androidPublisherService,
            BundleSettings configs)
        {
            var edit = androidPublisherService.Edits
                .Insert(null /** no content */, configs.PackageName)
                .Execute();
            Console.WriteLine("Created edit with id: " +
                              edit.Id +
                              " (valid for " + edit.ExpiryTimeSeconds + " seconds)");
            return edit;
        }

        private static AndroidPublisherService CreateGoogleConsoleAPIService(BundleSettings configs)
        {
            var cred = GoogleCredential.FromJson(File.ReadAllText(configs.JsonKeyPath));
            cred = cred.CreateScoped(new[] {AndroidPublisherService.Scope.Androidpublisher});

            // Create the AndroidPublisherService.
            var androidPublisherService = new AndroidPublisherService(new BaseClientService.Initializer
                {HttpClientInitializer = cred});
            return androidPublisherService;
        }
    }
}
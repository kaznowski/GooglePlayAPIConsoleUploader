using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ConsoleGPlayAPITool
{
    public class BundleSettings
    {
        
        Dictionary<string,string> _RawDict = new Dictionary<string, string>();
        
        public string PackageName => GetValue("umake.android.packagename");
        public string JsonKeyPath => GetValue("umake.android.jsonkeypath");
        public string ApkPath => GetValue("umake.android.apkPath");
        public string RecentChanges => GetValue("umake.android.recentchanges");
        public string RecentChangesLang => GetValue("umake.android.recentchangeslang");
        public string TrackBranch => GetValue("umake.android.trackbranch");
        public string ReleaseName => GetValue("umake.android.releasename");
        public int UserFraction
        {
            get
            {
                int.TryParse(GetValue("umake.android.userfraction"), out int num);
                return num;
            }
        }

        public string TrackStatus => GetValue("umake.android.trackstatus");

        public BundleSettings(Dictionary<string, string> raw)
        {
            _RawDict = raw;
        }

        public static BundleSettings FromFilePath(string path)
        {
                
            try
            {
                if (!File.Exists(path))
                {
                    throw new Exception("Cannot find file");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            var dict = new Dictionary<string, string>();
            var lines = File.ReadAllLines(path);
            var sb = new StringBuilder();
        
            foreach (var line in lines)
            {
                sb.AppendLine(line);
                var split = line.Split('=');
                if (split.Length >= 2)
                {
                    dict.Add(split[0], split[1]);
                }
            }
            var s = new BundleSettings(dict);
            return s;
        }
        
        public string GetValue(string key, string defaultValue = "")
        {
            if (_RawDict == null)
                return "";
            if (_RawDict.TryGetValue(key, out var ret))
                return ret;
            return defaultValue;
        }
    }
}
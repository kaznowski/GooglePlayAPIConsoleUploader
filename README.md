# GooglePlayAPIConsoleUploader
A Project to upload an Apk and the extensions file using CMD automatic.
Create to help developers upload yours apks (including CLI's) automatically.


#How to use?

call the program passing one arg: a path to a file config (text) with this informations:
umake.android.packagename=YOUR_PACKAGE_BUNDLE
umake.android.jsonkeypath=YOUR_JSON_OATH_KEY
umake.android.apkPath=YOUR_APK_PATH
umake.android.recentchanges=YOUR_RECENT_MESSAGE_DESCRIPTION
umake.android.recentchangeslang=YOUR_RECENT_MESSAGE_LANG
umake.android.trackbranch=YOUR_BRANCH_TARGET(alpha,beta,internal)
umake.android.releasename=YOUR_RELEASE_NAME
umake.android.trackstatus=YOUR_TRACK_STATUS(completed,inProgress, etc)
umake.android.userfraction=YOUR_USER_FRACTION


#TIPS
If you are using TEAMCITY CLI, put config values inside build environment system, and pass to the CMD the configBuildPath, and everything will work :)

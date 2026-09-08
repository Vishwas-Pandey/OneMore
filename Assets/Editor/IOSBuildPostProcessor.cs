#if UNITY_IOS
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;

/// <summary>
/// Runs automatically after Unity exports the Xcode project (BuildScript.BuildIOS,
/// or File > Build Settings > Build for iOS). Injects everything Android's
/// AndroidManifest.xml gets "for free" but iOS needs added by hand:
///  - NSUserTrackingUsageDescription (required to call ATT, see ATTManager.cs)
///  - SKAdNetworkItems (required for AdMob/network attribution to work on iOS)
///  - Links the AppTrackingTransparency + AdSupport frameworks used by ATTBridge.mm
/// Wrapped in #if UNITY_IOS so this file compiles even on machines without the
/// iOS Build Support module installed (it simply won't be included in that case).
/// </summary>
public static class IOSBuildPostProcessor
{
    [PostProcessBuild(100)]
    public static void OnPostProcessBuild(BuildTarget buildTarget, string pathToBuiltProject)
    {
        if (buildTarget != BuildTarget.iOS) return;

        UpdateInfoPlist(pathToBuiltProject);
        UpdateXcodeProject(pathToBuiltProject);
    }

    private static void UpdateInfoPlist(string pathToBuiltProject)
    {
        string plistPath = Path.Combine(pathToBuiltProject, "Info.plist");
        PlistDocument plist = new PlistDocument();
        plist.ReadFromFile(plistPath);

        PlistElementDict root = plist.root;

        // App Tracking Transparency usage string shown in the system dialog.
        root.SetString("NSUserTrackingUsageDescription",
            "This identifier is used to deliver relevant ads and measure ad performance.");

        // SKAdNetwork IDs Google/AdMob and common ad networks require for
        // attribution on iOS. Add your other mediated networks' IDs here too.
        PlistElementArray skAdNetworkItems = root.CreateArray("SKAdNetworkItems");
        string[] skAdNetworkIds =
        {
            "cstr6suwn9.skadnetwork", // Google
            "4fzdc2evr5.skadnetwork",
            "2fnua5tdw4.skadnetwork",
            "ydx93a7ass.skadnetwork",
            "5a6flpkh64.skadnetwork",
            "p78axxw29g.skadnetwork",
            "v72qych5uu.skadnetwork",
        };
        foreach (string id in skAdNetworkIds)
        {
            PlistElementDict entry = skAdNetworkItems.AddDict();
            entry.SetString("SKAdNetworkIdentifier", id);
        }

        root.SetBoolean("ITSAppUsesNonExemptEncryption", false);

        plist.WriteToFile(plistPath);
    }

    private static void UpdateXcodeProject(string pathToBuiltProject)
    {
        string pbxProjectPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);
        PBXProject project = new PBXProject();
        project.ReadFromFile(pbxProjectPath);

        string mainTargetGuid = project.GetUnityMainTargetGuid();
        string frameworkTargetGuid = project.GetUnityFrameworkTargetGuid();

        // ATTBridge.mm calls into these frameworks; Unity does not link them
        // by default since no C# API references them directly.
        project.AddFrameworkToProject(frameworkTargetGuid, "AppTrackingTransparency.framework", true);
        project.AddFrameworkToProject(frameworkTargetGuid, "AdSupport.framework", true);

        project.SetBuildProperty(mainTargetGuid, "ENABLE_BITCODE", "NO");

        File.WriteAllText(pbxProjectPath, project.WriteToString());
    }
}
#endif

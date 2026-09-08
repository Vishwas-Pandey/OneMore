using UnityEditor;
using UnityEngine;
using System.IO;
using System;

/// <summary>
/// Menu-driven build entry points for both platforms. Unity's BuildPipeline
/// only takes the Android build all the way to an installable artifact
/// (.apk/.aab); for iOS it can only generate the Xcode project - archiving,
/// signing, and uploading to App Store Connect still happen in Xcode or via
/// Fastlane afterward. See SETUP.md for the full steps after each build.
/// </summary>
public static class BuildScript
{
    private static readonly string[] Scenes =
    {
        "Assets/_Project/Scenes/Splash.unity",
        "Assets/_Project/Scenes/MainMenu.unity",
        "Assets/_Project/Scenes/Gameplay.unity"
    };

    [MenuItem("Build/Android/Development")]
    public static void BuildAndroidDevelopment()
    {
        BuildAndroid("Development", BuildOptions.Development | BuildOptions.ConnectWithProfiler, false);
    }

    [MenuItem("Build/Android/Release APK")]
    public static void BuildAndroidRelease()
    {
        BuildAndroid("Release", BuildOptions.None, false);
    }

    [MenuItem("Build/Android/Release App Bundle (Play Store)")]
    public static void BuildAndroidAppBundle()
    {
        BuildAndroid("Release", BuildOptions.None, true);
    }

    private static void BuildAndroid(string variant, BuildOptions options, bool buildAppBundle)
    {
        string outputDir = "Builds/Android";
        Directory.CreateDirectory(outputDir);

        string fileName = $"OneMore_{variant}.{(buildAppBundle ? "aab" : "apk")}";
        string outputPath = Path.Combine(outputDir, fileName);

        EditorUserBuildSettings.buildAppBundle = buildAppBundle;
        EditorUserBuildSettings.development = options.HasFlag(BuildOptions.Development);
        EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;

        var buildOptions = new BuildPlayerOptions
        {
            scenes = Scenes,
            locationPathName = outputPath,
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            options = options
        };

        var report = BuildPipeline.BuildPlayer(buildOptions);
        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log($"[BuildScript] Android build succeeded: {outputPath}");
        }
        else
        {
            Debug.LogError($"[BuildScript] Android build failed: {report.summary.result}");
        }
    }

    [MenuItem("Build/iOS/Generate Xcode Project")]
    public static void BuildIOS()
    {
        string outputDir = "Builds/iOS";
        Directory.CreateDirectory(outputDir);

        var buildOptions = new BuildPlayerOptions
        {
            scenes = Scenes,
            locationPathName = outputDir,
            target = BuildTarget.iOS,
            targetGroup = BuildTargetGroup.iOS,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(buildOptions);
        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log($"[BuildScript] Xcode project generated at: {outputDir}\n" +
                      "Next: open Builds/iOS/Unity-iPhone.xcworkspace in Xcode, " +
                      "select your Team under Signing & Capabilities, then Product > Archive.");
        }
        else
        {
            Debug.LogError($"[BuildScript] iOS build failed: {report.summary.result}");
        }
    }
}

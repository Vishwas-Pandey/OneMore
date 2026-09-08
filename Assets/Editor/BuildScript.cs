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

    /// <summary>
    /// Release builds need the keystore/key passwords, but those must never be
    /// hardcoded or committed. Unity does not persist PlayerSettings.Android.
    /// keystorePass/keyaliasPass to ProjectSettings.asset, so they have to be
    /// supplied fresh each build via environment variables the calling shell
    /// sets transiently (see RELEASE_SIGNING.md) — never printed or logged here.
    /// </summary>
    private static void ApplySigningCredentialsIfAvailable()
    {
        if (!PlayerSettings.Android.useCustomKeystore) return;

        string storePass = Environment.GetEnvironmentVariable("ONEMORE_KEYSTORE_PASS");
        string keyPass = Environment.GetEnvironmentVariable("ONEMORE_KEY_ALIAS_PASS");

        if (string.IsNullOrEmpty(storePass) || string.IsNullOrEmpty(keyPass))
        {
            Debug.LogWarning("[BuildScript] Custom keystore is configured but ONEMORE_KEYSTORE_PASS / " +
                              "ONEMORE_KEY_ALIAS_PASS are not set in the environment - the release build " +
                              "will fail to sign. See RELEASE_SIGNING.md.");
            return;
        }

        PlayerSettings.Android.keystorePass = storePass;
        PlayerSettings.Android.keyaliasPass = keyPass;

        // Gradle resolves a relative keystoreName against its generated module
        // directory (Library/Bee/Android/Prj/IL2CPP/Gradle/launcher/...), not
        // the project root, so a relative path here fails signing even though
        // it's the right value for anyone reading ProjectSettings.asset.
        // Resolve to an absolute path in memory only - never written back to
        // the committed asset, so the repo stays portable across machines.
        if (!Path.IsPathRooted(PlayerSettings.Android.keystoreName))
        {
            PlayerSettings.Android.keystoreName = Path.GetFullPath(PlayerSettings.Android.keystoreName);
        }
    }

    private static void BuildAndroid(string variant, BuildOptions options, bool buildAppBundle)
    {
        ApplySigningCredentialsIfAvailable();

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

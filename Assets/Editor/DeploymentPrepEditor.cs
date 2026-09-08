using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.IO;

/// <summary>
/// One-time deployment-prep utilities run via -executeMethod in batch mode:
/// imports the generated app-icon artwork with correct texture settings and
/// wires it into PlayerSettings for Android (legacy + adaptive) and iOS, since
/// both were previously empty icon slots. Not part of the regular build flow —
/// BuildScript.cs handles that; this only needs to run once (or whenever the
/// icon art changes).
/// </summary>
public static class DeploymentPrepEditor
{
    private const string IconDir = "Assets/_Project/Sprites/AppIcon";

    [MenuItem("Build/Deployment Prep/Run All")]
    public static void RunAll()
    {
        ApplyAppIcons();
        WireConsentManagerIntoScenes();
    }

    // NOTE: Unity 6000.3's layered adaptive-icon API (PlatformIconKind /
    // PlayerSettings.SetIcons with fore+background layers) did not match the
    // signature this was written against - Enum.GetNames threw "Type provided
    // must be an Enum" against the resolved type, meaning the real API in
    // this Editor version differs from the one these notes were based on.
    // Rather than guess further at an unfamiliar API, this only applies the
    // legacy/application icon (which Google Play and the Android launcher
    // both accept on their own - adaptive is a visual nicety, not a
    // submission requirement). The two adaptive-layer source images are still
    // generated on disk (Assets/_Project/Sprites/AppIcon/icon_adaptive_*.png)
    // for whoever wants to wire them up later from the Player Settings UI.
    [MenuItem("Build/Deployment Prep/Apply App Icons")]
    public static void ApplyAppIcons()
    {
        Texture2D legacy = LoadIconTexture($"{IconDir}/icon_legacy_1024.png");
        if (legacy == null)
        {
            Debug.LogError("[DeploymentPrep] Icon source texture failed to load. Aborting icon assignment.");
            return;
        }

        ApplyLegacyIcon(BuildTargetGroup.Android, legacy);
        ApplyLegacyIcon(BuildTargetGroup.iOS, legacy);

        AssetDatabase.SaveAssets();
        Debug.Log("[DeploymentPrep] App icon applied to PlayerSettings (Android + iOS).");
    }

    private static void ApplyLegacyIcon(BuildTargetGroup group, Texture2D legacy)
    {
        try
        {
            int[] appSizes = PlayerSettings.GetIconSizesForTargetGroup(group);
            Debug.Log($"[DeploymentPrep] {group} icon slots required: {string.Join(",", appSizes)}");
            var icons = new Texture2D[appSizes.Length];
            for (int i = 0; i < appSizes.Length; i++) icons[i] = legacy;
            PlayerSettings.SetIconsForTargetGroup(group, icons);
        }
        catch (Exception e)
        {
            Debug.LogError($"[DeploymentPrep] Icon assignment failed for {group}: {e}");
        }
    }

    /// <summary>
    /// AdManager/AnalyticsManager/ATTManager are each bundled directly on a
    /// "Managers" object in both MainMenu.unity and Gameplay.unity (whichever
    /// loads first wins as the DontDestroyOnLoad singleton; the other
    /// self-destructs). ConsentManager needs to run before AdManager on
    /// whichever one actually initializes, so it's added alongside it in both
    /// scenes to match the existing pattern.
    /// </summary>
    [MenuItem("Build/Deployment Prep/Wire Consent Manager Into Scenes")]
    public static void WireConsentManagerIntoScenes()
    {
        string[] scenePaths =
        {
            "Assets/_Project/Scenes/MainMenu.unity",
            "Assets/_Project/Scenes/Gameplay.unity"
        };

        foreach (string path in scenePaths)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            AdManager adManager = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                adManager = root.GetComponentInChildren<AdManager>(true);
                if (adManager != null) break;
            }

            if (adManager == null)
            {
                Debug.LogError($"[DeploymentPrep] No AdManager found in {path} - cannot wire ConsentManager.");
                continue;
            }

            var host = adManager.gameObject;
            if (host.GetComponent<ConsentManager>() == null)
            {
                Undo.AddComponent<ConsentManager>(host);
                Debug.Log($"[DeploymentPrep] Added ConsentManager to '{host.name}' in {path}.");
            }
            else
            {
                Debug.Log($"[DeploymentPrep] ConsentManager already present on '{host.name}' in {path}.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }

    private static Texture2D LoadIconTexture(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            bool changed = false;
            if (importer.textureType != TextureImporterType.Default) { importer.textureType = TextureImporterType.Default; changed = true; }
            if (!importer.isReadable) { importer.isReadable = true; changed = true; }
            if (importer.textureCompression != TextureImporterCompression.Uncompressed) { importer.textureCompression = TextureImporterCompression.Uncompressed; changed = true; }
            if (importer.mipmapEnabled) { importer.mipmapEnabled = false; changed = true; }
            if (changed)
            {
                importer.SaveAndReimport();
            }
        }
        else
        {
            Debug.LogError($"[DeploymentPrep] No importer found at {path} — does the file exist?");
        }

        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
}

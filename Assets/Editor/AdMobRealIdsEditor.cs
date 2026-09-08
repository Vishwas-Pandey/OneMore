using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Wires the real AdMob ad unit IDs (created under app com.onemorestudio.onemore,
/// AdMob app ID ca-app-pub-2894715849700422~8121251292) into every scene's
/// AdManager component and flips useTestAds off. Test ads stay the default in
/// the AdManager script itself so any future scene/prefab keeps failing safe
/// (visible placeholder test ads) until explicitly switched, but both real
/// scenes are switched here.
/// </summary>
public static class AdMobRealIdsEditor
{
    private const string RealInterstitialId = "ca-app-pub-2894715849700422/2781294704";
    private const string RealRewardedId = "ca-app-pub-2894715849700422/5048365394";
    private const string RealBannerId = "ca-app-pub-2894715849700422/6528968020";

    [MenuItem("Build/Ads/Apply Real AdMob IDs")]
    public static void ApplyAll()
    {
        ApplyToScene("Assets/_Project/Scenes/MainMenu.unity");
        ApplyToScene("Assets/_Project/Scenes/Gameplay.unity");
    }

    private static void ApplyToScene(string path)
    {
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        var adManager = Object.FindFirstObjectByType<AdManager>(FindObjectsInactive.Include);
        if (adManager == null)
        {
            Debug.LogWarning($"[AdMobRealIds] No AdManager found in {scene.name}, skipping.");
            return;
        }

        var so = new SerializedObject(adManager);
        so.FindProperty("config.androidInterstitialAdUnitId").stringValue = RealInterstitialId;
        so.FindProperty("config.androidRewardedAdUnitId").stringValue = RealRewardedId;
        so.FindProperty("config.androidBannerAdUnitId").stringValue = RealBannerId;
        so.FindProperty("useTestAds").boolValue = false;
        so.ApplyModifiedPropertiesWithoutUndo();

        Debug.Log($"[AdMobRealIds] {scene.name}: real Android ad unit IDs applied, useTestAds=false.");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}

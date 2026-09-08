using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Applies the new "grass block" theme requested for the Main Menu: an
/// original volcanic-sunset background, a chunky voxel-style title logo, and
/// a grass/dirt button texture reused across every button in the game (since
/// it overwrites button_bg.png in place under the same GUID). Also adds a
/// working Exit button. Only the Main Menu's background/title change -
/// Gameplay and Game Over keep their existing dark torch-lit look, since
/// that wasn't part of what was asked.
/// </summary>
public static class VoxelThemeEditor
{
    private const string VoxelDir = "Assets/_Project/Sprites/Voxel";
    private const string ButtonBgPath = "Assets/_Project/Sprites/Generated/button_bg.png";

    [MenuItem("Build/Diagnostics/Apply Voxel Theme")]
    public static void RunAll()
    {
        ConfigureTextures();
        PolishMainMenu();
    }

    private static void ConfigureTextures()
    {
        foreach (var path in new[] { $"{VoxelDir}/bg_volcanic.png", $"{VoxelDir}/title_logo.png", ButtonBgPath })
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) { Debug.LogError($"[VoxelTheme] No importer at {path}"); continue; }
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear; // smooth painted art, not pixel-art this time
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            Debug.Log($"[VoxelTheme] Configured {path} (Bilinear, Sprite).");
        }
    }

    private static GameObject FindDeep(Scene scene, string name)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            var t = FindRecursive(root.transform, name);
            if (t != null) return t.gameObject;
        }
        return null;
    }

    private static Transform FindRecursive(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            var found = FindRecursive(child, name);
            if (found != null) return found;
        }
        return null;
    }

    private static Sprite LoadSprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

    public static void PolishMainMenu()
    {
        var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/MainMenu.unity", OpenSceneMode.Single);

        // Background: swap the tiled dark pattern for the full-bleed volcanic
        // painting (Simple stretch - this is a single composed image, not a
        // repeating tile).
        var bgGo = FindDeep(scene, "MenuBackground");
        if (bgGo != null)
        {
            var img = bgGo.GetComponent<Image>();
            img.sprite = LoadSprite($"{VoxelDir}/bg_volcanic.png");
            img.type = Image.Type.Simple;
            img.color = Color.white;
        }

        // Title: keep the existing TitleText GameObject (and its bounce
        // animation, which targets this RectTransform) but show the new
        // logo image instead of the plain TMP text.
        var titleGo = FindDeep(scene, "TitleText");
        if (titleGo != null)
        {
            // A GameObject can only have one Graphic component, and both
            // Image and TextMeshProUGUI derive from Graphic - confirmed via
            // AddComponent<Image> throwing "already has TextMeshProUGUI"
            // when attempted directly on titleGo. The logo has to live on a
            // child instead; parenting it under titleGo means it still rides
            // along with the existing bounce animation (which moves
            // titleGo's RectTransform).
            var tmp = titleGo.GetComponent<TextMeshProUGUI>();
            if (tmp != null) tmp.text = "";

            var existingLogo = FindRecursive(titleGo.transform, "TitleLogoImage");
            GameObject logoGo = existingLogo != null ? existingLogo.gameObject : new GameObject("TitleLogoImage", typeof(RectTransform));
            logoGo.transform.SetParent(titleGo.transform, false);
            var logoRt = logoGo.GetComponent<RectTransform>();
            logoRt.anchorMin = Vector2.zero;
            logoRt.anchorMax = Vector2.one;
            logoRt.offsetMin = Vector2.zero;
            logoRt.offsetMax = Vector2.zero;

            var img = logoGo.GetComponent<Image>();
            if (img == null) img = logoGo.AddComponent<Image>();
            img.sprite = LoadSprite($"{VoxelDir}/title_logo.png");
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            img.color = Color.white;

            var rt = titleGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(820, 240);
        }

        // Subtitle/best-score text: warmer tones to suit the new background
        // instead of the neon green tuned for the old dark pattern.
        var subtitleGo = FindDeep(scene, "SubtitleText");
        if (subtitleGo != null)
        {
            var tmp = subtitleGo.GetComponent<TextMeshProUGUI>();
            if (tmp != null) tmp.color = new Color(1f, 0.96f, 0.88f, 1f);
        }
        var bestGo = FindDeep(scene, "BestScoreText");
        if (bestGo != null)
        {
            var tmp = bestGo.GetComponent<TextMeshProUGUI>();
            if (tmp != null) tmp.color = new Color(1f, 0.85f, 0.4f, 1f);
        }

        // The pixel-art torches suited the old dark hex-pattern background;
        // they'd clash with this painted volcanic scene, which already has
        // its own fire/lava motif. Remove them from the Main Menu only.
        foreach (var name in new[] { "TitleTorch_Left", "TitleTorch_Right" })
        {
            var go = FindDeep(scene, name);
            if (go != null) Object.DestroyImmediate(go);
        }

        AddExitButton(scene);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void AddExitButton(Scene scene)
    {
        if (FindDeep(scene, "ExitButton") != null)
        {
            Debug.Log("[VoxelTheme] ExitButton already present, skipping creation.");
            return;
        }

        var privacyGo = FindDeep(scene, "PrivacyButton");
        if (privacyGo == null) { Debug.LogError("[VoxelTheme] PrivacyButton not found - can't anchor ExitButton relative to it."); return; }

        var privacyRt = privacyGo.GetComponent<RectTransform>();
        var go = new GameObject("ExitButton", typeof(RectTransform));
        go.transform.SetParent(privacyGo.transform.parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = privacyRt.anchorMin;
        rt.anchorMax = privacyRt.anchorMax;
        rt.pivot = privacyRt.pivot;
        rt.sizeDelta = privacyRt.sizeDelta;
        rt.anchoredPosition = privacyRt.anchoredPosition + new Vector2(0, -140);

        var img = go.AddComponent<Image>();
        img.sprite = LoadSprite(ButtonBgPath);
        img.type = Image.Type.Simple;
        img.color = new Color(0.85f, 0.35f, 0.3f, 1f);

        var button = go.AddComponent<Button>();
        button.targetGraphic = img;

        var textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(go.transform, false);
        var textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = "EXIT";
        tmp.fontSize = 40;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        var controllerGo = FindDeep(scene, "MainMenuController");
        if (controllerGo != null)
        {
            var so = new SerializedObject(controllerGo.GetComponent<MainMenuController>());
            so.FindProperty("exitButton").objectReferenceValue = button;
            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[VoxelTheme] Added ExitButton and wired it to MainMenuController.");
        }
        else
        {
            Debug.LogError("[VoxelTheme] MainMenuController GameObject not found - ExitButton created but not wired.");
        }
    }
}

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// One-time visual-polish pass: wires the two new background textures
/// (sky_bg.png for Gameplay, menu_bg.png for MainMenu) into their scenes as
/// GameObjects, and recolors specific named buttons/panels/text to match the
/// glossy-gradient reference style (was flat placeholder colors before).
/// Run once via -executeMethod; safe to re-run (checks for existing objects).
/// </summary>
public static class VisualPolishEditor
{
    [MenuItem("Build/Visual Polish/Run All")]
    public static void RunAll()
    {
        ConfigureNewBackgroundSprites();
        PolishGameplayScene();
        PolishMainMenuScene();
    }

    private static void ConfigureNewBackgroundSprites()
    {
        foreach (var path in new[]
        {
            "Assets/_Project/Sprites/Generated/sky_bg.png",
            "Assets/_Project/Sprites/Generated/menu_bg.png"
        })
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"[VisualPolish] No importer at {path}");
                continue;
            }
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            Debug.Log($"[VisualPolish] Configured {path} as a Sprite.");
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

    private static void SetImageColor(Scene scene, string name, Color color)
    {
        var go = FindDeep(scene, name);
        if (go == null) { Debug.LogWarning($"[VisualPolish] '{name}' not found in {scene.name}"); return; }
        var img = go.GetComponent<Image>();
        if (img != null) { img.color = color; Debug.Log($"[VisualPolish] {scene.name}: set {name} Image color to {color}"); }
        else Debug.LogWarning($"[VisualPolish] '{name}' in {scene.name} has no Image component");
    }

    private static void SetTMPColor(Scene scene, string name, Color color)
    {
        var go = FindDeep(scene, name);
        if (go == null) { Debug.LogWarning($"[VisualPolish] '{name}' not found in {scene.name}"); return; }
        var tmp = go.GetComponent<TextMeshProUGUI>();
        if (tmp != null) { tmp.color = color; Debug.Log($"[VisualPolish] {scene.name}: set {name} text color to {color}"); }
        else Debug.LogWarning($"[VisualPolish] '{name}' in {scene.name} has no TextMeshProUGUI component");
    }

    private static Sprite LoadSprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

    public static void PolishGameplayScene()
    {
        var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Gameplay.unity", OpenSceneMode.Single);

        if (FindDeep(scene, "SkyBackground") == null)
        {
            var bgGo = new GameObject("SkyBackground");
            var sr = bgGo.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite("Assets/_Project/Sprites/Generated/sky_bg.png");
            sr.sortingOrder = -100;
            bgGo.transform.position = new Vector3(0, 0, 10);
            bgGo.transform.localScale = new Vector3(40, 40, 1);
            Debug.Log("[VisualPolish] Added SkyBackground to Gameplay scene");
        }
        else
        {
            Debug.Log("[VisualPolish] SkyBackground already present in Gameplay scene");
        }

        SetImageColor(scene, "RestartButton", new Color(0.20f, 0.60f, 0.94f, 1f));
        SetImageColor(scene, "ContinueButton", new Color(1f, 0.80f, 0f, 1f));
        SetImageColor(scene, "MenuButton", new Color(0.55f, 0.55f, 0.60f, 1f));
        SetImageColor(scene, "GameOverPanel", new Color(0.08f, 0.09f, 0.13f, 0.94f));

        SetTMPColor(scene, "ScoreText", new Color(0.06f, 0.09f, 0.15f, 1f));
        SetTMPColor(scene, "CountdownText", new Color(0.06f, 0.09f, 0.15f, 1f));

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    public static void PolishMainMenuScene()
    {
        var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/MainMenu.unity", OpenSceneMode.Single);

        var canvasGo = FindDeep(scene, "Canvas");
        if (canvasGo != null && FindDeep(scene, "MenuBackground") == null)
        {
            var bgGo = new GameObject("MenuBackground", typeof(RectTransform));
            bgGo.transform.SetParent(canvasGo.transform, false);
            bgGo.transform.SetAsFirstSibling();
            var rt = bgGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = bgGo.AddComponent<Image>();
            img.sprite = LoadSprite("Assets/_Project/Sprites/Generated/menu_bg.png");
            img.type = Image.Type.Simple;
            img.color = Color.white;
            Debug.Log("[VisualPolish] Added MenuBackground to MainMenu scene");
        }
        else if (canvasGo == null)
        {
            Debug.LogError("[VisualPolish] Canvas not found in MainMenu scene");
        }
        else
        {
            Debug.Log("[VisualPolish] MenuBackground already present in MainMenu scene");
        }

        SetImageColor(scene, "PlayButton", new Color(0.30f, 0.69f, 0.31f, 1f));
        SetImageColor(scene, "SettingsButton", new Color(0.55f, 0.55f, 0.60f, 1f));
        SetImageColor(scene, "PrivacyButton", new Color(0.55f, 0.55f, 0.60f, 1f));
        SetImageColor(scene, "CloseButton", new Color(0.90f, 0.30f, 0.30f, 1f));
        SetImageColor(scene, "SettingsPanel", new Color(0.08f, 0.09f, 0.13f, 0.94f));
        SetImageColor(scene, "PrivacyPanel", new Color(0.08f, 0.09f, 0.13f, 0.94f));

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}

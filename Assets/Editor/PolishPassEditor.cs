using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Follow-up polish pass: fixes MainMenuPanel's leftover opaque fill (it was
/// covering the new pattern background), adds torch decorations flanking the
/// title on MainMenu and the "GAME OVER" label on Gameplay to match the
/// reference composition, and adds a themed pause button to the gameplay HUD.
/// </summary>
public static class PolishPassEditor
{
    private const string TorchDecoPath = "Assets/_Project/Sprites/Torch/torch_decoration.png";
    private const string ButtonBgPath = "Assets/_Project/Sprites/Generated/button_bg.png";

    [MenuItem("Build/Diagnostics/Run Polish Pass 2")]
    public static void RunAll()
    {
        ConfigureTorchDecoTexture();
        PolishMainMenu();
        PolishGameplay();
    }

    private static void ConfigureTorchDecoTexture()
    {
        var importer = AssetImporter.GetAtPath(TorchDecoPath) as TextureImporter;
        if (importer == null) { Debug.LogError($"[Polish2] No importer at {TorchDecoPath}"); return; }
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        Debug.Log($"[Polish2] Configured {TorchDecoPath} as a Sprite.");
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

    private static void AddTorchPair(Transform parent, float y, string namePrefix)
    {
        var torchSprite = LoadSprite(TorchDecoPath);
        foreach (float x in new[] { -430f, 430f })
        {
            string name = $"{namePrefix}_{(x < 0 ? "Left" : "Right")}";
            if (FindRecursive(parent, name) != null) continue;

            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.transform.SetAsFirstSibling();
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(150, 460);

            var img = go.AddComponent<Image>();
            img.sprite = torchSprite;
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            img.color = Color.white;
        }
    }

    public static void PolishMainMenu()
    {
        var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/MainMenu.unity", OpenSceneMode.Single);

        var panelGo = FindDeep(scene, "MainMenuPanel");
        if (panelGo != null)
        {
            var img = panelGo.GetComponent<Image>();
            if (img != null)
            {
                var c = img.color;
                c.a = 0f;
                img.color = c;
                Debug.Log("[Polish2] MainMenuPanel background made transparent (was covering the pattern background).");
            }
        }

        var canvasGo = FindDeep(scene, "Canvas");
        if (canvasGo != null)
        {
            AddTorchPair(canvasGo.transform, 480f, "TitleTorch");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    public static void PolishGameplay()
    {
        var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Gameplay.unity", OpenSceneMode.Single);

        var gameOverPanelGo = FindDeep(scene, "GameOverPanel");
        if (gameOverPanelGo != null)
        {
            AddTorchPair(gameOverPanelGo.transform, 380f, "GameOverTorch");
        }

        AddPauseButton(scene);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void AddPauseButton(Scene scene)
    {
        var hudGo = FindDeep(scene, "HUD");
        if (hudGo == null) { Debug.LogError("[Polish2] HUD not found in Gameplay scene"); return; }
        if (FindRecursive(hudGo.transform, "PauseButton") != null)
        {
            Debug.Log("[Polish2] PauseButton already present, skipping.");
            return;
        }

        var go = new GameObject("PauseButton", typeof(RectTransform));
        go.transform.SetParent(hudGo.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(30, -50);
        rt.sizeDelta = new Vector2(160, 90);

        var img = go.AddComponent<Image>();
        img.sprite = LoadSprite(ButtonBgPath);
        img.type = Image.Type.Simple;
        img.color = new Color(0.6f, 0.6f, 0.6f, 1f);

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
        tmp.text = "PAUSE";
        tmp.fontSize = 32;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        // Wire into GameplayUIController's serialized fields.
        var controllerGo = FindDeep(scene, "GameplayUIController");
        if (controllerGo != null)
        {
            var so = new SerializedObject(controllerGo.GetComponent<GameplayUIController>());
            so.FindProperty("pauseButton").objectReferenceValue = button;
            so.FindProperty("pauseButtonText").objectReferenceValue = tmp;
            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[Polish2] Added PauseButton and wired it to GameplayUIController.");
        }
        else
        {
            Debug.LogError("[Polish2] GameplayUIController GameObject not found - PauseButton created but not wired.");
        }
    }
}

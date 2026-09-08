using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.IO;

/// <summary>
/// One-time rebuild-wiring pass for the flappy-bird conversion: configures
/// the new pixel-art textures, builds the Pillar prefab, adds a Ceiling to
/// match the existing Ground, retags/retriggers both for the new death-on-
/// touch rule, repoints the background layers to the new dark pixel pattern,
/// wires ObstacleSpawner's new fields, recolors UI for the neon-retro theme,
/// and wires the real audio clips into AudioManager. Safe to re-run.
/// </summary>
public static class FlappyRebuildEditor
{
    private const string SpriteDir = "Assets/_Project/Sprites/Generated";
    private const string TorchDir = "Assets/_Project/Sprites/Torch";
    private const string AudioDir = "Assets/_Project/Audio";

    [MenuItem("Build/Flappy Rebuild/Run All")]
    public static void RunAll()
    {
        ConfigureNewTextures();
        BuildPillarPrefab();
        PolishGameplayScene();
        PolishMainMenuScene();
    }

    private static void ConfigureNewTextures()
    {
        foreach (var path in new[]
        {
            $"{SpriteDir}/bg_pattern.png",
            $"{TorchDir}/flame_0.png",
            $"{TorchDir}/shaft.png",
        })
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) { Debug.LogError($"[FlappyRebuild] No importer at {path}"); continue; }
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spritePixelsPerUnit = 300;
            importer.SaveAndReimport();
            Debug.Log($"[FlappyRebuild] Configured {path} (Point filter, Sprite, 300 PPU).");
        }
    }

    private static Sprite LoadSprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

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
        if (go == null) { Debug.LogWarning($"[FlappyRebuild] '{name}' not found in {scene.name}"); return; }
        var img = go.GetComponent<Image>();
        if (img != null) { img.color = color; Debug.Log($"[FlappyRebuild] {scene.name}: {name} Image color -> {color}"); }
    }

    private static void SetTMPColor(Scene scene, string name, Color color)
    {
        var go = FindDeep(scene, name);
        if (go == null) { Debug.LogWarning($"[FlappyRebuild] '{name}' not found in {scene.name}"); return; }
        var tmp = go.GetComponent<TextMeshProUGUI>();
        if (tmp != null) { tmp.color = color; Debug.Log($"[FlappyRebuild] {scene.name}: {name} text color -> {color}"); }
    }

    // ---------------- Pillar prefab ----------------

    private const string PillarPrefabPath = "Assets/_Project/Prefabs/Obstacles/Pillar.prefab";

    private static void BuildPillarPrefab()
    {
        Directory.CreateDirectory("Assets/_Project/Prefabs/Obstacles");

        var root = new GameObject("Pillar");
        root.layer = LayerMask.NameToLayer("Default");
        root.tag = "Obstacle";

        var shaftGo = new GameObject("Shaft");
        shaftGo.transform.SetParent(root.transform, false);
        var shaftSr = shaftGo.AddComponent<SpriteRenderer>();
        shaftSr.sprite = LoadSprite($"{TorchDir}/shaft.png");
        shaftSr.sortingOrder = 0;

        var flameGo = new GameObject("FlameCap");
        flameGo.transform.SetParent(root.transform, false);
        var flameSr = flameGo.AddComponent<SpriteRenderer>();
        flameSr.sprite = LoadSprite($"{TorchDir}/flame_0.png");
        flameSr.sortingOrder = 1;

        var box = root.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(0.5f, 1f);

        root.AddComponent<Poolable>();
        root.AddComponent<TorchPillar>();

        // Wire the TorchPillar's serialized references via SerializedObject
        // since it's a fresh component with private [SerializeField] fields.
        var so = new SerializedObject(root.GetComponent<TorchPillar>());
        so.FindProperty("shaft").objectReferenceValue = shaftGo.transform;
        so.FindProperty("flameCap").objectReferenceValue = flameGo.transform;
        so.FindProperty("boxCollider").objectReferenceValue = box;
        so.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, PillarPrefabPath);
        Object.DestroyImmediate(root);
        Debug.Log($"[FlappyRebuild] Built pillar prefab at {PillarPrefabPath}");
    }

    // ---------------- Gameplay scene ----------------

    public static void PolishGameplayScene()
    {
        var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Gameplay.unity", OpenSceneMode.Single);

        const float floorY = -4.6f;
        const float ceilingY = 4.6f;

        // Background: repoint to the dark pixel pattern, tiled to cover the screen.
        var skyGo = FindDeep(scene, "SkyBackground");
        if (skyGo != null)
        {
            var sr = skyGo.GetComponent<SpriteRenderer>();
            sr.sprite = LoadSprite($"{SpriteDir}/bg_pattern.png");
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(20f, 14f);
            sr.sortingOrder = -100;
        }

        // Ground: death trigger instead of solid floor.
        var groundGo = FindDeep(scene, "Ground");
        if (groundGo != null)
        {
            groundGo.tag = "Ground";
            var col = groundGo.GetComponent<BoxCollider2D>();
            if (col != null) col.isTrigger = true;
            groundGo.transform.position = new Vector3(groundGo.transform.position.x, floorY, groundGo.transform.position.z);
        }

        // Ceiling: mirror of Ground at the top, added fresh if missing.
        var ceilingGo = FindDeep(scene, "Ceiling");
        if (ceilingGo == null && groundGo != null)
        {
            ceilingGo = Object.Instantiate(groundGo);
            ceilingGo.name = "Ceiling";
            ceilingGo.tag = "Ceiling";
            ceilingGo.transform.position = new Vector3(groundGo.transform.position.x, ceilingY, 0f);
            ceilingGo.transform.localScale = groundGo.transform.localScale;
            var csr = ceilingGo.GetComponent<SpriteRenderer>();
            if (csr != null) csr.flipY = true;
            Debug.Log("[FlappyRebuild] Added Ceiling to Gameplay scene");
        }

        // Player sprite already repointed via same-GUID texture swap; just
        // confirm rotation reset (bird tilts via code, base rotation must be zero).
        var playerGo = FindDeep(scene, "Player");
        if (playerGo != null) playerGo.transform.rotation = Quaternion.identity;

        // Wire ObstacleSpawner's new fields.
        var spawnerGo = FindDeep(scene, "ObstacleSpawner");
        var scoreManagerGo = FindDeep(scene, "ScoreManager");
        if (spawnerGo != null)
        {
            var spawnerSo = new SerializedObject(spawnerGo.GetComponent<ObstacleSpawner>());
            var pillarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PillarPrefabPath);
            spawnerSo.FindProperty("pillarPrefab").objectReferenceValue = pillarPrefab;
            if (playerGo != null) spawnerSo.FindProperty("playerTransform").objectReferenceValue = playerGo.transform;
            if (scoreManagerGo != null) spawnerSo.FindProperty("scoreManager").objectReferenceValue = scoreManagerGo.GetComponent<ScoreManager>();
            spawnerSo.FindProperty("floorY").floatValue = floorY + 0.3f;
            spawnerSo.FindProperty("ceilingY").floatValue = ceilingY - 0.3f;
            spawnerSo.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[FlappyRebuild] Wired ObstacleSpawner fields");
        }
        else
        {
            Debug.LogError("[FlappyRebuild] ObstacleSpawner GameObject not found in Gameplay scene");
        }

        // Camera background: near-black fallback behind the tiled pattern.
        var camGo = FindDeep(scene, "Main Camera");
        if (camGo != null)
        {
            var cam = camGo.GetComponent<Camera>();
            cam.backgroundColor = new Color(0.05f, 0.05f, 0.06f, 1f);
            if (camGo.GetComponent<AudioListener>() == null)
            {
                camGo.AddComponent<AudioListener>();
                Debug.Log($"[FlappyRebuild] Added missing AudioListener to Main Camera in {scene.name}");
            }
        }

        // Neon-retro UI recolor.
        SetImageColor(scene, "RestartButton", new Color(0.2f, 0.6f, 0.94f, 1f));
        SetImageColor(scene, "ContinueButton", new Color(1f, 0.8f, 0f, 1f));
        SetImageColor(scene, "MenuButton", new Color(0.6f, 0.6f, 0.6f, 1f));
        SetImageColor(scene, "GameOverPanel", Color.white); // panel_bg art is already the dark bevel card

        var neonGreen = new Color(0.22f, 1f, 0.08f, 1f);
        SetTMPColor(scene, "ScoreText", neonGreen);
        SetTMPColor(scene, "CountdownText", neonGreen);
        SetTMPColor(scene, "FinalScoreText", neonGreen);

        // Audio: wire the real clips into this scene's AudioManager instance.
        WireAudio(scene);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    // ---------------- Main menu scene ----------------

    public static void PolishMainMenuScene()
    {
        var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/MainMenu.unity", OpenSceneMode.Single);

        var menuBgGo = FindDeep(scene, "MenuBackground");
        if (menuBgGo != null)
        {
            var img = menuBgGo.GetComponent<Image>();
            img.sprite = LoadSprite($"{SpriteDir}/bg_pattern.png");
            img.type = Image.Type.Tiled;
            img.color = Color.white;
        }

        SetImageColor(scene, "PlayButton", new Color(0.30f, 0.85f, 0.30f, 1f));
        SetImageColor(scene, "SettingsButton", new Color(0.6f, 0.6f, 0.6f, 1f));
        SetImageColor(scene, "PrivacyButton", new Color(0.6f, 0.6f, 0.6f, 1f));
        SetImageColor(scene, "CloseButton", new Color(0.9f, 0.3f, 0.3f, 1f));
        SetImageColor(scene, "SettingsPanel", Color.white);
        SetImageColor(scene, "PrivacyPanel", Color.white);

        var neonGreen = new Color(0.22f, 1f, 0.08f, 1f);
        SetTMPColor(scene, "TitleText", neonGreen);
        SetTMPColor(scene, "SubtitleText", Color.white);
        SetTMPColor(scene, "BestScoreText", neonGreen);

        var camGo = FindDeep(scene, "Main Camera");
        if (camGo != null)
        {
            var cam = camGo.GetComponent<Camera>();
            cam.backgroundColor = new Color(0.05f, 0.05f, 0.06f, 1f);
            if (camGo.GetComponent<AudioListener>() == null)
            {
                camGo.AddComponent<AudioListener>();
                Debug.Log($"[FlappyRebuild] Added missing AudioListener to Main Camera in {scene.name}");
            }
        }

        WireAudio(scene);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    // ---------------- Audio wiring ----------------

    private static void WireAudio(Scene scene)
    {
        var audioGo = FindDeep(scene, "AudioManager");
        if (audioGo == null) { Debug.LogWarning($"[FlappyRebuild] AudioManager not found in {scene.name}"); return; }

        var flap = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioDir}/flap.mp3");
        var point = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioDir}/point.mp3");
        var hit = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioDir}/hit.mp3");
        var bg = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioDir}/bg.mp3");

        if (flap == null || point == null || hit == null || bg == null)
        {
            Debug.LogError("[FlappyRebuild] One or more audio clips failed to load - check they were imported.");
            return;
        }

        var so = new SerializedObject(audioGo.GetComponent<AudioManager>());
        var soundsProp = so.FindProperty("sounds");
        soundsProp.arraySize = 3;
        ConfigureSoundElement(soundsProp.GetArrayElementAtIndex(0), "jump", flap, 1f);
        ConfigureSoundElement(soundsProp.GetArrayElementAtIndex(1), "score", point, 1f);
        ConfigureSoundElement(soundsProp.GetArrayElementAtIndex(2), "gameover", hit, 1f);

        var musicProp = so.FindProperty("musicTracks");
        musicProp.arraySize = 1;
        musicProp.GetArrayElementAtIndex(0).objectReferenceValue = bg;

        so.ApplyModifiedPropertiesWithoutUndo();
        Debug.Log($"[FlappyRebuild] {scene.name}: wired AudioManager sounds (jump/score/gameover) + music track");
    }

    private static void ConfigureSoundElement(SerializedProperty element, string name, AudioClip clip, float volume)
    {
        element.FindPropertyRelative("name").stringValue = name;
        element.FindPropertyRelative("clip").objectReferenceValue = clip;
        element.FindPropertyRelative("volume").floatValue = volume;
        element.FindPropertyRelative("pitch").floatValue = 1f;
        element.FindPropertyRelative("loop").boolValue = false;
    }
}

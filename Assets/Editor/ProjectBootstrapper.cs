using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

/// <summary>
/// One-shot headless project builder, run via:
///   Unity -batchmode -projectPath "..." -executeMethod ProjectBootstrapper.Bootstrap -quit
/// Generates placeholder art, prefabs, and the three scenes (Splash/MainMenu/Gameplay)
/// with every component wired up, since no hand-authored scenes/art exist yet.
/// Not meant to be a permanent part of the shipped project - it's a bootstrap tool.
/// </summary>
public static class ProjectBootstrapper
{
    private const string SpriteDir = "Assets/_Project/Sprites/Generated";
    private const string PrefabDir = "Assets/_Project/Prefabs";
    private const string SceneDir = "Assets/_Project/Scenes";

    private static readonly Color BgColor = new Color32(0x0F, 0x17, 0x2A, 0xFF);
    private static readonly Color SurfaceColor = new Color32(0x1E, 0x29, 0x3B, 0xFF);
    private static readonly Color PrimaryColor = new Color32(0x4A, 0x9E, 0xFF, 0xFF);
    private static readonly Color DangerColor = new Color32(0xEF, 0x44, 0x44, 0xFF);
    private static readonly Color TextColor = Color.white;

    public static void Bootstrap()
    {
        Debug.Log("[Bootstrap] Starting...");

        PlayerSettings.companyName = "OneMoreStudio";
        PlayerSettings.productName = "One More";
        PlayerSettings.colorSpace = ColorSpace.Linear;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;

        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.onemorestudio.onemore");
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.onemorestudio.onemore");
        PlayerSettings.iOS.targetOSVersionString = "13.0";

        Sprite playerSprite = CreateCircleSprite("player", 64, PrimaryColor);
        Sprite obstacleSprite = CreateRectSprite("obstacle", 32, 96, DangerColor);
        Sprite groundSprite = CreateRectSprite("ground", 64, 32, SurfaceColor);
        Sprite panelSprite = CreateRectSprite("panel_bg", 4, 4, SurfaceColor);
        Sprite buttonSprite = CreateRectSprite("button_bg", 4, 4, PrimaryColor);

        EnsureLayer("Ground");
        EnsureTag("Obstacle");

        GameObject obstaclePrefab = BuildObstaclePrefab(obstacleSprite);
        GameObject playerPrefab = BuildPlayerPrefab(playerSprite);

        BuildMainMenuScene(panelSprite, buttonSprite);
        BuildGameplayScene(playerPrefab, obstaclePrefab, groundSprite, panelSprite, buttonSprite);
        BuildSplashScene();

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene($"{SceneDir}/Splash.unity", true),
            new EditorBuildSettingsScene($"{SceneDir}/MainMenu.unity", true),
            new EditorBuildSettingsScene($"{SceneDir}/Gameplay.unity", true),
        };

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Bootstrap] Done.");
    }

    // ---------- Placeholder art ----------

    private static Sprite CreateCircleSprite(string name, int size, Color color)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color[size * size];
        float r = size / 2f;
        Vector2 center = new Vector2(r, r);
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
            pixels[y * size + x] = d <= r ? color : new Color(0, 0, 0, 0);
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return SaveTextureAsSprite(tex, name);
    }

    private static Sprite CreateRectSprite(string name, int w, int h, Color color)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var pixels = new Color[w * h];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
        tex.SetPixels(pixels);
        tex.Apply();
        return SaveTextureAsSprite(tex, name);
    }

    private static Sprite SaveTextureAsSprite(Texture2D tex, string name)
    {
        Directory.CreateDirectory(SpriteDir);
        string path = $"{SpriteDir}/{name}.png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 100;
        importer.filterMode = FilterMode.Bilinear;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // ---------- Layers / Tags ----------

    private static void EnsureLayer(string layerName)
    {
        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layersProp = tagManager.FindProperty("layers");
        for (int i = 8; i < layersProp.arraySize; i++)
        {
            var sp = layersProp.GetArrayElementAtIndex(i);
            if (sp.stringValue == layerName) return;
        }
        for (int i = 8; i < layersProp.arraySize; i++)
        {
            var sp = layersProp.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(sp.stringValue))
            {
                sp.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                return;
            }
        }
        Debug.LogWarning("[Bootstrap] No free layer slot found for " + layerName);
    }

    private static void EnsureTag(string tagName)
    {
        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var tagsProp = tagManager.FindProperty("tags");
        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            if (tagsProp.GetArrayElementAtIndex(i).stringValue == tagName) return;
        }
        tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
        tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tagName;
        tagManager.ApplyModifiedProperties();
    }

    // ---------- Reflection helper (private [SerializeField] assignment) ----------

    private static void SetField(object target, string name, object value)
    {
        var f = target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        if (f == null)
        {
            Debug.LogError($"[Bootstrap] Field '{name}' not found on {target.GetType().Name}");
            return;
        }
        f.SetValue(target, value);
    }

    // ---------- Prefabs ----------

    private static GameObject BuildObstaclePrefab(Sprite sprite)
    {
        Directory.CreateDirectory($"{PrefabDir}/Obstacles");

        var go = new GameObject("Obstacle_Basic");
        go.tag = "Obstacle";
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        go.AddComponent<Obstacle>();

        string path = $"{PrefabDir}/Obstacles/Obstacle_Basic.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    private static GameObject BuildPlayerPrefab(Sprite sprite)
    {
        Directory.CreateDirectory($"{PrefabDir}/Player");

        var go = new GameObject("Player");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.32f;

        var groundCheck = new GameObject("GroundCheck");
        groundCheck.transform.SetParent(go.transform, false);
        groundCheck.transform.localPosition = new Vector3(0, -0.35f, 0);

        var controller = go.AddComponent<PlayerController>();
        SetField(controller, "groundCheck", groundCheck.transform);
        SetField(controller, "groundLayer", (LayerMask)LayerMask.GetMask("Ground"));

        string path = $"{PrefabDir}/Player/Player.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    // ---------- UI helpers ----------

    private static GameObject CreateCanvas(string name)
    {
        var canvasGO = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }
        return canvasGO;
    }

    private static RectTransform AddPanel(Transform parent, string name, Sprite bg, Color color, bool stretch = true)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        if (stretch)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
        var img = go.GetComponent<Image>();
        img.sprite = bg;
        img.type = Image.Type.Sliced;
        img.color = color;
        return rt;
    }

    private static TextMeshProUGUI AddText(Transform parent, string name, string text, float fontSize, Vector2 anchoredPos, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        return tmp;
    }

    private static Button AddButton(Transform parent, string name, string label, Sprite bg, Vector2 anchoredPos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;

        var img = go.GetComponent<Image>();
        img.sprite = bg;
        img.type = Image.Type.Sliced;
        img.color = PrimaryColor;

        AddText(go.transform, "Label", label, 32, Vector2.zero, size, TextColor);

        return go.GetComponent<Button>();
    }

    private static void Persist(Button button, Object target, string methodName)
    {
        var method = target.GetType().GetMethod(methodName);
        var action = System.Delegate.CreateDelegate(typeof(UnityEngine.Events.UnityAction), target, method) as UnityEngine.Events.UnityAction;
        UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick, action);
    }

    private static GameObject AddCamera(Color bg)
    {
        var camGO = new GameObject("Main Camera", typeof(Camera));
        camGO.tag = "MainCamera";
        var cam = camGO.GetComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.backgroundColor = bg;
        cam.clearFlags = CameraClearFlags.SolidColor;
        camGO.transform.position = new Vector3(0, 0, -10);
        return camGO;
    }

    private static GameObject AddManagers()
    {
        var root = new GameObject("Managers");

        root.AddComponent<GameManager>();

        var audioGO = new GameObject("AudioManager");
        audioGO.transform.SetParent(root.transform);
        var audio = audioGO.AddComponent<AudioManager>();
        var musicSource = audioGO.AddComponent<AudioSource>();
        var sfxSource = audioGO.AddComponent<AudioSource>();
        SetField(audio, "musicSource", musicSource);
        SetField(audio, "sfxSource", sfxSource);
        SetField(audio, "sounds", new AudioManager.Sound[0]);
        SetField(audio, "musicTracks", new AudioClip[0]);

        var hapticGO = new GameObject("HapticManager");
        hapticGO.transform.SetParent(root.transform);
        hapticGO.AddComponent<HapticManager>();

        var adGO = new GameObject("AdManager");
        adGO.transform.SetParent(root.transform);
        adGO.AddComponent<AdManager>();

        var analyticsGO = new GameObject("AnalyticsManager");
        analyticsGO.transform.SetParent(root.transform);
        analyticsGO.AddComponent<AnalyticsManager>();

        var attGO = new GameObject("ATTManager");
        attGO.transform.SetParent(root.transform);
        attGO.AddComponent<ATTManager>();

        return root;
    }

    // ---------- Scenes ----------

    private static void BuildSplashScene()
    {
        Directory.CreateDirectory(SceneDir);
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        AddCamera(BgColor);

        var canvasGO = CreateCanvas("Canvas");
        AddText(canvasGO.transform, "SplashText", "ONE MORE", 72, Vector2.zero, new Vector2(800, 200), TextColor);

        var loaderGO = new GameObject("SplashLoader");
        loaderGO.AddComponent<SplashLoader>();

        EditorSceneManager.SaveScene(scene, $"{SceneDir}/Splash.unity");
    }

    private static void BuildMainMenuScene(Sprite panelSprite, Sprite buttonSprite)
    {
        Directory.CreateDirectory(SceneDir);
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        AddCamera(BgColor);
        AddManagers();

        var canvasGO = CreateCanvas("Canvas");
        var safeAreaGO = new GameObject("SafeArea", typeof(RectTransform));
        safeAreaGO.transform.SetParent(canvasGO.transform, false);
        var safeRT = safeAreaGO.GetComponent<RectTransform>();
        safeRT.anchorMin = Vector2.zero;
        safeRT.anchorMax = Vector2.one;
        safeRT.offsetMin = Vector2.zero;
        safeRT.offsetMax = Vector2.zero;
        safeAreaGO.AddComponent<SafeAreaFitter>();

        var mainPanelRT = AddPanel(safeAreaGO.transform, "MainMenuPanel", panelSprite, BgColor);
        var mainPanel = mainPanelRT.gameObject;

        var title = AddText(mainPanel.transform, "TitleText", "ONE MORE", 96, new Vector2(0, 600), new Vector2(900, 150), TextColor);
        var subtitle = AddText(mainPanel.transform, "SubtitleText", "tap to jump. just one more try.", 32, new Vector2(0, 480), new Vector2(900, 80), new Color(1, 1, 1, 0.7f));
        var bestScore = AddText(mainPanel.transform, "BestScoreText", "BEST: 0000", 40, new Vector2(0, 380), new Vector2(700, 60), TextColor);

        var playButton = AddButton(mainPanel.transform, "PlayButton", "PLAY", buttonSprite, new Vector2(0, 0), new Vector2(400, 140));
        var settingsButton = AddButton(mainPanel.transform, "SettingsButton", "SETTINGS", buttonSprite, new Vector2(0, -200), new Vector2(400, 100));
        var privacyButton = AddButton(mainPanel.transform, "PrivacyButton", "PRIVACY POLICY", buttonSprite, new Vector2(0, -320), new Vector2(400, 80));
        var versionText = AddText(mainPanel.transform, "VersionText", "v1.0.0", 24, new Vector2(0, -800), new Vector2(300, 50), new Color(1, 1, 1, 0.5f));

        var settingsPanelRT = AddPanel(safeAreaGO.transform, "SettingsPanel", panelSprite, SurfaceColor);
        var settingsPanel = settingsPanelRT.gameObject;
        var soundToggle = AddButton(settingsPanel.transform, "SoundToggleButton", "ON", buttonSprite, new Vector2(0, 200), new Vector2(300, 100));
        var musicToggle = AddButton(settingsPanel.transform, "MusicToggleButton", "ON", buttonSprite, new Vector2(0, 60), new Vector2(300, 100));
        var hapticsToggle = AddButton(settingsPanel.transform, "HapticsToggleButton", "ON", buttonSprite, new Vector2(0, -80), new Vector2(300, 100));
        var closeSettings = AddButton(settingsPanel.transform, "CloseButton", "CLOSE", buttonSprite, new Vector2(0, -300), new Vector2(300, 100));
        settingsPanel.SetActive(false);

        var privacyPanelRT = AddPanel(safeAreaGO.transform, "PrivacyPanel", panelSprite, SurfaceColor);
        var privacyPanel = privacyPanelRT.gameObject;
        AddText(privacyPanel.transform, "PrivacyBody", "Replace this with your real privacy policy text or a link before shipping.", 28, Vector2.zero, new Vector2(800, 400), TextColor);
        var closePrivacy = AddButton(privacyPanel.transform, "CloseButton", "CLOSE", buttonSprite, new Vector2(0, -600), new Vector2(300, 100));
        privacyPanel.SetActive(false);

        var controllerGO = new GameObject("MainMenuController");
        var controller = controllerGO.AddComponent<MainMenuController>();
        SetField(controller, "mainMenuPanel", mainPanel);
        SetField(controller, "settingsPanel", settingsPanel);
        SetField(controller, "privacyPanel", privacyPanel);
        SetField(controller, "playButton", playButton);
        SetField(controller, "settingsButton", settingsButton);
        SetField(controller, "privacyButton", privacyButton);
        SetField(controller, "soundToggleButton", soundToggle);
        SetField(controller, "musicToggleButton", musicToggle);
        SetField(controller, "hapticsToggleButton", hapticsToggle);
        SetField(controller, "bestScoreText", bestScore);
        SetField(controller, "versionText", versionText);
        SetField(controller, "titleText", title);
        SetField(controller, "subtitleText", subtitle);

        Persist(closeSettings, controller, "OnSettingsClicked");
        Persist(closePrivacy, controller, "OnPrivacyClicked");
        Persist(soundToggle, controller, "ToggleSound");
        Persist(musicToggle, controller, "ToggleMusic");
        Persist(hapticsToggle, controller, "ToggleHaptics");

        EditorSceneManager.SaveScene(scene, $"{SceneDir}/MainMenu.unity");
    }

    private static void BuildGameplayScene(GameObject playerPrefab, GameObject obstaclePrefab, Sprite groundSprite, Sprite panelSprite, Sprite buttonSprite)
    {
        Directory.CreateDirectory(SceneDir);
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        AddCamera(new Color32(0x33, 0x55, 0x88, 0xFF));

        var groundGO = new GameObject("Ground");
        groundGO.layer = LayerMask.NameToLayer("Ground");
        groundGO.transform.position = new Vector3(0, -3f, 0);
        groundGO.transform.localScale = new Vector3(20f, 1f, 1f);
        var groundSR = groundGO.AddComponent<SpriteRenderer>();
        groundSR.sprite = groundSprite;
        var groundCol = groundGO.AddComponent<BoxCollider2D>();

        var playerInstance = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
        playerInstance.transform.position = new Vector3(-3f, 0f, 0f);
        var playerController = playerInstance.GetComponent<PlayerController>();

        var spawnPointGO = new GameObject("SpawnPoint");
        spawnPointGO.transform.position = new Vector3(9f, -2.2f, 0f);

        var spawnerGO = new GameObject("ObstacleSpawner");
        var spawner = spawnerGO.AddComponent<ObstacleSpawner>();
        SetField(spawner, "obstaclePrefabs", new[] { obstaclePrefab });
        SetField(spawner, "spawnPoint", spawnPointGO.transform);

        AddManagers();

        var canvasGO = CreateCanvas("Canvas");
        var safeAreaGO = new GameObject("SafeArea", typeof(RectTransform));
        safeAreaGO.transform.SetParent(canvasGO.transform, false);
        var safeRT = safeAreaGO.GetComponent<RectTransform>();
        safeRT.anchorMin = Vector2.zero;
        safeRT.anchorMax = Vector2.one;
        safeRT.offsetMin = Vector2.zero;
        safeRT.offsetMax = Vector2.zero;
        safeAreaGO.AddComponent<SafeAreaFitter>();

        var hudGO = new GameObject("HUD", typeof(RectTransform));
        hudGO.transform.SetParent(safeAreaGO.transform, false);
        var hudRT = hudGO.GetComponent<RectTransform>();
        hudRT.anchorMin = Vector2.zero;
        hudRT.anchorMax = Vector2.one;
        hudRT.offsetMin = Vector2.zero;
        hudRT.offsetMax = Vector2.zero;

        var scoreText = AddText(hudGO.transform, "ScoreText", "0000", 80, new Vector2(0, 750), new Vector2(500, 120), TextColor);
        var bestScoreText = AddText(hudGO.transform, "BestScoreText", "BEST 0000", 32, new Vector2(0, 650), new Vector2(500, 60), new Color(1, 1, 1, 0.7f));
        var newBestGO = new GameObject("NewBestIndicator", typeof(RectTransform));
        newBestGO.transform.SetParent(hudGO.transform, false);
        AddText(newBestGO.transform, "Label", "NEW BEST!", 40, new Vector2(0, 550), new Vector2(500, 80), PrimaryColor);
        newBestGO.SetActive(false);

        var scoreManagerGO = new GameObject("ScoreManager");
        var scoreManager = scoreManagerGO.AddComponent<ScoreManager>();
        SetField(scoreManager, "scoreText", scoreText);
        SetField(scoreManager, "bestScoreText", bestScoreText);
        SetField(scoreManager, "newBestIndicator", newBestGO);

        var countdownRT = AddPanel(safeAreaGO.transform, "CountdownOverlay", panelSprite, new Color(0, 0, 0, 0.6f));
        var countdownText = AddText(countdownRT.transform, "CountdownText", "3", 160, Vector2.zero, new Vector2(400, 400), TextColor);
        countdownRT.gameObject.SetActive(false);

        var gameOverRT = AddPanel(safeAreaGO.transform, "GameOverPanel", panelSprite, BgColor);
        var gameOverPanel = gameOverRT.gameObject;
        AddText(gameOverPanel.transform, "GameOverLabel", "GAME OVER", 64, new Vector2(0, 500), new Vector2(700, 100), DangerColor);
        var finalScoreText = AddText(gameOverPanel.transform, "FinalScoreText", "0000", 96, new Vector2(0, 350), new Vector2(700, 150), TextColor);
        var newBestBadgeGO = new GameObject("NewBestBadge", typeof(RectTransform));
        newBestBadgeGO.transform.SetParent(gameOverPanel.transform, false);
        var newBestBadgeRT = newBestBadgeGO.GetComponent<RectTransform>();
        newBestBadgeRT.anchoredPosition = new Vector2(0, 250);
        AddText(newBestBadgeGO.transform, "Label", "NEW BEST!", 36, Vector2.zero, new Vector2(500, 60), PrimaryColor);
        newBestBadgeGO.SetActive(false);

        var restartButton = AddButton(gameOverPanel.transform, "RestartButton", "RESTART", buttonSprite, new Vector2(0, 0), new Vector2(400, 130));
        var continueButton = AddButton(gameOverPanel.transform, "ContinueButton", "CONTINUE (AD)", buttonSprite, new Vector2(0, -160), new Vector2(400, 100));
        var menuButton = AddButton(gameOverPanel.transform, "MenuButton", "MENU", buttonSprite, new Vector2(0, -300), new Vector2(400, 100));
        gameOverPanel.SetActive(false);

        var uiControllerGO = new GameObject("GameplayUIController");
        var uiController = uiControllerGO.AddComponent<GameplayUIController>();
        SetField(uiController, "player", playerController);
        SetField(uiController, "obstacleSpawner", spawner);
        SetField(uiController, "scoreManager", scoreManager);
        SetField(uiController, "playerStartPosition", new Vector3(-3f, 0f, 0f));
        SetField(uiController, "countdownOverlay", countdownRT.gameObject);
        SetField(uiController, "countdownText", countdownText);
        SetField(uiController, "gameOverPanel", gameOverPanel);
        SetField(uiController, "finalScoreText", finalScoreText);
        SetField(uiController, "newBestBadge", newBestBadgeGO);
        SetField(uiController, "restartButton", restartButton);
        SetField(uiController, "continueButton", continueButton);
        SetField(uiController, "menuButton", menuButton);
        SetField(uiController, "hud", hudGO);

        Persist(restartButton, uiController, "OnRestartClicked");
        Persist(continueButton, uiController, "OnContinueClicked");
        Persist(menuButton, uiController, "OnMenuClicked");

        EditorSceneManager.SaveScene(scene, $"{SceneDir}/Gameplay.unity");
    }
}

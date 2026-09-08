using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

/// <summary>
/// Renders a representative mid-gameplay frame at a real phone resolution by
/// staging scene state directly (no actual Play Mode needed) and capturing
/// the Main Camera + UI Canvas to a PNG. Never saves the scene - all staging
/// happens on the in-memory scene only, discarded when the process exits.
/// </summary>
public static class ScreenshotEditor
{
    [MenuItem("Build/Diagnostics/Capture Mobile Screenshot")]
    public static void Capture()
    {
        var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Gameplay.unity", OpenSceneMode.Single);

        GameObject Find(string name)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var t = FindRecursive(root.transform, name);
                if (t != null) return t.gameObject;
            }
            return null;
        }

        var camGo = Find("Main Camera");
        var cam = camGo.GetComponent<Camera>();

        // Include the UI Canvas in the camera capture by temporarily switching
        // it to Screen Space - Camera for this render only (scene is never saved).
        var canvasGo = Find("Canvas");
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 15f;

        // Stage a representative "mid-flight" gameplay moment.
        var hud = Find("HUD");
        var countdown = Find("CountdownOverlay");
        var gameOver = Find("GameOverPanel");
        if (hud != null) hud.SetActive(true);
        if (countdown != null) countdown.SetActive(false);
        if (gameOver != null) gameOver.SetActive(false);

        var scoreText = Find("ScoreText")?.GetComponent<TMPro.TextMeshProUGUI>();
        if (scoreText != null) scoreText.text = "0012";
        var bestScoreText = Find("BestScoreText")?.GetComponent<TMPro.TextMeshProUGUI>();
        if (bestScoreText != null) bestScoreText.text = "BEST 0018";

        var playerGo = Find("Player");
        if (playerGo != null)
        {
            playerGo.transform.position = new Vector3(-1.8f, 0.6f, 0f);
            playerGo.transform.rotation = Quaternion.Euler(0, 0, 12f);
        }

        var pillarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Obstacles/Pillar.prefab");
        SpawnStagedPair(pillarPrefab, 1.5f, 2.0f, -4.6f, 4.6f);
        SpawnStagedPair(pillarPrefab, 4.5f, 1.6f, -4.6f, 4.6f);

        int width = 1080, height = 1920;
        var rt = new RenderTexture(width, height, 24);
        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = rt;
        var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();

        string outPath = "/tmp/one_more_mobile_screenshot.png";
        File.WriteAllBytes(outPath, tex.EncodeToPNG());

        cam.targetTexture = null;
        RenderTexture.active = null;

        Debug.Log($"[Screenshot] Saved to {outPath}");
        // Deliberately not saving the scene - all staged changes above are discarded on exit.
    }

    private static void SpawnStagedPair(GameObject prefab, float x, float gap, float floorY, float ceilingY)
    {
        var top = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        var bottom = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        float totalHeight = ceilingY - floorY;
        float topHeight = 3.2f;
        float bottomHeight = totalHeight - gap - topHeight;
        top.transform.position = new Vector3(x, 0, 0);
        bottom.transform.position = new Vector3(x, 0, 0);
        top.GetComponent<TorchPillar>().Configure(topHeight, true, ceilingY);
        bottom.GetComponent<TorchPillar>().Configure(bottomHeight, false, floorY);
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
}

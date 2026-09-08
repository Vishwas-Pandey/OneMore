using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Wires the new PlayerController.scoreManager field (added to fix the dual
/// score-tracking bug - GameOver() now needs the authoritative score/isNewBest
/// straight from ScoreManager instead of GameManager's own stale copy).
/// </summary>
public static class WirePlayerScoreManagerEditor
{
    [MenuItem("Build/Diagnostics/Wire Player ScoreManager Reference")]
    public static void Run()
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Gameplay.unity", OpenSceneMode.Single);

        var player = Object.FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
        var scoreManager = Object.FindFirstObjectByType<ScoreManager>(FindObjectsInactive.Include);

        if (player == null) { Debug.LogError("[WirePlayerScoreManager] No PlayerController found."); return; }
        if (scoreManager == null) { Debug.LogError("[WirePlayerScoreManager] No ScoreManager found."); return; }

        var so = new SerializedObject(player);
        so.FindProperty("scoreManager").objectReferenceValue = scoreManager;
        so.ApplyModifiedPropertiesWithoutUndo();

        Debug.Log("[WirePlayerScoreManager] PlayerController.scoreManager wired.");
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}

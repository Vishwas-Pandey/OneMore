using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// RestartButton, ContinueButton and MenuButton in Gameplay.unity each carry
/// a persistent (Inspector-serialized) onClick call to their handler AND
/// GameplayUIController.Awake() adds the same handler again via
/// onClick.AddListener - every tap fired the handler twice. Harmless for
/// Restart/Menu (a duplicate scene load), but broke Continue: the second,
/// spurious call resumed gameplay immediately (behind the still-playing
/// rewarded ad) instead of waiting for the real ad-closed callback, so the
/// bird fell and died invisibly while the ad was still on screen. The
/// project's own convention (every other button, including PauseButton and
/// all of MainMenu's buttons) is code-only wiring via AddListener, so this
/// removes the redundant persistent calls to match.
/// </summary>
public static class FixDoubleWiredButtonsEditor
{
    [MenuItem("Build/Diagnostics/Fix Double-Wired Gameplay Buttons")]
    public static void Run()
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Gameplay.unity", OpenSceneMode.Single);

        int fixedCount = 0;
        foreach (var name in new[] { "RestartButton", "ContinueButton", "MenuButton" })
        {
            GameObject go = FindDeep(scene, name);
            if (go == null) { Debug.LogWarning($"[FixDoubleWiredButtons] '{name}' not found."); continue; }

            var button = go.GetComponent<Button>();
            if (button == null) { Debug.LogWarning($"[FixDoubleWiredButtons] '{name}' has no Button component."); continue; }

            int before = button.onClick.GetPersistentEventCount();
            while (button.onClick.GetPersistentEventCount() > 0)
            {
                UnityEventTools.RemovePersistentListener(button.onClick, 0);
            }
            Debug.Log($"[FixDoubleWiredButtons] {name}: removed {before} persistent listener(s), code-side AddListener remains.");
            fixedCount++;
        }

        Debug.Log($"[FixDoubleWiredButtons] Fixed {fixedCount} button(s).");
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
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
}

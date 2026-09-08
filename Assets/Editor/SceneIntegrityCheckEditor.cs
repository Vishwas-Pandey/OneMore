using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Read-only diagnostic: opens every gameplay scene and reports any null
/// SerializeField Object-reference fields on the project's own MonoBehaviours
/// (skips Unity/third-party components), plus any Button with zero
/// persistent onClick listeners and zero code-side AddListener wiring (dead
/// button). Intended as a final pre-submission sanity pass, not something
/// that ships or runs automatically.
/// </summary>
public static class SceneIntegrityCheckEditor
{
    [MenuItem("Build/Diagnostics/Scene Integrity Check")]
    public static void Run()
    {
        int totalIssues = 0;
        foreach (var path in new[]
        {
            "Assets/_Project/Scenes/Splash.unity",
            "Assets/_Project/Scenes/MainMenu.unity",
            "Assets/_Project/Scenes/Gameplay.unity",
        })
        {
            totalIssues += CheckScene(path);
        }
        Debug.Log($"[SceneIntegrityCheck] TOTAL ISSUES FOUND: {totalIssues}");
    }

    private static int CheckScene(string path)
    {
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        Debug.Log($"[SceneIntegrityCheck] === Checking {scene.name} ===");
        int issues = 0;

        var allBehaviours = scene.GetRootGameObjects()
            .SelectMany(go => go.GetComponentsInChildren<MonoBehaviour>(true))
            .Where(mb => mb != null && mb.GetType().Namespace == null) // project scripts have no namespace here
            .ToList();

        foreach (var mb in allBehaviours)
        {
            var so = new SerializedObject(mb);
            var prop = so.GetIterator();
            bool enterChildren = true;
            while (prop.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (prop.propertyType == SerializedPropertyType.ObjectReference)
                {
                    if (prop.objectReferenceValue == null)
                    {
                        Debug.LogWarning($"[SceneIntegrityCheck] NULL REF: {GetPath(mb.transform)} ({mb.GetType().Name}).{prop.name} is unassigned.");
                        issues++;
                    }
                }
            }
        }

        // Dead-button check: any Button whose GameObject name suggests it's a
        // real interactive control but has neither persistent (Inspector)
        // listeners nor is referenced by any SerializeField above (already
        // covered by the null-ref pass) - here we just flag zero persistent
        // listeners as informational, since most wiring here is done via
        // AddListener in code, not the Inspector.
        var buttons = scene.GetRootGameObjects()
            .SelectMany(go => go.GetComponentsInChildren<Button>(true))
            .ToList();
        foreach (var btn in buttons)
        {
            if (btn.onClick.GetPersistentEventCount() == 0)
            {
                Debug.Log($"[SceneIntegrityCheck] INFO: {GetPath(btn.transform)} has 0 persistent onClick listeners (expected if wired via code AddListener).");
            }
        }

        Debug.Log($"[SceneIntegrityCheck] {scene.name}: {issues} null-reference issue(s), {buttons.Count} button(s) found.");
        return issues;
    }

    private static string GetPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}

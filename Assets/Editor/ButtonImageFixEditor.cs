using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Every button Image was set to Type=Sliced with a zero-width sprite border
/// (inherited from the original bootstrap, never actually needed since no
/// border was ever defined). Confirmed on-device: this renders inconsistently
/// depending on the button's exact size - Play/Exit showed the grass/dirt
/// texture correctly, but Settings/Privacy (identical sprite, identical
/// m_Type, only the tint color differed) rendered as flat solid gray with no
/// texture at all. Switching to Simple removes the ambiguous 9-slice math
/// entirely - we don't use a border, so there's nothing Sliced mode buys us.
/// </summary>
public static class ButtonImageFixEditor
{
    private static readonly string[] MainMenuButtons =
    {
        "PlayButton", "SettingsButton", "PrivacyButton", "ExitButton",
        "SoundToggleButton", "MusicToggleButton", "HapticsToggleButton", "CloseButton"
    };

    private static readonly string[] GameplayButtons =
    {
        "RestartButton", "ContinueButton", "MenuButton", "PauseButton"
    };

    [MenuItem("Build/Diagnostics/Fix Button Image Type To Simple")]
    public static void FixAll()
    {
        FixScene("Assets/_Project/Scenes/MainMenu.unity", MainMenuButtons);
        FixScene("Assets/_Project/Scenes/Gameplay.unity", GameplayButtons);
    }

    private static void FixScene(string path, string[] names)
    {
        var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        int fixedCount = 0;

        foreach (var name in names)
        {
            GameObject go = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                var t = FindRecursive(root.transform, name);
                if (t != null) { go = t.gameObject; break; }
            }
            if (go == null) { Debug.LogWarning($"[ButtonImageFix] '{name}' not found in {scene.name}"); continue; }

            var img = go.GetComponent<Image>();
            if (img == null) { Debug.LogWarning($"[ButtonImageFix] '{name}' has no Image component"); continue; }

            bool wasNull = img.sprite == null;
            // Reassign via live asset lookup unconditionally rather than
            // trusting whatever fileID the scene currently has serialized -
            // this project has repeatedly hit stale-sprite-reference bugs
            // whenever a texture gets reimported (Multiple<->Single mode,
            // re-exporting under the same filename). A fresh lookup by path
            // is always correct regardless of any fileID drift.
            var freshSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Sprites/Generated/button_bg.png");
            img.sprite = freshSprite;
            img.type = Image.Type.Simple;
            Debug.Log($"[ButtonImageFix] {scene.name}: '{name}' sprite was {(wasNull ? "NULL" : "already valid")} -> reassigned fresh ({(freshSprite != null ? "OK" : "STILL NULL!")}), type=Simple");
            fixedCount++;
        }

        Debug.Log($"[ButtonImageFix] {scene.name}: switched {fixedCount} button Image(s) from Sliced to Simple.");
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
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

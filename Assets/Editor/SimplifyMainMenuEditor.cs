using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Removes the Settings overlay panel entirely (it only ever held
/// placeholder Sound/Music/Haptics toggles plus a Reset Ad Consent button,
/// and its washed-out background kept needing fixes) and replaces the
/// Settings button with a single direct "SOUND: ON/OFF" toggle on the Main
/// Menu itself - no submenu. Also removes the PrivacyPanel overlay: Privacy
/// now opens the real hosted policy page in the device browser instead of
/// duplicating the text in-app (see MainMenuController.OnPrivacyClicked).
/// </summary>
public static class SimplifyMainMenuEditor
{
    private const string ButtonBgPath = "Assets/_Project/Sprites/Generated/button_bg.png";

    [MenuItem("Build/Diagnostics/Simplify Main Menu (Remove Settings Panel)")]
    public static void Run()
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/MainMenu.unity", OpenSceneMode.Single);

        GameObject settingsButtonGo = FindDeep(scene, "SettingsButton");
        if (settingsButtonGo == null) { Debug.LogError("[SimplifyMainMenu] SettingsButton not found."); return; }

        // Repurpose the SettingsButton GameObject in place as the new sound
        // toggle - same slot in the button column, same visual style, one
        // less object to create/wire.
        settingsButtonGo.name = "SoundToggleButton";
        var img = settingsButtonGo.GetComponent<Image>();
        if (img != null)
        {
            img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ButtonBgPath);
            img.type = Image.Type.Simple;
            img.color = Color.white;
        }
        var label = settingsButtonGo.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null) label.text = "SOUND: ON";

        GameObject settingsPanel = FindDeep(scene, "SettingsPanel");
        if (settingsPanel != null) Object.DestroyImmediate(settingsPanel);

        GameObject privacyPanel = FindDeep(scene, "PrivacyPanel");
        if (privacyPanel != null) Object.DestroyImmediate(privacyPanel);

        GameObject controllerGo = FindDeep(scene, "MainMenuController");
        if (controllerGo != null)
        {
            var so = new SerializedObject(controllerGo.GetComponent<MainMenuController>());
            so.FindProperty("soundToggleButton").objectReferenceValue = settingsButtonGo.GetComponent<Button>();
            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[SimplifyMainMenu] Wired soundToggleButton to repurposed button.");
        }
        else
        {
            Debug.LogError("[SimplifyMainMenu] MainMenuController not found - soundToggleButton not wired.");
        }

        Debug.Log("[SimplifyMainMenu] Removed SettingsPanel + PrivacyPanel, repurposed SettingsButton as SoundToggleButton.");
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

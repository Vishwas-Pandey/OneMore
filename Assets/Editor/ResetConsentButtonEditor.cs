using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Adds a "RESET AD CONSENT" button to the Main Menu's Settings panel,
/// anchored below HapticsToggleButton using the same button_bg texture as
/// every other button. The privacy policy (Section 8) points users here to
/// withdraw/change their ad-personalization consent, so the control needs to
/// actually exist before that section stops being a placeholder.
/// </summary>
public static class ResetConsentButtonEditor
{
    private const string ButtonBgPath = "Assets/_Project/Sprites/Generated/button_bg.png";

    [MenuItem("Build/Diagnostics/Add Reset Ad Consent Button")]
    public static void Run()
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/MainMenu.unity", OpenSceneMode.Single);

        var hapticsGo = FindDeep(scene, "HapticsToggleButton");
        if (hapticsGo == null) { Debug.LogError("[ResetConsentButton] HapticsToggleButton not found - can't anchor relative to it."); return; }
        var hapticsRt = hapticsGo.GetComponent<RectTransform>();

        // Idempotent: if a previous run already created it (possibly at the
        // wrong position - the first pass overlapped CloseButton), reuse and
        // reposition it rather than skipping or duplicating.
        GameObject existing = FindDeep(scene, "ResetAdConsentButton");
        GameObject go = existing;
        if (go == null)
        {
            go = new GameObject("ResetAdConsentButton", typeof(RectTransform));
            go.transform.SetParent(hapticsGo.transform.parent, false);
        }
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = hapticsRt.anchorMin;
        rt.anchorMax = hapticsRt.anchorMax;
        rt.pivot = hapticsRt.pivot;
        rt.sizeDelta = hapticsRt.sizeDelta;
        // Existing toggle buttons step down by 140 each (Sound 200 -> Music
        // 60 -> Haptics -80); slot this new button into that same rhythm
        // rather than the CloseButton's wider -220 gap (which was sized for
        // "no more buttons after this", not for inserting one).
        rt.anchoredPosition = hapticsRt.anchoredPosition + new Vector2(0, -140);

        // CloseButton previously sat right after Haptics with a bigger gap
        // (-300, i.e. -220 below Haptics) since it was the last item. Push it
        // one more step down so it clears the button just inserted above it.
        // Scoped to a direct sibling of HapticsToggleButton (not a scene-wide
        // FindDeep) because PrivacyPanel has its own, unrelated CloseButton.
        Transform closeSibling = null;
        foreach (Transform sibling in hapticsGo.transform.parent)
        {
            if (sibling.name == "CloseButton") { closeSibling = sibling; break; }
        }
        if (closeSibling != null)
        {
            var closeRt = closeSibling.GetComponent<RectTransform>();
            closeRt.anchoredPosition = rt.anchoredPosition + new Vector2(0, -140);
        }
        else
        {
            Debug.LogWarning("[ResetConsentButton] No sibling CloseButton found under SettingsPanel to reposition.");
        }

        var img = go.GetComponent<Image>();
        if (img == null) img = go.AddComponent<Image>();
        img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ButtonBgPath);
        img.type = Image.Type.Simple;
        img.color = new Color(0.55f, 0.55f, 0.85f, 1f);

        var button = go.GetComponent<Button>();
        if (button == null) button = go.AddComponent<Button>();
        button.targetGraphic = img;

        Transform textT = go.transform.Find("Text");
        GameObject textGo = textT != null ? textT.gameObject : new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(go.transform, false);
        var textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        var tmp = textGo.GetComponent<TextMeshProUGUI>();
        if (tmp == null) tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = "RESET AD CONSENT";
        tmp.fontSize = 28;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        var controllerGo = FindDeep(scene, "MainMenuController");
        if (controllerGo != null)
        {
            var so = new SerializedObject(controllerGo.GetComponent<MainMenuController>());
            so.FindProperty("resetAdConsentButton").objectReferenceValue = button;
            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[ResetConsentButton] Added and wired to MainMenuController.");
        }
        else
        {
            Debug.LogError("[ResetConsentButton] MainMenuController GameObject not found - button created but not wired.");
        }

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

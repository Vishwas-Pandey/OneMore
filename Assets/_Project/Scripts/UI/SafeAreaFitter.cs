using UnityEngine;

/// <summary>
/// Shrinks a full-stretch RectTransform to Screen.safeArea so UI never sits
/// under a notch, the Dynamic Island, a punch-hole camera, or a gesture bar.
/// Unity's Screen.safeArea already accounts for both platforms correctly, so
/// one component covers Android and iOS with no per-platform branching.
/// Put this on the top-level panel of every screen (or on a dedicated
/// "SafeArea" child that the rest of the UI is parented under).
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    private RectTransform rectTransform;
    private Rect lastSafeArea = new Rect(0, 0, 0, 0);
    private ScreenOrientation lastOrientation = ScreenOrientation.AutoRotation;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        Apply();
    }

    private void Update()
    {
        // Safe area can change at runtime (device rotation, iPhone Dynamic
        // Island reveal, Android cutout changes) so re-check cheaply each frame.
        if (Screen.safeArea != lastSafeArea)
        {
            Apply();
        }
    }

    private void Apply()
    {
        Rect safeArea = Screen.safeArea;
        lastSafeArea = safeArea;

        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;

        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
    }
}

using UnityEngine;
using System.Collections;

public static class UIHelper
{
    public static IEnumerator AnimateScale(RectTransform rect, Vector3 from, Vector3 to, float duration, AnimationCurve curve = null)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            if (curve != null) t = curve.Evaluate(t);

            rect.localScale = Vector3.Lerp(from, to, t);
            yield return null;
        }

        rect.localScale = to;
    }

    public static IEnumerator AnimateFade(CanvasGroup group, float from, float to, float duration, AnimationCurve curve = null)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            if (curve != null) t = curve.Evaluate(t);

            group.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        group.alpha = to;
    }
}

using UnityEngine;
using System.Collections;

public static class CoroutineHelper
{
    public static IEnumerator WaitForSecondsRealtime(float seconds)
    {
        float start = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - start < seconds)
        {
            yield return null;
        }
    }

    public static IEnumerator WaitForFrames(int frames)
    {
        for (int i = 0; i < frames; i++)
        {
            yield return null;
        }
    }

    public static IEnumerator DoAfterDelay(System.Action action, float delay)
    {
        yield return new WaitForSeconds(delay);
        action?.Invoke();
    }
}

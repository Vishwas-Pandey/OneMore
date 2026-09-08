using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Google Mobile Ads SDK callbacks (ad loaded, ad closed, reward earned, etc.)
/// fire on a background thread on Android, not Unity's main thread. Calling
/// almost any Unity API from inside one of those callbacks directly (e.g.
/// SceneManager.LoadScene, GameObject.SetActive) throws
/// "X can only be called from the main thread" and silently breaks whatever
/// was supposed to happen next - confirmed on-device: an interstitial's
/// close callback tried to load the Gameplay scene and threw, leaving the
/// player stuck on the Main Menu after closing the ad.
/// Ad callbacks should route their Unity-API-touching work through
/// Enqueue() so it actually runs on the main thread, on the next Update().
/// </summary>
public class MainThreadDispatcher : MonoBehaviour
{
    private static MainThreadDispatcher instance;
    private readonly Queue<Action> pending = new Queue<Action>();

    public static void Enqueue(Action action)
    {
        if (action == null) return;
        EnsureInstance();
        lock (instance.pending)
        {
            instance.pending.Enqueue(action);
        }
    }

    private static void EnsureInstance()
    {
        if (instance != null) return;
        var go = new GameObject("MainThreadDispatcher");
        instance = go.AddComponent<MainThreadDispatcher>();
        DontDestroyOnLoad(go);
    }

    private void Update()
    {
        while (true)
        {
            Action action;
            lock (pending)
            {
                if (pending.Count == 0) break;
                action = pending.Dequeue();
            }
            action.Invoke();
        }
    }
}

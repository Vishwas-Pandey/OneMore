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

    /// <summary>
    /// Creates the singleton eagerly at startup, on the main thread, before
    /// any background-thread SDK callback gets a chance to call Enqueue()
    /// first - lazily creating it from Enqueue() would itself call
    /// `new GameObject(...)` from whatever thread called Enqueue(), which
    /// throws the exact same main-thread-only exception this class exists
    /// to avoid (confirmed on-device).
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;
        var go = new GameObject("MainThreadDispatcher");
        instance = go.AddComponent<MainThreadDispatcher>();
        DontDestroyOnLoad(go);
    }

    public static void Enqueue(Action action)
    {
        if (action == null) return;
        if (instance == null)
        {
            Debug.LogWarning("[MainThreadDispatcher] Enqueue called before Bootstrap ran; invoking immediately.");
            action.Invoke();
            return;
        }
        lock (instance.pending)
        {
            instance.pending.Enqueue(action);
        }
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

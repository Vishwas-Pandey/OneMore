using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

public class AnalyticsManager : MonoBehaviour
{
    public static AnalyticsManager Instance { get; private set; }

    [Serializable]
    public class AnalyticsConfig
    {
        public bool enableAnalytics = true;
        public bool enableFirebase = false;
    }

    [SerializeField] private AnalyticsConfig config;
    [SerializeField] private bool logEvents = true;
    [SerializeField] private float flushInterval = 5f;

    private readonly Queue<AnalyticsEvent> eventQueue = new Queue<AnalyticsEvent>();

    private class AnalyticsEvent
    {
        public string name;
        public Dictionary<string, object> parameters;
        public DateTime timestamp;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Initialize()
    {
        if (!config.enableAnalytics) return;

        if (config.enableFirebase)
        {
            InitializeFirebase();
        }

        StartCoroutine(FlushLoop());
    }

    private void InitializeFirebase()
    {
        // Firebase Analytics initializes identically on Android and iOS once
        // the platform config files are present (google-services.json for
        // Android, GoogleService-Info.plist for iOS) - see SETUP.md.
        try
        {
            // Firebase.FirebaseApp.CheckAndFixDependenciesAsync();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AnalyticsManager] Firebase init failed: {e.Message}");
        }
    }

    public void TrackEvent(string eventName, Dictionary<string, object> parameters = null)
    {
        if (!config.enableAnalytics) return;

        eventQueue.Enqueue(new AnalyticsEvent
        {
            name = eventName,
            parameters = parameters ?? new Dictionary<string, object>(),
            timestamp = DateTime.UtcNow
        });

        if (logEvents)
        {
            string paramString = parameters != null
                ? string.Join(", ", parameters.Select(kvp => $"{kvp.Key}: {kvp.Value}"))
                : "";
            Debug.Log($"[Analytics] {eventName} {paramString}");
        }
    }

    private IEnumerator FlushLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(flushInterval);
            FlushQueuedEvents();
        }
    }

    private void FlushQueuedEvents()
    {
        int count = Mathf.Min(eventQueue.Count, 20);
        for (int i = 0; i < count; i++)
        {
            SendEvent(eventQueue.Dequeue());
        }
    }

    private void SendEvent(AnalyticsEvent ev)
    {
        if (config.enableFirebase)
        {
            SendToFirebase(ev);
        }
    }

    private void SendToFirebase(AnalyticsEvent ev)
    {
        try
        {
            // Firebase.Analytics.FirebaseAnalytics.LogEvent(ev.name, ...);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AnalyticsManager] Firebase event send failed: {e.Message}");
        }
    }

    public static void TrackGameStarted()
    {
        Instance?.TrackEvent("game_started");
    }

    public static void TrackGameOver(int score, int bestScore, bool isNewBest)
    {
        Instance?.TrackEvent("game_over", new Dictionary<string, object>
        {
            ["score"] = score,
            ["best_score"] = bestScore,
            ["is_new_best"] = isNewBest,
            ["game_duration"] = Time.timeSinceLevelLoad
        });
    }

    public static void TrackMilestone(int score)
    {
        Instance?.TrackEvent("score_milestone", new Dictionary<string, object> { ["score"] = score });
    }

    public static void TrackNewHighScore(int score)
    {
        Instance?.TrackEvent("new_high_score", new Dictionary<string, object> { ["score"] = score });
    }

    public static void TrackRewardedAdOffered() => Instance?.TrackEvent("rewarded_ad_offered");
    public static void TrackRewardedAdCompleted() => Instance?.TrackEvent("rewarded_ad_completed");
    public static void TrackInterstitialShown() => Instance?.TrackEvent("interstitial_shown");
    public static void TrackRestartPressed() => Instance?.TrackEvent("restart_pressed");

    public static void TrackSettingsChanged(string setting, bool value)
    {
        Instance?.TrackEvent("settings_changed", new Dictionary<string, object>
        {
            ["setting"] = setting,
            ["value"] = value
        });
    }
}

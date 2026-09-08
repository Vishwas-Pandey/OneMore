using UnityEngine;
using System.Runtime.InteropServices;

/// <summary>
/// Cross-platform haptics.
/// Android: uses the native Vibrator service via AndroidJavaObject (no plugin needed).
/// iOS: Unity has no built-in haptics API, so this calls into a tiny native bridge
/// (Assets/Plugins/iOS/HapticsBridge.mm) that wraps UIImpactFeedbackGenerator.
/// </summary>
public class HapticManager : MonoBehaviour
{
    public static HapticManager Instance { get; private set; }

    public enum HapticStyle { Light, Medium, Heavy }

    [Header("Haptic Settings")]
    [SerializeField] private bool hapticsEnabled = true;
    [SerializeField] private long androidLightMs = 15;
    [SerializeField] private long androidMediumMs = 30;
    [SerializeField] private long androidStrongMs = 60;

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void _HapticImpact(int style);

    [DllImport("__Internal")]
    private static extern bool _HapticsAvailable();
#endif

    private AndroidJavaObject androidVibrator;
    private bool androidVibratorAvailable;

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
            return;
        }

        InitializeAndroidVibrator();
        LoadSettings();
    }

    private void InitializeAndroidVibrator()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                androidVibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                androidVibratorAvailable = androidVibrator != null && androidVibrator.Call<bool>("hasVibrator");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[HapticManager] Android vibrator init failed: {e.Message}");
        }
#endif
    }

    public void VibrateLight() => Vibrate(HapticStyle.Light);
    public void VibrateMedium() => Vibrate(HapticStyle.Medium);
    public void VibrateStrong() => Vibrate(HapticStyle.Heavy);

    public void Vibrate(HapticStyle style)
    {
        if (!hapticsEnabled) return;

#if UNITY_ANDROID && !UNITY_EDITOR
        if (!androidVibratorAvailable) return;
        long ms = style switch
        {
            HapticStyle.Light => androidLightMs,
            HapticStyle.Medium => androidMediumMs,
            _ => androidStrongMs,
        };
        try
        {
            androidVibrator.Call("vibrate", ms);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[HapticManager] Android vibrate failed: {e.Message}");
        }
#elif UNITY_IOS && !UNITY_EDITOR
        try
        {
            _HapticImpact((int)style);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[HapticManager] iOS haptic failed: {e.Message}");
        }
#endif
    }

    public void ToggleHaptics()
    {
        hapticsEnabled = !hapticsEnabled;
        SaveSystem.SaveData data = SaveSystem.LoadGameData();
        SaveSystem.SetAudioSettings(data.soundEnabled, data.musicEnabled, hapticsEnabled);
    }

    public bool IsEnabled() => hapticsEnabled;

    private void LoadSettings()
    {
        hapticsEnabled = SaveSystem.LoadGameData().hapticsEnabled;
    }
}

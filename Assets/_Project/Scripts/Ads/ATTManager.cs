using UnityEngine;
using System;
using System.Runtime.InteropServices;

/// <summary>
/// iOS-only: requests App Tracking Transparency authorization (required by
/// Apple on iOS 14.5+, App Store Review Guideline 5.1.1, before any
/// IDFA-based ad targeting/measurement). Android has no equivalent gate, so
/// on Android this simply reports "authorized" and does nothing.
/// Must be called AFTER a short delay following app launch/first frame, and
/// only once per install unless the user resets tracking permissions.
/// </summary>
public class ATTManager : MonoBehaviour
{
    public static ATTManager Instance { get; private set; }

    public enum ATTStatus { NotDetermined = 0, Restricted = 1, Denied = 2, Authorized = 3 }

    public event Action<ATTStatus> OnAuthorizationResolved;

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern int _ATTRequestAuthorization();

    [DllImport("__Internal")]
    private static extern int _ATTGetStatus();
#endif

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

    /// <summary>
    /// Shows the system tracking-permission dialog if it hasn't been shown yet.
    /// Call this once, a beat after the main menu appears (Apple guidance: not
    /// on the very first frame, so the user has context first).
    /// </summary>
    public void RequestAuthorizationIfNeeded()
    {
#if UNITY_IOS && !UNITY_EDITOR
        ATTStatus status = (ATTStatus)_ATTGetStatus();
        if (status != ATTStatus.NotDetermined)
        {
            OnAuthorizationResolved?.Invoke(status);
            return;
        }

        ATTStatus result = (ATTStatus)_ATTRequestAuthorization();
        SaveSystem.SetATTRequested(true);
        OnAuthorizationResolved?.Invoke(result);
#else
        // Android / Editor: no ATT gate, ads can request non-personalized or
        // personalized ads per AdManager's own consent handling.
        OnAuthorizationResolved?.Invoke(ATTStatus.Authorized);
#endif
    }

    public ATTStatus GetCurrentStatus()
    {
#if UNITY_IOS && !UNITY_EDITOR
        return (ATTStatus)_ATTGetStatus();
#else
        return ATTStatus.Authorized;
#endif
    }
}

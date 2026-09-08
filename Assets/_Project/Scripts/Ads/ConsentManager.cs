using UnityEngine;
using System;
using GoogleMobileAds.Ump.Api;

/// <summary>
/// Google User Messaging Platform (UMP) consent flow, required before
/// requesting ads for users in the EEA/UK (and used elsewhere too since the
/// SDK decides regional applicability on-device). Must run and resolve
/// (accepted, declined, or not-required) before AdManager.Initialize() so the
/// Mobile Ads SDK only serves ads consistent with the user's choice.
/// Consent gathering must never block or delay actual gameplay - if it fails
/// (no network, UMP error), we simply skip ad initialization for this session
/// rather than retry-looping or halting startup.
/// </summary>
public class ConsentManager : MonoBehaviour
{
    public static ConsentManager Instance { get; private set; }

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
    /// Requests/updates consent info and shows the consent form only if the
    /// platform determines one is required. Always calls onResolved exactly
    /// once, regardless of outcome - callers should check CanRequestAds()
    /// afterward rather than assuming success.
    /// </summary>
    public void GatherConsent(Action onResolved)
    {
        var request = new ConsentRequestParameters();

        ConsentInformation.Update(request, updateError =>
        {
            // UMP callbacks (like the AdMob ones) can fire off Unity's main
            // thread - confirmed on-device that a same-shaped AdMob callback
            // crashed calling SceneManager.LoadScene from here. Route through
            // MainThreadDispatcher so anything onResolved does (it goes on to
            // call AdManager.Initialize) runs safely on the main thread.
            MainThreadDispatcher.Enqueue(() =>
            {
                if (updateError != null)
                {
                    Debug.LogWarning($"[ConsentManager] ConsentInformation.Update failed: {updateError.Message}");
                    onResolved?.Invoke();
                    return;
                }

                ConsentForm.LoadAndShowConsentFormIfRequired(showError =>
                {
                    MainThreadDispatcher.Enqueue(() =>
                    {
                        if (showError != null)
                        {
                            Debug.LogWarning($"[ConsentManager] Consent form failed to load/show: {showError.Message}");
                        }
                        onResolved?.Invoke();
                    });
                });
            });
        });
    }

    public bool CanRequestAds() => ConsentInformation.CanRequestAds();

    /// <summary>
    /// Play Console / App Store surfaces a "reset consent" option under
    /// Privacy Settings so a user can change their choice later.
    /// </summary>
    public void ResetConsentState() => ConsentInformation.Reset();
}

using UnityEngine;
using System;
using GoogleMobileAds.Api;

/// <summary>
/// Cross-platform AdMob wrapper using the modern (static Load + callback)
/// Google Mobile Ads Unity SDK API. Google publishes separate official test
/// ad-unit IDs per platform, so every ID here is resolved by #if UNITY_IOS /
/// UNITY_ANDROID rather than shared - using an Android test ID on iOS (or
/// vice versa) silently fails to fill.
/// </summary>
public class AdManager : MonoBehaviour
{
    [Serializable]
    public class AdConfig
    {
        [Header("Production Ad Unit IDs - Android")]
        public string androidRewardedAdUnitId = "ca-app-pub-XXXXXXXXXXXXXXXX/YYYYYYYYYY";
        public string androidInterstitialAdUnitId = "ca-app-pub-XXXXXXXXXXXXXXXX/YYYYYYYYYY";

        [Header("Production Ad Unit IDs - iOS")]
        public string iosRewardedAdUnitId = "ca-app-pub-XXXXXXXXXXXXXXXX/ZZZZZZZZZZ";
        public string iosInterstitialAdUnitId = "ca-app-pub-XXXXXXXXXXXXXXXX/ZZZZZZZZZZ";

        public bool enableAds = true;
        public int maxContinuesPerRun = 1;
        public int interstitialFrequency = 3;
    }

    // Google's official test ad unit IDs (safe to ship during development;
    // always fill, never real inventory). Different per platform.
    private const string AndroidTestRewardedId = "ca-app-pub-3940256099942544/5224354917";
    private const string AndroidTestInterstitialId = "ca-app-pub-3940256099942544/1033173712";
    private const string IosTestRewardedId = "ca-app-pub-3940256099942544/1712485313";
    private const string IosTestInterstitialId = "ca-app-pub-3940256099942544/4411468910";

    public static AdManager Instance { get; private set; }

    [SerializeField] private AdConfig config;
    [SerializeField] private bool useTestAds = true;
    [SerializeField] private bool logAdEvents = true;

    private RewardedAd rewardedAd;
    private InterstitialAd interstitialAd;
    private Action onRewardedAdSuccess;
    private int continueCount;

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
        if (!config.enableAds) return;

        MobileAds.Initialize(initStatus =>
        {
            if (logAdEvents) Debug.Log("[AdManager] Mobile Ads SDK initialized.");
            LoadRewardedAd();
            LoadInterstitialAd();
        });
    }

    private string RewardedAdUnitId()
    {
        if (useTestAds)
        {
#if UNITY_IOS
            return IosTestRewardedId;
#else
            return AndroidTestRewardedId;
#endif
        }
#if UNITY_IOS
        return config.iosRewardedAdUnitId;
#else
        return config.androidRewardedAdUnitId;
#endif
    }

    private string InterstitialAdUnitId()
    {
        if (useTestAds)
        {
#if UNITY_IOS
            return IosTestInterstitialId;
#else
            return AndroidTestInterstitialId;
#endif
        }
#if UNITY_IOS
        return config.iosInterstitialAdUnitId;
#else
        return config.androidInterstitialAdUnitId;
#endif
    }

    public void LoadRewardedAd()
    {
        if (!config.enableAds) return;

        if (rewardedAd != null)
        {
            rewardedAd.Destroy();
            rewardedAd = null;
        }

        AdRequest request = new AdRequest();
        RewardedAd.Load(RewardedAdUnitId(), request, (ad, error) =>
        {
            if (error != null || ad == null)
            {
                if (logAdEvents) Debug.LogWarning($"[AdManager] Rewarded ad failed to load: {error}");
                return;
            }

            rewardedAd = ad;
            RegisterRewardedEventHandlers(rewardedAd);
            if (logAdEvents) Debug.Log("[AdManager] Rewarded ad loaded.");
        });
    }

    public void LoadInterstitialAd()
    {
        if (!config.enableAds) return;

        if (interstitialAd != null)
        {
            interstitialAd.Destroy();
            interstitialAd = null;
        }

        AdRequest request = new AdRequest();
        InterstitialAd.Load(InterstitialAdUnitId(), request, (ad, error) =>
        {
            if (error != null || ad == null)
            {
                if (logAdEvents) Debug.LogWarning($"[AdManager] Interstitial ad failed to load: {error}");
                return;
            }

            interstitialAd = ad;
            RegisterInterstitialEventHandlers(interstitialAd);
            if (logAdEvents) Debug.Log("[AdManager] Interstitial ad loaded.");
        });
    }

    public void ShowRewardedAd(Action onComplete)
    {
        if (!config.enableAds)
        {
            onComplete?.Invoke();
            return;
        }

        if (rewardedAd != null && rewardedAd.CanShowAd())
        {
            onRewardedAdSuccess = onComplete;
            continueCount++;
            AnalyticsManager.TrackRewardedAdOffered();

            rewardedAd.Show(reward =>
            {
                onRewardedAdSuccess?.Invoke();
                onRewardedAdSuccess = null;
                AnalyticsManager.TrackRewardedAdCompleted();
            });
        }
        else
        {
            if (logAdEvents) Debug.LogWarning("[AdManager] Rewarded ad not ready, skipping reward.");
            LoadRewardedAd();
            onComplete?.Invoke();
        }
    }

    public void ShowInterstitialAd(Action onComplete = null)
    {
        if (!config.enableAds)
        {
            onComplete?.Invoke();
            return;
        }

        if (interstitialAd != null && interstitialAd.CanShowAd())
        {
            AnalyticsManager.TrackInterstitialShown();
            _pendingInterstitialComplete = onComplete;
            interstitialAd.Show();
        }
        else
        {
            LoadInterstitialAd();
            onComplete?.Invoke();
        }
    }

    private Action _pendingInterstitialComplete;

    public bool CanContinueRun()
    {
        return continueCount < config.maxContinuesPerRun && rewardedAd != null && rewardedAd.CanShowAd();
    }

    public void ResetContinueCount() => continueCount = 0;

    public int InterstitialFrequency => config.interstitialFrequency;

    private void RegisterRewardedEventHandlers(RewardedAd ad)
    {
        ad.OnAdFullScreenContentClosed += () =>
        {
            if (logAdEvents) Debug.Log("[AdManager] Rewarded ad closed.");
            LoadRewardedAd();
        };
        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            if (logAdEvents) Debug.LogWarning($"[AdManager] Rewarded ad failed to show: {error}");
            onRewardedAdSuccess?.Invoke();
            onRewardedAdSuccess = null;
        };
    }

    private void RegisterInterstitialEventHandlers(InterstitialAd ad)
    {
        ad.OnAdFullScreenContentClosed += () =>
        {
            if (logAdEvents) Debug.Log("[AdManager] Interstitial ad closed.");
            _pendingInterstitialComplete?.Invoke();
            _pendingInterstitialComplete = null;
            LoadInterstitialAd();
        };
        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            if (logAdEvents) Debug.LogWarning($"[AdManager] Interstitial ad failed to show: {error}");
            _pendingInterstitialComplete?.Invoke();
            _pendingInterstitialComplete = null;
        };
    }

    public bool IsRewardedAdLoaded() => rewardedAd != null && rewardedAd.CanShowAd();
    public bool IsInterstitialAdLoaded() => interstitialAd != null && interstitialAd.CanShowAd();
    public bool IsAdsEnabled() => config.enableAds;

    private void OnDestroy()
    {
        rewardedAd?.Destroy();
        interstitialAd?.Destroy();
    }
}

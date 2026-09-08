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
        public string androidBannerAdUnitId = "ca-app-pub-XXXXXXXXXXXXXXXX/YYYYYYYYYY";

        [Header("Production Ad Unit IDs - iOS")]
        public string iosRewardedAdUnitId = "ca-app-pub-XXXXXXXXXXXXXXXX/ZZZZZZZZZZ";
        public string iosInterstitialAdUnitId = "ca-app-pub-XXXXXXXXXXXXXXXX/ZZZZZZZZZZ";
        public string iosBannerAdUnitId = "ca-app-pub-XXXXXXXXXXXXXXXX/ZZZZZZZZZZ";

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
    private const string AndroidTestBannerId = "ca-app-pub-3940256099942544/6300978111";
    private const string IosTestBannerId = "ca-app-pub-3940256099942544/2934735716";

    public static AdManager Instance { get; private set; }

    [SerializeField] private AdConfig config;
    [SerializeField] private bool useTestAds = true;
    [SerializeField] private bool logAdEvents = true;

    private RewardedAd rewardedAd;
    private InterstitialAd interstitialAd;
    private BannerView bannerView;
    private Action onRewardedAdSuccess;
    private int continueCount;
    private bool bannerRequestedVisible;

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
            MainThreadDispatcher.Enqueue(() =>
            {
                if (logAdEvents) Debug.Log("[AdManager] Mobile Ads SDK initialized.");
                LoadRewardedAd();
                LoadInterstitialAd();
                LoadBannerAd();
            });
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

    private string BannerAdUnitId()
    {
        if (useTestAds)
        {
#if UNITY_IOS
            return IosTestBannerId;
#else
            return AndroidTestBannerId;
#endif
        }
#if UNITY_IOS
        return config.iosBannerAdUnitId;
#else
        return config.androidBannerAdUnitId;
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
            MainThreadDispatcher.Enqueue(() =>
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
            MainThreadDispatcher.Enqueue(() =>
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
        });
    }

    /// <summary>
    /// Banners are never shown during actual gameplay (per design: they'd sit
    /// on top of the play area) - only on Main Menu, the Gameplay countdown,
    /// and the Game Over panel. ShowBannerAd()/HideBannerAd() just toggle
    /// visibility of an already-loaded banner rather than reloading it, since
    /// banners are meant to stay loaded and be shown/hidden as the player
    /// moves between those screens within a single scene load.
    /// </summary>
    public void LoadBannerAd()
    {
        if (!config.enableAds) return;

        bannerView?.Destroy();
        bannerView = new BannerView(BannerAdUnitId(), AdSize.Banner, AdPosition.Bottom);
        bannerView.OnBannerAdLoaded += () =>
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                if (logAdEvents) Debug.Log("[AdManager] Banner ad loaded.");
                if (!bannerRequestedVisible) bannerView.Hide();
            });
        };
        bannerView.OnBannerAdLoadFailed += (error) =>
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                if (logAdEvents) Debug.LogWarning($"[AdManager] Banner ad failed to load: {error}");
            });
        };

        bannerView.LoadAd(new AdRequest());
    }

    public void ShowBannerAd()
    {
        bannerRequestedVisible = true;
        if (!config.enableAds) return;
        if (bannerView == null) LoadBannerAd();
        bannerView?.Show();
    }

    public void HideBannerAd()
    {
        bannerRequestedVisible = false;
        bannerView?.Hide();
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
                MainThreadDispatcher.Enqueue(() =>
                {
                    onRewardedAdSuccess?.Invoke();
                    onRewardedAdSuccess = null;
                    AnalyticsManager.TrackRewardedAdCompleted();
                });
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
            MainThreadDispatcher.Enqueue(() =>
            {
                if (logAdEvents) Debug.Log("[AdManager] Rewarded ad closed.");
                // If the player closed the ad before finishing it, the
                // reward callback below never fired and onRewardedAdSuccess
                // is still pending - resolve it now so the caller isn't left
                // stuck on the Game Over screen with its one continue
                // already spent for nothing.
                onRewardedAdSuccess?.Invoke();
                onRewardedAdSuccess = null;
                LoadRewardedAd();
            });
        };
        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                if (logAdEvents) Debug.LogWarning($"[AdManager] Rewarded ad failed to show: {error}");
                onRewardedAdSuccess?.Invoke();
                onRewardedAdSuccess = null;
            });
        };
    }

    private void RegisterInterstitialEventHandlers(InterstitialAd ad)
    {
        ad.OnAdFullScreenContentClosed += () =>
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                if (logAdEvents) Debug.Log("[AdManager] Interstitial ad closed.");
                _pendingInterstitialComplete?.Invoke();
                _pendingInterstitialComplete = null;
                LoadInterstitialAd();
            });
        };
        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                if (logAdEvents) Debug.LogWarning($"[AdManager] Interstitial ad failed to show: {error}");
                _pendingInterstitialComplete?.Invoke();
                _pendingInterstitialComplete = null;
            });
        };
    }

    public bool IsRewardedAdLoaded() => rewardedAd != null && rewardedAd.CanShowAd();
    public bool IsInterstitialAdLoaded() => interstitialAd != null && interstitialAd.CanShowAd();
    public bool IsAdsEnabled() => config.enableAds;

    private void OnDestroy()
    {
        rewardedAd?.Destroy();
        interstitialAd?.Destroy();
        bannerView?.Destroy();
    }
}

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject privacyPanel;

    [Header("Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button privacyButton;
    [SerializeField] private Button soundToggleButton;
    [SerializeField] private Button musicToggleButton;
    [SerializeField] private Button hapticsToggleButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private Button resetAdConsentButton;

    [Header("Text")]
    [SerializeField] private TextMeshProUGUI bestScoreText;
    [SerializeField] private TextMeshProUGUI versionText;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI subtitleText;

    [Header("Visual Settings")]
    [SerializeField] private float titleBounceHeight = 10f;
    [SerializeField] private float titleBounceSpeed = 2f;
    [SerializeField] private float subtitleFadeSpeed = 1f;

    [Header("iOS App Tracking Transparency")]
    [SerializeField] private float attPromptDelay = 1.5f;

    private Vector3 titleOriginalPosition;
    private bool isTransitioning;
    private int totalGamesPlayed;

    private void Awake()
    {
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        InitializeUI();
        LoadGameData();
        SetupListeners();
    }

    private void Start()
    {
        StartCoroutine(AnimateTitle());
        StartCoroutine(AnimateSubtitle());

        if (versionText != null)
        {
            versionText.text = $"v{Application.version}";
        }

        AdManager.Instance?.LoadRewardedAd();
        AdManager.Instance?.LoadInterstitialAd();
        AdManager.Instance?.ShowBannerAd();

        // iOS: request the App Tracking Transparency prompt a beat after the
        // menu appears, per Apple's guidance to not show it on the very first
        // frame. On Android, ATTManager resolves immediately as a no-op.
        StartCoroutine(CoroutineHelper.DoAfterDelay(
            () => ATTManager.Instance?.RequestAuthorizationIfNeeded(),
            attPromptDelay));
    }

    private void InitializeUI()
    {
        mainMenuPanel.SetActive(true);
        settingsPanel.SetActive(false);
        privacyPanel.SetActive(false);

        if (titleText != null)
        {
            titleOriginalPosition = titleText.rectTransform.localPosition;
        }

        UpdateToggleStates();
    }

    private void LoadGameData()
    {
        SaveSystem.SaveData data = SaveSystem.LoadGameData();
        totalGamesPlayed = data.totalGamesPlayed;
        UpdateBestScore();
    }

    private void UpdateBestScore()
    {
        int bestScore = SaveSystem.LoadBestScore();
        if (bestScoreText != null)
        {
            bestScoreText.text = $"BEST: {bestScore:D4}";
        }
    }

    private void SetupListeners()
    {
        if (playButton != null) playButton.onClick.AddListener(OnPlayClicked);
        if (settingsButton != null) settingsButton.onClick.AddListener(OnSettingsClicked);
        if (privacyButton != null) privacyButton.onClick.AddListener(OnPrivacyClicked);
        if (soundToggleButton != null) soundToggleButton.onClick.AddListener(ToggleSound);
        if (musicToggleButton != null) musicToggleButton.onClick.AddListener(ToggleMusic);
        if (hapticsToggleButton != null) hapticsToggleButton.onClick.AddListener(ToggleHaptics);
        if (exitButton != null) exitButton.onClick.AddListener(OnExitClicked);
        if (resetAdConsentButton != null) resetAdConsentButton.onClick.AddListener(OnResetAdConsentClicked);
    }

    /// <summary>
    /// Referenced by the privacy policy (Section 8, "Your choices") as the
    /// place users can withdraw/change their ad-personalization consent.
    /// Resets UMP's stored consent state, then immediately re-runs the
    /// gathering flow so the platform-appropriate form (if any) shows again
    /// right away rather than requiring an app restart.
    /// </summary>
    public void OnResetAdConsentClicked()
    {
        PlayClickSound();
        ConsentManager.Instance?.ResetConsentState();
        ConsentManager.Instance?.GatherConsent(() =>
        {
            if (ConsentManager.Instance != null && ConsentManager.Instance.CanRequestAds())
            {
                AdManager.Instance?.Initialize();
            }
        });
    }

    public void OnExitClicked()
    {
        PlayClickSound();
        QuitApplication();
    }

    private void QuitApplication()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OnPlayClicked()
    {
        if (isTransitioning) return;
        isTransitioning = true;

        PlayClickSound();

        totalGamesPlayed++;
        SaveSystem.IncrementTotalGames();

        int frequency = AdManager.Instance != null ? AdManager.Instance.InterstitialFrequency : 3;
        if (frequency > 0 && totalGamesPlayed % frequency == 0)
        {
            AdManager.Instance?.ShowInterstitialAd(StartGame);
        }
        else
        {
            StartGame();
        }
    }

    private void StartGame()
    {
        SceneManager.LoadScene("Gameplay");
    }

    public void OnSettingsClicked()
    {
        PlayClickSound();
        settingsPanel.SetActive(!settingsPanel.activeSelf);
        UpdateToggleStates();
    }

    public void OnPrivacyClicked()
    {
        PlayClickSound();
        privacyPanel.SetActive(!privacyPanel.activeSelf);
    }

    public void ToggleSound()
    {
        AudioManager.Instance?.ToggleSound();
        UpdateToggleStates();
        PlayClickSound();
    }

    public void ToggleMusic()
    {
        AudioManager.Instance?.ToggleMusic();
        UpdateToggleStates();
        PlayClickSound();
    }

    public void ToggleHaptics()
    {
        HapticManager.Instance?.ToggleHaptics();
        UpdateToggleStates();
        PlayClickSound();
    }

    private void UpdateToggleStates()
    {
        if (soundToggleButton != null && AudioManager.Instance != null)
            UpdateToggleButton(soundToggleButton, AudioManager.Instance.IsSoundEnabled());

        if (musicToggleButton != null && AudioManager.Instance != null)
            UpdateToggleButton(musicToggleButton, AudioManager.Instance.IsMusicEnabled());

        if (hapticsToggleButton != null && HapticManager.Instance != null)
            UpdateToggleButton(hapticsToggleButton, HapticManager.Instance.IsEnabled());
    }

    private void UpdateToggleButton(Button button, bool isActive)
    {
        Image image = button.GetComponent<Image>();
        if (image != null) image.color = isActive ? Color.white : Color.gray;

        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
        {
            label.text = isActive ? "ON" : "OFF";
            label.color = isActive ? Color.white : Color.gray;
        }
    }

    private void PlayClickSound() => AudioManager.Instance?.PlayButtonSound();

    private IEnumerator AnimateTitle()
    {
        if (titleText == null) yield break;

        RectTransform rectTransform = titleText.rectTransform;
        Vector3 basePosition = titleOriginalPosition;

        while (true)
        {
            float bounce = Mathf.Sin(Time.time * titleBounceSpeed) * titleBounceHeight;
            rectTransform.localPosition = basePosition + new Vector3(0, bounce, 0);
            yield return null;
        }
    }

    private IEnumerator AnimateSubtitle()
    {
        if (subtitleText == null) yield break;

        while (true)
        {
            float alpha = Mathf.Sin(Time.time * subtitleFadeSpeed) * 0.3f + 0.7f;
            subtitleText.alpha = alpha;
            yield return null;
        }
    }

    private void Update()
    {
        // Android hardware/gesture back button. iOS has no equivalent
        // hardware key, so this branch is naturally inert there.
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (settingsPanel.activeSelf)
            {
                settingsPanel.SetActive(false);
            }
            else if (privacyPanel.activeSelf)
            {
                privacyPanel.SetActive(false);
            }
            else
            {
                QuitApplication();
            }
        }
    }
}

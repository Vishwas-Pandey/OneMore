using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject mainMenuPanel;

    [Header("Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button soundToggleButton;
    [SerializeField] private Button privacyButton;
    [SerializeField] private Button exitButton;

    [Header("Privacy")]
    [SerializeField] private string privacyPolicyUrl = "https://vishwas-pandey.github.io/OneMore/privacy-policy.html";

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

        if (titleText != null)
        {
            titleOriginalPosition = titleText.rectTransform.localPosition;
        }

        UpdateSoundButtonLabel();
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
        if (soundToggleButton != null) soundToggleButton.onClick.AddListener(ToggleSound);
        if (privacyButton != null) privacyButton.onClick.AddListener(OnPrivacyClicked);
        if (exitButton != null) exitButton.onClick.AddListener(OnExitClicked);
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

    /// <summary>
    /// Opens the hosted privacy policy in the device's own browser rather
    /// than an in-app panel - simpler than maintaining a second copy of the
    /// policy text in-game, and it's always the current, live version.
    /// </summary>
    public void OnPrivacyClicked()
    {
        PlayClickSound();
        Application.OpenURL(privacyPolicyUrl);
    }

    public void ToggleSound()
    {
        AudioManager.Instance?.ToggleSound();
        UpdateSoundButtonLabel();
        PlayClickSound();
    }

    private void UpdateSoundButtonLabel()
    {
        if (soundToggleButton == null || AudioManager.Instance == null) return;

        bool isOn = AudioManager.Instance.IsSoundEnabled();
        TextMeshProUGUI label = soundToggleButton.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
        {
            label.text = isOn ? "SOUND: ON" : "SOUND: OFF";
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
            QuitApplication();
        }
    }
}

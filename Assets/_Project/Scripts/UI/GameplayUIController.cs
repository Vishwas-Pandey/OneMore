using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

/// <summary>
/// Wires the Gameplay scene together: countdown overlay, game-over panel,
/// restart/continue buttons, and the collision -> game over -> UI flow.
/// Not part of the original single-file script list, but required for the
/// systems above (GameManager, ScoreManager, PlayerController, AdManager) to
/// actually form a playable loop instead of disconnected components.
/// </summary>
public class GameplayUIController : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private PlayerController player;
    [SerializeField] private ObstacleSpawner obstacleSpawner;
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private Vector3 playerStartPosition = new Vector3(-1.8f, 0f, 0f);

    [Header("Countdown")]
    [SerializeField] private GameObject countdownOverlay;
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private float countdownSeconds = 3f;

    [Header("Game Over Panel")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI finalScoreText;
    [SerializeField] private GameObject newBestBadge;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button menuButton;

    [Header("HUD")]
    [SerializeField] private GameObject hud;

    [Header("Pause")]
    [SerializeField] private Button pauseButton;
    [SerializeField] private TextMeshProUGUI pauseButtonText;

    private void Awake()
    {
        if (restartButton != null) restartButton.onClick.AddListener(OnRestartClicked);
        if (continueButton != null) continueButton.onClick.AddListener(OnContinueClicked);
        if (menuButton != null) menuButton.onClick.AddListener(OnMenuClicked);
        if (pauseButton != null) pauseButton.onClick.AddListener(OnPauseClicked);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver += HandleGameOver;
        }
    }

    private void Start()
    {
        gameOverPanel.SetActive(false);
        hud.SetActive(false);
        AdManager.Instance?.ResetContinueCount();

        StartCoroutine(RunCountdown());
    }

    private IEnumerator RunCountdown()
    {
        GameManager.Instance?.StartGame();
        player.ResetPlayer(playerStartPosition);

        AdManager.Instance?.ShowBannerAd();

        countdownOverlay.SetActive(true);
        for (int i = Mathf.CeilToInt(countdownSeconds); i > 0; i--)
        {
            if (countdownText != null) countdownText.text = i.ToString();
            yield return new WaitForSeconds(1f);
        }
        countdownOverlay.SetActive(false);

        // Banners are only for the countdown/menu/game-over screens, never
        // over the play area itself.
        AdManager.Instance?.HideBannerAd();

        hud.SetActive(true);
        GameManager.Instance?.BeginPlaying();
        player.BeginFlight();
        obstacleSpawner.StartSpawning();
    }

    private void HandleGameOver()
    {
        hud.SetActive(false);
        gameOverPanel.SetActive(true);
        obstacleSpawner.StopSpawning();
        AdManager.Instance?.ShowBannerAd();

        if (finalScoreText != null)
        {
            finalScoreText.text = scoreManager.GetCurrentScore().ToString("D4");
        }

        if (newBestBadge != null)
        {
            newBestBadge.SetActive(GameManager.Instance != null && GameManager.Instance.IsNewBest());
        }

        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(AdManager.Instance != null && AdManager.Instance.CanContinueRun());
        }
    }

    public void OnRestartClicked()
    {
        AnalyticsManager.TrackRestartPressed();
        SceneManager.LoadScene("Gameplay");
    }

    public void OnContinueClicked()
    {
        AdManager.Instance?.ShowRewardedAd(() =>
        {
            gameOverPanel.SetActive(false);
            hud.SetActive(true);
            AdManager.Instance?.HideBannerAd();
            GameManager.Instance?.BeginPlaying();
            player.ResetPlayer(playerStartPosition);
            player.BeginFlight();
            obstacleSpawner.ResetSpawner();
        });
    }

    public void OnMenuClicked()
    {
        Time.timeScale = 1f; // defensive: don't leave the whole app frozen if this is reached while paused
        SceneManager.LoadScene("MainMenu");
    }

    public void OnPauseClicked()
    {
        GameManager.Instance?.TogglePause();

        bool paused = GameManager.Instance != null && GameManager.Instance.IsPaused();
        if (pauseButtonText != null) pauseButtonText.text = paused ? "PLAY" : "PAUSE";
        AudioManager.Instance?.PlayButtonSound();
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver -= HandleGameOver;
        }
    }
}

using UnityEngine;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Menu, Countdown, Playing, Paused, GameOver }

    [Header("Game Settings")]
    [SerializeField] private GameState currentState;
    [SerializeField] private bool isNewBest;

    public event Action<GameState> OnGameStateChanged;
    public event Action OnGameOver;
    public event Action OnGameRestart;

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

    private void Start()
    {
        // Deferred to Start() (rather than Awake()) so every other
        // singleton's own Awake() has already run first - Unity guarantees
        // all Awake calls finish before any Start call, which plain Awake
        // ordering does not. AnalyticsManager/ConsentManager/AdManager are
        // only guaranteed non-null here.
        InitializeSystems();
    }

    private void InitializeSystems()
    {
        // Order matters: analytics should exist before anything reports events.
        if (AnalyticsManager.Instance != null) AnalyticsManager.Instance.Initialize();

        // UMP consent must be resolved before the Mobile Ads SDK initializes
        // (Google policy for EEA/UK users). Gathering never blocks gameplay -
        // ad init is simply skipped for this session if consent isn't granted
        // or the flow fails outright (e.g. no network at launch).
        if (ConsentManager.Instance != null)
        {
            ConsentManager.Instance.GatherConsent(() =>
            {
                if (ConsentManager.Instance.CanRequestAds())
                {
                    AdManager.Instance?.Initialize();
                }
            });
        }
        else
        {
            AdManager.Instance?.Initialize();
        }
    }

    public void StartGame()
    {
        isNewBest = false;
        Time.timeScale = 1f;
        SetGameState(GameState.Countdown);
        AnalyticsManager.TrackGameStarted();
    }

    /// <summary>
    /// Time.timeScale = 0 freezes every Time.deltaTime-driven system at once
    /// (bird physics, pipe movement/spawning) without needing each script to
    /// separately check a "paused" flag - simplest correct way to pause here.
    /// </summary>
    public void TogglePause()
    {
        if (currentState == GameState.Playing)
        {
            Time.timeScale = 0f;
            SetGameState(GameState.Paused);
        }
        else if (currentState == GameState.Paused)
        {
            Time.timeScale = 1f;
            SetGameState(GameState.Playing);
        }
    }

    public bool IsPaused() => currentState == GameState.Paused;

    public void BeginPlaying()
    {
        SetGameState(GameState.Playing);
    }

    /// <summary>
    /// finalScore/isNewBest are supplied by the caller (from ScoreManager,
    /// the single source of truth for score - it already applies the combo
    /// multiplier and already saved any new best to disk in real time).
    /// GameManager used to track its own separate score/bestScore in
    /// parallel and re-save here, which could overwrite a correct save with
    /// a stale, lower value since its own cache was never refreshed after
    /// the initial load - removed rather than kept in sync, since nothing
    /// but this method needed it.
    /// </summary>
    public void GameOver(int finalScore, bool isNewBest)
    {
        SetGameState(GameState.GameOver);
        this.isNewBest = isNewBest;

        if (isNewBest)
        {
            AnalyticsManager.TrackNewHighScore(finalScore);
        }

        AnalyticsManager.TrackGameOver(finalScore, SaveSystem.LoadBestScore(), isNewBest);
        OnGameOver?.Invoke();
    }

    public void RestartGame()
    {
        OnGameRestart?.Invoke();
        StartGame();
    }

    private void SetGameState(GameState newState)
    {
        currentState = newState;
        OnGameStateChanged?.Invoke(newState);
    }

    public GameState GetGameState() => currentState;
    public bool IsNewBest() => isNewBest;

    /// <summary>
    /// Fires when the app loses/regains foreground (notification shade,
    /// quick settings, a system dialog, app switch) - without this, gravity
    /// and obstacle movement kept running while the player couldn't see or
    /// touch the screen, producing an unfair death the instant focus
    /// returned. Only auto-pauses; resuming is still the player's own
    /// explicit action via the pause button.
    /// </summary>
    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && currentState == GameState.Playing)
        {
            TogglePause();
        }
    }
}

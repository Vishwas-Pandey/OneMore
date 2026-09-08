using UnityEngine;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Menu, Countdown, Playing, Paused, GameOver }

    [Header("Game Settings")]
    [SerializeField] private GameState currentState;
    [SerializeField] private int score;
    [SerializeField] private int bestScore;
    [SerializeField] private float gameSpeed = 1f;
    [SerializeField] private float maxGameSpeed = 2.5f;
    [SerializeField] private float speedRampPerPoint = 0.02f;
    [SerializeField] private bool isNewBest;

    public event Action<int> OnScoreUpdated;
    public event Action<GameState> OnGameStateChanged;
    public event Action<int> OnBestScoreUpdated;
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
            return;
        }

        Initialize();
    }

    private void Initialize()
    {
        bestScore = SaveSystem.LoadBestScore();
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
        score = 0;
        gameSpeed = 1f;
        isNewBest = false;
        SetGameState(GameState.Countdown);
        AnalyticsManager.TrackGameStarted();
    }

    public void BeginPlaying()
    {
        SetGameState(GameState.Playing);
    }

    public void GameOver()
    {
        SetGameState(GameState.GameOver);

        if (score > bestScore)
        {
            bestScore = score;
            SaveSystem.SaveBestScore(bestScore);
            isNewBest = true;
            OnBestScoreUpdated?.Invoke(bestScore);
            AnalyticsManager.TrackNewHighScore(score);
        }

        AnalyticsManager.TrackGameOver(score, bestScore, isNewBest);
        OnGameOver?.Invoke();
    }

    public void RestartGame()
    {
        OnGameRestart?.Invoke();
        StartGame();
    }

    public void AddScore(int points = 1)
    {
        score += points;
        gameSpeed = Mathf.Min(maxGameSpeed, 1f + score * speedRampPerPoint);
        OnScoreUpdated?.Invoke(score);

        if (score % 10 == 0)
        {
            AnalyticsManager.TrackMilestone(score);
        }
    }

    private void SetGameState(GameState newState)
    {
        currentState = newState;
        OnGameStateChanged?.Invoke(newState);
    }

    public GameState GetGameState() => currentState;
    public float GetGameSpeed() => gameSpeed;
    public int GetScore() => score;
    public int GetBestScore() => bestScore;
    public bool IsNewBest() => isNewBest;
}

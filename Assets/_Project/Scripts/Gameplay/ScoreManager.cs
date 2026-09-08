using UnityEngine;
using System.Collections;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    [Header("Score Settings")]
    [SerializeField] private int currentScore;
    [SerializeField] private int bestScore;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI bestScoreText;
    [SerializeField] private GameObject newBestIndicator;
    [SerializeField] private Animator scoreAnimator;

    [Header("Visual Effects")]
    [SerializeField] private ParticleSystem scoreParticles;

    public event System.Action<int> OnScoreChanged;
    public event System.Action<int> OnBestScoreChanged;
    public event System.Action OnNewBest;

    private Coroutine newBestCoroutine;
    private int comboCount;
    private float comboResetTimer;
    private const float ComboWindow = 0.5f;

    private void Start()
    {
        bestScore = SaveSystem.LoadBestScore();
        UpdateScoreUI();
    }

    private void Update()
    {
        if (comboCount > 0)
        {
            comboResetTimer -= Time.deltaTime;
            if (comboResetTimer <= 0f) comboCount = 0;
        }
    }

    public void AddScore(int points = 1)
    {
        int multiplier = 1 + Mathf.FloorToInt(comboCount / 3f);
        currentScore += points * multiplier;

        comboCount++;
        comboResetTimer = ComboWindow;

        CheckMilestones();

        OnScoreChanged?.Invoke(currentScore);
        UpdateScoreUI();

        if (scoreParticles != null) scoreParticles.Play();
        AudioManager.Instance?.PlayScoreSound();

        if (currentScore % 10 == 0)
        {
            HapticManager.Instance?.VibrateMedium();
        }
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = currentScore.ToString("D4");
            if (scoreAnimator != null) scoreAnimator.SetTrigger("Update");
        }

        if (currentScore > bestScore)
        {
            bestScore = currentScore;
            OnBestScoreChanged?.Invoke(bestScore);
            SaveSystem.SaveBestScore(bestScore);

            if (newBestIndicator != null && newBestCoroutine == null)
            {
                newBestCoroutine = StartCoroutine(ShowNewBest());
            }

            OnNewBest?.Invoke();
        }

        if (bestScoreText != null)
        {
            bestScoreText.text = $"BEST {bestScore:D4}";
        }
    }

    private IEnumerator ShowNewBest()
    {
        newBestIndicator.SetActive(true);

        Animator indicatorAnimator = newBestIndicator.GetComponent<Animator>();
        if (indicatorAnimator != null) indicatorAnimator.SetTrigger("Show");

        AudioManager.Instance?.PlayNewBestSound();
        HapticManager.Instance?.VibrateStrong();

        yield return new WaitForSeconds(2f);

        newBestIndicator.SetActive(false);
        newBestCoroutine = null;
    }

    private static readonly int[] Milestones = { 10, 25, 50, 100, 250, 500, 1000 };

    private void CheckMilestones()
    {
        foreach (int milestone in Milestones)
        {
            if (currentScore == milestone)
            {
                AnalyticsManager.TrackMilestone(currentScore);
                break;
            }
        }
    }

    public void ResetScore()
    {
        currentScore = 0;
        comboCount = 0;
        comboResetTimer = 0f;
        UpdateScoreUI();

        if (newBestIndicator != null)
        {
            newBestIndicator.SetActive(false);
            if (newBestCoroutine != null)
            {
                StopCoroutine(newBestCoroutine);
                newBestCoroutine = null;
            }
        }
    }

    public int GetCurrentScore() => currentScore;
    public int GetBestScore() => bestScore;
    public int GetComboCount() => comboCount;
}

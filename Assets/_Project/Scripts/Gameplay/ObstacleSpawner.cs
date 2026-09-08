using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spawns torch-pillar pairs (top-hanging + bottom-standing) with a gap the
/// bird must fly through - the flappy-bird pipe mechanic. Gap size and pipe
/// speed both scale with score, matching the reference tuning: every 5
/// points the gap shrinks and the speed increases, until the floor/ceiling
/// values are hit. Scoring fires when a pair's X passes the player's X,
/// exactly like the reference's boundingRect check (simplified to an X
/// comparison since both pillars in a pair always share the same X/speed).
/// </summary>
public class ObstacleSpawner : MonoBehaviour
{
    private class ActivePair
    {
        public TorchPillar top;
        public TorchPillar bottom;
        public bool scored;
    }

    [Header("Prefab & Spawn Point")]
    [SerializeField] private GameObject pillarPrefab;
    [SerializeField] private float spawnX = 6f;
    [SerializeField] private Transform playerTransform;

    [Header("Screen Bounds (world units)")]
    [SerializeField] private float floorY = -4.6f;
    [SerializeField] private float ceilingY = 4.6f;

    [Header("Gap Settings")]
    [SerializeField] private float initialGap = 2.6f;
    [SerializeField] private float minGap = 1.4f;
    [SerializeField] private float gapShrinkPerStep = 0.15f;

    [Header("Speed Settings")]
    [SerializeField] private float initialSpeed = 2f;
    [SerializeField] private float maxSpeed = 5f;
    [SerializeField] private float speedIncreasePerStep = 0.33f;

    [Header("Spawn Timing")]
    // Interval is derived from current speed (spacing / speed), not fixed -
    // a fixed interval would mean pipes get FARTHER apart in world space as
    // speed ramps up (distance = speed * interval), which undercuts the
    // difficulty curve instead of reinforcing it. Keeping the spatial spacing
    // constant means faster pipes also arrive more often, not less.
    [SerializeField] private float pipeSpacing = 4f;
    [SerializeField] private float minSpawnInterval = 0.75f;

    [SerializeField] private ScoreManager scoreManager;

    private ObjectPool pillarPool;
    private float spawnTimer;
    private bool isSpawning;
    private readonly List<ActivePair> activePairs = new List<ActivePair>();

    private void Awake()
    {
        pillarPool = GetComponent<ObjectPool>();
        if (pillarPool == null)
        {
            pillarPool = gameObject.AddComponent<ObjectPool>();
        }
        pillarPool.Initialize(new[] { pillarPrefab }, 12);
    }

    private void Update()
    {
        if (!isSpawning) return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            SpawnPair();
            var (speed, _) = GetCurrentTuning();
            spawnTimer = Mathf.Max(minSpawnInterval, pipeSpacing / speed);
        }

        CheckScoring();
        activePairs.RemoveAll(p => p.bottom == null || !p.bottom.gameObject.activeInHierarchy);
    }

    public void StartSpawning()
    {
        isSpawning = true;
        spawnTimer = 1f;
        activePairs.Clear();
    }

    public void StopSpawning() => isSpawning = false;

    private (float speed, float gap) GetCurrentTuning()
    {
        int score = scoreManager != null ? scoreManager.GetCurrentScore() : 0;
        int steps = score / 5;
        float speed = Mathf.Min(maxSpeed, initialSpeed + steps * speedIncreasePerStep);
        float gap = Mathf.Max(minGap, initialGap - steps * gapShrinkPerStep);
        return (speed, gap);
    }

    private void SpawnPair()
    {
        var (speed, gap) = GetCurrentTuning();

        float totalHeight = ceilingY - floorY;
        float minSegment = 0.6f;
        float maxTopHeight = totalHeight - gap - minSegment;
        if (maxTopHeight < minSegment) maxTopHeight = minSegment;

        float topHeight = Random.Range(minSegment, maxTopHeight);
        float bottomHeight = totalHeight - gap - topHeight;

        var topGo = pillarPool.GetObject(pillarPrefab);
        var bottomGo = pillarPool.GetObject(pillarPrefab);
        if (topGo == null || bottomGo == null) return;

        topGo.transform.position = new Vector3(spawnX, 0f, 0f);
        bottomGo.transform.position = new Vector3(spawnX, 0f, 0f);
        topGo.SetActive(true);
        bottomGo.SetActive(true);

        var topPillar = topGo.GetComponent<TorchPillar>();
        var bottomPillar = bottomGo.GetComponent<TorchPillar>();
        topPillar.SetOwnerPool(pillarPool);
        bottomPillar.SetOwnerPool(pillarPool);
        topPillar.SetSpeed(speed);
        bottomPillar.SetSpeed(speed);
        topPillar.Configure(topHeight, true, ceilingY);
        bottomPillar.Configure(bottomHeight, false, floorY);

        activePairs.Add(new ActivePair { top = topPillar, bottom = bottomPillar, scored = false });
    }

    private void CheckScoring()
    {
        if (playerTransform == null || scoreManager == null) return;
        float playerX = playerTransform.position.x;

        foreach (var pair in activePairs)
        {
            if (pair.scored || pair.bottom == null) continue;
            if (pair.bottom.GetX() < playerX)
            {
                pair.scored = true;
                scoreManager.AddScore(1);
            }
        }
    }

    public void ClearAllPillars()
    {
        // Pooled objects deactivate themselves on despawn; nothing still
        // active needs to be force-cleared beyond forgetting our tracking list.
        foreach (var pair in activePairs)
        {
            if (pair.top != null) pillarPool.ReturnObject(pair.top.gameObject);
            if (pair.bottom != null) pillarPool.ReturnObject(pair.bottom.gameObject);
        }
        activePairs.Clear();
    }

    public void ResetSpawner()
    {
        ClearAllPillars();
        StopSpawning();
        StartSpawning();
    }
}

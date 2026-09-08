using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ObstacleSpawner : MonoBehaviour
{
    [System.Serializable]
    public class ObstaclePattern
    {
        public string name;
        public GameObject[] obstacles;
        public float heightVariation;
        public bool isMoving;
        public float moveSpeed;
        public int difficultyLevel;
    }

    [Header("Obstacle Settings")]
    [SerializeField] private GameObject[] obstaclePrefabs;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float minSpawnDistance = 1.5f;
    [SerializeField] private float maxSpawnDistance = 3.5f;
    [SerializeField] private int maxObstacles = 10;

    [Header("Pattern Settings")]
    [SerializeField] private ObstaclePattern[] patterns;
    [SerializeField] private float patternChangeInterval = 5f;

    [Header("Difficulty Settings")]
    [SerializeField] private float baseDifficulty = 1f;
    [SerializeField] private float difficultyMultiplier = 0.1f;

    private readonly List<GameObject> activeObstacles = new List<GameObject>();
    private ObjectPool obstaclePool;
    private float spawnTimer;
    private float patternTimer;
    private int currentPatternIndex;
    private readonly List<int> validPatternIndices = new List<int>();
    private float currentDifficulty = 1f;
    private bool isSpawning;

    private void Awake()
    {
        obstaclePool = GetComponent<ObjectPool>();
        if (obstaclePool == null)
        {
            obstaclePool = gameObject.AddComponent<ObjectPool>();
        }
        obstaclePool.Initialize(obstaclePrefabs, 20);

        UpdateValidPatterns();
    }

    private void Start()
    {
        StartSpawning();
    }

    private void Update()
    {
        if (!isSpawning) return;

        currentDifficulty = baseDifficulty + (GameManager.Instance != null ? GameManager.Instance.GetGameSpeed() : 1f) * difficultyMultiplier;

        spawnTimer -= Time.deltaTime * currentDifficulty;
        if (spawnTimer <= 0f)
        {
            SpawnObstacle();
            spawnTimer = Random.Range(minSpawnDistance, maxSpawnDistance) / currentDifficulty;
        }

        patternTimer += Time.deltaTime;
        if (patternTimer >= patternChangeInterval)
        {
            ChangePattern();
            patternTimer = 0f;
        }

        activeObstacles.RemoveAll(item => item == null || !item.activeInHierarchy);

        if (activeObstacles.Count > maxObstacles)
        {
            RemoveOldestObstacle();
        }
    }

    private void StartSpawning()
    {
        isSpawning = true;
        spawnTimer = 1f;
        currentPatternIndex = 0;
        patternTimer = 0f;

        for (int i = 0; i < 3; i++)
        {
            SpawnObstacle();
        }
    }

    public void StopSpawning() => isSpawning = false;

    private void SpawnObstacle()
    {
        GameObject prefab = GetObstacleFromCurrentPattern();
        if (prefab == null) return;

        GameObject obstacle = obstaclePool.GetObject(prefab);
        if (obstacle == null) return;

        Vector3 position = spawnPoint.position;
        position.x += Random.Range(-0.5f, 0.5f);
        position.y += Random.Range(-0.3f, 0.3f);

        if (patterns.Length > 0 && currentPatternIndex < patterns.Length)
        {
            ObstaclePattern pattern = patterns[currentPatternIndex];
            position.y += Random.Range(-pattern.heightVariation, pattern.heightVariation);

            Obstacle obstacleComponent = obstacle.GetComponent<Obstacle>();
            if (obstacleComponent != null)
            {
                obstacleComponent.SetDifficulty(currentDifficulty);
                obstacleComponent.SetMovement(pattern.isMoving, pattern.moveSpeed);
            }
        }

        obstacle.transform.position = position;
        obstacle.SetActive(true);

        activeObstacles.Add(obstacle);
    }

    private GameObject GetObstacleFromCurrentPattern()
    {
        if (patterns == null || patterns.Length == 0)
        {
            return obstaclePrefabs.Length > 0 ? obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)] : null;
        }

        ObstaclePattern pattern = patterns[currentPatternIndex];
        if (pattern.obstacles == null || pattern.obstacles.Length == 0) return null;

        return pattern.obstacles[Random.Range(0, pattern.obstacles.Length)];
    }

    private void ChangePattern()
    {
        UpdateValidPatterns();
        if (validPatternIndices.Count == 0) return;

        currentPatternIndex = validPatternIndices[Random.Range(0, validPatternIndices.Count)];
    }

    private void UpdateValidPatterns()
    {
        validPatternIndices.Clear();
        if (patterns == null) return;

        int currentDifficultyLevel = Mathf.FloorToInt(currentDifficulty);

        for (int i = 0; i < patterns.Length; i++)
        {
            if (patterns[i].difficultyLevel <= currentDifficultyLevel)
            {
                validPatternIndices.Add(i);
            }
        }
    }

    private void RemoveOldestObstacle()
    {
        if (activeObstacles.Count == 0) return;

        GameObject oldest = activeObstacles[0];
        activeObstacles.RemoveAt(0);

        if (oldest != null)
        {
            obstaclePool.ReturnObject(oldest);
        }
    }

    public void ClearAllObstacles()
    {
        foreach (GameObject obstacle in activeObstacles)
        {
            if (obstacle != null) obstaclePool.ReturnObject(obstacle);
        }
        activeObstacles.Clear();
    }

    public void ResetSpawner()
    {
        ClearAllObstacles();
        StopSpawning();
        StartSpawning();
    }
}

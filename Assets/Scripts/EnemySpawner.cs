using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EnemyEntry
{
    public string enemyName;
    public GameObject enemyPrefab;
    public int powerValue;
}

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private List<EnemyEntry> enemyPool;
    [SerializeField] private Transform spawnCenter;
    [SerializeField] private float spawnRadius = 20f; // Increased for FPS scale
    [SerializeField] private float timeBetweenWaves = 5f;

    [Header("Player Avoidance")]
    [SerializeField] private Transform playerTarget;
    [SerializeField] private float minDistanceFromPlayer = 15f;
    [SerializeField] private int maxSpawnRetries = 10; // Prevents infinite loops

    [Header("Difficulty Scaling")]
    [SerializeField] private int initialTargetPower = 10;
    [SerializeField] private int powerIncreasePerWave = 5;
    
    private int _currentWaveTargetPower;
    private float _waveTimer;
    private List<EnemyEntry> _affordableEnemies = new List<EnemyEntry>();

    void Start()
    {
        _currentWaveTargetPower = initialTargetPower;
        if (spawnCenter == null) spawnCenter = this.transform;
        
        // Auto-find player by tag if not assigned
        if (playerTarget == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTarget = player.transform;
        }
    }

    void Update()
    {
        _waveTimer -= Time.deltaTime;

        if (_waveTimer <= 0f)
        {
            SpawnWave();
            _waveTimer = timeBetweenWaves;
            _currentWaveTargetPower += powerIncreasePerWave;
        }
    }

    private void SpawnWave()
    {
        int remainingBudget = _currentWaveTargetPower;

        while (remainingBudget > 0)
        {
            _affordableEnemies.Clear();
            for (int i = 0; i < enemyPool.Count; i++)
            {
                if (enemyPool[i].powerValue <= remainingBudget)
                {
                    _affordableEnemies.Add(enemyPool[i]);
                }
            }

            if (_affordableEnemies.Count == 0) break;

            EnemyEntry selected = _affordableEnemies[Random.Range(0, _affordableEnemies.Count)];
            
            // Logic moved to find valid position before spawning
            Vector3 validPos = GetValidSpawnPosition();
            SpawnEnemy(selected.enemyPrefab, validPos);
            
            remainingBudget -= selected.powerValue;
        }
    }

    private Vector3 GetValidSpawnPosition()
    {
        Vector3 spawnPos = Vector3.zero;
        bool foundValidPoint = false;

        for (int i = 0; i < maxSpawnRetries; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2);
            float x = Mathf.Cos(angle) * spawnRadius;
            float z = Mathf.Sin(angle) * spawnRadius;
            spawnPos = spawnCenter.position + new Vector3(x, 0, z);

            // If no player is assigned, any point is valid
            if (playerTarget == null) return spawnPos;

            // Check if the point is far enough from the player
            if (Vector3.Distance(spawnPos, playerTarget.position) >= minDistanceFromPlayer)
            {
                foundValidPoint = true;
                break;
            }
        }

        // Fallback: If we couldn't find a spot after retries, 
        // pick the point on the circle furthest from the player
        if (!foundValidPoint && playerTarget != null)
        {
            Vector3 dirFromPlayer = (spawnCenter.position - playerTarget.position).normalized;
            spawnPos = spawnCenter.position + (dirFromPlayer * spawnRadius);
        }

        return spawnPos;
    }

    private void SpawnEnemy(GameObject prefab, Vector3 position)
    {
        // Face the player for immediate engagement, or face center
        Quaternion rotation = Quaternion.LookRotation(spawnCenter.position - position);
        Instantiate(prefab, position, rotation);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 center = spawnCenter ? spawnCenter.position : transform.position;
        Gizmos.DrawWireSphere(center, spawnRadius);

        if (playerTarget != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(playerTarget.position, minDistanceFromPlayer);
        }
    }
}
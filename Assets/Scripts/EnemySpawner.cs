using System.Collections;
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
    [SerializeField] private float spawnRadius = 10f;
    [SerializeField] private float timeBetweenWaves = 5f;

    [Header("Difficulty Scaling")]
    [SerializeField] private int initialTargetPower = 10;
    [SerializeField] private int powerIncreasePerWave = 5;
    
    private int _currentWaveTargetPower;
    private float _waveTimer;
    
    // Pre-allocated list to avoid allocations in Update/Spawn logic
    private List<EnemyEntry> _affordableEnemies = new List<EnemyEntry>();

    void Start()
    {
        _currentWaveTargetPower = initialTargetPower;
        // Initialize timer so the first wave spawns after the first interval
        // _waveTimer = timeBetweenWaves; 

        if (spawnCenter == null) spawnCenter = this.transform;
    }

    void Update()
    {
        // Countdown timer
        _waveTimer -= Time.deltaTime;

        if (_waveTimer <= 0f)
        {
            SpawnWave();
            
            // Reset timer and scale difficulty
            _waveTimer = timeBetweenWaves;
            _currentWaveTargetPower += powerIncreasePerWave;
        }
    }

    private void SpawnWave()
    {
        int remainingBudget = _currentWaveTargetPower;

        // Keep selecting enemies until the budget is depleted or no affordable options remain
        while (remainingBudget > 0)
        {
            _affordableEnemies.Clear();

            // Check pool for affordable units
            for (int i = 0; i < enemyPool.Count; i++)
            {
                if (enemyPool[i].powerValue <= remainingBudget)
                {
                    _affordableEnemies.Add(enemyPool[i]);
                }
            }

            // If we can't afford anything else, stop spawning for this wave
            if (_affordableEnemies.Count == 0) break;

            // Pick a random affordable enemy
            EnemyEntry selected = _affordableEnemies[Random.Range(0, _affordableEnemies.Count)];
            
            SpawnEnemy(selected.enemyPrefab);
            remainingBudget -= selected.powerValue;
        }
    }

    private void SpawnEnemy(GameObject prefab)
    {
        // Random angle in radians
        float angle = Random.Range(0f, Mathf.PI * 2);
        
        // Calculate offsets
        float x = Mathf.Cos(angle) * spawnRadius;
        float z = Mathf.Sin(angle) * spawnRadius;

        Vector3 spawnPos = spawnCenter.position + new Vector3(x, 0, z);

        // Instantiate and rotate to face the center
        Instantiate(prefab, spawnPos, Quaternion.LookRotation(spawnCenter.position - spawnPos));
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 center = spawnCenter ? spawnCenter.position : transform.position;
        Gizmos.DrawWireSphere(center, spawnRadius);
    }
}
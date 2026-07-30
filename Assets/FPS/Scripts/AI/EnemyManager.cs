using System.Collections;
using System.Collections.Generic;
using Unity.FPS.Game;
using UnityEngine;
using UnityEngine.AI;

namespace Unity.FPS.AI
{
    public class EnemyManager : MonoBehaviour
    {
        [Header("Enemy Spawning")]
        [Tooltip("If enabled, more enemies will spawn when the scene starts.")]
        public bool SpawnEnemiesOnStart = true;

        [Tooltip("How many enemies to spawn initially.")]
        public int InitialSpawnCount = 50;

        [Tooltip("How far from the manager position enemies can spawn.")]
        public float SpawnRadius = 100f;

        [Tooltip("Delay between each spawned enemy.")]
        public float SpawnDelayBetweenEnemies = 0.08f;

        [Tooltip("Continuous spawn interval - spawn enemies every X seconds after initial spawn")]
        public float ContinuousSpawnInterval = 4.5f;

        [Tooltip("Number of enemies to spawn each interval")]
        public int EnemiesPerSpawnWave = 2;

        [Tooltip("Specific enemy prefabs to spawn. If empty, the manager will pick from any enemy prefab found in the project.")]
        public List<GameObject> EnemyPrefabs = new List<GameObject>();

        [Tooltip("Specific spawn points to use. If empty, the manager uses a radius around its own position.")]
        public List<Transform> SpawnPoints = new List<Transform>();

        [Tooltip("Use NavMesh sampling to place enemies on walkable surfaces.")]
        public bool UseNavMeshSpawning = true;

        public List<EnemyController> Enemies { get; private set; }
        public int NumberOfEnemiesTotal { get; private set; }
        public int NumberOfEnemiesRemaining => Enemies.Count;

        void Awake()
        {
            Enemies = new List<EnemyController>();
        }

        void Start()
        {
            if (SpawnEnemiesOnStart)
            {
                StartCoroutine(SpawnInitialEnemiesRoutine());
                StartCoroutine(ContinuousSpawnRoutine());
            }
        }

        IEnumerator SpawnInitialEnemiesRoutine()
        {
            for (int i = 0; i < InitialSpawnCount; i++)
            {
                SpawnEnemy();
                if (i < InitialSpawnCount - 1)
                {
                    yield return new WaitForSeconds(SpawnDelayBetweenEnemies);
                }
            }
        }

        IEnumerator ContinuousSpawnRoutine()
        {
            yield return new WaitForSeconds(InitialSpawnCount * SpawnDelayBetweenEnemies);
            
            while (true)
            {
                yield return new WaitForSeconds(ContinuousSpawnInterval);
                
                for (int i = 0; i < EnemiesPerSpawnWave; i++)
                {
                    SpawnEnemy();
                }
            }
        }

        public GameObject SpawnEnemy()
        {
            GameObject prefabToSpawn = GetEnemyPrefabToSpawn();
            if (!prefabToSpawn)
            {
                return null;
            }

            Vector3 spawnPosition = GetSpawnPosition();
            return Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
        }

        GameObject GetEnemyPrefabToSpawn()
        {
            if (EnemyPrefabs != null && EnemyPrefabs.Count > 0)
            {
                List<GameObject> preferredPrefabs = new List<GameObject>();
                foreach (var prefab in EnemyPrefabs)
                {
                    if (prefab == null)
                        continue;

                    string name = prefab.name.ToLowerInvariant();
                    if ((name.Contains("drone") || name.Contains("robot") || name.Contains("bot") || name.Contains("mobile"))
                        && !name.Contains("turret") && !name.Contains("tower"))
                    {
                        preferredPrefabs.Add(prefab);
                    }
                }

                if (preferredPrefabs.Count > 0)
                {
                    return preferredPrefabs[Random.Range(0, preferredPrefabs.Count)];
                }

                return EnemyPrefabs[Random.Range(0, EnemyPrefabs.Count)];
            }

            List<GameObject> candidates = new List<GameObject>();
            foreach (var enemyPrefab in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (enemyPrefab == null)
                    continue;

                string name = enemyPrefab.name.ToLowerInvariant();
                if (enemyPrefab.GetComponent<EnemyController>() != null && name.Contains("enemy")
                    && !name.Contains("turret") && !name.Contains("tower"))
                {
                    if (name.Contains("drone") || name.Contains("robot") || name.Contains("bot") || name.Contains("mobile"))
                    {
                        return enemyPrefab;
                    }

                    candidates.Add(enemyPrefab);
                }
            }

            if (candidates.Count > 0)
            {
                return candidates[Random.Range(0, candidates.Count)];
            }

            return null;
        }

        Vector3 GetSpawnPosition()
        {
            if (SpawnPoints != null && SpawnPoints.Count > 0)
            {
                Transform spawnPoint = SpawnPoints[Random.Range(0, SpawnPoints.Count)];
                if (spawnPoint)
                {
                    return spawnPoint.position + Random.insideUnitSphere * 1.5f;
                }
            }

            Vector3 proposedPosition = transform.position + Random.insideUnitSphere * SpawnRadius;
            if (UseNavMeshSpawning && NavMesh.SamplePosition(proposedPosition, out NavMeshHit hit, SpawnRadius, NavMesh.AllAreas))
            {
                return hit.position;
            }

            return proposedPosition;
        }

        public void RegisterEnemy(EnemyController enemy)
        {
            Enemies.Add(enemy);

            NumberOfEnemiesTotal++;
        }

        public void UnregisterEnemy(EnemyController enemyKilled)
        {
            int enemiesRemainingNotification = NumberOfEnemiesRemaining - 1;

            EnemyKillEvent evt = Events.EnemyKillEvent;
            evt.Enemy = enemyKilled.gameObject;
            evt.RemainingEnemyCount = enemiesRemainingNotification;
            EventManager.Broadcast(evt);

            // removes the enemy from the list, so that we can keep track of how many are left on the map
            Enemies.Remove(enemyKilled);
        }
    }
}
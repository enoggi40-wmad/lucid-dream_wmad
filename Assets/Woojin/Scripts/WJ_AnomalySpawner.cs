using System.Collections.Generic;
using UnityEngine;

public class WJ_AnomalySpawner : MonoBehaviour
{
    [Header("현재 씬 난이도")]
    [SerializeField] private WJ_AnomalyDifficulty sceneDifficulty = WJ_AnomalyDifficulty.Medium;

    [Header("생성할 이상현상 개수")]
    [SerializeField] private int spawnCount = 3;

    [Header("같은 이상현상 Prefab 중복 생성 허용")]
    [SerializeField] private bool allowDuplicatePrefab = false;

    [Header("난이도별 이상현상 Prefab 목록")]
    [SerializeField] private WJ_AnomalySpawnData[] anomalyPool;

    [Header("Anomaly Manager")]
    [SerializeField] private WJ_AnomalyManager anomalyManager;

    private void Start()
    {
        if (anomalyManager == null)
        {
            anomalyManager = WJ_AnomalyManager.Instance;
        }

        if (anomalyManager == null)
        {
            Debug.LogWarning("[WJ_AnomalySpawner] WJ_AnomalyManager를 찾을 수 없습니다.");
            return;
        }

        GameObject[] spawnedAnomalies = SpawnAnomalies();

        anomalyManager.RegisterAnomalies(spawnedAnomalies);
    }

    private GameObject[] SpawnAnomalies()
    {
        List<WJ_AnomalySpawnData> filteredPool = GetFilteredAnomalyPool();
        List<GameObject> spawnedAnomalies = new List<GameObject>();

        if (spawnCount <= 0)
        {
            Debug.Log("[WJ_AnomalySpawner] Spawn Count가 0입니다. 이상현상을 생성하지 않습니다.");
            return spawnedAnomalies.ToArray();
        }

        if (filteredPool.Count == 0)
        {
            Debug.LogWarning("[WJ_AnomalySpawner] 현재 난이도에 맞는 이상현상이 없습니다: " + sceneDifficulty);
            return spawnedAnomalies.ToArray();
        }

        int actualSpawnCount = spawnCount;

        if (!allowDuplicatePrefab)
        {
            actualSpawnCount = Mathf.Min(actualSpawnCount, filteredPool.Count);
        }

        for (int i = 0; i < actualSpawnCount; i++)
        {
            WJ_AnomalySpawnData selectedData = GetRandomAnomalyData(filteredPool);

            if (selectedData == null)
            {
                continue;
            }

            Transform selectedSpawnPoint = GetRandomSpawnPoint(selectedData);

            if (selectedSpawnPoint == null)
            {
                Debug.LogWarning("[WJ_AnomalySpawner] "
                    + selectedData.anomalyPrefab.name
                    + "에 사용 가능한 SpawnPoint가 없습니다.");
                continue;
            }

            GameObject spawned = Instantiate(
                selectedData.anomalyPrefab,
                selectedSpawnPoint.position,
                selectedSpawnPoint.rotation
            );

            spawned.name = selectedData.anomalyPrefab.name;

            spawnedAnomalies.Add(spawned);

            if (!allowDuplicatePrefab)
            {
                filteredPool.Remove(selectedData);
            }

            Debug.Log("[WJ_AnomalySpawner] 이상현상 생성: "
                + spawned.name
                + " / 난이도: "
                + selectedData.difficulty
                + " / 위치: "
                + selectedSpawnPoint.name);
        }

        return spawnedAnomalies.ToArray();
    }

    private List<WJ_AnomalySpawnData> GetFilteredAnomalyPool()
    {
        List<WJ_AnomalySpawnData> filteredPool = new List<WJ_AnomalySpawnData>();

        if (anomalyPool == null)
        {
            return filteredPool;
        }

        foreach (WJ_AnomalySpawnData data in anomalyPool)
        {
            if (data == null) continue;
            if (data.anomalyPrefab == null) continue;

            if (data.difficulty == sceneDifficulty)
            {
                filteredPool.Add(data);
            }
        }

        return filteredPool;
    }

    private WJ_AnomalySpawnData GetRandomAnomalyData(List<WJ_AnomalySpawnData> pool)
    {
        if (pool == null || pool.Count == 0)
        {
            return null;
        }

        int randomIndex = Random.Range(0, pool.Count);
        return pool[randomIndex];
    }

    private Transform GetRandomSpawnPoint(WJ_AnomalySpawnData data)
    {
        if (data == null) return null;
        if (data.spawnPoints == null || data.spawnPoints.Length == 0) return null;

        List<Transform> validSpawnPoints = new List<Transform>();

        foreach (Transform point in data.spawnPoints)
        {
            if (point != null)
            {
                validSpawnPoints.Add(point);
            }
        }

        if (validSpawnPoints.Count == 0)
        {
            return null;
        }

        int randomIndex = Random.Range(0, validSpawnPoints.Count);
        return validSpawnPoints[randomIndex];
    }
}
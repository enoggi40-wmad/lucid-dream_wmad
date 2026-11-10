using UnityEngine;

public enum WJ_AnomalyDifficulty
{
    Easy,
    Medium,
    Hard
}

[System.Serializable]
public class WJ_AnomalySpawnData
{
    [Header("이상현상 Prefab")]
    public GameObject anomalyPrefab;

    [Header("이상현상 난이도")]
    public WJ_AnomalyDifficulty difficulty;

    [Header("이 이상현상이 생성될 수 있는 위치")]
    public Transform[] spawnPoints;
}
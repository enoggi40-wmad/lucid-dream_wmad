using UnityEngine;
using LucidDream.Core;

public class WJ_DifficultySceneSelector : MonoBehaviour
{
    [Header("씬 이름")]
    [SerializeField] private string level1SceneName;      // Easy Anomaly
    [SerializeField] private string level2SceneName;      // Medium Anomaly
    [SerializeField] private string level3SceneName;      // Hard Anomaly
    [SerializeField] private string noAnomalySceneName;   // No Anomaly

    public string GetSceneNameByCurrentDissociationLevel()
    {
        if (DissociationLevelManager.Instance == null)
        {
            Debug.LogWarning("[WJ_DifficultySceneSelector] DissociationLevelManager.Instance가 없습니다. Level2로 이동합니다.");
            return level2SceneName;
        }

        int currentIndex = DissociationLevelManager.Instance.Current.level - 1;

        Debug.Log("[WJ_DifficultySceneSelector] 현재 해리성 Index: " + currentIndex);

        switch (currentIndex)
        {
            case 0:
            case 1:
                Debug.Log("[WJ_DifficultySceneSelector] Index 0/1 → Level3 또는 NoAnomaly 랜덤");
                return GetRandomScene(level3SceneName, noAnomalySceneName);

            case 2:
                Debug.Log("[WJ_DifficultySceneSelector] Index 2 → Level2");
                return level2SceneName;

            case 3:
            case 4:
                Debug.Log("[WJ_DifficultySceneSelector] Index 3/4 → Level1 또는 NoAnomaly 랜덤");
                return GetRandomScene(level1SceneName, noAnomalySceneName);

            default:
                Debug.LogWarning("[WJ_DifficultySceneSelector] 알 수 없는 해리성 Index: " + currentIndex);
                return level2SceneName;
        }
    }

    private string GetRandomScene(string sceneA, string sceneB)
    {
        if (string.IsNullOrEmpty(sceneA))
        {
            Debug.LogWarning("[WJ_DifficultySceneSelector] SceneA가 비어 있음. SceneB로 이동: " + sceneB);
            return sceneB;
        }

        if (string.IsNullOrEmpty(sceneB))
        {
            Debug.LogWarning("[WJ_DifficultySceneSelector] SceneB가 비어 있음. SceneA로 이동: " + sceneA);
            return sceneA;
        }

        int randomValue = Random.Range(0, 2);

        string selectedScene = randomValue == 0 ? sceneA : sceneB;

        Debug.Log("[WJ_DifficultySceneSelector] 랜덤 결과: " + randomValue + " / 선택된 씬: " + selectedScene);

        return selectedScene;
    }
}
using UnityEngine;
using UnityEngine.SceneManagement;

public class WJ_LucidPortal : MonoBehaviour
{
    [Header("씬 선택기")]
    [SerializeField] private WJ_DifficultySceneSelector difficultySceneSelector;

    [Header("Anomaly Manager")]
    [SerializeField] private WJ_AnomalyManager anomalyManager;

    private bool isUsed = false;

    public void EnterPortal(Collider other)
    {
        if (isUsed) return;
        if (other == null) return;

        if (!other.CompareTag("Player"))
        {
            return;
        }

        isUsed = true;

        // 1. 이미 해리성 Index가 0인 상태에서 포탈을 탔으면 Clear
        if (WJ_GameResultManager.Instance != null)
        {
            bool loadedClear = WJ_GameResultManager.Instance.TryLoadClearScene();

            if (loadedClear)
            {
                return;
            }
        }
        else
        {
            Debug.LogWarning("[WJ_LucidPortal] WJ_GameResultManager.Instance가 없습니다.");
        }

        // 2. Clear가 아니라면 그때 해리성 감소
        if (anomalyManager == null)
        {
            anomalyManager = WJ_AnomalyManager.Instance;
        }

        if (anomalyManager != null)
        {
            anomalyManager.ApplyDreamClearSuccess();
        }
        else
        {
            Debug.LogWarning("[WJ_LucidPortal] WJ_AnomalyManager를 찾을 수 없습니다.");
        }

        // 3. 감소 후 다음 씬 이동
        LoadNextScene();
    }

    private void LoadNextScene()
    {
        if (difficultySceneSelector == null)
        {
            Debug.LogWarning("[WJ_LucidPortal] difficultySceneSelector가 연결되지 않았습니다.");
            return;
        }

        string nextSceneName = difficultySceneSelector.GetSceneNameByCurrentDissociationLevel();

        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogWarning("[WJ_LucidPortal] 다음 씬 이름이 비어 있습니다.");
            return;
        }

        Debug.Log("[WJ_LucidPortal] 다음 씬 이동: " + nextSceneName);
        SceneManager.LoadScene(nextSceneName);
    }
}
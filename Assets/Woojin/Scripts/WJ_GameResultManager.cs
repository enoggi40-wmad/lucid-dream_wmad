using UnityEngine;
using UnityEngine.SceneManagement;
using LucidDream.Core;

public class WJ_GameResultManager : MonoBehaviour
{
    public static WJ_GameResultManager Instance { get; private set; }

    [Header("결과 씬 이름")]
    [SerializeField] private string clearSceneName = "WJ_Clear";
    [SerializeField] private string failSceneName = "WJ_Fail";

    [Header("해리성 조건")]
    [Tooltip("해리성 Index가 이 값 이하이면 Clear 조건으로 판단합니다.")]
    [SerializeField] private int clearDissociationIndex = 0;

    [Tooltip("해리성 Index가 이 값을 넘으려고 하거나, 이 값 이상에서 실패하면 Fail 조건으로 판단합니다.")]
    [SerializeField] private int failDissociationIndex = 4;

    private bool isResultLoading = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public int GetCurrentDissociationIndex()
    {
        if (DissociationLevelManager.Instance == null)
        {
            Debug.LogWarning("[WJ_GameResultManager] DissociationLevelManager.Instance가 없습니다.");
            return -1;
        }

        // DissociationLevelManager의 Current.level은 1~5라서
        // 게임에서 쓰는 해리성 Index는 0~4로 변환해서 사용
        return DissociationLevelManager.Instance.Current.level - 1;
    }

    public bool TryLoadClearScene()
    {
        if (isResultLoading) return true;

        int currentIndex = GetCurrentDissociationIndex();

        if (currentIndex < 0)
        {
            return false;
        }

        if (currentIndex <= clearDissociationIndex)
        {
            LoadClearScene();
            return true;
        }

        return false;
    }

    public bool TryLoadFailSceneBeforeDissociationIncrease()
    {
        if (isResultLoading) return true;

        int currentIndex = GetCurrentDissociationIndex();

        if (currentIndex < 0)
        {
            return false;
        }

        if (currentIndex >= failDissociationIndex)
        {
            LoadFailScene("해리성 수치 한계 상태에서 추가 실패");
            return true;
        }

        return false;
    }

    public bool TryLoadFailSceneByDissociation()
    {
        if (isResultLoading) return true;

        int currentIndex = GetCurrentDissociationIndex();

        if (currentIndex < 0)
        {
            return false;
        }

        if (currentIndex >= failDissociationIndex)
        {
            LoadFailScene("해리성 수치 한계 도달");
            return true;
        }

        return false;
    }

    public void LoadFailScene(string reason)
    {
        if (isResultLoading) return;

        isResultLoading = true;

        Debug.Log("[WJ_GameResultManager] Fail 씬 이동. 이유: " + reason);

        if (string.IsNullOrEmpty(failSceneName))
        {
            Debug.LogWarning("[WJ_GameResultManager] Fail Scene Name이 비어 있습니다.");
            return;
        }

        SceneManager.LoadScene(failSceneName);
    }

    private void LoadClearScene()
    {
        if (isResultLoading) return;

        isResultLoading = true;

        Debug.Log("[WJ_GameResultManager] Clear 씬 이동");

        if (string.IsNullOrEmpty(clearSceneName))
        {
            Debug.LogWarning("[WJ_GameResultManager] Clear Scene Name이 비어 있습니다.");
            return;
        }

        SceneManager.LoadScene(clearSceneName);
    }
}
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using LucidDream.Core;

public class WJ_AnomalyManager : MonoBehaviour
{
    public static WJ_AnomalyManager Instance { get; private set; }

    [Header("현재 씬의 이상현상 목록")]
    [SerializeField] private GameObject[] sceneAnomalies;

    [Header("클리어 후 등장할 포탈")]
    [SerializeField] private GameObject portalObject;

    [Header("꿈 판별 UI")]
    [SerializeField] private GameObject dreamQuestionPanel;

    [Header("남은 이상현상 UI")]
    [SerializeField] private TMP_Text remainingAnomalyText;

    [Header("질문 등장 시간")]
    [SerializeField] private float questionDelay = 30f;

    [Header("실제 꿈 여부")]
    [SerializeField] private bool isActualDream = true;

    [Header("기본 다음 씬 이름")]
    [SerializeField] private string nextSceneName;

    [Header("해리성 단계 기반 씬 선택기")]
    [SerializeField] private WJ_DifficultySceneSelector difficultySceneSelector;

    [Header("꿈 판별 전 Sanity 감소 정지")]
    [SerializeField] private bool pauseSanityUntilDreamJudged = true;

    private int remainingAnomalyCount;

    private bool isDreamJudged = false;
    private bool isDreamConfirmed = false;

    private bool isSanityPausedByThisScene = false;

    public bool IsDreamJudged => isDreamJudged;
    public bool IsDreamConfirmed => isDreamConfirmed;

    public bool IsSanityDecreasePaused
    {
        get
        {
            return pauseSanityUntilDreamJudged && !isDreamJudged;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        remainingAnomalyCount = sceneAnomalies != null ? sceneAnomalies.Length : 0;

        if (portalObject != null)
        {
            portalObject.SetActive(false);
        }

        if (dreamQuestionPanel != null)
        {
            dreamQuestionPanel.SetActive(false);
        }

        if (remainingAnomalyText != null)
        {
            remainingAnomalyText.gameObject.SetActive(false);
        }

        UpdateRemainingAnomalyUI();

        if (pauseSanityUntilDreamJudged && SanityManager.Instance != null)
        {
            SanityManager.Instance.PausePassiveDecay();
            isSanityPausedByThisScene = true;

            Debug.Log("[WJ_AnomalyManager] Is it Dream 패널 전까지 Sanity 자연 감소 정지");
        }

        Invoke(nameof(ShowDreamQuestion), questionDelay);

        Debug.Log("[WJ_AnomalyManager] 현재 씬 이상현상 개수: " + remainingAnomalyCount);
    }

    public void RegisterAnomalies(GameObject[] anomalies)
    {
        sceneAnomalies = anomalies;
        remainingAnomalyCount = sceneAnomalies != null ? sceneAnomalies.Length : 0;

        UpdateRemainingAnomalyUI();

        Debug.Log("[WJ_AnomalyManager] 이상현상 등록 완료: " + remainingAnomalyCount);
    }

    private void ShowDreamQuestion()
    {
        ResumeSanityIfPausedByThisScene();

        if (dreamQuestionPanel != null)
        {
            dreamQuestionPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("[WJ_AnomalyManager] dreamQuestionPanel이 연결되지 않았습니다.");
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("[WJ_AnomalyManager] 꿈입니까? UI 등장");
    }

    private void ResumeSanityIfPausedByThisScene()
    {
        if (!isSanityPausedByThisScene) return;

        if (SanityManager.Instance != null)
        {
            SanityManager.Instance.ResumePassiveDecay();
            Debug.Log("[WJ_AnomalyManager] Is it Dream 패널 등장. Sanity 자연 감소 재개");
        }

        isSanityPausedByThisScene = false;
    }

    public void SelectYes()
    {
        isDreamJudged = true;

        HideDreamQuestionPanel();

        if (isActualDream)
        {
            // 실제 꿈인데 Yes 선택: 정답
            isDreamConfirmed = true;

            if (remainingAnomalyText != null)
            {
                remainingAnomalyText.gameObject.SetActive(true);
                UpdateRemainingAnomalyUI();
            }

            Debug.Log("[WJ_AnomalyManager] Yes 선택: 정답. 정화 가능 상태.");
        }
        else
        {
            // 실제 꿈이 아닌데 Yes 선택: 오답
            isDreamConfirmed = false;

            bool loadedFail = ApplyDreamJudgementFail();

            Debug.Log("[WJ_AnomalyManager] Yes 선택: 오답. 실제 꿈이 아니므로 해리성 단계 상승.");

            if (loadedFail)
            {
                return;
            }

            LoadSceneByDissociationLevel();
        }
    }

    public void SelectNo()
    {
        isDreamJudged = true;
        isDreamConfirmed = false;

        HideDreamQuestionPanel();

        if (!isActualDream)
        {
            // 실제 꿈이 아닌데 No 선택: 정답
            Debug.Log("[WJ_AnomalyManager] No 선택: 정답. 실제 꿈이 아니라고 판단.");

            // 1. 이미 해리성 Index가 0인 상태에서 NoAnomaly 정답이면 Clear
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
                Debug.LogWarning("[WJ_AnomalyManager] WJ_GameResultManager.Instance가 없습니다.");
            }

            // 2. Clear가 아니라면 해리성 감소
            ApplyDreamClearSuccess();

            Debug.Log("[WJ_AnomalyManager] NoAnomaly 정답 처리. 해리성 단계 감소.");

            // 3. 감소 후 다음 씬 이동
            LoadSceneByDissociationLevel();
        }
        else
        {
            // 실제 꿈인데 No 선택: 오답
            bool loadedFail = ApplyDreamJudgementFail();

            Debug.Log("[WJ_AnomalyManager] No 선택: 오답. 실제 꿈인데 아니라고 판단하여 해리성 단계 상승.");

            if (loadedFail)
            {
                return;
            }

            LoadSceneByDissociationLevel();
        }
    }

    private void HideDreamQuestionPanel()
    {
        if (dreamQuestionPanel != null)
        {
            dreamQuestionPanel.SetActive(false);
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private bool ApplyDreamJudgementFail()
    {
        // 1. 이미 해리성 Index가 한계치라면, 더 올리지 않고 Fail
        if (WJ_GameResultManager.Instance != null)
        {
            bool loadedFail = WJ_GameResultManager.Instance.TryLoadFailSceneBeforeDissociationIncrease();

            if (loadedFail)
            {
                return true;
            }
        }
        else
        {
            Debug.LogWarning("[WJ_AnomalyManager] WJ_GameResultManager.Instance가 없습니다.");
        }

        // 2. 아직 한계치가 아니라면 해리성 증가
        if (DissociationLevelManager.Instance != null)
        {
            DissociationLevelManager.Instance.ForceLevelUp("DreamJudgementFailed");
            return false;
        }
        else
        {
            Debug.LogWarning("[WJ_AnomalyManager] DissociationLevelManager.Instance가 없습니다.");
            return false;
        }
    }

    public void ApplyDreamClearSuccess()
    {
        if (DissociationLevelManager.Instance != null)
        {
            DissociationLevelManager.Instance.TemporaryReduce(1);
            Debug.Log("[WJ_AnomalyManager] 꿈 클리어 성공. 해리성 단계 감소.");
        }
        else
        {
            Debug.LogWarning("[WJ_AnomalyManager] DissociationLevelManager.Instance가 없습니다.");
        }
    }

    private void LoadSceneByDissociationLevel()
    {
        string targetSceneName = nextSceneName;

        if (difficultySceneSelector != null)
        {
            targetSceneName = difficultySceneSelector.GetSceneNameByCurrentDissociationLevel();
        }
        else
        {
            Debug.LogWarning("[WJ_AnomalyManager] difficultySceneSelector가 연결되지 않았습니다. nextSceneName을 사용합니다.");
        }

        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogWarning("[WJ_AnomalyManager] 이동할 씬 이름이 비어 있습니다.");
            return;
        }

        Debug.Log("[WJ_AnomalyManager] 해리성 단계에 따른 씬 이동: " + targetSceneName);
        SceneManager.LoadScene(targetSceneName);
    }

    public void RemoveAnomaly(GameObject anomaly)
    {
        if (!isDreamJudged)
        {
            Debug.Log("[WJ_AnomalyManager] 아직 꿈인지 판별하지 않아 정화할 수 없습니다.");
            return;
        }

        if (!isDreamConfirmed)
        {
            Debug.Log("[WJ_AnomalyManager] 꿈으로 판별되지 않았기 때문에 정화할 수 없습니다.");
            return;
        }

        if (anomaly == null) return;

        anomaly.SetActive(false);
        remainingAnomalyCount--;

        if (remainingAnomalyCount < 0)
        {
            remainingAnomalyCount = 0;
        }

        UpdateRemainingAnomalyUI();

        Debug.Log("[WJ_AnomalyManager] 이상현상 제거: "
            + anomaly.name + " / 남은 개수: " + remainingAnomalyCount);

        if (remainingAnomalyCount <= 0)
        {
            OpenPortal();
        }
    }

    private void UpdateRemainingAnomalyUI()
    {
        if (remainingAnomalyText != null)
        {
            remainingAnomalyText.text = "Remaining Anomaly: " + remainingAnomalyCount;
        }
    }

    private void OpenPortal()
    {
        Debug.Log("[WJ_AnomalyManager] 모든 이상현상 제거 완료. 포탈 등장!");

        if (portalObject != null)
        {
            portalObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("[WJ_AnomalyManager] portalObject가 연결되지 않았습니다.");
        }
    }

    private void OnDestroy()
    {
        ResumeSanityIfPausedByThisScene();
    }
}
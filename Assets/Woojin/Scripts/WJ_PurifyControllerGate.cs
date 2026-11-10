using UnityEngine;

public class WJ_PurifyControllerGate : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PurifyController purifyController;

    [Header("Input")]
    [SerializeField] private int purifyMouseButton = 1; // Right Mouse Button

    private bool waitingForMouseRelease = true;

    private void Awake()
    {
        if (purifyController == null)
        {
            purifyController = GetComponent<PurifyController>();
        }
    }

    private void Start()
    {
        if (WJ_AnomalyManager.Instance != null && purifyController != null)
        {
            purifyController.enabled = false;
            waitingForMouseRelease = true;

            Debug.Log("[WJ_PurifyControllerGate] 꿈 판별 전 정화 컨트롤러 비활성화");
        }
    }

    private void Update()
    {
        if (purifyController == null) return;

        if (WJ_AnomalyManager.Instance == null)
        {
            purifyController.enabled = true;
            return;
        }

        bool canPurify =
            WJ_AnomalyManager.Instance.IsDreamJudged &&
            WJ_AnomalyManager.Instance.IsDreamConfirmed;

        if (!canPurify)
        {
            purifyController.enabled = false;
            waitingForMouseRelease = true;
            return;
        }

        if (waitingForMouseRelease)
        {
            purifyController.enabled = false;

            if (!Input.GetMouseButton(purifyMouseButton))
            {
                waitingForMouseRelease = false;
                purifyController.enabled = true;

                Debug.Log("[WJ_PurifyControllerGate] 우클릭 입력 초기화 완료. 정화 가능.");
            }

            return;
        }

        purifyController.enabled = true;
    }
}
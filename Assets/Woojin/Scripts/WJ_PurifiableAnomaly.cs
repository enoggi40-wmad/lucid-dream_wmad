using UnityEngine;
using UnityEngine.InputSystem;

public class WJ_PurifiableAnomaly : PurifiableObject
{
    [Header("정화 설정")]
    [SerializeField] private Transform player;
    [SerializeField] private float purifyDistance = 3f;

    [Header("자동 Player 찾기")]
    [SerializeField] private bool autoFindPlayer = true;

    private bool isPurifying = false;

    private void Start()
    {
        TryFindPlayer();
    }

    private void Update()
    {
        if (IsPurified) return;

        if (player == null)
        {
            TryFindPlayer();

            if (player == null)
            {
                return;
            }
        }

        if (Keyboard.current == null) return;

        if (WJ_AnomalyManager.Instance == null) return;

        if (!WJ_AnomalyManager.Instance.IsDreamJudged)
        {
            return;
        }

        if (!WJ_AnomalyManager.Instance.IsDreamConfirmed)
        {
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);
        bool isNear = distance <= purifyDistance;
        bool isHoldingPurifyKey = Keyboard.current.pKey.isPressed;

        if (isNear && isHoldingPurifyKey)
        {
            if (!isPurifying)
            {
                isPurifying = true;
                OnPurifyStart();
            }

            AddPurifyTime(Time.deltaTime);

            if (Progress01 >= 1f)
            {
                CompletePurification();
                isPurifying = false;
            }
        }
        else
        {
            if (isPurifying)
            {
                isPurifying = false;
                OnPurifyStop();
            }
        }
    }

    private void TryFindPlayer()
    {
        if (!autoFindPlayer) return;
        if (player != null) return;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;
            Debug.Log("[WJ_PurifiableAnomaly] Player 자동 연결 완료: " + playerObject.name);
        }
    }

    protected override void OnPurifyComplete()
    {
        base.OnPurifyComplete();

        Debug.Log("[WJ_PurifiableAnomaly] 이상현상 정화 완료: " + gameObject.name);

        if (WJ_AnomalyManager.Instance != null)
        {
            WJ_AnomalyManager.Instance.RemoveAnomaly(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
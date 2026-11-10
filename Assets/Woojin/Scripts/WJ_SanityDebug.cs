using UnityEngine;
using UnityEngine.InputSystem;
using LucidDream.Core;

public class WJ_SanityDebug : MonoBehaviour
{
    [Header("디버그 로그 설정")]
    [SerializeField] private bool showSanityLog = true; // true면 1초마다 Sanity 출력
    [SerializeField] private float logInterval = 1f;    // 몇 초마다 출력할지

    private float logTimer = 0f;

    private void Start()
    {
        Debug.Log("[WJ_SanityDebug] Start 실행됨");

        if (SanityManager.Instance == null)
        {
            Debug.Log("[WJ_SanityDebug] Start 시점: SanityManager.Instance == null");
        }
        else
        {
            Debug.Log("[WJ_SanityDebug] Start 시점: SanityManager 찾음 = "
                + SanityManager.Instance.gameObject.name);
            Debug.Log("[WJ_SanityDebug] 시작 Sanity = "
                + SanityManager.Instance.CurrentSanity);
        }
    }

    private void Update()
    {
        PrintSanityEverySecond();
        HandleManualDamageInput();
    }

    /// <summary>
    /// 일정 시간마다 현재 Sanity 상태를 콘솔에 출력
    /// </summary>
    private void PrintSanityEverySecond()
    {
        if (!showSanityLog) return;
        if (SanityManager.Instance == null) return;

        logTimer += Time.deltaTime;

        if (logTimer >= logInterval)
        {
            logTimer = 0f;

            Debug.Log("[WJ_SanityDebug] 현재 Sanity = "
                + SanityManager.Instance.CurrentSanity
                + " / "
                + SanityManager.Instance.MaxSanity
                + " | 비율 = "
                + SanityManager.Instance.SanityRatio);
        }
    }

    /// <summary>
    /// K키를 누르면 테스트용으로 Sanity를 감소시킴
    /// </summary>
    private void HandleManualDamageInput()
    {
        if (Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame)
        {
            Debug.Log("[WJ_SanityDebug] K 눌림");

            if (SanityManager.Instance == null)
            {
                Debug.Log("[WJ_SanityDebug] SanityManager.Instance == null 이라서 데미지 못 줌");
                return;
            }

            Debug.Log("[WJ_SanityDebug] 감소 전 Sanity = "
                + SanityManager.Instance.CurrentSanity);

            SanityManager.Instance.ApplyDamage(10f);

            Debug.Log("[WJ_SanityDebug] 감소 후 Sanity = "
                + SanityManager.Instance.CurrentSanity);
        }
    }
}
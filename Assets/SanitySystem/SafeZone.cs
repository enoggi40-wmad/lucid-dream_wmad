using UnityEngine;

namespace LucidDream.Core
{
    /// <summary>
    /// 안정 구역(Safe Zone).
    /// - 진입 시 정신력 완만 회복
    /// - 해리성 단계 일시적 하락 (선택)
    /// - 이탈 시 원래 상태로 복귀
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SafeZone : MonoBehaviour
    {
        [Header("효과")]
        [SerializeField] private bool reduceDissociationOnEnter = true;
        [SerializeField] private int dissociationReductionAmount = 1;

        private bool alreadyReduced = false;

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            SanityManager.Instance?.SetSafeZone(true);

            if (reduceDissociationOnEnter && !alreadyReduced)
            {
                DissociationLevelManager.Instance?.TemporaryReduce(dissociationReductionAmount);
                alreadyReduced = true;
            }

            Debug.Log("[SafeZone] 플레이어 진입 - 안정화");
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            SanityManager.Instance?.SetSafeZone(false);
            Debug.Log("[SafeZone] 플레이어 이탈");
        }
    }
}
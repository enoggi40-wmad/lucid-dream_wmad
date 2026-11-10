using UnityEngine;
using UnityEngine.Events;

namespace LucidDream.Core
{
    /// <summary>
    /// 정화의 샘 / 오염된 오브젝트.
    /// - Interact() 호출 시 정신력 회복 + 체크포인트로 기록
    /// - 해리성 단계의 purificationEfficiency 계수를 적용해 회복량이 감소
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PurificationPoint : MonoBehaviour
    {
        [Header("정화 설정")]
        [SerializeField] private float baseHealAmount = 30f;
        [SerializeField] private bool consumeOnUse = true; // 1회성 여부
        [SerializeField] private bool setAsRespawnPoint = true;

        [Header("상태")]
        [SerializeField] private bool isCorrupted = true; // true면 아직 정화 필요

        [Header("비주얼")]
        [SerializeField] private GameObject corruptedVFX;
        [SerializeField] private GameObject purifiedVFX;

        public UnityEvent OnPurified;

        private void Start()
        {
            UpdateVFX();
        }

        public void Interact()
        {
            if (!isCorrupted) return;

            float multiplier = DissociationLevelManager.Instance != null
                ? DissociationLevelManager.Instance.CurrentPurificationEfficiency
                : 1f;

            float heal = baseHealAmount * multiplier;
            SanityManager.Instance?.Heal(heal);

            if (setAsRespawnPoint)
                CheckpointRegistry.SetLastCheckpoint(transform.position);

            if (consumeOnUse)
            {
                isCorrupted = false;
                UpdateVFX();
            }

            OnPurified?.Invoke();
            Debug.Log($"[Purification] +{heal:F1} Sanity 회복 (효율 x{multiplier:F2})");
        }

        private void UpdateVFX()
        {
            if (corruptedVFX) corruptedVFX.SetActive(isCorrupted);
            if (purifiedVFX) purifiedVFX.SetActive(!isCorrupted);
        }

        // 예시: 트리거 진입 시 E키로 상호작용
        private void OnTriggerStay(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            if (Input.GetKeyDown(KeyCode.E)) Interact();
        }
    }

}
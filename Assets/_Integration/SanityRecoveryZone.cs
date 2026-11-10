using UnityEngine;
using LucidDream.Core;

namespace LucidDream.Integration
{
    /// <summary>
    /// 난이도 조절용 정신력 회복존(Recovery Zone).
    /// - 플레이어가 존 안에 머무는 동안 존별로 지정한 속도로 Sanity를 회복한다.
    /// - 옵션으로 머무는 동안 Passive Decay(자연 감소)를 멈춰 회복이 감소와 상쇄되지 않게 한다.
    /// - 옵션으로 진입 시 해리성 단계를 1회 낮출 수 있다.
    ///
    /// 기존 SanitySystem.SafeZone(전역 회복률 사용)과 달리, 이 컴포넌트는 존마다
    /// 회복률을 따로 줄 수 있어 맵 구간별 난이도 튜닝에 쓰기 좋다.
    /// SetSafeZone(전역 bool)을 건드리지 않고 Heal()로 직접 회복하므로 존이 겹쳐도 안전하다.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SanityRecoveryZone : MonoBehaviour
    {
        [Header("감지")]
        [SerializeField] private string playerTag = "Player";

        [Header("회복")]
        [Tooltip("존 안에 있는 동안 초당 회복하는 Sanity 양.")]
        [SerializeField] private float regenPerSecond = 6f;
        [Tooltip("존 안에 있는 동안 Passive Decay를 멈춰 회복이 자연 감소와 상쇄되지 않게 한다.")]
        [SerializeField] private bool pausePassiveDecayInside = true;

        [Header("해리성 단계 (선택)")]
        [Tooltip("플레이어가 처음 진입할 때 해리성 단계를 1회 낮춘다.")]
        [SerializeField] private bool reduceDissociationOnEnter = false;
        [SerializeField] private int dissociationReductionAmount = 1;

        [Header("기즈모")]
        [SerializeField] private Color gizmoColor = new Color(0.3f, 0.8f, 1f, 0.25f);

        private bool playerInside = false;
        private bool decayPaused = false;
        private bool alreadyReduced = false;

        private void Reset()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(playerTag)) return;
            playerInside = true;

            if (pausePassiveDecayInside && !decayPaused && SanityManager.Instance != null)
            {
                SanityManager.Instance.PausePassiveDecay();
                decayPaused = true;
            }

            if (reduceDissociationOnEnter && !alreadyReduced)
            {
                DissociationLevelManager.Instance?.TemporaryReduce(dissociationReductionAmount);
                alreadyReduced = true;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag(playerTag)) return;
            playerInside = false;
            ReleaseDecayPause();
        }

        private void Update()
        {
            if (!playerInside || SanityManager.Instance == null) return;
            SanityManager.Instance.Heal(regenPerSecond * Time.deltaTime);
        }

        private void ReleaseDecayPause()
        {
            if (decayPaused && SanityManager.Instance != null)
                SanityManager.Instance.ResumePassiveDecay();
            decayPaused = false;
        }

        // 존이 비활성화/파괴되거나 씬이 언로드될 때 Passive Decay가 멈춘 채 남지 않도록 보장.
        private void OnDisable()
        {
            playerInside = false;
            ReleaseDecayPause();
        }

        private void OnDrawGizmos()
        {
            var col = GetComponent<Collider>();
            if (col == null) return;
            Gizmos.color = gizmoColor;
            Gizmos.matrix = transform.localToWorldMatrix;
            if (col is BoxCollider box) Gizmos.DrawCube(box.center, box.size);
            else if (col is SphereCollider sph) Gizmos.DrawSphere(sph.center, sph.radius);
        }
    }
}

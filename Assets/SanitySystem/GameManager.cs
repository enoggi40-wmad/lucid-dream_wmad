using UnityEngine;

namespace LucidDream.Core
{
    /// <summary>
    /// 게임 진행 총괄: 의식 붕괴 시 부활 처리, 오염 구역 판정 등.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private float respawnDelay = 2f;

        private void Start()
        {
            if (SanityManager.Instance != null)
                SanityManager.Instance.OnSanityBreak.AddListener(OnPlayerBreak);
        }

        private void OnPlayerBreak()
        {
            Invoke(nameof(DoRespawn), respawnDelay);
        }

        private void DoRespawn()
        {
            if (player == null || SanityManager.Instance == null) return;
            Vector3 pos = CheckpointRegistry.GetLastCheckpoint();
            SanityManager.Instance.Respawn(pos);
            Debug.Log("[GameManager] 플레이어 부활 완료");
        }
    }

    /// <summary>
    /// 오염 구역(Corrupted Zone). 진입 시 환경 요인(Environmental Factor) 증가.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class CorruptedZone : MonoBehaviour
    {
        [SerializeField] private float environmentalDamagePerSecond = 2f;

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            SanityManager.Instance?.SetEnvironmentalFactor(environmentalDamagePerSecond);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            SanityManager.Instance?.SetEnvironmentalFactor(0f);
        }
    }
}
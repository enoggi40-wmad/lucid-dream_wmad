using UnityEngine;
using UnityEngine.Events;
using LucidDream.Core;

namespace LucidDream.Integration
{
    /// <summary>
    /// 기존 PurifiableObject(예: HospitalChartPurifiable 등)를 한 줄도 고치지 않고
    /// "정화 완료 → Sanity 회복 / 해리성 완화" 보상을 붙여주는 어댑터.
    ///
    /// 같은 오브젝트의 PurifiableObject 가 정화 완료(IsPurified == true)되는 순간을 감지하여
    /// 단 한 번 보상을 지급한다.
    ///   => Purifying ↔ Sanity ↔ Dissociation 연결을 "모든" 정화 대상으로 확장.
    ///
    /// 참고: 이상현상 전용 연결은 AnomalyPurifiable 가 담당하므로, 이 어댑터는
    ///       이상현상이 아닌 일반 정화 대상(차트, 오브젝트 등)에 붙이면 된다.
    /// </summary>
    [DisallowMultipleComponent]
    public class PurifyReward : MonoBehaviour
    {
        [Header("대상")]
        [Tooltip("비워두면 같은 오브젝트의 PurifiableObject 를 자동으로 찾는다.")]
        [SerializeField] private PurifiableObject target;

        [Header("Sanity 보상")]
        [SerializeField] private float sanityReward = 15f;
        [Tooltip("회복량을 현재 해리성 단계의 정화 효율로 보정할지 여부.")]
        [SerializeField] private bool scaleByEfficiency = true;

        [Header("해리성 단계")]
        [SerializeField] private bool reduceDissociation = false;
        [SerializeField] private int dissociationReduceAmount = 1;

        [Header("이벤트")]
        public UnityEvent OnRewardGranted;

        private bool rewarded;

        private void Awake()
        {
            if (target == null) target = GetComponent<PurifiableObject>();
            if (target == null)
                Debug.LogWarning($"[PurifyReward] '{name}'에 PurifiableObject가 없어 보상이 동작하지 않습니다.");
        }

        private void Update()
        {
            if (rewarded || target == null) return;
            if (!target.IsPurified) return;

            GrantReward();
        }

        private void GrantReward()
        {
            rewarded = true;

            float efficiency = (scaleByEfficiency && DissociationLevelManager.Instance != null)
                ? DissociationLevelManager.Instance.CurrentPurificationEfficiency
                : 1f;

            float heal = sanityReward * efficiency;
            if (SanityManager.Instance != null && heal > 0f)
                SanityManager.Instance.Heal(heal);

            if (reduceDissociation && DissociationLevelManager.Instance != null)
                DissociationLevelManager.Instance.TemporaryReduce(dissociationReduceAmount);

            OnRewardGranted?.Invoke();
            Debug.Log($"[PurifyReward] '{name}' 정화 보상 → Sanity +{heal:F1} (정화 효율 x{efficiency:F2})");
        }
    }
}

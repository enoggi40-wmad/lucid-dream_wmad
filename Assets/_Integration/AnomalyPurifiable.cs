using UnityEngine;
using UnityEngine.Events;
using LucidDream.Core;

namespace LucidDream.Integration
{
    /// <summary>
    /// "정화 가능한 이상현상". JJY의 PurifiableObject(빔 정화 시스템)를 상속하여,
    /// 정화가 완료되는 순간 네 시스템을 한꺼번에 연결하는 핵심(keystone) 컴포넌트.
    ///
    /// 정화 완료 시:
    ///   1) Anomaly      : 같은 오브젝트의 AnomalyThreat.Neutralize() 호출 → 이상현상 무력화(Sanity 침식 중단)
    ///   2) Purifying→Sanity : SanityManager.Heal() 로 정신력 회복
    ///   3) Dissociation : 회복량을 현재 해리성 단계의 정화 효율(CurrentPurificationEfficiency)로 보정
    ///                     (옵션) 해리성 단계를 한 단계 낮춤(TemporaryReduce)
    ///
    /// 사용법: 이상현상 GameObject에
    ///   - 이 컴포넌트(AnomalyPurifiable)
    ///   - AnomalyThreat
    ///   - Collider (정화 빔 레이캐스트 대상)
    /// 를 붙이면, 플레이어가 PurifyController(우클릭 정화 빔)로 조준해 정화할 수 있고
    /// 정화가 끝나면 위 1~3이 자동으로 일어난다.
    /// </summary>
    public class AnomalyPurifiable : PurifiableObject
    {
        [Header("정화 보상 - Sanity")]
        [Tooltip("정화 완료 시 회복되는 기본 Sanity 양.")]
        [SerializeField] private float sanityRewardOnComplete = 25f;

        [Tooltip("회복량을 현재 해리성 단계의 정화 효율로 보정할지 여부. (단계가 높을수록 회복량 감소)")]
        [SerializeField] private bool scaleRewardByEfficiency = true;

        [Header("정화 보상 - 해리성 단계")]
        [Tooltip("정화 완료 시 해리성 단계를 낮출지 여부.")]
        [SerializeField] private bool reduceDissociationOnComplete = false;
        [SerializeField] private int dissociationReduceAmount = 1;

        [Header("정화 시 소멸(시각)")]
        [Tooltip("정화 완료 시 모델(자식 Renderer/Collider)을 꺼서 사라지게 한다.")]
        [SerializeField] private bool hideOnComplete = true;
        [Tooltip("소멸까지 지연(초). 정화 이펙트가 보이도록 약간 줄 수 있음.")]
        [SerializeField] private float hideDelay = 0.3f;

        [Header("이벤트")]
        public UnityEvent OnAnomalyPurified;

        protected override void OnPurifyComplete()
        {
            base.OnPurifyComplete();

            // 1) 이상현상 무력화 (Purifying ↔ Anomaly)
            var threat = GetComponent<AnomalyThreat>();
            if (threat == null) threat = GetComponentInParent<AnomalyThreat>();
            if (threat != null) threat.Neutralize();

            // 3) 해리성 단계 정화 효율 보정 (Dissociation ↔ Purifying)
            float efficiency = (scaleRewardByEfficiency && DissociationLevelManager.Instance != null)
                ? DissociationLevelManager.Instance.CurrentPurificationEfficiency
                : 1f;

            // 2) Sanity 회복 (Purifying ↔ Sanity)
            float heal = sanityRewardOnComplete * efficiency;
            if (SanityManager.Instance != null && heal > 0f)
                SanityManager.Instance.Heal(heal);

            // (옵션) 해리성 단계 완화
            if (reduceDissociationOnComplete && DissociationLevelManager.Instance != null)
                DissociationLevelManager.Instance.TemporaryReduce(dissociationReduceAmount);

            OnAnomalyPurified?.Invoke();
            Debug.Log($"[AnomalyPurifiable] '{name}' 정화 완료 → Sanity +{heal:F1} (정화 효율 x{efficiency:F2})");

            // 4) 정화 시 소멸 (시각): 모델/콜라이더 끄기
            if (hideOnComplete)
            {
                if (hideDelay > 0f) Invoke(nameof(HideVisuals), hideDelay);
                else HideVisuals();
            }
        }

        private void HideVisuals()
        {
            foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = false;
            foreach (var c in GetComponentsInChildren<Collider>(true)) c.enabled = false;
        }
    }
}

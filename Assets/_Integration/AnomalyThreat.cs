using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using LucidDream.Core;

namespace LucidDream.Integration
{
    /// <summary>
    /// 이상현상(Anomaly)을 "위협"으로 만들어 Sanity 시스템과 연결하는 컴포넌트.
    ///
    /// 연결 관계:
    ///  - Anomaly → Sanity      : 등장(활성) 상태이고 플레이어가 위협 반경 안에 있으면 매 초 Sanity를 깎는다.
    ///  - Dissociation → Anomaly : 현재 해리성 단계의 "활성 난이도 등급"과 이 이상현상의 difficulty가
    ///                             일치할 때만 등장한다.
    ///                              · 단계가 높을수록(해리 심화) 난이도 하(Easy)가 등장 + 정신력 소모 빨라짐.
    ///                              · 단계가 낮을수록(자각 상태) 난이도 상(Hard)가 등장 + 정신력 소모 느려짐.
    ///                             => 단계가 오르내리면 등장하는 이상현상의 난이도 등급이 바뀐다.
    ///  - Purifying → Anomaly    : Neutralize() 가 호출되면 영구 무력화된다(이후 단계가 바뀌어도 재등장하지 않음).
    ///
    /// 사용법: 기존 Woojin 이상현상 스크립트(WJ_Anomaly_*)를 수정할 필요 없이,
    ///         이상현상 GameObject에 이 컴포넌트를 붙이고 difficulty 등급만 지정하면 된다.
    /// </summary>
    [DisallowMultipleComponent]
    public class AnomalyThreat : MonoBehaviour
    {
        [Header("위협(Sanity 침식) 설정")]
        [Tooltip("초당 기본 Sanity 감소량. 여기에 해리성 단계 공격성 배수가 곱해진다.")]
        [SerializeField] private float baseSanityDrainPerSecond = 1.5f;

        [Tooltip("플레이어가 이 반경 안에 있을 때만 침식한다. 0 이하이면 거리를 무시하고 항상 침식한다.")]
        [SerializeField] private float threatRange = 8f;

        [Tooltip("시야(Line of Sight)가 막혀 있으면 침식하지 않는다.")]
        [SerializeField] private bool requireLineOfSight = false;

        [Tooltip("시야 차단을 판정할 때 사용할 장애물 레이어.")]
        [SerializeField] private LayerMask sightObstacleMask = ~0;

        [Header("난이도 등급 연동")]
        [Tooltip("이 이상현상의 난이도 등급. 현재 해리성 단계의 '활성 등급'과 일치할 때만 등장한다.\n" +
                 "해리성 단계가 높을수록 Easy(하), 낮을수록 Hard(상) 등급이 활성화된다.")]
        [SerializeField] private AnomalyDifficulty difficulty = AnomalyDifficulty.Medium;

        [Tooltip("등장 시 위협(Sanity 침식)을 곧바로 시작할지 여부. false면 Activate()로 켤 수 있다.")]
        [SerializeField] private bool startActive = true;

        [Tooltip("잠복(미등장) 상태에서는 같은 오브젝트의 이상현상 동작 스크립트를 꺼둔다.")]
        [SerializeField] private bool disableSiblingsWhenDormant = true;

        [Header("정화 시 비활성화할 동작들")]
        [Tooltip("Neutralize() 호출 시 꺼질 Behaviour 목록(예: WJ_Anomaly_* 스크립트, 파티클 등).")]
        [SerializeField] private List<Behaviour> disableOnNeutralize = new List<Behaviour>();

        [Tooltip("켜면 등장/잠복/정화 시 같은 오브젝트의 다른 MonoBehaviour(이상현상 동작)들도 자동으로 제어한다. 정화 시스템(PurifiableObject)과 본 컴포넌트는 제외.")]
        [SerializeField] private bool autoControlSiblingBehaviours = true;

        [Header("이벤트")]
        public UnityEvent OnActivated;     // 등장(활성)
        public UnityEvent OnDormant;       // 잠복(미등장)으로 전환
        public UnityEvent OnNeutralized;   // 정화로 영구 무력화

        private Transform player;
        private bool isPresent;       // 현재 단계의 활성 등급과 일치하여 등장 중인지
        private bool isActive;        // 등장 상태에서 실제로 위협을 가하는지
        private bool isNeutralized;   // 정화되어 영구 무력화됐는지

        // Awake 시점에 캐싱한, 본 컴포넌트가 제어할 형제 스크립트들(이상현상 동작).
        private readonly List<MonoBehaviour> siblingBehaviours = new List<MonoBehaviour>();

        /// <summary>현재 플레이어 정신력을 침식하고 있는지(등장 + 활성 + 미정화).</summary>
        public bool IsActive => isPresent && isActive && !isNeutralized;
        public bool IsNeutralized => isNeutralized;
        public bool IsPresent => isPresent;
        public AnomalyDifficulty Difficulty => difficulty;

        private void Awake()
        {
            CacheSiblingBehaviours();

            // 활성 등급은 런타임에 해리성 단계로 결정되므로, 시작 시점에는 잠복시켜
            // 형제 스크립트의 Start()가 조기 실행되지 않도록 Awake 단계에서 미리 꺼둔다.
            if (disableSiblingsWhenDormant)
                SetSiblingBehaviours(false);
        }

        private void Start()
        {
            CachePlayer();

            var mgr = DissociationLevelManager.Instance;
            if (mgr != null)
                mgr.OnLevelChanged.AddListener(OnDissociationLevelChanged);

            // 현재 단계의 활성 등급에 따라 등장/잠복을 결정한다.
            Evaluate(mgr != null ? mgr.CurrentActiveDifficulty : difficulty);
        }

        private void OnDestroy()
        {
            var mgr = DissociationLevelManager.Instance;
            if (mgr != null)
                mgr.OnLevelChanged.RemoveListener(OnDissociationLevelChanged);
        }

        private void OnDissociationLevelChanged(DissociationLevelManager.LevelData data)
        {
            if (data == null) return;
            Evaluate(data.activeDifficulty);
        }

        /// <summary>현재 활성 난이도 등급과 이 이상현상의 등급을 비교해 등장/잠복을 결정한다.</summary>
        private void Evaluate(AnomalyDifficulty activeDifficulty)
        {
            if (isNeutralized) return;

            if (difficulty == activeDifficulty)
                Appear();
            else
                EnterDormant();
        }

        private void Update()
        {
            if (!IsActive) return;

            if (player == null)
            {
                CachePlayer();
                if (player == null) return;
            }

            if (threatRange > 0f)
            {
                float dist = Vector3.Distance(transform.position, player.position);
                if (dist > threatRange) return;
                if (requireLineOfSight && IsSightBlocked(dist)) return;
            }

            var mgr = DissociationLevelManager.Instance;
            float aggression = mgr != null ? mgr.CurrentEnemyAggression : 1f;

            float drain = baseSanityDrainPerSecond * aggression * Time.deltaTime;
            SanityManager.Instance?.ApplyDamage(drain, SanityManager.DamageType.Active);
        }

        private void CacheSiblingBehaviours()
        {
            siblingBehaviours.Clear();
            var all = GetComponents<MonoBehaviour>();
            foreach (var mb in all)
            {
                if (mb == null || mb == this) continue;
                if (mb is PurifiableObject) continue; // 정화 대상 자체는 제어하지 않는다.
                siblingBehaviours.Add(mb);
            }
        }

        private void SetSiblingBehaviours(bool enabled)
        {
            if (!autoControlSiblingBehaviours) return;
            foreach (var mb in siblingBehaviours)
                if (mb != null) mb.enabled = enabled;
        }

        private void CachePlayer()
        {
            // SanityManager가 플레이어에 붙어있는 경우가 많으므로 우선 사용하고, 없으면 태그로 탐색한다.
            var sanity = SanityManager.Instance;
            if (sanity != null)
                player = sanity.transform;

            if (player == null)
            {
                var go = GameObject.FindGameObjectWithTag("Player");
                if (go != null) player = go.transform;
            }
        }

        private bool IsSightBlocked(float dist)
        {
            Vector3 origin = transform.position;
            Vector3 dir = player.position - origin;

            if (Physics.Raycast(origin, dir.normalized, out RaycastHit hit, dist, sightObstacleMask, QueryTriggerInteraction.Ignore))
            {
                // 플레이어보다 먼저 다른 콜라이더에 막혔다면 시야가 차단된 것으로 본다.
                return hit.transform != player && !hit.transform.IsChildOf(player);
            }
            return false;
        }

        /// <summary>이상현상을 등장시킨다(동작 스크립트 활성화 + 위협 시작).</summary>
        private void Appear()
        {
            if (isNeutralized || isPresent) return;
            isPresent = true;
            isActive = startActive;
            SetSiblingBehaviours(true);
            OnActivated?.Invoke();
        }

        /// <summary>이상현상을 잠복(미등장) 상태로 되돌린다. 정화와 달리 단계가 다시 맞으면 재등장한다.</summary>
        private void EnterDormant()
        {
            if (!isPresent)
            {
                // 아직 한 번도 등장하지 않은 초기 상태: 형제 스크립트만 꺼둔다.
                if (disableSiblingsWhenDormant)
                    SetSiblingBehaviours(false);
                return;
            }

            isPresent = false;
            isActive = false;
            if (disableSiblingsWhenDormant)
                SetSiblingBehaviours(false);
            OnDormant?.Invoke();
        }

        /// <summary>이상현상을 정화/무력화한다(영구). 이후 단계가 바뀌어도 다시 등장하지 않는다.</summary>
        public void Neutralize()
        {
            if (isNeutralized) return;
            isNeutralized = true;
            isPresent = false;
            isActive = false;

            foreach (var b in disableOnNeutralize)
                if (b != null) b.enabled = false;

            SetSiblingBehaviours(false);

            OnNeutralized?.Invoke();
            Debug.Log($"[AnomalyThreat] '{name}' 이상현상이 정화되어 무력화됨.");
        }

        /// <summary>(외부 제어용) 등장 상태에서 위협을 다시 켠다. 잠복/정화 상태면 무시된다.</summary>
        public void Activate()
        {
            if (isNeutralized || !isPresent) return;
            isActive = true;
            OnActivated?.Invoke();
        }

        private void OnDrawGizmosSelected()
        {
            if (threatRange <= 0f) return;
            Gizmos.color = new Color(0.7f, 0.1f, 0.9f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, threatRange);
        }
    }
}

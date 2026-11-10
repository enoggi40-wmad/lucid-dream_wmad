using System;
using UnityEngine;
using UnityEngine.Events;

namespace LucidDream.Core
{
    /// <summary>
    /// 플레이어의 정신력(Sanity)을 관리하는 싱글턴 매니저.
    /// - Passive Decay: 해리성 단계에 비례한 자연 감소
    /// - Active Decay: 외부 요인(공격, 기믹 실패 등)에 의한 직접 감소
    /// - Purification: 즉시 회복 / 세이프존 회복
    /// - Break: 0 도달 시 의식 붕괴 이벤트 발생
    /// </summary>
    [DisallowMultipleComponent]
    public class SanityManager : MonoBehaviour
    {
        public static SanityManager Instance { get; private set; }

        [Header("Sanity 기본 설정")]
        [SerializeField] private float maxSanity = 100f;
        [SerializeField] private float currentSanity = 100f;

        [Header("Decay 설정")]
        [Tooltip("해리성 Lv.1 기준 초당 자연 감소량")]
        [SerializeField] private float baseDecayPerSecond = 0.8f;
        [Tooltip("환경적 추가 감소량 (오염 구역 체류 등)")]
        [SerializeField] private float environmentalFactor = 0f;

        [Header("임계점(Threshold)")]
        [Range(0f, 1f)]
        [SerializeField] private float lowSanityThreshold = 0.3f; // 30% 이하 = 악순환 트리거
        [SerializeField] private float breakRespawnPenalty = 25f; // 부활 시 회복량 제한

        [Header("회복 설정")]
        [SerializeField] private float safeZoneRegenPerSecond = 5f;
        private bool isInSafeZone = false;

        // 이벤트
        public UnityEvent<float, float> OnSanityChanged;   // (current, max)
        public UnityEvent OnLowSanityReached;              // 30% 이하 최초 진입
        public UnityEvent OnSanityBreak;                   // 0 도달
        public UnityEvent OnSanityRestored;                // 30% 이상 복귀

        private bool lowSanityFlag = false;
        private bool isBroken = false;

        public float CurrentSanity => currentSanity;
        public float MaxSanity => maxSanity;
        public float SanityRatio => currentSanity / maxSanity;
        public bool IsLowSanity => SanityRatio <= lowSanityThreshold;
        public bool IsBroken => isBroken;

        // Passive Decay pause (ref-counted so multiple pausers stack safely).
        private int passiveDecayPauseCount = 0;
        public bool IsPassiveDecayPaused => passiveDecayPauseCount > 0;

        // Full sanity freeze (blocks ALL decrease: passive + active damage). Ref-counted.
        private int sanityFreezeCount = 0;
        public bool IsSanityFrozen => sanityFreezeCount > 0;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            currentSanity = maxSanity;
        }

        private void Update()
        {
            if (isBroken) return;

            // 1) 세이프존 회복
            if (isInSafeZone)
            {
                Heal(safeZoneRegenPerSecond * Time.deltaTime);
                return;
            }

            // 2) Passive Decay 계산 (해리성 Lv 배율 적용)
            // Passive decay holds while paused (e.g. awareness window before the dream-judgement UI).
            if (IsPassiveDecayPaused) return;

            float levelMultiplier = DissociationLevelManager.Instance != null
                ? DissociationLevelManager.Instance.CurrentDecayMultiplier
                : 1f;

            float finalDecay = (baseDecayPerSecond * levelMultiplier) + environmentalFactor;
            ApplyDamage(finalDecay * Time.deltaTime, DamageType.Passive);
        }

        /// <summary>적 공격, 기믹 실패 등 외부 요인에 의한 즉시 감소</summary>
        public void ApplyDamage(float amount, DamageType type = DamageType.Active)
        {
            if (isBroken || amount <= 0f || IsSanityFrozen) return;

            currentSanity = Mathf.Max(0f, currentSanity - amount);
            OnSanityChanged?.Invoke(currentSanity, maxSanity);

            // 임계점(30%) 최초 진입 체크 → 악순환 트리거
            if (!lowSanityFlag && IsLowSanity)
            {
                lowSanityFlag = true;
                OnLowSanityReached?.Invoke();
                DissociationLevelManager.Instance?.ForceLevelUp("LowSanityTrigger");
            }

            if (currentSanity <= 0f)
            {
                TriggerBreak();
            }
        }

        /// <summary>정화의 샘 등에 의한 즉시 회복</summary>
        public void Heal(float amount)
        {
            if (isBroken) return;
            currentSanity = Mathf.Min(maxSanity, currentSanity + amount);
            OnSanityChanged?.Invoke(currentSanity, maxSanity);

            if (lowSanityFlag && SanityRatio > lowSanityThreshold + 0.05f) // 히스테리시스
            {
                lowSanityFlag = false;
                OnSanityRestored?.Invoke();
            }
        }

        public void SetSafeZone(bool inZone) => isInSafeZone = inZone;

        // --- Passive Decay pause API (awareness-UI window / recovery zones) ---
        public void PausePassiveDecay() => passiveDecayPauseCount++;
        public void ResumePassiveDecay() => passiveDecayPauseCount = Mathf.Max(0, passiveDecayPauseCount - 1);
        public void ResetPassiveDecayPause() => passiveDecayPauseCount = 0;

        // --- Sanity freeze API (dream-judgement UI: keep sanity from dropping at all) ---
        public void FreezeSanity() => sanityFreezeCount++;
        public void UnfreezeSanity() => sanityFreezeCount = Mathf.Max(0, sanityFreezeCount - 1);
        public void ResetSanityFreeze() => sanityFreezeCount = 0;

        public void SetEnvironmentalFactor(float factor) => environmentalFactor = Mathf.Max(0f, factor);

        private void TriggerBreak()
        {
            isBroken = true;
            OnSanityBreak?.Invoke();
            Debug.Log("[Sanity] 의식 붕괴 - 가장 최근 정화 지점에서 부활합니다.");
        }

        /// <summary>최근 정화 지점에서 부활 (패널티 적용)</summary>
        public void Respawn(Vector3 respawnPosition)
        {
            isBroken = false;
            currentSanity = Mathf.Max(breakRespawnPenalty, maxSanity * 0.5f - breakRespawnPenalty);
            lowSanityFlag = false;
            transform.position = respawnPosition;
            OnSanityChanged?.Invoke(currentSanity, maxSanity);
        }

        public enum DamageType { Passive, Active, Environmental }
    }
}
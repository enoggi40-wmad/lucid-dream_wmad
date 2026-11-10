using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace LucidDream.Core
{
    /// <summary>
    /// 해리성 단계(Dissociation Level)를 관리.
    /// - 시간 경과로 자동 상승
    /// - 정화 실패 시 즉시 상승
    /// - 정신력 임계점 도달 시 강제 상승 (악순환)
    /// - 단계별 정신력 감소 배율, 적 공격성 계수, 정화 효율 계수를 제공
    /// </summary>
    [DisallowMultipleComponent]
    public class DissociationLevelManager : MonoBehaviour
    {
        public static DissociationLevelManager Instance { get; private set; }

        [Serializable]
        public class LevelData
        {
            public int level;
            public string name;
            [Tooltip("정신력 감소 배율")]
            public float decayMultiplier = 1f;
            [Tooltip("적 공격성 배율")]
            public float enemyAggressionMultiplier = 1f;
            [Tooltip("정화 효율 배율 (낮을수록 회복량 감소)")]
            public float purificationEfficiency = 1f;
            [Tooltip("이 단계에서 머무는 최대 시간(초). 초과 시 다음 단계로.")]
            public float timeToNextLevel = 120f;
            [Tooltip("Active anomaly difficulty at this level. High level => Easy, low level => Hard.")]
            public AnomalyDifficulty activeDifficulty = AnomalyDifficulty.Medium;
        }

        [Header("단계 정의 (기획안 기준 5단계)")]
        [SerializeField]
        private List<LevelData> levels = new List<LevelData>
        {
            new LevelData { level = 1, name = "자각 (Awareness)",      decayMultiplier = 1.0f, enemyAggressionMultiplier = 1.0f, purificationEfficiency = 1.0f, timeToNextLevel = 90f, activeDifficulty = AnomalyDifficulty.Hard },
            new LevelData { level = 2, name = "혼돈 (Chaos)",          decayMultiplier = 1.2f, enemyAggressionMultiplier = 1.2f, purificationEfficiency = 0.9f, timeToNextLevel = 120f, activeDifficulty = AnomalyDifficulty.Hard },
            new LevelData { level = 3, name = "침식 (Erosion)",        decayMultiplier = 1.5f, enemyAggressionMultiplier = 1.4f, purificationEfficiency = 0.7f, timeToNextLevel = 150f, activeDifficulty = AnomalyDifficulty.Medium },
            new LevelData { level = 4, name = "망각 (Oblivion)",       decayMultiplier = 2.0f, enemyAggressionMultiplier = 1.7f, purificationEfficiency = 0.5f, timeToNextLevel = 180f, activeDifficulty = AnomalyDifficulty.Easy },
            new LevelData { level = 5, name = "해리 (Dissociation)",   decayMultiplier = 3.0f, enemyAggressionMultiplier = 2.0f, purificationEfficiency = 0.3f, timeToNextLevel = float.MaxValue, activeDifficulty = AnomalyDifficulty.Easy },
        };

        [Header("현재 상태")]
        [SerializeField] private int currentLevelIndex = 0;
        [SerializeField] private float elapsedInLevel = 0f;

        [Header("이벤트")]
        public UnityEvent<LevelData> OnLevelChanged;
        public UnityEvent<LevelData> OnForcedLevelUp; // 악순환/실패 트리거

        public LevelData Current => levels[currentLevelIndex];
        public float CurrentDecayMultiplier => Current.decayMultiplier;
        public float CurrentEnemyAggression => Current.enemyAggressionMultiplier;
        public float CurrentPurificationEfficiency => Current.purificationEfficiency;
        public AnomalyDifficulty CurrentActiveDifficulty => Current.activeDifficulty;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            OnLevelChanged?.Invoke(Current);
        }

        private void Update()
        {
            if (currentLevelIndex >= levels.Count - 1) return;

            elapsedInLevel += Time.deltaTime;
            if (elapsedInLevel >= Current.timeToNextLevel)
            {
                LevelUp("TimeElapsed");
            }
        }

        /// <summary>일반적인 단계 상승 (시간 경과)</summary>
        public void LevelUp(string reason)
        {
            if (currentLevelIndex >= levels.Count - 1) return;
            currentLevelIndex++;
            elapsedInLevel = 0f;
            Debug.Log($"[Dissociation] {Current.name} 단계로 상승. 사유: {reason}");
            OnLevelChanged?.Invoke(Current);
        }

        /// <summary>정화 실패 / 정신력 임계점 등 즉시 상승</summary>
        public void ForceLevelUp(string reason)
        {
            if (currentLevelIndex >= levels.Count - 1) return;
            currentLevelIndex++;
            elapsedInLevel = 0f;
            Debug.Log($"[Dissociation] 강제 상승 → {Current.name}. 사유: {reason}");
            OnLevelChanged?.Invoke(Current);
            OnForcedLevelUp?.Invoke(Current);
        }

        /// <summary>세이프존 등에서 일시적 단계 하락 (선택 기믹)</summary>
        public void TemporaryReduce(int amount = 1)
        {
            currentLevelIndex = Mathf.Max(0, currentLevelIndex - amount);
            elapsedInLevel = 0f;
            OnLevelChanged?.Invoke(Current);
        }
    }
}
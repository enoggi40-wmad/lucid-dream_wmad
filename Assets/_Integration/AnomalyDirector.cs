using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace LucidDream.Integration
{
    /// <summary>
    /// 씬 안의 모든 이상현상(AnomalyPurifiable)을 추적하는 감독 컴포넌트.
    ///
    /// 역할:
    ///  - 씬 시작 시 모든 AnomalyPurifiable을 자동 수집 (수동 등록도 가능)
    ///  - 정화 완료 개수 / 전체 개수를 집계하여 HUD에 제공
    ///  - 모든 이상현상이 정화되면 OnAllPurified 발생 → GameFlowManager가 게임 클리어 처리
    ///
    /// 사용법: 씬에 빈 GameObject를 만들고 이 컴포넌트를 붙이기만 하면 된다.
    /// (Tools ▸ Lucid Dream ▸ UI ▸ Build In-Game UI 실행 시 자동 생성됨)
    /// </summary>
    [DisallowMultipleComponent]
    public class AnomalyDirector : MonoBehaviour
    {
        public static AnomalyDirector Instance { get; private set; }

        [Header("수집 설정")]
        [Tooltip("씬 시작 시 씬 안의 모든 AnomalyPurifiable을 자동으로 수집한다.")]
        [SerializeField] private bool autoCollectOnStart = true;

        [Tooltip("비활성 오브젝트(잠복 중 이상현상)도 수집 대상에 포함한다.")]
        [SerializeField] private bool includeInactive = true;

        [Header("이벤트")]
        [Tooltip("(정화된 개수, 전체 개수)")]
        public UnityEvent<int, int> OnProgressChanged;
        public UnityEvent<AnomalyPurifiable> OnAnomalyPurified;
        public UnityEvent OnAllPurified;

        private readonly List<AnomalyPurifiable> anomalies = new List<AnomalyPurifiable>();
        private int purifiedCount;
        private bool allPurifiedFired;

        public int Total => anomalies.Count;
        public int PurifiedCount => purifiedCount;
        public int Remaining => Total - purifiedCount;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            if (autoCollectOnStart)
                CollectAll();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>씬 안의 모든 AnomalyPurifiable을 수집하고 정화 이벤트를 구독한다.</summary>
        public void CollectAll()
        {
            var found = FindObjectsByType<AnomalyPurifiable>(
                includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude);

            foreach (var anomaly in found)
                Register(anomaly);

            NotifyProgress();
        }

        /// <summary>이상현상을 수동으로 등록한다. (런타임에 스폰되는 이상현상용)</summary>
        public void Register(AnomalyPurifiable anomaly)
        {
            if (anomaly == null || anomalies.Contains(anomaly)) return;

            anomalies.Add(anomaly);

            if (anomaly.IsPurified)
            {
                purifiedCount++;
            }
            else
            {
                // 람다 캡처로 어떤 이상현상이 정화됐는지 추적한다.
                anomaly.OnAnomalyPurified.AddListener(() => HandlePurified(anomaly));
            }
        }

        private void HandlePurified(AnomalyPurifiable anomaly)
        {
            purifiedCount++;
            OnAnomalyPurified?.Invoke(anomaly);
            NotifyProgress();

            if (!allPurifiedFired && purifiedCount >= Total && Total > 0)
            {
                allPurifiedFired = true;
                Debug.Log("[AnomalyDirector] 모든 이상현상 정화 완료!");
                OnAllPurified?.Invoke();
            }
        }

        private void NotifyProgress()
        {
            OnProgressChanged?.Invoke(purifiedCount, Total);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using LucidDream.Core;

namespace LucidDream.Integration
{
    /// <summary>
    /// 스테이지(씬) 전환 시 정신력을 일정량 회복시키는 매니저.
    ///
    /// SanityManager는 DontDestroyOnLoad라 씬을 넘어가도 정신력이 유지된다.
    /// 이 컴포넌트는 그 위에서, 다음 스테이지로 넘어갈 때마다 한 번씩 숨통을 틔워 주는
    /// 난이도 페이싱 장치다. (Awake에서 sceneLoaded를 구독하므로, 자기 자신이 생성된
    /// "현재 스테이지"는 회복 대상이 아니다 — 그 이후의 전환부터 적용된다.)
    ///
    /// 싱글톤 + DontDestroyOnLoad. 부트스트랩(타이틀/첫 스테이지) GameObject에 하나만 둔다.
    /// </summary>
    [DisallowMultipleComponent]
    public class StageTransitionSanity : MonoBehaviour
    {
        public static StageTransitionSanity Instance { get; private set; }

        [Header("회복량")]
        [Tooltip("true면 amount를 최대 정신력의 퍼센트(0~100)로, false면 고정 수치로 해석한다.")]
        [SerializeField] private bool asPercentOfMax = false;
        [Tooltip("스테이지 전환마다 회복할 정신력. (퍼센트 모드면 0~100)")]
        [SerializeField] private float amount = 30f;

        [Header("어떤 씬을 스테이지로 볼지")]
        [Tooltip("비어 있지 않으면, 여기에 적힌 씬 이름으로 로드될 때만 회복한다. 비어 있으면 제외 목록 외 모든 씬 로드에서 회복.")]
        [SerializeField] private List<string> stageSceneNames = new List<string>();
        [Tooltip("절대 회복을 발생시키지 않을 씬 이름들 (예: Title, 메뉴).")]
        [SerializeField] private List<string> excludedScenes = new List<string> { "Title" };

        [Header("옵션")]
        [Tooltip("스테이지 전환 시 해리성 단계도 1 낮춘다.")]
        [SerializeField] private bool alsoReduceDissociation = false;

        public UnityEvent OnStageRecovery;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Additive) return;
            if (!QualifiesAsStage(scene.name)) return;
            RecoverNow();
        }

        private bool QualifiesAsStage(string sceneName)
        {
            if (excludedScenes != null && excludedScenes.Contains(sceneName)) return false;
            if (stageSceneNames != null && stageSceneNames.Count > 0)
                return stageSceneNames.Contains(sceneName);
            return true;
        }

        /// <summary>설정된 회복을 즉시 적용. UI 버튼/UnityEvent에서도 호출 가능.</summary>
        public void RecoverNow()
        {
            var sanity = SanityManager.Instance;
            if (sanity == null) return;

            float heal = asPercentOfMax ? sanity.MaxSanity * (amount / 100f) : amount;
            sanity.Heal(heal);

            if (alsoReduceDissociation)
                DissociationLevelManager.Instance?.TemporaryReduce(1);

            OnStageRecovery?.Invoke();
            Debug.Log($"[StageTransitionSanity] 스테이지 전환 회복: +{heal:0.#} Sanity");
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using LucidDream.Core;

namespace LucidDream.Integration
{
    /// <summary>
    /// 게임 흐름(상태) 총괄 매니저. 프로토타입의 뼈대가 되는 게임 루프를 완성한다.
    ///
    /// 상태 전이:
    ///   Playing ──ESC──▶ Paused ──ESC/재개──▶ Playing
    ///   Playing ──모든 이상현상 정화──▶ Cleared   (AnomalyDirector.OnAllPurified)
    ///   Playing ──Sanity 0 + 부활 횟수 소진──▶ GameOver (SanityManager.OnSanityBreak)
    ///
    /// 기존 GameManager(SanitySystem)의 체크포인트 부활 로직은 그대로 두고,
    /// 이 매니저는 "부활 가능 횟수"만 추가로 관리한다. (maxRespawns 이하일 때는 GameManager가 부활시킴)
    ///
    /// 일시정지 시 PlayerController가 ESC 입력으로 커서를 다시 잠그는 것을 막기 위해
    /// disableWhilePaused 목록의 컴포넌트를 통째로 꺼둔다.
    /// </summary>
    [DisallowMultipleComponent]
    public class GameFlowManager : MonoBehaviour
    {
        public static GameFlowManager Instance { get; private set; }

        public enum GameState { Playing, Paused, Cleared, GameOver }

        [Header("씬 이름")]
        [SerializeField] private string titleSceneName = "Title";

        [Header("게임 오버 규칙")]
        [Tooltip("정신력 붕괴(Sanity 0) 시 부활 가능한 횟수. -1이면 무제한 부활(게임 오버 없음).")]
        [SerializeField] private int maxRespawns = -1;

        [Header("일시정지 시 끌 컴포넌트")]
        [Tooltip("일시정지 동안 비활성화할 컴포넌트들. 비워두면 PlayerController/PurifyController를 자동으로 찾는다.")]
        [SerializeField] private List<Behaviour> disableWhilePaused = new List<Behaviour>();

        [Header("이벤트")]
        public UnityEvent OnPaused;
        public UnityEvent OnResumed;
        public UnityEvent OnGameCleared;
        public UnityEvent OnGameOver;

        private int breakCount;

        public GameState State { get; private set; } = GameState.Playing;
        public bool IsGameplayActive => State == GameState.Playing;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            Time.timeScale = 1f;

            if (disableWhilePaused.Count == 0)
                AutoFindPauseTargets();

            if (SanityManager.Instance != null)
                SanityManager.Instance.OnSanityBreak.AddListener(HandleSanityBreak);

            if (AnomalyDirector.Instance != null)
                AnomalyDirector.Instance.OnAllPurified.AddListener(HandleAllPurified);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                Time.timeScale = 1f; // 씬 전환 도중 멈춘 상태로 남지 않도록 복구
            }

            if (SanityManager.Instance != null)
                SanityManager.Instance.OnSanityBreak.RemoveListener(HandleSanityBreak);

            if (AnomalyDirector.Instance != null)
                AnomalyDirector.Instance.OnAllPurified.RemoveListener(HandleAllPurified);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (State == GameState.Playing) Pause();
                else if (State == GameState.Paused) Resume();
            }
        }

        private void AutoFindPauseTargets()
        {
            var player = FindAnyObjectByType<PlayerController>();
            if (player != null) disableWhilePaused.Add(player);

            var purify = FindAnyObjectByType<PurifyController>();
            if (purify != null) disableWhilePaused.Add(purify);
        }

        // ── 일시정지 ──────────────────────────────────────────────

        public void Pause()
        {
            if (State != GameState.Playing) return;
            State = GameState.Paused;

            Time.timeScale = 0f;
            SetPauseTargets(false);
            UnlockCursor();
            OnPaused?.Invoke();
        }

        public void Resume()
        {
            if (State != GameState.Paused) return;
            State = GameState.Playing;

            Time.timeScale = 1f;
            SetPauseTargets(true);
            LockCursor();
            OnResumed?.Invoke();
        }

        private void SetPauseTargets(bool enabled)
        {
            foreach (var b in disableWhilePaused)
                if (b != null) b.enabled = enabled;
        }

        // ── 클리어 / 게임 오버 ────────────────────────────────────

        private void HandleAllPurified()
        {
            if (State == GameState.Cleared || State == GameState.GameOver) return;
            State = GameState.Cleared;

            Time.timeScale = 0f;
            SetPauseTargets(false);
            UnlockCursor();
            Debug.Log("[GameFlow] 게임 클리어 - 꿈이 정화되었습니다.");
            OnGameCleared?.Invoke();
        }

        private void HandleSanityBreak()
        {
            breakCount++;

            // 무제한 부활이거나 아직 횟수가 남았으면 기존 GameManager가 부활을 처리한다.
            if (maxRespawns < 0 || breakCount <= maxRespawns) return;

            if (State == GameState.Cleared || State == GameState.GameOver) return;
            State = GameState.GameOver;

            Time.timeScale = 0f;
            SetPauseTargets(false);
            UnlockCursor();
            Debug.Log("[GameFlow] 게임 오버 - 의식이 악몽에 잠식되었습니다.");
            OnGameOver?.Invoke();
        }

        // ── 씬 전환 (UI 버튼에서 호출) ────────────────────────────

        public void RestartCurrentScene()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void ReturnToTitle()
        {
            Time.timeScale = 1f;
            UnlockCursor();
            SceneManager.LoadScene(titleSceneName);
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ── 커서 ─────────────────────────────────────────────────

        private static void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private static void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}

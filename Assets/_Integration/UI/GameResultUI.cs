using UnityEngine;
using UnityEngine.UI;
using LucidDream.Integration;

namespace LucidDream.UI
{
    /// <summary>
    /// 게임 결과 화면(클리어 / 게임 오버) UI.
    /// GameFlowManager의 OnGameCleared / OnGameOver 이벤트를 받아 해당 패널을 띄운다.
    ///
    /// 각 패널 버튼: 다시 하기(씬 재시작) / 타이틀로
    /// </summary>
    public class GameResultUI : MonoBehaviour
    {
        [Header("클리어 패널")]
        [SerializeField] private GameObject clearPanel;
        [SerializeField] private Button clearRestartButton;
        [SerializeField] private Button clearTitleButton;

        [Header("게임 오버 패널")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private Button gameOverRestartButton;
        [SerializeField] private Button gameOverTitleButton;

        private void Awake()
        {
            if (clearRestartButton != null) clearRestartButton.onClick.AddListener(Restart);
            if (clearTitleButton != null) clearTitleButton.onClick.AddListener(ToTitle);
            if (gameOverRestartButton != null) gameOverRestartButton.onClick.AddListener(Restart);
            if (gameOverTitleButton != null) gameOverTitleButton.onClick.AddListener(ToTitle);
        }

        private void Start()
        {
            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.OnGameCleared.AddListener(ShowClear);
                GameFlowManager.Instance.OnGameOver.AddListener(ShowGameOver);
            }

            if (clearPanel != null) clearPanel.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.OnGameCleared.RemoveListener(ShowClear);
                GameFlowManager.Instance.OnGameOver.RemoveListener(ShowGameOver);
            }
        }

        private void ShowClear()
        {
            if (clearPanel != null) clearPanel.SetActive(true);
        }

        private void ShowGameOver()
        {
            if (gameOverPanel != null) gameOverPanel.SetActive(true);
        }

        private void Restart()
        {
            GameFlowManager.Instance?.RestartCurrentScene();
        }

        private void ToTitle()
        {
            GameFlowManager.Instance?.ReturnToTitle();
        }
    }
}

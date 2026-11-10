using UnityEngine;
using UnityEngine.UI;
using LucidDream.Integration;

namespace LucidDream.UI
{
    /// <summary>
    /// 일시정지 메뉴 UI. ESC 입력 자체는 GameFlowManager가 처리하고,
    /// 이 컴포넌트는 GameFlowManager의 OnPaused/OnResumed 이벤트에 맞춰 패널을 열고 닫는다.
    ///
    /// 버튼: 계속하기 / 설정 / 타이틀로 나가기
    /// </summary>
    public class PauseMenuUI : MonoBehaviour
    {
        [Header("패널")]
        [Tooltip("일시정지 시 표시할 루트 패널. 이 컴포넌트가 붙은 오브젝트와 분리해야 한다.")]
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private SettingsPanelUI settingsPanel;

        [Header("버튼")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button titleButton;

        private void Awake()
        {
            if (resumeButton != null) resumeButton.onClick.AddListener(OnResumeClicked);
            if (settingsButton != null) settingsButton.onClick.AddListener(OnSettingsClicked);
            if (titleButton != null) titleButton.onClick.AddListener(OnTitleClicked);
        }

        private void Start()
        {
            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.OnPaused.AddListener(ShowPanel);
                GameFlowManager.Instance.OnResumed.AddListener(HidePanel);
            }

            HidePanel();
            if (settingsPanel != null)
                settingsPanel.Close();
        }

        private void OnDestroy()
        {
            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.OnPaused.RemoveListener(ShowPanel);
                GameFlowManager.Instance.OnResumed.RemoveListener(HidePanel);
            }
        }

        private void ShowPanel()
        {
            if (pausePanel != null) pausePanel.SetActive(true);
        }

        private void HidePanel()
        {
            if (pausePanel != null) pausePanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.Close();
        }

        private void OnResumeClicked()
        {
            GameFlowManager.Instance?.Resume();
        }

        private void OnSettingsClicked()
        {
            if (settingsPanel != null)
                settingsPanel.Open();
        }

        private void OnTitleClicked()
        {
            GameFlowManager.Instance?.ReturnToTitle();
        }
    }
}

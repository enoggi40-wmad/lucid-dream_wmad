using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LucidDream.UI
{
    /// <summary>
    /// 타이틀 화면 UI. 시작 / 설정 / 종료 3버튼 구성.
    ///
    /// 사용법:
    ///  - 타이틀 전용 씬의 Canvas에 붙인다. (Tools ▸ Lucid Dream ▸ UI ▸ Build Title Scene UI로 자동 생성 가능)
    ///  - gameSceneName에 실제 게임 씬 이름(예: "School")을 넣는다.
    ///  - 게임 씬과 타이틀 씬 모두 Build Settings의 Scenes In Build에 등록해야 한다.
    /// </summary>
    public class TitleScreenUI : MonoBehaviour
    {
        [Header("씬")]
        [Tooltip("시작 버튼을 누르면 로드할 게임 씬 이름.")]
        [SerializeField] private string gameSceneName = "School";

        [Header("버튼")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;

        [Header("패널")]
        [SerializeField] private SettingsPanelUI settingsPanel;

        [Header("연출 (선택)")]
        [Tooltip("타이틀 텍스트가 천천히 숨쉬듯 깜빡이게 한다.")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private float titlePulseSpeed = 1.2f;
        [SerializeField] private float titleMinAlpha = 0.65f;

        private void Awake()
        {
            if (startButton != null) startButton.onClick.AddListener(StartGame);
            if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
            if (quitButton != null) quitButton.onClick.AddListener(QuitGame);
        }

        private void Start()
        {
            // 타이틀에서는 커서가 자유로워야 한다.
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 1f;

            // 설정 매니저를 미리 깨워 저장된 설정(해상도 등)을 적용한다.
            SettingsManager.Instance.ApplyAll();

            if (settingsPanel != null)
                settingsPanel.Close();
        }

        private void Update()
        {
            if (titleText != null)
            {
                float a = Mathf.Lerp(titleMinAlpha, 1f,
                    0.5f + 0.5f * Mathf.Sin(Time.time * titlePulseSpeed));
                var c = titleText.color;
                c.a = a;
                titleText.color = c;
            }
        }

        public void StartGame()
        {
            SceneManager.LoadScene(gameSceneName);
        }

        public void OpenSettings()
        {
            if (settingsPanel != null)
                settingsPanel.Open();
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}

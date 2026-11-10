using UnityEngine;
using UnityEngine.UI;

public class SettingsController : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject settingsPanel; // 설정창 전체 배경판

    [Header("Volume Settings")]
    public Slider volumeSlider;      // 설정창 안의 볼륨 슬라이더

    void Start()
    {
        // 게임이 시작할 때는 설정창을 자동으로 꺼두고 시간을 정상 작동시킵니다.
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
        Time.timeScale = 1f;

        // 이전에 저장된 볼륨 값 불러오기
        if (volumeSlider != null)
        {
            volumeSlider.value = PlayerPrefs.GetFloat("GameVolume", 0.5f);
            AudioListener.volume = volumeSlider.value;
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }
    }

    void Update()
    {
        // 매 프레임마다 ESC 키가 눌렸는지 감지합니다.
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (settingsPanel.activeSelf)
            {
                CloseSettings();
            }
            else
            {
                OpenSettings();
            }
        }
    }

    // 설정창을 여는 함수
    public void OpenSettings()
    {
        settingsPanel.SetActive(true);
        Time.timeScale = 0f; // 게임 일시정지
    }

    // 설정창을 닫는 함수 (ESC를 다시 누르면 실행됨)
    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
        Time.timeScale = 1f; // 게임 재개
    }

    private void OnVolumeChanged(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat("GameVolume", value);
    }

    // 💡 새로운 기능: Quit 버튼을 누르면 게임을 종료하는 함수
    public void QuitGame()
    {
#if UNITY_EDITOR
        // 유니티 에디터 상에서 테스트할 때는 플레이 모드가 꺼지도록 합니다.
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // 실제로 빌드된 빌드 파일(.exe 등)에서는 게임 프로그램이 완전히 종료됩니다.
        Application.Quit();
#endif
    }
}
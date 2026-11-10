using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LucidDream.UI
{
    /// <summary>
    /// 설정 패널 UI. 타이틀 화면과 일시정지 메뉴 양쪽에서 같은 프리팹/패널을 재사용한다.
    ///
    /// 슬라이더/토글/드롭다운 값을 SettingsManager에 전달하고,
    /// 패널이 열릴 때 저장된 값으로 컨트롤을 동기화한다.
    /// </summary>
    public class SettingsPanelUI : MonoBehaviour
    {
        [Header("볼륨")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private TMP_Text masterVolumeValueText;

        [Header("마우스 감도")]
        [SerializeField] private Slider sensitivitySlider;
        [SerializeField] private TMP_Text sensitivityValueText;

        [Header("화면")]
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private TMP_Dropdown resolutionDropdown;
        [SerializeField] private TMP_Dropdown qualityDropdown;

        [Header("닫기")]
        [SerializeField] private Button closeButton;

        private readonly List<Resolution> resolutionOptions = new List<Resolution>();
        private bool syncing; // 동기화 중 onValueChanged 콜백이 설정을 덮어쓰지 않도록 막는 플래그

        private void Awake()
        {
            if (masterVolumeSlider != null)
                masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);

            if (sensitivitySlider != null)
            {
                sensitivitySlider.minValue = 0.1f;
                sensitivitySlider.maxValue = 3f;
                sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
            }

            if (fullscreenToggle != null)
                fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);

            if (resolutionDropdown != null)
            {
                BuildResolutionOptions();
                resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
            }

            if (qualityDropdown != null)
            {
                qualityDropdown.ClearOptions();
                qualityDropdown.AddOptions(new List<string>(QualitySettings.names));
                qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
            }

            if (closeButton != null)
                closeButton.onClick.AddListener(Close);
        }

        private void OnEnable()
        {
            SyncFromSettings();
        }

        /// <summary>저장된 설정값으로 모든 컨트롤을 동기화한다.</summary>
        public void SyncFromSettings()
        {
            var settings = SettingsManager.Instance;
            syncing = true;

            if (masterVolumeSlider != null)
                masterVolumeSlider.value = settings.MasterVolume;
            UpdateVolumeLabel(settings.MasterVolume);

            if (sensitivitySlider != null)
                sensitivitySlider.value = settings.MouseSensitivity;
            UpdateSensitivityLabel(settings.MouseSensitivity);

            if (fullscreenToggle != null)
                fullscreenToggle.isOn = settings.Fullscreen;

            if (resolutionDropdown != null)
                resolutionDropdown.value = FindResolutionIndex(settings.ResolutionWidth, settings.ResolutionHeight);

            if (qualityDropdown != null)
                qualityDropdown.value = settings.QualityLevel;

            syncing = false;
        }

        public void Open()
        {
            gameObject.SetActive(true);
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        // ── 콜백 ─────────────────────────────────────────────────

        private void OnMasterVolumeChanged(float value)
        {
            UpdateVolumeLabel(value);
            if (syncing) return;
            SettingsManager.Instance.SetMasterVolume(value);
        }

        private void OnSensitivityChanged(float value)
        {
            UpdateSensitivityLabel(value);
            if (syncing) return;
            SettingsManager.Instance.SetMouseSensitivity(value);
        }

        private void OnFullscreenChanged(bool value)
        {
            if (syncing) return;
            SettingsManager.Instance.SetFullscreen(value);
        }

        private void OnResolutionChanged(int index)
        {
            if (syncing) return;
            if (index < 0 || index >= resolutionOptions.Count) return;
            var res = resolutionOptions[index];
            SettingsManager.Instance.SetResolution(res.width, res.height);
        }

        private void OnQualityChanged(int index)
        {
            if (syncing) return;
            SettingsManager.Instance.SetQualityLevel(index);
        }

        // ── 헬퍼 ─────────────────────────────────────────────────

        private void UpdateVolumeLabel(float value)
        {
            if (masterVolumeValueText != null)
                masterVolumeValueText.text = $"{Mathf.RoundToInt(value * 100)}%";
        }

        private void UpdateSensitivityLabel(float value)
        {
            if (sensitivityValueText != null)
                sensitivityValueText.text = $"x{value:0.0}";
        }

        private void BuildResolutionOptions()
        {
            resolutionOptions.Clear();
            var labels = new List<string>();

            // 같은 해상도의 주사율 중복을 제거하고 내림차순으로 정렬한다.
            foreach (var res in Screen.resolutions)
            {
                bool exists = resolutionOptions.Exists(r => r.width == res.width && r.height == res.height);
                if (!exists)
                    resolutionOptions.Add(res);
            }
            resolutionOptions.Sort((a, b) => (b.width * b.height).CompareTo(a.width * a.height));

            foreach (var res in resolutionOptions)
                labels.Add($"{res.width} x {res.height}");

            resolutionDropdown.ClearOptions();
            resolutionDropdown.AddOptions(labels);
        }

        private int FindResolutionIndex(int width, int height)
        {
            for (int i = 0; i < resolutionOptions.Count; i++)
            {
                if (resolutionOptions[i].width == width && resolutionOptions[i].height == height)
                    return i;
            }
            return 0;
        }
    }
}

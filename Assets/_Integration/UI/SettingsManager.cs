using UnityEngine;
using UnityEngine.SceneManagement;

namespace LucidDream.UI
{
    /// <summary>
    /// 게임 설정값(볼륨·마우스 감도·해상도·전체화면·그래픽 품질)을
    /// PlayerPrefs에 저장/복원하고 실제 게임에 적용하는 싱글톤.
    ///
    /// - 어느 씬에서든 처음 접근하는 순간 자동 생성된다 (수동 배치 불필요).
    /// - 마우스 감도는 씬이 로드될 때마다 PlayerController.SetLookSensitivityMultiplier()로 적용된다.
    /// - 볼륨은 AudioListener.volume(마스터)으로 적용한다.
    ///   BGM/SFX 분리가 필요해지면 AudioMixer 도입 후 ApplyVolume()만 고치면 된다.
    /// </summary>
    public class SettingsManager : MonoBehaviour
    {
        private const string KeyMasterVolume = "Settings.MasterVolume";
        private const string KeySensitivity = "Settings.MouseSensitivity";
        private const string KeyFullscreen = "Settings.Fullscreen";
        private const string KeyResolutionWidth = "Settings.ResWidth";
        private const string KeyResolutionHeight = "Settings.ResHeight";
        private const string KeyQuality = "Settings.Quality";

        private static SettingsManager instance;

        /// <summary>설정 패널을 열지 않아도 저장된 설정이 게임에 적용되도록, 게임 시작 시 자동 생성한다.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            _ = Instance;
        }

        public static SettingsManager Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("SettingsManager");
                    instance = go.AddComponent<SettingsManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        // ── 설정값 (0~1 또는 배수) ────────────────────────────────

        public float MasterVolume { get; private set; } = 1f;
        public float MouseSensitivity { get; private set; } = 1f; // PlayerController 기본 감도에 곱해지는 배수
        public bool Fullscreen { get; private set; } = true;
        public int ResolutionWidth { get; private set; }
        public int ResolutionHeight { get; private set; }
        public int QualityLevel { get; private set; }

        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            DontDestroyOnLoad(gameObject);

            Load();
            ApplyAll();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (instance == this)
                SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // 새 씬의 PlayerController에 저장된 감도를 다시 적용한다.
            ApplySensitivity();
        }

        // ── 저장 / 불러오기 ──────────────────────────────────────

        private void Load()
        {
            MasterVolume = PlayerPrefs.GetFloat(KeyMasterVolume, 1f);
            MouseSensitivity = PlayerPrefs.GetFloat(KeySensitivity, 1f);
            Fullscreen = PlayerPrefs.GetInt(KeyFullscreen, 1) == 1;
            ResolutionWidth = PlayerPrefs.GetInt(KeyResolutionWidth, Screen.currentResolution.width);
            ResolutionHeight = PlayerPrefs.GetInt(KeyResolutionHeight, Screen.currentResolution.height);
            QualityLevel = PlayerPrefs.GetInt(KeyQuality, QualitySettings.GetQualityLevel());
        }

        private void Save()
        {
            PlayerPrefs.SetFloat(KeyMasterVolume, MasterVolume);
            PlayerPrefs.SetFloat(KeySensitivity, MouseSensitivity);
            PlayerPrefs.SetInt(KeyFullscreen, Fullscreen ? 1 : 0);
            PlayerPrefs.SetInt(KeyResolutionWidth, ResolutionWidth);
            PlayerPrefs.SetInt(KeyResolutionHeight, ResolutionHeight);
            PlayerPrefs.SetInt(KeyQuality, QualityLevel);
            PlayerPrefs.Save();
        }

        // ── Setter (설정 UI에서 호출) ────────────────────────────

        public void SetMasterVolume(float volume01)
        {
            MasterVolume = Mathf.Clamp01(volume01);
            ApplyVolume();
            Save();
        }

        public void SetMouseSensitivity(float multiplier)
        {
            MouseSensitivity = Mathf.Clamp(multiplier, 0.1f, 3f);
            ApplySensitivity();
            Save();
        }

        public void SetFullscreen(bool fullscreen)
        {
            Fullscreen = fullscreen;
            ApplyResolution();
            Save();
        }

        public void SetResolution(int width, int height)
        {
            ResolutionWidth = width;
            ResolutionHeight = height;
            ApplyResolution();
            Save();
        }

        public void SetQualityLevel(int level)
        {
            QualityLevel = Mathf.Clamp(level, 0, QualitySettings.names.Length - 1);
            ApplyQuality();
            Save();
        }

        // ── 적용 ─────────────────────────────────────────────────

        public void ApplyAll()
        {
            ApplyVolume();
            ApplySensitivity();
            ApplyResolution();
            ApplyQuality();
        }

        private void ApplyVolume()
        {
            AudioListener.volume = MasterVolume;
        }

        private void ApplySensitivity()
        {
            var player = FindAnyObjectByType<PlayerController>();
            if (player != null)
                player.SetLookSensitivityMultiplier(MouseSensitivity);
        }

        private void ApplyResolution()
        {
            if (ResolutionWidth <= 0 || ResolutionHeight <= 0) return;
            Screen.SetResolution(ResolutionWidth, ResolutionHeight, Fullscreen);
        }

        private void ApplyQuality()
        {
            QualitySettings.SetQualityLevel(QualityLevel, true);
        }
    }
}

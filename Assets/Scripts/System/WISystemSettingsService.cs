using System;
using ProjectWI.Administration;
using UnityEngine;

namespace ProjectWI.Systems
{
    [Serializable]
    public class WISystemSettingsState
    {
        public int Version = 1;
        public float MasterVolume = 1f;
        public float MusicVolume = 0.8f;
        public float SfxVolume = 0.9f;
        public bool Fullscreen = true;
        public int TargetFrameRate = 60;
        public bool UseEnglish;
    }

    public class WISystemSettingsService : MonoBehaviour
    {
        private const string PlayerPrefsKey = "ProjectWI_SystemSettings_V1";

        [SerializeField] private WISystemSettingsConfigSO config;
        [SerializeField] private WIAdministrationDatabaseSO administrationDatabase;

        public static WISystemSettingsService Instance { get; private set; }
        public WISystemSettingsState Settings { get; private set; }

        // 단일 설정 서비스를 유지하고 로컬 설정을 불러와 즉시 적용합니다.
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
            Apply();
        }

        // 서비스 제거 시 폐기된 정적 참조를 정리합니다.
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // 현재 설정을 로컬 PlayerPrefs JSON에 저장합니다.
        public void Save()
        {
            PlayerPrefs.SetString(PlayerPrefsKey, Serialize(Settings));
            PlayerPrefs.Save();
        }

        // 로컬 설정을 불러오고 없거나 손상되었으면 ScriptableObject 기본값을 사용합니다.
        public void Load()
        {
            Settings = CreateDefaults();
            if (PlayerPrefs.HasKey(PlayerPrefsKey) == false)
            {
                return;
            }
            try
            {
                if (TryDeserialize(PlayerPrefs.GetString(PlayerPrefsKey), out WISystemSettingsState loaded))
                {
                    Settings = loaded;
                    Normalize();
                }
            }
            catch (Exception)
            {
                Settings = CreateDefaults();
            }
        }

        // 시스템 설정 상태를 로컬 저장용 JSON 문자열로 변환합니다.
        public static string Serialize(WISystemSettingsState settings)
        {
            return settings == null ? string.Empty : JsonUtility.ToJson(settings);
        }

        // 시스템 설정 JSON을 검사해 설정 상태로 변환합니다.
        public static bool TryDeserialize(string json, out WISystemSettingsState settings)
        {
            settings = null;
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }
            try
            {
                settings = JsonUtility.FromJson<WISystemSettingsState>(json);
                return settings != null && settings.Version <= 1;
            }
            catch (Exception)
            {
                settings = null;
                return false;
            }
        }

        // 오디오, 전체 화면, 목표 프레임과 언어 설정을 현재 실행 환경에 적용합니다.
        public void Apply()
        {
            Normalize();
            AudioListener.volume = Settings.MasterVolume;
            Screen.fullScreen = Settings.Fullscreen;
            Application.targetFrameRate = Settings.TargetFrameRate;
            administrationDatabase?.SetUseEnglish(Settings.UseEnglish);
        }

        // 설정을 기본값으로 되돌리고 적용한 뒤 저장합니다.
        public void ResetToDefaults()
        {
            Settings = CreateDefaults();
            Apply();
            Save();
        }

        // ScriptableObject에 정의된 새 설정 상태를 생성합니다.
        private WISystemSettingsState CreateDefaults()
        {
            return new WISystemSettingsState
            {
                MasterVolume = config == null ? 1f : config.DefaultMasterVolume,
                MusicVolume = config == null ? 0.8f : config.DefaultMusicVolume,
                SfxVolume = config == null ? 0.9f : config.DefaultSfxVolume,
                Fullscreen = config == null || config.DefaultFullscreen,
                TargetFrameRate = config == null ? 60 : config.DefaultTargetFrameRate,
                UseEnglish = config != null && config.DefaultUseEnglish
            };
        }

        // 설정값을 지원 범위로 제한해 손상된 로컬 값을 방지합니다.
        private void Normalize()
        {
            Settings.MasterVolume = Mathf.Clamp01(Settings.MasterVolume);
            Settings.MusicVolume = Mathf.Clamp01(Settings.MusicVolume);
            Settings.SfxVolume = Mathf.Clamp01(Settings.SfxVolume);
            Settings.TargetFrameRate = Mathf.Clamp(Settings.TargetFrameRate, 30, 120);
        }
    }
}

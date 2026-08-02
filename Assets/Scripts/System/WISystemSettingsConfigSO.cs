using UnityEngine;

namespace ProjectWI.Systems
{
    [CreateAssetMenu(fileName = "WI_SystemSettingsConfig", menuName = "WI/System/Settings Config")]
    public class WISystemSettingsConfigSO : ScriptableObject
    {
        [SerializeField, Range(0f, 1f)] private float defaultMasterVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float defaultMusicVolume = 0.8f;
        [SerializeField, Range(0f, 1f)] private float defaultSfxVolume = 0.9f;
        [SerializeField] private bool defaultFullscreen = true;
        [SerializeField] private int defaultTargetFrameRate = 60;
        [SerializeField] private bool defaultUseEnglish;

        public float DefaultMasterVolume => defaultMasterVolume;
        public float DefaultMusicVolume => defaultMusicVolume;
        public float DefaultSfxVolume => defaultSfxVolume;
        public bool DefaultFullscreen => defaultFullscreen;
        public int DefaultTargetFrameRate => defaultTargetFrameRate;
        public bool DefaultUseEnglish => defaultUseEnglish;
    }
}

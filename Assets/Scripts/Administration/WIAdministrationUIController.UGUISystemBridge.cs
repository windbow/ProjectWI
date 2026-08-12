using ProjectWI.Systems;
using UnityEngine;

namespace ProjectWI.Administration
{
    public partial class WIAdministrationUIController
    {
        // 현재 시스템 설정과 저장 슬롯 상태를 UGUI 표시용 스냅샷으로 반환합니다.
        public bool TryGetUGUISystemSnapshot(out WIAdministrationSystemSnapshot snapshot)
        {
            snapshot = null;
            WISystemSettingsService settingsService = WISystemSettingsService.Instance;
            WICampaignRuntimeService campaignService = WICampaignRuntimeService.Instance;
            if (settingsService == null || campaignService == null) return false;
            WISystemSettingsState settings = settingsService.Settings;
            snapshot = new WIAdministrationSystemSnapshot
            {
                AutoSaveEnabled = campaignService.AutoSaveEnabled,
                UseEnglish = settings.UseEnglish,
                Fullscreen = settings.Fullscreen,
                MasterVolume = settings.MasterVolume,
                MusicVolume = settings.MusicVolume,
                SfxVolume = settings.SfxVolume,
                TargetFrameRate = settings.TargetFrameRate,
                HasAutoSave = campaignService.HasSave(0),
                HasSave1 = campaignService.HasSave(1),
                HasSave2 = campaignService.HasSave(2),
                HasSave3 = campaignService.HasSave(3)
            };
            return true;
        }

        // 지정한 설정 카드를 토글하거나 단계 값으로 순환시킵니다.
        public void ChangeUGUISystemSetting(int index)
        {
            WISystemSettingsState settings = WISystemSettingsService.Instance?.Settings;
            if (settings == null) return;
            if (index == 0) settings.UseEnglish = settings.UseEnglish == false;
            else if (index == 1) settings.Fullscreen = settings.Fullscreen == false;
            else if (index == 2) settings.MasterVolume = NextVolume(settings.MasterVolume);
            else if (index == 3) settings.MusicVolume = NextVolume(settings.MusicVolume);
            else if (index == 4) settings.SfxVolume = NextVolume(settings.SfxVolume);
            else if (index == 5) settings.TargetFrameRate = settings.TargetFrameRate >= 120 ? 30 : settings.TargetFrameRate == 30 ? 60 : 120;
        }

        // 변경한 시스템 설정을 실행 환경과 로컬 저장소에 반영합니다.
        public void ApplyUGUISystemSettings()
        {
            WISystemSettingsService service = WISystemSettingsService.Instance;
            if (service == null) return;
            service.Apply();
            service.Save();
            RefreshAll();
            ShowUGUIMessage("안내", database.UseEnglish ? "Settings applied." : "설정을 적용했습니다.");
        }

        // 시스템 설정을 ScriptableObject 기본값으로 복원합니다.
        public void ResetUGUISystemSettings()
        {
            WISystemSettingsService.Instance?.ResetToDefaults();
            RefreshAll();
            ShowUGUIMessage("안내", "설정을 기본값으로 되돌렸습니다.");
        }

        // 현재 캠페인을 지정한 수동 슬롯에 저장합니다.
        public void SaveUGUICampaignSlot(int slot)
        {
            WICampaignRuntimeService service = WICampaignRuntimeService.Instance;
            string error = "캠페인 런타임 서비스를 찾을 수 없습니다.";
            bool saved = service != null && service.SaveSlot(slot, out error);
            ShowUGUIMessage("저장", saved ? $"슬롯 {slot}에 저장했습니다." : error);
        }

        // 지정한 슬롯을 불러오고 월드 화면의 상태 참조를 교체합니다.
        public void LoadUGUICampaignSlot(int slot)
        {
            WICampaignRuntimeService service = WICampaignRuntimeService.Instance;
            string error = "캠페인 런타임 서비스를 찾을 수 없습니다.";
            if (service == null || service.LoadSlot(slot, out error) == false)
            {
                ShowUGUIMessage("불러오기", error);
                return;
            }
            state = service.State;
            selectedCastle = null;
            SelectInitialCastle();
            RefreshAll();
            string message = slot == 0 ? "자동 저장을 불러왔습니다." : $"슬롯 {slot}을 불러왔습니다.";
            ShowUGUIMessage("불러오기", string.IsNullOrEmpty(error) ? message : $"{message}\n{error}");
        }

        // 시스템 UGUI를 열도록 구독 화면에 요청합니다.
        public void OpenUGUISystem()
        {
            UGUISystemRequested?.Invoke();
        }

        // 음량을 25퍼센트 단위로 순환시킵니다.
        private static float NextVolume(float value)
        {
            return value >= 0.99f ? 0f : Mathf.Clamp01(Mathf.Round(value * 4f + 1f) / 4f);
        }
    }

    public sealed class WIAdministrationSystemSnapshot
    {
        public bool AutoSaveEnabled;
        public bool UseEnglish;
        public bool Fullscreen;
        public float MasterVolume;
        public float MusicVolume;
        public float SfxVolume;
        public int TargetFrameRate;
        public bool HasAutoSave;
        public bool HasSave1;
        public bool HasSave2;
        public bool HasSave3;
    }
}

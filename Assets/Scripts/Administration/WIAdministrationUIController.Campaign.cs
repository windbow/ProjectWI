using ProjectWI.Systems;

namespace ProjectWI.Administration
{
    public partial class WIAdministrationUIController
    {
        // 지정 난이도로 새 캠페인을 시작해 시작 화면을 닫습니다.
        public void BeginCampaign(WICampaignDifficulty difficulty,
            WICampaignVariant variant = WICampaignVariant.Classic)
        {
            WICampaignRuntimeService service = WICampaignRuntimeService.Instance;
            state = service == null
                ? WIAdministrationState.Create(database, difficulty, variant)
                : service.StartNewCampaign(difficulty, variant);
            RebuildCampaignView();
            OpenUGUICampaignObjective(true);
        }

        // 화면별 QA에서는 캠페인 후속 목표를 열지 않고 검증용 상태만 시작합니다.
        public void BeginCampaignForQA(WICampaignDifficulty difficulty,
            WICampaignVariant variant = WICampaignVariant.Classic)
        {
            WICampaignRuntimeService service = WICampaignRuntimeService.Instance;
            state = service == null
                ? WIAdministrationState.Create(database, difficulty, variant)
                : service.StartNewCampaign(difficulty, variant);
            RebuildCampaignView();
        }

        // 자동 저장 슬롯을 불러와 캠페인 화면으로 진입합니다.
        private void ContinueAutoSave()
        {
            WICampaignRuntimeService service = WICampaignRuntimeService.Instance;
            string error = string.Empty;
            if (service == null || service.LoadSlot(0, out error) == false)
            {
                ShowUGUIMessage("자동 저장 불러오기", string.IsNullOrEmpty(error) ? "자동 저장을 불러올 수 없습니다." : error);
                return;
            }
            state = service.State;
            RebuildCampaignView();
            if (string.IsNullOrEmpty(error) == false)
            {
                ShowUGUIMessage("자동 저장 불러오기", error);
                return;
            }
            ShowCurrentUGUICampaignResultOrTutorial();
        }

        // UGUI 타이틀 화면에서 기존 자동 저장 불러오기 흐름을 실행합니다.
        public void ContinueCampaignFromAutoSave()
        {
            ContinueAutoSave();
        }

        // 교체된 캠페인 상태를 지도·HUD·초기 선택 성에 다시 연결합니다.
        private void RebuildCampaignView()
        {
            selectedCastle = null;
            RefreshAll();
            SelectInitialCastle();
        }

    }
}

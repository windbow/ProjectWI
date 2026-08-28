namespace ProjectWI.Administration
{
    public partial class WIAdministrationUIController
    {
        // 비용을 지불하고 선택한 성에 월간 중점 사업을 지정합니다.
        private void AssignCastleProject(
            WICastleProjectType projectType,
            WIProjectInvestment investment,
            WIHeroDefinition manager)
        {
            if (WIAdministrationTurnSystem.IsAdministrationCapable(state, manager.Id) == false)
            {
                ShowMessage("영웅 등급의 인물만 내정 사업을 담당할 수 있습니다.");
                return;
            }

            if (projectType == WICastleProjectType.Expansion && CanStartExpansion(selectedCastle) == false)
            {
                ShowMessage("확장에는 성 규모에 맞는 번영과 기술이 필요하며 대형 성은 더 확장할 수 없습니다.");
                return;
            }

            int cost = WIAdministrationTurnSystem.GetProjectCost(database, projectType, investment);
            if (state.Gold < cost)
            {
                ShowMessage("UI_NOT_ENOUGH_RESOURCE");
                return;
            }

            state.Gold -= cost;
            state.PendingPlayerGoldSpent += cost;
            selectedCastle.ActiveProject = new WICastleProjectState
            {
                ProjectType = projectType,
                Investment = investment,
                ManagerHeroId = manager.Id,
                RemainingMonths = projectType == WICastleProjectType.Expansion
                    ? database.ProjectBalance.ExpansionDurationMonths
                    : 1
            };
            WITutorialSystem.Complete(state, "tutorial_project");
            CloseModal();
            SelectCastle(selectedCastle.CastleId);
            RefreshAll();
        }

        // 현재 성이 규모 확장 사업의 수치 조건을 만족하는지 확인합니다.
        private bool CanStartExpansion(WICastleRuntimeState castleState)
        {
            if (castleState.CastleSize == WICastleSize.Large || castleState.PendingSpecialFacilityChoice)
            {
                return false;
            }

            int requiredValue = castleState.CastleSize == WICastleSize.Small ? 50 : 70;
            return castleState.Prosperity >= requiredValue && castleState.Technology >= requiredValue;
        }

        // 사업 열거형을 UI에 표시할 한국어 이름으로 변환합니다.
        private string GetProjectDisplayName(WICastleProjectType projectType)
        {
            switch (projectType)
            {
                case WICastleProjectType.Prosperity:
                    return "번영 사업";
                case WICastleProjectType.Technology:
                    return "기술 사업";
                case WICastleProjectType.Stability:
                    return "안정 사업";
                case WICastleProjectType.Fortification:
                    return "요새 사업";
                case WICastleProjectType.Recruitment:
                    return "인재 사업";
                case WICastleProjectType.Training:
                    return "훈련 사업";
                case WICastleProjectType.Recovery:
                    return "회복 사업";
                case WICastleProjectType.Expansion:
                    return "확장 사업";
                default:
                    return projectType.ToString();
            }
        }

        // 투자 열거형을 UI에 표시할 한국어 이름으로 변환합니다.
        private string GetInvestmentDisplayName(WIProjectInvestment investment)
        {
            return investment == WIProjectInvestment.Intensive ? "집중 투자" : "기본 투자";
        }

        // 현재 선택한 성의 플레이어 소유권을 확인하고 잘못된 명령 진입을 차단합니다.
        private bool EnsureSelectedCastleManageable()
        {
            if (WIAdministrationTurnSystem.CanPlayerManageCastle(state, selectedCastle))
            {
                return true;
            }

            ShowMessage("다른 진영의 성은 정보를 열람할 수 있지만 영지 관리 명령은 내릴 수 없습니다.");
            return false;
        }
    }
}

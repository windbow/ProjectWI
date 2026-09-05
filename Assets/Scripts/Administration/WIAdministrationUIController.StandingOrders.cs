namespace ProjectWI.Administration
{
    public partial class WIAdministrationUIController
    {
        // 임무를 점유하지 않는 다음 달 사업 지시 사본을 만듭니다.
        private static WICastleProjectState CopyProjectOrder(WICastleProjectState project)
        {
            if (project == null || project.ProjectType == WICastleProjectType.Expansion)
            {
                return null;
            }
            return new WICastleProjectState
            {
                ProjectType = project.ProjectType, Investment = project.Investment,
                ManagerHeroId = project.ManagerHeroId, RemainingMonths = 1
            };
        }

        // 기존 화면에 저장된 유지 지시 버튼의 표시 문자열을 반환합니다.
        public string GetStandingOrderText(bool activity, string heroId)
        {
            bool enabled = activity ? state?.GetCharacter(heroId)?.RepeatActivity == true : selectedCastle?.RepeatProject == true;
            return database.GetText(enabled ? "UI_ORDER_REPEAT_ON" : "UI_ORDER_REPEAT_OFF");
        }

        // 사업 또는 개인 활동 유지 설정을 전환하고 해제 시 다음 달 예약만 제거합니다.
        public void ToggleStandingOrder(bool activity, string heroId)
        {
            if (WIAdministrationTurnSystem.CanPlayerManageCastle(state, selectedCastle) == false)
            {
                return;
            }
            if (activity)
            {
                WICharacterRuntimeState character = state.GetCharacter(heroId);
                if (character == null || selectedCastle.HeroIds.Contains(heroId) == false)
                {
                    return;
                }
                character.RepeatActivity = character.RepeatActivity == false;
                if (character.RepeatActivity == false)
                {
                    character.StandingActivity = WICharacterActivityType.None;
                    character.AutomaticRecovery = false;
                }
            }
            else
            {
                selectedCastle.RepeatProject = selectedCastle.RepeatProject == false;
                selectedCastle.StandingProject = selectedCastle.RepeatProject
                    ? CopyProjectOrder(selectedCastle.ActiveProject) ?? selectedCastle.StandingProject : null;
            }
            RefreshAll();
        }

        // 선택 인물의 현재 내정 특성과 활동 종류별 자격을 UI에서 확인합니다.
        public bool CanSelectCharacterActivity(string heroId, WICharacterActivityType activity)
        {
            return WIAdministrationTurnSystem.CanPerformCharacterActivity(
                state, heroId, activity, database.Automation.TalentOfficeCapacity);
        }

        // 사업 선택에서 다음 전투 준비에 주는 이득을 짧게 표시합니다.
        private string GetProjectCombatHint(WICastleProjectType project)
        {
            string uid;
            switch (project)
            {
                case WICastleProjectType.Prosperity: uid = "UI_ADMIN_COMBAT_PROSPERITY"; break;
                case WICastleProjectType.Technology: uid = "UI_ADMIN_COMBAT_TECHNOLOGY"; break;
                case WICastleProjectType.Training: uid = "UI_ADMIN_COMBAT_TRAINING"; break;
                case WICastleProjectType.Recovery: uid = "UI_ADMIN_COMBAT_RECOVERY"; break;
                default: return string.Empty;
            }
            return "\n<size=65%>" + database.GetText(uid) + "</size>";
        }

        // 새 고정 UI의 문구를 데이터베이스 문자열 UID로 제공합니다.
        public string GetAdministrationText(string uid) => database.GetText(uid);
    }
}

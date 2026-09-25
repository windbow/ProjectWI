namespace ProjectWI.Administration
{
    public partial class WIAdministrationState
    {
        // 구 저장의 Common 업무를 해제하고 지불한 성 사업은 기본 내정으로 보존합니다.
        public void NormalizeCharacterDuties()
        {
            foreach (WICastleRuntimeState castle in Castles)
            {
                if (IsCommonCharacter(castle.GovernorHeroId) == true)
                {
                    castle.GovernorHeroId = string.Empty;
                }
                if (castle.ActiveProject != null && IsCommonCharacter(castle.ActiveProject.ManagerHeroId) == true)
                {
                    castle.ActiveProject.ManagerHeroId = string.Empty;
                    castle.ActiveProject.AssistantHeroId = string.Empty;
                    castle.ActiveProject.Delegated = true;
                }
                if (castle.StandingProject != null && IsCommonCharacter(castle.StandingProject.ManagerHeroId) == true)
                {
                    castle.StandingProject = null;
                    castle.RepeatProject = false;
                    castle.DelegatedToGovernor = true;
                }
            }
            foreach (WIFactionRuntimeState faction in Factions)
            {
                if (IsCommonCharacter(faction.ResearcherHeroId) == true)
                {
                    // 지불한 연구와 남은 기간은 보존하고 영웅 담당자 배정을 기다립니다.
                    faction.ResearcherHeroId = string.Empty;
                }
            }
            SchemeMissions?.RemoveAll(mission => IsCommonCharacter(mission.AgentHeroId));
            PendingRecruitmentEvents.RemoveAll(item => IsCommonCharacter(item.RecruiterHeroId) || IsCommonCharacter(item.CandidateHeroId));
            foreach (WICharacterRuntimeState character in Characters)
            {
                bool common = character.BaseGrade == WICharacterGrade.Common && character.PromotedToHero == false;
                // 구 저장에서 일반 인물을 설득하던 지시는 해제하고 기존 고용 인원은 보존합니다.
                if (character.Activity == WICharacterActivityType.Recruit && IsCommonCharacter(character.ActivityTargetHeroId) == true)
                {
                    character.Activity = WICharacterActivityType.None;
                    character.ActivityTargetHeroId = string.Empty;
                }
                if (character.StandingActivity == WICharacterActivityType.Recruit && IsCommonCharacter(character.StandingActivityTargetHeroId) == true)
                {
                    character.StandingActivity = WICharacterActivityType.None;
                    character.StandingActivityTargetHeroId = string.Empty;
                    character.RepeatActivity = false;
                }
                // 폐지된 수동 활동은 해제하되 인재실 반복 지시의 자동 회복은 보존합니다.
                if (character.Activity == WICharacterActivityType.Training ||
                    character.Activity == WICharacterActivityType.Socialize ||
                    (character.Activity == WICharacterActivityType.Rest &&
                     WIAdministrationTurnSystem.IsTalentOfficeActivity(character.StandingActivity) == false) ||
                    (common == true && WIAdministrationTurnSystem.IsTalentOfficeActivity(character.Activity) == true))
                {
                    character.Activity = WICharacterActivityType.None;
                    character.ActivityTargetHeroId = string.Empty;
                }
                if (character.StandingActivity == WICharacterActivityType.Training ||
                    character.StandingActivity == WICharacterActivityType.Socialize ||
                    character.StandingActivity == WICharacterActivityType.Rest ||
                    (common == true && WIAdministrationTurnSystem.IsTalentOfficeActivity(character.StandingActivity) == true))
                {
                    character.StandingActivity = WICharacterActivityType.None;
                    character.StandingActivityTargetHeroId = string.Empty;
                    character.RepeatActivity = false;
                }
            }
        }

        // 승격하지 않은 일반 인물인지 런타임 등급으로 확인합니다.
        public bool IsCommonCharacter(string heroId)
        {
            WICharacterRuntimeState character = GetCharacter(heroId);
            return character != null && character.BaseGrade == WICharacterGrade.Common && character.PromotedToHero == false;
        }
    }
}

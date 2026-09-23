using System;
using System.Linq;
using UnityEngine;

namespace ProjectWI.Administration
{
    [Serializable]
    public class WIAdministrationAutomationDefinition
    {
        // 반복 활동의 회복 전환·복귀 기준과 훈련 종료 경험치를 데이터로 조정합니다.
        [SerializeField] private int restStartFatigue = 70;
        [SerializeField] private int restEndFatigue = 20;
        [SerializeField] private int trainingTargetExperience = 500;
        [SerializeField] private int talentOfficeCapacity = 2;
        // 주둔 인물의 자동 훈련 사용 여부와 월간 경험·피로 증가량입니다.
        [SerializeField] private bool automaticTrainingEnabled = true;
        [SerializeField, Min(0)] private int automaticTrainingExperience = 8;
        [SerializeField, Min(0)] private int automaticTrainingFatigue = 5;
        public bool AutomaticTrainingEnabled => automaticTrainingEnabled;
        public int AutomaticTrainingExperience => Mathf.Max(0, automaticTrainingExperience);
        public int AutomaticTrainingFatigue => Mathf.Max(0, automaticTrainingFatigue);
        // 점령 직후 불안 기간, 월간 자연 안정량과 승리 후 의무 재편 기간입니다.
        [SerializeField, Min(0)] private int occupationUnrestMonths = 3;
        [SerializeField, Min(0)] private int occupationStabilityGain = 5;
        [SerializeField, Min(0)] private int victoryReorganizationMonths = 0;
        public int OccupationUnrestMonths => Mathf.Max(0, occupationUnrestMonths);
        public int OccupationStabilityGain => Mathf.Max(0, occupationStabilityGain);
        public int VictoryReorganizationMonths => Mathf.Max(0, victoryReorganizationMonths);
        public int RestStartFatigue => restStartFatigue;
        public int RestEndFatigue => restEndFatigue;
        public int TrainingTargetExperience => trainingTargetExperience;
        public int TalentOfficeCapacity => Mathf.Max(1, talentOfficeCapacity);
    }

    public static partial class WIAdministrationTurnSystem
    {
        // 다음 달 유지 지시를 재검증하고 수동 명령·원정이 없는 인물에게만 예약합니다.
        private static void PrepareStandingOrders(WIAdministrationDatabaseSO database,
            WIAdministrationState state, WITurnSummary summary)
        {
            foreach (WICastleRuntimeState castle in state.Castles.Where(item => item.FactionId == state.PlayerFactionId))
            {
                if (castle.PendingGovernorAppointment)
                {
                    // 구 저장의 임명 대기도 인원 도착을 기다리지 않고 기본 운영으로 전환합니다.
                    castle.DelegatedToGovernor = true;
                    castle.PendingGovernorAppointment = false;
                    castle.GovernorPolicy = WIGovernorPolicy.Frontline;
                    string candidate = castle.HeroIds.Where(id => IsAdministrationCapable(state, id))
                        .Where(id => state.IsCharacterBusy(id, ignoreArmy: true) == false)
                        .OrderByDescending(id => database.GetHero(id).Politics).FirstOrDefault();
                    if (string.IsNullOrEmpty(candidate) == false && AssignGovernor(state, castle.CastleId, candidate))
                    {
                        castle.DelegatedToGovernor = true;
                        castle.GovernorPolicy = WIGovernorPolicy.Frontline;
                        castle.PendingGovernorAppointment = false;
                        summary.News.Add(string.Format(database.GetText("UI_ADMIN_APPOINTED"),
                            database.GetCastle(castle.CastleId).DisplayName.Get(database.UseEnglish),
                            database.GetHero(candidate).DisplayName.Get(database.UseEnglish)));
                    }
                }
                WICastleProjectState order = castle.StandingProject;
                if (castle.RepeatProject == false || order == null || castle.ActiveProject != null || castle.DelegatedToGovernor)
                {
                    continue;
                }
                if (order.ProjectType == WICastleProjectType.Expansion ||
                    IsHeroInFaction(state, order.ManagerHeroId, castle.FactionId) == false ||
                    IsAdministrationCapable(state, order.ManagerHeroId) == false ||
                    HasUsefulProjectWork(state, castle, order.ProjectType) == false)
                {
                    castle.StandingProject = null;
                    continue;
                }
                int cost = GetProjectCost(database, order.ProjectType, order.Investment);
                if (state.IsCharacterBusy(order.ManagerHeroId, ignoreArmy: true) == true || state.Gold < cost)
                {
                    continue;
                }
                state.Gold -= cost;
                summary.GoldSpent += cost;
                castle.ActiveProject = new WICastleProjectState
                {
                    ProjectType = order.ProjectType, Investment = order.Investment,
                    ManagerHeroId = order.ManagerHeroId, RemainingMonths = 1, GoldCost = cost
                };
            }
            foreach (WICharacterRuntimeState character in state.Characters)
            {
                if (character.RepeatActivity == false || character.StandingActivity == WICharacterActivityType.None ||
                    character.Recruited == false || character.IsDead || character.Captured || state.IsCharacterBusy(character.HeroId))
                {
                    continue;
                }
                WICastleRuntimeState home = state.Castles.FirstOrDefault(castle =>
                    castle.FactionId == state.PlayerFactionId && castle.HeroIds.Contains(character.HeroId));
                if (home == null || (home.DelegatedToGovernor && home.GovernorHeroId == character.HeroId))
                {
                    continue;
                }
                WICharacterActivityType activity = character.StandingActivity;
                WICharacterRuntimeState target = state.GetCharacter(character.StandingActivityTargetHeroId);
                bool finished = (activity == WICharacterActivityType.Recruit &&
                    (target == null || target.Recruited || target.IsDead || target.Captured)) ||
                    (activity == WICharacterActivityType.Socialize &&
                    (target == null || target.IsDead || target.Captured || home.HeroIds.Contains(target.HeroId) == false)) ||
                    (activity == WICharacterActivityType.Training && character.Experience >= database.Automation.TrainingTargetExperience) ||
                    (activity == WICharacterActivityType.Rest && character.Fatigue == 0 && character.InjuryMonths == 0);
                if (finished || CanPerformCharacterActivity(state, character.HeroId, activity,
                    database.Automation.TalentOfficeCapacity) == false)
                {
                    character.StandingActivity = WICharacterActivityType.None;
                    character.RepeatActivity = false;
                    continue;
                }
                if (character.Fatigue >= database.Automation.RestStartFatigue || character.InjuryMonths > 0)
                {
                    character.AutomaticRecovery = true;
                }
                if (character.Fatigue <= database.Automation.RestEndFatigue && character.InjuryMonths == 0)
                {
                    character.AutomaticRecovery = false;
                }
                character.Activity = character.AutomaticRecovery ? WICharacterActivityType.Rest : activity;
                character.ActivityTargetHeroId = character.AutomaticRecovery ? string.Empty : character.StandingActivityTargetHeroId;
            }
        }

        // 이미 최대치인 사업이나 대상이 없는 회복·훈련에는 예산을 사용하지 않습니다.
        public static bool HasUsefulProjectWork(WIAdministrationState state, WICastleRuntimeState castle,
            WICastleProjectType project)
        {
            switch (project)
            {
                case WICastleProjectType.Prosperity: return castle.Prosperity < 100;
                case WICastleProjectType.Technology: return castle.Technology < 100;
                case WICastleProjectType.Stability: return castle.Stability < 100;
                case WICastleProjectType.Fortification: return castle.Defense < 100;
                case WICastleProjectType.Recovery:
                    return castle.HeroIds.Any(id => state.GetCharacter(id)?.Fatigue > 0 || state.GetCharacter(id)?.InjuryMonths > 0);
                case WICastleProjectType.Training:
                    return state.Armies.Any(army => army.CurrentCastleId == castle.CastleId && army.IsMoving == false);
                default: return true;
            }
        }
    }
}

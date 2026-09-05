using System.Linq;
using UnityEngine;

namespace ProjectWI.Administration
{
    public static partial class WIAdministrationTurnSystem
    {
        // 주둔 인물을 대장으로 지정해 새 전투단을 생성합니다.
        public static WIArmyState CreateArmy(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WICastleRuntimeState castle,
            string commanderHeroId)
        {
            if (castle == null || castle.HeroIds.Contains(commanderHeroId) == false || state.IsCharacterBusy(commanderHeroId))
            {
                return null;
            }

            WIArmyState army = new WIArmyState
            {
                ArmyId = $"army_{state.NextArmyNumber}",
                DisplayName = $"제 {state.NextArmyNumber} 전투단",
                FactionId = castle.FactionId,
                CurrentCastleId = castle.CastleId
            };
            army.Members.Add(new WIArmyMemberState { HeroId = commanderHeroId, Role = WIUnitRole.Commander });
            state.NextArmyNumber += 1;
            state.Armies.Add(army);
            return army;
        }

        // 같은 성의 대기 인물을 지정된 역할로 전투단에 추가합니다.
        public static bool AddArmyMember(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WIArmyState army,
            string heroId,
            WIUnitRole role)
        {
            WICastleRuntimeState castle = state.GetCastle(army.CurrentCastleId);
            if (army.IsMoving || army.AwaitingBattle || army.ReorganizationMonths > 0 || castle == null ||
                castle.HeroIds.Contains(heroId) == false || state.IsCharacterBusy(heroId))
            {
                return false;
            }

            if (army.Members.Count >= GetRecommendedArmySize(database, army))
            {
                return false;
            }

            army.Members.Add(new WIArmyMemberState { HeroId = heroId, Role = role });
            return true;
        }

        // 대장이 아닌 전투단원을 제외하고 편성 변경에 따른 합동 경험을 감소시킵니다.
        public static bool RemoveArmyMember(
            WIAdministrationState state,
            WIArmyState army,
            string heroId)
        {
            if (army == null || army.IsMoving || army.AwaitingBattle || army.ReorganizationMonths > 0 || army.JointTrainingScheduled)
            {
                return false;
            }

            WIArmyMemberState member = army.Members.Find(item => item.HeroId == heroId);
            if (member == null || member.Role == WIUnitRole.Commander)
            {
                return false;
            }

            army.Members.Remove(member);
            army.CohesionExperience = Mathf.Max(0, army.CohesionExperience - 20);
            UpdateArmyProficiency(army);
            WICastleRuntimeState castle = state.GetCastle(army.CurrentCastleId);
            if (castle != null && castle.HeroIds.Contains(heroId) == false)
            {
                castle.HeroIds.Add(heroId);
            }

            return true;
        }

        // 주둔 중인 전투단을 해산하고 모든 구성원을 현재 성으로 복귀시킵니다.
        public static bool DisbandArmy(WIAdministrationState state, WIArmyState army)
        {
            if (army == null || army.IsMoving || army.AwaitingBattle || army.ReorganizationMonths > 0)
            {
                return false;
            }

            WICastleRuntimeState castle = state.GetCastle(army.CurrentCastleId);
            if (castle == null)
            {
                return false;
            }

            foreach (WIArmyMemberState member in army.Members)
            {
                if (castle.HeroIds.Contains(member.HeroId) == false)
                {
                    castle.HeroIds.Add(member.HeroId);
                }
            }

            state.Armies.Remove(army);
            return true;
        }

        // 주둔 중인 전투단에 다음 달 합동 훈련을 예약합니다.
        public static bool ScheduleJointTraining(WIArmyState army)
        {
            if (army == null || army.IsMoving || army.AwaitingBattle || army.ReorganizationMonths > 0 || army.JointTrainingScheduled)
            {
                return false;
            }

            army.JointTrainingScheduled = true;
            return true;
        }

        // 대장의 통솔에 따른 현재 전투단의 권장 인원 상한을 반환합니다.
        public static int GetRecommendedArmySize(WIAdministrationDatabaseSO database, WIArmyState army)
        {
            WIArmyMemberState commander = army.Members.Find(member => member.Role == WIUnitRole.Commander);
            WIHeroDefinition hero = commander == null ? null : database.GetHero(commander.HeroId);
            return hero == null ? 1 : Mathf.Clamp(3 + hero.Leadership / 20, 4, 8);
        }

        // 인접 성을 목표로 월간 이동을 시작합니다.
        public static bool BeginArmyMarch(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WIArmyState army,
            string targetCastleId)
        {
            if (army == null || army.IsMoving || army.AwaitingBattle || army.ReorganizationMonths > 0)
            {
                return false;
            }

            WICastleRuntimeState origin = state.GetCastle(army.CurrentCastleId);
            if (origin == null || origin.AdjacentCastleIds.Contains(targetCastleId) == false)
            {
                return false;
            }

            WICastleRuntimeState target = state.GetCastle(targetCastleId);
            WIFactionRuntimeState factionState = state.GetFactionState(army.FactionId);
            if (target == null || factionState == null)
            {
                return false;
            }

            if (target.FactionId != army.FactionId)
            {
                const int influenceCost = 20;
                if (AreFactionsAtWar(state, army.FactionId, target.FactionId) == false || factionState.Influence < influenceCost)
                {
                    return false;
                }
                factionState.Influence -= influenceCost;
            }

            army.Supply = GetArmySupplyState(origin);
            army.OriginCastleId = origin.CastleId;
            army.TargetCastleId = targetCastleId;
            army.RemainingTravelMonths = army.Supply == WISupplyState.Sufficient ? 1 : 2;
            foreach (WIArmyMemberState member in army.Members)
            {
                origin.HeroIds.Remove(member.HeroId);
                if (origin.GovernorHeroId == member.HeroId)
                {
                    origin.GovernorHeroId = string.Empty;
                    origin.DelegatedToGovernor = false;
                }
            }

            return true;
        }

        // 이동 중인 전투단의 남은 기간, 도착, 보급과 숙련을 처리합니다.
        private static void ResolveArmyMovement(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WITurnSummary summary)
        {
            foreach (WIArmyState army in state.Armies)
            {
                if (army.IsMoving == false)
                {
                    continue;
                }

                army.RemainingTravelMonths -= 1;
                if (army.RemainingTravelMonths > 0)
                {
                    summary.News.Add($"{army.DisplayName} 이동 중 · 남은 기간 {army.RemainingTravelMonths}개월");
                    continue;
                }

                WICastleRuntimeState target = state.GetCastle(army.TargetCastleId);
                army.CurrentCastleId = army.TargetCastleId;
                army.CohesionExperience += 10;
                UpdateArmyProficiency(army);
                ApplyMarchFatigue(state, army);

                if (target.FactionId == army.FactionId)
                {
                    foreach (WIArmyMemberState member in army.Members)
                    {
                        if (target.HeroIds.Contains(member.HeroId) == false)
                        {
                            target.HeroIds.Add(member.HeroId);
                        }
                    }

                    summary.News.Add($"{army.DisplayName} · {database.GetCastle(target.CastleId).DisplayName.Get(database.UseEnglish)} 도착");
                }
                else
                {
                    army.AwaitingBattle = true;
                    summary.News.Add($"{army.DisplayName} · {database.GetCastle(target.CastleId).DisplayName.Get(database.UseEnglish)} 성외 도착 · 전투 대기");
                }

                army.TargetCastleId = string.Empty;
            }
        }

        // 예약된 합동 훈련을 처리해 전투단 숙련과 구성원 경험을 높입니다.
        private static void ResolveArmyTraining(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WITurnSummary summary)
        {
            foreach (WIArmyState army in state.Armies)
            {
                if (army.JointTrainingScheduled == false)
                {
                    continue;
                }

                army.JointTrainingScheduled = false;
                if (army.IsMoving || army.AwaitingBattle)
                {
                    continue;
                }

                army.CohesionExperience += 20;
                foreach (WIArmyMemberState member in army.Members)
                {
                    WICharacterRuntimeState character = state.GetCharacter(member.HeroId);
                    character.Experience += 8;
                    character.Fatigue = Mathf.Clamp(character.Fatigue + 10, 0, 100);
                }

                UpdateArmyProficiency(army);
                summary.News.Add($"{army.DisplayName} · 합동 훈련 완료 · 숙련 {army.Proficiency}");
            }
        }

        // 합동 경험 수치에 따라 전투단 숙련 단계를 갱신합니다.
        private static void UpdateArmyProficiency(WIArmyState army)
        {
            army.Proficiency = army.CohesionExperience >= 80
                ? WIUnitProficiency.Elite
                : (army.CohesionExperience >= 30 ? WIUnitProficiency.Trained : WIUnitProficiency.Rookie);
        }
        // 출발 성의 번영 상태로 전투단 보급 상태를 판정합니다.
        private static WISupplyState GetArmySupplyState(WICastleRuntimeState origin)
        {
            if (origin.Prosperity >= 50)
            {
                return WISupplyState.Sufficient;
            }
            if (origin.Prosperity >= 25)
            {
                return WISupplyState.Shortage;
            }
            return WISupplyState.Depleted;
        }

        // 보급 상태에 따라 이동을 마친 전투단원의 피로를 증가시킵니다.
        private static void ApplyMarchFatigue(WIAdministrationState state, WIArmyState army)
        {
            int fatigue = army.Supply == WISupplyState.Sufficient ? 5 : (army.Supply == WISupplyState.Shortage ? 12 : 20);
            foreach (WIArmyMemberState member in army.Members)
            {
                WICharacterRuntimeState character = state.GetCharacter(member.HeroId);
                character.Fatigue = Mathf.Clamp(character.Fatigue + fatigue, 0, 100);
            }
        }

    }
}

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProjectWI.Administration
{
    public static partial class WIAdministrationTurnSystem
    {
        // 패배 전투단을 가까운 아군 성으로 후퇴시키고 한 달간 재편성 상태로 전환합니다.
        private static void RetreatAndReorganizeArmy(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WIArmyState army,
            string battleCastleId,
            int defeatMargin,
            bool orderlyRetreat,
            string captorFactionId,
            WITurnSummary summary)
        {
            string retreatId = string.Empty;
            if (string.IsNullOrEmpty(army.OriginCastleId) == false && state.GetCastle(army.OriginCastleId)?.FactionId == army.FactionId)
            {
                retreatId = army.OriginCastleId;
            }
            if (string.IsNullOrEmpty(retreatId))
            {
                retreatId = state.GetCastle(battleCastleId)?.AdjacentCastleIds
                    .FirstOrDefault(id => state.GetCastle(id)?.FactionId == army.FactionId);
            }
            if (string.IsNullOrEmpty(retreatId))
            {
                retreatId = state.Castles.FirstOrDefault(castle => castle.FactionId == army.FactionId)?.CastleId;
            }

            army.AwaitingBattle = false;
            army.RemainingTravelMonths = 0;
            army.TargetCastleId = string.Empty;
            army.CurrentCastleId = retreatId;
            army.Mission = WIArmyMission.Reserve;
            army.StrategicTargetCastleId = string.Empty;
            army.ReorganizationMonths = 1;
            WICastleRuntimeState battleCastleState = state.GetCastle(battleCastleId);
            WICastleRuntimeState retreatCastle = state.GetCastle(retreatId);
            foreach (WIArmyMemberState member in army.Members)
            {
                battleCastleState?.HeroIds.Remove(member.HeroId);
                if (retreatCastle != null && retreatCastle.HeroIds.Contains(member.HeroId) == false)
                {
                    retreatCastle.HeroIds.Add(member.HeroId);
                }
            }
            ApplyBattleConsequences(database, state, army, false, defeatMargin, orderlyRetreat, summary);
            ResolveDefeatedCharacterFate(database, state, army, defeatMargin, orderlyRetreat, captorFactionId, summary);
        }

        // 승패와 전력 차이에 따라 피로, 부상, 경험과 공훈을 반영합니다.
        private static void ApplyBattleConsequences(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WIArmyState army,
            bool victory,
            int margin,
            bool orderlyRetreat,
            WITurnSummary summary)
        {
            foreach (WIArmyMemberState member in army.Members)
            {
                WICharacterRuntimeState character = state.GetCharacter(member.HeroId);
                if (character == null)
                {
                    continue;
                }
                int fatigueGain = victory ? database.BattleVictoryFatigue
                    : orderlyRetreat ? database.OrderlyRetreatFatigue : database.BattleDefeatFatigue;
                int experienceGain = victory ? database.BattleVictoryExperience : database.BattleDefeatExperience;
                character.Fatigue = Mathf.Clamp(character.Fatigue + fatigueGain, 0, 100);
                character.Experience += experienceGain;
                int meritGain = 0;
                if (victory)
                {
                    meritGain = database.BattleVictoryMerit;
                    character.Merit += meritGain;
                    if (character.BaseGrade == WICharacterGrade.Common && character.PromotedToHero == false)
                    {
                        character.PromotionAchievement = true;
                    }
                }
                bool injured = victory == false && orderlyRetreat == false && margin >= database.BattleInjuryPowerMargin;
                if (injured)
                {
                    character.InjuryMonths = Mathf.Max(character.InjuryMonths, database.BattleInjuryMonths);
                }
                WIHeroDefinition hero = database.GetHero(member.HeroId);
                string result = victory ? "승리" : orderlyRetreat ? "질서 있는 후퇴" : "패배";
                summary?.News.Add($"전투 인물 · {hero?.DisplayName.Get(database.UseEnglish) ?? member.HeroId} · {result} · " +
                    $"공훈 +{meritGain} · 경험 +{experienceGain} · 피로 +{fatigueGain}" +
                    (injured ? $" · 부상 {database.BattleInjuryMonths}개월" : string.Empty));
            }
        }

        // 함께 승리한 전투단원 쌍의 전우 승리를 누적하고 기준 도달 시 관계를 한 단계 개선합니다.
        private static void ApplyBattleRelationshipConsequences(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WIArmyState army,
            WITurnSummary summary)
        {
            List<string> memberIds = army.Members.Select(item => item.HeroId).Distinct().OrderBy(item => item).ToList();
            for (int firstIndex = 0; firstIndex < memberIds.Count; firstIndex += 1)
            {
                for (int secondIndex = firstIndex + 1; secondIndex < memberIds.Count; secondIndex += 1)
                {
                    WIRelationshipState relationship = state.GetOrCreateRelationship(memberIds[firstIndex], memberIds[secondIndex]);
                    relationship.SharedBattleVictories += 1;
                    if (relationship.SharedBattleVictories < database.BattleBondVictoryThreshold ||
                        relationship.Level == WIRelationshipLevel.Fondness)
                    {
                        continue;
                    }
                    relationship.SharedBattleVictories = 0;
                    relationship.Level = (WIRelationshipLevel)Mathf.Min(
                        (int)WIRelationshipLevel.Fondness,
                        (int)relationship.Level + 1);
                    WIHeroDefinition first = database.GetHero(memberIds[firstIndex]);
                    WIHeroDefinition second = database.GetHero(memberIds[secondIndex]);
                    summary?.News.Add($"전우 관계 발전 · {first?.DisplayName.Get(database.UseEnglish)} ↔ " +
                        $"{second?.DisplayName.Get(database.UseEnglish)} · {relationship.Level}");
                }
            }
        }


        // 아군 성에 머무는 전투단의 피로를 회복하고 재편성 남은 기간을 줄입니다.
        private static void ResolveArmyReorganization(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WITurnSummary summary)
        {
            foreach (WIArmyState army in state.Armies.Where(item => item.IsMoving == false &&
                         item.AwaitingBattle == false &&
                         state.GetCastle(item.CurrentCastleId)?.FactionId == item.FactionId))
            {
                foreach (WIArmyMemberState member in army.Members)
                {
                    WICharacterRuntimeState character = state.GetCharacter(member.HeroId);
                    if (character != null)
                    {
                        character.Fatigue = Mathf.Max(0, character.Fatigue - 15);
                    }
                }

                if (army.ReorganizationMonths > 0)
                {
                    army.ReorganizationMonths -= 1;
                    if (army.ReorganizationMonths == 0)
                    {
                        summary.News.Add($"{army.DisplayName} · {database.GetCastle(army.CurrentCastleId).DisplayName.Get(database.UseEnglish)}에서 재편성 완료");
                    }
                }
            }
        }

        // 전투 시스템에서 승리 결과를 넘겨받아 적 성을 점령 상태로 전환합니다.
        public static bool ResolveArmyVictoryAndOccupation(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WIArmyState army,
            WITurnSummary summary = null)
        {
            if (army == null || army.AwaitingBattle == false)
            {
                return false;
            }

            WICastleRuntimeState castle = state.GetCastle(army.CurrentCastleId);
            if (castle == null || castle.FactionId == army.FactionId)
            {
                return false;
            }

            string defeatedFactionId = castle.FactionId;
            HashSet<string> occupyingHeroIds = new HashSet<string>(army.Members.Select(member => member.HeroId));
            WICastleRuntimeState defeatedFallback = state.Castles
                .Where(item => item.CastleId != castle.CastleId && item.FactionId == defeatedFactionId)
                .OrderBy(item => item.AdjacentCastleIds.Contains(castle.CastleId) ? 0 : 1)
                .ThenBy(item => item.CastleId)
                .FirstOrDefault();
            foreach (string heroId in castle.HeroIds.Where(id => occupyingHeroIds.Contains(id) == false).ToList())
            {
                castle.HeroIds.Remove(heroId);
                if (defeatedFallback != null && defeatedFallback.HeroIds.Contains(heroId) == false)
                {
                    defeatedFallback.HeroIds.Add(heroId);
                }
                else if (defeatedFallback == null)
                {
                    WICharacterRuntimeState character = state.GetCharacter(heroId);
                    if (character != null)
                    {
                        character.Recruited = false;
                        character.Discovered = false;
                    }
                }
            }
            castle.FactionId = army.FactionId;
            castle.Stability = Mathf.Min(castle.Stability, 20);
            castle.OccupationUnrestMonths = 3;
            castle.GovernorHeroId = string.Empty;
            castle.StandingProject = null;
            castle.RepeatProject = false;
            castle.PendingGovernorAppointment = castle.FactionId == state.PlayerFactionId;
            castle.DelegatedToGovernor = false;
            army.AwaitingBattle = false;
            army.OriginCastleId = castle.CastleId;
            army.ReorganizationMonths = Mathf.Max(army.ReorganizationMonths, 1);
            foreach (WIArmyMemberState member in army.Members)
            {
                if (castle.HeroIds.Contains(member.HeroId) == false)
                {
                    castle.HeroIds.Add(member.HeroId);
                }
            }

            if (army.FactionId == state.PlayerFactionId)
            {
                WIOccupationEventSystem.CreatePending(state, castle.CastleId, defeatedFactionId, summary, database);
            }

            return true;
        }

        // 점령지의 영지관과 주둔 전투단 유무에 따라 월간 질서 안정을 처리합니다.
        private static void ResolveOccupationStability(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WITurnSummary summary)
        {
            foreach (WICastleRuntimeState castle in state.Castles)
            {
                if (castle.OccupationUnrestMonths <= 0)
                {
                    continue;
                }

                bool hasGovernor = string.IsNullOrEmpty(castle.GovernorHeroId) == false;
                bool hasGarrison = state.Armies.Exists(army => army.CurrentCastleId == castle.CastleId && army.AwaitingBattle == false);
                if (hasGovernor && hasGarrison)
                {
                    castle.Stability = Mathf.Clamp(castle.Stability + 5, 0, 100);
                    castle.OccupationUnrestMonths -= 1;
                }
                else
                {
                    castle.Stability = Mathf.Clamp(castle.Stability - 3, 0, 100);
                }

                summary.News.Add($"점령지 안정 · {database.GetCastle(castle.CastleId).DisplayName.Get(database.UseEnglish)} · 질서 {castle.Stability} · 불안 {castle.OccupationUnrestMonths}개월");
            }
        }
    }
}

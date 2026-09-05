using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProjectWI.Administration
{
    public static partial class WIAdministrationTurnSystem
    {
        // 장기 평화로 전선이 하나뿐일 때 인접 AI 진영 사이에 새로운 전쟁 압력을 만듭니다.
        public static bool EnsureAIWarPressure(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WITurnSummary summary)
        {
            if (database == null || state == null ||
                state.Turn < database.AIWarPressureIntervalMonths ||
                state.Turn % database.AIWarPressureIntervalMonths != 0)
            {
                return false;
            }

            int activeWarFronts = state.DiplomaticRelations.Count(relation =>
                relation.Status == WIDiplomaticStatus.War &&
                state.GetFactionState(relation.FirstFactionId)?.Eliminated == false &&
                state.GetFactionState(relation.SecondFactionId)?.Eliminated == false);
            if (activeWarFronts >= database.AIMinimumActiveWarFronts)
            {
                return false;
            }

            List<WIFactionDefinition> aiFactions = database.Factions
                .Where(faction => faction.PlayerFaction == false &&
                                  state.GetFactionState(faction.Id)?.Eliminated == false)
                .ToList();
            var candidate = (from first in aiFactions
                             from second in aiFactions
                             where string.CompareOrdinal(first.Id, second.Id) < 0
                             let relation = state.GetOrCreateDiplomaticRelation(first.Id, second.Id)
                             let borderCount = CountFactionBorderConnections(database, state, first.Id, second.Id)
                             where borderCount > 0 && relation != null &&
                                   (relation.Status == WIDiplomaticStatus.Neutral ||
                                    relation.Status == WIDiplomaticStatus.Friendly)
                             orderby GetWarPressureScore(first, second, borderCount) descending,
                                 first.Id, second.Id
                             select new { First = first, Second = second }).FirstOrDefault();
            if (candidate == null)
            {
                return false;
            }

            WIFactionDefinition initiator = GetStrategyAggression(candidate.First.AIStrategy) >=
                                            GetStrategyAggression(candidate.Second.AIStrategy)
                ? candidate.First
                : candidate.Second;
            WIFactionDefinition target = initiator == candidate.First ? candidate.Second : candidate.First;
            if (DeclareWar(state, initiator.Id, target.Id) == false)
            {
                return false;
            }

            string initiatorName = initiator.DisplayName.Get(database.UseEnglish);
            string targetName = target.DisplayName.Get(database.UseEnglish);
            summary?.News.Add($"전선 격화 · {initiatorName}이 장기 교착을 깨고 {targetName}에 선전포고");
            AddAIReasonReport(summary, initiator.Id, initiatorName, "군사",
                $"활성 전선 {activeWarFronts}/{database.AIMinimumActiveWarFronts} · 장기 교착 해소를 위해 인접 진영과 전쟁 개시");
            return true;
        }

        // 두 진영이 소유한 성 사이의 인접 경계 연결 수를 계산합니다.
        private static int CountFactionBorderConnections(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            string firstFactionId,
            string secondFactionId)
        {
            int count = 0;
            foreach (WICastleRuntimeState castle in state.Castles.Where(item => item.FactionId == firstFactionId))
            {
                count += castle.AdjacentCastleIds.Count(id => state.GetCastle(id)?.FactionId == secondFactionId);
            }
            return count;
        }

        // AI 성향과 접경 규모를 조합해 신규 전쟁 후보의 우선순위를 계산합니다.
        private static int GetWarPressureScore(
            WIFactionDefinition first,
            WIFactionDefinition second,
            int borderCount)
        {
            return borderCount * 10 + GetStrategyAggression(first.AIStrategy) +
                   GetStrategyAggression(second.AIStrategy);
        }

        // AI 성향을 전쟁 개시 성향 점수로 변환합니다.
        private static int GetStrategyAggression(WIAIStrategy strategy)
        {
            switch (strategy)
            {
                case WIAIStrategy.Aggressive:
                    return 8;
                case WIAIStrategy.Scheme:
                    return 5;
                case WIAIStrategy.Defense:
                    return 2;
                default:
                    return 3;
            }
        }

        // AI 진영이 전선 위협도를 평가해 복수 전투단을 방어, 증원과 공격 임무에 배치합니다.
        private static void PlanAIActions(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WITurnSummary summary)
        {
            foreach (WIFactionDefinition faction in database.Factions)
            {
                if (faction.PlayerFaction)
                {
                    continue;
                }
                if (state.GetFactionState(faction.Id)?.Eliminated == true)
                {
                    continue;
                }

                List<WICastleRuntimeState> factionCastles = state.Castles.FindAll(item => item.FactionId == faction.Id);
                int armyLimit = Mathf.Max(1, factionCastles.Count / 8) + (faction.AIStrategy == WIAIStrategy.Aggressive ? 1 : 0);
                while (state.Armies.Count(item => item.FactionId == faction.Id) < armyLimit)
                {
                    WICastleRuntimeState baseCastle = factionCastles
                        .OrderByDescending(item => GetCastleThreatScore(database, state, item, faction.Id))
                        .FirstOrDefault(item =>
                        item.FactionId == faction.Id &&
                        item.HeroIds.Count(heroId => state.IsCharacterBusy(heroId) == false) >= 2);
                    string commanderId = baseCastle?.HeroIds.FirstOrDefault(heroId => state.IsCharacterBusy(heroId) == false);
                    if (baseCastle == null || string.IsNullOrEmpty(commanderId))
                    {
                        break;
                    }
                    WIArmyState createdArmy = CreateArmy(database, state, baseCastle, commanderId);
                    FillAIArmy(database, state, baseCastle, createdArmy, faction.AIStrategy);
                }

                List<WIArmyState> idleArmies = state.Armies
                    .Where(item => item.FactionId == faction.Id && item.IsOperational)
                    .ToList();
                WIDiplomaticRelationState jointRelation = state.DiplomaticRelations.FirstOrDefault(item =>
                    item.JointAttackMonthsRemaining > 0 &&
                    (item.FirstFactionId == faction.Id || item.SecondFactionId == faction.Id));
                WICastleRuntimeState jointTarget = state.GetCastle(jointRelation?.JointAttackTargetCastleId);
                WICastleRuntimeState jointStagingCastle = jointTarget == null ? null : factionCastles.FirstOrDefault(item =>
                    item.AdjacentCastleIds.Contains(jointTarget.CastleId));
                WICastleRuntimeState threatenedCastle = factionCastles
                    .Where(item => GetCastleThreatScore(database, state, item, faction.Id) > 0)
                    .OrderByDescending(item => GetCastleThreatScore(database, state, item, faction.Id))
                    .FirstOrDefault();
                WICastleRuntimeState frontlineCastle = factionCastles
                    .Where(item => item.AdjacentCastleIds.Any(adjacentId =>
                    {
                        WICastleRuntimeState adjacent = state.GetCastle(adjacentId);
                        return adjacent != null && adjacent.FactionId != faction.Id &&
                               AreFactionsAtWar(state, faction.Id, adjacent.FactionId);
                    }))
                    .OrderByDescending(item => GetCastleThreatScore(database, state, item, faction.Id))
                    .ThenBy(item => item.CastleId)
                    .FirstOrDefault();
                WICastleRuntimeState stagingCastle = threatenedCastle ?? frontlineCastle;
                int attackInterval = faction.Id == "valdor"
                    ? database.GetCampaignVariant(state.CampaignVariant)?.ValdorAttackIntervalMonths ?? 3
                    : 3;
                bool shouldMarch = faction.AIStrategy == WIAIStrategy.Aggressive
                    ? state.Turn % attackInterval == 0
                    : state.Turn % Mathf.Max(attackInterval, 3) == 0;
                string militaryReason = jointTarget != null ?
                    $"동맹 공동 목표 {database.GetCastle(jointTarget.CastleId).DisplayName.Get(database.UseEnglish)} 우선" :
                    threatenedCastle != null ?
                        $"전선 위협 {GetCastleThreatScore(database, state, threatenedCastle, faction.Id)}점 · {database.GetCastle(threatenedCastle.CastleId).DisplayName.Get(database.UseEnglish)} 방어·증원" :
                    frontlineCastle != null ?
                        $"접경 전선 {database.GetCastle(frontlineCastle.CastleId).DisplayName.Get(database.UseEnglish)} 집결 · {GetAIStrategyReason(faction.AIStrategy)} 성향에 따라 공격 검토" :
                        shouldMarch ? $"{GetAIStrategyReason(faction.AIStrategy)} 성향에 따라 인접 적 공격 검토" : "전선 위협이 낮아 예비대 유지";
                AddAIReasonReport(summary, faction.Id, faction.DisplayName.Get(database.UseEnglish), "군사", militaryReason);
                foreach (WIArmyState army in idleArmies)
                {
                    if (jointTarget != null && jointStagingCastle != null &&
                        AreFactionsAtWar(state, faction.Id, jointTarget.FactionId))
                    {
                        army.StrategicTargetCastleId = jointTarget.CastleId;
                        if (army.CurrentCastleId == jointStagingCastle.CastleId)
                        {
                            if (BeginArmyMarch(database, state, army, jointTarget.CastleId))
                            {
                                army.Mission = WIArmyMission.Attack;
                            }
                        }
                        else
                        {
                            string jointStep = GetNextFriendlyStep(database, state, army.CurrentCastleId,
                                jointStagingCastle.CastleId, faction.Id);
                            if (string.IsNullOrEmpty(jointStep) == false && BeginArmyMarch(database, state, army, jointStep))
                            {
                                army.Mission = WIArmyMission.Reinforce;
                            }
                        }
                        continue;
                    }

                    if (stagingCastle == null)
                    {
                        army.Mission = WIArmyMission.Reserve;
                        army.StrategicTargetCastleId = string.Empty;
                        continue;
                    }

                    army.StrategicTargetCastleId = stagingCastle.CastleId;
                    if (army.CurrentCastleId != stagingCastle.CastleId)
                    {
                        string reinforcementStep = GetNextFriendlyStep(database, state, army.CurrentCastleId, stagingCastle.CastleId, faction.Id);
                        if (string.IsNullOrEmpty(reinforcementStep) == false && BeginArmyMarch(database, state, army, reinforcementStep))
                        {
                            army.Mission = WIArmyMission.Reinforce;
                        }
                        continue;
                    }

                    army.Mission = WIArmyMission.Defend;
                    if (shouldMarch == false)
                    {
                        continue;
                    }

                    WICastleRuntimeState origin = state.GetCastle(army.CurrentCastleId);
                    List<string> targetCandidates = origin?.AdjacentCastleIds
                        .Where(id =>
                        {
                            WICastleRuntimeState target = state.GetCastle(id);
                            return target != null && target.FactionId != faction.Id &&
                                   AreFactionsAtWar(state, faction.Id, target.FactionId) &&
                                   CanAIFactionAttack(database, state, army, target, faction);
                        })
                        .OrderByDescending(id => GetAttackTargetScore(state, id))
                        .ToList();
                    int militarySalt = database.Factions.ToList().FindIndex(item => item.Id == faction.Id) + (army.ArmyId?.Length ?? 0);
                    int targetIndex = targetCandidates == null || targetCandidates.Count == 0 ? -1 :
                        GetAICandidateIndex(database, state, targetCandidates.Count, militarySalt);
                    string targetId = targetIndex < 0 ? string.Empty : targetCandidates[targetIndex];
                    if (string.IsNullOrEmpty(targetId) || BeginArmyMarch(database, state, army, targetId) == false)
                    {
                        continue;
                    }

                    army.Mission = WIArmyMission.Attack;
                    army.StrategicTargetCastleId = targetId;
                    WICastleRuntimeState target = state.GetCastle(targetId);
                    if (target.FactionId == state.PlayerFactionId)
                    {
                        target.InvasionWarning = true;
                        summary.News.Add($"침공 경고 · {faction.DisplayName.Get(database.UseEnglish)}이 {database.GetCastle(targetId).DisplayName.Get(database.UseEnglish)}로 진격 중");
                    }
                }
            }
        }

        // AI 전투단이 지휘관 한 명으로만 원정하지 않도록 성에 최소 한 명을 남기고 전투 인원을 편성합니다.
        private static void FillAIArmy(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WICastleRuntimeState castle,
            WIArmyState army,
            WIAIStrategy strategy)
        {
            if (castle == null || army == null)
            {
                return;
            }

            int recommended = GetRecommendedArmySize(database, army);
            int targetSize = strategy == WIAIStrategy.Aggressive ? Mathf.Min(4, recommended) : Mathf.Min(3, recommended);
            targetSize = Mathf.Min(targetSize, Mathf.Max(1, castle.HeroIds.Count - 1));
            WIUnitRole[] roles = { WIUnitRole.Vanguard, WIUnitRole.Ranged, WIUnitRole.Magic, WIUnitRole.Support };
            int roleIndex = 0;
            foreach (string heroId in castle.HeroIds
                         .Where(id => state.IsCharacterBusy(id) == false)
                         .OrderByDescending(id => database.GetHero(id)?.Might ?? 0)
                         .ToList())
            {
                if (army.Members.Count >= targetSize)
                {
                    break;
                }

                AddArmyMember(database, state, army, heroId, roles[roleIndex % roles.Length]);
                roleIndex += 1;
            }
        }

        // 성의 적 인접 수, 적 전투단 접근과 방어 상태를 조합한 전선 위협도를 반환합니다.
        public static int GetCastleThreatScore(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WICastleRuntimeState castle,
            string factionId)
        {
            if (castle.FactionId != factionId)
            {
                return 0;
            }

            int enemyBorders = castle.AdjacentCastleIds.Count(id =>
            {
                string adjacentFactionId = state.GetCastle(id)?.FactionId;
                return string.IsNullOrEmpty(adjacentFactionId) == false &&
                       adjacentFactionId != factionId &&
                       AreFactionsAtWar(state, factionId, adjacentFactionId);
            });
            int approachingEnemies = state.Armies.Count(army =>
                army.FactionId != factionId &&
                AreFactionsAtWar(state, factionId, army.FactionId) &&
                ((army.IsMoving && army.TargetCastleId == castle.CastleId) ||
                 (army.AwaitingBattle && army.CurrentCastleId == castle.CastleId)));
            return Mathf.Max(0, enemyBorders * 30 + approachingEnemies * 50 - castle.Defense / 4 - castle.Stability / 5);
        }

        // 시나리오 보존선과 예상 전력을 검사해 AI 진영의 무모한 원정을 막습니다.
        private static bool CanAIFactionAttack(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WIArmyState army,
            WICastleRuntimeState target,
            WIFactionDefinition faction)
        {
            WICampaignVariantDefinition variant = database.GetCampaignVariant(state.CampaignVariant);
            string preservationFactionId = variant?.AIPreservationFactionId;
            if (string.IsNullOrEmpty(preservationFactionId) == false &&
                target.FactionId == preservationFactionId &&
                variant.ValdorAIPreservationCastleCount > 0 &&
                state.Castles.Count(castle => castle.FactionId == preservationFactionId) <=
                variant.ValdorAIPreservationCastleCount)
            {
                return false;
            }

            List<WIArmyState> defenders = state.Armies.Where(defender =>
                defender.FactionId == target.FactionId && defender.CurrentCastleId == target.CastleId &&
                defender.IsOperational).ToList();
            int attackPower = state.Armies
                .Where(candidate => candidate.FactionId == army.FactionId &&
                                    candidate.CurrentCastleId == army.CurrentCastleId &&
                                    candidate.IsOperational)
                .Sum(candidate => GetArmyBattlePower(database, state, candidate));
            int defensePower = GetCastleDefensePower(database, state, target, defenders);
            int requiredPercent = faction.AIStrategy == WIAIStrategy.Aggressive
                ? database.AggressiveAIAttackPowerPercent
                : database.StandardAIAttackPowerPercent;
            return attackPower * 100 >= defensePower * requiredPercent;
        }


        // 점령 가치가 높고 방어가 약한 적 성을 우선하도록 공격 점수를 계산합니다.
        private static int GetAttackTargetScore(WIAdministrationState state, string castleId)
        {
            WICastleRuntimeState castle = state.GetCastle(castleId);
            if (castle == null)
            {
                return int.MinValue;
            }
            int playerPriority = castle.FactionId == state.PlayerFactionId ? 20 : 0;
            return playerPriority + 100 - castle.Defense - castle.Stability / 2;
        }

    }
}

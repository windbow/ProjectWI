using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProjectWI.Administration
{
    public static partial class WICampaignAutoPlayer
    {
        // 정책에 따라 플레이어 전투단을 편성하고 합법적인 인접 적 성으로 원정시킵니다.
        private static void PrepareMilitaryAction(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WIAutoPlayerPolicy policy,
            WIAutoCampaignMetrics metrics,
            WIAutoStrategicPlan plan,
            int month)
        {
            UpdateStrategicGoal(database, state, metrics, plan, month);
            EnsurePlayerArmies(database, state, policy, metrics, plan, month);
            if (RespondToIncomingThreat(database, state, metrics, plan, month))
            {
                return;
            }
            if (state.Turn < plan.RecoveryUntilTurn)
            {
                plan.Phase = WIAutoStrategicPhase.Recover;
                metrics.RecoveryMonths += 1;
                metrics.RecordDecision(month, plan, "DEFEAT_RECOVERY", $"패전 후 {plan.RecoveryUntilTurn - state.Turn}개월 동안 재편합니다.");
                return;
            }

            PositionArmiesForJointAttack(database, state, policy, plan);
            PrepareBlockedArmyTraining(database, state, policy);

            // 첫 관문은 1년 안에 공략하고 이후에는 정책별 준비 주기로 연속 점령 속도를 제한합니다.
            bool firstExpansion = state.Castles.Count(castle =>
                castle.FactionId == state.PlayerFactionId) <= 1;
            int marchInterval = firstExpansion
                ? (policy == WIAutoPlayerPolicy.Administration ? 6 : 3)
                : (policy == WIAutoPlayerPolicy.Administration ? 12 :
                    policy == WIAutoPlayerPolicy.Aggressive ? 6 : 8);
            bool allowMarch = state.Turn >= 3 && state.Turn % marchInterval == 0;
            if (allowMarch == false)
            {
                plan.Phase = WIAutoStrategicPhase.Prepare;
                metrics.RecordDecision(month, plan, "PREPARATION_INTERVAL", $"다음 원정 검토 주기 {marchInterval}개월을 준비합니다.");
                return;
            }
            bool finalValdorCampaign = IsFinalValdorCampaign(state);

            foreach (WIArmyState army in state.Armies.Where(item =>
                         item.FactionId == state.PlayerFactionId && item.IsOperational &&
                         item.Mission != WIArmyMission.Defend).ToList())
            {
                WICastleRuntimeState origin = state.GetCastle(army.CurrentCastleId);
                string targetId = origin?.AdjacentCastleIds
                    .Where(id =>
                    {
                        WICastleRuntimeState target = state.GetCastle(id);
                        return target != null && target.FactionId != state.PlayerFactionId &&
                               WIAdministrationTurnSystem.AreFactionsAtWar(
                                   state, state.PlayerFactionId, target.FactionId) &&
                               (CanAutoAttack(database, state, army, target, policy) ||
                                finalValdorCampaign && plan.ConsecutiveNoAttackChecks >= 6 &&
                                CanAutoAttack(database, state, army, target, policy, 100));
                    })
                    .OrderByDescending(id => id == plan.TargetCastleId)
                    .ThenBy(id => GetStrategicTargetScore(database, state, state.GetCastle(id)))
                    .FirstOrDefault();
                if (string.IsNullOrEmpty(targetId) == false &&
                    WIAdministrationTurnSystem.BeginArmyMarch(database, state, army, targetId))
                {
                    bool finalAssault = finalValdorCampaign && plan.ConsecutiveNoAttackChecks >= 6;
                    army.Mission = WIArmyMission.Attack;
                    army.StrategicTargetCastleId = targetId;
                    metrics.MarchesStarted += 1;
                    plan.ConsecutiveNoAttackChecks = 0;
                    plan.Phase = WIAutoStrategicPhase.Attack;
                    metrics.RecordDecision(month, plan, finalAssault ? "FINAL_ASSAULT_STARTED" : "MARCH_STARTED",
                        finalAssault
                            ? $"장기 교착을 끝내기 위해 안전 여유를 포기하고 {targetId}(으)로 최종 공세를 시작합니다."
                            : $"{origin.CastleId}에서 {targetId}(으)로 원정을 시작합니다.");
                }
            }
            if (metrics.DecisionTraces.Any(trace => trace.Month == month &&
                    (trace.ReasonCode == "MARCH_STARTED" || trace.ReasonCode == "FINAL_ASSAULT_STARTED")) == false)
            {
                plan.Phase = WIAutoStrategicPhase.Prepare;
                plan.ConsecutiveNoAttackChecks += 1;
                if (policy == WIAutoPlayerPolicy.Aggressive && plan.ConsecutiveNoAttackChecks >= 3 &&
                    HasAlternativeStrategicTarget(state, plan.TargetCastleId))
                {
                    string blockedTargetId = plan.TargetCastleId;
                    plan.AvoidedTargetCastleId = blockedTargetId;
                    plan.AvoidedTargetUntilTurn = state.Turn + 24;
                    plan.TargetCastleId = string.Empty;
                    plan.AssemblyCastleId = string.Empty;
                    plan.ConsecutiveNoAttackChecks = 0;
                    metrics.GoalsAbandoned += 1;
                    metrics.RecordDecision(month, plan, "BLOCKED_GOAL_ROTATED",
                        $"{blockedTargetId}의 전력 조건을 3회 연속 충족하지 못해 다른 접경 목표를 검토합니다.");
                    return;
                }
                if (policy == WIAutoPlayerPolicy.Aggressive && plan.ConsecutiveNoAttackChecks >= 3 &&
                    TryOpenAlternativeFront(database, state, out string targetFactionId))
                {
                    plan.TargetCastleId = string.Empty;
                    plan.AssemblyCastleId = string.Empty;
                    plan.ConsecutiveNoAttackChecks = 0;
                    metrics.WarsDeclared += 1;
                    metrics.RecordDecision(month, plan, "ALTERNATIVE_FRONT_DECLARED",
                        $"강한 단일 전선의 장기 교착을 피하기 위해 인접한 {targetFactionId} 진영에 선전포고합니다.");
                    return;
                }
                metrics.RecordDecision(month, plan, "NO_ATTACKABLE_TARGET", "접경·전쟁·피로·예상 전력 조건을 만족하는 목표가 없습니다.");
            }
        }

        // 현재 목표 외에 전쟁 중인 다른 접경 성이 있어 목표 전환이 가능한지 확인합니다.
        private static bool HasAlternativeStrategicTarget(WIAdministrationState state, string currentTargetCastleId)
        {
            return state.Castles.Any(castle => castle.CastleId != currentTargetCastleId &&
                castle.FactionId != state.PlayerFactionId &&
                WIAdministrationTurnSystem.AreFactionsAtWar(
                    state, state.PlayerFactionId, castle.FactionId) &&
                castle.AdjacentCastleIds.Any(id => state.GetCastle(id)?.FactionId == state.PlayerFactionId) &&
                IsStrategicTargetReachable(state, castle));
        }

        // 단일 강적 전선에 장기 봉쇄됐을 때 중립 또는 우호 접경 진영에 두 번째 전선을 엽니다.
        private static bool TryOpenAlternativeFront(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            out string targetFactionId)
        {
            targetFactionId = string.Empty;
            int activePlayerWars = state.DiplomaticRelations.Count(relation =>
            {
                if (relation.Status != WIDiplomaticStatus.War ||
                    relation.FirstFactionId != state.PlayerFactionId &&
                    relation.SecondFactionId != state.PlayerFactionId)
                {
                    return false;
                }
                string enemyFactionId = relation.FirstFactionId == state.PlayerFactionId
                    ? relation.SecondFactionId : relation.FirstFactionId;
                return state.Castles.Any(castle => castle.FactionId == enemyFactionId &&
                    castle.AdjacentCastleIds.Any(id => state.GetCastle(id)?.FactionId == state.PlayerFactionId) &&
                    IsStrategicTargetReachable(state, castle));
            });
            if (activePlayerWars >= 2)
            {
                return false;
            }

            WICastleRuntimeState target = state.Castles
                .Where(castle => castle.FactionId != state.PlayerFactionId &&
                    castle.AdjacentCastleIds.Any(id => state.GetCastle(id)?.FactionId == state.PlayerFactionId) &&
                    IsStrategicTargetReachable(state, castle))
                .Where(castle =>
                {
                    WIDiplomaticRelationState relation = state.GetOrCreateDiplomaticRelation(
                        state.PlayerFactionId, castle.FactionId);
                    return relation.Status == WIDiplomaticStatus.Neutral ||
                           relation.Status == WIDiplomaticStatus.Friendly;
                })
                .OrderBy(castle => GetStrategicTargetScore(database, state, castle))
                .ThenBy(castle => castle.CastleId)
                .FirstOrDefault();
            if (target == null || WIAdministrationTurnSystem.DeclareWar(
                    state, state.PlayerFactionId, target.FactionId) == false)
            {
                return false;
            }

            targetFactionId = target.FactionId;
            return true;
        }

        // 기본 전투단 둘을 운용하고 아레스 메인은 영토 성장에 따라 수비대와 원정대를 단계적으로 늘립니다.
        private static void EnsurePlayerArmies(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WIAutoPlayerPolicy policy,
            WIAutoCampaignMetrics metrics,
            WIAutoStrategicPlan plan,
            int month)
        {
            int playerCastleCount = state.Castles.Count(castle => castle.FactionId == state.PlayerFactionId);
            int armyLimit = 2;
            if (state.CampaignVariant == WICampaignVariant.AresMain)
            {
                armyLimit = playerCastleCount >= 24 ? 5 :
                    playerCastleCount >= 12 ? 4 :
                    playerCastleCount >= 4 ? 3 : 2;
            }
            while (state.Armies.Count(army => army.FactionId == state.PlayerFactionId) < armyLimit)
            {
                WICastleRuntimeState baseCastle = state.Castles
                    .Where(castle => castle.FactionId == state.PlayerFactionId)
                    .OrderByDescending(castle => castle.HeroIds.Count)
                    .FirstOrDefault(castle => castle.HeroIds.Count(heroId =>
                        state.IsCharacterBusy(heroId) == false) >= 2);
                string commanderId = baseCastle?.HeroIds
                    .Where(heroId => state.IsCharacterBusy(heroId) == false && heroId != baseCastle.GovernorHeroId)
                    .OrderByDescending(heroId => database.GetHero(heroId)?.Leadership ?? 0)
                    .FirstOrDefault();
                if (baseCastle == null || string.IsNullOrEmpty(commanderId))
                {
                    break;
                }

                WIArmyState army = WIAdministrationTurnSystem.CreateArmy(database, state, baseCastle, commanderId);
                if (army == null)
                {
                    break;
                }
                metrics.ArmiesCreated += 1;
                FillArmy(database, state, baseCastle, army, policy);
            }

            foreach (WIArmyState army in state.Armies.Where(item =>
                         item.FactionId == state.PlayerFactionId && item.IsOperational).ToList())
            {
                WICastleRuntimeState castle = state.GetCastle(army.CurrentCastleId);
                if (castle == null || castle.FactionId != state.PlayerFactionId)
                {
                    continue;
                }
                int before = army.Members.Count;
                FillArmy(database, state, castle, army, policy);
                int added = army.Members.Count - before;
                if (added > 0)
                {
                    metrics.ArmyReinforcements += added;
                    metrics.RecordDecision(month, plan, "ARMY_REINFORCED", $"{army.ArmyId}에 {added}명을 보충했습니다.");
                }
                else
                {
                    ScheduleFrontlineReinforcementTransfer(database, state, castle, army, policy, metrics, plan, month);
                }
            }
        }

        // 손실 전투단의 주둔 성에 병력이 없으면 후방 성의 유휴 일반 인물을 매달 한 성씩 전선으로 이동시킵니다.
        private static void ScheduleFrontlineReinforcementTransfer(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WICastleRuntimeState destination,
            WIArmyState army,
            WIAutoPlayerPolicy policy,
            WIAutoCampaignMetrics metrics,
            WIAutoStrategicPlan plan,
            int month)
        {
            int maximum = WIAdministrationTurnSystem.GetRecommendedArmySize(database, army);
            bool firstExpansion = state.Castles.Count(castle =>
                castle.FactionId == state.PlayerFactionId) <= 1;
            int targetSize = policy == WIAutoPlayerPolicy.Administration && firstExpansion == false
                ? Mathf.Min(4, maximum) : maximum;
            if (army.Members.Count >= targetSize)
            {
                return;
            }
            // 메인에서는 이동 예약 인원까지 보충 인원에 포함해 과잉 이동을 막습니다.
            if (state.CampaignVariant == WICampaignVariant.AresMain && army.Members.Count + state.CharacterTransfers.Count(item => item.TargetCastleId == destination.CastleId) >= targetSize)
            {
                return;
            }
            var transferCandidate = state.Castles
                .Where(castle => castle.FactionId == state.PlayerFactionId && castle.CastleId != destination.CastleId)
                .SelectMany(castle => castle.HeroIds
                    .Where(heroId => state.IsCharacterBusy(heroId) == false)
                    .Where(heroId => state.CampaignVariant == WICampaignVariant.AresMain || state.GetCharacter(heroId)?.BaseGrade == WICharacterGrade.Common)
                    .Where(heroId => IsReservedAdministrator(state, castle, heroId) == false)
                    .Select(heroId => new
                    {
                        Castle = castle,
                        HeroId = heroId,
                        NextCastleId = FindFriendlyStep(state, castle.CastleId, new[] { destination.CastleId })
                    }))
                .Where(item => string.IsNullOrEmpty(item.NextCastleId) == false)
                .OrderBy(item => GetFriendlyPathDistance(state, item.Castle.CastleId, destination.CastleId))
                .ThenByDescending(item => database.GetHero(item.HeroId)?.Might ?? 0)
                .FirstOrDefault();
            if (transferCandidate == null ||
                WIAdministrationTurnSystem.StartCharacterTransfer(
                    database, state, transferCandidate.HeroId, state.CampaignVariant == WICampaignVariant.AresMain ? destination.CastleId : transferCandidate.NextCastleId) == false)
            {
                return;
            }

            metrics.RecordDecision(month, plan, "REINFORCEMENT_TRANSFER",
                $"{transferCandidate.HeroId}을(를) {destination.CastleId}의 {army.ArmyId} 보충을 위해 " +
                $"{transferCandidate.NextCastleId}(으)로 이동시켰습니다.");
        }

        // 아군 성으로 진군 중인 적을 감지하면 원정보다 수비를 우선하고 가용 전투단을 증원시킵니다.
        private static bool RespondToIncomingThreat(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WIAutoCampaignMetrics metrics,
            WIAutoStrategicPlan plan,
            int month)
        {
            WICastleRuntimeState threatenedCastle = state.Castles
                .Where(castle => castle.FactionId == state.PlayerFactionId)
                .Select(castle => new
                {
                    Castle = castle,
                    IncomingPower = state.Armies.Where(enemy =>
                            enemy.FactionId != state.PlayerFactionId && enemy.IsMoving &&
                            enemy.TargetCastleId == castle.CastleId &&
                            WIAdministrationTurnSystem.AreFactionsAtWar(
                                state, state.PlayerFactionId, enemy.FactionId))
                        .Sum(enemy => WIAdministrationTurnSystem.GetArmyBattlePower(database, state, enemy))
                })
                .Where(item => item.IncomingPower > 0)
                .OrderByDescending(item => item.IncomingPower)
                .Select(item => item.Castle)
                .FirstOrDefault();
            if (threatenedCastle == null)
            {
                return false;
            }

            metrics.ThreatResponseMonths += 1;
            plan.Phase = WIAutoStrategicPhase.Prepare;
            foreach (WIArmyState army in state.Armies.Where(item =>
                         item.FactionId == state.PlayerFactionId && item.IsOperational).ToList())
            {
                army.Mission = WIArmyMission.Defend;
                if (army.CurrentCastleId == threatenedCastle.CastleId)
                {
                    continue;
                }
                string step = FindFriendlyStep(state, army.CurrentCastleId,
                    new[] { threatenedCastle.CastleId });
                if (string.IsNullOrEmpty(step) == false &&
                    WIAdministrationTurnSystem.BeginArmyMarch(database, state, army, step))
                {
                    army.Mission = WIArmyMission.Reinforce;
                    metrics.DefensiveReinforcementMarches += 1;
                }
            }
            metrics.RecordDecision(month, plan, "INCOMING_THREAT_RESPONSE",
                $"{threatenedCastle.CastleId}로 접근하는 적을 감지해 원정을 보류하고 수비 증원을 명령했습니다.");
            return true;
        }

        // 보유 접경 전체에서 약한 적성을 장기 목표로 유지하고 점령 또는 외교 변화 때만 다시 선정합니다.
        private static void UpdateStrategicGoal(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WIAutoCampaignMetrics metrics,
            WIAutoStrategicPlan plan,
            int month)
        {
            WICastleRuntimeState current = state.GetCastle(plan.TargetCastleId);
            bool finalValdorCampaign = IsFinalValdorCampaign(state);
            bool hasValdorFront = state.Castles.Any(castle => castle.FactionId == "valdor" &&
                castle.AdjacentCastleIds.Any(id => state.GetCastle(id)?.FactionId == state.PlayerFactionId));
            if (finalValdorCampaign && hasValdorFront &&
                WIAdministrationTurnSystem.AreFactionsAtWar(state, state.PlayerFactionId, "valdor") == false &&
                WIAdministrationTurnSystem.DeclareWar(state, state.PlayerFactionId, "valdor"))
            {
                metrics.WarsDeclared += 1;
                metrics.RecordDecision(month, plan, "VALDOR_WAR_RESUMED",
                    "발도르 멸망 목표를 계속하기 위해 영향력을 지불하고 전쟁을 재개합니다.");
            }
            bool remainsValid = current != null && current.FactionId != state.PlayerFactionId &&
                                current.AdjacentCastleIds.Any(id => state.GetCastle(id)?.FactionId == state.PlayerFactionId) &&
                                IsStrategicTargetReachable(state, current) &&
                                WIAdministrationTurnSystem.AreFactionsAtWar(state, state.PlayerFactionId, current.FactionId) &&
                                (finalValdorCampaign == false || hasValdorFront == false || current.FactionId == "valdor");
            if (remainsValid)
            {
                RefreshStrategicAssemblyCastle(database, state, plan, current);
                return;
            }

            List<WICastleRuntimeState> candidates = state.Castles
                .Where(castle => castle.FactionId != state.PlayerFactionId &&
                                 WIAdministrationTurnSystem.AreFactionsAtWar(
                                     state, state.PlayerFactionId, castle.FactionId) &&
                                 castle.AdjacentCastleIds.Any(id => state.GetCastle(id)?.FactionId == state.PlayerFactionId) &&
                                 IsStrategicTargetReachable(state, castle))
                .ToList();
            List<WICastleRuntimeState> available = candidates.Where(castle =>
                castle.CastleId != plan.AvoidedTargetCastleId || state.Turn >= plan.AvoidedTargetUntilTurn).ToList();
            WICastleRuntimeState target = available
                .OrderByDescending(castle => finalValdorCampaign && castle.FactionId == "valdor")
                .ThenBy(castle => GetStrategicTargetScore(database, state, castle))
                .ThenBy(castle => castle.CastleId)
                .FirstOrDefault();
            string targetId = target?.CastleId ?? string.Empty;
            if (plan.TargetCastleId != targetId)
            {
                plan.TargetCastleId = targetId;
                RefreshStrategicAssemblyCastle(database, state, plan, target);
                plan.Phase = string.IsNullOrEmpty(targetId)
                    ? WIAutoStrategicPhase.Observe : WIAutoStrategicPhase.Assemble;
                metrics.GoalChanges += 1;
                metrics.RecordDecision(month, plan, string.IsNullOrEmpty(targetId) ? "NO_STRATEGIC_TARGET" : "GOAL_SELECTED",
                    string.IsNullOrEmpty(targetId) ? "전쟁 중인 도달 가능 접경 목표가 없습니다." : $"장기 공략 목표를 {targetId}(으)로 정했습니다.");
            }
        }

        // 현재 수비 전력, 주변 반격 위험과 경제 가치를 합산해 낮을수록 좋은 공략 점수를 계산합니다.
        private static int GetStrategicTargetScore(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WICastleRuntimeState target)
        {
            if (target == null)
            {
                return int.MaxValue;
            }
            List<WIArmyState> defenders = state.Armies.Where(army =>
                army.FactionId == target.FactionId && army.CurrentCastleId == target.CastleId &&
                army.IsOperational).ToList();
            int defensePower = WIAdministrationTurnSystem.GetCastleDefensePower(database, state, target, defenders);
            int counterAttackRisk = target.AdjacentCastleIds.Select(state.GetCastle)
                .Where(castle => castle != null && castle.FactionId == target.FactionId)
                .Sum(castle => 25 + state.Armies.Where(army => army.FactionId == target.FactionId &&
                                                               army.CurrentCastleId == castle.CastleId)
                    .Sum(army => WIAdministrationTurnSystem.GetArmyBattlePower(database, state, army) / 3));
            int economicValue = target.Prosperity * 2 + target.Technology + (int)target.CastleSize * 20;
            return defensePower + counterAttackRisk - economicValue;
        }

        // 전선의 예상 전력을 넘지 못한 전투단이 단순 대기하지 않고 합동훈련을 준비하게 합니다.
        private static void PrepareBlockedArmyTraining(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WIAutoPlayerPolicy policy)
        {
            foreach (WIArmyState army in state.Armies.Where(item =>
                         item.FactionId == state.PlayerFactionId && item.IsOperational))
            {
                float averageFatigue = army.Members.Count == 0 ? 100f : (float)army.Members
                    .Average(member => state.GetCharacter(member.HeroId)?.Fatigue ?? 100);
                if (army.Proficiency == WIUnitProficiency.Elite || averageFatigue > 50f)
                {
                    continue;
                }

                WICastleRuntimeState origin = state.GetCastle(army.CurrentCastleId);
                if (origin == null)
                {
                    continue;
                }
                List<WICastleRuntimeState> enemies = origin.AdjacentCastleIds
                    .Select(state.GetCastle)
                    .Where(target => target != null && target.FactionId != state.PlayerFactionId &&
                                     WIAdministrationTurnSystem.AreFactionsAtWar(
                                         state, state.PlayerFactionId, target.FactionId))
                    .ToList();
                if (enemies.Count > 0 && enemies.Any(target =>
                        CanAutoAttack(database, state, army, target, policy)) == false)
                {
                    WIAdministrationTurnSystem.ScheduleJointTraining(army);
                }
            }
        }

        // 공동 공격 전력을 유지하면서 후방 전투단을 목표 성 앞의 집결지로 이동시킵니다.
        private static void PositionArmiesForJointAttack(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WIAutoPlayerPolicy policy,
            WIAutoStrategicPlan plan)
        {
            // 프리 시나리오는 기존 통합 운용을 유지하고 아레스 메인은 수비대와 원정대를 분리합니다.
            if (state.CampaignVariant != WICampaignVariant.AresMain)
            {
                ConsolidateArmiesAtSameCastle(database, state);
            }

            List<WICastleRuntimeState> frontlines = state.Castles.Where(castle =>
                castle.FactionId == state.PlayerFactionId && castle.AdjacentCastleIds.Any(id =>
                {
                    WICastleRuntimeState adjacent = state.GetCastle(id);
                    return adjacent != null && adjacent.FactionId != state.PlayerFactionId &&
                           WIAdministrationTurnSystem.AreFactionsAtWar(
                               state, state.PlayerFactionId, adjacent.FactionId);
                })).ToList();
            WIArmyState reserveArmy = null;
            WICastleRuntimeState reserveCastle = null;
            if (state.CampaignVariant == WICampaignVariant.AresMain &&
                state.Armies.Count(army => army.FactionId == state.PlayerFactionId) >= 2)
            {
                reserveCastle = frontlines.OrderByDescending(castle => castle.AdjacentCastleIds
                        .Select(state.GetCastle)
                        .Where(adjacent => adjacent != null && adjacent.FactionId != state.PlayerFactionId &&
                                           WIAdministrationTurnSystem.AreFactionsAtWar(
                                               state, state.PlayerFactionId, adjacent.FactionId))
                        .Sum(adjacent => state.Armies.Where(enemy =>
                                enemy.FactionId == adjacent.FactionId &&
                                enemy.CurrentCastleId == adjacent.CastleId && enemy.IsOperational)
                            .Sum(enemy => WIAdministrationTurnSystem.GetArmyBattlePower(database, state, enemy))))
                    .FirstOrDefault();
                List<WIArmyState> operationalArmies = state.Armies.Where(army =>
                        army.FactionId == state.PlayerFactionId && army.IsOperational)
                    .OrderBy(army => WIAdministrationTurnSystem.GetArmyBattlePower(database, state, army))
                    .ToList();
                WIArmyState weakestArmy = operationalArmies.FirstOrDefault();
                WICastleRuntimeState strategicTarget = state.GetCastle(plan.TargetCastleId);
                int totalPower = operationalArmies.Sum(army =>
                    WIAdministrationTurnSystem.GetArmyBattlePower(database, state, army));
                int weakestPower = weakestArmy == null ? 0 :
                    WIAdministrationTurnSystem.GetArmyBattlePower(database, state, weakestArmy);
                int requiredPower = GetRequiredAttackPower(database, state, strategicTarget, policy);
                bool canKeepReserve = weakestArmy != null && strategicTarget != null &&
                                      totalPower - weakestPower >= requiredPower;
                reserveArmy = canKeepReserve ? weakestArmy : null;
                if (reserveArmy != null)
                {
                    reserveArmy.Mission = WIArmyMission.Defend;
                }
            }
            if (string.IsNullOrEmpty(plan.AssemblyCastleId) == false)
            {
                WICastleRuntimeState assembly = state.GetCastle(plan.AssemblyCastleId);
                if (assembly != null && assembly.FactionId == state.PlayerFactionId)
                {
                    frontlines = new List<WICastleRuntimeState> { assembly };
                }
            }
            foreach (WIArmyState army in state.Armies.Where(item =>
                         item.FactionId == state.PlayerFactionId && item.IsOperational).ToList())
            {
                WICastleRuntimeState origin = state.GetCastle(army.CurrentCastleId);
                WICastleRuntimeState reinforcementCastle = FindNearestReinforcementCastle(database, state, army);
                List<WICastleRuntimeState> destinations = reinforcementCastle != null
                    ? new List<WICastleRuntimeState> { reinforcementCastle }
                    : army == reserveArmy && reserveCastle != null
                        ? new List<WICastleRuntimeState> { reserveCastle }
                        : frontlines;
                if (army != reserveArmy)
                {
                    army.Mission = WIArmyMission.Reserve;
                }
                if (origin == null || destinations.Contains(origin))
                {
                    continue;
                }
                string step = FindFriendlyStep(state, origin.CastleId, destinations.Select(item => item.CastleId));
                if (string.IsNullOrEmpty(step) == false &&
                    WIAdministrationTurnSystem.BeginArmyMarch(database, state, army, step))
                {
                    army.Mission = WIArmyMission.Reinforce;
                }
            }
        }

        // 목표 접경의 아군 집결지까지 작전 가능한 전투단 하나 이상이 아군 영토로 도달할 수 있는지 확인합니다.
        private static bool IsStrategicTargetReachable(
            WIAdministrationState state,
            WICastleRuntimeState target)
        {
            if (target == null)
            {
                return false;
            }
            List<string> assemblyIds = target.AdjacentCastleIds
                .Where(id => state.GetCastle(id)?.FactionId == state.PlayerFactionId).ToList();
            return assemblyIds.Count > 0 && state.Armies.Any(army =>
                army.FactionId == state.PlayerFactionId && army.IsOperational &&
                assemblyIds.Any(id => GetFriendlyPathDistance(state, army.CurrentCastleId, id) < int.MaxValue));
        }

        // 아레스 메인에서 충분한 기반을 확보한 뒤 발도르 멸망에 전력을 집중할 단계인지 확인합니다.
        private static bool IsFinalValdorCampaign(WIAdministrationState state)
        {
            return state.CampaignVariant == WICampaignVariant.AresMain &&
                   state.Castles.Count(castle => castle.FactionId == state.PlayerFactionId) >= 24 &&
                   state.Castles.Any(castle => castle.FactionId == "valdor");
        }

        // 현재 목표와 인접한 아군 성 중 전투단 전력이 가장 큰 성을 새 집결지로 지정합니다.
        private static void RefreshStrategicAssemblyCastle(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WIAutoStrategicPlan plan,
            WICastleRuntimeState target)
        {
            plan.AssemblyCastleId = target?.AdjacentCastleIds
                .Where(id => state.GetCastle(id)?.FactionId == state.PlayerFactionId)
                .OrderByDescending(id => state.Armies.Where(army => army.FactionId == state.PlayerFactionId &&
                                                                   army.CurrentCastleId == id)
                    .Sum(army => WIAdministrationTurnSystem.GetArmyBattlePower(database, state, army)))
                .ThenBy(id => id)
                .FirstOrDefault() ?? string.Empty;
        }

        // 손실 전투단이 편입 가능한 유휴 일반 인물이 있는 가장 가까운 아군 성을 찾습니다.
        private static WICastleRuntimeState FindNearestReinforcementCastle(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WIArmyState army)
        {
            int maximum = WIAdministrationTurnSystem.GetRecommendedArmySize(database, army);
            if (army.Members.Count >= maximum)
            {
                return null;
            }

            return state.Castles
                .Where(castle => castle.FactionId == state.PlayerFactionId)
                .Where(castle => castle.HeroIds.Any(heroId =>
                    state.GetCharacter(heroId)?.BaseGrade == WICharacterGrade.Common &&
                    IsReservedAdministrator(state, castle, heroId) == false &&
                    state.IsCharacterBusy(heroId) == false))
                .Select(castle => new
                {
                    Castle = castle,
                    Distance = GetFriendlyPathDistance(state, army.CurrentCastleId, castle.CastleId)
                })
                .Where(item => item.Distance < int.MaxValue)
                .OrderBy(item => item.Distance)
                .ThenByDescending(item => item.Castle.HeroIds.Count(heroId =>
                    state.GetCharacter(heroId)?.BaseGrade == WICharacterGrade.Common &&
                    IsReservedAdministrator(state, item.Castle, heroId) == false &&
                    state.IsCharacterBusy(heroId) == false))
                .ThenBy(item => item.Castle.CastleId)
                .Select(item => item.Castle)
                .FirstOrDefault();
        }

        // 같은 성의 전투단을 지휘관 권장 인원까지 합쳐 프리 시나리오의 분산 전력 붕괴를 막습니다.
        private static void ConsolidateArmiesAtSameCastle(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state)
        {
            List<WIArmyState> playerArmies = state.Armies.Where(army =>
                army.FactionId == state.PlayerFactionId && army.IsOperational).ToList();
            foreach (IGrouping<string, WIArmyState> group in playerArmies.GroupBy(army => army.CurrentCastleId))
            {
                WIArmyState primary = group.OrderByDescending(army => army.Members.Count).FirstOrDefault();
                if (primary == null)
                {
                    continue;
                }
                int maximum = WIAdministrationTurnSystem.GetRecommendedArmySize(database, primary);
                foreach (WIArmyState support in group.Where(army => army != primary).ToList())
                {
                    foreach (WIArmyMemberState member in support.Members.ToList())
                    {
                        if (primary.Members.Count >= maximum)
                        {
                            break;
                        }
                        support.Members.Remove(member);
                        if (WIAdministrationTurnSystem.AddArmyMember(
                                database, state, primary, member.HeroId, member.Role) == false)
                        {
                            support.Members.Add(member);
                        }
                    }
                    if (support.Members.Count == 0)
                    {
                        state.Armies.Remove(support);
                    }
                }
            }
        }

        // 아군 성만 통과해 가장 가까운 목적지 집합으로 가는 첫 이동 성을 반환합니다.
        private static string FindFriendlyStep(
            WIAdministrationState state,
            string originId,
            IEnumerable<string> destinationIds)
        {
            HashSet<string> destinations = new HashSet<string>(destinationIds);
            Queue<string> queue = new Queue<string>();
            Dictionary<string, string> previous = new Dictionary<string, string>();
            queue.Enqueue(originId);
            previous[originId] = string.Empty;
            string destination = string.Empty;
            while (queue.Count > 0)
            {
                string current = queue.Dequeue();
                if (current != originId && destinations.Contains(current))
                {
                    destination = current;
                    break;
                }
                WICastleRuntimeState castle = state.GetCastle(current);
                if (castle == null)
                {
                    continue;
                }
                foreach (string adjacentId in castle.AdjacentCastleIds)
                {
                    if (previous.ContainsKey(adjacentId) ||
                        state.GetCastle(adjacentId)?.FactionId != state.PlayerFactionId)
                    {
                        continue;
                    }
                    previous[adjacentId] = current;
                    queue.Enqueue(adjacentId);
                }
            }

            if (string.IsNullOrEmpty(destination))
            {
                return string.Empty;
            }
            string step = destination;
            while (previous[step] != originId && string.IsNullOrEmpty(previous[step]) == false)
            {
                step = previous[step];
            }
            return step;
        }

        // 아군 영토만 통과하는 두 성 사이의 최단 이동 개월을 계산합니다.
        private static int GetFriendlyPathDistance(
            WIAdministrationState state,
            string originId,
            string destinationId)
        {
            Queue<(string CastleId, int Distance)> queue = new Queue<(string CastleId, int Distance)>();
            HashSet<string> visited = new HashSet<string> { originId };
            queue.Enqueue((originId, 0));
            while (queue.Count > 0)
            {
                (string castleId, int distance) = queue.Dequeue();
                if (castleId == destinationId)
                {
                    return distance;
                }

                WICastleRuntimeState castle = state.GetCastle(castleId);
                if (castle == null)
                {
                    continue;
                }
                foreach (string adjacentId in castle.AdjacentCastleIds)
                {
                    if (visited.Add(adjacentId) == false ||
                        state.GetCastle(adjacentId)?.FactionId != state.PlayerFactionId)
                    {
                        continue;
                    }
                    queue.Enqueue((adjacentId, distance + 1));
                }
            }
            return int.MaxValue;
        }

        // 자동 플레이가 피로 누적 또는 현격한 열세에서 반복 원정하지 않도록 공격 가능성을 판정합니다.
        private static bool CanAutoAttack(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WIArmyState army,
            WICastleRuntimeState target,
            WIAutoPlayerPolicy policy,
            int requiredPercentOverride = 0)
        {
            float averageFatigue = army.Members.Count == 0 ? 100f : (float)army.Members
                .Average(member => state.GetCharacter(member.HeroId)?.Fatigue ?? 100);
            if (averageFatigue > 60f)
            {
                return false;
            }

            int attackPower = state.Armies.Where(ally =>
                    ally.FactionId == state.PlayerFactionId &&
                    ally.CurrentCastleId == army.CurrentCastleId &&
                    (ally.IsOperational || ally.IsMoving && ally.TargetCastleId == target.CastleId))
                .Sum(ally => WIAdministrationTurnSystem.GetArmyBattlePower(database, state, ally));
            int requiredPower = GetRequiredAttackPower(
                database, state, target, policy, army.CurrentCastleId, requiredPercentOverride);
            return attackPower >= requiredPower;
        }

        // 목표 성의 현재 수비와 정책별 안전선을 실제 필요한 공격 전력으로 변환합니다.
        private static int GetRequiredAttackPower(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WICastleRuntimeState target,
            WIAutoPlayerPolicy policy,
            string originCastleId = "",
            int requiredPercentOverride = 0)
        {
            if (target == null)
            {
                return int.MaxValue;
            }

            List<WIArmyState> defenders = state.Armies.Where(defender =>
                defender.FactionId == target.FactionId && defender.CurrentCastleId == target.CastleId &&
                defender.IsOperational).ToList();
            int defensePower = WIAdministrationTurnSystem.GetCastleDefensePower(database, state, target, defenders);
            // 서로의 성을 동시에 공격하면 회전 전투로 병합되므로 아군 출발 성으로 오는 적 전투단도 예상 수비 전력에 포함합니다.
            if (string.IsNullOrEmpty(originCastleId) == false)
            {
                defensePower += state.Armies.Where(enemy =>
                        enemy.FactionId == target.FactionId && enemy.IsOperational && enemy.IsMoving &&
                        enemy.TargetCastleId == originCastleId && defenders.Contains(enemy) == false)
                    .Sum(enemy => WIAdministrationTurnSystem.GetArmyBattlePower(database, state, enemy));
            }
            // 원정 명령 뒤 실제 전투가 열리기 전까지 수비 사업과 증원으로 전력이 변할 수 있으므로 안전 여유를 둡니다.
            bool firstExpansion = state.Castles.Count(castle =>
                castle.FactionId == state.PlayerFactionId) <= 1;
            bool longCampaign = state.Turn >= 180;
            int requiredPercent;
            if (firstExpansion)
            {
                requiredPercent = 80;
            }
            else if (policy == WIAutoPlayerPolicy.Administration)
            {
                requiredPercent = longCampaign ? 130 : 140;
            }
            else if (policy == WIAutoPlayerPolicy.Aggressive)
            {
                requiredPercent = longCampaign ? 115 : 120;
            }
            else
            {
                requiredPercent = longCampaign ? 110 : 120;
            }
            if (requiredPercentOverride > 0)
            {
                requiredPercent = requiredPercentOverride;
            }
            return Mathf.CeilToInt(defensePower * requiredPercent / 100f);
        }

        // 성의 대기 인물로 정책별 권장 규모까지 전투단원을 채웁니다.
        private static void FillArmy(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WICastleRuntimeState castle,
            WIArmyState army,
            WIAutoPlayerPolicy policy)
        {
            int maximum = WIAdministrationTurnSystem.GetRecommendedArmySize(database, army);
            bool firstExpansion = state.Castles.Count(item =>
                item.FactionId == state.PlayerFactionId) <= 1;
            int targetSize = policy == WIAutoPlayerPolicy.Administration && firstExpansion == false
                ? Mathf.Min(4, maximum)
                : maximum;
            WIUnitRole[] roles =
            {
                WIUnitRole.Vanguard, WIUnitRole.Ranged, WIUnitRole.Magic,
                WIUnitRole.Support, WIUnitRole.Melee
            };
            int roleIndex = 0;
            foreach (string heroId in castle.HeroIds
                         .Where(id => state.IsCharacterBusy(id) == false)
                         .Where(id => state.CampaignVariant == WICampaignVariant.AresMain || state.GetCharacter(id)?.BaseGrade == WICharacterGrade.Common)
                         .Where(id => IsReservedAdministrator(state, castle, id) == false)
                         .OrderBy(id => WIAdministrationTurnSystem.IsAdministrationCapable(state, id) ? 1 : 0)
                         .ThenByDescending(id => database.GetHero(id)?.Might ?? 0)
                         .ToList())
            {
                if (army.Members.Count >= targetSize)
                {
                    break;
                }
                WIAdministrationTurnSystem.AddArmyMember(
                    database, state, army, heroId, roles[roleIndex % roles.Length]);
                roleIndex += 1;
            }
        }

        // 성마다 영지관과 마지막 대기 내정 인물 한 명은 전투단 편입·전선 이동에서 보존합니다.
        private static bool IsReservedAdministrator(WIAdministrationState state, WICastleRuntimeState castle, string heroId)
        {
            // 메인에서는 마지막 대기 영입 담당자를 전선에 보내 후방 영입이 끊기지 않게 합니다.
            if (state.CampaignVariant == WICampaignVariant.AresMain &&
                WIAdministrationTurnSystem.CanRecruitTalent(state, heroId) &&
                castle.HeroIds.Count(id => state.IsCharacterBusy(id) == false &&
                    WIAdministrationTurnSystem.CanRecruitTalent(state, id)) <= 1)
            {
                return true;
            }
            if (heroId == castle.GovernorHeroId)
            {
                return true;
            }
            if (WIAdministrationTurnSystem.IsAdministrationCapable(state, heroId) == false)
            {
                return false;
            }
            return castle.HeroIds.Count(id => id != castle.GovernorHeroId &&
                state.IsCharacterBusy(id) == false &&
                WIAdministrationTurnSystem.IsAdministrationCapable(state, id)) <= 1;
        }

        // 플레이어 참가 대기 전투를 전력 스냅샷에 따른 전략 판정으로 모두 해소합니다.
        private static void ResolvePendingBattles(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WIAutoCampaignMetrics metrics,
            WIAutoStrategicPlan plan,
            int month,
            ref int lastBattleMonth)
        {
            foreach (WIBattleSessionState session in state.BattleSessions.Where(item =>
                         item.PlayerInvolved && item.Status != WIBattleSessionStatus.Resolved).ToList())
            {
                WIBattleOutcome attackerOutcome = ResolveStrategicBattleOutcome(state, session);
                int capturedBefore = state.Characters.Count(character => character.Captured);
                int defectedBefore = state.Characters.Count(character =>
                    string.IsNullOrEmpty(character.JoinedEnemyFactionId) == false);
                if (WIAdministrationTurnSystem.SubmitBattleResult(
                        database, state, session.SessionId, attackerOutcome,
                        WIBattleResolutionSource.StrategicFallback, state.LastMonthlyReport) == false)
                {
                    continue;
                }
                metrics.CharacterCaptures += Mathf.Max(0,
                    state.Characters.Count(character => character.Captured) - capturedBefore);
                metrics.CharacterDefections += Mathf.Max(0,
                    state.Characters.Count(character =>
                        string.IsNullOrEmpty(character.JoinedEnemyFactionId) == false) - defectedBefore);

                metrics.BattlesResolved += 1;
                if (session.AttackerFactionId == state.PlayerFactionId &&
                    session.AttackerArmyIds != null && session.AttackerArmyIds.Count >= 2)
                {
                    metrics.JointAttackBattles += 1;
                    metrics.RecordDecision(month, plan, "JOINT_ATTACK_RESOLVED",
                        $"{session.AttackerArmyIds.Count}개 전투단이 {session.CastleId} 공동 공격에 참가했습니다.");
                }
                metrics.BattleIntervals.Add(lastBattleMonth == 0 ? month : month - lastBattleMonth);
                lastBattleMonth = month;
                bool playerWon = session.AttackerFactionId == state.PlayerFactionId
                    ? attackerOutcome == WIBattleOutcome.Victory
                    : attackerOutcome == WIBattleOutcome.Defeat;
                int playerPower = session.AttackerFactionId == state.PlayerFactionId
                    ? session.AttackerPowerSnapshot : session.DefenderPowerSnapshot;
                int enemyPower = session.AttackerFactionId == state.PlayerFactionId
                    ? session.DefenderPowerSnapshot : session.AttackerPowerSnapshot;
                metrics.PlayerBattlePowerMargins.Add(playerPower - enemyPower);
                if (playerWon)
                {
                    metrics.PlayerVictories += 1;
                    plan.ConsecutiveDefeats = 0;
                    plan.RecoveryUntilTurn = 0;
                    if (session.AttackerFactionId == state.PlayerFactionId)
                    {
                        plan.SameTargetDefeats = 0;
                        plan.LastAttackTargetCastleId = string.Empty;
                    }
                }
                else
                {
                    metrics.PlayerDefeats += 1;
                    plan.LastDefeatTurn = state.Turn;
                    plan.ConsecutiveDefeats += 1;
                    plan.RecoveryUntilTurn = state.Turn + Mathf.Min(6, 2 + plan.ConsecutiveDefeats);
                    plan.Phase = WIAutoStrategicPhase.Recover;
                    metrics.RecordDecision(month, plan, "BATTLE_DEFEAT", $"패전 후 {plan.RecoveryUntilTurn - state.Turn}개월 회복 계획을 세웠습니다.");
                    if (session.AttackerFactionId == state.PlayerFactionId)
                    {
                        plan.SameTargetDefeats = plan.LastAttackTargetCastleId == session.CastleId
                            ? plan.SameTargetDefeats + 1 : 1;
                        plan.LastAttackTargetCastleId = session.CastleId;
                        if (plan.SameTargetDefeats >= 2)
                        {
                            plan.AvoidedTargetCastleId = session.CastleId;
                            plan.AvoidedTargetUntilTurn = state.Turn + 18;
                            plan.TargetCastleId = string.Empty;
                            plan.AssemblyCastleId = string.Empty;
                            plan.SameTargetDefeats = 0;
                            metrics.GoalsAbandoned += 1;
                            metrics.RecordDecision(month, plan, "GOAL_ABANDONED",
                                $"{session.CastleId} 연속 패배로 18개월 동안 다른 목표를 검토합니다.");
                        }
                    }
                }
            }
        }

        // 전략 자동 전투에 난이도별 전술 변동을 적용해 우세한 공격도 항상 확정 승리가 되지 않게 합니다.
        public static WIBattleOutcome ResolveStrategicBattleOutcome(
            WIAdministrationState state,
            WIBattleSessionState session)
        {
            if (state == null || session == null)
            {
                return WIBattleOutcome.Defeat;
            }

            int variation = state.Difficulty == WICampaignDifficulty.Relaxed ? 5 :
                state.Difficulty == WICampaignDifficulty.Hard ? 15 : 12;
            int attackerPercent = 100 + GetStableBattleVariation(state, session.SessionId, "attacker", variation);
            int defenderPercent = 100 + GetStableBattleVariation(state, session.SessionId, "defender", variation);
            int adjustedAttackerPower = session.AttackerPowerSnapshot * attackerPercent / 100;
            int adjustedDefenderPower = session.DefenderPowerSnapshot * defenderPercent / 100;
            bool attackerWon = adjustedAttackerPower > adjustedDefenderPower;
            bool closeBattle = session.AttackerPowerSnapshot * 100 <=
                               session.DefenderPowerSnapshot * 125;
            int setbackChance = state.Difficulty == WICampaignDifficulty.Relaxed ? 5 :
                state.Difficulty == WICampaignDifficulty.Hard ? 25 : 18;
            if (attackerWon && closeBattle &&
                GetStableBattleRoll(state, session.SessionId, "setback") < setbackChance)
            {
                attackerWon = false;
            }
            return attackerWon ? WIBattleOutcome.Victory : WIBattleOutcome.Defeat;
        }

        // 턴·전투 ID·진영 구분으로 저장 후에도 동일한 전술 변동값을 계산합니다.
        private static int GetStableBattleVariation(
            WIAdministrationState state,
            string sessionId,
            string side,
            int variation)
        {
            unchecked
            {
                int hash = state.Turn * 397 + state.SimulationSeed * 7919;
                string key = $"{sessionId}|{side}";
                foreach (char character in key)
                {
                    hash = hash * 31 + character;
                }
                int width = variation * 2 + 1;
                return Mathf.Abs(hash % width) - variation;
            }
        }

        // 전술적 패착 여부에 사용하는 재현 가능한 0~99 값을 반환합니다.
        private static int GetStableBattleRoll(
            WIAdministrationState state,
            string sessionId,
            string salt)
        {
            unchecked
            {
                int hash = state.Turn * 397 + state.SimulationSeed * 7919;
                string key = $"{sessionId}|{salt}";
                foreach (char character in key)
                {
                    hash = hash * 31 + character;
                }
                return (hash & int.MaxValue) % 100;
            }
        }

    }
}

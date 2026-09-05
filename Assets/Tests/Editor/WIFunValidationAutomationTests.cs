using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectWI.Administration;
using ProjectWI.Battle;
using UnityEditor;
using UnityEngine;

namespace ProjectWI.Tests.Editor
{
    public class WIFunValidationAutomationTests
    {
        private const string DatabasePath = "Assets/Data/ScriptableObject/Administration/WI_AdministrationDatabase.asset";

        // 세 자동 플레이 정책이 24·60·120개월 동안 대기 전투와 선택을 남기지 않는지 검증합니다.
        [TestCase(WIAutoPlayerPolicy.Administration, 24)]
        [TestCase(WIAutoPlayerPolicy.Administration, 60)]
        [TestCase(WIAutoPlayerPolicy.Administration, 120)]
        [TestCase(WIAutoPlayerPolicy.Balanced, 24)]
        [TestCase(WIAutoPlayerPolicy.Balanced, 60)]
        [TestCase(WIAutoPlayerPolicy.Balanced, 120)]
        [TestCase(WIAutoPlayerPolicy.Aggressive, 24)]
        [TestCase(WIAutoPlayerPolicy.Aggressive, 60)]
        [TestCase(WIAutoPlayerPolicy.Aggressive, 120)]
        public void AutoPlayerPolicy_LongRun_ResolvesBlockingDecisions(
            WIAutoPlayerPolicy policy,
            int months)
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);

            WIAutoCampaignMetrics metrics = WICampaignAutoPlayer.Run(database, state, policy, months);

            Assert.AreEqual(months, metrics.MonthsSimulated);
            Assert.AreEqual(0, metrics.RemainingPlayerBattles);
            Assert.AreEqual(0, metrics.RemainingDecisions,
                $"미결 선택: 사업 {state.PendingProjectEvents.Count}, 관계 {state.PendingRelationshipEvents.Count}, " +
                $"지역 {state.PendingRegionalEvents.Count}, 점령 {state.PendingOccupationEvents.Count}, " +
                $"영입 {state.PendingRecruitmentEvents.Count}, 유산 {state.PendingLegacyChoices.Count}");
            Assert.Greater(metrics.DecisionsResolved, 0);
            if (policy == WIAutoPlayerPolicy.Administration)
            {
                Assert.GreaterOrEqual(metrics.LongestNoMarchMonths, 1);
            }
            Assert.Greater(metrics.ArmiesCreated, 0);
            Assert.IsNotEmpty(metrics.DecisionTraces);
            TestContext.WriteLine(
                $"{policy}: 전투 {metrics.BattlesResolved}, 승/패 {metrics.PlayerVictories}/{metrics.PlayerDefeats}, " +
                $"원정 {metrics.MarchesStarted}, 점령 변화 {metrics.OwnershipChanges}, 선택 {metrics.DecisionsResolved}, " +
                $"전력차 [{string.Join(", ", metrics.PlayerBattlePowerMargins)}]");
            UnityEngine.Debug.Log(
                $"[FunValidation] {policy} {months}개월 · 전투 {metrics.BattlesResolved} · " +
                $"승/패 {metrics.PlayerVictories}/{metrics.PlayerDefeats} · 원정 {metrics.MarchesStarted} · " +
                $"영토 변화 {metrics.OwnershipChanges} · 플레이어 성 {metrics.FinalPlayerCastleCount} · " +
                $"결과 {metrics.CampaignResult} · 선택 {metrics.DecisionsResolved}");
        }

        // 정책별 장기 실행이 성장형 완전 방어와 공세형 조기 전멸의 양극단으로 돌아가지 않는지 검증합니다.
        [TestCase(WIAutoPlayerPolicy.Administration, 24)]
        [TestCase(WIAutoPlayerPolicy.Balanced, 60)]
        [TestCase(WIAutoPlayerPolicy.Aggressive, 24)]
        public void AutoPlayerPolicy_BalanceWindow_AvoidsEarlyCollapse(
            WIAutoPlayerPolicy policy,
            int months)
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);

            WIAutoCampaignMetrics metrics = WICampaignAutoPlayer.Run(database, state, policy, months);

            Assert.AreEqual(WICampaignResult.Ongoing, metrics.CampaignResult,
                $"{policy} 정책이 {months}개월 전에 멸망했습니다.");
            Assert.GreaterOrEqual(metrics.FinalPlayerCastleCount, 1);
            if (policy != WIAutoPlayerPolicy.Administration)
            {
                string armyStatus = string.Join(" | ", state.Armies
                    .Where(army => army.FactionId == state.PlayerFactionId)
                    .Select(army =>
                    {
                        WICastleRuntimeState origin = state.GetCastle(army.CurrentCastleId);
                        int armyPower = WIAdministrationTurnSystem.GetArmyBattlePower(database, state, army);
                        string targets = string.Join(",", origin?.AdjacentCastleIds
                            .Select(state.GetCastle)
                            .Where(target => target != null && target.FactionId != state.PlayerFactionId)
                            .Select(target =>
                            {
                                List<WIArmyState> defenders = state.Armies.Where(defender =>
                                    defender.FactionId == target.FactionId &&
                                    defender.CurrentCastleId == target.CastleId && defender.IsOperational).ToList();
                                int defensePower = WIAdministrationTurnSystem.GetCastleDefensePower(
                                    database, state, target, defenders);
                                bool atWar = WIAdministrationTurnSystem.AreFactionsAtWar(
                                    state, state.PlayerFactionId, target.FactionId);
                                return $"{target.CastleId}:{target.FactionId}:전쟁{atWar}:방어력{defensePower}";
                            }) ??
                            Enumerable.Empty<string>());
                        return $"{army.ArmyId}@{army.CurrentCastleId}:전력{armyPower}:인원{army.Members.Count}:숙련{army.Proficiency}/결속{army.CohesionExperience}:피로" +
                               $"{army.Members.Average(member => state.GetCharacter(member.HeroId)?.Fatigue ?? 100):0.0}:인접[{targets}]";
                    }));
                Assert.Greater(metrics.MarchesStarted, 0,
                    $"{policy} 정책이 원정을 시작하지 못했습니다. 전투단: {armyStatus}");
            }
        }

        // 설정된 장기 평화 시점에 인접 AI 사이의 두 번째 전선이 생성되는지 검증합니다.
        [Test]
        public void AIWarPressure_AtConfiguredInterval_CreatesSecondWarFront()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            state.Turn = database.AIWarPressureIntervalMonths;
            int originalWars = state.DiplomaticRelations.Count(relation =>
                relation.Status == WIDiplomaticStatus.War);

            bool created = WIAdministrationTurnSystem.EnsureAIWarPressure(
                database, state, new WITurnSummary());

            Assert.IsTrue(created);
            Assert.AreEqual(originalWars + 1, state.DiplomaticRelations.Count(relation =>
                relation.Status == WIDiplomaticStatus.War));
            Assert.GreaterOrEqual(state.DiplomaticRelations.Count(relation =>
                relation.Status == WIDiplomaticStatus.War), database.AIMinimumActiveWarFronts);
        }

        // 세 성향 모두 장기 목표를 유지하면서 실제 플레이 가능한 제한적 원정을 수행하는지 검증합니다.
        [TestCase(WIAutoPlayerPolicy.Administration)]
        [TestCase(WIAutoPlayerPolicy.Balanced)]
        [TestCase(WIAutoPlayerPolicy.Aggressive)]
        public void AutoPlayerPolicy_AllStylesCreateGoalAndMarch(WIAutoPlayerPolicy policy)
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(
                database, WICampaignDifficulty.Standard, WICampaignVariant.AresMain);

            WIAutoCampaignMetrics metrics = WICampaignAutoPlayer.Run(database, state, policy, 36);

            Assert.Greater(metrics.GoalChanges, 0);
            Assert.Greater(metrics.MarchesStarted, 0);
            Assert.IsTrue(metrics.DecisionTraces.Any(trace => trace.ReasonCode == "GOAL_SELECTED"));
            Assert.IsTrue(metrics.DecisionTraces.Any(trace => trace.ReasonCode == "MARCH_STARTED"));
        }

        // 피로가 높은 대기 영웅을 자동 휴식시키고 다음 인재 활동 담당자로 교대하는지 검증합니다.
        [Test]
        public void AutoPlayer_HighFatigueHeroUsesRestAndRecovers()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(
                database, WICampaignDifficulty.Standard, WICampaignVariant.AresMain);
            WICharacterRuntimeState ares = state.GetCharacter("ares");
            ares.Fatigue = 90;

            WIAutoCampaignMetrics metrics = WICampaignAutoPlayer.Run(
                database, state, WIAutoPlayerPolicy.Balanced, 1);

            Assert.GreaterOrEqual(metrics.RestActions, 1);
            Assert.LessOrEqual(ares.Fatigue, 45);
            Assert.IsTrue(metrics.DecisionTraces.Any(trace => trace.ReasonCode == "HERO_REST"));
        }

        // 자동 플레이가 재야 인재를 무한 영입하지 않고 보유 성에 필요한 목표 인원에서 멈추는지 검증합니다.
        [Test]
        public void AutoPlayer_RecruitmentStopsAtTerritoryRosterTarget()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(
                database, WICampaignDifficulty.Standard, WICampaignVariant.AresMain);

            WIAutoCampaignMetrics metrics = WICampaignAutoPlayer.Run(
                database, state, WIAutoPlayerPolicy.Balanced, 120);

            int targetRosterSize = 14 + Mathf.Max(0, metrics.FinalPlayerCastleCount - 1) * 8;
            Assert.LessOrEqual(metrics.FinalEmployedCharacters, targetRosterSize + 1);
            Assert.IsTrue(metrics.DecisionTraces.Any(trace => trace.ReasonCode == "ROSTER_TARGET_MET"));
        }

        // 손실된 전투단이 회복 기간에 단순 대기하지 않고 같은 성의 대기 병력으로 보충되는지 검증합니다.
        [Test]
        public void AutoPlayer_UndersizedArmyReceivesAvailableReinforcements()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(
                database, WICampaignDifficulty.Standard, WICampaignVariant.AresMain);
            WICastleRuntimeState frosthorn = state.GetCastle("castle_28");
            WIArmyState army = WIAdministrationTurnSystem.CreateArmy(database, state, frosthorn, "ares");
            Assert.IsNotNull(army);
            Assert.AreEqual(1, army.Members.Count);

            WIAutoCampaignMetrics metrics = WICampaignAutoPlayer.Run(
                database, state, WIAutoPlayerPolicy.Balanced, 1);

            Assert.Greater(army.Members.Count, 1);
            Assert.Greater(metrics.ArmyReinforcements, 0);
            Assert.IsTrue(metrics.DecisionTraces.Any(trace => trace.ReasonCode == "ARMY_REINFORCED"));
        }

        // 개선된 공세형이 장기 실행에서 과거의 압도적인 반복 패배 상태로 돌아가지 않는지 검증합니다.
        [Test]
        public void AutoPlayer_AggressiveLongRunAvoidsRepeatedDefeatSpiral()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(
                database, WICampaignDifficulty.Standard, WICampaignVariant.AresMain);

            WIAutoCampaignMetrics metrics = WICampaignAutoPlayer.Run(
                database, state, WIAutoPlayerPolicy.Aggressive, 240);

            Assert.GreaterOrEqual(metrics.FinalPlayerCastleCount, 4);
            Assert.LessOrEqual(metrics.PlayerDefeats, metrics.PlayerVictories + 8);
        }

        // 신규 AI 전선이 실제 전투단 이동으로 이어지는 진영과 시점을 기록합니다.
        [Test]
        public void AIFrontActivation_ThirtySixMonths_RecordsMovementByFaction()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            HashSet<string> movingFactions = new HashSet<string>();
            for (int month = 1; month <= 36; month += 1)
            {
                WIAdministrationTurnSystem.ExecuteTurn(database, state);
                foreach (WIArmyState army in state.Armies.Where(item => item.IsMoving))
                {
                    movingFactions.Add(army.FactionId);
                    TestContext.WriteLine(
                        $"{month}개월 · {army.FactionId} · {army.CurrentCastleId} → {army.TargetCastleId}");
                }
            }

            string wars = string.Join(", ", state.DiplomaticRelations
                .Where(relation => relation.Status == WIDiplomaticStatus.War)
                .Select(relation => $"{relation.FirstFactionId}-{relation.SecondFactionId}"));
            string armies = string.Join(" | ", state.Armies.Select(army =>
                $"{army.FactionId}:{army.CurrentCastleId}:{army.Mission}:{army.IsOperational}"));
            TestContext.WriteLine($"이동 진영: {string.Join(", ", movingFactions)} · 전쟁: {wars}");
            Assert.GreaterOrEqual(state.DiplomaticRelations.Count(relation =>
                relation.Status == WIDiplomaticStatus.War), database.AIMinimumActiveWarFronts);
            Assert.GreaterOrEqual(movingFactions.Count, 2,
                $"신규 전쟁은 생겼지만 이동 진영은 {string.Join(", ", movingFactions)}뿐입니다. 전쟁: {wars} · 전투단: {armies}");
        }

        // 사업 선택 사건이 UI 없이도 공용 전략 로직을 통해 해결되는지 검증합니다.
        [Test]
        public void ProjectEvent_PublicResolver_AppliesResultWithoutUI()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            WICastleRuntimeState castle = state.Castles.First(item => item.FactionId == state.PlayerFactionId);
            WIPendingProjectEvent pending = new WIPendingProjectEvent
            {
                CastleId = castle.CastleId,
                ProjectType = WICastleProjectType.Prosperity,
                Title = "자동 검증 사건"
            };
            state.PendingProjectEvents.Add(pending);
            int original = castle.Prosperity;

            bool resolved = WIAdministrationTurnSystem.ResolveProjectEvent(
                state, pending, 4, false, new WITurnSummary());

            Assert.IsTrue(resolved);
            Assert.AreEqual(original + 4, castle.Prosperity);
            Assert.IsEmpty(state.PendingProjectEvents);
        }

        // 세 전투 목표가 데이터로 분리되고 제한 방어와 거점 점령의 별도 승리 조건이 작동하는지 검증합니다.
        [Test]
        public void BattleObjectives_ThreeTypes_HaveExecutableVictoryRules()
        {
            WIBattleConfigSO config = AssetDatabase.LoadAssetAtPath<WIBattleConfigSO>(
                "Assets/Data/ScriptableObject/Battle/WI_BattleConfig.asset");
            Assert.AreEqual(3, config.BattleObjectives.Count);
            CollectionAssert.AreEquivalent(
                new[] { WIBattleObjectiveType.Elimination, WIBattleObjectiveType.TimedDefense, WIBattleObjectiveType.ControlPoint },
                config.BattleObjectives.Select(item => item.ObjectiveType));
            Assert.AreEqual(3, new[] { "battle_1", "battle_2", "battle_3" }
                .Select(id => config.SelectBattleObjective(id).Id).Distinct().Count());

            WIBattleRuntimeState defense = new WIBattleRuntimeState
            {
                ObjectiveType = WIBattleObjectiveType.TimedDefense,
                ObjectiveDurationSeconds = 1f
            };
            Assert.AreEqual(WIBattleOutcome.Defeat, WIBattleSimulation.Step(config, defense, 1f));

            WIBattleRuntimeState control = new WIBattleRuntimeState
            {
                ObjectiveType = WIBattleObjectiveType.ControlPoint,
                ControlDurationSeconds = 1f,
                ControlRadius = 2f,
                AttackerCommand = WIBattleCommand.Hold,
                DefenderCommand = WIBattleCommand.Hold
            };
            control.Characters.Add(new WIBattleCharacterState
            {
                HeroId = "objective_attacker", Side = WIBattleSide.Attacker, Position = Vector2.zero,
                FormationPosition = Vector2.zero, Health = 100, AttackRange = 0.1f, MoveSpeed = 0f
            });
            control.Characters.Add(new WIBattleCharacterState
            {
                HeroId = "objective_defender", Side = WIBattleSide.Defender, Position = new Vector2(5f, 0f),
                FormationPosition = new Vector2(5f, 0f), Health = 100, AttackRange = 0.1f, MoveSpeed = 0f
            });
            Assert.AreEqual(WIBattleOutcome.None, WIBattleSimulation.Step(config, control, 0.5f));
            Assert.AreEqual(WIBattleOutcome.Victory, WIBattleSimulation.Step(config, control, 0.5f));
        }

        // 첫 12개월의 선택 사건이 한 달에 몰리지 않고 여러 달에 걸쳐 발생하는지 자동 검증합니다.
        [Test]
        public void EarlyEventDensity_FirstTwelveMonths_RemainsReadableAndActive()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);

            WIAutoCampaignMetrics metrics = WICampaignAutoPlayer.Run(
                database, state, WIAutoPlayerPolicy.Balanced, 12);

            Assert.AreEqual(12, metrics.EarlyDecisionCountsByMonth.Count);
            Assert.GreaterOrEqual(metrics.EarlyDecisionCountsByMonth.Sum(), 2);
            Assert.LessOrEqual(metrics.EarlyDecisionCountsByMonth.Max(), 3);
            Assert.GreaterOrEqual(metrics.EarlyDecisionCountsByMonth.Count(count => count > 0), 2);
            Assert.AreEqual(0, metrics.RemainingDecisions);
            TestContext.WriteLine($"초반 월별 선택 사건: {string.Join(", ", metrics.EarlyDecisionCountsByMonth)}");
        }
    }
}

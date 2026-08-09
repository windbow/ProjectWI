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
            Assert.AreEqual(0, metrics.RemainingDecisions);
            Assert.Greater(metrics.DecisionsResolved, 0);
            if (policy == WIAutoPlayerPolicy.Administration)
            {
                Assert.AreEqual(0, metrics.ArmiesCreated);
            }
            else
            {
                Assert.Greater(metrics.ArmiesCreated, 0);
            }
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
                Assert.Greater(metrics.MarchesStarted, 0, $"{policy} 정책이 원정을 시작하지 못했습니다.");
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
            Assert.LessOrEqual(metrics.EarlyDecisionCountsByMonth.Max(), 2);
            Assert.GreaterOrEqual(metrics.EarlyDecisionCountsByMonth.Count(count => count > 0), 2);
            Assert.AreEqual(0, metrics.RemainingDecisions);
            TestContext.WriteLine($"초반 월별 선택 사건: {string.Join(", ", metrics.EarlyDecisionCountsByMonth)}");
        }
    }
}

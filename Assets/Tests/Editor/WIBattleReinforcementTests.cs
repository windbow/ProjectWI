using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ProjectWI.Administration;
using ProjectWI.Battle;

namespace ProjectWI.Tests.Editor
{
    public class WIBattleReinforcementTests
    {
        // 선발대는 도착했고 후발대는 한 달 남은 메인 캠페인 전장을 구성합니다.
        private static WIAdministrationState Prepare(bool defending, out WIAdministrationDatabaseSO database,
            out WIBattleConfigSO config, out WIArmyState reinforcement, out WIBattleSessionState session)
        {
            database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>("Assets/Data/ScriptableObject/Administration/WI_AdministrationDatabase.asset");
            config = AssetDatabase.LoadAssetAtPath<WIBattleConfigSO>("Assets/Data/ScriptableObject/Battle/WI_BattleConfig.asset");
            var state = WIAdministrationState.Create(database, WICampaignDifficulty.Standard, WICampaignVariant.AresMain);
            var origin = state.GetCastle("castle_28");
            var first = WIAdministrationTurnSystem.CreateArmy(database, state, origin, "ares");
            Assert.IsTrue(WIAdministrationTurnSystem.BeginArmyMarch(database, state, first, "castle_04"));
            first.RemainingTravelMonths = 1;
            var rear = defending == true ? state.Castles.First(item => item.FactionId == "valdor" && item.CastleId != "castle_04" && item.HeroIds.Count > 0) : origin;
            if (rear.AdjacentCastleIds.Contains("castle_04") == false)
            {
                rear.AdjacentCastleIds.Add("castle_04");
            }
            string hero = defending == true ? rear.HeroIds.First(id => state.IsCharacterBusy(id) == false) : "lyria";
            reinforcement = WIAdministrationTurnSystem.CreateArmy(database, state, rear, hero);
            Assert.IsTrue(WIAdministrationTurnSystem.BeginArmyMarch(database, state, reinforcement, "castle_04"));
            reinforcement.RemainingTravelMonths = 2;
            WIAdministrationTurnSystem.ExecuteTurn(database, state);
            session = state.BattleSessions.Single(item => item.CastleId == "castle_04");
            Assert.AreEqual(1, reinforcement.RemainingTravelMonths);
            return state;
        }

        // 다음 월에 도착한 공격·수비 증원이 별도 세션 없이 기존 전투에 합류합니다.
        [TestCase(false)]
        [TestCase(true)]
        public void PendingBattleAcceptsLaterArrival(bool defending)
        {
            var state = Prepare(defending, out var database, out _, out var army, out var session);
            WIAdministrationTurnSystem.ExecuteTurn(database, state);
            Assert.AreEqual(1, state.BattleSessions.Count(item => item.CastleId == session.CastleId));
            Assert.Contains(army.ArmyId, defending == true ? session.DefenderArmyIds : session.AttackerArmyIds);
            Assert.IsTrue(army.AwaitingBattle);
            Assert.IsFalse(WIAdministrationTurnSystem.JoinBattleReinforcement(database, state, session, army));
        }

        // 전투 중 합류는 예약 시간에 한 번만 발생하고 기존 피해와 위치를 보존하며 결과 처리에 포함됩니다.
        [TestCase(false, WIBattleOutcome.Victory)]
        [TestCase(false, WIBattleOutcome.Defeat)]
        [TestCase(true, WIBattleOutcome.Victory)]
        [TestCase(true, WIBattleOutcome.Defeat)]
        public void TimedReinforcementPreservesActorsAndReceivesOutcome(bool defending, WIBattleOutcome outcome)
        {
            var state = Prepare(defending, out var database, out var config, out var army, out var session);
            Assert.IsTrue(WIAdministrationTurnSystem.BeginRealTimeBattle(state, session.SessionId));
            var runtime = WIBattleRuntimeBuilder.Build(config, database, session);
            var system = new WIBattleReinforcementSystem(config, database, state, session);
            var actor = runtime.Characters[0];
            actor.Health = 13;
            actor.SkillCooldownRemaining = 7f;
            var position = actor.Position;
            int count = runtime.Characters.Count;
            runtime.ElapsedSeconds = config.ReinforcementSecondsPerMonth - 0.01f;
            system.Advance(config, runtime);
            Assert.AreEqual(count, runtime.Characters.Count);
            runtime.ElapsedSeconds = config.ReinforcementSecondsPerMonth;
            system.Advance(config, runtime);
            system.Advance(config, runtime);
            Assert.AreEqual(count + army.Members.Count, runtime.Characters.Count);
            Assert.AreEqual(13, actor.Health);
            Assert.AreEqual(7f, actor.SkillCooldownRemaining);
            Assert.AreEqual(position, actor.Position);
            Assert.IsFalse(army.IsMoving);
            Assert.IsTrue(army.AwaitingBattle);
            Assert.IsTrue(runtime.Characters.Where(item => item.ArmyId == army.ArmyId).All(item =>
                item.Side == (defending == true ? WIBattleSide.Defender : WIBattleSide.Attacker)));
            Assert.AreEqual(runtime.Characters.Count, runtime.Characters.Select(item => item.HeroId).Distinct().Count());
            Assert.IsTrue(WIAdministrationTurnSystem.SubmitBattleResult(database, state, session.SessionId, outcome,
                WIBattleResolutionSource.RealTimeBattle, new WITurnSummary()));
            Assert.IsFalse(army.AwaitingBattle);
            Assert.IsFalse(army.IsMoving);
        }

        // 먼저 끝난 전투는 늦은 증원을 이동시키거나 피해 대상으로 넣지 않습니다.
        [Test]
        public void FinishedBattleLeavesIncomingArmyUntouched()
        {
            var state = Prepare(false, out var database, out var config, out var army, out var session);
            var runtime = WIBattleRuntimeBuilder.Build(config, database, session);
            var system = new WIBattleReinforcementSystem(config, database, state, session);
            string before = JsonUtility.ToJson(army);
            runtime.Finished = true;
            runtime.ElapsedSeconds = 100f;
            system.Advance(config, runtime);
            Assert.AreEqual(before, JsonUtility.ToJson(army));
            Assert.IsFalse(session.AttackerArmyIds.Contains(army.ArmyId));
        }

        // 실시간 컨트롤러도 추가된 인물에 기존 캐릭터 프리팹 표시를 생성합니다.
        [Test]
        public void ControllerCreatesViewsForArrivingArmy()
        {
            var state = Prepare(false, out var database, out var config, out _, out var session);
            WIAdministrationTurnSystem.BeginRealTimeBattle(state, session.SessionId);
            var runtime = WIBattleRuntimeBuilder.Build(config, database, session);
            runtime.AttackerCommand = WIBattleCommand.Hold;
            runtime.DefenderCommand = WIBattleCommand.Hold;
            runtime.ElapsedSeconds = config.ReinforcementSecondsPerMonth;
            var host = new GameObject("WIReinforcementTest");
            host.SetActive(false);
            try
            {
                var controller = host.AddComponent<WIBattleRuntimeController>();
                var type = typeof(WIBattleRuntimeController);
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                type.GetField("config", flags).SetValue(controller, config);
                type.GetField("runtime", flags).SetValue(controller, runtime);
                type.GetField("battleDatabase", flags).SetValue(controller, database);
                type.GetField("reinforcements", flags).SetValue(controller, new WIBattleReinforcementSystem(config, database, state, session));
                type.GetField("characterPrefab", flags).SetValue(controller,
                    AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Battle/WIBattleCharacter.prefab").GetComponent<WIBattleCharacterView>());
                controller.SimulateStep(0.01f);
                Assert.AreEqual(runtime.Characters.Count, host.GetComponentsInChildren<WIBattleCharacterView>(true).Length);
                Assert.IsTrue(controller.ReinforcementStatus.Contains("증원 합류"));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }
    }
}

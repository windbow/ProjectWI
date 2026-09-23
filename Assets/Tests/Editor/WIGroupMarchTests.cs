using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ProjectWI.Administration;
using ProjectWI.Battle;

namespace ProjectWI.Tests.Editor
{
    public class WIGroupMarchTests
    {
        // 실제 메인 데이터로 독립된 검증 상태와 두 전투단을 준비합니다.
        private static WIAdministrationState Prepare(out WIAdministrationDatabaseSO database, out WIArmyState first, out WIArmyState second)
        {
            database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>("Assets/Data/ScriptableObject/Administration/WI_AdministrationDatabase.asset");
            var state = WIAdministrationState.Create(database, WICampaignDifficulty.Standard, WICampaignVariant.AresMain);
            var castle = state.GetCastle("castle_28");
            // 초기 배치 밸런스와 독립적으로 합동 출정 및 스크롤 검사용 인원을 구성합니다.
            foreach (string id in new[] { "hero_009", "hero_010", "common_013", "common_014", "common_015", "common_016", "common_017", "common_018", "common_019", "common_020" })
            {
                foreach (var origin in state.Castles)
                {
                    origin.HeroIds.Remove(id);
                }
                state.GetCharacter(id).Recruited = true;
                castle.HeroIds.Add(id);
            }
            first = WIAdministrationTurnSystem.CreateArmy(database, state, castle, "ares");
            second = WIAdministrationTurnSystem.CreateArmy(database, state, castle, "hero_009");
            WIAdministrationTurnSystem.AddArmyMember(database, state, first, "lyria", WIUnitRole.Magic);
            WIAdministrationTurnSystem.AddArmyMember(database, state, first, "common_013", WIUnitRole.Melee);
            WIAdministrationTurnSystem.AddArmyMember(database, state, second, "hero_010", WIUnitRole.Melee);
            WIAdministrationTurnSystem.AddArmyMember(database, state, second, "common_014", WIUnitRole.Melee);
            state.GetPlayerFactionState().Influence = 200;
            return state;
        }

        // 함께 출정한 두 전투단의 여섯 인물이 하나의 전투에 생성되고 점령 후 모두 주둔합니다.
        [Test]
        public void TwoArmiesJoinOneBattleAndOccupation()
        {
            var state = Prepare(out var database, out var first, out var second);
            Assert.IsTrue(WIAdministrationTurnSystem.BeginArmyGroupMarch(database, state, new[] { first.ArmyId, second.ArmyId }, "castle_04"));
            Assert.AreEqual(160, state.GetPlayerFactionState().Influence);
            Assert.AreEqual(first.RemainingTravelMonths, second.RemainingTravelMonths);
            for (int month = 0; month < 3 && state.BattleSessions.Count == 0; month++)
            {
                WIAdministrationTurnSystem.ExecuteTurn(database, state);
            }
            var battle = state.BattleSessions.Single(item => item.CastleId == "castle_04");
            Assert.AreEqual(2, battle.AttackerArmyIds.Count);
            Assert.AreEqual(6, battle.AttackerHeroIds.Count);
            Assert.AreEqual(6, battle.AttackerHeroIds.Select(item => item.HeroId).Distinct().Count());
            var config = AssetDatabase.LoadAssetAtPath<WIBattleConfigSO>("Assets/Data/ScriptableObject/Battle/WI_BattleConfig.asset");
            Assert.AreEqual(6, WIBattleRuntimeBuilder.Build(config, database, battle).Characters.Count(item => item.Side == WIBattleSide.Attacker));
            Assert.IsTrue(WIAdministrationTurnSystem.SubmitBattleResult(database, state, battle.SessionId,
                WIBattleOutcome.Victory, WIBattleResolutionSource.RealTimeBattle, new WITurnSummary()));
            Assert.IsFalse(first.AwaitingBattle);
            Assert.IsFalse(second.AwaitingBattle);
            Assert.IsTrue(first.Members.Concat(second.Members).All(item => state.GetCastle("castle_04").HeroIds.Contains(item.HeroId)));
            Assert.AreEqual(0, first.ReorganizationMonths);
            Assert.AreEqual(0, second.ReorganizationMonths);
            int turn = state.Turn;
            Assert.IsTrue(WIAdministrationTurnSystem.BeginArmyGroupMarch(database, state,
                new[] { first.ArmyId, second.ArmyId }, "castle_28"));
            Assert.AreEqual(turn, state.Turn);
            Assert.AreEqual(6, first.Members.Count + second.Members.Count);
        }

        // 개별 출정한 두 전투단도 같은 달 도착 시 하나의 전투만 만들고 승리 후 재시작하지 않습니다.
        [Test]
        public void SeparateMarchesShareBattleAndCannotRestartAfterOccupation()
        {
            var state = Prepare(out var database, out var first, out var second);
            Assert.IsTrue(WIAdministrationTurnSystem.BeginArmyMarch(database, state, first, "castle_04"));
            Assert.IsTrue(WIAdministrationTurnSystem.BeginArmyMarch(database, state, second, "castle_04"));
            first.RemainingTravelMonths = 1;
            second.RemainingTravelMonths = 1;
            WIAdministrationTurnSystem.ExecuteTurn(database, state);
            var battles = state.BattleSessions.Where(item => item.CastleId == "castle_04").ToList();
            Assert.AreEqual(1, battles.Count);
            var battle = battles[0];
            Assert.Contains(second.ArmyId, battle.AttackerArmyIds);
            Assert.IsTrue(WIAdministrationTurnSystem.BeginRealTimeBattle(state, battle.SessionId));
            Assert.IsTrue(WIAdministrationTurnSystem.SubmitBattleResult(database, state, battle.SessionId,
                WIBattleOutcome.Victory, WIBattleResolutionSource.RealTimeBattle, new WITurnSummary()));
            Assert.AreEqual(first.FactionId, state.GetCastle("castle_04").FactionId);
            Assert.IsFalse(second.AwaitingBattle);
            string after = JsonUtility.ToJson(state);
            Assert.IsFalse(WIAdministrationTurnSystem.BeginRealTimeBattle(state, battle.SessionId));
            Assert.AreEqual(after, JsonUtility.ToJson(state));
        }
        // 이전 저장에 점령 후 대기 기록이 남아 있어도 실제 전투 진입과 화면의 시작 버튼을 차단합니다.
        [Test]
        public void OccupiedCastleRejectsStalePendingBattle()
        {
            var state = Prepare(out var database, out var first, out var second);
            first.CurrentCastleId = "castle_04";
            first.AwaitingBattle = true;
            var battle = WIAdministrationTurnSystem.CreateBattleSession(database, state, first);
            state.GetCastle("castle_04").FactionId = first.FactionId;
            string before = JsonUtility.ToJson(state);
            Assert.IsFalse(WIAdministrationTurnSystem.BeginRealTimeBattle(state, battle.SessionId));
            Assert.AreEqual(before, JsonUtility.ToJson(state));
            var host = new GameObject("WIStaleBattleTest");
            host.SetActive(false);
            try
            {
                var controller = host.AddComponent<WIAdministrationUIController>();
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(WIAdministrationUIController).GetField("state", flags).SetValue(controller, state);
                typeof(WIAdministrationUIController).GetField("database", flags).SetValue(controller, database);
                Assert.IsTrue(controller.TryGetUGUIMilitaryPanel("battle", battle.SessionId, 0, out var panel, out _));
                Assert.IsFalse(panel.Items.Single(item => item.Kind == "start-battle").Interactable);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }
        // 비용·대기 상태·출발 성 중 하나라도 잘못되면 전체 명령이 아무 상태도 변경하지 않습니다.
        [TestCase("cost")]
        [TestCase("busy")]
        [TestCase("origin")]
        [TestCase("missing")]
        public void InvalidGroupDoesNotPartiallyMarch(string failure)
        {
            var state = Prepare(out var database, out var first, out var second);
            if (failure == "cost")
            {
                state.GetPlayerFactionState().Influence = 20;
            }
            if (failure == "busy")
            {
                second.ReorganizationMonths = 1;
            }
            if (failure == "origin")
            {
                second.CurrentCastleId = "castle_03";
            }
            var ids = new[] { first.ArmyId, failure == "missing" ? "missing" : second.ArmyId };
            string before = JsonUtility.ToJson(state);
            Assert.IsFalse(WIAdministrationTurnSystem.BeginArmyGroupMarch(database, state, ids, "castle_04"));
            Assert.AreEqual(before, JsonUtility.ToJson(state));
        }

        // 스크롤 목록에서 떨어진 두 행을 선택한 전투단이 함께 출정하는 화면 흐름을 검증합니다.
        [Test]
        public void ScrollRowsKeepSelectionsAndMarchTogether()
        {
            var state = Prepare(out var database, out var first, out var second);
            var castle = state.GetCastle("castle_28");
            foreach (string id in castle.HeroIds.Where(id => state.IsCharacterBusy(id) == false).Take(6).ToList())
            {
                WIAdministrationTurnSystem.CreateArmy(database, state, castle, id);
            }
            var host = new GameObject("WIGroupMarchTest");
            host.SetActive(false);
            try
            {
                var controller = host.AddComponent<WIAdministrationUIController>();
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(WIAdministrationUIController).GetField("state", flags).SetValue(controller, state);
                typeof(WIAdministrationUIController).GetField("database", flags).SetValue(controller, database);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Administration/WIAdministrationMilitaryUGUI.prefab");
                var instance = Object.Instantiate(prefab, host.transform);
                var panel = instance.GetComponent<WIAdministrationMilitaryUGUIController>();
                typeof(WIAdministrationUGUIPanelController).GetField("administrationController", flags).SetValue(panel, controller);
                var type = typeof(WIAdministrationMilitaryUGUIController);
                type.GetField("marchTargetId", flags).SetValue(panel, "castle_04");
                type.GetMethod("Push", flags).Invoke(panel, new object[] { "march-armies", first.ArmyId, 0 });
                var click = type.GetMethod("OpenItem", flags);
                click.Invoke(panel, new object[] { 0 });
                click.Invoke(panel, new object[] { 6 });
                type.GetMethod("HandleFooter", flags).Invoke(panel, null);
                Assert.AreEqual(2, state.Armies.Count(item => item.IsMoving));
                Assert.IsTrue(first.IsMoving);
                Assert.IsTrue(state.Armies[6].IsMoving);
                Assert.AreEqual(160, state.GetPlayerFactionState().Influence);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }
    }
}

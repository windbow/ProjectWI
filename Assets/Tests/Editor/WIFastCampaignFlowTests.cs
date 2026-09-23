using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ProjectWI.Administration;
using ProjectWI.Systems;
using UnityEditor;
using UnityEngine;

namespace ProjectWI.Tests.Editor
{
    public class WIFastCampaignFlowTests
    {
        // 원본 에셋을 변경하지 않는 데이터 사본과 독립 캠페인 상태입니다.
        private WIAdministrationDatabaseSO database;
        private WIAdministrationState state;
        private WICastleRuntimeState home;

        // 실제 마스터 데이터에서 검증용 캠페인과 기본 내정 방침을 준비합니다.
        [SetUp]
        public void SetUp()
        {
            database = Object.Instantiate(AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(
                "Assets/Data/ScriptableObject/Administration/WI_AdministrationDatabase.asset"));
            state = WIAdministrationState.Create(database);
            home = state.GetCastle("castle_00");
            home.DelegatedToGovernor = true;
            home.GovernorPolicy = WIGovernorPolicy.Prosperity;
            home.GovernorMonthlyBudget = database.ProjectBalance.BasicCost;
            home.Prosperity = 10;
            state.Gold = 10000;
        }

        // 검증에 사용한 데이터 사본만 해제합니다.
        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(database);
        }

        // 월간 처리의 실제 내정 단계를 호출해 다른 진영의 행동과 분리해서 검증합니다.
        private WITurnSummary RunAdministration()
        {
            var summary = new WITurnSummary();
            InvokeTurnStep("AssignDelegatedProject", home, summary);
            InvokeTurnStep("ResolveCastleProject", home, summary);
            return summary;
        }

        // 실제 월간 처리 메서드를 동일한 런타임 상태에 실행합니다.
        private void InvokeTurnStep(string method, params object[] tail)
        {
            object[] args = new object[] { database, state }.Concat(tail).ToArray();
            typeof(WIAdministrationTurnSystem).GetMethod(method,
                BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, args);
        }

        // 영지관도 거주 인물도 없는 성이 예산으로 성장하고 통상 선택 창을 쌓지 않습니다.
        [Test]
        public void EmptyCastle_ContinuesBudgetedAdministration()
        {
            home.GovernorHeroId = string.Empty;
            home.HeroIds.Clear();
            int gold = state.Gold;
            var summary = RunAdministration();
            Assert.Greater(home.Prosperity, 10);
            Assert.AreEqual(database.ProjectBalance.BasicCost, gold - state.Gold);
            Assert.AreEqual(database.ProjectBalance.BasicCost, summary.GoldSpent);
            Assert.AreEqual(2, summary.DelegationReports.Count);
            Assert.IsNull(home.ActiveProject);
            Assert.IsEmpty(state.PendingProjectEvents);
            Assert.IsEmpty(state.PendingLegacyChoices);
            StringAssert.Contains(database.GetText("UI_ADMIN_BASIC_OPERATION"),
                WIAdministrationTurnSystem.GetDelegationPreview(database, state, home));
        }

        // 실제 출정과 턴 진행 후에도 출발 성의 방침·예산·성장이 유지됩니다.
        [Test]
        public void GovernorMarches_HomeKeepsOperating()
        {
            home.GovernorHeroId = "ares";
            var army = WIAdministrationTurnSystem.CreateArmy(database, state, home, "ares");
            Assert.IsNotNull(army);
            Assert.IsTrue(WIAdministrationTurnSystem.BeginArmyMarch(database, state, army, "castle_01"));
            Assert.AreEqual("ares", home.GovernorHeroId);
            WIAdministrationTurnSystem.ExecuteTurn(database, state);
            Assert.IsTrue(home.DelegatedToGovernor);
            Assert.AreEqual(WIGovernorPolicy.Prosperity, home.GovernorPolicy);
            Assert.AreEqual(database.ProjectBalance.BasicCost, home.GovernorMonthlyBudget);
            Assert.Greater(home.Prosperity, 10);
            Assert.AreEqual("ares", army.Members.Single().HeroId);
        }

        // 다른 활동 중인 영지관의 보너스를 중복 적용하지 않고 기본 성과만 제공합니다.
        [Test]
        public void BusyGovernor_UsesSameGainAsEmptyCastle()
        {
            home.GovernorHeroId = string.Empty;
            RunAdministration();
            int baseline = home.Prosperity;
            home.Prosperity = 10;
            home.GovernorHeroId = "ares";
            state.GetCharacter("ares").Activity = WICharacterActivityType.Training;
            RunAdministration();
            Assert.AreEqual(baseline, home.Prosperity);
            Assert.AreEqual(WICharacterActivityType.Training, state.GetCharacter("ares").Activity);
        }

        // 자금 부족·목표 완료·직접 관리 상태에서는 자동 지출이 발생하지 않습니다.
        [TestCase("funds")]
        [TestCase("complete")]
        [TestCase("disabled")]
        public void Administration_DoesNotSpendWithoutUsefulAuthorizedWork(string condition)
        {
            home.GovernorHeroId = string.Empty;
            if (condition == "funds")
            {
                state.Gold = database.ProjectBalance.BasicCost - 1;
            }
            if (condition == "complete")
            {
                home.Prosperity = home.Technology = home.Stability = home.Defense = 100;
            }
            if (condition == "disabled")
            {
                home.DelegatedToGovernor = false;
            }
            int before = state.Gold;
            Assert.AreEqual(0, RunAdministration().GoldSpent);
            Assert.AreEqual(before, state.Gold);
        }

        // 전선 훈련 인원이 출전한 뒤에는 부족한 성 수치 개선으로 자동 전환합니다.
        [Test]
        public void EmptyFrontline_ImprovesCastleInsteadOfWaitingForArmy()
        {
            home.GovernorHeroId = string.Empty;
            home.HeroIds.Clear();
            home.GovernorPolicy = WIGovernorPolicy.Frontline;
            home.Defense = home.Stability = home.Technology = 100;
            RunAdministration();
            Assert.Greater(home.Prosperity, 10);
        }

        // 기존 UGUI 프리팹의 버튼으로 영지관 없이 운영을 켜고 해임 후에도 유지합니다.
        [Test]
        public void ExistingDelegationUI_StartsWithoutGovernorAndSurvivesDismissal()
        {
            var host = new GameObject("WIFastFlowTest");
            host.SetActive(false);
            try
            {
                var controller = host.AddComponent<WIAdministrationUIController>();
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(WIAdministrationUIController).GetField("database", flags).SetValue(controller, database);
                typeof(WIAdministrationUIController).GetField("state", flags).SetValue(controller, state);
                typeof(WIAdministrationUIController).GetField("selectedCastle", flags).SetValue(controller, home);
                home.GovernorHeroId = string.Empty;
                home.DelegatedToGovernor = false;
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Prefabs/Administration/WIAdministrationDelegationUGUI.prefab");
                var instance = Object.Instantiate(prefab, host.transform);
                var panel = instance.GetComponent<WIAdministrationDelegationUGUIController>();
                typeof(WIAdministrationUGUIPanelController).GetField("administrationController", flags)
                    .SetValue(panel, controller);
                typeof(WIAdministrationDelegationUGUIController).GetMethod("ToggleDelegation", flags)
                    .Invoke(panel, null);
                Assert.IsTrue(home.DelegatedToGovernor);
                Assert.IsTrue(controller.TryGetUGUIDelegationSnapshot(out var snapshot, out string error), error);
                Assert.AreEqual(database.GetText("UI_ADMIN_OPERATION_STOP"), snapshot.ToggleLabel);
                StringAssert.Contains(database.GetText("UI_ADMIN_BASIC_OPERATION"), snapshot.Preview);
                home.GovernorHeroId = "ares";
                Assert.IsTrue(controller.DismissUGUIGovernor(out error), error);
                Assert.IsTrue(home.DelegatedToGovernor);
                Assert.IsEmpty(home.GovernorHeroId);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        // 승리·통치 선택 후 전투단원과 역할을 유지한 채 추가 턴 없이 재출정합니다.
        [Test]
        public void Occupation_KeepsFormationAndAllowsImmediateNextMarch()
        {
            var army = WIAdministrationTurnSystem.CreateArmy(database, state, home, "ares");
            var target = state.GetCastle("castle_01");
            home.HeroIds.Remove("ares");
            army.CurrentCastleId = target.CastleId;
            army.AwaitingBattle = true;
            string formation = JsonUtility.ToJson(army.Members.Single());
            int turn = state.Turn;
            Assert.IsTrue(WIAdministrationTurnSystem.ResolveArmyVictoryAndOccupation(database, state, army));
            Assert.IsFalse(target.PendingGovernorAppointment);
            Assert.IsTrue(target.DelegatedToGovernor);
            Assert.IsEmpty(target.GovernorHeroId);
            Assert.AreEqual(0, army.ReorganizationMonths);
            Assert.IsTrue(WIOccupationEventSystem.Resolve(database, state,
                state.PendingOccupationEvents.Single(), 0, new WITurnSummary()));
            Assert.IsTrue(WIAdministrationTurnSystem.BeginArmyMarch(database, state, army, home.CastleId));
            Assert.AreEqual(turn, state.Turn);
            Assert.AreEqual(formation, JsonUtility.ToJson(army.Members.Single()));
            Assert.IsEmpty(target.HeroIds);
            int unrest = target.OccupationUnrestMonths;
            int stability = target.Stability;
            for (int month = 0; month < unrest; month += 1)
            {
                InvokeTurnStep("ResolveOccupationStability", new WITurnSummary());
            }
            Assert.AreEqual(0, target.OccupationUnrestMonths);
            Assert.AreEqual(Mathf.Min(100, stability + unrest * database.Automation.OccupationStabilityGain), target.Stability);
        }

        // 불안 기간·안정량·승리 후 재편 기간이 편집 가능한 데이터 값을 따릅니다.
        [Test]
        public void Occupation_UsesSerializedBalance()
        {
            var serialized = new SerializedObject(database);
            serialized.FindProperty("automation.occupationUnrestMonths").intValue = 4;
            serialized.FindProperty("automation.occupationStabilityGain").intValue = 7;
            serialized.FindProperty("automation.victoryReorganizationMonths").intValue = 2;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var army = WIAdministrationTurnSystem.CreateArmy(database, state, home, "ares");
            army.CurrentCastleId = "castle_01";
            army.AwaitingBattle = true;
            Assert.IsTrue(WIAdministrationTurnSystem.ResolveArmyVictoryAndOccupation(database, state, army));
            var target = state.GetCastle("castle_01");
            Assert.AreEqual(4, target.OccupationUnrestMonths);
            Assert.AreEqual(2, army.ReorganizationMonths);
            int stability = target.Stability;
            InvokeTurnStep("ResolveOccupationStability", new WITurnSummary());
            Assert.AreEqual(stability + 7, target.Stability);
            Assert.AreEqual(3, target.OccupationUnrestMonths);
        }

        // 구 저장의 영지관 임명 대기와 새 운영 방침이 저장 왕복 뒤에도 정상 처리됩니다.
        [Test]
        public void SaveRoundTrip_RestoresUnstaffedAdministration()
        {
            home.GovernorHeroId = string.Empty;
            home.HeroIds.Clear();
            home.PendingGovernorAppointment = true;
            home.DelegatedToGovernor = false;
            Assert.IsTrue(WICampaignSaveSystem.TryDeserialize(WICampaignSaveSystem.Serialize(state),
                out state, out string error), error);
            home = state.GetCastle("castle_00");
            WIAdministrationTurnSystem.ExecuteTurn(database, state);
            Assert.IsTrue(home.DelegatedToGovernor);
            Assert.IsFalse(home.PendingGovernorAppointment);
            Assert.IsEmpty(home.GovernorHeroId);
            Assert.IsTrue(WICampaignSaveSystem.TryDeserialize(WICampaignSaveSystem.Serialize(state),
                out var loaded, out error), error);
            Assert.IsTrue(loaded.GetCastle(home.CastleId).DelegatedToGovernor);
            Assert.AreEqual(home.GovernorPolicy, loaded.GetCastle(home.CastleId).GovernorPolicy);
            Assert.AreEqual(home.GovernorMonthlyBudget, loaded.GetCastle(home.CastleId).GovernorMonthlyBudget);
        }
    }
}

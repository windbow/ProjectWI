using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ProjectWI.Administration;
using ProjectWI.Systems;
using UnityEditor;
using UnityEngine;

namespace ProjectWI.Tests.Editor
{
    public class WIConcurrentDutyTests
    {
        // 실제 데이터 사본과 비활성 컨트롤러로 화면 선택부터 원정까지 검증합니다.
        private WIAdministrationDatabaseSO database;
        private WIAdministrationState state;
        private WICastleRuntimeState castle;
        private GameObject host;
        private WIAdministrationUIController controller;

        // 실제 편성 UI와 같은 컨트롤러에 독립 상태를 주입합니다.
        [SetUp]
        public void SetUp()
        {
            database = Object.Instantiate(AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(
                "Assets/Data/ScriptableObject/Administration/WI_AdministrationDatabase.asset"));
            state = WIAdministrationState.Create(database);
            castle = state.GetCastle("castle_00");
            castle.Prosperity = 10;
            state.Gold = 10000;
            host = new GameObject("WIConcurrentDutyTest");
            host.SetActive(false);
            controller = host.AddComponent<WIAdministrationUIController>();
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(WIAdministrationUIController).GetField("database", flags).SetValue(controller, database);
            typeof(WIAdministrationUIController).GetField("state", flags).SetValue(controller, state);
            typeof(WIAdministrationUIController).GetField("selectedCastle", flags).SetValue(controller, castle);
        }

        // 검사에만 사용하는 컨트롤러와 데이터 사본을 해제합니다.
        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(host);
            Object.DestroyImmediate(database);
        }

        // 사업 담당자가 군사·성 출정 후보에 표시되고 실제 대장 편성·출전 뒤 사업을 완료합니다.
        [Test]
        public void ProjectManager_CanBeCommanderAndFinishProjectWhileMarching()
        {
            Assert.IsTrue(controller.AssignUGUIFocusProject(WICastleProjectType.Prosperity,
                WIProjectInvestment.Basic, "ares", out string error), error);
            Assert.IsTrue(controller.TryGetUGUIMilitaryPanel("commanders", castle.CastleId, 0, out var panel, out error), error);
            Assert.IsTrue(panel.Items.Any(item => item.Id == "ares"));
            Assert.IsTrue(controller.TryGetUGUIMarchCommanders(out var march, out error), error);
            Assert.IsTrue(march.Options.Any(item => item.Id == "ares"));
            Assert.IsTrue(controller.ExecuteUGUIMilitaryAction("create", castle.CastleId, "ares", 0,
                out string armyId, out error), error);
            var army = state.Armies.Single(item => item.ArmyId == armyId);
            Assert.IsTrue(WIAdministrationTurnSystem.BeginArmyMarch(database, state, army, "castle_01"));
            WIAdministrationTurnSystem.ExecuteTurn(database, state);
            Assert.Greater(castle.Prosperity, 10);
            Assert.IsNull(castle.ActiveProject);
            Assert.AreEqual("ares", army.Members.Single().HeroId);
        }

        // 내정 중인 영웅을 기존 다중 선택 경로로 단원 편성할 수 있습니다.
        [Test]
        public void ProjectManager_CanJoinThroughBatchMemberSelection()
        {
            Assert.IsTrue(controller.AssignUGUIFocusProject(WICastleProjectType.Prosperity,
                WIProjectInvestment.Basic, "ares", out string error), error);
            var army = WIAdministrationTurnSystem.CreateArmy(database, state, castle, "lyria");
            Assert.IsNotNull(army);
            Assert.IsTrue(controller.TryGetUGUIMilitaryPanel("members", army.ArmyId, 0, out var panel, out error), error);
            Assert.IsTrue(panel.Items.Any(item => item.Id == "ares"));
            Assert.IsFalse(panel.Items.Single(item => item.Id == "lyria").Interactable);
            Assert.That(panel.Items.Single(item => item.Id == "lyria").Description, Does.Contain("전투단"));
            Assert.IsTrue(controller.AssignUGUIArmyMembers(army.ArmyId, new[] { "ares" }, out error), error);
            Assert.IsNotNull(castle.ActiveProject);
            Assert.IsNull(WIAdministrationTurnSystem.CreateArmy(database, state, castle, "ares"));
        }

        // 이미 출정한 영웅도 같은 진영의 사업과 영지관 후보로 지정할 수 있습니다.
        [Test]
        public void DeployedHero_CanReceiveAdministrationAndKeepGovernorBonus()
        {
            var army = WIAdministrationTurnSystem.CreateArmy(database, state, castle, "ares");
            Assert.IsTrue(WIAdministrationTurnSystem.BeginArmyMarch(database, state, army, "castle_01"));
            Assert.IsTrue(controller.TryGetUGUIProjectManagers(WICastleProjectType.Prosperity,
                WIProjectInvestment.Basic, out var candidates, out string error), error);
            Assert.IsTrue(candidates.Any(candidate => candidate.HeroId == "ares"));
            Assert.IsTrue(controller.AssignUGUIGovernor("ares", out error), error);
            castle.DelegatedToGovernor = true;
            castle.GovernorPolicy = WIGovernorPolicy.Prosperity;
            StringAssert.Contains(database.GetHero("ares").DisplayName.Korean,
                WIAdministrationTurnSystem.GetDelegationPreview(database, state, castle));
            WIAdministrationTurnSystem.ExecuteTurn(database, state);
            Assert.AreEqual("ares", castle.GovernorHeroId);
            Assert.Greater(castle.Prosperity, 10);
        }

        // Common의 전문 업무는 모두 차단하고 영웅 승격 뒤에만 보유 특성을 사용합니다.
        [Test]
        public void CommonCannotAdministerRecruitResearchOrSpyUntilPromoted()
        {
            var common = state.Characters.First(item => item.BaseGrade == WICharacterGrade.Common);
            common.Traits.AddRange(new[] { WITraitType.Administration, WITraitType.TalentRecruitment,
                WITraitType.Scholar, WITraitType.Espionage });
            Assert.IsFalse(WIAdministrationTurnSystem.IsAdministrationCapable(state, common.HeroId));
            Assert.IsFalse(WIAdministrationTurnSystem.CanRecruitTalent(state, common.HeroId));
            Assert.IsFalse(WIAdministrationTurnSystem.CanResearch(state, common.HeroId));
            Assert.IsFalse(WIAdministrationTurnSystem.CanPerformScheme(state, common.HeroId));
            common.PromotedToHero = true;
            Assert.IsTrue(WIAdministrationTurnSystem.IsAdministrationCapable(state, common.HeroId));
            Assert.IsTrue(WIAdministrationTurnSystem.CanRecruitTalent(state, common.HeroId));
            Assert.IsTrue(WIAdministrationTurnSystem.CanResearch(state, common.HeroId));
            Assert.IsTrue(WIAdministrationTurnSystem.CanPerformScheme(state, common.HeroId));
        }

        // 구 저장의 Common 역할을 해제하되 지불한 사업·연구와 반복 운영은 보존합니다.
        [Test]
        public void LegacySave_ClearsCommonDutiesWithoutDiscardingPaidProgress()
        {
            string id = state.Characters.First(item => item.BaseGrade == WICharacterGrade.Common).HeroId;
            castle.GovernorHeroId = id;
            castle.ActiveProject = new WICastleProjectState { ManagerHeroId = id, RemainingMonths = 2,
                ProjectType = WICastleProjectType.Expansion, GoldCost = 600 };
            castle.RepeatProject = true;
            castle.StandingProject = new WICastleProjectState { ManagerHeroId = id };
            state.GetPlayerFactionState().ResearcherHeroId = id;
            state.GetPlayerFactionState().ActiveResearchId = database.ResearchDefinitions[0].Id;
            state.GetPlayerFactionState().ResearchRemainingMonths = 3;
            state.SchemeMissions.Add(new WISchemeMissionState { AgentHeroId = id });
            state.GetCharacter(id).Activity = WICharacterActivityType.Search;
            Assert.IsTrue(WICampaignSaveSystem.TryDeserialize(WICampaignSaveSystem.Serialize(state),
                out var loaded, out string error), error);
            loaded.SynchronizeCharacterTraits(database);
            var restored = loaded.GetCastle(castle.CastleId);
            Assert.IsEmpty(restored.GovernorHeroId);
            Assert.IsTrue(restored.ActiveProject.Delegated);
            Assert.AreEqual(2, restored.ActiveProject.RemainingMonths);
            Assert.AreEqual(600, restored.ActiveProject.GoldCost);
            Assert.IsTrue(restored.DelegatedToGovernor);
            Assert.IsEmpty(loaded.GetPlayerFactionState().ResearcherHeroId);
            Assert.AreEqual(3, loaded.GetPlayerFactionState().ResearchRemainingMonths);
            Assert.IsEmpty(loaded.SchemeMissions);
            Assert.AreEqual(WICharacterActivityType.None, loaded.GetCharacter(id).Activity);
        }

        // 주둔 전투단의 영웅과 일반 모두 아무 개인 명령 없이 성장합니다.
        [Test]
        public void StationedCharactersTrainAutomatically_WithoutBlockingOrders()
        {
            string commonId = state.Characters.First(item => item.BaseGrade == WICharacterGrade.Common).HeroId;
            foreach (var home in state.Castles)
            {
                home.HeroIds.Remove(commonId);
            }
            castle.HeroIds.Add(commonId);
            state.GetCharacter(commonId).Recruited = true;
            var army = WIAdministrationTurnSystem.CreateArmy(database, state, castle, "ares");
            Assert.IsTrue(WIAdministrationTurnSystem.AddArmyMember(database, state, army, commonId, WIUnitRole.Melee));
            WIAdministrationTurnSystem.ExecuteTurn(database, state);
            Assert.Greater(state.GetCharacter("ares").Experience, 0);
            Assert.Greater(state.GetCharacter(commonId).Experience, 0);
            Assert.AreEqual(WICharacterActivityType.None, state.GetCharacter(commonId).Activity);
            Assert.IsTrue(WIAdministrationTurnSystem.BeginArmyMarch(database, state, army, "castle_01"));
        }

        // 부상자는 자동 회복하며 이동 중인 전투단은 훈련 경험을 얻지 않습니다.
        [Test]
        public void AutomaticTraining_RecoversInjuriesAndSkipsMovingArmy()
        {
            var character = state.GetCharacter("ares");
            character.InjuryMonths = 2;
            character.Fatigue = 80;
            WIAdministrationTurnSystem.ExecuteTurn(database, state);
            Assert.Less(character.InjuryMonths, 2);
            Assert.Less(character.Fatigue, 80);
            Assert.AreEqual(0, character.Experience);
            character.InjuryMonths = 0;
            character.Fatigue = 0;
            var army = WIAdministrationTurnSystem.CreateArmy(database, state, castle, "ares");
            Assert.IsTrue(WIAdministrationTurnSystem.BeginArmyMarch(database, state, army, "castle_01"));
            army.RemainingTravelMonths = 3;
            WIAdministrationTurnSystem.ExecuteTurn(database, state);
            Assert.AreEqual(0, character.Experience);
        }

        // 연구 담당 영웅도 편성 가능하지만 중복 전투단·포로·이동은 여전히 차단합니다.
        [Test]
        public void ResearchAllowsArmy_WhilePhysicalUnavailabilityStillBlocks()
        {
            state.GetPlayerFactionState().ResearcherHeroId = "ares";
            state.GetPlayerFactionState().ActiveResearchId = database.ResearchDefinitions[0].Id;
            state.GetCharacter("ares").Captured = true;
            Assert.IsNull(WIAdministrationTurnSystem.CreateArmy(database, state, castle, "ares"));
            state.GetCharacter("ares").Captured = false;
            state.CharacterTransfers.Add(new WICharacterTransferState { HeroId = "ares" });
            Assert.IsNull(WIAdministrationTurnSystem.CreateArmy(database, state, castle, "ares"));
            state.CharacterTransfers.Clear();
            Assert.IsNotNull(WIAdministrationTurnSystem.CreateArmy(database, state, castle, "ares"));
            Assert.IsNull(WIAdministrationTurnSystem.CreateArmy(database, state, castle, "ares"));
        }
    }
}

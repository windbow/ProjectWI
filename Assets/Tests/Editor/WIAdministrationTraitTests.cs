using System.Linq;
using NUnit.Framework;
using ProjectWI.Administration;
using ProjectWI.Systems;
using UnityEditor;
using UnityEngine;

namespace ProjectWI.Tests.Editor
{
    public class WIAdministrationTraitTests
    {
        // 검사별 데이터 사본으로 실제 로스터를 변경하지 않습니다.
        private WIAdministrationDatabaseSO database;
        private WIAdministrationState state;
        private WICastleRuntimeState castle;
        private string administrator;

        // 내정 일반 인물을 수도에 배치한 독립 검사 상태를 준비합니다.
        [SetUp]
        public void SetUp()
        {
            database = Object.Instantiate(AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(
                "Assets/Data/ScriptableObject/Administration/WI_AdministrationDatabase.asset"));
            state = WIAdministrationState.Create(database);
            castle = state.GetCastle("castle_00");
            administrator = database.Heroes.First(hero => hero.Grade == WICharacterGrade.Hero &&
                hero.HasTrait(WITraitType.Administration) && hero.HasTrait(WITraitType.TalentRecruitment)).Id;
            foreach (var home in state.Castles)
            {
                home.HeroIds.Remove(administrator);
                if (home.GovernorHeroId == administrator)
                {
                    home.GovernorHeroId = string.Empty;
                }
            }
            castle.HeroIds.Add(administrator);
            castle.GovernorHeroId = administrator;
            state.GetCharacter(administrator).Recruited = true;
            state.Gold = 10000;
        }

        // 생성한 검사 사본만 해제합니다.
        [TearDown]
        public void TearDown() => Object.DestroyImmediate(database);

        // 실제 로스터 수와 일반 내정 특성 300명의 정적 저장을 검증합니다.
        [Test]
        public void MasterData_HasExactly300CommonAdministrators()
        {
            Assert.AreEqual(1200, database.Heroes.Count);
            Assert.AreEqual(1000, database.Heroes.Count(hero => hero.Grade == WICharacterGrade.Common));
            Assert.AreEqual(300, database.Heroes.Count(hero => hero.Grade == WICharacterGrade.Common && hero.HasTrait(WITraitType.Administration)));
            Assert.IsTrue(database.Heroes.Any(hero => hero.Grade == WICharacterGrade.Hero && hero.HasTrait(WITraitType.Administration) == false));
            Assert.IsTrue(database.Heroes.All(hero => hero.Traits.Distinct().Count() == hero.Traits.Count));
            Assert.AreEqual("내정", database.GetTrait(WITraitType.Administration).DisplayName.Korean);
            Assert.AreEqual(180, database.Heroes.Count(hero => hero.Grade == WICharacterGrade.Common &&
                hero.HasTrait(WITraitType.TalentRecruitment)));
            Assert.AreEqual(160, database.Heroes.Count(hero => hero.Grade == WICharacterGrade.Common &&
                hero.HasTrait(WITraitType.Scholar)));
            Assert.AreEqual(140, database.Heroes.Count(hero => hero.Grade == WICharacterGrade.Common &&
                hero.HasTrait(WITraitType.Espionage)));
            Assert.AreEqual(71, database.Heroes.Count(hero => hero.Grade == WICharacterGrade.Hero &&
                hero.HasTrait(WITraitType.TalentRecruitment)));
            Assert.AreEqual(75, database.Heroes.Count(hero => hero.Grade == WICharacterGrade.Hero &&
                hero.HasTrait(WITraitType.Scholar)));
            Assert.AreEqual(61, database.Heroes.Count(hero => hero.Grade == WICharacterGrade.Hero &&
                hero.HasTrait(WITraitType.Espionage)));
        }

        // 등급이 아니라 특성으로 자격을 판정하는 네 조합을 검증합니다.
        [TestCase(WICharacterGrade.Common, true)]
        [TestCase(WICharacterGrade.Common, false)]
        [TestCase(WICharacterGrade.Hero, true)]
        [TestCase(WICharacterGrade.Hero, false)]
        public void Eligibility_DependsOnTrait(WICharacterGrade grade, bool hasTrait)
        {
            string id = database.Heroes.First(hero => hero.Grade == grade && hero.HasTrait(WITraitType.Administration) == hasTrait).Id;
            Assert.AreEqual(grade == WICharacterGrade.Hero && hasTrait, WIAdministrationTurnSystem.IsAdministrationCapable(state, id));
            Assert.IsTrue(WIAdministrationTurnSystem.CanPerformCharacterActivity(state, id, WICharacterActivityType.Training));
            Assert.IsTrue(WIAdministrationTurnSystem.CanPerformCharacterActivity(state, id, WICharacterActivityType.Rest));
            Assert.IsTrue(WIAdministrationTurnSystem.CanPerformCharacterActivity(state, id, WICharacterActivityType.Socialize));
        }

        // 인재영입·학자·첩보 특성이 각각 해당 업무만 허용하는지 검증합니다.
        [Test]
        public void OperationalTraits_GrantOnlyTheirOwnWork()
        {
            string recruiter = state.Characters.First(item => item.Traits.Contains(WITraitType.TalentRecruitment)).HeroId;
            string scholar = state.Characters.First(item => item.Traits.Contains(WITraitType.Scholar)).HeroId;
            string spy = state.Characters.First(item => item.Traits.Contains(WITraitType.Espionage)).HeroId;
            Assert.IsTrue(WIAdministrationTurnSystem.CanRecruitTalent(state, recruiter));
            Assert.IsTrue(WIAdministrationTurnSystem.CanResearch(state, scholar));
            Assert.IsTrue(WIAdministrationTurnSystem.CanPerformScheme(state, spy));

            WICharacterRuntimeState plain = state.Characters.First(item =>
                item.Traits.Contains(WITraitType.TalentRecruitment) == false &&
                item.Traits.Contains(WITraitType.Scholar) == false &&
                item.Traits.Contains(WITraitType.Espionage) == false);
            Assert.IsFalse(WIAdministrationTurnSystem.CanRecruitTalent(state, plain.HeroId));
            Assert.IsFalse(WIAdministrationTurnSystem.CanResearch(state, plain.HeroId));
            Assert.IsFalse(WIAdministrationTurnSystem.CanPerformScheme(state, plain.HeroId));
        }

        // 승격해도 내정 특성을 자동 취득하지 않으며 죽거나 포로인 인물은 업무를 하지 못합니다.
        [Test]
        public void PromotionAndUnavailableCharacters_DoNotBypassTrait()
        {
            var common = state.Characters.First(character => character.BaseGrade == WICharacterGrade.Common &&
                character.Traits.Contains(WITraitType.Administration) == false);
            common.PromotedToHero = true;
            Assert.IsFalse(WIAdministrationTurnSystem.IsAdministrationCapable(state, common.HeroId));
            state.GetCharacter(administrator).Captured = true;
            Assert.IsFalse(WIAdministrationTurnSystem.AssignGovernor(state, castle.CastleId, administrator));
            state.GetCharacter(administrator).Captured = false;
            state.GetCharacter(administrator).IsDead = true;
            Assert.IsFalse(WIAdministrationTurnSystem.CanPerformCharacterActivity(state, administrator, WICharacterActivityType.Rest));
        }

        // 아레스가 영지관과 전투단 대장을 겸임하면서 성의 내정을 유지합니다.
        [Test]
        public void AresCanFight_WhileCommonGovernorRunsHome()
        {
            state = WIAdministrationState.Create(database, WICampaignDifficulty.Standard, WICampaignVariant.AresMain);
            castle = state.Castles.First(home => home.HeroIds.Contains("ares"));
            Assert.IsTrue(WIAdministrationTurnSystem.AssignGovernor(state, castle.CastleId, "ares"));
            Assert.AreEqual(WICharacterGrade.Hero, state.GetCharacter(castle.GovernorHeroId).BaseGrade);
            Assert.IsTrue(WIAdministrationTurnSystem.IsAdministrationCapable(state, castle.GovernorHeroId));
            castle.DelegatedToGovernor = true;
            int before = castle.Prosperity;
            WIArmyState army = WIAdministrationTurnSystem.CreateArmy(database, state, castle, "ares");
            Assert.IsNotNull(army);
            for (int month = 0; month < 3; month += 1)
            {
                WIAdministrationTurnSystem.ExecuteTurn(database, state);
            }

            Assert.Greater(castle.Prosperity, before);
            Assert.IsNotEmpty(castle.GovernorHeroId);
            Assert.AreEqual("ares", castle.GovernorHeroId);
            Assert.IsTrue(castle.DelegatedToGovernor);
            Assert.IsTrue(army.Members.Any(member => member.HeroId == "ares"));
            Assert.IsEmpty(state.PendingProjectEvents);
            Assert.IsEmpty(state.PendingLegacyChoices);
        }

        // 구 저장의 특성 누락을 마스터에서 복원하고 저장 왕복 시 유지 지시를 보존합니다.
        [Test]
        public void SaveRoundTrip_PreservesOrdersAndRepairsMissingTraits()
        {
            SetProjectOrder();
            var character = state.GetCharacter(administrator);
            character.RepeatActivity = true;
            character.StandingActivity = WICharacterActivityType.Search;
            character.Traits.Clear();
            Assert.IsTrue(WICampaignSaveSystem.TryDeserialize(WICampaignSaveSystem.Serialize(state), out var loaded, out string error), error);
            loaded.SynchronizeCharacterTraits(database);
            Assert.IsTrue(WIAdministrationTurnSystem.IsAdministrationCapable(loaded, administrator));
            Assert.IsTrue(loaded.GetCastle(castle.CastleId).RepeatProject);
            Assert.AreEqual(WICharacterActivityType.Search, loaded.GetCharacter(administrator).StandingActivity);
        }

        // 유지 사업이 담당자를 영구 점유하지 않고 폐지된 수동 휴식 지시에도 계속되는지 검증합니다.
        [Test]
        public void StandingProject_RepeatsWithoutPermanentlyOccupyingActor()
        {
            SetProjectOrder();
            castle.Prosperity = 10;
            WIAdministrationTurnSystem.ExecuteTurn(database, state);
            Assert.Greater(castle.Prosperity, 10);
            Assert.IsNull(castle.ActiveProject);
            Assert.IsFalse(state.IsCharacterBusy(administrator));
            int before = castle.Prosperity;
            state.GetCharacter(administrator).Activity = WICharacterActivityType.Rest;
            WIAdministrationTurnSystem.ExecuteTurn(database, state);
            Assert.Greater(castle.Prosperity, before);
            Assert.AreEqual(WICharacterActivityType.None, state.GetCharacter(administrator).Activity);
        }

        // 자금 부족과 목표 달성 시 유지 사업이 비용을 쓰지 않는지 검증합니다.
        [Test]
        public void StandingProject_StopsAtTargetAndWaitsForFunds()
        {
            SetProjectOrder();
            castle.Prosperity = 100;
            var summary = WIAdministrationTurnSystem.ExecuteTurn(database, state);
            Assert.AreEqual(0, summary.GoldSpent);
            Assert.IsNull(castle.StandingProject);
            SetProjectOrder();
            castle.Prosperity = 5;
            state.Gold = 0;
            summary = WIAdministrationTurnSystem.ExecuteTurn(database, state);
            Assert.AreEqual(0, summary.GoldSpent);
            Assert.IsNotNull(castle.StandingProject);
        }

        // 담당자 이동이나 특성 제거로 잘못된 위치에서 반복 사업을 수행하지 않습니다.
        [Test]
        public void StandingProject_CancelsWhenManagerLeaves()
        {
            SetProjectOrder();
            castle.HeroIds.Remove(administrator);
            var summary = WIAdministrationTurnSystem.ExecuteTurn(database, state);
            Assert.AreEqual(0, summary.GoldSpent);
            Assert.IsNull(castle.StandingProject);
        }

        // 최대치 도달 위임은 낭비 없이 정지하고 부족한 수치가 있으면 전환합니다.
        [Test]
        public void Delegation_ReallocatesAndStopsSpendingAtMaximum()
        {
            castle.DelegatedToGovernor = true;
            castle.GovernorPolicy = WIGovernorPolicy.Prosperity;
            castle.Prosperity = 100;
            castle.Technology = 10;
            Assert.AreEqual(WICastleProjectType.Technology, WIAdministrationTurnSystem.ChooseGovernorProject(castle));
            castle.Technology = castle.Stability = castle.Defense = 100;
            var summary = WIAdministrationTurnSystem.ExecuteTurn(database, state);
            Assert.AreEqual(0, summary.GoldSpent);
        }

        // 위임 사업은 통상 선택 사건과 흔적 선택을 새로 쌓지 않습니다.
        [Test]
        public void Delegation_DoesNotQueueRoutineProjectChoices()
        {
            castle.DelegatedToGovernor = true;
            castle.GovernorPolicy = WIGovernorPolicy.Prosperity;
            castle.GovernorMonthlyBudget = database.ProjectBalance.IntensiveCost;
            WIAdministrationTurnSystem.ExecuteTurn(database, state);
            Assert.AreEqual(0, state.PendingProjectEvents.Count);
            Assert.AreEqual(0, state.PendingLegacyChoices.Count);
        }

        // 반복 개인 활동이 과로 시 휴식하고 회복 후 원래 활동으로 돌아오는지 검증합니다.
        [Test]
        public void RepeatedActivity_RestsAndResumes()
        {
            var character = state.GetCharacter(administrator);
            character.RepeatActivity = true;
            character.StandingActivity = WICharacterActivityType.Training;
            character.Fatigue = 80;
            int experience = character.Experience;
            WIAdministrationTurnSystem.ExecuteTurn(database, state);
            Assert.Less(character.Fatigue, 80);
            Assert.AreEqual(experience, character.Experience);
            character.Fatigue = 0;
            WIAdministrationTurnSystem.ExecuteTurn(database, state);
            Assert.Greater(character.Experience, experience);
        }

        // 자동 반복의 영입 목표가 이미 합류하면 지시를 종료합니다.
        [Test]
        public void RepeatedRecruitment_StopsWhenTargetJoins()
        {
            var character = state.GetCharacter(administrator);
            character.RepeatActivity = true;
            character.StandingActivity = WICharacterActivityType.Recruit;
            character.StandingActivityTargetHeroId = "ares";
            WIAdministrationTurnSystem.ExecuteTurn(database, state);
            Assert.IsFalse(character.RepeatActivity);
            Assert.AreEqual(WICharacterActivityType.None, character.StandingActivity);
        }

        // 한 성의 선술집 인재실에는 탐색·영입 담당자를 최대 두 명만 배치할 수 있는지 검증합니다.
        [Test]
        public void TalentOffice_AllowsOnlyTwoSearchOrRecruitWorkersPerCastle()
        {
            string[] administrators = state.Characters
                .Where(character => WIAdministrationTurnSystem.CanRecruitTalent(state, character.HeroId))
                .Select(character => character.HeroId)
                .Take(3).ToArray();
            Assert.AreEqual(3, administrators.Length);
            foreach (WICastleRuntimeState home in state.Castles)
            {
                home.HeroIds.RemoveAll(id => administrators.Contains(id));
            }
            castle.HeroIds.AddRange(administrators);
            state.GetCharacter(administrators[0]).StandingActivity = WICharacterActivityType.Search;
            state.GetCharacter(administrators[1]).StandingActivity = WICharacterActivityType.Recruit;

            Assert.AreEqual(2, WIAdministrationTurnSystem.GetTalentOfficeWorkerCount(state, castle));
            Assert.IsTrue(WIAdministrationTurnSystem.CanPerformCharacterActivity(
                state, administrators[0], WICharacterActivityType.Search));
            Assert.IsFalse(WIAdministrationTurnSystem.CanPerformCharacterActivity(
                state, administrators[2], WICharacterActivityType.Recruit));

            state.GetCharacter(administrators[0]).StandingActivity = WICharacterActivityType.None;
            Assert.IsTrue(WIAdministrationTurnSystem.CanPerformCharacterActivity(
                state, administrators[2], WICharacterActivityType.Recruit));
        }

        // 새 점령지에 배치된 내정 일반 인물로 후방 위임을 시작합니다.
        [Test]
        public void NewTerritory_AppointsAvailableAdministrator()
        {
            castle.GovernorHeroId = string.Empty;
            castle.PendingGovernorAppointment = true;
            WIAdministrationTurnSystem.ExecuteTurn(database, state);
            Assert.AreEqual(WICharacterGrade.Hero, state.GetCharacter(castle.GovernorHeroId).BaseGrade);
            Assert.IsTrue(castle.DelegatedToGovernor);
            Assert.IsFalse(castle.PendingGovernorAppointment);
        }

        // 반복 UI가 런타임 생성 없이 두 프리팹에 저장되어 있는지 검사합니다.
        [TestCase("WIAdministrationFocusProjectUGUI", typeof(WIAdministrationFocusProjectUGUIController))]
        [TestCase("WIAdministrationCharacterActivityUGUI", typeof(WIAdministrationCharacterActivityUGUIController))]
        public void RepeatControls_AreSerializedInPrefabs(string name, System.Type type)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Administration/" + name + ".prefab");
            var serialized = new SerializedObject(prefab.GetComponent(type));
            Assert.IsNotNull(serialized.FindProperty("repeatButton").objectReferenceValue);
        }

        // 수도의 내정 일반 인물에게 비용을 아직 지불하지 않은 유지 지시를 준비합니다.
        private void SetProjectOrder()
        {
            castle.DelegatedToGovernor = false;
            castle.RepeatProject = true;
            castle.StandingProject = new WICastleProjectState
            {
                ProjectType = WICastleProjectType.Prosperity,
                Investment = WIProjectInvestment.Basic, ManagerHeroId = administrator
            };
        }
    }
}

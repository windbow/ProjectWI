using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ProjectWI.Administration;
using ProjectWI.Systems;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectWI.Tests.Editor
{
    public class WIActivityRetirementTests
    {
        // 실제 마스터 데이터와 저장에 영향을 주지 않는 검사 인스턴스입니다.
        private WIAdministrationDatabaseSO database;
        private WIAdministrationState state;
        private GameObject host;
        private WIAdministrationUIController controller;
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;

        // 독립 캠페인과 비활성 UI 컨트롤러를 준비합니다.
        [SetUp]
        public void SetUp()
        {
            database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>("Assets/Data/ScriptableObject/Administration/WI_AdministrationDatabase.asset");
            state = WIAdministrationState.Create(database, WICampaignDifficulty.Standard, WICampaignVariant.AresMain);
            host = new GameObject("WIActivityRetirementTest");
            host.SetActive(false);
            controller = host.AddComponent<WIAdministrationUIController>();
            typeof(WIAdministrationUIController).GetField("database", Flags).SetValue(controller, database);
            typeof(WIAdministrationUIController).GetField("state", Flags).SetValue(controller, state);
        }

        // 임시 인스턴스를 해제합니다.
        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(host);
        }

        // 인사 목록에서 적군·미영입·사망자를 제외하고 이동 및 포로 상태의 아군을 유지합니다.
        [Test]
        public void PersonnelListsOnlyLivingPlayerCharacters()
        {
            var castle = state.GetCastle("castle_28");
            string[] allies = castle.HeroIds.ToArray();
            Assert.That(allies.Length, Is.GreaterThanOrEqualTo(4));
            var enemyCastle = state.Castles.First(item => item.FactionId != state.PlayerFactionId && item.HeroIds.Count > 0);
            string enemyId = enemyCastle.HeroIds[0];
            castle.HeroIds.Remove(allies[0]);
            state.CharacterTransfers.Add(new WICharacterTransferState
            {
                HeroId = allies[0], OriginCastleId = castle.CastleId, TargetCastleId = castle.CastleId
            });
            castle.HeroIds.Remove(allies[1]);
            state.GetCharacter(allies[1]).Captured = true;
            state.GetCharacter(allies[1]).CapturedFromFactionId = state.PlayerFactionId;
            state.GetCharacter(allies[1]).CaptorFactionId = enemyCastle.FactionId;
            state.GetCharacter(allies[2]).IsDead = true;
            state.GetCharacter(allies[3]).Recruited = false;
            state.GetCharacter(allies[3]).Discovered = true;
            Assert.That(controller.TryGetUGUIHeroesPanel("list", string.Empty, out var snapshot, out var error), Is.True, error);
            var ids = snapshot.Cards.Select(item => item.Id).ToArray();
            Assert.That(ids, Does.Contain(allies[0]));
            Assert.That(ids, Does.Contain(allies[1]));
            Assert.That(ids, Does.Not.Contain(allies[2]));
            Assert.That(ids, Does.Not.Contain(allies[3]));
            Assert.That(ids, Does.Not.Contain(enemyId));
            Assert.That(snapshot.Summary, Does.StartWith($"영입 영웅 {ids.Length}명"));
            Assert.That(snapshot.Cards.All(item => item.Action == "hero"), Is.True);
        }

        // 기존 군사 프리팹의 카드 핸들러로 성·인물·목적지 단계를 통과합니다.
        [Test]
        public void MilitaryRowsOpenResidentTransferAndReserveDestination()
        {
            var origin = state.GetCastle("castle_28");
            var destination = state.Castles.First(item => item.CastleId != origin.CastleId);
            destination.FactionId = state.PlayerFactionId;
            destination.HeroIds.Clear();
            origin.AdjacentCastleIds.Clear();
            origin.AdjacentCastleIds.Add(destination.CastleId);
            origin.GovernorHeroId = string.Empty;
            origin.ActiveProject = null;
            state.Armies.Clear();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Administration/WIAdministrationMilitaryUGUI.prefab");
            var panel = Object.Instantiate(prefab, host.transform).GetComponent<WIAdministrationMilitaryUGUIController>();
            panel.BindAdministrationController(controller);
            var type = panel.GetType();
            type.GetMethod("Open", Flags).Invoke(panel, null);
            Click(panel, "transfer-castles", null);
            Click(panel, "transfer-castle", origin.CastleId);
            Click(panel, "transfer-actor", "ares");
            Click(panel, "transfer-target", destination.CastleId);
            Assert.That(state.CharacterTransfers.Single().HeroId, Is.EqualTo("ares"));
            Assert.That(state.CharacterTransfers.Single().TargetCastleId, Is.EqualTo(destination.CastleId));
            Assert.That(origin.HeroIds, Does.Not.Contain("ares"));
        }

        // 전투단원 제외·영지관 비활성 표시와 상대 세력 인물 이동 차단을 확인합니다.
        [Test]
        public void MilitaryTransferRespectsDutiesAndOwnership()
        {
            var castle = state.GetCastle("castle_28");
            castle.GovernorHeroId = "ares";
            var army = WIAdministrationTurnSystem.CreateArmy(database, state, castle, "lyria");
            Assert.That(army, Is.Not.Null);
            Assert.That(controller.TryGetUGUIMilitaryPanel("transfer-actors", castle.CastleId, 0, out var snapshot, out var error), Is.True, error);
            Assert.That(snapshot.Items.Any(item => item.Id == "lyria"), Is.False);
            Assert.That(snapshot.Items.Single(item => item.Id == "ares").Interactable, Is.False);
            var enemy = state.Castles.First(item => item.FactionId != state.PlayerFactionId && item.HeroIds.Count > 0);
            Assert.That(controller.StartUGUICharacterTransfer(enemy.HeroIds[0], castle.CastleId, out error), Is.False);
        }

        // 구 저장의 폐지 활동을 해제하면서 인재실 반복 업무와 자동 회복을 보존합니다.
        [Test]
        public void LegacyOrdersClearButTalentRecoverySurvives()
        {
            var ares = state.GetCharacter("ares");
            var lyria = state.GetCharacter("lyria");
            ares.Activity = WICharacterActivityType.Socialize;
            ares.StandingActivity = WICharacterActivityType.Rest;
            ares.RepeatActivity = true;
            lyria.Activity = WICharacterActivityType.Rest;
            lyria.StandingActivity = WICharacterActivityType.Search;
            lyria.RepeatActivity = true;
            lyria.AutomaticRecovery = true;
            Assert.That(WICampaignSaveSystem.TryDeserialize(WICampaignSaveSystem.Serialize(state), out var restored, out var error), Is.True, error);
            restored.SynchronizeCharacterTraits(database);
            Assert.That(restored.GetCharacter("ares").Activity, Is.EqualTo(WICharacterActivityType.None));
            Assert.That(restored.GetCharacter("ares").RepeatActivity, Is.False);
            Assert.That(restored.GetCharacter("lyria").StandingActivity, Is.EqualTo(WICharacterActivityType.Search));
            Assert.That(restored.GetCharacter("lyria").Activity, Is.EqualTo(WICharacterActivityType.Rest));
        }

        // 수동 활동 배정 API가 폐지 명령을 거절하고 자동 회복이 계속 작동하는지 확인합니다.
        [Test]
        public void ManualOrdersRejectedAndIdleInjuryRecovers()
        {
            foreach (var activity in new[] { WICharacterActivityType.Socialize, WICharacterActivityType.Rest, WICharacterActivityType.Training })
            {
                Assert.That(controller.AssignUGUICharacterActivity("ares", activity, "", out _), Is.False);
            }
            var hero = state.GetCharacter("ares");
            hero.Fatigue = 80;
            hero.InjuryMonths = 2;
            var method = typeof(WIAdministrationTurnSystem).GetMethod("ResolveAutomaticCharacterTraining", BindingFlags.Static | BindingFlags.NonPublic);
            method.Invoke(null, new object[] { database, state, new WITurnSummary() });
            Assert.That(hero.Fatigue, Is.LessThan(80));
            Assert.That(hero.InjuryMonths, Is.EqualTo(1));
        }

        // 저장 프리팹에서 일반 활동 진입점과 폐지 버튼이 사라졌는지 검사합니다.
        [Test]
        public void PrefabsContainOnlyTalentActionsAndNoTerritoryActivity()
        {
            var territory = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Administration/WIAdministrationTerritoryUGUI.prefab");
            var so = new SerializedObject(territory.GetComponent<WIAdministrationTerritoryUGUIController>());
            foreach (string name in new[] { "commandActions", "basicFacilityActions" })
            {
                var actions = so.FindProperty(name);
                for (int i = 0; i < actions.arraySize; i += 1)
                {
                    Assert.That(actions.GetArrayElementAtIndex(i).intValue, Is.Not.EqualTo((int)WIAdministrationTerritoryCommand.CharacterActivity));
                }
            }
            var talent = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Administration/WIAdministrationCharacterActivityUGUI.prefab");
            var buttons = new SerializedObject(talent.GetComponent<WIAdministrationCharacterActivityUGUIController>()).FindProperty("activityButtons");
            Assert.That(buttons.arraySize, Is.EqualTo(2));
            Assert.That(((Button)buttons.GetArrayElementAtIndex(0).objectReferenceValue).name, Is.EqualTo("Activity-0"));
            Assert.That(((Button)buttons.GetArrayElementAtIndex(1).objectReferenceValue).name, Is.EqualTo("Activity-2"));
        }

        // 현재 목록에서 목적 항목을 찾아 실제 선택 핸들러를 호출합니다.
        private static void Click(WIAdministrationMilitaryUGUIController panel, string kind, string id)
        {
            var snapshot = (WIAdministrationMilitarySnapshot)panel.GetType().GetField("snapshot", Flags).GetValue(panel);
            int index = snapshot.Items.FindIndex(item => item.Kind == kind && (id == null || item.Id == id));
            Assert.That(index, Is.GreaterThanOrEqualTo(0), kind);
            panel.GetType().GetMethod("OpenItem", Flags).Invoke(panel, new object[] { index });
        }
    }
}

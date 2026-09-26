using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ProjectWI.Administration;
using ProjectWI.Systems;
using UnityEditor;
using UnityEngine;

namespace ProjectWI.Tests.Editor
{
    public class WIMusterTests
    {
        // 실제 데이터로 매 검사마다 독립적인 메인 캠페인과 전투단을 준비합니다.
        private WIAdministrationDatabaseSO database;
        private WIAdministrationState state;
        private WICastleRuntimeState castle;
        private WIArmyState army;

        // 키리엔 시작 인원은 그대로 두고 모병을 받을 전투단만 편성합니다.
        [SetUp]
        public void SetUp()
        {
            database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>("Assets/Data/ScriptableObject/Administration/WI_AdministrationDatabase.asset");
            Assert.NotNull(database.MusterConfig);
            state = WIAdministrationState.Create(database, WICampaignDifficulty.Standard, WICampaignVariant.AresMain);
            castle = state.GetCastle("castle_28");
            state.Gold = 10000;
            army = WIAdministrationTurnSystem.CreateArmy(database, state, castle, "ares");
            Assert.NotNull(army);
        }

        // 비용을 선납하고 다음 월 처리에서 기존 이름·경험을 보존한 일반 인물 한 명이 합류합니다.
        [Test]
        public void QueueAndTurn_RecruitsNamedCommonAndPreservesHistory()
        {
            int count = state.Characters.Count;
            Assert.IsTrue(WIAdministrationTurnSystem.TryQueueMuster(database, state, castle, state.PlayerFactionId, WIUnitRole.Melee, 1, army.ArmyId, out string error), error);
            string id = castle.MusterOrder.HeroIds.Single();
            state.GetCharacter(id).Experience = 37;
            Assert.IsFalse(state.GetCharacter(id).Recruited);
            Assert.AreEqual(10000 - database.MusterConfig.GoldPerCharacter, state.Gold);
            WITurnSummary summary = WIAdministrationTurnSystem.ExecuteTurn(database, state);
            Assert.IsNull(castle.MusterOrder);
            Assert.IsTrue(state.GetCharacter(id).Recruited);
            Assert.AreEqual(37, state.GetCharacter(id).Experience);
            Assert.IsTrue(army.Members.Any(member => member.HeroId == id));
            Assert.AreEqual(count, state.Characters.Count);
            Assert.IsTrue(summary.News.Any(news => news.Contains(database.GetHero(id).DisplayName.Get(false))));
            Assert.AreEqual(0, state.PlayerRecruitmentSuccessCount);
        }

        // 중복 예약과 비용 부족은 비용·인물·예약을 부분 변경하지 않습니다.
        [Test]
        public void RejectedOrders_AreAtomic()
        {
            state.Gold = 0;
            Assert.IsFalse(WIAdministrationTurnSystem.TryQueueMuster(database, state, castle, state.PlayerFactionId, WIUnitRole.Melee, 1, army.ArmyId, out _));
            Assert.IsNull(castle.MusterOrder);
            state.Gold = 10000;
            Assert.IsTrue(WIAdministrationTurnSystem.TryQueueMuster(database, state, castle, state.PlayerFactionId, WIUnitRole.Melee, 1, army.ArmyId, out _));
            int gold = state.Gold;
            Assert.IsFalse(WIAdministrationTurnSystem.TryQueueMuster(database, state, castle, state.PlayerFactionId, WIUnitRole.Melee, 1, army.ArmyId, out _));
            Assert.AreEqual(gold, state.Gold);
            Assert.AreEqual(1, castle.MusterOrder.HeroIds.Count);
        }

        // 성별 모집 속도와 세력 인원 상한에 도달하면 예약을 거부합니다.
        [Test]
        public void CapacityAndMonthlyLimit_AreEnforced()
        {
            int limit = WIAdministrationTurnSystem.GetMusterMonthlyLimit(database, castle);
            Assert.IsFalse(WIAdministrationTurnSystem.TryQueueMuster(database, state, castle, state.PlayerFactionId, WIUnitRole.Melee, limit + 1, army.ArmyId, out _));
            int capacity = WIAdministrationTurnSystem.GetMusterFactionCapacity(database, state, state.PlayerFactionId);
            foreach (var candidate in state.Characters.Where(item => item.Recruited == false).Take(capacity).ToList())
            {
                candidate.Recruited = true;
                castle.HeroIds.Add(candidate.HeroId);
            }
            Assert.IsFalse(WIAdministrationTurnSystem.TryQueueMuster(database, state, castle, state.PlayerFactionId, WIUnitRole.Melee, 1, army.ArmyId, out _));
            Assert.AreEqual(10000, state.Gold);
        }

        // 서로 다른 성의 예약도 같은 일반 인물을 중복 선택하지 않습니다.
        [Test]
        public void TwoCastles_ReserveDifferentCharacters()
        {
            var second = state.Castles.First(item => item.CastleId != castle.CastleId);
            second.FactionId = state.PlayerFactionId;
            second.HeroIds.Clear();
            Assert.IsTrue(WIAdministrationTurnSystem.TryQueueMuster(database, state, castle, state.PlayerFactionId, WIUnitRole.Melee, 1, army.ArmyId, out _));
            Assert.IsTrue(WIAdministrationTurnSystem.TryQueueMuster(database, state, second, state.PlayerFactionId, WIUnitRole.Melee, 1, null, out string error), error);
            Assert.AreNotEqual(castle.MusterOrder.HeroIds.Single(), second.MusterOrder.HeroIds.Single());
        }

        // 점령당한 성의 모집은 이전 소유자에게만 환불하고 인물을 넘기지 않습니다.
        [Test]
        public void OwnershipLoss_RefundsOriginalFaction()
        {
            Assert.IsTrue(WIAdministrationTurnSystem.TryQueueMuster(database, state, castle, state.PlayerFactionId, WIUnitRole.Melee, 1, army.ArmyId, out _));
            string id = castle.MusterOrder.HeroIds.Single();
            castle.FactionId = "valdor";
            WIAdministrationTurnSystem.ResolveMusterOrders(database, state, new WITurnSummary());
            Assert.AreEqual(10000, state.Gold);
            Assert.IsFalse(state.GetCharacter(id).Recruited);
            Assert.IsNull(castle.MusterOrder);
        }

        // 출정한 전투단에는 원격 충원하지 않고 공간이 있는 원래 모집 성에 대기시킵니다.
        [Test]
        public void DepartedArmy_ArrivalStaysAtRecruitingCastle()
        {
            Assert.IsTrue(WIAdministrationTurnSystem.TryQueueMuster(database, state, castle, state.PlayerFactionId, WIUnitRole.Melee, 1, army.ArmyId, out _));
            string id = castle.MusterOrder.HeroIds.Single();
            army.RemainingTravelMonths = 2;
            WIAdministrationTurnSystem.ResolveMusterOrders(database, state, new WITurnSummary());
            Assert.IsTrue(castle.HeroIds.Contains(id));
            Assert.IsFalse(army.Members.Any(member => member.HeroId == id));
        }

        // 예약과 자동 충원 지시는 저장 왕복 후 유지되고 완료 인물은 중복 생성되지 않습니다.
        [Test]
        public void SaveRoundTrip_PreservesOrderAndCompletesOnce()
        {
            Assert.IsTrue(WIAdministrationTurnSystem.SetMusterPolicy(database, state, castle, state.PlayerFactionId, WIUnitRole.Melee, army.ArmyId, 4, 60, true));
            Assert.IsTrue(WIAdministrationTurnSystem.TryQueueMuster(database, state, castle, state.PlayerFactionId, WIUnitRole.Melee, 1, army.ArmyId, out _));
            Assert.IsTrue(WICampaignSaveSystem.TryDeserialize(WICampaignSaveSystem.Serialize(state), out state, out string error), error);
            castle = state.GetCastle("castle_28");
            string id = castle.MusterOrder.HeroIds.Single();
            Assert.IsTrue(castle.MusterPolicy.Enabled);
            WIAdministrationTurnSystem.ResolveMusterOrders(database, state, new WITurnSummary());
            WIAdministrationTurnSystem.ResolveMusterOrders(database, state, new WITurnSummary());
            Assert.AreEqual(1, castle.HeroIds.Count(item => item == id));
            Assert.AreEqual(1, state.Armies.Sum(item => item.Members.Count(member => member.HeroId == id)));
        }

        // 자동 충원은 예산 범위에서 목표까지만 진행하고 출정 중에는 비용을 지불하지 않습니다.
        [Test]
        public void AutomaticMuster_StopsAtTargetAndWaitsWhileMoving()
        {
            Assert.IsTrue(WIAdministrationTurnSystem.SetMusterPolicy(database, state, castle, state.PlayerFactionId, WIUnitRole.Ranged, army.ArmyId, 2, 60, true));
            army.RemainingTravelMonths = 1;
            WIAdministrationTurnSystem.PlanMusterPolicies(database, state);
            Assert.IsNull(castle.MusterOrder);
            Assert.AreEqual(10000, state.Gold);
            army.RemainingTravelMonths = 0;
            WIAdministrationTurnSystem.PlanMusterPolicies(database, state);
            Assert.AreEqual(60, castle.MusterOrder.GoldPaid);
            WIAdministrationTurnSystem.ResolveMusterOrders(database, state, new WITurnSummary());
            WIAdministrationTurnSystem.PlanMusterPolicies(database, state);
            Assert.IsNull(castle.MusterOrder);
            Assert.AreEqual(2, army.Members.Count);
        }

        // 후보가 모두 사망·포로·고용 중이면 새 인물을 만들어 대체하지 않습니다.
        [Test]
        public void ExhaustedPool_DoesNotGenerateCharacters()
        {
            foreach (var candidate in state.Characters.Where(item => item.BaseGrade == WICharacterGrade.Common && item.Recruited == false))
            {
                candidate.IsDead = true;
            }
            int count = state.Characters.Count;
            Assert.IsFalse(WIAdministrationTurnSystem.TryQueueMuster(database, state, castle, state.PlayerFactionId, WIUnitRole.Melee, 1, army.ArmyId, out _));
            Assert.AreEqual(count, state.Characters.Count);
            Assert.AreEqual(10000, state.Gold);
        }

        // 메인에서도 AI가 같은 비용으로 모병하고 세력별 월간 예약 수 제한을 지킵니다.
        [Test]
        public void AIUsesMusterWithoutHeroRecruitmentPermission()
        {
            Assert.IsFalse(database.GetCampaignVariant(WICampaignVariant.AresMain).NonPlayerRecruitmentEnabled);
            int gold = state.Factions.Where(item => item.FactionId != state.PlayerFactionId).Sum(item => item.Gold);
            WIAdministrationTurnSystem.PlanAIMuster(database, state);
            var orders = state.Castles.Where(item => item.MusterOrder != null).Select(item => item.MusterOrder).ToList();
            Assert.Greater(orders.Count, 0);
            Assert.IsTrue(orders.All(order => order.HeroIds.All(id => state.IsCommonCharacter(id))));
            Assert.IsTrue(orders.GroupBy(order => order.FactionId).All(group => group.Count() <= database.MusterConfig.AIOrdersPerMonth));
            Assert.AreEqual(gold - orders.Sum(order => order.GoldPaid), state.Factions.Where(item => item.FactionId != state.PlayerFactionId).Sum(item => item.Gold));
        }

        // 일반 인물을 발견했더라도 영입 후보에서 제외하고 구 저장의 설득 지시를 해제합니다.
        [Test]
        public void HeroRecruitmentAndOldOrders_ExcludeCommons()
        {
            var common = state.Characters.First(item => item.BaseGrade == WICharacterGrade.Common && item.Recruited == false);
            common.Discovered = true;
            Assert.IsFalse(WIAdministrationTurnSystem.IsHeroRecruitmentCandidate(common));
            var actor = state.GetCharacter("lyria");
            actor.Activity = WICharacterActivityType.Recruit;
            actor.ActivityTargetHeroId = common.HeroId;
            actor.StandingActivity = WICharacterActivityType.Recruit;
            actor.StandingActivityTargetHeroId = common.HeroId;
            actor.RepeatActivity = true;
            state.NormalizeCharacterDuties();
            Assert.AreEqual(WICharacterActivityType.None, actor.Activity);
            Assert.IsFalse(actor.RepeatActivity);
            Assert.IsTrue(state.GetCharacter("common_alden").Recruited);
        }

        // 예약 후 빈자리가 사라지면 인물을 초과 배치하지 않고 비용을 반환합니다.
        [Test]
        public void LostArrivalSpace_RefundsWithoutRecruiting()
        {
            Assert.IsTrue(WIAdministrationTurnSystem.TryQueueMuster(database, state, castle, state.PlayerFactionId, WIUnitRole.Melee, 1, army.ArmyId, out _));
            string id = castle.MusterOrder.HeroIds.Single();
            Assert.IsTrue(WIAdministrationTurnSystem.DisbandArmy(state, army));
            WIAdministrationTurnSystem.ResolveMusterOrders(database, state, new WITurnSummary());
            Assert.AreEqual(10000, state.Gold);
            Assert.IsNull(castle.MusterOrder);
            Assert.IsFalse(state.GetCharacter(id).Recruited);
        }

        // 모병 필드가 없던 저장을 읽어도 인물과 금화는 보존하며 자동 충원은 시작하지 않습니다.
        [Test]
        public void LegacySaveWithoutMusterFields_RemainsCompatible()
        {
            string json = WICampaignSaveSystem.Serialize(state, false);
            json = System.Text.RegularExpressions.Regex.Replace(json, "\\\"MusterPolicy\\\":\\{[^}]*\\},?", "");
            json = json.Replace("\"MusterOrder\":null,", "");
            json = System.Text.RegularExpressions.Regex.Replace(json, "\\\"MusterOrder\\\":\\{[^}]*\\},?", "");
            Assert.IsTrue(WICampaignSaveSystem.TryDeserialize(json, out var restored, out string error), error);
            Assert.AreEqual(state.Characters.Count, restored.Characters.Count);
            Assert.AreEqual(state.Gold, restored.Gold);
            Assert.IsTrue(restored.Castles.All(item => item.MusterOrder == null && item.MusterPolicy.Enabled == false));
        }

        // 예약 없는 현재 저장도 역직렬화 후 가짜 예약을 만들지 않아 바로 모병할 수 있습니다.
        [Test]
        public void EmptyOrdersStayEmptyAfterSaveRoundTrip()
        {
            Assert.IsTrue(WICampaignSaveSystem.TryDeserialize(WICampaignSaveSystem.Serialize(state), out var restored, out string error), error);
            Assert.IsTrue(restored.Castles.All(item => item.MusterOrder == null));
            Assert.IsTrue(WIAdministrationTurnSystem.TryQueueMuster(database, restored, restored.GetCastle(castle.CastleId),
                restored.PlayerFactionId, WIUnitRole.Melee, 1, army.ArmyId, out error), error);
        }

        // 화면을 직접 조작하지 않고 실제 군사 프리팹의 열기·클릭 핸들러와 카드 바인딩을 검증합니다.
        [Test]
        public void ExistingMilitaryPrefab_OpensMusterAndQueuesFromRow()
        {
            GameObject host = new GameObject("WIMusterPrefabTest");
            host.SetActive(false);
            try
            {
                var controller = host.AddComponent<WIAdministrationUIController>();
                var flags = BindingFlags.NonPublic | BindingFlags.Instance;
                typeof(WIAdministrationUIController).GetField("database", flags).SetValue(controller, database);
                typeof(WIAdministrationUIController).GetField("state", flags).SetValue(controller, state);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Administration/WIAdministrationMilitaryUGUI.prefab");
                var instance = Object.Instantiate(prefab, host.transform);
                var panel = instance.GetComponent<WIAdministrationMilitaryUGUIController>();
                typeof(WIAdministrationUGUIPanelController).GetField("administrationController", flags).SetValue(panel, controller);
                var type = typeof(WIAdministrationMilitaryUGUIController);
                type.GetMethod("Push", flags).Invoke(panel, new object[] { "overview", "", 0 });
                var click = type.GetMethod("OpenItem", flags);
                click.Invoke(panel, new object[] { 0 });
                var snapshot = (WIAdministrationMilitarySnapshot)type.GetField("snapshot", flags).GetValue(panel);
                Assert.AreEqual("muster-castle", snapshot.Items[0].Kind);
                click.Invoke(panel, new object[] { 0 });
                snapshot = (WIAdministrationMilitarySnapshot)type.GetField("snapshot", flags).GetValue(panel);
                Assert.AreEqual("muster-queue", snapshot.Items[0].Kind);
                Assert.IsTrue(snapshot.Items[0].Interactable);
                click.Invoke(panel, new object[] { 0 });
                Assert.IsNotNull(castle.MusterOrder);
                snapshot = (WIAdministrationMilitarySnapshot)type.GetField("snapshot", flags).GetValue(panel);
                Assert.AreEqual("muster-cancel", snapshot.Items[0].Kind);
                click.Invoke(panel, new object[] { 0 });
                Assert.IsNull(castle.MusterOrder);
                Assert.AreEqual(10000, state.Gold);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        // 실제 군사 UI의 카드 명령으로 역할·합류 대상·모집·취소를 끝까지 연결합니다.
        [Test]
        public void MilitaryCards_QueueAndCancelThroughController()
        {
            GameObject host = new GameObject("WIMusterTest");
            host.SetActive(false);
            try
            {
                var controller = host.AddComponent<WIAdministrationUIController>();
                var flags = BindingFlags.NonPublic | BindingFlags.Instance;
                typeof(WIAdministrationUIController).GetField("database", flags).SetValue(controller, database);
                typeof(WIAdministrationUIController).GetField("state", flags).SetValue(controller, state);
                Assert.IsTrue(controller.TryGetUGUIMilitaryPanel("muster", castle.CastleId, 0, out var panel, out _));
                Assert.IsTrue(panel.Items.Any(item => item.Kind == "muster-role"));
                Assert.IsFalse(panel.Summary.Contains("UI_MUSTER"));
                Assert.IsTrue(controller.ExecuteUGUIMilitaryAction("muster-army", castle.CastleId, army.ArmyId, 0, out _, out _));
                Assert.IsTrue(controller.ExecuteUGUIMilitaryAction("muster-queue", castle.CastleId, "", 1, out _, out string error), error);
                Assert.IsNotNull(castle.MusterOrder);
                Assert.IsTrue(controller.ExecuteUGUIMilitaryAction("muster-cancel", castle.CastleId, "", 0, out _, out _));
                Assert.IsNull(castle.MusterOrder);
                Assert.AreEqual(10000, state.Gold);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }
    }
}

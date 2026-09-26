using System.Linq;
using System.Reflection;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using ProjectWI.Administration;
using ProjectWI.Battle;

namespace ProjectWI.Tests.Editor
{
    public class WIBorderDefenseTests
    {
        // 검증용 마스터 데이터를 읽으며 저장 파일과 원본 데이터는 변경하지 않습니다.
        private static WIAdministrationDatabaseSO Database()
        {
            return AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>("Assets/Data/ScriptableObject/Administration/WI_AdministrationDatabase.asset");
        }

        // 첫 공략은 수비 인물이 있는 실제 전투이며 키리엔 초기 네 명으로 끝까지 진행 가능한지 확인합니다.
        [Test]
        public void FirstAttackHasDefendersAndPlayableBattle()
        {
            var database = Database();
            var state = WIAdministrationState.Create(database, WICampaignDifficulty.Standard, WICampaignVariant.AresMain);
            Assert.AreEqual(3, state.GetCastle("castle_04").HeroIds.Count);
            var army = WIAdministrationTurnSystem.CreateArmy(database, state, state.GetCastle("castle_28"), "ares");
            foreach (string id in new[] { "lyria", "common_alden", "common_sable" })
            {
                var role = database.GetHeroClass(database.GetHero(id).HeroClass)?.RecommendedRole ?? WIUnitRole.Melee;
                Assert.IsTrue(WIAdministrationTurnSystem.AddArmyMember(database, state, army, id,
                    role == WIUnitRole.Commander ? WIUnitRole.Melee : role));
            }
            Assert.IsTrue(WIAdministrationTurnSystem.BeginArmyMarch(database, state, army, "castle_04"));
            for (int month = 0; month < 3 && state.BattleSessions.Count == 0; month++)
            {
                WIAdministrationTurnSystem.ExecuteTurn(database, state);
            }
            var battle = state.BattleSessions.Single(item => item.CastleId == "castle_04");
            Assert.AreEqual(WIBattleSessionStatus.Pending, battle.Status);
            Assert.AreEqual(3, battle.DefenderHeroIds.Count);
            var config = AssetDatabase.LoadAssetAtPath<WIBattleConfigSO>("Assets/Data/ScriptableObject/Battle/WI_BattleConfig.asset");
            var runtime = WIBattleRuntimeBuilder.Build(config, database, battle);
            var result = WIBattleSimulation.Step(config, runtime, 1f / 30f);
            Assert.AreEqual(WIBattleOutcome.None, result);
            int frames = 1;
            while (result == WIBattleOutcome.None && frames < 18000)
            {
                result = WIBattleSimulation.Step(config, runtime, 1f / 30f);
                frames++;
            }
            Directory.CreateDirectory("GameDocuments/DesignAuditEvidence");
            File.WriteAllText("GameDocuments/DesignAuditEvidence/Cardia-Defense-After.txt",
                $"초기 4명 대 수비 3명 · 전력 {battle.AttackerPowerSnapshot}:{battle.DefenderPowerSnapshot} · {result} · {frames / 30f:F2}초\r\n", new UTF8Encoding(false));
            Assert.AreNotEqual(WIBattleOutcome.None, result);
        }

        // 수비가 전혀 없는 성은 피해 없이 점령하고 보고에 이유를 남기며 전투 진입을 거절합니다.
        [TestCase(true, false)]
        [TestCase(false, false)]
        [TestCase(true, true)]
        public void EmptyCastleSkipsBattle(bool strategicFallback, bool existingPending)
        {
            var database = Database();
            var state = WIAdministrationState.Create(database, WICampaignDifficulty.Standard, WICampaignVariant.AresMain);
            state.UseStrategicBattleFallback = strategicFallback;
            var target = state.GetCastle("castle_04");
            target.HeroIds.Clear();
            var army = WIAdministrationTurnSystem.CreateArmy(database, state, state.GetCastle("castle_28"), "ares");
            state.GetCastle("castle_28").HeroIds.Remove("ares");
            army.CurrentCastleId = target.CastleId;
            army.AwaitingBattle = true;
            if (existingPending == true)
            {
                WIAdministrationTurnSystem.CreateBattleSession(database, state, army);
            }
            int experience = state.GetCharacter("ares").Experience;
            int injury = state.GetCharacter("ares").InjuryMonths;
            var summary = new WITurnSummary();
            summary.News.AddRange(new[] { "기존 소식 1", "기존 소식 2", "기존 소식 3" });
            typeof(WIAdministrationTurnSystem).GetMethod("ResolveStrategicBattles", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { database, state, summary });
            var battle = state.BattleSessions.Single();
            Assert.AreEqual(state.PlayerFactionId, target.FactionId);
            Assert.IsFalse(army.AwaitingBattle);
            Assert.AreEqual(WIBattleResolutionSource.UnopposedOccupation, battle.ResolutionSource);
            Assert.IsFalse(WIAdministrationTurnSystem.BeginRealTimeBattle(state, battle.SessionId));
            Assert.AreEqual(experience, state.GetCharacter("ares").Experience);
            Assert.AreEqual(injury, state.GetCharacter("ares").InjuryMonths);
            Assert.IsTrue(summary.News.Any(item => item.Contains("방어 병력이 없어")));
            Assert.IsTrue(summary.News.First().Contains("방어 병력이 없어"));
            Assert.IsFalse(target.PendingGovernorAppointment);
            Assert.IsTrue(target.DelegatedToGovernor);
            Assert.AreEqual(0, army.ReorganizationMonths);
        }

        // 출정 가능한 전투단이라도 적 접경 성의 마지막 수비라면 AI가 이동시키지 않습니다.
        [Test]
        public void AIKeepsLastBorderArmy()
        {
            var database = Database();
            var state = WIAdministrationState.Create(database, WICampaignDifficulty.Standard, WICampaignVariant.AresMain);
            var castle = state.GetCastle("castle_04");
            var ids = castle.HeroIds.ToList();
            var army = WIAdministrationTurnSystem.CreateArmy(database, state, castle, ids[0]);
            foreach (string id in ids.Skip(1))
            {
                Assert.IsTrue(WIAdministrationTurnSystem.AddArmyMember(database, state, army, id, WIUnitRole.Melee));
            }
            state.Turn = 24;
            typeof(WIAdministrationTurnSystem).GetMethod("PlanAIActions", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { database, state, new WITurnSummary() });
            Assert.AreEqual(WIArmyMission.Defend, army.Mission);
            Assert.IsFalse(army.IsMoving);
            Assert.AreEqual(castle.CastleId, army.CurrentCastleId);
        }

        // 월 진행 중 카르디아 수비가 비지 않고 평시 증원이 설정 규모에서 멈추는지 확인합니다.
        [Test]
        public void MainFrontlineRetainsDefenseWithoutEndlessReinforcements()
        {
            var database = Database();
            var state = WIAdministrationState.Create(database, WICampaignDifficulty.Standard, WICampaignVariant.AresMain);
            var log = new StringBuilder();
            for (int month = 0; month < 24; month++)
            {
                WIAdministrationTurnSystem.ExecuteTurn(database, state);
                var castle = state.GetCastle("castle_04");
                log.AppendLine($"{state.Turn}턴 카르디아 수비 등록 인물 {castle.HeroIds.Count}명");
                Assert.GreaterOrEqual(castle.HeroIds.Count, 2, $"월 {month}");
                Assert.LessOrEqual(castle.HeroIds.Count, 9, $"월 {month}: 증원 단위 최대 4명에 따른 초과 범위");
            }
            File.WriteAllText("GameDocuments/DesignAuditEvidence/Cardia-Reinforcement-After.txt", log.ToString(), new UTF8Encoding(false));
        }
    }
}

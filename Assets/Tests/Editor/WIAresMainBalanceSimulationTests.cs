using System.Linq;
using System.Text;
using NUnit.Framework;
using ProjectWI.Administration;
using UnityEditor;
using UnityEngine;

namespace ProjectWI.Tests.Editor
{
    public class WIAresMainBalanceSimulationTests
    {
        private const string DatabasePath =
            "Assets/Data/ScriptableObject/Administration/WI_AdministrationDatabase.asset";

        // 아레스 메인의 정책별 장기 진행을 반복 실행하고 밸런스 검토용 원시 지표를 출력합니다.
        [Test]
        public void AresMain_PoliciesProduceBalanceReport()
        {
            WIAdministrationDatabaseSO database =
                AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAutoPlayerPolicy[] policies =
            {
                WIAutoPlayerPolicy.Administration,
                WIAutoPlayerPolicy.Balanced,
                WIAutoPlayerPolicy.Aggressive
            };
            int[] durations = { 24, 60, 120 };
            StringBuilder report = new StringBuilder();

            foreach (WIAutoPlayerPolicy policy in policies)
            {
                foreach (int duration in durations)
                {
                    for (int seed = 1; seed <= 5; seed += 1)
                    {
                        Random.InitState(seed * 7919 + duration);
                        WIAdministrationState state = WIAdministrationState.Create(
                            database, WICampaignDifficulty.Standard, WICampaignVariant.AresMain);
                        WIAutoCampaignMetrics metrics = WICampaignAutoPlayer.Run(
                            database, state, policy, duration, true);
                        WIFactionRuntimeState player = state.GetFactionState(state.PlayerFactionId);
                        int armyPower = state.Armies
                            .Where(army => army.FactionId == state.PlayerFactionId && army.IsOperational)
                            .Sum(army => WIAdministrationTurnSystem.GetArmyBattlePower(database, state, army));
                        int firstBattleMonth = metrics.BattleIntervals.Count > 0
                            ? metrics.BattleIntervals[0]
                            : -1;
                        string castleCounts = string.Join(",", state.Factions.Select(faction =>
                            faction.FactionId + ":" + state.Castles.Count(castle =>
                                castle.FactionId == faction.FactionId)));

                        report.AppendLine($"{policy}|{duration}|{seed}" +
                            $"|months={metrics.MonthsSimulated}|battle={metrics.BattlesResolved}" +
                            $"|win={metrics.PlayerVictories}|loss={metrics.PlayerDefeats}" +
                            $"|first={firstBattleMonth}|march={metrics.MarchesStarted}" +
                            $"|change={metrics.OwnershipChanges}|pc={metrics.FinalPlayerCastleCount}" +
                            $"|result={metrics.CampaignResult}|gold={player.Gold}" +
                            $"|mana={player.ManaCrystal}|inf={player.Influence}|army={armyPower}" +
                            $"|chars={metrics.FinalEmployedCharacters}:" +
                            $"{metrics.FinalHeroCharacters}:{metrics.FinalCommonCharacters}" +
                            $"|talent={metrics.CharactersDiscovered}:" +
                            $"{metrics.CharactersRecruited}:{metrics.CommonCharactersReturned}:" +
                            $"{metrics.FinalWanderingCharacters}" +
                            $"|counts={castleCounts}");
                    }
                }
            }

            Debug.Log(report.ToString());
            Assert.IsTrue(report.Length > 0);
        }

        // 카르디아 첫 공략의 전력과 결과를 확인해 1년 점령 조건을 조정할 근거를 출력합니다.
        [Test]
        public void AresMain_CardiaFirstYearProducesBattleDiagnostics()
        {
            WIAdministrationDatabaseSO database =
                AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(
                database, WICampaignDifficulty.Standard, WICampaignVariant.AresMain);
            WIAutoCampaignMetrics metrics = WICampaignAutoPlayer.Run(
                database, state, WIAutoPlayerPolicy.Aggressive, 12, true);
            string sessions = string.Join(" / ", state.BattleSessions.Select(session =>
                $"{session.AttackerFactionId}>{session.DefenderFactionId}@{session.CastleId}:" +
                $"{session.AttackerPowerSnapshot}-{session.DefenderPowerSnapshot}:" +
                $"{session.AttackerOutcome}"));

            Debug.Log($"카르디아 1년 진단 · 성 {metrics.FinalPlayerCastleCount} · " +
                $"원정 {metrics.MarchesStarted} · 전투 {sessions}");
            Assert.AreEqual("avalon", state.GetCastle("castle_04").FactionId,
                "공세형 아레스는 첫해 안에 카르디아를 점령해야 합니다.");
        }
    }
}

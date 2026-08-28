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

        // 현재 아레스 메인을 난이도·정책 9개 조합으로 240개월 실행해 Lab과 같은 전체 표본을 출력합니다.
        [Test]
        public void AresMain_240MonthsFullMatrixProducesReport()
        {
            WIAdministrationDatabaseSO database =
                AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            StringBuilder report = new StringBuilder("아레스 메인 240개월 전체 매트릭스\n");
            foreach (WICampaignDifficulty difficulty in System.Enum.GetValues(typeof(WICampaignDifficulty)))
            {
                foreach (WIAutoPlayerPolicy policy in System.Enum.GetValues(typeof(WIAutoPlayerPolicy)))
                {
                    WIAdministrationState state = WIAdministrationState.Create(
                        database, difficulty, WICampaignVariant.AresMain);
                    WIAutoCampaignMetrics metrics = WICampaignAutoPlayer.Run(
                        database, state, policy, 240, false);
                    report.AppendLine($"{difficulty}/{policy} · 성 {metrics.FinalPlayerCastleCount} · " +
                        $"전투 {metrics.BattlesResolved}({metrics.PlayerVictories}/{metrics.PlayerDefeats}) · " +
                        $"인물 {metrics.FinalEmployedCharacters}({metrics.FinalHeroCharacters}/{metrics.FinalCommonCharacters}) · " +
                        $"발견/영입/복귀/사망 {metrics.CharactersDiscovered}/{metrics.CharactersRecruited}/" +
                        $"{metrics.CommonCharactersReturned}/{metrics.CharacterDeaths} · {metrics.CampaignResult}");
                }
            }

            Debug.Log(report.ToString());
            Assert.IsTrue(report.Length > 0);
        }

        // 표준 난이도 아레스 메인을 정책별 240개월 실행해 고용 수치가 단일 캠페인 기준인지 확인합니다.
        [Test]
        public void AresMain_240MonthsReportsRecruitmentPerIndependentRun()
        {
            WIAdministrationDatabaseSO database =
                AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            StringBuilder report = new StringBuilder("아레스 메인 표준 240개월\n");
            foreach (WIAutoPlayerPolicy policy in System.Enum.GetValues(typeof(WIAutoPlayerPolicy)))
            {
                WIAdministrationState state = WIAdministrationState.Create(
                    database, WICampaignDifficulty.Standard, WICampaignVariant.AresMain);
                WIAutoCampaignMetrics metrics = WICampaignAutoPlayer.Run(
                    database, state, policy, 240, false);
                report.AppendLine($"{policy} · 진행 {metrics.MonthsSimulated} · " +
                    $"최종 {metrics.FinalEmployedCharacters}({metrics.FinalHeroCharacters}/{metrics.FinalCommonCharacters}) · " +
                    $"신규 영입 성공 {metrics.CharactersRecruited} · 발견 {metrics.CharactersDiscovered} · " +
                    $"복귀 {metrics.CommonCharactersReturned} · 사망 {metrics.CharacterDeaths} · " +
                    $"성 {metrics.FinalPlayerCastleCount} · 결과 {metrics.CampaignResult}");

                Assert.LessOrEqual(metrics.CharactersRecruited, 250,
                    "단일 실행의 신규 영입 성공은 초기 미고용 250명을 넘을 수 없습니다.");
            }
            Debug.Log(report.ToString());
        }

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
                            $"|death={metrics.CharacterDeaths}:" +
                            $"{metrics.PermanentHeroDeaths}:{metrics.CommonCharacterDeaths}" +
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

        // 아레스는 기존 일행에 일반 10명을 더한 14명으로 시작하고 전체 로스터 절반이 배치되는지 검증합니다.
        [Test]
        public void ScenarioCharacterPlacements_AssignHalfRosterAndKeepAresNarrativePartySmall()
        {
            WIAdministrationDatabaseSO database =
                AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(
                database, WICampaignDifficulty.Standard, WICampaignVariant.AresMain);

            Assert.AreEqual(500, state.Characters.Count);
            Assert.AreEqual(250, state.Castles.Sum(castle => castle.HeroIds.Count));
            Assert.AreEqual(14, state.Castles.Where(castle => castle.FactionId == state.PlayerFactionId)
                .Sum(castle => castle.HeroIds.Count));
            Assert.AreEqual(10, state.GetCastle("castle_28").HeroIds.Count(heroId =>
                state.GetCharacter(heroId).BaseGrade == WICharacterGrade.Common));
            Assert.AreEqual(4, state.GetCastle("castle_28").HeroIds.Count(heroId =>
                state.GetCharacter(heroId).BaseGrade == WICharacterGrade.Hero));
            Assert.AreEqual(250, state.Characters.Count(character => character.Recruited));
            Debug.Log("아레스 메인 시작 인물 · " + string.Join(", ", state.Factions.Select(faction =>
                faction.FactionId + ":" + state.Castles.Where(castle => castle.FactionId == faction.FactionId)
                    .Sum(castle => castle.HeroIds.Count))));
        }

        // 일반 등급은 개인 활동과 내정·영지관·연구 담당자가 될 수 없는지 검증합니다.
        [Test]
        public void CommonCharacters_CannotPerformAdministration()
        {
            WIAdministrationDatabaseSO database =
                AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(
                database, WICampaignDifficulty.Standard, WICampaignVariant.AresMain);
            WICastleRuntimeState frosthorn = state.GetCastle("castle_28");
            string commonId = frosthorn.HeroIds.First(heroId =>
                state.GetCharacter(heroId).BaseGrade == WICharacterGrade.Common);

            Assert.IsFalse(WIAdministrationTurnSystem.IsAdministrationCapable(state, commonId));
            Assert.IsFalse(WIAdministrationTurnSystem.CanPerformCharacterActivity(
                state, commonId, WICharacterActivityType.Search));
            Assert.IsFalse(WIAdministrationTurnSystem.CanPerformCharacterActivity(
                state, commonId, WICharacterActivityType.Training));
            Assert.IsFalse(WIAdministrationTurnSystem.AssignGovernor(state, frosthorn.CastleId, commonId));
            Assert.IsFalse(WIAdministrationTurnSystem.BeginResearch(
                database, state, state.PlayerFactionId, database.ResearchDefinitions.First().Id, commonId));
            Assert.IsNull(WIAdministrationTurnSystem.SelectAIProjectManager(
                database, state, new WICastleRuntimeState
                {
                    CastleId = frosthorn.CastleId,
                    HeroIds = new System.Collections.Generic.List<string> { commonId }
                }, WICastleProjectType.Prosperity));
        }

        // 프리 시나리오에서는 비플레이어 군주도 남은 방랑 인물을 실제로 고용하는지 검증합니다.
        [Test]
        public void FreeScenario_NonPlayerFactionsRecruitCharacters()
        {
            WIAdministrationDatabaseSO database =
                AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(
                database, WICampaignDifficulty.Standard, WICampaignVariant.Free);
            int initialRecruited = state.Characters.Count(character => character.Recruited);

            for (int month = 0; month < 13; month += 1)
            {
                WIAdministrationTurnSystem.ExecuteTurn(database, state);
            }

            Assert.Greater(state.Characters.Count(character => character.Recruited), initialRecruited);
        }
    }
}

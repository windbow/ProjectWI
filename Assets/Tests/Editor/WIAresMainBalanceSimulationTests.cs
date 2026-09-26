using System.Linq;
using System.Text;
using NUnit.Framework;
using ProjectWI.Administration;
using ProjectWI.Editor;
using UnityEditor;
using UnityEngine;

namespace ProjectWI.Tests.Editor
{
    public class WIAresMainBalanceSimulationTests
    {
        private const string DatabasePath =
            "Assets/Data/ScriptableObject/Administration/WI_AdministrationDatabase.asset";

        // 현재 키리엔 메인을 난이도·정책 9개 조합으로 240개월 실행해 Lab과 같은 전체 표본을 출력합니다.
        [Test]
        public void AresMain_240MonthsFullMatrixProducesReport()
        {
            WIAdministrationDatabaseSO database =
                AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            StringBuilder report = new StringBuilder("키리엔 메인 240개월 전체 매트릭스\n");
            int relaxedDeaths = 0;
            int standardDeaths = 0;
            int hardDeaths = 0;
            foreach (WICampaignDifficulty difficulty in System.Enum.GetValues(typeof(WICampaignDifficulty)))
            {
                foreach (WIAutoPlayerPolicy policy in System.Enum.GetValues(typeof(WIAutoPlayerPolicy)))
                {
                    WIAdministrationState state = WIAdministrationState.Create(
                        database, difficulty, WICampaignVariant.AresMain);
                    WIAutoCampaignMetrics metrics = WICampaignAutoPlayer.Run(
                        database, state, policy, 240, false);
                    if (difficulty == WICampaignDifficulty.Relaxed)
                    {
                        relaxedDeaths += metrics.CharacterDeaths;
                    }
                    else if (difficulty == WICampaignDifficulty.Hard)
                    {
                        hardDeaths += metrics.CharacterDeaths;
                    }
                    else
                    {
                        standardDeaths += metrics.CharacterDeaths;
                    }
                    report.AppendLine($"{difficulty}/{policy} · 성 {metrics.FinalPlayerCastleCount} · " +
                        $"전투 {metrics.BattlesResolved}({metrics.PlayerVictories}/{metrics.PlayerDefeats}) · " +
                        $"인물 {metrics.FinalEmployedCharacters}({metrics.FinalHeroCharacters}/{metrics.FinalCommonCharacters}) · " +
                        $"발견/영입/복귀/사망 {metrics.CharactersDiscovered}/{metrics.CharactersRecruited}/" +
                        $"{metrics.CommonCharactersReturned}/{metrics.CharacterDeaths} · {metrics.CampaignResult}");
                }
            }

            Debug.Log(report.ToString());
            Assert.AreEqual(0, relaxedDeaths, "여유 난이도에서는 전투 사망이 발생하지 않아야 합니다.");
            Assert.Greater(standardDeaths, 0, "표준 난이도의 장기 전투에서는 사망 판정 경로가 실제로 실행되어야 합니다.");
            Assert.Greater(hardDeaths, 0, "도전 난이도의 장기 전투에서는 사망 판정 경로가 실제로 실행되어야 합니다.");
        }

        // 표준 난이도 키리엔 메인을 정책별 240개월 실행해 고용 수치가 단일 캠페인 기준인지 확인합니다.
        [Test]
        public void AresMain_240MonthsReportsRecruitmentPerIndependentRun()
        {
            WIAdministrationDatabaseSO database =
                AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            StringBuilder report = new StringBuilder("키리엔 메인 표준 240개월\n");
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
                    $"포로 {metrics.CharacterCaptures} · 적 합류 {metrics.CharacterDefections} · " +
                    $"교환 {metrics.PrisonerExchanges} · 몸값 {metrics.PrisonerRansoms} · 포로 귀환 {metrics.PrisonersRecovered} · " +
                    $"성 {metrics.FinalPlayerCastleCount} · 승패 {metrics.PlayerVictories}/{metrics.PlayerDefeats} · " +
                    $"출정 {metrics.MarchesStarted} · 보충 {metrics.ArmyReinforcements} · " +
                    $"목표 포기 {metrics.GoalsAbandoned} · 결과 {metrics.CampaignResult}");
                report.AppendLine("  판단: " + string.Join(", ", metrics.DecisionReasonCounts
                    .OrderByDescending(item => item.Value)
                    .Take(6)
                    .Select(item => $"{item.Key}={item.Value}")));
                report.AppendLine("  전투단: " + string.Join(", ", state.Armies
                    .Where(army => army.FactionId == state.PlayerFactionId)
                    .Select(army => $"{army.ArmyId}@{army.CurrentCastleId}:{army.Mission}:" +
                        $"{army.Members.Count}명/{WIAdministrationTurnSystem.GetArmyBattlePower(database, state, army)}")));
                report.AppendLine("  성별 대기 일반: " + string.Join(", ", state.Castles
                    .Where(castle => castle.FactionId == state.PlayerFactionId)
                    .Select(castle => $"{castle.CastleId}=" + castle.HeroIds.Count(heroId =>
                        state.GetCharacter(heroId)?.BaseGrade == WICharacterGrade.Common &&
                        state.IsCharacterBusy(heroId) == false))));
                report.AppendLine("  마지막 판단: " + string.Join(" / ", metrics.DecisionTraces
                    .TakeLast(12)
                    .Select(trace => $"{trace.Month}:{trace.ReasonCode}:{trace.TargetCastleId}")));

                Assert.LessOrEqual(metrics.CharactersRecruited, 950,
                    "단일 실행의 신규 영입 성공은 초기 재야 950명을 넘을 수 없습니다.");
                if (policy == WIAutoPlayerPolicy.Balanced)
                {
                    if (metrics.PlayerDefeats > 0)
                    {
                        Assert.GreaterOrEqual(metrics.FinalPlayerCastleCount, 10,
                            "균형형은 패전 뒤 보충·재집결하여 장기 확장을 재개해야 합니다.");
                    }
                }
            }
            Debug.Log(report.ToString());
        }

        // 표준 난이도 내정형 240개월을 5시드로 반복해 인물 운명과 확장 분포를 출력합니다.
        [Test]
        public void AresMain_Standard240AdministrationFiveSeedsProducesFateStatistics()
        {
            RunStandardFiveSeedStatistics(WIAutoPlayerPolicy.Administration);
        }

        // 표준 난이도 균형형 240개월을 5시드로 반복해 인물 운명과 확장 분포를 출력합니다.
        [Test]
        public void AresMain_Standard240BalancedFiveSeedsProducesFateStatistics()
        {
            RunStandardFiveSeedStatistics(WIAutoPlayerPolicy.Balanced);
        }

        // 표준 난이도 공세형 240개월을 5시드로 반복해 인물 운명과 확장 분포를 출력합니다.
        [Test]
        public void AresMain_Standard240AggressiveFiveSeedsProducesFateStatistics()
        {
            RunStandardFiveSeedStatistics(WIAutoPlayerPolicy.Aggressive);
        }

        // 공세형 키리엔 메인이 720개월 동안 다수 표본에서 발도르 멸망에 도달하는지 5시드로 확인합니다.
        [Test]
        public void AresMain_Standard720AggressiveFiveSeedsMostlyDefeatValdor()
        {
            WIAdministrationDatabaseSO database =
                AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            System.Collections.Generic.List<WICampaignAutoTestSample> samples =
                new System.Collections.Generic.List<WICampaignAutoTestSample>();
            for (int seed = 1; seed <= 5; seed += 1)
            {
                WIAdministrationState state = WIAdministrationState.Create(
                    database, WICampaignDifficulty.Standard, WICampaignVariant.AresMain);
                state.SimulationSeed = seed;
                samples.Add(new WICampaignAutoTestSample
                {
                    Seed = seed,
                    Metrics = WICampaignAutoPlayer.Run(
                        database, state, WIAutoPlayerPolicy.Aggressive, 720, true)
                });
            }

            WICampaignAutoBatchStatistics statistics = WICampaignAutoBatchStatistics.Create(samples);
            int victoryCount = samples.Count(item => item.Metrics.CampaignResult == WICampaignResult.Victory);
            string report = $"표준 720개월 공세형 5시드 · 승리 " +
                $"{victoryCount}/5 · " +
                $"완료개월 {statistics.CompletionMonths} · 발도르 잔여성 {statistics.ValdorCastles} · " +
                $"플레이어성 {statistics.FinalCastles} · " + string.Join(" / ", samples.Select(item =>
                    $"시드{item.Seed}:{item.Metrics.CampaignResult}:{item.Metrics.MonthsSimulated}개월:" +
                    $"발도르{item.Metrics.FinalValdorCastleCount}:키리엔{item.Metrics.FinalPlayerCastleCount}:" +
                    $"군단{item.Metrics.FinalOperationalArmyCount}:병력{item.Metrics.FinalArmyMemberCount}:" +
                    $"이동{item.Metrics.FinalMovingArmyCount}:대기{item.Metrics.FinalAwaitingBattleArmyCount}:재편{item.Metrics.FinalReorganizingArmyCount}:" +
                    $"이동월{item.Metrics.MovingArmyMonths}/{item.Metrics.LongestMovingArmyMonths}:" +
                    $"대기월{item.Metrics.AwaitingBattleArmyMonths}/{item.Metrics.LongestAwaitingBattleArmyMonths}:" +
                    $"재편월{item.Metrics.ReorganizingArmyMonths}/{item.Metrics.LongestReorganizingArmyMonths}:" +
                    $"전력{item.Metrics.FinalPlayerArmyPower}:피로{item.Metrics.FinalAverageArmyFatigue}:" +
                    $"유휴일반{item.Metrics.FinalIdleCommonCharacters}:접경{item.Metrics.FinalValdorBorderCastleCount}:" +
                    $"공격가능{item.Metrics.FinalAttackableValdorBorderCount}:무원정{item.Metrics.FinalCurrentNoMarchMonths}:" +
                    $"목표{item.Metrics.FinalStrategicTargetCastleId}:" +
                    $"집결{item.Metrics.FinalStrategicAssemblyPower}/{item.Metrics.FinalStrategicRequiredPower}"));
            Debug.Log(report);
            Assert.GreaterOrEqual(victoryCount, 3,
                $"공세형 키리엔 메인은 720개월 안에 과반 표본에서 발도르를 멸망시켜야 합니다. {report}");
            Assert.LessOrEqual(statistics.ValdorCastles.Median, 0f,
                $"720개월 발도르 잔여 성 중앙값은 0이어야 합니다. {report}");
        }

        // 키리엔 메인의 정책별 장기 진행을 반복 실행하고 밸런스 검토용 원시 지표를 출력합니다.
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
                        WIAdministrationState state = WIAdministrationState.Create(
                            database, WICampaignDifficulty.Standard, WICampaignVariant.AresMain);
                        state.SimulationSeed = seed;
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
                            $"|capture={metrics.CharacterCaptures}|defect={metrics.CharacterDefections}" +
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
            Assert.AreEqual("rimgard", state.GetCastle("castle_04").FactionId,
                "공세형 키리엔은 첫해 안에 카르디아를 점령해야 합니다.");
        }

        // 키리엔 영웅 2명·일반 2명과 축소된 전체 초기 배치 및 재야 인원을 검증합니다.
        [Test]
        public void ScenarioCharacterPlacements_AssignReducedRosterAndKeepAresNarrativePartySmall()
        {
            WIAdministrationDatabaseSO database =
                AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(
                database, WICampaignDifficulty.Standard, WICampaignVariant.AresMain);

            Assert.AreEqual(1200, state.Characters.Count);
            Assert.AreEqual(150, state.Castles.Sum(castle => castle.HeroIds.Count));
            Assert.AreEqual(60, state.Castles.SelectMany(castle => castle.HeroIds)
                .Count(heroId => state.GetCharacter(heroId).BaseGrade == WICharacterGrade.Hero));
            Assert.AreEqual(90, state.Castles.SelectMany(castle => castle.HeroIds)
                .Count(heroId => state.GetCharacter(heroId).BaseGrade == WICharacterGrade.Common));
            Assert.AreEqual(140, state.Characters.Count(character =>
                character.BaseGrade == WICharacterGrade.Hero && character.Recruited == false));
            Assert.AreEqual(910, state.Characters.Count(character =>
                character.BaseGrade == WICharacterGrade.Common && character.Recruited == false));
            Assert.AreEqual(4, state.Castles.Where(castle => castle.FactionId == state.PlayerFactionId)
                .Sum(castle => castle.HeroIds.Count));
            Assert.AreEqual(2, state.GetCastle("castle_28").HeroIds.Count(heroId =>
                state.GetCharacter(heroId).BaseGrade == WICharacterGrade.Common));
            Assert.AreEqual(2, state.GetCastle("castle_28").HeroIds.Count(heroId =>
                state.GetCharacter(heroId).BaseGrade == WICharacterGrade.Hero));
            Assert.AreEqual(150, state.Characters.Count(character => character.Recruited));
            Debug.Log("키리엔 메인 시작 인물 · " + string.Join(", ", state.Factions.Select(faction =>
                faction.FactionId + ":" + state.Castles.Where(castle => castle.FactionId == faction.FactionId)
                    .Sum(castle => castle.HeroIds.Count))));
        }

        // 내정 특성이 없는 일반 인물은 행정을 맡지 못하지만 훈련은 할 수 있는지 검증합니다.
        [Test]
        public void CommonCharactersWithoutTrait_CannotPerformAdministration()
        {
            WIAdministrationDatabaseSO database =
                AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(
                database, WICampaignDifficulty.Standard, WICampaignVariant.AresMain);
            WICastleRuntimeState frosthorn = state.GetCastle("castle_28");
            string commonId = frosthorn.HeroIds.First(heroId =>
                state.GetCharacter(heroId).BaseGrade == WICharacterGrade.Common);

            state.GetCharacter(commonId).Traits.Remove(WITraitType.Administration);
            Assert.IsFalse(WIAdministrationTurnSystem.IsAdministrationCapable(state, commonId));
            Assert.IsFalse(WIAdministrationTurnSystem.CanPerformCharacterActivity(
                state, commonId, WICharacterActivityType.Search));
            Assert.IsTrue(WIAdministrationTurnSystem.CanPerformCharacterActivity(
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

        // 전략 자동 전투의 전술 변동이 같은 상태에서는 재현되고 근소 우세의 이변을 허용하는지 검증합니다.
        [Test]
        public void StrategicBattleOutcome_IsDeterministicAndAllowsCloseUpsets()
        {
            WIAdministrationDatabaseSO database =
                AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(
                database, WICampaignDifficulty.Standard, WICampaignVariant.AresMain);
            WIBattleSessionState session = new WIBattleSessionState
            {
                SessionId = "variation_test",
                AttackerPowerSnapshot = 110,
                DefenderPowerSnapshot = 100
            };

            WIBattleOutcome first = WICampaignAutoPlayer.ResolveStrategicBattleOutcome(state, session);
            WIBattleOutcome second = WICampaignAutoPlayer.ResolveStrategicBattleOutcome(state, session);
            Assert.AreEqual(first, second);

            bool foundUpset = false;
            for (int turn = 1; turn <= 100; turn += 1)
            {
                state.Turn = turn;
                session.SessionId = $"variation_test_{turn}";
                if (WICampaignAutoPlayer.ResolveStrategicBattleOutcome(state, session) == WIBattleOutcome.Defeat)
                {
                    foundUpset = true;
                    break;
                }
            }
            Assert.IsTrue(foundUpset, "표준 난이도의 근소 우세 전투에서는 전술적 이변이 발생할 수 있어야 합니다.");
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

        // 지정 정책의 표준 240개월 캠페인을 5개 시드로 실행하고 통계 한 줄을 출력합니다.
        private static void RunStandardFiveSeedStatistics(WIAutoPlayerPolicy policy)
        {
            WIAdministrationDatabaseSO database =
                AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            System.Collections.Generic.List<WICampaignAutoTestSample> samples =
                new System.Collections.Generic.List<WICampaignAutoTestSample>();
            System.Collections.Generic.Dictionary<int, string> terminalDiagnostics =
                new System.Collections.Generic.Dictionary<int, string>();
            for (int seed = 1; seed <= 5; seed += 1)
            {
                WIAdministrationState state = WIAdministrationState.Create(
                    database, WICampaignDifficulty.Standard, WICampaignVariant.AresMain);
                state.SimulationSeed = seed;
                samples.Add(new WICampaignAutoTestSample
                {
                    Seed = seed,
                    Metrics = WICampaignAutoPlayer.Run(database, state, policy, 240, false)
                });
                terminalDiagnostics[seed] = "전투단 " + string.Join(",", state.Armies
                    .Where(army => army.FactionId == state.PlayerFactionId)
                    .Select(army => $"{army.CurrentCastleId}:{army.Members.Count}:" +
                        $"{WIAdministrationTurnSystem.GetArmyBattlePower(database, state, army)}:{army.Mission}"));
            }

            WICampaignAutoBatchStatistics statistics = WICampaignAutoBatchStatistics.Create(samples);
            string report = $"표준 240개월 5시드 · {policy} · 성 {statistics.FinalCastles} · " +
                $"승 {statistics.Victories} · 패 {statistics.Defeats} · 사망 {statistics.Deaths} · " +
                $"영웅사망 {statistics.PermanentHeroDeaths} · 포로 {statistics.Captures} · " +
                $"전향 {statistics.Defections}";
            foreach (WICampaignAutoTestSample sample in samples)
            {
                WIAutoCampaignMetrics metrics = sample.Metrics;
                string reasons = string.Join("/", new[]
                {
                    "NO_ATTACKABLE_TARGET", "DEFEAT_RECOVERY", "THREAT_RESPONSE",
                    "MARCH_STARTED", "ARMY_REINFORCED", "GOAL_ABANDONED"
                }.Select(code => $"{code}:{(metrics.DecisionReasonCounts.TryGetValue(code, out int count) ? count : 0)}"));
                report += $"\n시드 {sample.Seed} · 성 {metrics.FinalPlayerCastleCount} · " +
                    $"승패 {metrics.PlayerVictories}/{metrics.PlayerDefeats} · 목표포기 {metrics.GoalsAbandoned} · " +
                    $"최장무원정 {metrics.LongestNoMarchMonths} · 보충 {metrics.ArmyReinforcements} · {reasons} · " +
                    terminalDiagnostics[sample.Seed];
            }
            Debug.Log(report);
            Assert.AreEqual(5, samples.Count);
            if (policy == WIAutoPlayerPolicy.Aggressive)
            {
                Assert.GreaterOrEqual(samples.Min(item => item.Metrics.FinalPlayerCastleCount), 12,
                    "공세형은 단일 강적 전선에 봉쇄돼 6성에서 장기 정체하면 안 됩니다.");
                Assert.Greater(samples.Sum(item => item.Metrics.WarsDeclared), 0,
                    "공세형 반복 표본은 장기 교착 시 대체 전선 개방 정책을 실제로 검증해야 합니다.");
            }
        }
    }
}

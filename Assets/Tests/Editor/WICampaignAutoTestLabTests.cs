using System.Collections.Generic;
using NUnit.Framework;
using ProjectWI.Administration;
using ProjectWI.Editor;
using UnityEditor;
using UnityEngine.UIElements;

namespace ProjectWI.Tests.Editor
{
    public class WICampaignAutoTestLabTests
    {
        private const string DatabasePath = "Assets/Data/ScriptableObject/Administration/WI_AdministrationDatabase.asset";
        private const string LayoutPath = "Assets/Editor/UI/WICampaignAutoTestLab.uxml";

        // 캠페인 결말이 이미 확정된 상태에서는 자동 플레이가 추가 턴을 진행하지 않는지 검증합니다.
        [Test]
        public void AutoPlayer_StopWhenCampaignEnds_DoesNotAdvanceFinishedCampaign()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            state.CampaignResult = WICampaignResult.Defeat;

            WIAutoCampaignMetrics metrics = WICampaignAutoPlayer.Run(
                database, state, WIAutoPlayerPolicy.Balanced, 240, true);

            Assert.AreEqual(0, metrics.MonthsSimulated);
            Assert.AreEqual(WICampaignResult.Defeat, metrics.CampaignResult);
        }

        // 자동 테스트 랩의 결과 행 아홉 개가 UXML에 고정 배치되어 있는지 검증합니다.
        [Test]
        public void CampaignAutoTestLab_UxmlContainsNineFixedResultRows()
        {
            VisualTreeAsset layout = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(LayoutPath);
            Assert.IsNotNull(layout);
            VisualElement root = layout.CloneTree();

            for (int index = 0; index < 9; index += 1)
            {
                Assert.IsNotNull(root.Q<VisualElement>($"result-row-{index}"));
                Assert.IsNotNull(root.Q<Label>($"result-{index}-result"));
                Assert.IsNotNull(root.Q<Label>($"result-{index}-characters"));
                Assert.IsNotNull(root.Q<Label>($"result-{index}-recruitment"));
                Assert.IsNotNull(root.Q<Button>($"result-{index}-detail"));
            }
            Assert.IsNotNull(root.Q<VisualElement>("detail-panel"));
            Assert.IsNotNull(root.Q<Label>("detail-body"));
            Assert.IsNotNull(root.Q<IntegerField>("seed-start-field"));
            Assert.IsNotNull(root.Q<IntegerField>("seed-count-field"));
        }

        // 반복 표본 통계가 평균·중앙값·범위를 정확하게 계산하는지 검증합니다.
        [Test]
        public void CampaignAutoTestLab_BatchStatisticsSummarizeSeeds()
        {
            List<WICampaignAutoTestSample> samples = new List<WICampaignAutoTestSample>();
            foreach (int value in new[] { 1, 3, 8, 10 })
            {
                samples.Add(new WICampaignAutoTestSample
                {
                    Seed = value,
                    Metrics = new WIAutoCampaignMetrics
                    {
                        FinalPlayerCastleCount = value,
                        CharacterDeaths = value
                    }
                });
            }

            WICampaignAutoBatchStatistics statistics = WICampaignAutoBatchStatistics.Create(samples);
            Assert.AreEqual(5.5f, statistics.FinalCastles.Average);
            Assert.AreEqual(5.5f, statistics.FinalCastles.Median);
            Assert.AreEqual(1, statistics.Deaths.Minimum);
            Assert.AreEqual(10, statistics.Deaths.Maximum);
            Assert.AreEqual("5.5[1~10]", statistics.Deaths.ToCompactString());
        }

        // 상세 설명에 지표의 풀어쓴 의미와 장기 정체 진단이 포함되는지 검증합니다.
        [Test]
        public void CampaignAutoTestLab_DetailExplainsMetricsAndWarnings()
        {
            WICampaignAutoTestResult result = new WICampaignAutoTestResult
            {
                Difficulty = WICampaignDifficulty.Standard,
                Policy = WIAutoPlayerPolicy.Balanced,
                Metrics = new WIAutoCampaignMetrics
                {
                    MonthsSimulated = 240,
                    CampaignResult = WICampaignResult.Ongoing,
                    FinalPlayerCastleCount = 3,
                    FinalEmployedCharacters = 14,
                    FinalHeroCharacters = 4,
                    FinalCommonCharacters = 10,
                    RestActions = 3,
                    RecoveryMonths = 2,
                    ArmyReinforcements = 6,
                    GoalsAbandoned = 1,
                    JointAttackBattles = 2,
                    ThreatResponseMonths = 3,
                    DefensiveReinforcementMarches = 4,
                    PrisonerExchanges = 1,
                    PrisonerRansoms = 2,
                    PrisonersRecovered = 3,
                    PrisonerRansomGoldSpent = 300,
                    PrisonerRansomManaSpent = 100,
                    LongestNoMarchMonths = 20,
                    MaximumHeroFatigue = 92
                }
            };

            string detail = WICampaignAutoTestLabWindow.BuildDetailText(result);
            StringAssert.Contains("실제 신규 영입 성공 0명", detail);
            StringAssert.Contains("원정이 없어", detail);
            StringAssert.Contains("240개월에도 캠페인이 끝나지 않아", detail);
            StringAssert.Contains("개인 휴식 3회", detail);
            StringAssert.Contains("손실 보충 6명", detail);
            StringAssert.Contains("반복 패배 목표 포기 1회", detail);
            StringAssert.Contains("공동 공격 2회", detail);
            StringAssert.Contains("위협 대응 3개월", detail);
            StringAssert.Contains("방어 증원 이동 4회", detail);
            StringAssert.Contains("맞교환 1회 · 몸값 2회 · 귀환 3명 · 금화 300 · 마나 100 지출", detail);
            StringAssert.Contains("18개월 이상 원정 공백", detail);
            StringAssert.Contains("피로가 90 이상", detail);
        }

        // CSV와 Markdown 내보내기 문자열에 주요 캠페인 지표가 포함되는지 검증합니다.
        [Test]
        public void CampaignAutoTestLab_ExportsContainMetrics()
        {
            List<WICampaignAutoTestResult> source = new List<WICampaignAutoTestResult>
            {
                new WICampaignAutoTestResult
                {
                    Difficulty = WICampaignDifficulty.Standard,
                    Policy = WIAutoPlayerPolicy.Balanced,
                    Metrics = new WIAutoCampaignMetrics
                    {
                        MonthsSimulated = 24,
                        CampaignResult = WICampaignResult.Victory,
                        FinalPlayerCastleCount = 60,
                        PlayerVictories = 8,
                        PlayerDefeats = 2,
                        CommonCharactersReturned = 3,
                        CharactersDiscovered = 4,
                        CharactersRecruited = 2,
                        FinalEmployedCharacters = 12,
                        FinalHeroCharacters = 5,
                        FinalCommonCharacters = 7,
                        FinalWanderingCharacters = 30,
                        RestActions = 4,
                        RecoveryMonths = 2,
                        ArmyReinforcements = 3,
                        GoalsAbandoned = 1,
                        JointAttackBattles = 2,
                        ThreatResponseMonths = 3,
                        DefensiveReinforcementMarches = 4,
                        PrisonerExchanges = 1,
                        PrisonerRansoms = 2,
                        PrisonersRecovered = 3,
                        PrisonerRansomGoldSpent = 300,
                        PrisonerRansomManaSpent = 100,
                        LongestNoMarchMonths = 6,
                        MaximumHeroFatigue = 75,
                        MovingArmyMonths = 18,
                        LongestMovingArmyMonths = 4,
                        AwaitingBattleArmyMonths = 7,
                        LongestAwaitingBattleArmyMonths = 2,
                        ReorganizingArmyMonths = 5,
                        LongestReorganizingArmyMonths = 1
                    }
                }
            };

            string csv = WICampaignAutoTestLabWindow.BuildCsv(source);
            string markdown = WICampaignAutoTestLabWindow.BuildMarkdown(source);

            StringAssert.Contains("표준,균형형,24,승리,60", csv);
            StringAssert.Contains("| 표준 | 균형형 | 24 | 승리 | 60 | 8/2 |", markdown);
            StringAssert.Contains("일반 재야 복귀,인재 발견,신규 영입 성공", csv);
            StringAssert.Contains("| 12 (5/7) | 30 | 3 | 4 | 2 |", markdown);
            StringAssert.Contains("개인 휴식,패전 회복 개월", csv);
            StringAssert.Contains("포로 맞교환,포로 몸값,포로 귀환,몸값 금화,몸값 마나", csv);
            StringAssert.Contains("| 1 | 2 | 3 | 300/100 | 4 | 2 | 2 | 3 | 4 | 3 | 1 | 6 | 75 |", markdown);
            StringAssert.Contains("이동 부대월,이동 최장 연속,전투 대기 부대월", csv);
            StringAssert.Contains("| 18/4 | 7/2 | 5/1 |", markdown);
        }

        // 자동 플레이 지표가 전 세계 고용 순증이 아니라 플레이어의 실제 영입 성공만 보고하는지 검증합니다.
        [Test]
        public void AutoPlayer_ReportsOnlyPlayerRecruitmentSuccesses()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(
                database, WICampaignDifficulty.Standard, WICampaignVariant.AresMain);

            WIAutoCampaignMetrics metrics = WICampaignAutoPlayer.Run(
                database, state, WIAutoPlayerPolicy.Balanced, 24, false);

            Assert.AreEqual(state.PlayerRecruitmentSuccessCount, metrics.CharactersRecruited);
        }
    }
}

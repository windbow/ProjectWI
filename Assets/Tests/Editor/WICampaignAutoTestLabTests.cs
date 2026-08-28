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
                    FinalCommonCharacters = 10
                }
            };

            string detail = WICampaignAutoTestLabWindow.BuildDetailText(result);
            StringAssert.Contains("실제 신규 영입 성공 0명", detail);
            StringAssert.Contains("원정이 없어", detail);
            StringAssert.Contains("240개월에도 캠페인이 끝나지 않아", detail);
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
                        FinalWanderingCharacters = 30
                    }
                }
            };

            string csv = WICampaignAutoTestLabWindow.BuildCsv(source);
            string markdown = WICampaignAutoTestLabWindow.BuildMarkdown(source);

            StringAssert.Contains("표준,균형형,24,승리,60", csv);
            StringAssert.Contains("| 표준 | 균형형 | 24 | 승리 | 60 | 8/2 |", markdown);
            StringAssert.Contains("일반 재야 복귀,인재 발견,신규 영입 성공", csv);
            StringAssert.Contains("| 12 (5/7) | 30 | 3 | 4 | 2 |", markdown);
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ProjectWI.Administration;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectWI.Editor
{
    public class WICampaignAutoTestLabWindow : EditorWindow
    {
        private const string DatabasePath = "Assets/Data/ScriptableObject/Administration/WI_AdministrationDatabase.asset";
        private const string LayoutPath = "Assets/Editor/UI/WICampaignAutoTestLab.uxml";
        private const string StylePath = "Assets/Editor/UI/WICampaignAutoTestLab.uss";
        private const int ResultRowCount = 9;

        private readonly List<WICampaignAutoTestResult> results = new List<WICampaignAutoTestResult>();
        private ObjectField databaseField;
        private DropdownField difficultyField;
        private DropdownField policyField;
        private IntegerField maximumMonthsField;
        private IntegerField seedStartField;
        private IntegerField seedCountField;
        private Toggle runMatrixToggle;
        private Button runButton;
        private Button exportCsvButton;
        private Button exportMarkdownButton;
        private Label statusLabel;
        private VisualElement detailPanel;
        private Label detailTitle;
        private Label detailBody;

        [MenuItem("ProjectWI/Tools/Campaign Auto Test Lab")]
        public static void OpenWindow()
        {
            WICampaignAutoTestLabWindow window = GetWindow<WICampaignAutoTestLabWindow>("Campaign Auto Test Lab");
            window.minSize = new Vector2(1180f, 620f);
        }

        // UXML에 고정 배치된 에디터 도구 요소를 찾아 데이터와 동작을 연결합니다.
        public void CreateGUI()
        {
            VisualTreeAsset layout = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(LayoutPath);
            StyleSheet style = AssetDatabase.LoadAssetAtPath<StyleSheet>(StylePath);
            if (layout == null)
            {
                rootVisualElement.Add(new Label($"자동 테스트 랩 UXML을 찾을 수 없습니다: {LayoutPath}"));
                return;
            }

            layout.CloneTree(rootVisualElement);
            if (style != null)
            {
                rootVisualElement.styleSheets.Add(style);
            }

            databaseField = rootVisualElement.Q<ObjectField>("database-field");
            difficultyField = rootVisualElement.Q<DropdownField>("difficulty-field");
            policyField = rootVisualElement.Q<DropdownField>("policy-field");
            maximumMonthsField = rootVisualElement.Q<IntegerField>("maximum-months-field");
            seedStartField = rootVisualElement.Q<IntegerField>("seed-start-field");
            seedCountField = rootVisualElement.Q<IntegerField>("seed-count-field");
            runMatrixToggle = rootVisualElement.Q<Toggle>("run-matrix-toggle");
            runButton = rootVisualElement.Q<Button>("run-button");
            exportCsvButton = rootVisualElement.Q<Button>("export-csv-button");
            exportMarkdownButton = rootVisualElement.Q<Button>("export-markdown-button");
            statusLabel = rootVisualElement.Q<Label>("status-label");
            detailPanel = rootVisualElement.Q<VisualElement>("detail-panel");
            detailTitle = rootVisualElement.Q<Label>("detail-title");
            detailBody = rootVisualElement.Q<Label>("detail-body");

            databaseField.objectType = typeof(WIAdministrationDatabaseSO);
            databaseField.value = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            difficultyField.choices = new List<string> { "여유", "표준", "도전" };
            difficultyField.index = 1;
            policyField.choices = new List<string> { "내정형", "균형형", "공세형" };
            policyField.index = 1;
            maximumMonthsField.value = 240;
            seedStartField.value = 1;
            seedCountField.value = 5;
            runMatrixToggle.value = true;

            runButton.clicked += RunCampaignTests;
            exportCsvButton.clicked += ExportCsv;
            exportMarkdownButton.clicked += ExportMarkdown;
            rootVisualElement.Q<Button>("detail-close-button").clicked += HideResultDetail;
            for (int index = 0; index < ResultRowCount; index += 1)
            {
                int capturedIndex = index;
                Button detailButton = rootVisualElement.Q<Button>($"result-{index}-detail");
                if (detailButton != null)
                {
                    detailButton.clicked += () => ShowResultDetail(capturedIndex);
                }
            }
            RefreshResultRows();
        }

        // 선택한 단일 조합 또는 정책·난이도 9개 조합으로 캠페인을 끝까지 자동 실행합니다.
        private void RunCampaignTests()
        {
            WIAdministrationDatabaseSO database = databaseField.value as WIAdministrationDatabaseSO;
            if (database == null)
            {
                SetStatus("내정 데이터베이스를 선택해주세요.", true);
                return;
            }

            int maximumMonths = Mathf.Clamp(maximumMonthsField.value, 1, 1200);
            int seedStart = Mathf.Max(0, seedStartField.value);
            int seedCount = Mathf.Clamp(seedCountField.value, 1, 20);
            maximumMonthsField.SetValueWithoutNotify(maximumMonths);
            seedStartField.SetValueWithoutNotify(seedStart);
            seedCountField.SetValueWithoutNotify(seedCount);
            results.Clear();
            HideResultDetail();
            SetControlsEnabled(false);
            try
            {
                if (runMatrixToggle.value)
                {
                    foreach (WICampaignDifficulty difficulty in Enum.GetValues(typeof(WICampaignDifficulty)))
                    {
                        foreach (WIAutoPlayerPolicy policy in Enum.GetValues(typeof(WIAutoPlayerPolicy)))
                        {
                            results.Add(RunRepeated(database, difficulty, policy, maximumMonths, seedStart, seedCount));
                        }
                    }
                }
                else
                {
                    results.Add(RunRepeated(database, GetSelectedDifficulty(), GetSelectedPolicy(), maximumMonths, seedStart, seedCount));
                }

                RefreshResultRows();
                int completed = results.Sum(item => item.Samples.Count > 0
                    ? item.Samples.Count(sample => sample.Metrics.CampaignResult != WICampaignResult.Ongoing)
                    : (item.Metrics.CampaignResult != WICampaignResult.Ongoing ? 1 : 0));
                int returned = results.Sum(item => item.Samples.Count > 0
                    ? item.Samples.Sum(sample => sample.Metrics.CommonCharactersReturned)
                    : item.Metrics.CommonCharactersReturned);
                int recruited = results.Sum(item => item.Samples.Count > 0
                    ? item.Samples.Sum(sample => sample.Metrics.CharactersRecruited)
                    : item.Metrics.CharactersRecruited);
                int sampleCount = results.Sum(item => item.Samples.Count);
                SetStatus($"실행 완료 · 조합 {results.Count}개 · 표본 {sampleCount}개 · 결말 {completed}개 · " +
                          $"재야 복귀 합계 {returned}명 · 영입 성공 합계 {recruited}명 · 최대 {maximumMonths}개월", false);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                SetStatus($"실행 실패 · {exception.Message}", true);
            }
            finally
            {
                SetControlsEnabled(true);
            }
        }

        // 선택한 결과 행의 전체 지표와 자동 진단을 고정 상세 패널에 표시합니다.
        private void ShowResultDetail(int index)
        {
            if (index < 0 || index >= results.Count || detailPanel == null)
            {
                return;
            }

            WICampaignAutoTestResult result = results[index];
            detailTitle.text = $"{GetDifficultyLabel(result.Difficulty)} · {GetPolicyLabel(result.Policy)} · {result.Metrics.MonthsSimulated}개월";
            detailBody.text = BuildDetailText(result);
            detailPanel.style.display = DisplayStyle.Flex;
        }

        // 결과 상세 패널을 닫아 결과 표의 세로 공간을 돌려줍니다.
        private void HideResultDetail()
        {
            if (detailPanel != null)
            {
                detailPanel.style.display = DisplayStyle.None;
            }
        }

        // 한 실행의 수치를 항목별 설명과 주의 진단이 포함된 문장으로 변환합니다.
        internal static string BuildDetailText(WICampaignAutoTestResult result)
        {
            WIAutoCampaignMetrics metrics = result.Metrics;
            StringBuilder builder = new StringBuilder();
            if (result.Statistics != null && result.Samples.Count > 1)
            {
                builder.AppendLine($"반복 표본: {result.Samples.Count}개 · 시드 {result.Samples.Min(item => item.Seed)}~{result.Samples.Max(item => item.Seed)}");
                builder.AppendLine($"최종 성 평균/중앙/범위: {result.Statistics.FinalCastles}");
                builder.AppendLine($"발도르 잔여 성 평균/중앙/범위: {result.Statistics.ValdorCastles}");
                builder.AppendLine($"승리 완료 개월 평균/중앙/범위: {result.Statistics.CompletionMonths}");
                builder.AppendLine($"승리 평균/중앙/범위: {result.Statistics.Victories}");
                builder.AppendLine($"패배 평균/중앙/범위: {result.Statistics.Defeats}");
                builder.AppendLine($"사망 평균/중앙/범위: {result.Statistics.Deaths}");
                builder.AppendLine($"영웅 영구 사망 평균/중앙/범위: {result.Statistics.PermanentHeroDeaths}");
                builder.AppendLine($"포로 평균/중앙/범위: {result.Statistics.Captures}");
                builder.AppendLine($"적 합류 평균/중앙/범위: {result.Statistics.Defections}");
                builder.AppendLine($"교착 해소 선전포고 평균/중앙/범위: {result.Statistics.WarsDeclared}");
                builder.AppendLine();
            }
            builder.AppendLine($"결과: {GetCampaignResultLabel(metrics.CampaignResult)} · 최종 성 {metrics.FinalPlayerCastleCount}개 · 소유권 변화 {metrics.OwnershipChanges}건");
            builder.AppendLine($"시나리오 목표: 발도르 잔여 성 {metrics.FinalValdorCastleCount}개 · 완료 시점 {metrics.MonthsSimulated}개월");
            builder.AppendLine($"전쟁: 전투 {metrics.BattlesResolved}회 · 승리 {metrics.PlayerVictories}회 · 패배 {metrics.PlayerDefeats}회 · 출정 {metrics.MarchesStarted}회");
            builder.AppendLine($"인물: 최종 {metrics.FinalEmployedCharacters}명 · 영웅 {metrics.FinalHeroCharacters}명 · 일반 병사 {metrics.FinalCommonCharacters}명 · 현재 재야 {metrics.FinalWanderingCharacters}명");
            builder.AppendLine($"인재: 발견 {metrics.CharactersDiscovered}명 · 실제 신규 영입 성공 {metrics.CharactersRecruited}명 · 일반 재야 복귀 {metrics.CommonCharactersReturned}명");
            builder.AppendLine($"사망: 총 {metrics.CharacterDeaths}명 · 태생 영웅 영구 사망 {metrics.PermanentHeroDeaths}명 · 일반 사망 {metrics.CommonCharacterDeaths}명");
            builder.AppendLine($"전투 이탈: 포로 {metrics.CharacterCaptures}명 · 적 합류 {metrics.CharacterDefections}명");
            builder.AppendLine($"포로 대응: 맞교환 {metrics.PrisonerExchanges}회 · 몸값 {metrics.PrisonerRansoms}회 · " +
                $"귀환 {metrics.PrisonersRecovered}명 · 금화 {metrics.PrisonerRansomGoldSpent} · 마나 {metrics.PrisonerRansomManaSpent} 지출");
            builder.AppendLine($"선택: 사건·전투 선택 해결 {metrics.DecisionsResolved}건 · 미해결 선택 {metrics.RemainingDecisions}건 · 미해결 전투 {metrics.RemainingPlayerBattles}건");
            builder.AppendLine($"가상 플레이어: 개인 휴식 {metrics.RestActions}회 · 패전 회복 {metrics.RecoveryMonths}개월 · 목표 변경 {metrics.GoalChanges}회");
            builder.AppendLine($"전투단 관리: 공동 공격 {metrics.JointAttackBattles}회 · 손실 보충 {metrics.ArmyReinforcements}명 · 반복 패배 목표 포기 {metrics.GoalsAbandoned}회");
            builder.AppendLine($"수비 대응: 위협 대응 {metrics.ThreatResponseMonths}개월 · 방어 증원 이동 {metrics.DefensiveReinforcementMarches}회");
            builder.AppendLine($"외교 확장: 장기 교착 해소 선전포고 {metrics.WarsDeclared}회");
            builder.AppendLine($"진행 리듬: 최장 무원정 {metrics.LongestNoMarchMonths}개월 · 최고 영웅 피로 {metrics.MaximumHeroFatigue}");
            builder.AppendLine($"전투단 상태 체류: 이동 {metrics.MovingArmyMonths}부대·월(최장 {metrics.LongestMovingArmyMonths}) · " +
                $"전투 대기 {metrics.AwaitingBattleArmyMonths}부대·월(최장 {metrics.LongestAwaitingBattleArmyMonths}) · " +
                $"재편 {metrics.ReorganizingArmyMonths}부대·월(최장 {metrics.LongestReorganizingArmyMonths})");
            builder.AppendLine($"종료 전투단: 전체 {metrics.FinalPlayerArmyCount}개 · 작전 가능 {metrics.FinalOperationalArmyCount}개 · " +
                $"구성원 {metrics.FinalArmyMemberCount}명 · 총 전력 {metrics.FinalPlayerArmyPower} · 평균 피로 {metrics.FinalAverageArmyFatigue}");
            builder.AppendLine($"종료 전투단 상태: 이동 {metrics.FinalMovingArmyCount}개 · 전투 대기 {metrics.FinalAwaitingBattleArmyCount}개 · " +
                $"재편 {metrics.FinalReorganizingArmyCount}개");
            builder.AppendLine($"종료 보충·접경: 유휴 일반 {metrics.FinalIdleCommonCharacters}명 · 발도르 접경 {metrics.FinalValdorBorderCastleCount}개 · " +
                $"즉시 공격 가능 {metrics.FinalAttackableValdorBorderCount}개 · 현재 무원정 {metrics.FinalCurrentNoMarchMonths}개월");
            builder.AppendLine($"종료 전략 목표: {metrics.FinalStrategicTargetCastleId} · 집결 전력 {metrics.FinalStrategicAssemblyPower} / 필요 {metrics.FinalStrategicRequiredPower}");
            builder.AppendLine($"종료 외교 자원: 영향력 {metrics.FinalInfluence} · 발도르 교전 {metrics.FinalAtWarWithValdor}");
            if (metrics.DecisionReasonCounts.Count > 0)
            {
                builder.AppendLine("주요 판단 이유: " + string.Join(" · ", metrics.DecisionReasonCounts
                    .OrderByDescending(item => item.Value)
                    .Take(5)
                    .Select(item => $"{item.Key} {item.Value}회")));
            }
            if (metrics.DecisionTraces.Count > 0)
            {
                builder.AppendLine("최근 판단:");
                foreach (WIAutoDecisionTrace trace in metrics.DecisionTraces.TakeLast(5))
                {
                    builder.AppendLine($"- {trace.Month}개월 · {trace.Phase} · {trace.ReasonCode} · {trace.Description}");
                }
            }

            if (metrics.MarchesStarted == 0)
            {
                builder.AppendLine("진단: 원정이 없어 확장 경로 또는 자동 군사 판단을 확인해야 합니다.");
            }
            if (metrics.CharactersRecruited == 0)
            {
                builder.AppendLine("진단: 실제 신규 영입 성공이 없습니다. 영웅의 인재 활동 여유와 영입 조건을 확인해야 합니다.");
            }
            if (metrics.PlayerDefeats > metrics.PlayerVictories)
            {
                builder.AppendLine("진단: 패배가 승리보다 많아 전력 판단 또는 회복 주기를 확인해야 합니다.");
            }
            if (metrics.LongestNoMarchMonths >= 18)
            {
                builder.AppendLine("진단: 18개월 이상 원정 공백이 있어 준비·회복 또는 목표 선정 정체 이유를 확인해야 합니다.");
            }
            if (metrics.MaximumHeroFatigue >= 90)
            {
                builder.AppendLine("진단: 영웅 피로가 90 이상 도달해 활동 교대와 휴식 시점 검토가 필요합니다.");
            }
            if (metrics.CampaignResult == WICampaignResult.Ongoing && metrics.MonthsSimulated >= 240)
            {
                builder.AppendLine("진단: 240개월에도 캠페인이 끝나지 않아 장기 목표 달성 속도 검토가 필요합니다.");
            }
            return builder.ToString().TrimEnd();
        }

        // 새 캠페인을 만들고 결말 또는 최대 개월까지 한 조합을 실행합니다.
        private static WICampaignAutoTestResult RunRepeated(
            WIAdministrationDatabaseSO database,
            WICampaignDifficulty difficulty,
            WIAutoPlayerPolicy policy,
            int maximumMonths,
            int seedStart,
            int seedCount)
        {
            WICampaignAutoTestResult result = new WICampaignAutoTestResult
            {
                Difficulty = difficulty,
                Policy = policy
            };
            for (int offset = 0; offset < seedCount; offset += 1)
            {
                int seed = seedStart + offset;
                WIAdministrationState state = WIAdministrationState.Create(database, difficulty);
                state.SimulationSeed = seed;
                result.Samples.Add(new WICampaignAutoTestSample
                {
                    Seed = seed,
                    Metrics = WICampaignAutoPlayer.Run(database, state, policy, maximumMonths, true)
                });
            }
            result.Metrics = result.Samples[0].Metrics;
            result.Statistics = WICampaignAutoBatchStatistics.Create(result.Samples);
            return result;
        }

        // 고정된 아홉 결과 행에 최근 실행 결과를 바인딩합니다.
        private void RefreshResultRows()
        {
            for (int index = 0; index < ResultRowCount; index += 1)
            {
                VisualElement row = rootVisualElement.Q<VisualElement>($"result-row-{index}");
                if (row == null)
                {
                    continue;
                }

                bool hasResult = index < results.Count;
                row.style.display = hasResult ? DisplayStyle.Flex : DisplayStyle.None;
                if (hasResult == false)
                {
                    continue;
                }

                WICampaignAutoTestResult result = results[index];
                WIAutoCampaignMetrics metrics = result.Metrics;
                SetRowText(index, "difficulty", GetDifficultyLabel(result.Difficulty));
                SetRowText(index, "policy", GetPolicyLabel(result.Policy));
                SetRowText(index, "months", metrics.MonthsSimulated.ToString());
                SetRowText(index, "result", GetCampaignResultLabel(metrics.CampaignResult));
                SetRowText(index, "castles", metrics.FinalPlayerCastleCount.ToString());
                SetRowText(index, "battles", $"{metrics.BattlesResolved} ({metrics.PlayerVictories}/{metrics.PlayerDefeats})");
                SetRowText(index, "marches", metrics.MarchesStarted.ToString());
                SetRowText(index, "changes", metrics.OwnershipChanges.ToString());
                SetRowText(index, "decisions", metrics.DecisionsResolved.ToString());
                SetRowText(index, "characters",
                    $"{metrics.FinalEmployedCharacters} ({metrics.FinalHeroCharacters}/{metrics.FinalCommonCharacters})");
                SetRowText(index, "recruitment",
                    $"{metrics.CharactersDiscovered}/{metrics.CharactersRecruited}/" +
                    $"{metrics.CommonCharactersReturned}/{metrics.CharacterDeaths}");
                if (result.Samples.Count > 1)
                {
                    SetRowText(index, "castles", result.Statistics.FinalCastles.ToCompactString());
                    SetRowText(index, "battles", $"승 {result.Statistics.Victories.ToCompactString()} / 패 {result.Statistics.Defeats.ToCompactString()}");
                    SetRowText(index, "recruitment", $"사망 {result.Statistics.Deaths.ToCompactString()}");
                }
                row.EnableInClassList("result-victory", metrics.CampaignResult == WICampaignResult.Victory);
                row.EnableInClassList("result-defeat", metrics.CampaignResult == WICampaignResult.Defeat);
            }

            bool canExport = results.Count > 0;
            exportCsvButton?.SetEnabled(canExport);
            exportMarkdownButton?.SetEnabled(canExport);
        }

        // 결과 행의 지정 열에 문자열을 기록합니다.
        private void SetRowText(int rowIndex, string column, string value)
        {
            Label label = rootVisualElement.Q<Label>($"result-{rowIndex}-{column}");
            if (label != null)
            {
                label.text = value;
            }
        }

        // 현재 결과를 UTF-8 CSV 파일로 내보냅니다.
        private void ExportCsv()
        {
            string path = EditorUtility.SaveFilePanel("자동 캠페인 결과 CSV 저장", Application.dataPath, "CampaignAutoTest", "csv");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            File.WriteAllText(path, BuildCsv(results), new UTF8Encoding(false));
            SetStatus($"CSV 저장 완료 · {path}", false);
        }

        // 현재 결과를 UTF-8 Markdown 보고서로 내보냅니다.
        private void ExportMarkdown()
        {
            string path = EditorUtility.SaveFilePanel("자동 캠페인 결과 Markdown 저장", Application.dataPath, "CampaignAutoTest", "md");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            File.WriteAllText(path, BuildMarkdown(results), new UTF8Encoding(false));
            SetStatus($"Markdown 저장 완료 · {path}", false);
        }

        // 자동 테스트 결과 목록을 CSV 문자열로 변환합니다.
        internal static string BuildCsv(IReadOnlyList<WICampaignAutoTestResult> source)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("난이도,정책,진행 개월,결과,최종 성,전투,승리,패배,출정,소유권 변화,선택 해결,최종 고용 인물,최종 영웅,최종 일반,현재 재야,일반 재야 복귀,인재 발견,신규 영입 성공,총 사망,태생 영웅 영구 사망,일반 사망,포로 발생,적 합류,포로 맞교환,포로 몸값,포로 귀환,몸값 금화,몸값 마나,개인 휴식,패전 회복 개월,목표 변경,공동 공격,위협 대응 개월,방어 증원 이동,전투단 보충,목표 포기,최장 무원정 개월,최고 영웅 피로,이동 부대월,이동 최장 연속,전투 대기 부대월,전투 대기 최장 연속,재편 부대월,재편 최장 연속");
            foreach (WICampaignAutoTestResult result in source)
            {
                WIAutoCampaignMetrics metrics = result.Metrics;
                builder.AppendLine(string.Join(",", new[]
                {
                    GetDifficultyLabel(result.Difficulty), GetPolicyLabel(result.Policy), metrics.MonthsSimulated.ToString(),
                    GetCampaignResultLabel(metrics.CampaignResult), metrics.FinalPlayerCastleCount.ToString(),
                    metrics.BattlesResolved.ToString(), metrics.PlayerVictories.ToString(), metrics.PlayerDefeats.ToString(),
                    metrics.MarchesStarted.ToString(), metrics.OwnershipChanges.ToString(), metrics.DecisionsResolved.ToString(),
                    metrics.FinalEmployedCharacters.ToString(), metrics.FinalHeroCharacters.ToString(),
                    metrics.FinalCommonCharacters.ToString(), metrics.FinalWanderingCharacters.ToString(),
                    metrics.CommonCharactersReturned.ToString(), metrics.CharactersDiscovered.ToString(),
                    metrics.CharactersRecruited.ToString(), metrics.CharacterDeaths.ToString(),
                    metrics.PermanentHeroDeaths.ToString(), metrics.CommonCharacterDeaths.ToString(),
                    metrics.CharacterCaptures.ToString(), metrics.CharacterDefections.ToString(),
                    metrics.PrisonerExchanges.ToString(), metrics.PrisonerRansoms.ToString(),
                    metrics.PrisonersRecovered.ToString(), metrics.PrisonerRansomGoldSpent.ToString(),
                    metrics.PrisonerRansomManaSpent.ToString(),
                    metrics.RestActions.ToString(), metrics.RecoveryMonths.ToString(), metrics.GoalChanges.ToString(),
                    metrics.JointAttackBattles.ToString(),
                    metrics.ThreatResponseMonths.ToString(), metrics.DefensiveReinforcementMarches.ToString(),
                    metrics.ArmyReinforcements.ToString(), metrics.GoalsAbandoned.ToString(),
                    metrics.LongestNoMarchMonths.ToString(), metrics.MaximumHeroFatigue.ToString(),
                    metrics.MovingArmyMonths.ToString(), metrics.LongestMovingArmyMonths.ToString(),
                    metrics.AwaitingBattleArmyMonths.ToString(), metrics.LongestAwaitingBattleArmyMonths.ToString(),
                    metrics.ReorganizingArmyMonths.ToString(), metrics.LongestReorganizingArmyMonths.ToString()
                }));
            }
            return builder.ToString();
        }

        // 자동 테스트 결과 목록을 간단한 Markdown 보고서로 변환합니다.
        internal static string BuildMarkdown(IReadOnlyList<WICampaignAutoTestResult> source)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("# ProjectWI 캠페인 자동 테스트 결과");
            builder.AppendLine();
            builder.AppendLine($"생성 시각: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            builder.AppendLine();
            builder.AppendLine("| 난이도 | 정책 | 개월 | 결과 | 최종 성 | 승/패 | 출정 | 소유권 변화 | 선택 해결 | 최종 인물(영웅/일반) | 현재 재야 | 재야 복귀 | 발견 | 신규 영입 성공 | 사망(영웅/일반) | 포로 | 적 합류 | 교환 | 몸값 | 귀환 | 몸값 금화/마나 | 휴식 | 회복 개월 | 공동 공격 | 위협 대응 | 방어 증원 | 병력 보충 | 목표 포기 | 최장 무원정 | 최고 피로 | 이동 부대월/최장 | 전투 대기 부대월/최장 | 재편 부대월/최장 |");
            builder.AppendLine("|---|---|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|");
            foreach (WICampaignAutoTestResult result in source)
            {
                WIAutoCampaignMetrics metrics = result.Metrics;
                builder.AppendLine($"| {GetDifficultyLabel(result.Difficulty)} | {GetPolicyLabel(result.Policy)} | " +
                                   $"{metrics.MonthsSimulated} | {GetCampaignResultLabel(metrics.CampaignResult)} | " +
                                   $"{metrics.FinalPlayerCastleCount} | {metrics.PlayerVictories}/{metrics.PlayerDefeats} | " +
                                   $"{metrics.MarchesStarted} | {metrics.OwnershipChanges} | {metrics.DecisionsResolved} | " +
                                   $"{metrics.FinalEmployedCharacters} ({metrics.FinalHeroCharacters}/{metrics.FinalCommonCharacters}) | " +
                                   $"{metrics.FinalWanderingCharacters} | " +
                                   $"{metrics.CommonCharactersReturned} | {metrics.CharactersDiscovered} | " +
                                   $"{metrics.CharactersRecruited} | {metrics.CharacterDeaths} " +
                                   $"({metrics.PermanentHeroDeaths}/{metrics.CommonCharacterDeaths}) | " +
                                   $"{metrics.CharacterCaptures} | {metrics.CharacterDefections} | " +
                                   $"{metrics.PrisonerExchanges} | {metrics.PrisonerRansoms} | " +
                                   $"{metrics.PrisonersRecovered} | {metrics.PrisonerRansomGoldSpent}/{metrics.PrisonerRansomManaSpent} | " +
                                   $"{metrics.RestActions} | {metrics.RecoveryMonths} | {metrics.JointAttackBattles} | " +
                                   $"{metrics.ThreatResponseMonths} | {metrics.DefensiveReinforcementMarches} | " +
                                   $"{metrics.ArmyReinforcements} | {metrics.GoalsAbandoned} | " +
                                   $"{metrics.LongestNoMarchMonths} | {metrics.MaximumHeroFatigue} | " +
                                   $"{metrics.MovingArmyMonths}/{metrics.LongestMovingArmyMonths} | " +
                                   $"{metrics.AwaitingBattleArmyMonths}/{metrics.LongestAwaitingBattleArmyMonths} | " +
                                   $"{metrics.ReorganizingArmyMonths}/{metrics.LongestReorganizingArmyMonths} |");
            }
            return builder.ToString();
        }

        // 실행 중 중복 조작을 막도록 주요 입력과 실행 버튼 상태를 변경합니다.
        private void SetControlsEnabled(bool enabled)
        {
            databaseField.SetEnabled(enabled);
            difficultyField.SetEnabled(enabled);
            policyField.SetEnabled(enabled);
            maximumMonthsField.SetEnabled(enabled);
            seedStartField.SetEnabled(enabled);
            seedCountField.SetEnabled(enabled);
            runMatrixToggle.SetEnabled(enabled);
            runButton.SetEnabled(enabled);
        }

        // 상태 안내의 문구와 오류 강조 상태를 변경합니다.
        private void SetStatus(string message, bool error)
        {
            statusLabel.text = message;
            statusLabel.EnableInClassList("status-error", error);
        }

        // 선택된 난이도 인덱스를 열거형으로 변환합니다.
        private WICampaignDifficulty GetSelectedDifficulty()
        {
            return difficultyField.index == 0 ? WICampaignDifficulty.Relaxed :
                difficultyField.index == 2 ? WICampaignDifficulty.Hard : WICampaignDifficulty.Standard;
        }

        // 선택된 정책 인덱스를 열거형으로 변환합니다.
        private WIAutoPlayerPolicy GetSelectedPolicy()
        {
            return (WIAutoPlayerPolicy)Mathf.Clamp(policyField.index, 0, 2);
        }

        // 난이도 열거형을 한글 표시명으로 변환합니다.
        private static string GetDifficultyLabel(WICampaignDifficulty difficulty)
        {
            return difficulty == WICampaignDifficulty.Relaxed ? "여유" :
                difficulty == WICampaignDifficulty.Hard ? "도전" : "표준";
        }

        // 정책 열거형을 한글 표시명으로 변환합니다.
        private static string GetPolicyLabel(WIAutoPlayerPolicy policy)
        {
            return policy == WIAutoPlayerPolicy.Administration ? "내정형" :
                policy == WIAutoPlayerPolicy.Aggressive ? "공세형" : "균형형";
        }

        // 캠페인 결과 열거형을 한글 표시명으로 변환합니다.
        private static string GetCampaignResultLabel(WICampaignResult result)
        {
            return result == WICampaignResult.Victory ? "승리" :
                result == WICampaignResult.Defeat ? "패배" : "진행 중";
        }
    }

    [Serializable]
    public class WICampaignAutoTestResult
    {
        public WICampaignDifficulty Difficulty;
        public WIAutoPlayerPolicy Policy;
        public WIAutoCampaignMetrics Metrics;
        public List<WICampaignAutoTestSample> Samples = new List<WICampaignAutoTestSample>();
        public WICampaignAutoBatchStatistics Statistics;
    }

    [Serializable]
    public class WICampaignAutoTestSample
    {
        public int Seed;
        public WIAutoCampaignMetrics Metrics;
    }

    [Serializable]
    public class WIAutoMetricStatistics
    {
        public float Average;
        public float Median;
        public int Minimum;
        public int Maximum;

        // 표의 좁은 열에 평균과 범위를 간결하게 표시합니다.
        public string ToCompactString()
        {
            return $"{Average:0.0}[{Minimum}~{Maximum}]";
        }

        public override string ToString()
        {
            return $"{Average:0.0} / {Median:0.0} / {Minimum}~{Maximum}";
        }
    }

    [Serializable]
    public class WICampaignAutoBatchStatistics
    {
        public WIAutoMetricStatistics FinalCastles;
        public WIAutoMetricStatistics ValdorCastles;
        public WIAutoMetricStatistics CompletionMonths;
        public WIAutoMetricStatistics Victories;
        public WIAutoMetricStatistics Defeats;
        public WIAutoMetricStatistics Deaths;
        public WIAutoMetricStatistics PermanentHeroDeaths;
        public WIAutoMetricStatistics Captures;
        public WIAutoMetricStatistics Defections;
        public WIAutoMetricStatistics WarsDeclared;

        // 반복 실행 표본에서 밸런스 핵심 지표의 평균·중앙값·범위를 계산합니다.
        public static WICampaignAutoBatchStatistics Create(IReadOnlyList<WICampaignAutoTestSample> samples)
        {
            return new WICampaignAutoBatchStatistics
            {
                FinalCastles = Calculate(samples, item => item.Metrics.FinalPlayerCastleCount),
                ValdorCastles = Calculate(samples, item => item.Metrics.FinalValdorCastleCount),
                CompletionMonths = Calculate(samples, item => item.Metrics.MonthsSimulated),
                Victories = Calculate(samples, item => item.Metrics.PlayerVictories),
                Defeats = Calculate(samples, item => item.Metrics.PlayerDefeats),
                Deaths = Calculate(samples, item => item.Metrics.CharacterDeaths),
                PermanentHeroDeaths = Calculate(samples, item => item.Metrics.PermanentHeroDeaths),
                Captures = Calculate(samples, item => item.Metrics.CharacterCaptures),
                Defections = Calculate(samples, item => item.Metrics.CharacterDefections),
                WarsDeclared = Calculate(samples, item => item.Metrics.WarsDeclared)
            };
        }

        // 정수 지표 표본을 정렬해 평균·중앙값·최솟값·최댓값으로 요약합니다.
        private static WIAutoMetricStatistics Calculate(
            IReadOnlyList<WICampaignAutoTestSample> samples,
            Func<WICampaignAutoTestSample, int> selector)
        {
            List<int> values = samples.Select(selector).OrderBy(value => value).ToList();
            int middle = values.Count / 2;
            float median = values.Count % 2 == 0
                ? (values[middle - 1] + values[middle]) / 2f
                : values[middle];
            return new WIAutoMetricStatistics
            {
                Average = (float)values.Average(),
                Median = median,
                Minimum = values[0],
                Maximum = values[values.Count - 1]
            };
        }
    }
}

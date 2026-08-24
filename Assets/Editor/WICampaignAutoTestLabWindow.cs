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
        private Toggle runMatrixToggle;
        private Button runButton;
        private Button exportCsvButton;
        private Button exportMarkdownButton;
        private Label statusLabel;

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
            runMatrixToggle = rootVisualElement.Q<Toggle>("run-matrix-toggle");
            runButton = rootVisualElement.Q<Button>("run-button");
            exportCsvButton = rootVisualElement.Q<Button>("export-csv-button");
            exportMarkdownButton = rootVisualElement.Q<Button>("export-markdown-button");
            statusLabel = rootVisualElement.Q<Label>("status-label");

            databaseField.objectType = typeof(WIAdministrationDatabaseSO);
            databaseField.value = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            difficultyField.choices = new List<string> { "여유", "표준", "도전" };
            difficultyField.index = 1;
            policyField.choices = new List<string> { "내정형", "균형형", "공세형" };
            policyField.index = 1;
            maximumMonthsField.value = 240;
            runMatrixToggle.value = true;

            runButton.clicked += RunCampaignTests;
            exportCsvButton.clicked += ExportCsv;
            exportMarkdownButton.clicked += ExportMarkdown;
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
            maximumMonthsField.SetValueWithoutNotify(maximumMonths);
            results.Clear();
            SetControlsEnabled(false);
            try
            {
                if (runMatrixToggle.value)
                {
                    foreach (WICampaignDifficulty difficulty in Enum.GetValues(typeof(WICampaignDifficulty)))
                    {
                        foreach (WIAutoPlayerPolicy policy in Enum.GetValues(typeof(WIAutoPlayerPolicy)))
                        {
                            results.Add(RunSingle(database, difficulty, policy, maximumMonths));
                        }
                    }
                }
                else
                {
                    results.Add(RunSingle(database, GetSelectedDifficulty(), GetSelectedPolicy(), maximumMonths));
                }

                RefreshResultRows();
                int completed = results.Count(item => item.Metrics.CampaignResult != WICampaignResult.Ongoing);
                int returned = results.Sum(item => item.Metrics.CommonCharactersReturned);
                int recruited = results.Sum(item => item.Metrics.CharactersRecruited);
                SetStatus($"실행 완료 · {results.Count}개 조합 · 결말 {completed}개 · " +
                          $"재야 복귀 {returned}명 · 신규 영입 {recruited}명 · 최대 {maximumMonths}개월", false);
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

        // 새 캠페인을 만들고 결말 또는 최대 개월까지 한 조합을 실행합니다.
        private static WICampaignAutoTestResult RunSingle(
            WIAdministrationDatabaseSO database,
            WICampaignDifficulty difficulty,
            WIAutoPlayerPolicy policy,
            int maximumMonths)
        {
            WIAdministrationState state = WIAdministrationState.Create(database, difficulty);
            WIAutoCampaignMetrics metrics = WICampaignAutoPlayer.Run(database, state, policy, maximumMonths, true);
            return new WICampaignAutoTestResult
            {
                Difficulty = difficulty,
                Policy = policy,
                Metrics = metrics
            };
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
                    $"{metrics.CharactersDiscovered}/{metrics.CharactersRecruited}/{metrics.CommonCharactersReturned}");
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
            builder.AppendLine("난이도,정책,진행 개월,결과,최종 성,전투,승리,패배,출정,소유권 변화,선택 해결,최종 고용 인물,최종 영웅,최종 일반,현재 재야,일반 재야 복귀,인재 발견,신규 영입");
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
                    metrics.CharactersRecruited.ToString()
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
            builder.AppendLine("| 난이도 | 정책 | 개월 | 결과 | 최종 성 | 승/패 | 출정 | 소유권 변화 | 선택 해결 | 최종 인물(영웅/일반) | 현재 재야 | 재야 복귀 | 발견 | 영입 |");
            builder.AppendLine("|---|---|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|");
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
                                   $"{metrics.CharactersRecruited} |");
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
    }
}

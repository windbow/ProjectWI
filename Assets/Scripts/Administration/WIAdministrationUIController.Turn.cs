using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using ProjectWI.Systems;

namespace ProjectWI.Administration
{
    public partial class WIAdministrationUIController
    {
        // 턴 연산 오버레이를 표시한 뒤 결과 요약을 엽니다.
        private void BeginTurn()
        {
            if (WIAdministrationTurnSystem.HasUnresolvedPlayerBattles(state))
            {
                UGUIMonthlyReportRequested?.Invoke();
                return;
            }
            StartCoroutine(ExecuteUGUITurnRoutine());
        }

        // UGUI 연산 안내 뒤 기존 턴 계산을 실행하고 월간 보고 UGUI를 엽니다.
        private IEnumerator ExecuteUGUITurnRoutine()
        {
            ShowUGUITurnProcessing();
            yield return null;
            WIAdministrationTurnSystem.ExecuteTurn(database, state);
            WICampaignRuntimeService.Instance?.AutoSave();
            RefreshAll();
            ShowUGUITurnReport();
        }

        // 한 프레임 동안 연산 안내를 노출하고 턴 결과를 UI에 반영합니다.
        private IEnumerator ExecuteTurnRoutine()
        {
            VisualElement panel = CreateModal(database.GetText("UI_TURN_PROCESSING"));
            panel.Add(new Label($"TURN {state.Turn}"));
            yield return null;
            WITurnSummary summary = WIAdministrationTurnSystem.ExecuteTurn(database, state);
            WICampaignRuntimeService.Instance?.AutoSave();
            CloseModal();
            RefreshAll();
            ShowTurnSummary(summary);
        }

        // 턴 결과의 자원과 소식을 요약 모달로 표시합니다.
        private void ShowTurnSummary(WITurnSummary summary)
        {
            VisualElement panel = CreateModalWithFooter(database.GetText("UI_TURN_SUMMARY"), out VisualElement battleFooter);
            panel.Add(new Label($"Gold +{summary.GoldGained}"));
            panel.Add(new Label($"Gold Spent -{summary.GoldSpent}"));
            panel.Add(new Label($"Mana +{summary.ManaGained}"));
            panel.Add(new Label($"Influence +{summary.InfluenceGained}"));
            foreach (string report in summary.DelegationReports)
            {
                panel.Add(new Label(report));
            }
            if (summary.AIReasonReports != null && summary.AIReasonReports.Count > 0) panel.Add(new Label("AI 진영 판단 근거"));
            foreach (string aiReport in summary.AIReasonReports ?? new List<string>())
            {
                panel.Add(new Label(aiReport));
            }
            foreach (string news in summary.News)
            {
                panel.Add(new Label(news));
            }
            AddPendingBattleActions(battleFooter);
            if (state.CampaignResult != WICampaignResult.Ongoing && state.CampaignResultAcknowledged == false)
            {
                AddCampaignResultContent(panel);
            }
            else
            {
                AddPendingTutorial(panel);
            }
        }

        // 턴 결과에서 플레이어가 참가할 대기 전투를 놓치지 않도록 즉시 진입 버튼을 표시합니다.
        private void AddPendingBattleActions(VisualElement panel)
        {
            List<WIBattleSessionState> pendingBattles = state.BattleSessions.FindAll(item =>
                item.PlayerInvolved && item.Status == WIBattleSessionStatus.Pending);
            panel.style.display = pendingBattles.Count == 0 ? DisplayStyle.None : DisplayStyle.Flex;
            if (pendingBattles.Count == 0) return;

            Label heading = new Label($"전투 발생 · {pendingBattles.Count}건");
            heading.AddToClassList("modal-section-heading");
            panel.Add(heading);
            foreach (WIBattleSessionState session in pendingBattles)
            {
                WICastleDefinition castle = database.GetCastle(session.CastleId);
                Button battleButton = new Button(() =>
                {
                    WICampaignRuntimeService service = WICampaignRuntimeService.Instance;
                    if (service == null || service.StartBattle(session.SessionId) == false)
                    {
                        ShowMessage("전투를 시작할 수 없습니다. 군사 화면에서 전투 세션 상태를 확인하십시오.");
                    }
                });
                battleButton.text = $"전투 시작 · {castle?.DisplayName.Get(database.UseEnglish) ?? session.CastleId} · " +
                                    $"아군 {session.AttackerPowerSnapshot} / 적군 {session.DefenderPowerSnapshot}";
                battleButton.AddToClassList("danger");
                battleButton.tooltip = "실시간 전투 화면으로 이동합니다.";
                panel.Add(battleButton);
            }
        }

        // 저장에서 복원된 미확인 캠페인 결과를 독립 모달로 표시합니다.
        private bool ShowCampaignResult()
        {
            if (state.CampaignResult == WICampaignResult.Ongoing || state.CampaignResultAcknowledged)
            {
                return false;
            }
            WICampaignRuleDefinition rules = database.CampaignRules;
            WICampaignEndingDefinition ending = state.CampaignResult == WICampaignResult.Victory
                ? database.GetCampaignEnding(state.CampaignEnding) : null;
            string title = ending != null
                ? ending.Title.Get(database.UseEnglish)
                : state.CampaignResult == WICampaignResult.Victory
                    ? rules.VictoryTitle.Get(database.UseEnglish)
                    : rules.DefeatTitle.Get(database.UseEnglish);
            VisualElement panel = CreateModal(title);
            AddCampaignResultContent(panel);
            return true;
        }

        // 승리·패배 설명과 계속 보기·시작 화면 복귀 선택지를 구성합니다.
        private void AddCampaignResultContent(VisualElement panel)
        {
            WICampaignRuleDefinition rules = database.CampaignRules;
            WICampaignEndingDefinition ending = state.CampaignResult == WICampaignResult.Victory
                ? database.GetCampaignEnding(state.CampaignEnding) : null;
            string title = ending != null
                ? ending.Title.Get(database.UseEnglish)
                : state.CampaignResult == WICampaignResult.Victory
                    ? rules.VictoryTitle.Get(database.UseEnglish)
                    : rules.DefeatTitle.Get(database.UseEnglish);
            string description = ending != null
                ? ending.Description.Get(database.UseEnglish)
                : state.CampaignResult == WICampaignResult.Victory
                    ? rules.VictoryDescription.Get(database.UseEnglish)
                    : rules.DefeatDescription.Get(database.UseEnglish);
            panel.Add(new Label($"{title} · 판정 턴 {state.CampaignResultTurn}"));
            panel.Add(new Label(description));
            Button continueButton = new Button(() =>
            {
                state.CampaignResultAcknowledged = true;
                WICampaignRuntimeService.Instance?.AutoSave();
                CloseModal();
            });
            continueButton.text = "계속 보기";
            panel.Add(continueButton);
            Button startButton = new Button(() =>
            {
                state.CampaignResultAcknowledged = true;
                WICampaignRuntimeService.Instance?.AutoSave();
                CloseModal();
                campaignStartLayer.style.display = DisplayStyle.Flex;
            });
            startButton.text = "캠페인 시작 화면";
            panel.Add(startButton);
        }

        // 현재 월의 첫해 안내가 남아 있으면 독립 모달로 표시합니다.
        private void ShowCurrentTutorial()
        {
            WITutorialDefinition tutorial = WITutorialSystem.GetPending(database, state);
            if (tutorial == null)
            {
                return;
            }
            VisualElement panel = CreateModal(tutorial.Title.Get(database.UseEnglish));
            AddTutorialContent(panel, tutorial);
        }

        // 턴 결과 아래에 현재 월의 첫해 안내를 이어서 표시합니다.
        private void AddPendingTutorial(VisualElement panel)
        {
            WITutorialDefinition tutorial = WITutorialSystem.GetPending(database, state);
            if (tutorial == null)
            {
                return;
            }
            Label title = new Label(tutorial.Title.Get(database.UseEnglish));
            title.AddToClassList("modal-title");
            panel.Add(title);
            AddTutorialContent(panel, tutorial);
        }

        // 안내 본문과 확인·전체 건너뛰기 버튼을 구성합니다.
        private void AddTutorialContent(VisualElement panel, WITutorialDefinition tutorial)
        {
            panel.Add(new Label(tutorial.Description.Get(database.UseEnglish)));
            Button confirm = new Button(() =>
            {
                WITutorialSystem.Complete(state, tutorial.Id);
                WICampaignRuntimeService.Instance?.AutoSave();
                CloseModal();
            });
            confirm.text = "안내 확인";
            panel.Add(confirm);
            Button skip = new Button(() =>
            {
                WITutorialSystem.SkipAll(state);
                WICampaignRuntimeService.Instance?.AutoSave();
                CloseModal();
            });
            skip.text = "첫해 안내 전체 건너뛰기";
            panel.Add(skip);
        }

    }
}

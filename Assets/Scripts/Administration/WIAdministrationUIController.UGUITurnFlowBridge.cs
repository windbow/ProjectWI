using System;
using ProjectWI.Systems;

namespace ProjectWI.Administration
{
    public partial class WIAdministrationUIController
    {
        public event Action UGUITurnFollowupRequested;
        public event Action UGUITurnFollowupHideRequested;

        private WIAdministrationTurnFollowupMode uguiTurnFollowupMode;
        private bool uguiTurnFollowupPending;
        private bool uguiTutorialAfterObjective;
        private string uguiMessageTitle;
        private string uguiMessageDescription;

        // QA에서 현재 캠페인의 다음 턴 UGUI 흐름을 실행합니다.
        public void AdvanceTurnUGUIForQA()
        {
            BeginTurn();
        }

        // QA에서 공통 오류·안내 UGUI를 예시 문장으로 표시합니다.
        public void OpenMessageUGUIForQA()
        {
            OpenGlobalPreviewForQA();
            ShowUGUIMessage("안내", "명령을 실행할 수 없습니다. 조건과 현재 자원을 확인하십시오.");
        }

        // 턴 계산 전 연산 안내 UGUI를 표시합니다.
        private void ShowUGUITurnProcessing()
        {
            uguiTurnFollowupMode = WIAdministrationTurnFollowupMode.Processing;
            UGUITurnFollowupRequested?.Invoke();
        }

        // 턴 계산 안내를 닫고 월간 보고 UGUI를 표시합니다.
        private void ShowUGUITurnReport()
        {
            UGUITurnFollowupHideRequested?.Invoke();
            uguiTurnFollowupPending = true;
            UGUIMonthlyReportRequested?.Invoke();
        }

        // 월간 보고를 정상적으로 닫았을 때 캠페인 결과 또는 튜토리얼을 이어서 표시합니다.
        public void ContinueAfterUGUITurnReport()
        {
            if (uguiTurnFollowupPending == false) return;
            uguiTurnFollowupPending = false;
            if (state.CampaignResult != WICampaignResult.Ongoing && state.CampaignResultAcknowledged == false)
            {
                uguiTurnFollowupMode = WIAdministrationTurnFollowupMode.CampaignResult;
                UGUITurnFollowupRequested?.Invoke();
                return;
            }
            if (WITutorialSystem.GetPending(database, state) != null)
            {
                uguiTurnFollowupMode = WIAdministrationTurnFollowupMode.Tutorial;
                UGUITurnFollowupRequested?.Invoke();
            }
        }

        // 새 캠페인 시작 시 목표 UGUI를 열고 닫힌 뒤 튜토리얼을 이어서 표시하도록 예약합니다.
        public void OpenUGUICampaignObjective(bool showTutorialAfterClose)
        {
            uguiTutorialAfterObjective = showTutorialAfterClose;
            if (WICampaignObjectiveSystem.GetCurrent(database, state) == null)
            {
                uguiTutorialAfterObjective = false;
                ShowUGUIMessage("캠페인 목표", "현재 등록된 다음 캠페인 목표가 없습니다.");
                return;
            }
            UGUIObjectiveRequested?.Invoke();
        }

        // 목표 UGUI를 닫으면 예약된 첫해 튜토리얼을 표시합니다.
        public void ContinueAfterUGUIObjective()
        {
            if (uguiTutorialAfterObjective == false) return;
            uguiTutorialAfterObjective = false;
            ShowCurrentUGUICampaignResultOrTutorial();
        }

        // 미확인 캠페인 결과를 우선하고 없으면 현재 월의 튜토리얼을 표시합니다.
        public void ShowCurrentUGUICampaignResultOrTutorial()
        {
            if (state.CampaignResult != WICampaignResult.Ongoing && state.CampaignResultAcknowledged == false)
            {
                uguiTurnFollowupMode = WIAdministrationTurnFollowupMode.CampaignResult;
                UGUITurnFollowupRequested?.Invoke();
                return;
            }
            if (WITutorialSystem.GetPending(database, state) == null) return;
            uguiTurnFollowupMode = WIAdministrationTurnFollowupMode.Tutorial;
            UGUITurnFollowupRequested?.Invoke();
        }

        // 공통 UGUI 프레임으로 캠페인 시작과 불러오기 안내를 표시합니다.
        public void ShowUGUIMessage(string title, string description)
        {
            uguiMessageTitle = title;
            uguiMessageDescription = description;
            uguiTurnFollowupMode = WIAdministrationTurnFollowupMode.Message;
            UGUITurnFollowupRequested?.Invoke();
        }

        // 현재 턴 후속 단계의 제목, 설명과 고정 선택지를 구성합니다.
        public bool TryGetUGUITurnFollowupSnapshot(out WIAdministrationTurnFollowupSnapshot snapshot)
        {
            snapshot = null;
            if (state == null || database == null) return false;
            if (uguiTurnFollowupMode == WIAdministrationTurnFollowupMode.Processing)
            {
                snapshot = new WIAdministrationTurnFollowupSnapshot
                {
                    Mode = uguiTurnFollowupMode,
                    Title = database.GetText("UI_TURN_PROCESSING"),
                    Description = $"TURN {state.Turn}"
                };
                return true;
            }
            if (uguiTurnFollowupMode == WIAdministrationTurnFollowupMode.CampaignResult)
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
                snapshot = new WIAdministrationTurnFollowupSnapshot
                {
                    Mode = uguiTurnFollowupMode,
                    Title = title,
                    Description = $"{title} · 판정 턴 {state.CampaignResultTurn}\n{description}"
                };
                snapshot.Choices.Add("계속 보기\n현재 캠페인 지도를 계속 확인합니다.");
                snapshot.Choices.Add("캠페인 시작 화면\n새 캠페인 또는 자동 저장을 선택합니다.");
                return true;
            }
            if (uguiTurnFollowupMode == WIAdministrationTurnFollowupMode.Message)
            {
                snapshot = new WIAdministrationTurnFollowupSnapshot
                {
                    Mode = uguiTurnFollowupMode,
                    Title = uguiMessageTitle,
                    Description = uguiMessageDescription
                };
                snapshot.Choices.Add("확인\n안내를 닫습니다.");
                return true;
            }
            WITutorialDefinition tutorial = WITutorialSystem.GetPending(database, state);
            if (tutorial == null) return false;
            snapshot = new WIAdministrationTurnFollowupSnapshot
            {
                Mode = uguiTurnFollowupMode,
                Title = tutorial.Title.Get(database.UseEnglish),
                Description = tutorial.Description.Get(database.UseEnglish)
            };
            snapshot.Choices.Add("안내 확인\n이번 안내를 완료합니다.");
            snapshot.Choices.Add("첫해 안내 전체 건너뛰기\n남은 튜토리얼을 표시하지 않습니다.");
            return true;
        }

        // 캠페인 결과 확인 또는 튜토리얼 선택을 저장하고 자동 저장합니다.
        public bool ResolveUGUITurnFollowup(int choiceIndex)
        {
            if (choiceIndex < 0 || choiceIndex > 1 || state == null) return false;
            if (uguiTurnFollowupMode == WIAdministrationTurnFollowupMode.Message) return choiceIndex == 0;
            if (uguiTurnFollowupMode == WIAdministrationTurnFollowupMode.CampaignResult)
            {
                state.CampaignResultAcknowledged = true;
                WICampaignRuntimeService.Instance?.AutoSave();
                if (choiceIndex == 1) ShowUGUICampaignTitle();
                return true;
            }
            if (uguiTurnFollowupMode != WIAdministrationTurnFollowupMode.Tutorial) return false;
            WITutorialDefinition tutorial = WITutorialSystem.GetPending(database, state);
            if (tutorial == null) return false;
            if (choiceIndex == 0) WITutorialSystem.Complete(state, tutorial.Id);
            else WITutorialSystem.SkipAll(state);
            WICampaignRuntimeService.Instance?.AutoSave();
            return true;
        }

        // 비활성 상태를 포함해 UGUI 캠페인 타이틀 프리팹을 찾아 다시 표시합니다.
        private void ShowUGUICampaignTitle()
        {
            WICampaignTitleUGUIController title = FindFirstObjectByType<WICampaignTitleUGUIController>(UnityEngine.FindObjectsInactive.Include);
            if (title != null) title.ShowCampaignStart();
        }
    }
}

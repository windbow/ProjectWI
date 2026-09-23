using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectWI.Administration
{
    public sealed class WIAdministrationFocusProjectUGUIController : WIAdministrationUGUIPanelController
    {
        // 모든 후보를 스크롤 행으로 표시하는 공통 목록입니다.
        [SerializeField] private WICharacterSelectionList selectionList;
        [SerializeField] private WIAdministrationModalUGUIController modal;
        [SerializeField] private GameObject projectPage;
        [SerializeField] private GameObject managerPage;
        [SerializeField] private TMP_Text messageLabel;
        [SerializeField] private TMP_Text[] projectNames;
        [SerializeField] private TMP_Text[] basicLabels;
        [SerializeField] private TMP_Text[] intensiveLabels;
        [SerializeField] private Button[] basicButtons;
        [SerializeField] private Button[] intensiveButtons;
        [SerializeField] private Button[] managerButtons;
        [SerializeField] private Image[] managerPortraits;
        [SerializeField] private TMP_Text[] managerLabels;
        [SerializeField] private Button managerBackButton;

        // 프리팹에 미리 저장된 유지 설정 버튼입니다.
        [SerializeField] private Button repeatButton;

        private WIAdministrationFocusProjectSnapshot currentSnapshot;
        private WICastleProjectType selectedProject;
        private WIProjectInvestment selectedInvestment;
        private System.Collections.Generic.List<WIAdministrationProjectManagerSnapshot> currentManagers;

        // 고정 사업·담당자 버튼을 기존 캠페인 기능과 연결합니다.
        private void Awake()
        {
            ResolveAdministrationController();

            for (int index = 0; index < basicButtons.Length; index += 1)
            {
                int captured = index;
                basicButtons[index].onClick.AddListener(() => OpenManagers(captured, WIProjectInvestment.Basic));
                intensiveButtons[index].onClick.AddListener(() => OpenManagers(captured, WIProjectInvestment.Intensive));
            }
            for (int index = 0; index < managerButtons.Length; index += 1)
            {
                int captured = index;
                managerButtons[index].onClick.AddListener(() => AssignManager(captured));
            }
            managerBackButton.onClick.AddListener(ShowProjectPage);
            if (repeatButton != null)
            {
                repeatButton.onClick.AddListener(ToggleRepeat);
            }
        }

        // UGUI 중점 사업 열기 요청을 구독합니다.
        private void OnEnable()
        {
            ResolveAdministrationController();
            if (administrationController != null)
            {
                administrationController.UGUIFocusProjectRequested += Open;
            }
        }

        // UGUI 중점 사업 열기 요청 구독을 해제합니다.
        private void OnDisable()
        {
            if (administrationController != null)
            {
                administrationController.UGUIFocusProjectRequested -= Open;
            }
        }

        // 현재 선택 성의 사업 목록과 비용을 첫 페이지에 표시합니다.
        private void Open()
        {
            RefreshRepeat();
            if (administrationController.TryGetUGUIFocusProjectSnapshot(out currentSnapshot, out string error)
                == false)
            {
                modal.Show("중점 사업");
                projectPage.SetActive(false);
                managerPage.SetActive(false);
                messageLabel.gameObject.SetActive(true);
                messageLabel.text = error;
                return;
            }

            int count = Mathf.Min(projectNames.Length, currentSnapshot.Projects.Count);
            for (int index = 0; index < count; index += 1)
            {
                WIAdministrationProjectOptionSnapshot option = currentSnapshot.Projects[index];
                projectNames[index].text = option.DisplayName;
                basicLabels[index].text = $"기본 {option.BasicCost}G";
                intensiveLabels[index].text = $"집중 {option.IntensiveCost}G";
            }
            modal.Show($"{currentSnapshot.CastleName} · 이번 달 중점 사업");
            ShowProjectPage();
        }

        // 다음 달 유지 지시를 전환하고 현재 버튼의 상태를 갱신합니다.
        private void ToggleRepeat()
        {
            administrationController.ToggleStandingOrder(false, string.Empty);
            RefreshRepeat();
        }

        // 데이터베이스 문자열로 고정 유지 버튼을 표시합니다.
        private void RefreshRepeat()
        {
            if (repeatButton != null)
            {
                repeatButton.GetComponentInChildren<TMP_Text>().text = administrationController.GetStandingOrderText(false, string.Empty);
            }
        }

        // 사업 목록 페이지를 표시합니다.
        private void ShowProjectPage()
        {
            messageLabel.gameObject.SetActive(true);
            messageLabel.text = administrationController.GetAdministrationText("UI_ORDER_HINT");
            projectPage.SetActive(true);
            managerPage.SetActive(false);
        }

        // 선택 사업과 투자 단계에 맞는 대기 담당자 목록을 표시합니다.
        private void OpenManagers(int projectIndex, WIProjectInvestment investment)
        {
            selectedProject = currentSnapshot.Projects[projectIndex].ProjectType;
            selectedInvestment = investment;
            if (administrationController.TryGetUGUIProjectManagers(selectedProject, investment,
                out currentManagers, out string error) == false)
            {
                messageLabel.gameObject.SetActive(true);
                messageLabel.text = error;
                return;
            }

            projectPage.SetActive(false);
            managerPage.SetActive(true);
            messageLabel.gameObject.SetActive(false);
            selectionList.Prepare(currentManagers.Count, AssignManager, administrationController,
                out managerButtons, out managerLabels, out managerPortraits);
            for (int index = 0; index < managerButtons.Length; index += 1)
            {
                bool visible = index < currentManagers.Count;
                managerButtons[index].gameObject.SetActive(visible);
                if (visible == false)
                {
                    continue;
                }

                WIAdministrationProjectManagerSnapshot manager = currentManagers[index];
                managerPortraits[index].sprite = manager.Portrait;
                managerPortraits[index].enabled = manager.Portrait != null;
                managerLabels[index].text = $"{manager.DisplayName}\n예상 성과 +{manager.ExpectedGain} · {manager.TraitText}\n" + administrationController.GetSelectionCharacterSummary(manager.HeroId);
            }
        }

        // 선택한 영웅에게 사업을 배정하고 성공 시 모달을 닫습니다.
        private void AssignManager(int managerIndex)
        {
            if (managerIndex >= currentManagers.Count)
            {
                return;
            }

            if (administrationController.AssignUGUIFocusProject(selectedProject, selectedInvestment,
                currentManagers[managerIndex].HeroId, out string error))
            {
                modal.Hide();
                return;
            }

            messageLabel.gameObject.SetActive(true);
            messageLabel.text = error;
        }
    }
}

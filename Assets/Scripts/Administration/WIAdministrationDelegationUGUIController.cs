using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectWI.Administration
{
    public sealed class WIAdministrationDelegationUGUIController : WIAdministrationUGUIPanelController
    {
        [SerializeField] private WIAdministrationModalUGUIController modal;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private TMP_Text messageLabel;
        [SerializeField] private Button[] governorButtons;
        [SerializeField] private Image[] governorPortraits;
        [SerializeField] private TMP_Text[] governorLabels;
        [SerializeField] private Button dismissButton;
        [SerializeField] private Button[] policyButtons;
        [SerializeField] private Button basicBudgetButton;
        [SerializeField] private Button intensiveBudgetButton;
        [SerializeField] private TMP_Text previewLabel;
        [SerializeField] private Button toggleButton;

        private WIAdministrationDelegationSnapshot snapshot;

        // 고정 영지관·방침·예산 버튼을 위임 설정 기능에 연결합니다.
        private void Awake()
        {
            ResolveAdministrationController();
            for (int index = 0; index < governorButtons.Length; index += 1)
            {
                int captured = index;
                governorButtons[index].onClick.AddListener(() => AssignGovernor(captured));
            }
            for (int index = 0; index < policyButtons.Length; index += 1)
            {
                WIGovernorPolicy captured = (WIGovernorPolicy)index;
                policyButtons[index].onClick.AddListener(() => ChangePolicy(captured));
            }
            dismissButton.onClick.AddListener(DismissGovernor);
            basicBudgetButton.onClick.AddListener(() => ChangeBudget(false));
            intensiveBudgetButton.onClick.AddListener(() => ChangeBudget(true));
            toggleButton.onClick.AddListener(ToggleDelegation);
        }

        // 영지관 위임 UGUI 열기 요청을 구독합니다.
        private void OnEnable()
        {
            ResolveAdministrationController();
            if (administrationController != null)
            {
                administrationController.UGUIDelegationRequested += Open;
            }
        }

        // 영지관 위임 UGUI 열기 요청 구독을 해제합니다.
        private void OnDisable()
        {
            if (administrationController != null)
            {
                administrationController.UGUIDelegationRequested -= Open;
            }
        }

        // 현재 성의 위임 설정을 열고 최신 상태를 표시합니다.
        private void Open()
        {
            modal.Show("영지관 위임 설정");
            Refresh();
        }

        // 기존 캠페인 상태를 다시 읽어 모든 고정 컨트롤에 반영합니다.
        private void Refresh()
        {
            if (administrationController.TryGetUGUIDelegationSnapshot(out snapshot, out string error)
                == false)
            {
                messageLabel.gameObject.SetActive(true);
                messageLabel.text = error;
                return;
            }
            messageLabel.gameObject.SetActive(false);
            statusLabel.text = snapshot.CastleName + " · 영지관과 자동 운영 조건을 설정하십시오.";
            for (int index = 0; index < governorButtons.Length; index += 1)
            {
                bool visible = index < snapshot.Candidates.Count;
                governorButtons[index].gameObject.SetActive(visible);
                if (visible == false)
                {
                    continue;
                }
                WIAdministrationGovernorCandidateSnapshot candidate = snapshot.Candidates[index];
                governorPortraits[index].sprite = candidate.Portrait;
                governorPortraits[index].enabled = candidate.Portrait != null;
                governorLabels[index].text = (candidate.HeroId == snapshot.GovernorHeroId ? "● " : string.Empty) +
                    candidate.DisplayName + "\n" + candidate.Summary;
                governorButtons[index].interactable = candidate.Interactable;
            }
            string[] policyNames = { "균형", "번영", "연구", "전선", "인재" };
            for (int index = 0; index < policyButtons.Length; index += 1)
            {
                policyButtons[index].GetComponentInChildren<TMP_Text>().text =
                    ((int)snapshot.Policy == index ? "● " : string.Empty) + policyNames[index];
            }
            basicBudgetButton.GetComponentInChildren<TMP_Text>().text = $"기본 예산 · 월 {snapshot.BasicBudget}G";
            intensiveBudgetButton.GetComponentInChildren<TMP_Text>().text = $"집중 예산 · 월 {snapshot.IntensiveBudget}G";
            previewLabel.text = snapshot.Preview;
            toggleButton.GetComponentInChildren<TMP_Text>().text = snapshot.Delegated ? "직접 관리로 전환" : "영지관에게 위임";
            dismissButton.interactable = string.IsNullOrEmpty(snapshot.GovernorHeroId) == false;
        }

        // 선택한 주둔 인물을 영지관으로 임명합니다.
        private void AssignGovernor(int index)
        {
            if (index >= snapshot.Candidates.Count)
            {
                return;
            }
            Apply(administrationController.AssignUGUIGovernor(snapshot.Candidates[index].HeroId, out string error), error);
        }

        // 현재 영지관을 해임하고 직접 관리로 전환합니다.
        private void DismissGovernor() => Apply(administrationController.DismissUGUIGovernor(out string error), error);

        // 자동 운영 방침을 변경합니다.
        private void ChangePolicy(WIGovernorPolicy policy) => Apply(administrationController.SetUGUIGovernorPolicy(policy, out string error), error);

        // 기본 또는 집중 월간 예산으로 변경합니다.
        private void ChangeBudget(bool intensive) => Apply(administrationController.SetUGUIGovernorBudget(intensive, out string error), error);

        // 영지관 위임과 직접 관리 상태를 전환합니다.
        private void ToggleDelegation() => Apply(administrationController.ToggleUGUIDelegation(out string error), error);

        // 설정 변경 결과를 표시하고 성공하면 전체 화면을 갱신합니다.
        private void Apply(bool succeeded, string error)
        {
            if (succeeded)
            {
                Refresh();
                return;
            }
            messageLabel.gameObject.SetActive(true);
            messageLabel.text = error;
        }
    }
}

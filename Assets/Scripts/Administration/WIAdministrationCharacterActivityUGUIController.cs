using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectWI.Administration
{
    public sealed class WIAdministrationCharacterActivityUGUIController : WIAdministrationUGUIPanelController
    {
        private const int PageSize = 8;

        private enum PageMode { Actor, Activity, Target, Transfer }
        [SerializeField] private WIAdministrationModalUGUIController modal;
        [SerializeField] private TMP_Text contextLabel;
        [SerializeField] private TMP_Text messageLabel;
        [SerializeField] private GameObject cardRoot;
        [SerializeField] private Button[] cardButtons;
        [SerializeField] private Image[] cardPortraits;
        [SerializeField] private TMP_Text[] cardLabels;
        [SerializeField] private GameObject activityRoot;
        [SerializeField] private Button[] activityButtons;
        [SerializeField] private Button previousButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private TMP_Text pageLabel;

        private WIAdministrationCharacterActivitySnapshot snapshot;
        private PageMode pageMode;
        private WICharacterActivityType pendingActivity;
        private string selectedHeroId;
        private int pageIndex;

        // 고정 카드와 활동 버튼을 인재 활동 단계에 연결합니다.
        private void Awake()
        {
            ResolveAdministrationController();
            for (int index = 0; index < cardButtons.Length; index += 1)
            {
                int captured = index;
                cardButtons[index].onClick.AddListener(() => SelectCard(captured));
            }
            WICharacterActivityType[] activities = { WICharacterActivityType.Search, WICharacterActivityType.Socialize,
                WICharacterActivityType.Recruit, WICharacterActivityType.Training, WICharacterActivityType.Rest };
            for (int index = 0; index < activityButtons.Length && index < activities.Length; index += 1)
            {
                WICharacterActivityType captured = activities[index];
                activityButtons[index].onClick.AddListener(() => SelectActivity(captured));
            }
            if (activityButtons.Length > activities.Length)
            {
                activityButtons[activities.Length].onClick.AddListener(SelectTransfer);
            }
            previousButton.onClick.AddListener(() => ChangePage(-1));
            nextButton.onClick.AddListener(() => ChangePage(1));
        }

        // 인재 활동 UGUI 열기 요청을 구독합니다.
        private void OnEnable()
        {
            ResolveAdministrationController();
            if (administrationController != null)
            {
                administrationController.UGUICharacterActivityRequested += Open;
            }
        }

        // 인재 활동 UGUI 열기 요청 구독을 해제합니다.
        private void OnDisable()
        {
            if (administrationController != null)
            {
                administrationController.UGUICharacterActivityRequested -= Open;
            }
        }

        // 현재 성에서 활동 가능한 인물 선택 화면을 엽니다.
        private void Open()
        {
            modal.Show("인재 활동 · 인물 선택");
            pageMode = PageMode.Actor;
            pageIndex = 0;
            if (administrationController.TryGetUGUICharacterActivityActors(out snapshot, out string error)
                == false)
            {
                ShowError(error);
                return;
            }
            contextLabel.text = snapshot.CastleName + " · 이번 달 개인 활동을 수행할 인물을 선택하십시오.";
            messageLabel.gameObject.SetActive(false);
            RefreshCards();
        }

        // 현재 단계의 후보를 고정된 카드에 표시합니다.
        private void RefreshCards()
        {
            cardRoot.SetActive(true);
            activityRoot.SetActive(false);
            int count = snapshot?.Candidates.Count ?? 0;
            int pageCount = Mathf.Max(1, Mathf.CeilToInt(count / (float)PageSize));
            pageIndex = Mathf.Clamp(pageIndex, 0, pageCount - 1);
            for (int index = 0; index < cardButtons.Length; index += 1)
            {
                int candidateIndex = pageIndex * PageSize + index;
                bool visible = candidateIndex < count;
                cardButtons[index].gameObject.SetActive(visible);
                if (visible == false)
                {
                    continue;
                }
                WIAdministrationCharacterActivityCandidateSnapshot candidate = snapshot.Candidates[candidateIndex];
                cardPortraits[index].sprite = candidate.Portrait;
                cardPortraits[index].enabled = candidate.Portrait != null;
                cardLabels[index].text = candidate.DisplayName + "\n" + candidate.Summary;
                cardButtons[index].interactable = candidate.Interactable;
            }
            previousButton.interactable = pageIndex > 0;
            nextButton.interactable = pageIndex + 1 < pageCount;
            pageLabel.text = $"{pageIndex + 1} / {pageCount}";
        }

        // 선택한 인물의 활동 종류 선택 화면으로 전환합니다.
        private void SelectCard(int cardIndex)
        {
            int candidateIndex = pageIndex * PageSize + cardIndex;
            if (snapshot == null || candidateIndex >= snapshot.Candidates.Count)
            {
                return;
            }
            WIAdministrationCharacterActivityCandidateSnapshot candidate = snapshot.Candidates[candidateIndex];
            if (pageMode == PageMode.Actor)
            {
                selectedHeroId = candidate.HeroId;
                pageMode = PageMode.Activity;
                modal.SetTitle(candidate.DisplayName + " · 개인 활동");
                contextLabel.text = "이번 달 수행할 활동을 선택하십시오.";
                cardRoot.SetActive(false);
                activityRoot.SetActive(true);
                previousButton.interactable = false;
                nextButton.interactable = false;
                pageLabel.text = string.Empty;
                return;
            }
            if (pageMode == PageMode.Transfer)
            {
                StartTransfer(candidate.HeroId);
                return;
            }
            Assign(pendingActivity, candidate.HeroId);
        }

        // 대상 없는 활동은 즉시 배정하고, 교류와 영입은 대상 선택으로 전환합니다.
        private void SelectActivity(WICharacterActivityType activity)
        {
            if (activity != WICharacterActivityType.Socialize && activity != WICharacterActivityType.Recruit)
            {
                Assign(activity, string.Empty);
                return;
            }
            pendingActivity = activity;
            pageMode = PageMode.Target;
            pageIndex = 0;
            if (administrationController.TryGetUGUICharacterActivityTargets(selectedHeroId, activity,
                out snapshot, out string error) == false)
            {
                ShowError(error);
                return;
            }
            modal.SetTitle(activity == WICharacterActivityType.Socialize ? "교류 대상 선택" : "영입 대상 선택");
            contextLabel.text = activity == WICharacterActivityType.Socialize
                ? "관계를 개선할 같은 성의 인물을 선택하십시오."
                : "발견한 인재 중 설득할 대상을 선택하십시오.";
            messageLabel.gameObject.SetActive(false);
            RefreshCards();
        }

        // 선택 인물이 이동할 수 있는 같은 진영의 인접 성 카드 단계로 전환합니다.
        private void SelectTransfer()
        {
            pageMode = PageMode.Transfer;
            pageIndex = 0;
            if (administrationController.TryGetUGUICharacterTransferTargets(selectedHeroId, out snapshot, out string error)
                == false)
            {
                ShowError(error);
                return;
            }
            modal.SetTitle("인물 이동");
            contextLabel.text = "다음 달에 도착할 같은 진영의 인접 성을 선택하십시오.";
            messageLabel.gameObject.SetActive(false);
            RefreshCards();
        }

        // 선택 인물의 한 달 이동을 기존 이동 시스템으로 시작합니다.
        private void StartTransfer(string targetCastleId)
        {
            if (administrationController.StartUGUICharacterTransfer(selectedHeroId, targetCastleId, out string message)
                == false)
            {
                ShowError(message);
                return;
            }
            modal.Hide();
            administrationController.ShowUGUIMessage("인물 이동", message);
        }

        // 선택한 개인 활동을 기존 게임 상태에 배정합니다.
        private void Assign(WICharacterActivityType activity, string targetHeroId)
        {
            if (administrationController.AssignUGUICharacterActivity(selectedHeroId, activity, targetHeroId, out string error))
            {
                modal.Hide();
                return;
            }
            ShowError(error);
        }

        // 카드 페이지를 지정한 방향으로 이동합니다.
        private void ChangePage(int direction) { pageIndex += direction; RefreshCards(); }

        // 오류 메시지를 표시하고 선택 영역을 숨깁니다.
        private void ShowError(string error)
        {
            messageLabel.gameObject.SetActive(true);
            messageLabel.text = error;
            cardRoot.SetActive(false);
            activityRoot.SetActive(false);
            previousButton.interactable = false;
            nextButton.interactable = false;
            pageLabel.text = string.Empty;
        }
    }
}

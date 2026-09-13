using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectWI.Administration
{
    public sealed class WIAdministrationHeroAssignmentUGUIController : WIAdministrationUGUIPanelController
    {
        private const int PageSize = 8;
        [SerializeField] private WIAdministrationModalUGUIController modal;
        [SerializeField] private TMP_Text slotStatusLabel;
        [SerializeField] private TMP_Text messageLabel;
        [SerializeField] private Button[] candidateButtons;
        [SerializeField] private Image[] candidatePortraits;
        [SerializeField] private TMP_Text[] candidateLabels;
        [SerializeField] private Button previousButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private TMP_Text pageLabel;

        private WIAdministrationHeroAssignmentSnapshot currentSnapshot;
        private int pageIndex;

        // 고정 후보 카드와 페이지 버튼을 영웅 배치 기능에 연결합니다.
        private void Awake()
        {
            ResolveAdministrationController();
            for (int index = 0; index < candidateButtons.Length; index += 1)
            {
                int captured = index;
                candidateButtons[index].onClick.AddListener(() => Assign(captured));
            }
            previousButton.onClick.AddListener(() => ChangePage(-1));
            nextButton.onClick.AddListener(() => ChangePage(1));
        }

        // UGUI 영웅 배치 열기 요청을 구독합니다.
        private void OnEnable()
        {
            ResolveAdministrationController();
            if (administrationController != null)
            {
                administrationController.UGUIHeroAssignmentRequested += Open;
            }
        }

        // UGUI 영웅 배치 열기 요청 구독을 해제합니다.
        private void OnDisable()
        {
            if (administrationController != null)
            {
                administrationController.UGUIHeroAssignmentRequested -= Open;
            }
        }

        // 현재 성의 슬롯과 배치 가능한 후보를 표시합니다.
        private void Open()
        {
            modal.Show(administrationController.GetAdministrationText("UI_ASSIGN_RESIDENT_TITLE"));
            pageIndex = 0;
            if (administrationController.TryGetUGUIHeroAssignmentSnapshot(out currentSnapshot, out string error)
                == false)
            {
                slotStatusLabel.text = string.Empty;
                messageLabel.gameObject.SetActive(true);
                messageLabel.text = administrationController.GetAdministrationText("UI_ASSIGN_RESIDENT_HINT") + "\n\n" + error;
                HideCandidates();
                return;
            }

            messageLabel.gameObject.SetActive(false);
            slotStatusLabel.text =
                $"{currentSnapshot.CastleName} · 주둔 인물 {currentSnapshot.OccupiedSlots}/{currentSnapshot.MaximumSlots}";
            RefreshPage();
        }

        // 현재 후보 페이지를 고정된 8개 카드에 반영합니다.
        private void RefreshPage()
        {
            int pageCount = Mathf.Max(1, Mathf.CeilToInt(currentSnapshot.Candidates.Count / (float)PageSize));
            pageIndex = Mathf.Clamp(pageIndex, 0, pageCount - 1);
            for (int index = 0; index < candidateButtons.Length; index += 1)
            {
                int candidateIndex = pageIndex * PageSize + index;
                bool visible = candidateIndex < currentSnapshot.Candidates.Count;
                candidateButtons[index].gameObject.SetActive(visible);
                if (visible == false)
                {
                    continue;
                }

                WIAdministrationHeroAssignmentCandidateSnapshot candidate =
                    currentSnapshot.Candidates[candidateIndex];
                candidatePortraits[index].sprite = candidate.Portrait;
                candidatePortraits[index].enabled = candidate.Portrait != null;
                candidateLabels[index].text = $"{candidate.DisplayName}\n{candidate.Summary}";
            }
            previousButton.interactable = pageIndex > 0;
            nextButton.interactable = pageIndex + 1 < pageCount;
            previousButton.gameObject.SetActive(pageCount > 1);
            nextButton.gameObject.SetActive(pageCount > 1);
            pageLabel.gameObject.SetActive(pageCount > 1);
            pageLabel.text = $"{pageIndex + 1} / {pageCount}";
        }

        // 지정한 방향으로 후보 페이지를 전환합니다.
        private void ChangePage(int direction)
        {
            pageIndex += direction;
            RefreshPage();
        }

        // 선택한 후보를 현재 성에 배치하고 성공 시 모달을 닫습니다.
        private void Assign(int cardIndex)
        {
            int candidateIndex = pageIndex * PageSize + cardIndex;
            if (candidateIndex >= currentSnapshot.Candidates.Count)
            {
                return;
            }

            if (administrationController.AssignUGUIHeroToSelectedCastle(
                currentSnapshot.Candidates[candidateIndex].HeroId, out string error))
            {
                modal.Hide();
                return;
            }
            messageLabel.gameObject.SetActive(true);
            messageLabel.text = error;
        }

        // 오류 상태에서 후보 카드를 모두 숨깁니다.
        private void HideCandidates()
        {
            foreach (Button button in candidateButtons)
            {
                button.gameObject.SetActive(false);
            }
            previousButton.interactable = false;
            nextButton.interactable = false;
            previousButton.gameObject.SetActive(false);
            nextButton.gameObject.SetActive(false);
            pageLabel.gameObject.SetActive(false);
            pageLabel.text = string.Empty;
        }
    }
}

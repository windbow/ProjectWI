using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectWI.Administration
{
    public sealed class WIAdministrationDiplomacyUGUIController : WIAdministrationUGUIPanelController
    {
        [SerializeField] private WIAdministrationModalUGUIController modal;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private TMP_Text messageLabel;
        [SerializeField] private Button[] cardButtons;
        [SerializeField] private TMP_Text[] cardLabels;
        [SerializeField] private Button previousButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private TMP_Text pageLabel;
        [SerializeField] private Button backButton;

        private WIAdministrationDiplomacySnapshot snapshot;
        private string mode = "factions";
        private string factionId = string.Empty;
        private int page;

        // 외교 대상·명령 카드와 페이지 이동을 실제 외교 기능에 연결합니다.
        private void Awake()
        {
            ResolveAdministrationController();
            for (int index = 0; index < cardButtons.Length; index += 1)
            {
                int captured = index;
                cardButtons[index].onClick.AddListener(() => SelectCard(captured));
            }
            previousButton.onClick.AddListener(() => ChangePage(-1));
            nextButton.onClick.AddListener(() => ChangePage(1));
            backButton.onClick.AddListener(ReturnToFactions);
        }

        // 외교 UGUI 열기 요청을 구독합니다.
        private void OnEnable()
        {
            ResolveAdministrationController();
            if (administrationController != null)
            {
                administrationController.UGUIDiplomacyRequested += Open;
            }
        }

        // 외교 UGUI 열기 요청 구독을 해제합니다.
        private void OnDisable()
        {
            if (administrationController != null)
            {
                administrationController.UGUIDiplomacyRequested -= Open;
            }
        }

        // 외교 대상 진영 목록을 첫 페이지부터 엽니다.
        private void Open()
        {
            mode = "factions";
            factionId = string.Empty;
            page = 0;
            modal.Show("외교");
            Refresh();
        }

        // 현재 외교 단계의 스냅샷을 8개 고정 카드에 표시합니다.
        private void Refresh()
        {
            if (administrationController.TryGetUGUIDiplomacyPanel(mode, factionId, out snapshot, out string error)
                == false)
            {
                messageLabel.gameObject.SetActive(true);
                messageLabel.text = error;
                return;
            }
            int pageCount = Mathf.Max(1, Mathf.CeilToInt(snapshot.Cards.Count / 8f));
            page = Mathf.Clamp(page, 0, pageCount - 1);
            modal.SetTitle(snapshot.Title);
            statusLabel.text = snapshot.Summary;
            messageLabel.gameObject.SetActive(snapshot.Cards.Count == 0);
            messageLabel.text = snapshot.Cards.Count == 0 ? "실행 가능한 외교 대상이나 명령이 없습니다." : string.Empty;
            for (int index = 0; index < cardButtons.Length; index += 1)
            {
                int sourceIndex = page * cardButtons.Length + index;
                bool visible = sourceIndex < snapshot.Cards.Count;
                cardButtons[index].gameObject.SetActive(visible);
                if (visible == false)
                {
                    continue;
                }
                WIAdministrationDiplomacyCardSnapshot card = snapshot.Cards[sourceIndex];
                cardLabels[index].text = card.Title + "\n" + card.Description;
                cardButtons[index].interactable = card.Interactable;
            }
            previousButton.gameObject.SetActive(pageCount > 1);
            nextButton.gameObject.SetActive(pageCount > 1);
            pageLabel.gameObject.SetActive(pageCount > 1);
            previousButton.interactable = page > 0;
            nextButton.interactable = page < pageCount - 1;
            pageLabel.text = $"{page + 1} / {pageCount}";
            backButton.gameObject.SetActive(mode == "detail");
        }

        // 진영 상세 진입 또는 선택 외교 명령을 실행합니다.
        private void SelectCard(int index)
        {
            int sourceIndex = page * cardButtons.Length + index;
            if (snapshot == null || sourceIndex >= snapshot.Cards.Count)
            {
                return;
            }
            WIAdministrationDiplomacyCardSnapshot card = snapshot.Cards[sourceIndex];
            if (card.Action == "faction")
            {
                mode = "detail";
                factionId = card.Id;
                page = 0;
                Refresh();
                return;
            }
            if (administrationController.ExecuteUGUIDiplomacyAction(card.Action, factionId, card.Id, out string error)
                == false)
            {
                messageLabel.gameObject.SetActive(true);
                messageLabel.text = error;
                return;
            }
            Refresh();
        }

        // 외교 카드 페이지를 이동합니다.
        private void ChangePage(int delta)
        {
            page += delta;
            Refresh();
        }

        // 대상 진영 상세에서 전체 진영 목록으로 돌아갑니다.
        private void ReturnToFactions()
        {
            mode = "factions";
            factionId = string.Empty;
            page = 0;
            Refresh();
        }
    }
}

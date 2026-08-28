using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectWI.Administration
{
    public sealed class WIAdministrationSchemeUGUIController : WIAdministrationUGUIPanelController
    {
        [SerializeField] private WIAdministrationModalUGUIController modal;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private TMP_Text messageLabel;
        [SerializeField] private Button[] cardButtons;
        [SerializeField] private Image[] cardPortraits;
        [SerializeField] private TMP_Text[] cardLabels;
        [SerializeField] private Button previousButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private TMP_Text pageLabel;
        [SerializeField] private Button backButton;

        private readonly Stack<SchemeViewState> history = new();
        private WIAdministrationSchemeSnapshot snapshot;
        private SchemeViewState view = new("schemes", string.Empty, string.Empty, string.Empty);
        private int page;

        // 첩보 단계 카드와 페이지·이전 버튼을 실제 첩보 기능에 연결합니다.
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
            backButton.onClick.AddListener(GoBack);
        }

        // 첩보 UGUI 열기 요청을 구독합니다.
        private void OnEnable()
        {
            ResolveAdministrationController();
            if (administrationController != null)
            {
                administrationController.UGUISchemeRequested += Open;
            }
        }

        // 첩보 UGUI 열기 요청 구독을 해제합니다.
        private void OnDisable()
        {
            if (administrationController != null)
            {
                administrationController.UGUISchemeRequested -= Open;
            }
        }

        // 첩보 종류 목록을 첫 페이지부터 엽니다.
        private void Open()
        {
            history.Clear();
            view = new SchemeViewState("schemes", string.Empty, string.Empty, string.Empty);
            page = 0;
            modal.Show("첩보");
            Refresh();
        }

        // 현재 단계의 첩보 스냅샷을 8개 고정 카드에 표시합니다.
        private void Refresh()
        {
            if (administrationController.TryGetUGUISchemePanel(view.Mode, view.SchemeId, view.AgentId, view.CastleId,
                    out snapshot, out string error) == false)
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
            messageLabel.text = snapshot.Cards.Count == 0 ? "현재 조건에서 선택할 수 있는 대상이 없습니다." : string.Empty;
            for (int index = 0; index < cardButtons.Length; index += 1)
            {
                int sourceIndex = page * cardButtons.Length + index;
                bool visible = sourceIndex < snapshot.Cards.Count;
                cardButtons[index].gameObject.SetActive(visible);
                if (visible == false)
                {
                    continue;
                }
                WIAdministrationSchemeCardSnapshot card = snapshot.Cards[sourceIndex];
                cardLabels[index].text = card.Title + "\n" + card.Description;
                cardPortraits[index].sprite = card.Portrait;
                cardPortraits[index].enabled = card.Portrait != null;
                cardButtons[index].interactable = card.Interactable;
            }
            previousButton.gameObject.SetActive(pageCount > 1);
            nextButton.gameObject.SetActive(pageCount > 1);
            pageLabel.gameObject.SetActive(pageCount > 1);
            previousButton.interactable = page > 0;
            nextButton.interactable = page < pageCount - 1;
            pageLabel.text = $"{page + 1} / {pageCount}";
            backButton.gameObject.SetActive(view.Mode != "schemes");
        }

        // 첩보 종류·담당자·대상을 다음 단계로 이동하거나 임무를 예약합니다.
        private void SelectCard(int index)
        {
            int sourceIndex = page * cardButtons.Length + index;
            if (snapshot == null || sourceIndex >= snapshot.Cards.Count)
            {
                return;
            }
            WIAdministrationSchemeCardSnapshot card = snapshot.Cards[sourceIndex];
            if (card.Action == "scheme")
            {
                Push(new SchemeViewState("agents", card.Id, string.Empty, string.Empty));
                return;
            }
            if (card.Action == "agent")
            {
                Push(new SchemeViewState("castles", view.SchemeId, card.Id, string.Empty));
                return;
            }
            if (card.Action == "castle-heroes")
            {
                Push(new SchemeViewState("heroes", view.SchemeId, view.AgentId, card.Id));
                return;
            }
            string targetHeroId = card.Action == "execute-hero" ? card.Id : string.Empty;
            string targetCastleId = card.Action == "execute-hero" ? view.CastleId : card.Id;
            if (administrationController.ScheduleUGUIScheme(view.SchemeId, view.AgentId, targetCastleId, targetHeroId, out string error)
                == false)
            {
                messageLabel.gameObject.SetActive(true);
                messageLabel.text = error;
                return;
            }
            history.Clear();
            view = new SchemeViewState("schemes", string.Empty, string.Empty, string.Empty);
            page = 0;
            Refresh();
        }

        // 현재 선택 상태를 기록하고 다음 첩보 단계로 이동합니다.
        private void Push(SchemeViewState next)
        {
            history.Push(view);
            view = next;
            page = 0;
            Refresh();
        }

        // 첩보 카드 페이지를 이동합니다.
        private void ChangePage(int delta)
        {
            page += delta;
            Refresh();
        }

        // 직전 첩보 선택 단계로 돌아갑니다.
        private void GoBack()
        {
            view = history.Count > 0 ? history.Pop() : new SchemeViewState("schemes", string.Empty, string.Empty, string.Empty);
            page = 0;
            Refresh();
        }

        private readonly struct SchemeViewState
        {
            public readonly string Mode;
            public readonly string SchemeId;
            public readonly string AgentId;
            public readonly string CastleId;

            // 첩보 모달의 현재 단계와 선택 문맥을 보관합니다.
            public SchemeViewState(string mode, string schemeId, string agentId, string castleId)
            {
                Mode = mode;
                SchemeId = schemeId;
                AgentId = agentId;
                CastleId = castleId;
            }
        }
    }
}

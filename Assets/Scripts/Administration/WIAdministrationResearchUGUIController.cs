using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectWI.Administration
{
    public sealed class WIAdministrationResearchUGUIController : MonoBehaviour
    {
        [SerializeField] private WIAdministrationUIController administrationController;
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

        private WIAdministrationResearchSnapshot snapshot;
        private string mode = "research";
        private string researchId = string.Empty;
        private int page;

        // 연구·담당자 카드와 페이지·이전 버튼을 실제 연구 기능에 연결합니다.
        private void Awake()
        {
            if (administrationController == null) administrationController = FindFirstObjectByType<WIAdministrationUIController>();
            for (int index = 0; index < cardButtons.Length; index += 1)
            {
                int captured = index;
                cardButtons[index].onClick.AddListener(() => SelectCard(captured));
            }
            previousButton.onClick.AddListener(() => ChangePage(-1));
            nextButton.onClick.AddListener(() => ChangePage(1));
            backButton.onClick.AddListener(ReturnToResearch);
        }

        // 연구 UGUI 열기 요청을 구독합니다.
        private void OnEnable()
        {
            if (administrationController == null) administrationController = FindFirstObjectByType<WIAdministrationUIController>();
            if (administrationController != null) administrationController.UGUIResearchRequested += Open;
        }

        // 연구 UGUI 열기 요청 구독을 해제합니다.
        private void OnDisable()
        {
            if (administrationController != null) administrationController.UGUIResearchRequested -= Open;
        }

        // 전체 연구 목록을 첫 페이지부터 엽니다.
        private void Open()
        {
            mode = "research";
            researchId = string.Empty;
            page = 0;
            modal.Show("기술·마법 연구");
            Refresh();
        }

        // 현재 연구 단계의 스냅샷을 8개 고정 카드에 표시합니다.
        private void Refresh()
        {
            if (administrationController.TryGetUGUIResearchPanel(mode, researchId, out snapshot, out string error) == false)
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
            messageLabel.text = snapshot.Cards.Count == 0 ? "현재 표시할 연구 또는 담당자가 없습니다." : string.Empty;
            for (int index = 0; index < cardButtons.Length; index += 1)
            {
                int sourceIndex = page * cardButtons.Length + index;
                bool visible = sourceIndex < snapshot.Cards.Count;
                cardButtons[index].gameObject.SetActive(visible);
                if (visible == false) continue;
                WIAdministrationResearchCardSnapshot card = snapshot.Cards[sourceIndex];
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
            backButton.gameObject.SetActive(mode == "researchers");
        }

        // 연구 선택 시 담당자 단계로 이동하고 담당자 선택 시 연구를 시작합니다.
        private void SelectCard(int index)
        {
            int sourceIndex = page * cardButtons.Length + index;
            if (snapshot == null || sourceIndex >= snapshot.Cards.Count) return;
            WIAdministrationResearchCardSnapshot card = snapshot.Cards[sourceIndex];
            if (card.Action == "research")
            {
                mode = "researchers";
                researchId = card.Id;
                page = 0;
                Refresh();
                return;
            }
            if (administrationController.BeginUGUIResearch(researchId, card.Id, out string error) == false)
            {
                messageLabel.gameObject.SetActive(true);
                messageLabel.text = error;
                return;
            }
            mode = "research";
            researchId = string.Empty;
            page = 0;
            Refresh();
        }

        // 연구 또는 담당자 카드 페이지를 이동합니다.
        private void ChangePage(int delta)
        {
            page += delta;
            Refresh();
        }

        // 담당자 선택 화면에서 전체 연구 목록으로 돌아갑니다.
        private void ReturnToResearch()
        {
            mode = "research";
            researchId = string.Empty;
            page = 0;
            Refresh();
        }
    }
}

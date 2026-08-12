using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectWI.Administration
{
    public sealed class WIAdministrationHeroesUGUIController : MonoBehaviour
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

        private WIAdministrationHeroesSnapshot snapshot;
        private string mode = "heroes";
        private string heroId = string.Empty;
        private int page;

        // 고정 영웅 카드와 페이지·이전 버튼을 영웅 기능에 연결합니다.
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
            backButton.onClick.AddListener(ReturnToHeroes);
        }

        // 영웅 UGUI 열기 요청을 구독합니다.
        private void OnEnable()
        {
            if (administrationController == null) administrationController = FindFirstObjectByType<WIAdministrationUIController>();
            if (administrationController != null) administrationController.UGUIHeroesRequested += Open;
        }

        // 영웅 UGUI 열기 요청 구독을 해제합니다.
        private void OnDisable()
        {
            if (administrationController != null) administrationController.UGUIHeroesRequested -= Open;
        }

        // 영입 영웅 전체 목록을 첫 페이지부터 엽니다.
        private void Open()
        {
            mode = "heroes";
            heroId = string.Empty;
            page = 0;
            modal.Show("영웅 목록");
            Refresh();
        }

        // 현재 단계의 데이터를 읽어 8개 고정 카드에 표시합니다.
        private void Refresh()
        {
            if (administrationController.TryGetUGUIHeroesPanel(mode, heroId, out snapshot, out string error) == false)
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
            messageLabel.text = snapshot.Cards.Count == 0 ? "표시할 인물이 없습니다." : string.Empty;
            for (int index = 0; index < cardButtons.Length; index += 1)
            {
                int sourceIndex = page * cardButtons.Length + index;
                bool visible = sourceIndex < snapshot.Cards.Count;
                cardButtons[index].gameObject.SetActive(visible);
                if (visible == false) continue;
                WIAdministrationHeroCardSnapshot card = snapshot.Cards[sourceIndex];
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
            backButton.gameObject.SetActive(mode != "heroes");
        }

        // 영웅 상세 진입 또는 승격·작위 수여를 처리합니다.
        private void SelectCard(int index)
        {
            int sourceIndex = page * cardButtons.Length + index;
            if (snapshot == null || sourceIndex >= snapshot.Cards.Count) return;
            WIAdministrationHeroCardSnapshot card = snapshot.Cards[sourceIndex];
            if (card.Action == "hero")
            {
                mode = "titles";
                heroId = card.Id;
                page = 0;
                Refresh();
                return;
            }
            if (administrationController.ExecuteUGUIHeroAction(card.Action, heroId, card.Id, out string error) == false)
            {
                messageLabel.gameObject.SetActive(true);
                messageLabel.text = error;
                return;
            }
            Refresh();
        }

        // 영웅 또는 작위 목록의 페이지를 이동합니다.
        private void ChangePage(int delta)
        {
            page += delta;
            Refresh();
        }

        // 작위 화면에서 전체 영웅 목록으로 돌아갑니다.
        private void ReturnToHeroes()
        {
            mode = "heroes";
            heroId = string.Empty;
            page = 0;
            Refresh();
        }
    }
}

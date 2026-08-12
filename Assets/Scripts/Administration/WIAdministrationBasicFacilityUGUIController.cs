using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectWI.Administration
{
    public sealed class WIAdministrationBasicFacilityUGUIController : MonoBehaviour
    {
        private enum PageMode { Facility, Quest, Hero }

        [SerializeField] private WIAdministrationUIController administrationController;
        [SerializeField] private WIAdministrationModalUGUIController modal;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private TMP_Text messageLabel;
        [SerializeField] private GameObject facilityRoot;
        [SerializeField] private Button tavernButton;
        [SerializeField] private GameObject cardRoot;
        [SerializeField] private Button[] cardButtons;
        [SerializeField] private Image[] cardImages;
        [SerializeField] private TMP_Text[] cardLabels;

        private WIAdministrationBasicFacilitySnapshot snapshot;
        private PageMode pageMode;
        private string selectedQuestId;

        // 선술집 버튼과 고정 선택 카드를 기본 시설 흐름에 연결합니다.
        private void Awake()
        {
            if (administrationController == null) administrationController = FindFirstObjectByType<WIAdministrationUIController>();
            tavernButton.onClick.AddListener(OpenQuests);
            for (int index = 0; index < cardButtons.Length; index += 1)
            {
                int captured = index;
                cardButtons[index].onClick.AddListener(() => SelectCard(captured));
            }
        }

        // 기본 시설 UGUI 열기 요청을 구독합니다.
        private void OnEnable()
        {
            if (administrationController == null) administrationController = FindFirstObjectByType<WIAdministrationUIController>();
            if (administrationController != null) administrationController.UGUIBasicFacilityRequested += Open;
        }

        // 기본 시설 UGUI 열기 요청 구독을 해제합니다.
        private void OnDisable()
        {
            if (administrationController != null) administrationController.UGUIBasicFacilityRequested -= Open;
        }

        // 성관·시장·훈련소·선술집의 기본 역할 안내를 표시합니다.
        private void Open()
        {
            modal.Show("기본 시설");
            pageMode = PageMode.Facility;
            statusLabel.text = "모든 성이 기본으로 보유하며 건설하거나 강화하지 않습니다.";
            messageLabel.gameObject.SetActive(false);
            facilityRoot.SetActive(true);
            cardRoot.SetActive(false);
        }

        // 현재 성의 월간 선술집 의뢰를 카드 목록으로 표시합니다.
        private void OpenQuests()
        {
            pageMode = PageMode.Quest;
            modal.SetTitle("선술집 월간 의뢰");
            facilityRoot.SetActive(false);
            cardRoot.SetActive(true);
            if (administrationController.TryGetUGUITavernQuests(out snapshot, out string error) == false)
            {
                ShowError(error);
                return;
            }
            statusLabel.text = snapshot.CastleName + " · 담당 인물을 배정할 의뢰를 선택하십시오.";
            messageLabel.gameObject.SetActive(false);
            RefreshCards();
        }

        // 현재 의뢰 또는 담당 인물 스냅샷을 고정 카드에 반영합니다.
        private void RefreshCards()
        {
            int count = pageMode == PageMode.Quest ? snapshot.Quests.Count : snapshot.Heroes.Count;
            for (int index = 0; index < cardButtons.Length; index += 1)
            {
                bool visible = index < count;
                cardButtons[index].gameObject.SetActive(visible);
                if (visible == false) continue;
                if (pageMode == PageMode.Quest)
                {
                    WIAdministrationTavernQuestSnapshot quest = snapshot.Quests[index];
                    cardImages[index].enabled = false;
                    RectTransform questLabelRect = cardLabels[index].rectTransform;
                    questLabelRect.anchorMax = Vector2.one;
                    cardLabels[index].text = quest.DisplayName + "\n" + quest.Summary + "\n" + quest.Description;
                    cardButtons[index].interactable = quest.Available;
                }
                else
                {
                    WIAdministrationQuestHeroSnapshot hero = snapshot.Heroes[index];
                    cardImages[index].sprite = hero.Portrait;
                    cardImages[index].enabled = hero.Portrait != null;
                    RectTransform heroLabelRect = cardLabels[index].rectTransform;
                    heroLabelRect.anchorMax = new Vector2(1f, 0.31f);
                    cardLabels[index].text = hero.DisplayName + "\n" + hero.Summary;
                    cardButtons[index].interactable = true;
                }
            }
        }

        // 의뢰를 선택하면 담당 인물 단계로 이동하고, 인물을 선택하면 의뢰를 수락합니다.
        private void SelectCard(int index)
        {
            if (pageMode == PageMode.Quest)
            {
                if (index >= snapshot.Quests.Count) return;
                selectedQuestId = snapshot.Quests[index].QuestId;
                if (administrationController.TryGetUGUIQuestHeroes(selectedQuestId, out snapshot, out string error) == false)
                {
                    ShowError(error);
                    return;
                }
                pageMode = PageMode.Hero;
                modal.SetTitle("의뢰 담당 인물 선택");
                statusLabel.text = snapshot.CastleName + " · 권장 적성과 예상 보너스를 비교하십시오.";
                RefreshCards();
                return;
            }
            if (pageMode != PageMode.Hero || index >= snapshot.Heroes.Count) return;
            if (administrationController.AssignUGUITavernQuest(selectedQuestId, snapshot.Heroes[index].HeroId, out string assignError))
            {
                modal.Hide();
                return;
            }
            ShowError(assignError);
        }

        // 오류 문구를 표시하고 선택 카드를 숨깁니다.
        private void ShowError(string error)
        {
            messageLabel.gameObject.SetActive(true);
            messageLabel.text = error;
            foreach (Button button in cardButtons) button.gameObject.SetActive(false);
        }
    }
}

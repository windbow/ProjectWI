using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectWI.Administration
{
    public sealed class WIAdministrationBasicFacilityUGUIController : WIAdministrationUGUIPanelController
    {
        private enum PageMode { Facility, Quest, Hero }
        [SerializeField] private WIAdministrationModalUGUIController modal;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private TMP_Text messageLabel;
        [SerializeField] private GameObject facilityRoot;
        [SerializeField] private Button castleHallButton;
        [SerializeField] private Button marketButton;
        [SerializeField] private Button trainingGroundButton;
        [SerializeField] private Button tavernButton;
        [SerializeField] private GameObject cardRoot;
        [SerializeField] private Button[] cardButtons;
        [SerializeField] private Image[] cardImages;
        [SerializeField] private TMP_Text[] cardLabels;

        private WIAdministrationBasicFacilitySnapshot snapshot;
        private PageMode pageMode;
        private string selectedQuestId;
        private const string TalentOfficeCardId = "talent_office";

        // 네 기본 시설 버튼과 고정 선택 카드를 시설별 실제 기능 흐름에 연결합니다.
        private void Awake()
        {
            ResolveAdministrationController();
            castleHallButton.onClick.AddListener(OpenCastleHall);
            marketButton.onClick.AddListener(OpenMarket);
            trainingGroundButton.onClick.AddListener(OpenTrainingGround);
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
            ResolveAdministrationController();
            if (administrationController != null)
            {
                administrationController.UGUIBasicFacilityRequested += Open;
                administrationController.UGUITavernRequested += OpenTavern;
            }
        }

        // 기본 시설 UGUI 열기 요청 구독을 해제합니다.
        private void OnDisable()
        {
            if (administrationController != null)
            {
                administrationController.UGUIBasicFacilityRequested -= Open;
                administrationController.UGUITavernRequested -= OpenTavern;
            }
        }

        // 성관·시장·훈련소·선술집의 기본 역할 안내를 표시합니다.
        private void Open()
        {
            modal.Show("기본 시설");
            pageMode = PageMode.Facility;
            statusLabel.text = "시설을 선택해 현재 성의 운영 기능을 이용하십시오.";
            messageLabel.gameObject.SetActive(false);
            facilityRoot.SetActive(true);
            cardRoot.SetActive(false);
        }

        // 성관에서 현재 성의 영지관 임명과 위임 운영 화면으로 이동합니다.
        private void OpenCastleHall()
        {
            modal.Hide();
            administrationController.ExecuteUGUITerritoryCommand(WIAdministrationTerritoryCommand.Delegation);
        }

        // 시장에서 현재 성의 수입과 운영 기록을 확인하는 화면으로 이동합니다.
        private void OpenMarket()
        {
            modal.Hide();
            administrationController.ExecuteUGUITerritoryCommand(WIAdministrationTerritoryCommand.CastleRecord);
        }

        // 훈련소에서 개인 훈련을 포함한 주둔 인물 활동 화면으로 이동합니다.
        private void OpenTrainingGround()
        {
            modal.Hide();
            administrationController.ExecuteUGUITerritoryCommand(WIAdministrationTerritoryCommand.CharacterActivity);
        }

        // 기존 모달을 열고 시설 목록을 거치지 않고 선술집 내용을 표시합니다.
        private void OpenTavern()
        {
            modal.Show(administrationController.GetAdministrationText("UI_TAVERN_DIRECT_TITLE"));
            OpenQuests();
        }

        // 현재 성의 월간 선술집 의뢰를 카드 목록으로 표시합니다.
        private void OpenQuests()
        {
            pageMode = PageMode.Quest;
            modal.SetTitle(administrationController.GetAdministrationText("UI_TAVERN_DIRECT_TITLE"));
            facilityRoot.SetActive(false);
            cardRoot.SetActive(true);
            if (administrationController.TryGetUGUITavernQuests(out snapshot, out string error)
                == false)
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
                if (visible == false)
                {
                    continue;
                }
                if (pageMode == PageMode.Quest)
                {
                    WIAdministrationTavernQuestSnapshot quest = snapshot.Quests[index];
                    cardImages[index].enabled = false;
                    RectTransform questLabelRect = cardLabels[index].rectTransform;
                    questLabelRect.anchorMin = new Vector2(0.04f, 0.08f);
                    questLabelRect.anchorMax = new Vector2(0.97f, 0.92f);
                    cardLabels[index].text = quest.DisplayName + "\n" + quest.Summary + "\n" + quest.Description;
                    cardButtons[index].interactable = quest.Available;
                }
                else
                {
                    WIAdministrationQuestHeroSnapshot hero = snapshot.Heroes[index];
                    cardImages[index].sprite = hero.Portrait;
                    cardImages[index].enabled = hero.Portrait != null;
                    RectTransform heroLabelRect = cardLabels[index].rectTransform;
                    heroLabelRect.anchorMin = new Vector2(0.23f, 0.08f);
                    heroLabelRect.anchorMax = new Vector2(0.97f, 0.92f);
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
                if (index >= snapshot.Quests.Count)
                {
                    return;
                }
                selectedQuestId = snapshot.Quests[index].QuestId;
                if (selectedQuestId == TalentOfficeCardId)
                {
                    modal.Hide();
                    administrationController.OpenUGUICharacterActivity(true);
                    return;
                }
                if (administrationController.TryGetUGUIQuestHeroes(selectedQuestId, out snapshot, out string error)
                    == false)
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
            if (pageMode != PageMode.Hero || index >= snapshot.Heroes.Count)
            {
                return;
            }
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

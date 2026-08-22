using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectWI.Administration
{
    public sealed class WIAdministrationTerritoryUGUIController : MonoBehaviour
    {
        [SerializeField] private WIAdministrationUIController administrationController;
        [SerializeField] private GameObject contentRoot;
        [SerializeField] private Button backButton;
        [SerializeField] private Button nextTurnButton;
        [SerializeField] private TMP_Text factionLabel;
        [SerializeField] private TMP_Text dateLabel;
        [SerializeField] private TMP_Text goldLabel;
        [SerializeField] private TMP_Text manaLabel;
        [SerializeField] private TMP_Text influenceLabel;
        [SerializeField] private Button[] topCommandButtons;
        [SerializeField] private WIAdministrationShortcutAction[] topCommandActions;
        [SerializeField] private Button systemButton;
        [SerializeField] private TMP_Text castleTitle;
        [SerializeField] private TMP_Text castleInfo;
        [SerializeField] private Image castleBackground;
        [SerializeField] private Image governorPortrait;
        [SerializeField] private TMP_Text governorNameLabel;
        [SerializeField] private TMP_Text prosperityLabel;
        [SerializeField] private TMP_Text technologyLabel;
        [SerializeField] private TMP_Text stabilityLabel;
        [SerializeField] private TMP_Text defenseLabel;
        [SerializeField] private TMP_Text incomeLabel;
        [SerializeField] private TMP_Text projectStatusLabel;
        [SerializeField] private TMP_Text bottomProjectStatusLabel;
        [SerializeField] private GameObject[] heroSlots;
        [SerializeField] private Image[] heroImages;
        [SerializeField] private TMP_Text[] heroCaptions;
        [SerializeField] private GameObject[] facilitySlots;
        [SerializeField] private Image[] facilityImages;
        [SerializeField] private TMP_Text[] facilityCaptions;
        [SerializeField] private Button[] commandButtons;
        [SerializeField] private WIAdministrationTerritoryCommand[] commandActions;
        private Canvas rootCanvas;
        private GraphicRaycaster rootRaycaster;

        // 고정 배치된 영지 UGUI의 버튼을 기존 게임 기능에 연결합니다.
        private void Awake()
        {
            rootCanvas = GetComponent<Canvas>();
            rootRaycaster = GetComponent<GraphicRaycaster>();
            if (administrationController == null)
            {
                administrationController = FindFirstObjectByType<WIAdministrationUIController>();
            }
            if (administrationController == null)
            {
                enabled = false;
                return;
            }

            backButton.onClick.AddListener(administrationController.ReturnToUGUIWorld);
            nextTurnButton.onClick.AddListener(() =>
                administrationController.ExecuteUGUIShortcut(WIAdministrationShortcutAction.EndTurn));
            systemButton.onClick.AddListener(administrationController.OpenUGUISystem);
            int topCommandCount = Mathf.Min(topCommandButtons.Length, topCommandActions.Length);
            for (int index = 0; index < topCommandCount; index += 1)
            {
                WIAdministrationShortcutAction action = topCommandActions[index];
                topCommandButtons[index].onClick.AddListener(() =>
                    administrationController.ExecuteUGUIShortcut(action));
            }

            int count = Mathf.Min(commandButtons.Length, commandActions.Length);
            for (int index = 0; index < count; index += 1)
            {
                WIAdministrationTerritoryCommand action = commandActions[index];
                commandButtons[index].onClick.AddListener(() =>
                    administrationController.ExecuteUGUITerritoryCommand(action));
            }
        }

        // 월드 상태 알림을 구독하고 영지 화면을 갱신합니다.
        private void OnEnable()
        {
            if (administrationController == null)
            {
                administrationController = FindFirstObjectByType<WIAdministrationUIController>();
            }
            if (administrationController != null)
            {
                administrationController.UGUIWorldChanged += Refresh;
                Refresh();
            }
        }

        // 월드 상태 알림 구독을 해제합니다.
        private void OnDisable()
        {
            if (administrationController != null)
            {
                administrationController.UGUIWorldChanged -= Refresh;
            }
        }

        // 선택 성의 스냅샷을 영지 UGUI 요소에 반영합니다.
        private void Refresh()
        {
            if (administrationController.TryGetUGUITerritorySnapshot(out WIAdministrationTerritorySnapshot snapshot) == false)
            {
                contentRoot.SetActive(false);
                SetCanvasVisible(false);
                return;
            }

            contentRoot.SetActive(snapshot.Visible);
            SetCanvasVisible(snapshot.Visible);
            if (snapshot.Visible == false)
            {
                return;
            }

            factionLabel.text = snapshot.FactionName;
            dateLabel.text = snapshot.Date;
            goldLabel.text = snapshot.Gold;
            manaLabel.text = snapshot.Mana;
            influenceLabel.text = snapshot.Influence;
            castleTitle.text = snapshot.CastleTitle;
            castleInfo.text = snapshot.CastleInfo;
            ApplySprite(castleBackground, snapshot.CastleImage);
            ApplySprite(governorPortrait, snapshot.GovernorPortrait);
            governorNameLabel.text = snapshot.GovernorName;
            prosperityLabel.text = snapshot.Prosperity;
            technologyLabel.text = snapshot.Technology;
            stabilityLabel.text = snapshot.Stability;
            defenseLabel.text = snapshot.Defense;
            incomeLabel.text = snapshot.Income;
            projectStatusLabel.text = snapshot.ProjectStatus;
            bottomProjectStatusLabel.text = snapshot.ProjectStatus;
            ApplySlots(snapshot.HeroSlots, heroSlots, heroImages, heroCaptions);
            ApplySlots(snapshot.FacilitySlots, facilitySlots, facilityImages, facilityCaptions);

            for (int index = 0; index < commandButtons.Length; index += 1)
            {
                bool castleRecord = commandActions[index] == WIAdministrationTerritoryCommand.CastleRecord;
                bool specialFacility = commandActions[index] == WIAdministrationTerritoryCommand.ChooseSpecialFacility;
                commandButtons[index].interactable = specialFacility
                    ? snapshot.CanChooseSpecialFacility
                    : snapshot.Manageable || castleRecord;
            }
        }

        // 영지 화면이 숨겨진 동안 Canvas와 입력 레이캐스터가 다른 화면 선택을 가로막지 않게 합니다.
        private void SetCanvasVisible(bool visible)
        {
            if (rootCanvas != null)
            {
                rootCanvas.enabled = visible;
            }
            if (rootRaycaster != null)
            {
                rootRaycaster.enabled = visible;
            }
        }

        // 슬롯 스냅샷을 고정 배치된 이미지와 설명에 적용합니다.
        private static void ApplySlots(System.Collections.Generic.IReadOnlyList<WIAdministrationSlotSnapshot> snapshots,
            GameObject[] slots, Image[] images, TMP_Text[] captions)
        {
            int count = Mathf.Min(slots.Length, snapshots.Count);
            for (int index = 0; index < count; index += 1)
            {
                WIAdministrationSlotSnapshot snapshot = snapshots[index];
                slots[index].SetActive(snapshot.Visible);
                images[index].sprite = snapshot.Image;
                images[index].enabled = snapshot.Occupied && snapshot.Image != null;
                captions[index].text = snapshot.Caption;
            }
        }

        // 이미지 슬롯에 스프라이트가 있는 경우에만 표시합니다.
        private static void ApplySprite(Image image, Sprite sprite)
        {
            image.sprite = sprite;
            image.enabled = sprite != null;
        }
    }
}

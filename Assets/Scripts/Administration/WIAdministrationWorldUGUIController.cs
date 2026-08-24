using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ProjectWI.Administration
{
    public sealed class WIAdministrationWorldUGUIController : MonoBehaviour
    {
        [SerializeField] private WIAdministrationUIController administrationController;
        [SerializeField] private GameObject contentRoot;
        [SerializeField] private TMP_Text factionLabel;
        [SerializeField] private TMP_Text dateLabel;
        [SerializeField] private TMP_Text turnDescriptionLabel;
        [SerializeField] private TMP_Text goldLabel;
        [SerializeField] private TMP_Text manaLabel;
        [SerializeField] private TMP_Text influenceLabel;
        [SerializeField] private TMP_Text castleNameLabel;
        [SerializeField] private TMP_Text castleOwnerLabel;
        [SerializeField] private TMP_Text[] castleDetailRows;
        // 성 상세 행의 오른쪽 값을 표시하는 텍스트 목록입니다.
        [SerializeField] private TMP_Text[] castleDetailValueRows;
        [SerializeField] private Image[] castleHeroPortraits;
        [SerializeField] private TMP_Text[] castleHeroLabels;
        [SerializeField] private Image mapImage;
        [SerializeField] private Image castleImage;
        [SerializeField] private TMP_Text objectiveTitleLabel;
        [SerializeField] private TMP_Text objectiveProgressLabel;
        [SerializeField] private Image objectiveProgressFill;
        [SerializeField] private TMP_Text battleAlertLabel;
        [SerializeField] private Button battleAlertButton;
        [SerializeField] private TMP_Text monthlyNewsLabel;
        [SerializeField] private Button castleManageButton;
        [SerializeField] private Button castleRecordButton;
        [SerializeField] private Button objectiveButton;
        [SerializeField] private Button[] commandButtons;
        [SerializeField] private WIAdministrationShortcutAction[] commandActions;
        [SerializeField] private Button endTurnButton;
        [SerializeField] private TMP_Text endTurnLabel;
        [SerializeField] private Button systemButton;
        private Canvas rootCanvas;
        private GraphicRaycaster rootRaycaster;

        // 고정 배치된 UGUI 월드 화면을 기존 게임 기능과 연결합니다.
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
                Debug.LogError("UGUI 월드 화면에 행정 UI 컨트롤러가 연결되지 않았습니다.");
                enabled = false;
                return;
            }

            castleManageButton.onClick.AddListener(administrationController.OpenUGUIGlobalSummaryCastle);
            castleRecordButton.onClick.AddListener(administrationController.OpenUGUIGlobalSummaryCastle);
            objectiveButton.onClick.AddListener(administrationController.OpenUGUIObjective);
            battleAlertButton.onClick.AddListener(administrationController.OpenUGUIBattleAlert);
            endTurnButton.onClick.AddListener(() => administrationController.ExecuteUGUIShortcut(WIAdministrationShortcutAction.EndTurn));
            systemButton.onClick.AddListener(administrationController.OpenUGUISystem);
            int count = Mathf.Min(commandButtons.Length, commandActions.Length);
            for (int index = 0; index < count; index += 1)
            {
                WIAdministrationShortcutAction action = commandActions[index];
                commandButtons[index].onClick.AddListener(() => administrationController.ExecuteUGUIShortcut(action));
            }
        }

        // 게임 상태 변경 알림을 구독하고 최초 화면을 갱신합니다.
        private void OnEnable()
        {
            if (administrationController == null)
            {
                return;
            }

            administrationController.UGUIWorldChanged += Refresh;
            Refresh();
        }

        // 게임 상태 변경 알림 구독을 해제합니다.
        private void OnDisable()
        {
            if (administrationController != null)
            {
                administrationController.UGUIWorldChanged -= Refresh;
            }
        }

        // UI Toolkit 포커스 없이 UGUI 월드의 전역 키보드 단축키를 처리합니다.
        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.escapeKey.wasPressedThisFrame && WIAdministrationModalUGUIController.TryHideTopmost()) return;
            if (contentRoot.activeInHierarchy == false || WIAdministrationModalUGUIController.AnyVisible) return;

            WIAdministrationShortcutAction action = ResolveKeyboardShortcut(keyboard);
            if (action != WIAdministrationShortcutAction.None) administrationController.ExecuteUGUIShortcut(action);
        }

        // 현재 프레임에 눌린 전역 키를 행정 명령으로 변환합니다.
        private static WIAdministrationShortcutAction ResolveKeyboardShortcut(Keyboard keyboard)
        {
            if (keyboard.mKey.wasPressedThisFrame) return WIAdministrationShortcutAction.Military;
            if (keyboard.hKey.wasPressedThisFrame) return WIAdministrationShortcutAction.Heroes;
            if (keyboard.dKey.wasPressedThisFrame) return WIAdministrationShortcutAction.Diplomacy;
            if (keyboard.sKey.wasPressedThisFrame) return WIAdministrationShortcutAction.Scheme;
            if (keyboard.rKey.wasPressedThisFrame) return WIAdministrationShortcutAction.Research;
            if (keyboard.gKey.wasPressedThisFrame) return WIAdministrationShortcutAction.Faction;
            if (keyboard.cKey.wasPressedThisFrame) return WIAdministrationShortcutAction.Council;
            if (keyboard.lKey.wasPressedThisFrame) return WIAdministrationShortcutAction.MonthlyReport;
            if (keyboard.tKey.wasPressedThisFrame) return WIAdministrationShortcutAction.EndTurn;
            return WIAdministrationShortcutAction.None;
        }

        // 읽기 전용 월드 스냅샷을 UGUI 표시 요소에 반영합니다.
        private void Refresh()
        {
            if (administrationController.TryGetUGUIWorldSnapshot(out WIAdministrationWorldSnapshot snapshot) == false)
            {
                contentRoot.SetActive(false);
                SetCanvasVisible(false);
                return;
            }

            bool visible = snapshot.CampaignStarted && snapshot.WorldVisible;
            contentRoot.SetActive(visible);
            SetCanvasVisible(visible);
            if (visible == false)
            {
                return;
            }

            factionLabel.text = snapshot.FactionName;
            dateLabel.text = snapshot.Date;
            turnDescriptionLabel.text = snapshot.TurnDescription;
            goldLabel.text = snapshot.Gold;
            manaLabel.text = snapshot.Mana;
            influenceLabel.text = snapshot.Influence;
            castleNameLabel.text = snapshot.CastleName;
            castleOwnerLabel.text = snapshot.CastleOwner;
            for (int index = 0; index < castleDetailRows.Length; index += 1)
            {
                castleDetailRows[index].text = index < snapshot.CastleDetailTitles.Count
                    ? snapshot.CastleDetailTitles[index]
                    : string.Empty;
            }
            for (int index = 0; index < castleDetailValueRows.Length; index += 1)
            {
                castleDetailValueRows[index].text = index < snapshot.CastleDetailValues.Count
                    ? snapshot.CastleDetailValues[index]
                    : string.Empty;
            }
            RefreshCastleHeroCards(snapshot);
            mapImage.sprite = snapshot.MapImage;
            mapImage.enabled = snapshot.MapImage != null;
            castleImage.sprite = snapshot.CastleImage;
            castleImage.enabled = snapshot.CastleImage != null;
            objectiveTitleLabel.text = snapshot.ObjectiveTitle;
            objectiveProgressLabel.text = snapshot.ObjectiveProgress;
            objectiveProgressFill.fillAmount = snapshot.ObjectiveProgressNormalized;
            battleAlertLabel.text = snapshot.BattleAlert;
            battleAlertButton.interactable = snapshot.HasBattleAlert;
            monthlyNewsLabel.text = snapshot.MonthlyNews;
            endTurnButton.interactable = snapshot.CanEndTurn;
            endTurnLabel.text = snapshot.EndTurnText;
        }

        // 선택 성에 주둔한 최대 네 명의 영웅 초상과 이름을 카드에 반영합니다.
        private void RefreshCastleHeroCards(WIAdministrationWorldSnapshot snapshot)
        {
            int count = Mathf.Min(castleHeroPortraits.Length, castleHeroLabels.Length);
            for (int index = 0; index < count; index += 1)
            {
                bool occupied = index < snapshot.CastleHeroCards.Count;
                WIAdministrationWorldHeroSnapshot hero = occupied ? snapshot.CastleHeroCards[index] : null;
                castleHeroPortraits[index].sprite = hero?.Portrait;
                castleHeroPortraits[index].enabled = occupied && hero?.Portrait != null;
                castleHeroLabels[index].text = occupied ? $"{hero.LevelText}\n{hero.DisplayName}" : string.Empty;
            }
        }

        // 월드 화면이 숨겨진 동안 Canvas와 입력 레이캐스터가 다른 화면 선택을 가로막지 않게 합니다.
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
    }
}

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
        [SerializeField] private TMP_Text castleStatsLabel;
        [SerializeField] private TMP_Text castleHeroesLabel;
        [SerializeField] private Image mapImage;
        [SerializeField] private Image castleImage;
        [SerializeField] private TMP_Text objectiveTitleLabel;
        [SerializeField] private TMP_Text objectiveProgressLabel;
        [SerializeField] private Image objectiveProgressFill;
        [SerializeField] private TMP_Text battleAlertLabel;
        [SerializeField] private Button battleAlertButton;
        [SerializeField] private TMP_Text monthlyNewsLabel;
        [SerializeField] private Button castleManageButton;
        [SerializeField] private Button objectiveButton;
        [SerializeField] private Button[] commandButtons;
        [SerializeField] private WIAdministrationShortcutAction[] commandActions;
        [SerializeField] private Button endTurnButton;
        [SerializeField] private TMP_Text endTurnLabel;
        [SerializeField] private Button systemButton;

        // 고정 배치된 UGUI 월드 화면을 기존 게임 기능과 연결합니다.
        private void Awake()
        {
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
                return;
            }

            contentRoot.SetActive(snapshot.CampaignStarted && snapshot.WorldVisible);
            if (snapshot.CampaignStarted == false || snapshot.WorldVisible == false)
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
            castleStatsLabel.text = snapshot.CastleStats;
            castleHeroesLabel.text = snapshot.CastleHeroes;
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
    }
}

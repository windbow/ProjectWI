using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectWI.Administration
{
    public sealed class WIAdministrationTopHUDUGUIController : MonoBehaviour
    {
        [SerializeField] private TMP_Text factionLabel;
        [SerializeField] private TMP_Text dateLabel;
        [SerializeField] private TMP_Text turnDescriptionLabel;
        [SerializeField] private TMP_Text goldLabel;
        [SerializeField] private TMP_Text manaLabel;
        [SerializeField] private TMP_Text influenceLabel;
        [SerializeField] private Button monthlyReportButton;
        [SerializeField] private Button councilButton;
        [SerializeField] private Button researchButton;
        [SerializeField] private Button systemButton;

        private WIAdministrationUIController administrationController;

        /// <summary>
        /// 공용 상단 HUD 버튼을 행정 화면 관리자에 한 번 연결합니다.
        /// </summary>
        public void Initialize(WIAdministrationUIController controller)
        {
            if (administrationController == controller)
            {
                return;
            }

            administrationController = controller;
            monthlyReportButton.onClick.AddListener(OpenMonthlyReport);
            councilButton.onClick.AddListener(OpenCouncil);
            researchButton.onClick.AddListener(OpenResearch);
            systemButton.onClick.AddListener(OpenSystem);
        }

        /// <summary>
        /// 현재 진영의 날짜와 자원을 공용 상단 HUD에 표시합니다.
        /// </summary>
        public void Apply(string factionName, string date, string turnDescription, string gold, string mana, string influence)
        {
            factionLabel.text = factionName;
            dateLabel.text = date;
            turnDescriptionLabel.text = turnDescription;
            goldLabel.text = gold;
            manaLabel.text = mana;
            influenceLabel.text = influence;
        }

        /// <summary>
        /// 월간 보고 화면을 엽니다.
        /// </summary>
        private void OpenMonthlyReport()
        {
            administrationController.ExecuteUGUIShortcut(WIAdministrationShortcutAction.MonthlyReport);
        }

        /// <summary>
        /// 의회 화면을 엽니다.
        /// </summary>
        private void OpenCouncil()
        {
            administrationController.ExecuteUGUIShortcut(WIAdministrationShortcutAction.Council);
        }

        /// <summary>
        /// 연구 화면을 엽니다.
        /// </summary>
        private void OpenResearch()
        {
            administrationController.ExecuteUGUIShortcut(WIAdministrationShortcutAction.Research);
        }

        /// <summary>
        /// 시스템 화면을 엽니다.
        /// </summary>
        private void OpenSystem()
        {
            administrationController.OpenUGUISystem();
        }
    }
}

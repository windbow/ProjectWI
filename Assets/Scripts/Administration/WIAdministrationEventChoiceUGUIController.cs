using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectWI.Administration
{
    public sealed class WIAdministrationEventChoiceUGUIController : MonoBehaviour
    {
        [SerializeField] private WIAdministrationUIController administrationController;
        [SerializeField] private WIAdministrationModalUGUIController modal;
        [SerializeField] private TMP_Text descriptionLabel;
        [SerializeField] private TMP_Text messageLabel;
        [SerializeField] private Button[] choiceButtons;
        [SerializeField] private TMP_Text[] choiceLabels;

        private WIAdministrationReportActionType actionType;
        private string actionId;

        // 고정 선택 카드에 각 배열 위치의 사건 처리 함수를 연결합니다.
        private void Awake()
        {
            if (administrationController == null) administrationController = FindFirstObjectByType<WIAdministrationUIController>();
            for (int index = 0; index < choiceButtons.Length; index += 1)
            {
                int capturedIndex = index;
                choiceButtons[index].onClick.AddListener(() => Resolve(capturedIndex));
            }
        }

        // 선택 사건 UGUI 열기 요청을 구독합니다.
        private void OnEnable()
        {
            if (administrationController == null) administrationController = FindFirstObjectByType<WIAdministrationUIController>();
            if (administrationController != null) administrationController.UGUIEventChoiceRequested += Open;
        }

        // 선택 사건 UGUI 열기 요청 구독을 해제합니다.
        private void OnDisable()
        {
            if (administrationController != null) administrationController.UGUIEventChoiceRequested -= Open;
        }

        // 요청한 월간 사건의 설명과 선택지를 공통 고정 카드에 표시합니다.
        private void Open(WIAdministrationReportActionType type, string id)
        {
            actionType = type;
            actionId = id;
            if (administrationController.TryGetUGUIEventChoiceSnapshot(type, id, out WIAdministrationEventChoiceSnapshot snapshot) == false) return;
            modal.Show(snapshot.Title);
            descriptionLabel.text = snapshot.Description;
            messageLabel.gameObject.SetActive(snapshot.Choices.Count == 0);
            messageLabel.text = snapshot.Choices.Count == 0 ? "선택 가능한 결과가 없습니다." : string.Empty;
            for (int index = 0; index < choiceButtons.Length; index += 1)
            {
                bool visible = index < snapshot.Choices.Count;
                choiceButtons[index].gameObject.SetActive(visible);
                if (visible == false) continue;
                WIAdministrationEventChoiceCardSnapshot choice = snapshot.Choices[index];
                choiceLabels[index].text = choice.Label;
                choiceButtons[index].interactable = choice.Interactable;
            }
        }

        // 선택한 결과를 기존 사건 판정 시스템에 적용하고 모달을 닫습니다.
        private void Resolve(int choiceIndex)
        {
            if (administrationController.ResolveUGUIEventChoice(actionType, actionId, choiceIndex) == false) return;
            modal.Hide();
        }
    }
}

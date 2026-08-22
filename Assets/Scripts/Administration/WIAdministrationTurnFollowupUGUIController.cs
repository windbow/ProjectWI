using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectWI.Administration
{
    public sealed class WIAdministrationTurnFollowupUGUIController : MonoBehaviour
    {
        [SerializeField] private WIAdministrationUIController administrationController;
        [SerializeField] private WIAdministrationModalUGUIController modal;
        [SerializeField] private TMP_Text descriptionLabel;
        [SerializeField] private TMP_Text messageLabel;
        [SerializeField] private GameObject tutorialContent;
        [SerializeField] private Button[] choiceButtons;
        [SerializeField] private TMP_Text[] choiceLabels;
        [SerializeField] private TMP_Text[] choiceDescriptionLabels;

        // 고정 선택 카드에 턴 후속 처리 함수를 연결합니다.
        private void Awake()
        {
            if (administrationController == null) administrationController = FindFirstObjectByType<WIAdministrationUIController>();
            for (int index = 0; index < choiceButtons.Length; index += 1)
            {
                int capturedIndex = index;
                choiceButtons[index].onClick.AddListener(() => Resolve(capturedIndex));
            }
        }

        // 턴 후속 UGUI 표시와 숨김 요청을 구독합니다.
        private void OnEnable()
        {
            if (administrationController == null) administrationController = FindFirstObjectByType<WIAdministrationUIController>();
            if (administrationController == null) return;
            administrationController.UGUITurnFollowupRequested += Open;
            administrationController.UGUITurnFollowupHideRequested += modal.Hide;
        }

        // 턴 후속 UGUI 요청 구독을 해제합니다.
        private void OnDisable()
        {
            if (administrationController == null) return;
            administrationController.UGUITurnFollowupRequested -= Open;
            administrationController.UGUITurnFollowupHideRequested -= modal.Hide;
        }

        // 연산 안내, 캠페인 결과 또는 튜토리얼 스냅샷을 표시합니다.
        private void Open()
        {
            if (administrationController.TryGetUGUITurnFollowupSnapshot(out WIAdministrationTurnFollowupSnapshot snapshot) == false) return;
            modal.Show(snapshot.Title);
            descriptionLabel.text = snapshot.Description;
            tutorialContent.SetActive(snapshot.Mode == WIAdministrationTurnFollowupMode.Tutorial);
            messageLabel.gameObject.SetActive(snapshot.Mode == WIAdministrationTurnFollowupMode.Processing);
            messageLabel.text = snapshot.Mode == WIAdministrationTurnFollowupMode.Processing ? "월간 결과를 계산하고 있습니다." : string.Empty;
            for (int index = 0; index < choiceButtons.Length; index += 1)
            {
                bool visible = index < snapshot.Choices.Count;
                choiceButtons[index].gameObject.SetActive(visible);
                if (visible)
                {
                    string choice = snapshot.Choices[index];
                    int lineBreak = choice.IndexOf('\n');
                    choiceLabels[index].text = lineBreak >= 0 ? choice[..lineBreak] : choice;
                    choiceDescriptionLabels[index].text = lineBreak >= 0 ? choice[(lineBreak + 1)..] : string.Empty;
                    choiceDescriptionLabels[index].gameObject.SetActive(lineBreak >= 0);
                }
                else
                {
                    choiceDescriptionLabels[index].gameObject.SetActive(false);
                }
            }
        }

        // 캠페인 결과 또는 튜토리얼의 선택 결과를 기존 상태에 적용합니다.
        private void Resolve(int choiceIndex)
        {
            if (administrationController.ResolveUGUITurnFollowup(choiceIndex) == false) return;
            modal.Hide();
        }
    }
}

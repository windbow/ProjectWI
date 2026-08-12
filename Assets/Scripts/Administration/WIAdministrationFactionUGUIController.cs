using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectWI.Administration
{
    public sealed class WIAdministrationFactionUGUIController : MonoBehaviour
    {
        [SerializeField] private WIAdministrationUIController administrationController;
        [SerializeField] private WIAdministrationModalUGUIController modal;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private TMP_Text messageLabel;
        [SerializeField] private Button[] cardButtons;
        [SerializeField] private TMP_Text[] cardLabels;

        // 읽기 전용 진영 카드가 입력을 가로채지 않도록 비활성화합니다.
        private void Awake()
        {
            if (administrationController == null) administrationController = FindFirstObjectByType<WIAdministrationUIController>();
            foreach (Button button in cardButtons) button.interactable = false;
        }

        // 진영 정세 UGUI 열기 요청을 구독합니다.
        private void OnEnable()
        {
            if (administrationController == null) administrationController = FindFirstObjectByType<WIAdministrationUIController>();
            if (administrationController != null) administrationController.UGUIFactionRequested += Open;
        }

        // 진영 정세 UGUI 열기 요청 구독을 해제합니다.
        private void OnDisable()
        {
            if (administrationController != null) administrationController.UGUIFactionRequested -= Open;
        }

        // 현재 대륙의 진영 정세 스냅샷을 고정 카드로 표시합니다.
        private void Open()
        {
            modal.Show("대륙 진영 정세");
            if (administrationController.TryGetUGUIFactionSnapshot(out WIAdministrationFactionSnapshot snapshot) == false)
            {
                messageLabel.gameObject.SetActive(true);
                messageLabel.text = "진영 정보를 불러올 수 없습니다.";
                return;
            }
            statusLabel.text = snapshot.Summary;
            messageLabel.gameObject.SetActive(snapshot.Cards.Count == 0);
            messageLabel.text = snapshot.Cards.Count == 0 ? "표시할 진영이 없습니다." : string.Empty;
            for (int index = 0; index < cardButtons.Length; index += 1)
            {
                bool visible = index < snapshot.Cards.Count;
                cardButtons[index].gameObject.SetActive(visible);
                if (visible == false) continue;
                WIAdministrationFactionCardSnapshot card = snapshot.Cards[index];
                cardLabels[index].text = card.Title + "\n" + card.Description;
            }
        }
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectWI.Administration
{
    public sealed class WIAdministrationCouncilUGUIController : MonoBehaviour
    {
        [SerializeField] private WIAdministrationUIController administrationController;
        [SerializeField] private WIAdministrationModalUGUIController modal;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private TMP_Text messageLabel;
        [SerializeField] private Button[] cardButtons;
        [SerializeField] private TMP_Text[] cardLabels;

        // 고정 방침 카드에 각 배열 위치의 선택 처리를 연결합니다.
        private void Awake()
        {
            if (administrationController == null) administrationController = FindFirstObjectByType<WIAdministrationUIController>();
            for (int index = 0; index < cardButtons.Length; index += 1)
            {
                int capturedIndex = index;
                cardButtons[index].onClick.AddListener(() => SelectPolicy(capturedIndex));
            }
        }

        // 의회 UGUI 열기 요청을 구독합니다.
        private void OnEnable()
        {
            if (administrationController == null) administrationController = FindFirstObjectByType<WIAdministrationUIController>();
            if (administrationController != null) administrationController.UGUICouncilRequested += Open;
        }

        // 의회 UGUI 열기 요청 구독을 해제합니다.
        private void OnDisable()
        {
            if (administrationController != null) administrationController.UGUICouncilRequested -= Open;
        }

        // 현재 월간 방침과 선택 가능한 방침을 표시합니다.
        private void Open()
        {
            modal.Show("이번 달 진영 방침");
            Refresh();
        }

        // 최신 진영 방침 스냅샷으로 고정 카드를 갱신합니다.
        private void Refresh()
        {
            if (administrationController.TryGetUGUICouncilSnapshot(out WIAdministrationCouncilSnapshot snapshot) == false)
            {
                messageLabel.gameObject.SetActive(true);
                messageLabel.text = "진영 방침 정보를 불러올 수 없습니다.";
                return;
            }

            statusLabel.text = snapshot.Summary;
            messageLabel.gameObject.SetActive(snapshot.Cards.Count == 0);
            messageLabel.text = snapshot.Cards.Count == 0 ? "선택 가능한 방침이 없습니다." : string.Empty;
            for (int index = 0; index < cardButtons.Length; index += 1)
            {
                bool visible = index < snapshot.Cards.Count;
                cardButtons[index].gameObject.SetActive(visible);
                if (visible == false) continue;
                WIAdministrationCouncilCardSnapshot card = snapshot.Cards[index];
                cardLabels[index].text = (card.Selected ? "● " : string.Empty) + card.Title + "\n" + card.Description;
                cardButtons[index].interactable = card.Selected == false;
            }
        }

        // 선택한 카드의 월간 진영 방침을 기존 캠페인 상태에 반영합니다.
        private void SelectPolicy(int index)
        {
            if (administrationController.TryGetUGUICouncilSnapshot(out WIAdministrationCouncilSnapshot snapshot) == false || index >= snapshot.Cards.Count) return;
            if (administrationController.SetUGUICouncilPolicy(snapshot.Cards[index].Policy) == false) return;
            modal.Hide();
        }
    }
}

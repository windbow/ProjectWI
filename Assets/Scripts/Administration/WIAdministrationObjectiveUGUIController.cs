using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectWI.Administration
{
    public sealed class WIAdministrationObjectiveUGUIController : MonoBehaviour
    {
        [SerializeField] private WIAdministrationUIController administrationController;
        [SerializeField] private WIAdministrationModalUGUIController modal;
        [SerializeField] private TMP_Text situationLabel;
        [SerializeField] private TMP_Text descriptionLabel;
        [SerializeField] private TMP_Text progressLabel;
        [SerializeField] private Button confirmButton;

        // 목표 확인 버튼을 공통 모달 닫기에 연결합니다.
        private void Awake()
        {
            if (administrationController == null) administrationController = FindFirstObjectByType<WIAdministrationUIController>();
            confirmButton.onClick.AddListener(modal.Hide);
            modal.Closed += HandleClosed;
        }

        // 캠페인 목표 UGUI 열기 요청을 구독합니다.
        private void OnEnable()
        {
            if (administrationController == null) administrationController = FindFirstObjectByType<WIAdministrationUIController>();
            if (administrationController != null) administrationController.UGUIObjectiveRequested += Open;
        }

        // 캠페인 목표 UGUI 열기 요청 구독을 해제합니다.
        private void OnDisable()
        {
            if (administrationController != null) administrationController.UGUIObjectiveRequested -= Open;
        }

        // 목표 프리팹이 제거될 때 공통 모달 종료 구독을 해제합니다.
        private void OnDestroy()
        {
            modal.Closed -= HandleClosed;
        }

        // 캠페인 시작 직후 목표를 닫았으면 예약된 튜토리얼 흐름을 이어갑니다.
        private void HandleClosed()
        {
            administrationController.ContinueAfterUGUIObjective();
        }

        // 현재 캠페인 목표의 정세·조건·진행도·보상을 표시합니다.
        private void Open()
        {
            if (administrationController.TryGetUGUIObjectiveSnapshot(out WIAdministrationObjectiveSnapshot snapshot) == false)
            {
                return;
            }
            modal.Show(snapshot.Title);
            situationLabel.text = snapshot.Situation;
            descriptionLabel.text = snapshot.Description;
            progressLabel.text = snapshot.Progress;
        }
    }
}

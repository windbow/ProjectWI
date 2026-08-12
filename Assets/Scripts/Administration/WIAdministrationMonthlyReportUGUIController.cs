using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectWI.Administration
{
    public sealed class WIAdministrationMonthlyReportUGUIController : MonoBehaviour
    {
        private const int PageSize = 6;
        [SerializeField] private WIAdministrationUIController administrationController;
        [SerializeField] private WIAdministrationModalUGUIController modal;
        [SerializeField] private TMP_Text bodyLabel;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private Button[] actionButtons;
        [SerializeField] private Button previousButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private TMP_Text pageLabel;

        private WIAdministrationMonthlyReportSnapshot snapshot;
        private int pageIndex;
        private bool closingForAction;

        // 고정 행동 버튼과 페이지 이동을 월간 보고 기능에 연결합니다.
        private void Awake()
        {
            if (administrationController == null) administrationController = FindFirstObjectByType<WIAdministrationUIController>();
            for (int index = 0; index < actionButtons.Length; index += 1)
            {
                int captured = index;
                actionButtons[index].onClick.AddListener(() => Execute(captured));
            }
            previousButton.onClick.AddListener(() => ChangePage(-1));
            nextButton.onClick.AddListener(() => ChangePage(1));
            modal.Closed += HandleClosed;
        }

        // 월간 보고 UGUI 열기 요청을 구독합니다.
        private void OnEnable()
        {
            if (administrationController == null) administrationController = FindFirstObjectByType<WIAdministrationUIController>();
            if (administrationController != null) administrationController.UGUIMonthlyReportRequested += Open;
        }

        // 월간 보고 UGUI 열기 요청 구독을 해제합니다.
        private void OnDisable()
        {
            if (administrationController != null) administrationController.UGUIMonthlyReportRequested -= Open;
        }

        // 월간 보고 프리팹이 제거될 때 공통 모달 종료 구독을 해제합니다.
        private void OnDestroy()
        {
            modal.Closed -= HandleClosed;
        }

        // 지난달 보고 본문과 처리할 사건·전투 행동을 표시합니다.
        private void Open()
        {
            if (administrationController.TryGetUGUIMonthlyReportSnapshot(out snapshot) == false) return;
            modal.Show("지난달 월간 보고");
            closingForAction = false;
            bodyLabel.text = snapshot.Body;
            scrollRect.verticalNormalizedPosition = 1f;
            pageIndex = 0;
            RefreshActions();
        }

        // 현재 행동 페이지를 고정된 6개 버튼에 반영합니다.
        private void RefreshActions()
        {
            int pageCount = Mathf.Max(1, Mathf.CeilToInt(snapshot.Actions.Count / (float)PageSize));
            pageIndex = Mathf.Clamp(pageIndex, 0, pageCount - 1);
            for (int index = 0; index < actionButtons.Length; index += 1)
            {
                int actionIndex = pageIndex * PageSize + index;
                bool visible = actionIndex < snapshot.Actions.Count;
                actionButtons[index].gameObject.SetActive(visible);
                if (visible == false) continue;
                WIAdministrationReportActionSnapshot action = snapshot.Actions[actionIndex];
                actionButtons[index].GetComponentInChildren<TMP_Text>().text = action.Caption;
                actionButtons[index].GetComponent<Image>().color = action.Danger
                    ? new Color32(150, 52, 52, 255) : Color.white;
            }
            previousButton.interactable = pageIndex > 0;
            nextButton.interactable = pageIndex + 1 < pageCount;
            pageLabel.text = snapshot.Actions.Count == 0 ? "처리할 사건 없음" : $"{pageIndex + 1} / {pageCount}";
        }

        // 선택한 사건 또는 전투를 기존 처리 흐름으로 전달합니다.
        private void Execute(int buttonIndex)
        {
            int actionIndex = pageIndex * PageSize + buttonIndex;
            if (actionIndex >= snapshot.Actions.Count) return;
            closingForAction = true;
            modal.Hide();
            administrationController.ExecuteUGUIReportAction(snapshot.Actions[actionIndex].Type, snapshot.Actions[actionIndex].Id);
        }

        // 사건 진입이 아닌 정상 닫기일 때 턴 결과의 캠페인 결과·튜토리얼 흐름을 이어갑니다.
        private void HandleClosed()
        {
            if (closingForAction)
            {
                closingForAction = false;
                return;
            }
            administrationController.ContinueAfterUGUITurnReport();
        }

        // 행동 버튼 페이지를 변경합니다.
        private void ChangePage(int direction) { pageIndex += direction; RefreshActions(); }
    }
}

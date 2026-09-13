using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectWI.Administration
{
    public sealed class WIAdministrationMonthlyReportUGUIController : WIAdministrationUGUIPanelController
    {
        private const int PageSize = 3;
        [SerializeField] private WIAdministrationModalUGUIController modal;
        [SerializeField] private TMP_Text[] summaryLabels;
        [SerializeField] private TMP_Text[] resourceLabels;
        [SerializeField] private GameObject[] operationCards;
        [SerializeField] private TMP_Text[] operationLabels;
        [SerializeField] private GameObject[] newsCards;
        [SerializeField] private TMP_Text[] newsLabels;
        [SerializeField] private Button[] actionButtons;
        [SerializeField] private TMP_Text[] actionCategoryLabels;
        [SerializeField] private TMP_Text[] actionTitleLabels;
        [SerializeField] private TMP_Text[] actionDetailLabels;
        [SerializeField] private TMP_Text[] actionUrgencyLabels;
        [SerializeField] private Image[] actionThumbnails;
        [SerializeField] private Sprite occupationSprite;
        [SerializeField] private Sprite recruitmentSprite;
        [SerializeField] private Sprite relationshipSprite;
        [SerializeField] private Sprite militarySprite;
        [SerializeField] private Sprite defaultActionSprite;
        [SerializeField] private Button[] filterButtons;
        [SerializeField] private TMP_Text[] filterLabels;
        [SerializeField] private Sprite filterNormalSprite;
        [SerializeField] private Sprite filterSelectedSprite;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button previousButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private TMP_Text pageLabel;

        private WIAdministrationMonthlyReportSnapshot snapshot;
        private int pageIndex;
        private bool closingForAction;
        private string currentFilter = "전체";

        // 고정 행동 버튼과 페이지 이동을 월간 보고 기능에 연결합니다.
        private void Awake()
        {
            ResolveAdministrationController();
            for (int index = 0; index < actionButtons.Length; index += 1)
            {
                int captured = index;
                actionButtons[index].onClick.AddListener(() => Execute(captured));
            }
            previousButton.onClick.AddListener(() => ChangePage(-1));
            nextButton.onClick.AddListener(() => ChangePage(1));
            confirmButton.onClick.AddListener(modal.Hide);
            for (int index = 0; index < filterButtons.Length; index += 1)
            {
                int captured = index;
                filterButtons[index].onClick.AddListener(() => ApplyFilter(captured));
            }
            modal.Closed += HandleClosed;
        }

        // 월간 보고 UGUI 열기 요청을 구독합니다.
        private void OnEnable()
        {
            ResolveAdministrationController();
            if (administrationController != null)
            {
                administrationController.UGUIMonthlyReportRequested += Open;
            }
        }

        // 월간 보고 UGUI 열기 요청 구독을 해제합니다.
        private void OnDisable()
        {
            if (administrationController != null)
            {
                administrationController.UGUIMonthlyReportRequested -= Open;
            }
        }

        // 월간 보고 프리팹이 제거될 때 공통 모달 종료 구독을 해제합니다.
        private void OnDestroy()
        {
            modal.Closed -= HandleClosed;
        }

        // 지난달 보고를 요약·운영·소식·결정 카드에 나누어 표시합니다.
        private void Open()
        {
            if (administrationController.TryGetUGUIMonthlyReportSnapshot(out snapshot) == false)
            {
                return;
            }
            modal.Show(snapshot.Title);
            closingForAction = false;
            SetLabels(summaryLabels, new[] { snapshot.TerritorySummary, snapshot.CharacterSummary,
                snapshot.ArmySummary, snapshot.StabilitySummary, snapshot.ResearchSummary });
            SetLabels(resourceLabels, new[] { snapshot.GoldSummary, snapshot.ManaSummary, snapshot.InfluenceSummary });
            RefreshTextCards(operationCards, operationLabels, snapshot.Operations, "이번 달 위임 결과가 없습니다.");
            RefreshTextCards(newsCards, newsLabels, snapshot.News, "새로운 주요 소식이 없습니다.");
            pageIndex = 0;
            currentFilter = "전체";
            RefreshFilters();
            RefreshActions();
        }

        // 고정 텍스트 슬롯에 전달된 문구를 순서대로 반영합니다.
        private static void SetLabels(TMP_Text[] labels, string[] values)
        {
            for (int index = 0; index < labels.Length; index += 1)
            {
                labels[index].text = index < values.Length ? values[index] : string.Empty;
            }
        }

        // 고정 카드 슬롯을 실제 항목 수에 맞춰 표시합니다.
        private static void RefreshTextCards(GameObject[] cards, TMP_Text[] labels,
            System.Collections.Generic.List<string> values, string emptyMessage)
        {
            for (int index = 0; index < cards.Length; index += 1)
            {
                bool visible = index < values.Count || (values.Count == 0 && index == 0);
                cards[index].SetActive(visible);
                if (visible)
                {
                    labels[index].text = values.Count == 0 ? emptyMessage : values[index];
                }
            }
        }

        // 현재 행동 페이지를 고정된 3개 버튼에 반영합니다.
        private void RefreshActions()
        {
            System.Collections.Generic.List<WIAdministrationReportActionSnapshot> visibleActions = GetVisibleActions();
            int pageCount = Mathf.Max(1, Mathf.CeilToInt(visibleActions.Count / (float)PageSize));
            pageIndex = Mathf.Clamp(pageIndex, 0, pageCount - 1);
            for (int index = 0; index < actionButtons.Length; index += 1)
            {
                int actionIndex = pageIndex * PageSize + index;
                bool visible = actionIndex < visibleActions.Count;
                actionButtons[index].gameObject.SetActive(visible);
                if (visible == false)
                {
                    continue;
                }
                WIAdministrationReportActionSnapshot action = visibleActions[actionIndex];
                actionCategoryLabels[index].text = action.Category;
                actionTitleLabels[index].text = action.Caption;
                actionDetailLabels[index].text = action.Detail;
                actionUrgencyLabels[index].text = action.Danger ? "! 긴급" : "! 확인";
                actionUrgencyLabels[index].color = action.Danger
                    ? new Color32(232, 91, 72, 255) : new Color32(225, 184, 73, 255);
                actionThumbnails[index].sprite = GetActionSprite(action.Type);
            }
            previousButton.interactable = pageIndex > 0;
            nextButton.interactable = pageIndex + 1 < pageCount;
            previousButton.gameObject.SetActive(pageCount > 1);
            nextButton.gameObject.SetActive(pageCount > 1);
            pageLabel.text = visibleActions.Count == 0 ? "해당 결정 없음" : $"{pageIndex + 1} / {pageCount}";
        }

        // 현재 분류에 해당하는 결정 카드만 반환합니다.
        private System.Collections.Generic.List<WIAdministrationReportActionSnapshot> GetVisibleActions()
        {
            if (currentFilter == "전체")
            {
                return snapshot.Actions;
            }
            return snapshot.Actions.FindAll(action => action.Category == currentFilter);
        }

        // 선택한 하단 필터를 적용하고 결정 카드 첫 페이지를 표시합니다.
        private void ApplyFilter(int index)
        {
            currentFilter = filterLabels[index].text;
            pageIndex = 0;
            RefreshFilters();
            RefreshActions();
        }

        // 선택 상태에 맞춰 하단 필터 Sprite와 글자색을 갱신합니다.
        private void RefreshFilters()
        {
            for (int index = 0; index < filterButtons.Length; index += 1)
            {
                bool selected = filterLabels[index].text == currentFilter;
                filterButtons[index].GetComponent<Image>().sprite = selected ? filterSelectedSprite : filterNormalSprite;
                filterLabels[index].color = selected ? Color.white : new Color32(175, 181, 186, 255);
            }
        }

        // 선택 사건 종류에 맞는 카드 삽화를 반환합니다.
        private Sprite GetActionSprite(WIAdministrationReportActionType type)
        {
            if (type == WIAdministrationReportActionType.OccupationEvent)
            {
                return occupationSprite;
            }
            if (type == WIAdministrationReportActionType.RecruitmentEvent)
            {
                return recruitmentSprite;
            }
            if (type == WIAdministrationReportActionType.RelationshipEvent)
            {
                return relationshipSprite;
            }
            if (type == WIAdministrationReportActionType.Battle)
            {
                return militarySprite;
            }
            return defaultActionSprite;
        }

        // 선택한 사건 또는 전투를 기존 처리 흐름으로 전달합니다.
        private void Execute(int buttonIndex)
        {
            System.Collections.Generic.List<WIAdministrationReportActionSnapshot> visibleActions = GetVisibleActions();
            int actionIndex = pageIndex * PageSize + buttonIndex;
            if (actionIndex >= visibleActions.Count)
            {
                return;
            }
            closingForAction = true;
            modal.Hide();
            administrationController.ExecuteUGUIReportAction(visibleActions[actionIndex].Type, visibleActions[actionIndex].Id);
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

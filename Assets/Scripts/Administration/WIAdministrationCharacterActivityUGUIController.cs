using TMPro;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectWI.Administration
{
    public sealed class WIAdministrationCharacterActivityUGUIController : WIAdministrationUGUIPanelController
    {
        private const int PageSize = 8;

        private enum PageMode { Actor, Activity, Target }
        private enum GradeFilter { All, Hero, Common }
        // 모든 후보를 스크롤 행으로 표시하는 공통 목록입니다.
        [SerializeField] private WICharacterSelectionList selectionList;
        [SerializeField] private WIAdministrationModalUGUIController modal;
        [SerializeField] private TMP_Text contextLabel;
        [SerializeField] private TMP_Text messageLabel;
        [SerializeField] private GameObject cardRoot;
        [SerializeField] private Button[] cardButtons;
        [SerializeField] private Image[] cardPortraits;
        [SerializeField] private TMP_Text[] cardLabels;
        [SerializeField] private GameObject activityRoot;
        [SerializeField] private Button[] activityButtons;
        [SerializeField] private Button previousButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private TMP_Text pageLabel;
        [SerializeField] private GameObject toolbarRoot;
        [SerializeField] private TMP_InputField searchInput;
        [SerializeField] private Button[] filterButtons;
        [SerializeField] private TMP_Text[] filterLabels;
        [SerializeField] private Button sortButton;
        [SerializeField] private TMP_Text sortLabel;
        [SerializeField] private Sprite filterNormalSprite;
        [SerializeField] private Sprite filterSelectedSprite;

        // 활동 페이지에 저장된 유지 설정 버튼입니다.
        [SerializeField] private Button repeatButton;

        private WIAdministrationCharacterActivitySnapshot snapshot;
        private PageMode pageMode;
        private WICharacterActivityType pendingActivity;
        private string selectedHeroId;
        // 대상 선택에서도 유지할 활동 담당자의 표시 이름입니다.
        private string selectedHeroName;
        private int pageIndex;
        private GradeFilter gradeFilter;
        private bool sortByName;
        private readonly List<int> visibleCandidateIndices = new List<int>();


        // 고정 카드와 활동 버튼을 인재실 단계에 연결합니다.
        private void Awake()
        {
            ResolveAdministrationController();
            for (int index = 0; index < cardButtons.Length; index += 1)
            {
                int captured = index;
                cardButtons[index].onClick.AddListener(() => SelectCard(captured));
            }
            WICharacterActivityType[] activities = { WICharacterActivityType.Search, WICharacterActivityType.Recruit };
            for (int index = 0; index < activityButtons.Length && index < activities.Length; index += 1)
            {
                WICharacterActivityType captured = activities[index];
                activityButtons[index].onClick.AddListener(() => SelectActivity(captured));
            }

            if (repeatButton != null)
            {
                repeatButton.onClick.AddListener(ToggleRepeatActivity);
            }
            previousButton.onClick.AddListener(() => ChangePage(-1));
            nextButton.onClick.AddListener(() => ChangePage(1));
            if (searchInput != null)
            {
                searchInput.onValueChanged.AddListener(_ => ApplyCandidateViewOptions());
            }
            for (int index = 0; index < filterButtons.Length; index += 1)
            {
                int captured = index;
                filterButtons[index].onClick.AddListener(() => SetGradeFilter(captured));
            }
            if (sortButton != null)
            {
                sortButton.onClick.AddListener(ToggleSort);
            }
        }

        // 인재실 UGUI 열기 요청을 구독합니다.
        private void OnEnable()
        {
            ResolveAdministrationController();
            if (administrationController != null)
            {
                administrationController.UGUICharacterActivityRequested += Open;
            }
        }

        // 인재실 UGUI 열기 요청 구독을 해제합니다.
        private void OnDisable()
        {
            if (administrationController != null)
            {
                administrationController.UGUICharacterActivityRequested -= Open;
            }
        }

        // 현재 성에서 활동 가능한 인물 선택 화면을 엽니다.
        private void Open()
        {
            modal.Show(administrationController.GetAdministrationText("UI_TALENT_OFFICE_ACTORS"));
            pageMode = PageMode.Actor;
            pageIndex = 0;
            gradeFilter = GradeFilter.All;
            sortByName = false;
            if (searchInput != null)
            {
                searchInput.SetTextWithoutNotify(string.Empty);
            }
            if (administrationController.TryGetUGUICharacterActivityActors(out snapshot, out string error)
                == false)
            {
                ShowError(error);
                return;
            }
            snapshot.Candidates.RemoveAll(candidate => administrationController.CanSelectCharacterActivity(
                candidate.HeroId, WICharacterActivityType.Search) == false);
            if (snapshot.Candidates.Count == 0)
            {
                ShowError(administrationController.GetAdministrationText("UI_TALENT_OFFICE_NO_ACTOR"));
                return;
            }
            contextLabel.text = snapshot.CastleName + " · " + administrationController.GetAdministrationText("UI_TALENT_OFFICE_CONTEXT");
            messageLabel.gameObject.SetActive(false);
            RefreshToolbar();
            RefreshCards();
        }

        // 현재 단계의 후보를 고정된 카드에 표시합니다.
        private void RefreshCards()
        {
            cardRoot.SetActive(true);
            activityRoot.SetActive(false);
            RebuildVisibleCandidateIndices();
            selectionList.Prepare(visibleCandidateIndices.Count, SelectCard, administrationController,
                out cardButtons, out cardLabels, out cardPortraits);
            int count = visibleCandidateIndices.Count;
            int pageCount = 1;
            pageIndex = Mathf.Clamp(pageIndex, 0, pageCount - 1);
            for (int index = 0; index < cardButtons.Length; index += 1)
            {
                int visibleIndex = pageIndex * PageSize + index;
                bool visible = visibleIndex < count;
                cardButtons[index].gameObject.SetActive(visible);
                if (visible == false)
                {
                    continue;
                }
                int candidateIndex = visibleCandidateIndices[visibleIndex];
                WIAdministrationCharacterActivityCandidateSnapshot candidate = snapshot.Candidates[candidateIndex];
                cardPortraits[index].sprite = candidate.Portrait;
                cardPortraits[index].enabled = candidate.Portrait != null;
                string status = candidate.Interactable ? "대기" : "조건 미달";
                cardLabels[index].text = $"<size=115%>{candidate.DisplayName}</size>\n{candidate.ClassName} · {status}\n<size=88%>{candidate.Summary}</size>";
                cardButtons[index].interactable = candidate.Interactable;
            }
            previousButton.interactable = pageIndex > 0;
            nextButton.interactable = pageIndex + 1 < pageCount;
            previousButton.gameObject.SetActive(pageCount > 1);
            nextButton.gameObject.SetActive(pageCount > 1);
            pageLabel.gameObject.SetActive(pageCount > 1);
            pageLabel.text = $"{pageIndex + 1} / {pageCount}";
            RefreshToolbar();
        }

        // 선택한 인물의 활동 종류 선택 화면으로 전환합니다.
        private void SelectCard(int cardIndex)
        {
            int visibleIndex = pageIndex * PageSize + cardIndex;
            if (snapshot == null || visibleIndex >= visibleCandidateIndices.Count)
            {
                return;
            }
            int candidateIndex = visibleCandidateIndices[visibleIndex];
            WIAdministrationCharacterActivityCandidateSnapshot candidate = snapshot.Candidates[candidateIndex];
            if (pageMode == PageMode.Actor)
            {
                selectedHeroId = candidate.HeroId;
                selectedHeroName = candidate.DisplayName;
                pageMode = PageMode.Activity;
                modal.SetTitle(string.Format(administrationController.GetAdministrationText("UI_ACTIVITY_ACTOR_TITLE"), selectedHeroName));
                contextLabel.text = administrationController.GetAdministrationText("UI_ACTIVITY_REPEAT_HINT");
                WICharacterActivityType[] activities = { WICharacterActivityType.Search, WICharacterActivityType.Recruit };
                for (int index = 0; index < activities.Length && index < activityButtons.Length; index += 1)
                {
                    bool visible = true;
                    activityButtons[index].gameObject.SetActive(visible);
                    activityButtons[index].interactable = administrationController.CanSelectCharacterActivity(selectedHeroId, activities[index]);
                }

                RefreshRepeatActivity();
                cardRoot.SetActive(false);
                activityRoot.SetActive(true);
                ArrangeVisibleActivities();
                if (toolbarRoot != null)
                {
                    toolbarRoot.SetActive(false);
                }
                previousButton.interactable = false;
                nextButton.interactable = false;
                pageLabel.text = string.Empty;
                return;
            }

            Assign(pendingActivity, candidate.HeroId);
        }

        // 선택한 인물의 반복 지시 설정을 전환합니다.
        private void ToggleRepeatActivity()
        {
            administrationController.ToggleStandingOrder(true, selectedHeroId);
            RefreshRepeatActivity();
        }

        // 고정 유지 버튼에 선택 인물의 설정을 표시합니다.
        private void RefreshRepeatActivity()
        {
            if (repeatButton != null)
            {
                repeatButton.GetComponentInChildren<TMP_Text>().text = administrationController.GetStandingOrderText(true, selectedHeroId);
            }
        }

        // 숨겨진 업무를 제외하고 기존 행동 버튼만 같은 간격으로 재배치합니다.
        private void ArrangeVisibleActivities()
        {
            int visibleCount = activityButtons.Count(button => button.gameObject.activeSelf);
            int row = 0;
            foreach (Button button in activityButtons)
            {
                if (button.gameObject.activeSelf == false)
                {
                    continue;
                }
                RectTransform rect = (RectTransform)button.transform;
                float top = 1f - row / (float)Mathf.Max(visibleCount, 1);
                rect.anchorMin = new Vector2(0f, top - .17f);
                rect.anchorMax = new Vector2(1f, top);
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                row += 1;
            }
            previousButton.gameObject.SetActive(false);
            nextButton.gameObject.SetActive(false);
        }

        // 탐색은 즉시 배정하고 영입은 대상 선택으로 전환합니다.
        private void SelectActivity(WICharacterActivityType activity)
        {
            if (activity != WICharacterActivityType.Recruit)
            {
                Assign(activity, string.Empty);
                return;
            }
            pendingActivity = activity;
            pageMode = PageMode.Target;
            pageIndex = 0;
            if (administrationController.TryGetUGUICharacterActivityTargets(selectedHeroId, activity,
                out snapshot, out string error) == false)
            {
                ShowError(error);
                return;
            }
            modal.SetTitle(string.Format(administrationController.GetAdministrationText("UI_ACTIVITY_RECRUIT_TARGET"), selectedHeroName));
            contextLabel.text = string.Format(administrationController.GetAdministrationText("UI_ACTIVITY_TARGET_HINT"), selectedHeroName);
            messageLabel.gameObject.SetActive(false);
            ResetCandidateViewOptions();
            RefreshCards();
        }

        // 선택한 개인 활동을 기존 게임 상태에 배정합니다.
        private void Assign(WICharacterActivityType activity, string targetHeroId)
        {
            if (administrationController.AssignUGUICharacterActivity(selectedHeroId, activity, targetHeroId, out string error))
            {
                modal.Hide();
                return;
            }
            ShowError(error);
        }

        // 카드 페이지를 지정한 방향으로 이동합니다.
        private void ChangePage(int direction) { pageIndex += direction; RefreshCards(); }

        // 검색·등급 필터·정렬 조건을 현재 후보 목록에 적용합니다.
        private void RebuildVisibleCandidateIndices()
        {
            visibleCandidateIndices.Clear();
            if (snapshot == null)
            {
                return;
            }
            string search = searchInput == null ? string.Empty : searchInput.text.Trim();
            IEnumerable<int> indices = Enumerable.Range(0, snapshot.Candidates.Count).Where(index =>
            {
                WIAdministrationCharacterActivityCandidateSnapshot candidate = snapshot.Candidates[index];
                bool gradeMatches = gradeFilter == GradeFilter.All ||
                    (gradeFilter == GradeFilter.Hero && candidate.Grade == WICharacterGrade.Hero) ||
                    (gradeFilter == GradeFilter.Common && candidate.Grade == WICharacterGrade.Common);
                bool searchMatches = string.IsNullOrEmpty(search) ||
                    candidate.DisplayName.IndexOf(search, System.StringComparison.CurrentCultureIgnoreCase) >= 0;
                return gradeMatches && searchMatches;
            });
            if (sortByName)
            {
                indices = indices.OrderBy(index => snapshot.Candidates[index].DisplayName);
            }
            visibleCandidateIndices.AddRange(indices);
        }

        // 검색이나 필터 변경 시 첫 페이지에서 후보를 다시 표시합니다.
        private void ApplyCandidateViewOptions()
        {
            pageIndex = 0;
            if (pageMode != PageMode.Activity)
            {
                RefreshCards();
            }
        }

        // 선택한 등급 필터를 적용합니다.
        private void SetGradeFilter(int index)
        {
            gradeFilter = (GradeFilter)Mathf.Clamp(index, 0, 2);
            ApplyCandidateViewOptions();
        }

        // 추천 순서와 이름 순서를 전환합니다.
        private void ToggleSort()
        {
            sortByName = sortByName == false;
            ApplyCandidateViewOptions();
        }

        // 후보 단계가 바뀔 때 검색·필터·정렬을 기본값으로 되돌립니다.
        private void ResetCandidateViewOptions()
        {
            gradeFilter = GradeFilter.All;
            sortByName = false;
            if (searchInput != null)
            {
                searchInput.SetTextWithoutNotify(string.Empty);
            }
        }

        // 툴바 표시와 선택 상태 문구를 갱신합니다.
        private void RefreshToolbar()
        {
            if (toolbarRoot == null)
            {
                return;
            }
            bool visible = pageMode != PageMode.Activity;
            toolbarRoot.SetActive(visible);
            for (int index = 0; index < filterLabels.Length; index += 1)
            {
                bool selected = index == (int)gradeFilter;
                filterLabels[index].color = selected ? new Color32(190, 225, 255, 255) : new Color32(190, 197, 202, 255);
                if (index < filterButtons.Length && filterButtons[index] != null)
                {
                    filterButtons[index].image.sprite = selected ? filterSelectedSprite : filterNormalSprite;
                }
            }
            if (sortLabel != null)
            {
                sortLabel.text = sortByName ? "이름순" : "추천순";
            }
        }

        // 오류 메시지를 표시하고 선택 영역을 숨깁니다.
        private void ShowError(string error)
        {
            messageLabel.gameObject.SetActive(true);
            messageLabel.text = error;
            cardRoot.SetActive(false);
            activityRoot.SetActive(false);
            if (toolbarRoot != null)
            {
                toolbarRoot.SetActive(false);
            }
            previousButton.interactable = false;
            nextButton.interactable = false;
            pageLabel.text = string.Empty;
        }
    }
}

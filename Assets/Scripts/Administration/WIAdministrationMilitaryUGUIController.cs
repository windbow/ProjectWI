using TMPro;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectWI.Administration
{
    public sealed class WIAdministrationMilitaryUGUIController : WIAdministrationUGUIPanelController
    {
        [SerializeField] private WIAdministrationModalUGUIController modal;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private TMP_Text messageLabel;
        [SerializeField] private Button[] itemButtons;
        [SerializeField] private TMP_Text[] itemLabels;
        [SerializeField] private Button createButton;
        // 군사 단계 전체에서 재사용하는 스크롤 목록과 인물 초상 참조입니다.
        [SerializeField] private WICharacterSelectionList selectionList;
        [SerializeField] private Image[] itemPortraits;

        // 여러 페이지에 걸쳐 선택한 편성 인물 또는 출정 전투단 식별자입니다.
        private readonly HashSet<string> selectedMembers = new();
        private int memberPage;
        // 출정 전투단 선택 화면에서 확정할 공통 목표 성입니다.
        private string marchTargetId;
        private WIAdministrationMilitarySnapshot snapshot;
        private readonly Stack<MilitaryViewState> history = new();
        private MilitaryViewState view = new("overview", string.Empty, 0);

        // 고정 전투단 카드와 편성 버튼을 군사 기능에 연결합니다.
        private void Awake()
        {
            ResolveAdministrationController();
            for (int index = 0; index < itemButtons.Length; index += 1)
            {
                int captured = index;
                itemButtons[index].onClick.AddListener(() => OpenItem(captured));
            }
            createButton.onClick.AddListener(HandleFooter);
        }

        // 군사 UGUI 열기 요청을 구독합니다.
        private void OnEnable()
        {
            ResolveAdministrationController();
            if (administrationController != null)
            {
                administrationController.UGUIMilitaryRequested += Open;
            }
        }

        // 군사 UGUI 열기 요청 구독을 해제합니다.
        private void OnDisable()
        {
            if (administrationController != null)
            {
                administrationController.UGUIMilitaryRequested -= Open;
            }
        }

        // 현재 전투 세션과 플레이어 전투단 목록을 엽니다.
        private void Open()
        {
            history.Clear();
            selectedMembers.Clear();
            memberPage = 0;
            view = new MilitaryViewState("overview", string.Empty, 0);
            modal.Show("군사 · 전투단");
            Refresh();
        }

        // 캠페인 상태를 다시 읽어 고정 카드에 반영합니다.
        private void Refresh()
        {
            string error;
            bool loaded = view.Mode == "march-armies"
                ? administrationController.TryGetUGUIGroupMarchPanel(view.Context, marchTargetId, out snapshot, out error)
                : administrationController.TryGetUGUIMilitaryPanel(view.Mode, view.Context, view.Role, out snapshot, out error);
            if (loaded == false)
            {
                messageLabel.gameObject.SetActive(true);
                messageLabel.text = error;
                return;
            }
            modal.SetTitle(snapshot.Title);
            selectionList.Prepare(snapshot.Items.Count, OpenItem, administrationController,
                out itemButtons, out itemLabels, out itemPortraits);
            memberPage = 0;
            for (int index = 0; index < snapshot.Items.Count; index++)
            {
                Sprite portrait = administrationController.GetSelectionPortrait(snapshot.Items[index].Id);
                itemPortraits[index].sprite = portrait;
                itemPortraits[index].enabled = portrait != null;
            }
            if (view.Mode == "members" || view.Mode == "march-armies")
            {
                RefreshMemberSelection();
                return;
            }
            createButton.interactable = true;
            messageLabel.gameObject.SetActive(snapshot.Items.Count == 0);
            messageLabel.text = snapshot.Items.Count == 0 ? "진행 중인 전투와 편성된 전투단이 없습니다." : string.Empty;
            statusLabel.text = snapshot.Summary;
            int pageSize = itemButtons.Length;
            int pages = Mathf.Max(1, Mathf.CeilToInt(snapshot.Items.Count / (float)pageSize));
            memberPage = Mathf.Clamp(memberPage, 0, pages - 1);
            for (int index = 0; index < itemButtons.Length; index += 1)
            {
                if (index >= pageSize)
                {
                    itemButtons[index].gameObject.SetActive(pages > 1);
                    itemButtons[index].interactable = index == pageSize ? memberPage > 0 : memberPage + 1 < pages;
                    itemLabels[index].text = index == pageSize ? "◀" : "▶";
                    continue;
                }
                int candidate = memberPage * pageSize + index;
                bool visible = candidate < snapshot.Items.Count;
                itemButtons[index].gameObject.SetActive(visible);
                if (visible == false)
                {
                    continue;
                }
                WIAdministrationMilitaryItemSnapshot item = snapshot.Items[candidate];
                itemLabels[index].text = item.Title + "\n" + item.Description + "\n" + administrationController.GetSelectionCharacterSummary(item.Id);
                itemButtons[index].interactable = item.Interactable;
            }
            createButton.GetComponentInChildren<TMP_Text>().text = view.Mode == "overview" ? "새 전투단 편성" : "이전";
        }

        // 선택한 카드의 단계 이동 또는 군사 상태 변경을 처리합니다.
        private void OpenItem(int index)
        {
            if (snapshot == null)
            {
                return;
            }
            int pageSize = itemButtons.Length;
            if (index >= pageSize)
            {
                memberPage += index == pageSize ? -1 : 1;
                Refresh();
                return;
            }
            if (view.Mode == "members" || view.Mode == "march-armies")
            {
                int candidate = memberPage * pageSize + index;
                if (candidate >= snapshot.Items.Count)
                {
                    return;
                }
                string id = snapshot.Items[candidate].Id;
                if (snapshot.Items[candidate].Interactable == false)
                {
                    return;
                }
                if (selectedMembers.Remove(id) == false && selectedMembers.Count < snapshot.AvailableMemberSlots)
                {
                    selectedMembers.Add(id);
                }
                RefreshMemberSelection();
                return;
            }
            int itemIndex = memberPage * pageSize + index;
            if (itemIndex >= snapshot.Items.Count)
            {
                return;
            }
            WIAdministrationMilitaryItemSnapshot item = snapshot.Items[itemIndex];
            if (item.Interactable == false)
            {
                return;
            }
            switch (item.Kind)
            {
                case "muster-castles": Push("muster-castles", string.Empty, 0); return;
                case "muster-castle": Push("muster", item.Id, 0); return;
                case "battle": Push("battle", item.Id, 0); return;
                case "army": Push("army", item.Id, 0); return;
                case "castle": Push("commanders", item.Id, 0); return;
                case "members": selectedMembers.Clear(); memberPage = 0; Push("members", view.Context, 0); return;
                case "targets": Push("targets", view.Context, 0); return;
                case "march":
                    marchTargetId = item.Id;
                    selectedMembers.Clear();
                    selectedMembers.Add(view.Context);
                    Push("march-armies", view.Context, 0);
                    return;
            }
            if (administrationController.ExecuteUGUIMilitaryAction(
                    item.Kind, view.Context, item.Id, item.Value, out string nextContext, out string error) == false)
            {
                messageLabel.gameObject.SetActive(true);
                messageLabel.text = error;
                return;
            }
            if (item.Kind == "create")
            {
                history.Clear();
                view = new MilitaryViewState("army", nextContext, 0);
            }
            else if (item.Kind == "disband" || item.Kind == "march" || item.Kind == "start-battle")
            {
                history.Clear();
                view = new MilitaryViewState("overview", string.Empty, 0);
            }
            else if (item.Kind == "add-member")
            {
                while (history.Count > 0 && history.Peek().Mode != "army")
                {
                    history.Pop();
                }
                view = history.Count > 0 ? history.Pop() : new MilitaryViewState("army", view.Context, 0);
            }
            Refresh();
        }

        // 첫 화면에서는 편성 성 선택을 열고 하위 화면에서는 직전 단계로 돌아갑니다.
        private void HandleFooter()
        {
            selectionList.ResetView();
            if (view.Mode == "march-armies")
            {
                if (administrationController.MarchUGUIArmies(selectedMembers, marchTargetId, out string marchError) == true)
                {
                    history.Clear();
                    selectedMembers.Clear();
                    memberPage = 0;
                    view = new MilitaryViewState("overview", string.Empty, 0);
                    Refresh();
                }
                else
                {
                    messageLabel.gameObject.SetActive(true);
                    messageLabel.text = marchError;
                }
                return;
            }
            if (view.Mode == "members")
            {
                if (administrationController.AssignUGUIArmyMembers(view.Context, selectedMembers, out string error))
                {
                    view = history.Count > 0 ? history.Pop() : new MilitaryViewState("army", view.Context, 0);
                    selectedMembers.Clear();
                    Refresh();
                }
                else
                {
                    messageLabel.gameObject.SetActive(true);
                    messageLabel.text = error;
                }
                return;
            }
            if (view.Mode == "overview")
            {
                Push("castles", string.Empty, 0);
                return;
            }
            view = history.Count > 0 ? history.Pop() : new MilitaryViewState("overview", string.Empty, 0);
            memberPage = 0;
            Refresh();
        }

        // 기존 카드 여섯 개를 다중 선택 목록으로, 마지막 두 개를 페이지 이동으로 사용합니다.
        private void RefreshMemberSelection()
        {
            selectionList.Prepare(snapshot.Items.Count, OpenItem, administrationController,
                out itemButtons, out itemLabels, out itemPortraits);
            int pageSize = itemButtons.Length;
            int pages = Mathf.Max(1, Mathf.CeilToInt(snapshot.Items.Count / (float)pageSize));
            memberPage = Mathf.Clamp(memberPage, 0, pages - 1);
            selectedMembers.RemoveWhere(id => snapshot.Items.Exists(item => item.Id == id) == false);
            statusLabel.text = view.Mode == "march-armies"
                ? snapshot.Summary + "\n" + string.Format(administrationController.GetAdministrationText("UI_MARCH_LIST_STATUS"), selectedMembers.Count,
                    selectedMembers.Count * snapshot.MarchInfluencePerArmy, memberPage + 1, pages)
                : string.Format(administrationController.GetAdministrationText("UI_ARMY_LIST_STATUS"), selectedMembers.Count, snapshot.AvailableMemberSlots);
            messageLabel.gameObject.SetActive(false);
            for (int i = 0; i < itemButtons.Length; i++)
            {
                if (i >= pageSize)
                {
                    itemButtons[i].gameObject.SetActive(pages > 1);
                    itemButtons[i].interactable = i == pageSize ? memberPage > 0 : memberPage + 1 < pages;
                    itemLabels[i].text = i == pageSize ? "◀" : "▶";
                    continue;
                }
                int candidate = memberPage * pageSize + i;
                itemButtons[i].gameObject.SetActive(candidate < snapshot.Items.Count);
                if (candidate >= snapshot.Items.Count)
                {
                    continue;
                }
                var item = snapshot.Items[candidate];
                bool selected = selectedMembers.Contains(item.Id);
                itemLabels[i].text = (selected ? "[●] " : "[ ] ") + item.Title + "\n" + item.Description + "\n" + administrationController.GetSelectionCharacterSummary(item.Id);
                itemButtons[i].interactable = item.Interactable == true &&
                    (selected == true || selectedMembers.Count < snapshot.AvailableMemberSlots);
            }
            createButton.GetComponentInChildren<TMP_Text>().text = string.Format(administrationController.GetAdministrationText(
                view.Mode == "march-armies" ? "UI_GROUP_MARCH_CONFIRM" : "UI_ARMY_MULTI_CONFIRM"), selectedMembers.Count);
            createButton.interactable = selectedMembers.Count > 0;
        }

        // 현재 단계를 기록하고 다음 고정 카드 화면으로 이동합니다.
        private void Push(string mode, string context, int role)
        {
            selectionList.ResetView();
            history.Push(view);
            memberPage = 0;
            view = new MilitaryViewState(mode, context, role);
            Refresh();
        }

        private readonly struct MilitaryViewState
        {
            public readonly string Mode;
            public readonly string Context;
            public readonly int Role;

            // 군사 모달의 현재 단계와 선택 문맥을 보관합니다.
            public MilitaryViewState(string mode, string context, int role)
            {
                Mode = mode;
                Context = context;
                Role = role;
            }
        }
    }
}

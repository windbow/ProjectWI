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

        // 여러 페이지에 걸쳐 사용자가 선택한 편성 후보입니다.
        private readonly HashSet<string> selectedMembers = new();
        private int memberPage;
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
            view = new MilitaryViewState("overview", string.Empty, 0);
            modal.Show("군사 · 전투단");
            Refresh();
        }

        // 캠페인 상태를 다시 읽어 고정 카드에 반영합니다.
        private void Refresh()
        {
            if (administrationController.TryGetUGUIMilitaryPanel(view.Mode, view.Context, view.Role, out snapshot, out string error)
                == false)
            {
                messageLabel.gameObject.SetActive(true);
                messageLabel.text = error;
                return;
            }
            modal.SetTitle(snapshot.Title);
            if (view.Mode == "members")
            {
                RefreshMemberSelection();
                return;
            }
            createButton.interactable = true;
            messageLabel.gameObject.SetActive(snapshot.Items.Count == 0);
            messageLabel.text = snapshot.Items.Count == 0 ? "진행 중인 전투와 편성된 전투단이 없습니다." : string.Empty;
            statusLabel.text = snapshot.Summary;
            for (int index = 0; index < itemButtons.Length; index += 1)
            {
                bool visible = index < snapshot.Items.Count;
                itemButtons[index].gameObject.SetActive(visible);
                if (visible == false)
                {
                    continue;
                }
                WIAdministrationMilitaryItemSnapshot item = snapshot.Items[index];
                itemLabels[index].text = item.Title + "\n" + item.Description;
                itemButtons[index].interactable = item.Interactable;
            }
            createButton.GetComponentInChildren<TMP_Text>().text = view.Mode == "overview" ? "새 전투단 편성" : "이전";
        }

        // 선택한 카드의 단계 이동 또는 군사 상태 변경을 처리합니다.
        private void OpenItem(int index)
        {
            if (snapshot == null || (view.Mode != "members" && index >= snapshot.Items.Count))
            {
                return;
            }
            if (view.Mode == "members")
            {
                int pageSize = itemButtons.Length - 2;
                if (index >= pageSize)
                {
                    memberPage += index == pageSize ? -1 : 1;
                    RefreshMemberSelection();
                    return;
                }
                int candidate = memberPage * pageSize + index;
                if (candidate >= snapshot.Items.Count)
                {
                    return;
                }
                string id = snapshot.Items[candidate].Id;
                if (selectedMembers.Remove(id) == false && selectedMembers.Count < snapshot.AvailableMemberSlots)
                {
                    selectedMembers.Add(id);
                }
                RefreshMemberSelection();
                return;
            }
            WIAdministrationMilitaryItemSnapshot item = snapshot.Items[index];
            switch (item.Kind)
            {
                case "battle": Push("battle", item.Id, 0); return;
                case "army": Push("army", item.Id, 0); return;
                case "castle": Push("commanders", item.Id, 0); return;
                case "members": selectedMembers.Clear(); memberPage = 0; Push("members", view.Context, 0); return;
                case "targets": Push("targets", view.Context, 0); return;
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
            Refresh();
        }

        // 기존 카드 여섯 개를 다중 선택 목록으로, 마지막 두 개를 페이지 이동으로 사용합니다.
        private void RefreshMemberSelection()
        {
            int pageSize = itemButtons.Length - 2;
            int pages = Mathf.Max(1, Mathf.CeilToInt(snapshot.Items.Count / (float)pageSize));
            memberPage = Mathf.Clamp(memberPage, 0, pages - 1);
            statusLabel.text = string.Format(administrationController.GetAdministrationText("UI_ARMY_MULTI_STATUS"), selectedMembers.Count, snapshot.AvailableMemberSlots, memberPage + 1, pages);
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
                itemLabels[i].text = (selected ? "[●] " : "[ ] ") + item.Title + "\n" + item.Description;
                itemButtons[i].interactable = selected || selectedMembers.Count < snapshot.AvailableMemberSlots;
            }
            createButton.GetComponentInChildren<TMP_Text>().text = string.Format(administrationController.GetAdministrationText("UI_ARMY_MULTI_CONFIRM"), selectedMembers.Count);
            createButton.interactable = selectedMembers.Count > 0;
        }

        // 현재 단계를 기록하고 다음 고정 카드 화면으로 이동합니다.
        private void Push(string mode, string context, int role)
        {
            history.Push(view);
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

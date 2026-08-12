using TMPro;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectWI.Administration
{
    public sealed class WIAdministrationMilitaryUGUIController : MonoBehaviour
    {
        [SerializeField] private WIAdministrationUIController administrationController;
        [SerializeField] private WIAdministrationModalUGUIController modal;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private TMP_Text messageLabel;
        [SerializeField] private Button[] itemButtons;
        [SerializeField] private TMP_Text[] itemLabels;
        [SerializeField] private Button createButton;

        private WIAdministrationMilitarySnapshot snapshot;
        private readonly Stack<MilitaryViewState> history = new();
        private MilitaryViewState view = new("overview", string.Empty, 0);

        // 고정 전투단 카드와 편성 버튼을 군사 기능에 연결합니다.
        private void Awake()
        {
            if (administrationController == null) administrationController = FindFirstObjectByType<WIAdministrationUIController>();
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
            if (administrationController == null) administrationController = FindFirstObjectByType<WIAdministrationUIController>();
            if (administrationController != null) administrationController.UGUIMilitaryRequested += Open;
        }

        // 군사 UGUI 열기 요청 구독을 해제합니다.
        private void OnDisable()
        {
            if (administrationController != null) administrationController.UGUIMilitaryRequested -= Open;
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
            if (administrationController.TryGetUGUIMilitaryPanel(view.Mode, view.Context, view.Role, out snapshot, out string error) == false)
            {
                messageLabel.gameObject.SetActive(true);
                messageLabel.text = error;
                return;
            }
            modal.SetTitle(snapshot.Title);
            messageLabel.gameObject.SetActive(snapshot.Items.Count == 0);
            messageLabel.text = snapshot.Items.Count == 0 ? "진행 중인 전투와 편성된 전투단이 없습니다." : string.Empty;
            statusLabel.text = snapshot.Summary;
            for (int index = 0; index < itemButtons.Length; index += 1)
            {
                bool visible = index < snapshot.Items.Count;
                itemButtons[index].gameObject.SetActive(visible);
                if (visible == false) continue;
                WIAdministrationMilitaryItemSnapshot item = snapshot.Items[index];
                itemLabels[index].text = item.Title + "\n" + item.Description;
                itemButtons[index].interactable = item.Interactable;
            }
            createButton.GetComponentInChildren<TMP_Text>().text = view.Mode == "overview" ? "새 전투단 편성" : "이전";
        }

        // 선택한 카드의 단계 이동 또는 군사 상태 변경을 처리합니다.
        private void OpenItem(int index)
        {
            if (snapshot == null || index >= snapshot.Items.Count) return;
            WIAdministrationMilitaryItemSnapshot item = snapshot.Items[index];
            switch (item.Kind)
            {
                case "battle": Push("battle", item.Id, 0); return;
                case "army": Push("army", item.Id, 0); return;
                case "castle": Push("commanders", item.Id, 0); return;
                case "roles": Push("roles", view.Context, 0); return;
                case "role": Push("members", view.Context, item.Value); return;
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
                while (history.Count > 0 && history.Peek().Mode != "army") history.Pop();
                view = history.Count > 0 ? history.Pop() : new MilitaryViewState("army", view.Context, 0);
            }
            Refresh();
        }

        // 첫 화면에서는 편성 성 선택을 열고 하위 화면에서는 직전 단계로 돌아갑니다.
        private void HandleFooter()
        {
            if (view.Mode == "overview")
            {
                Push("castles", string.Empty, 0);
                return;
            }
            view = history.Count > 0 ? history.Pop() : new MilitaryViewState("overview", string.Empty, 0);
            Refresh();
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

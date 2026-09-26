using System.Collections.Generic;
using System.Linq;
using ProjectWI.Administration;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectWI.Battle
{
    public partial class WIBattleHUDController
    {
        // 이 거리(패널 픽셀) 이상 끌면 클릭 대신 범위 선택으로 처리합니다.
        private const float DragSelectThreshold = 12f;

        // 현재 선택한 플레이어 분대 번호 목록입니다.
        private readonly HashSet<int> selectedSquadIds = new HashSet<int>();
        // UXML에 미리 만든 범위 선택 사각형 요소입니다.
        private VisualElement selectionBox;
        // UXML에 미리 만든 전술 일시정지 표시 요소입니다.
        private Label pauseIndicator;
        // 전장 포인터를 누른 패널 좌표와 버튼, 누른 상태 여부입니다.
        private Vector2 pointerStart;
        private int pointerButton = -1;
        private bool pointerDragging;

        // 선택·일시정지 표시 요소를 찾고 전장 포인터 입력을 연결합니다.
        private void BindSelectionInput(VisualElement root)
        {
            selectionBox = root.Q<VisualElement>("selection-box");
            pauseIndicator = root.Q<Label>("pause-indicator");
            if (selectionBox == null || pauseIndicator == null)
            {
                Debug.LogError("전투 HUD UXML에 selection-box 또는 pause-indicator가 없습니다.", this);
            }
            root.RegisterCallback<PointerDownEvent>(HandlePointerDown, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerMoveEvent>(HandlePointerMove, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerUpEvent>(HandlePointerUp, TrickleDown.TrickleDown);
        }

        // 선택 분대가 있으면 그 분대에만, 없으면 진영 전체에 명령을 내리는지 반환합니다.
        private bool HasSquadSelection => selectedSquadIds.Count > 0;

        // 전장 빈 곳이나 인물 위에서 누른 포인터 위치를 기록합니다.
        private void HandlePointerDown(PointerDownEvent pointerEvent)
        {
            if (IsOverHudButton(pointerEvent.target) == true || battleController?.CameraController == null)
            {
                return;
            }
            pointerStart = pointerEvent.position;
            lastPointerPanelPosition = pointerEvent.position;
            pointerButton = pointerEvent.button;
            pointerDragging = false;
        }

        // 왼쪽 버튼으로 일정 거리 이상 끌면 범위 선택 사각형을 표시합니다.
        private void HandlePointerMove(PointerMoveEvent pointerEvent)
        {
            Vector2 previousPanelPosition = lastPointerPanelPosition;
            lastPointerPanelPosition = pointerEvent.position;
            if (pointerButton == 2)
            {
                battleController?.CameraController?.DragPan(PanelToScreen(previousPanelPosition), PanelToScreen(pointerEvent.position));
                return;
            }
            if (pointerButton != 0 || IsTargetingSkill == true)
            {
                return;
            }
            Vector2 current = pointerEvent.position;
            if (pointerDragging == false && Vector2.Distance(current, pointerStart) < DragSelectThreshold)
            {
                return;
            }
            pointerDragging = true;
            Rect rect = GetRect(pointerStart, current);
            if (selectionBox != null)
            {
                selectionBox.style.display = DisplayStyle.Flex;
                selectionBox.style.left = rect.xMin;
                selectionBox.style.top = rect.yMin;
                selectionBox.style.width = rect.width;
                selectionBox.style.height = rect.height;
            }
        }

        // 범위 선택을 확정하거나, 클릭 위치에 따라 분대 선택·이동·공격 명령을 처리합니다.
        private void HandlePointerUp(PointerUpEvent pointerEvent)
        {
            int button = pointerButton;
            pointerButton = -1;
            if (button == 2)
            {
                pointerEvent.StopPropagation();
                return;
            }
            if (button < 0 || battleController?.CameraController == null || battleController.Runtime == null)
            {
                return;
            }
            bool additive = pointerEvent.shiftKey == true;
            if (pointerDragging == true)
            {
                pointerDragging = false;
                if (selectionBox != null)
                {
                    selectionBox.style.display = DisplayStyle.None;
                }
                SelectSquadsInRect(GetRect(pointerStart, pointerEvent.position), additive);
                pointerEvent.StopPropagation();
                return;
            }
            Vector2 worldPosition = battleController.CameraController.ScreenToBattlePosition(PanelToScreen(pointerEvent.position));
            if (IsTargetingSkill == true)
            {
                if (button == 0)
                {
                    CastTargetedSkill(worldPosition);
                }
                else
                {
                    CancelSkillTargeting();
                    commandFeedbackLabel.text = Text("UI_BATTLE_SKILL_TARGETING_CANCELLED");
                }
                pointerEvent.StopPropagation();
                return;
            }
            WIBattleCharacterState clicked = battleController.FindCharacterAt(worldPosition);
            if (button == 1)
            {
                IssueOrderAt(worldPosition, clicked);
            }
            else if (button == 0)
            {
                HandleLeftClick(worldPosition, clicked, additive);
            }
            pointerEvent.StopPropagation();
        }

        // 아군 클릭은 분대 선택, 적 클릭은 정보 표시(선택 분대가 있으면 공격), 빈 곳은 선택 분대 이동으로 처리합니다.
        private void HandleLeftClick(Vector2 worldPosition, WIBattleCharacterState clicked, bool additive)
        {
            if (clicked != null && clicked.Side == playerSide)
            {
                ToggleSquadSelection(clicked.SquadId, additive);
                selectionInfoLabel.text = DescribeCharacter(clicked);
                return;
            }
            if (HasSquadSelection == true || IsControllingHero == true)
            {
                IssueOrderAt(worldPosition, clicked);
                return;
            }
            battleController.SelectCharacterAt(worldPosition);
            selectionInfoLabel.text = clicked == null ? Text("UI_BATTLE_SELECT_NONE") : DescribeCharacter(clicked);
        }

        // 선택 분대에 적 표적이면 공격, 아니면 해당 좌표로 이동 명령을 내립니다.
        private void IssueOrderAt(Vector2 worldPosition, WIBattleCharacterState clicked)
        {
            if (battleController.IsDeploying == true)
            {
                if (HasSquadSelection == true)
                {
                    battleController.DeploySquads(selectedSquadIds, worldPosition);
                    battleController.ShowOrderPing(worldPosition, false);
                    commandFeedbackLabel.text = Text("UI_BATTLE_DEPLOY_MOVED", selectedSquadIds.Count);
                }
                return;
            }
            bool clickedEnemy = clicked != null && clicked.Side != playerSide;
            if (IsControllingHero == true && clickedEnemy == false)
            {
                battleController.OrderControlledHeroMove(worldPosition);
                battleController.ShowOrderPing(worldPosition, false);
                return;
            }
            if (HasSquadSelection == false)
            {
                commandFeedbackLabel.text = Text("UI_BATTLE_ORDER_NEED_SELECTION");
                return;
            }
            if (clicked != null && clicked.Side != playerSide)
            {
                battleController.OrderSquadsAttack(selectedSquadIds, clicked.HeroId);
                battleController.ShowOrderPing(clicked.Position, true);
                commandFeedbackLabel.text = Text("UI_BATTLE_ORDER_ATTACK", clicked.DisplayName, selectedSquadIds.Count);
                return;
            }
            battleController.OrderSquadsMove(selectedSquadIds, worldPosition);
            battleController.ShowOrderPing(worldPosition, false);
            commandFeedbackLabel.text = Text("UI_BATTLE_ORDER_MOVE", selectedSquadIds.Count);
        }

        // 한 분대의 선택을 바꾸며 Shift를 누르지 않았으면 기존 선택을 대체합니다.
        private void ToggleSquadSelection(int squadId, bool additive)
        {
            if (additive == false)
            {
                bool onlyThis = selectedSquadIds.Count == 1 && selectedSquadIds.Contains(squadId) == true;
                selectedSquadIds.Clear();
                if (onlyThis == false)
                {
                    selectedSquadIds.Add(squadId);
                }
            }
            else if (selectedSquadIds.Remove(squadId) == false)
            {
                selectedSquadIds.Add(squadId);
            }
            ApplySquadSelection();
        }

        // 화면 사각형 안에 발 위치가 들어온 플레이어 인물의 분대를 선택합니다.
        private void SelectSquadsInRect(Rect panelRect, bool additive)
        {
            if (additive == false)
            {
                selectedSquadIds.Clear();
            }
            Camera battleCamera = battleController.CameraController.BattleCamera;
            IPanel panel = statusLabel.panel;
            foreach (WIBattleCharacterState character in battleController.Runtime.Characters)
            {
                if (character.IsAlive == false || character.Side != playerSide || battleCamera == null || panel == null)
                {
                    continue;
                }
                Vector2 panelPosition = RuntimePanelUtils.CameraTransformWorldToPanel(panel, character.Position, battleCamera);
                if (panelRect.Contains(panelPosition) == true)
                {
                    selectedSquadIds.Add(character.SquadId);
                }
            }
            ApplySquadSelection();
        }

        // 플레이어 진영 분대를 번호 순서대로 반환합니다.
        private List<WIBattleSquadState> GetPlayerSquads()
        {
            WIBattleRuntimeState runtime = battleController?.Runtime;
            if (runtime == null)
            {
                return new List<WIBattleSquadState>();
            }
            return runtime.Squads.Where(item => item.Side == playerSide && runtime.Characters.Any(character =>
                character.SquadId == item.SquadId && character.IsAlive == true)).OrderBy(item => item.SquadId).ToList();
        }

        // 숫자키 순번의 분대를 선택합니다. Shift를 누르면 선택에 추가·제거합니다.
        private void SelectSquadByIndex(int index, bool additive)
        {
            List<WIBattleSquadState> squads = GetPlayerSquads();
            if (index < 0 || index >= squads.Count)
            {
                return;
            }
            ToggleSquadSelection(squads[index].SquadId, additive);
        }

        // 살아 있는 플레이어 분대를 모두 선택합니다.
        private void SelectAllSquads()
        {
            selectedSquadIds.Clear();
            foreach (WIBattleSquadState squad in GetPlayerSquads())
            {
                selectedSquadIds.Add(squad.SquadId);
            }
            ApplySquadSelection();
        }

        // 분대 선택을 모두 해제합니다.
        private void ClearSquadSelection()
        {
            selectedSquadIds.Clear();
            ApplySquadSelection();
            selectionInfoLabel.text = Text("UI_BATTLE_SELECTION_CLEARED");
        }

        // 전멸한 분대를 선택에서 빼고 선택 표시와 안내 문구를 갱신합니다.
        private void ApplySquadSelection()
        {
            HashSet<int> alive = new HashSet<int>(GetPlayerSquads().Select(item => item.SquadId));
            selectedSquadIds.RemoveWhere(item => alive.Contains(item) == false);
            battleController?.SetSelectedSquads(selectedSquadIds);
            if (selectedSquadIds.Count == 0)
            {
                return;
            }
            int members = battleController.Runtime.Characters.Count(item =>
                item.IsAlive == true && selectedSquadIds.Contains(item.SquadId) == true);
            float morale = battleController.Runtime.Squads.Where(item => selectedSquadIds.Contains(item.SquadId) == true)
                .Average(item => item.Morale);
            commandFeedbackLabel.text = Text("UI_BATTLE_SQUAD_SELECTED", selectedSquadIds.Count, members, morale);
        }

        // 전술 일시정지를 전환하고 표시를 갱신합니다.
        private void TogglePause()
        {
            if (battleController == null)
            {
                return;
            }
            battleController.IsPaused = battleController.IsPaused == false;
            if (pauseIndicator != null)
            {
                pauseIndicator.text = Text("UI_BATTLE_PAUSED");
                pauseIndicator.style.display = battleController.IsPaused == true ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        // 선택한 인물의 등급·이름·역할·체력·마나 안내를 반환합니다.
        private string DescribeCharacter(WIBattleCharacterState character)
        {
            WIBattleSquadState squad = WIBattleSimulation.FindSquad(battleController.Runtime, character.SquadId);
            return Text("UI_BATTLE_SELECT_INFO",
                Text(character.Grade == WICharacterGrade.Hero ? "UI_SELECTION_HERO" : "UI_SELECTION_COMMON"),
                character.DisplayName, character.Role, character.Health, character.MaxHealth, character.Mana, character.MaxMana,
                squad == null ? 0f : squad.Morale, GetTerrainName(character.Position), GetArchetypeName(character.Archetype));
        }

        // 병과 이름과 한 줄 특징을 반환합니다.
        private string GetArchetypeName(WIBattleArchetype archetype)
        {
            switch (archetype)
            {
                case WIBattleArchetype.Shield: return Text("UI_BATTLE_ARCHETYPE_SHIELD");
                case WIBattleArchetype.Charger: return Text("UI_BATTLE_ARCHETYPE_CHARGER");
                case WIBattleArchetype.Skirmisher: return Text("UI_BATTLE_ARCHETYPE_SKIRMISHER");
                case WIBattleArchetype.Archer: return Text("UI_BATTLE_ARCHETYPE_ARCHER");
                case WIBattleArchetype.Caster: return Text("UI_BATTLE_ARCHETYPE_CASTER");
                case WIBattleArchetype.Support: return Text("UI_BATTLE_ARCHETYPE_SUPPORT");
                default: return Text("UI_BATTLE_ARCHETYPE_COMMANDER");
            }
        }

        // 패널 좌표를 카메라 변환에 사용할 화면 좌표(좌하단 원점)로 바꿉니다.
        private Vector2 PanelToScreen(Vector2 panelPosition)
        {
            float panelWidth = statusLabel.panel?.visualTree.layout.width ?? Screen.width;
            float scale = panelWidth > 0f ? Screen.width / panelWidth : 1f;
            return new Vector2(panelPosition.x * scale, Screen.height - panelPosition.y * scale);
        }

        // 두 점을 모서리로 하는 사각형을 반환합니다.
        private static Rect GetRect(Vector2 start, Vector2 end)
        {
            return Rect.MinMaxRect(
                Mathf.Min(start.x, end.x), Mathf.Min(start.y, end.y),
                Mathf.Max(start.x, end.x), Mathf.Max(start.y, end.y));
        }

        // 포인터 대상이 HUD 버튼이면 전장 입력으로 처리하지 않습니다.
        private static bool IsOverHudButton(IEventHandler target)
        {
            VisualElement element = target as VisualElement;
            return element is Button || element?.GetFirstAncestorOfType<Button>() != null;
        }
    }
}

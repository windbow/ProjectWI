using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectWI.Battle
{
    [RequireComponent(typeof(UIDocument))]
    public class WIBattleHUDController : MonoBehaviour
    {
        [SerializeField] private WIBattleRuntimeController battleController;
        [SerializeField] private WIBattleSide playerSide = WIBattleSide.Attacker;

        private Label statusLabel;
        private Label commandFeedbackLabel;
        private Label selectionInfoLabel;
        // UXML에 저장된 네 슬롯과 페이지 이동 요소를 재사용합니다.
        private readonly Button[] skillSlots = new Button[4];
        private Button skillPrevious;
        private Button skillNext;
        private Label skillPageLabel;
        private int skillPage;
        private readonly Dictionary<string, Button> skillButtonByHeroId = new Dictionary<string, Button>();
        private readonly Dictionary<WIBattleCommand, Button> commandButtons = new Dictionary<WIBattleCommand, Button>();
        private float retreatConfirmUntil;

        // 전투 세션에서 확인한 플레이어 진영을 HUD 명령 대상으로 지정합니다.
        public void SetPlayerSide(WIBattleSide side)
        {
            playerSide = side;
        }

        // 전투 HUD 요소와 전투단 명령 버튼을 런타임 컨트롤러에 연결합니다.
        private void Awake()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            statusLabel = root.Q<Label>("battle-status");
            commandFeedbackLabel = root.Q<Label>("command-feedback");
            selectionInfoLabel = root.Q<Label>("selection-info");
            skillPrevious = root.Q<Button>("skill-previous");
            skillNext = root.Q<Button>("skill-next");
            skillPageLabel = root.Q<Label>("skill-page");
            skillPrevious.clicked += () => skillPage = Mathf.Max(0, skillPage - 1);
            skillNext.clicked += () => skillPage += 1;
            for (int index = 0; index < skillSlots.Length; index += 1)
            {
                Button slot = root.Q<Button>("skill-slot-" + index);
                skillSlots[index] = slot;
                slot.clicked += () =>
                {
                    if (slot.userData is string heroId)
                    {
                        battleController.TryActivateHeroSkill(heroId);
                    }
                };
            }
            RegisterCommandButton(root, "advance-command", WIBattleCommand.Advance);
            RegisterCommandButton(root, "hold-command", WIBattleCommand.Hold);
            RegisterCommandButton(root, "focus-command", WIBattleCommand.Focus);
            RegisterCommandButton(root, "retreat-command", WIBattleCommand.Retreat);
            root.focusable = true;
            root.RegisterCallback<KeyDownEvent>(HandleKeyboardCommand, TrickleDown.TrickleDown);
            root.RegisterCallback<KeyUpEvent>(HandleKeyboardRelease, TrickleDown.TrickleDown);
            root.RegisterCallback<WheelEvent>(HandleMouseWheel, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerDownEvent>(HandleBattlefieldClick, TrickleDown.TrickleDown);
            root.Focus();
        }

        // 현재 생존 인원과 전투 시간을 표시하고 영웅 스킬 버튼을 보충합니다.
        private void Update()
        {
            if (statusLabel == null)
            {
                return;
            }
            WIBattleRuntimeState runtime = battleController == null ? null : battleController.Runtime;
            if (runtime == null)
            {
                statusLabel.text = "전투 세션 대기 중";
                return;
            }
            int attackers = runtime.Characters.Count(item => item.Side == WIBattleSide.Attacker && item.IsAlive);
            int defenders = runtime.Characters.Count(item => item.Side == WIBattleSide.Defender && item.IsAlive);
            statusLabel.text = $"{runtime.ObjectiveName} · {runtime.ElapsedSeconds:0.0}초 · 공격 {attackers} / 수비 {defenders}";
            BuildSkillButtons(runtime);
            RefreshSkillButtons(runtime);
            RefreshCommandFeedback(runtime);
        }

        // 선택한 전투단 명령을 플레이어 진영에 적용합니다.
        private void SetCommand(WIBattleCommand command)
        {
            if (battleController == null || battleController.Runtime == null) return;
            if (command == WIBattleCommand.Retreat)
            {
                if (Time.unscaledTime > retreatConfirmUntil)
                {
                    retreatConfirmUntil = Time.unscaledTime + 3f;
                    commandFeedbackLabel.text = "후퇴하면 즉시 패배합니다. 3초 안에 다시 눌러 확정하세요.";
                    commandButtons[WIBattleCommand.Retreat].AddToClassList("retreat-armed");
                    return;
                }
            }

            string focusHeroId = command == WIBattleCommand.Focus ? SelectFocusTarget(battleController.Runtime) : string.Empty;
            battleController.SetCommand(playerSide, command, focusHeroId);
            retreatConfirmUntil = 0f;
            commandButtons[WIBattleCommand.Retreat].RemoveFromClassList("retreat-armed");
            commandFeedbackLabel.text = GetCommandFeedback(command, focusHeroId, battleController.Runtime);
        }

        // UXML의 명령 버튼을 명령 사전에 등록하고 공통 클릭 처리를 연결합니다.
        private void RegisterCommandButton(VisualElement root, string elementName, WIBattleCommand command)
        {
            Button button = root.Q<Button>(elementName);
            commandButtons[command] = button;
            button.clicked += () => SetCommand(command);
        }

        // 플레이어 진영 중앙에서 가장 가까운 생존 적을 집중 공격 대상으로 선택합니다.
        private string SelectFocusTarget(WIBattleRuntimeState runtime)
        {
            List<WIBattleCharacterState> allies = runtime.Characters.Where(item => item.Side == playerSide && item.IsAlive).ToList();
            if (allies.Count == 0) return string.Empty;
            Vector2 center = allies.Aggregate(Vector2.zero, (sum, item) => sum + item.Position) / allies.Count;
            WIBattleCharacterState target = runtime.Characters
                .Where(item => item.Side != playerSide && item.IsAlive)
                .OrderBy(item => Vector2.SqrMagnitude(item.Position - center))
                .ThenBy(item => item.Health)
                .FirstOrDefault();
            return target?.HeroId ?? string.Empty;
        }

        // 현재 선택한 명령과 집중 표적을 플레이어가 이해할 수 있는 안내로 반환합니다.
        private static string GetCommandFeedback(WIBattleCommand command, string focusHeroId, WIBattleRuntimeState runtime)
        {
            if (command == WIBattleCommand.Advance) return "전진 명령 · 진형 행을 유지하며 적에게 접근합니다.";
            if (command == WIBattleCommand.Hold) return "위치 사수 · 원래 진형으로 복귀하며 피해와 밀치기를 줄입니다.";
            if (command == WIBattleCommand.Retreat) return "후퇴 확정 · 전투 패배를 받아들이고 철수합니다.";
            WIBattleCharacterState target = runtime.Characters.Find(item => item.HeroId == focusHeroId);
            return target == null ? "집중 공격 · 유효한 표적이 없습니다." : $"집중 공격 · {target.DisplayName}을 우선 공격합니다.";
        }

        // 현재 명령 버튼을 금색으로 강조하고 후퇴 확인 제한 시간이 지나면 경고 상태를 해제합니다.
        private void RefreshCommandFeedback(WIBattleRuntimeState runtime)
        {
            WIBattleCommand current = playerSide == WIBattleSide.Attacker ? runtime.AttackerCommand : runtime.DefenderCommand;
            foreach (KeyValuePair<WIBattleCommand, Button> pair in commandButtons)
            {
                pair.Value.EnableInClassList("active-command", pair.Key == current);
            }
            if (retreatConfirmUntil > 0f && Time.unscaledTime > retreatConfirmUntil)
            {
                retreatConfirmUntil = 0f;
                commandButtons[WIBattleCommand.Retreat].RemoveFromClassList("retreat-armed");
                commandFeedbackLabel.text = "후퇴 확인이 취소되었습니다.";
            }
        }

        // PC 키보드의 숫자 1·2·3과 R 키를 전투 명령에 연결합니다.
        private void HandleKeyboardCommand(KeyDownEvent keyboardEvent)
        {
            if (keyboardEvent.keyCode == KeyCode.Alpha1) SetCommand(WIBattleCommand.Advance);
            else if (keyboardEvent.keyCode == KeyCode.Alpha2) SetCommand(WIBattleCommand.Hold);
            else if (keyboardEvent.keyCode == KeyCode.Alpha3) SetCommand(WIBattleCommand.Focus);
            else if (keyboardEvent.keyCode == KeyCode.R) SetCommand(WIBattleCommand.Retreat);
            else if (keyboardEvent.keyCode == KeyCode.Home)
            {
                battleController?.CameraController?.ResetView();
                selectionInfoLabel.text = "카메라를 전장 중앙 기본 시점으로 복원했습니다.";
            }
            else if (IsCameraMoveKey(keyboardEvent.keyCode))
            {
                battleController?.CameraController?.SetMoveKey(keyboardEvent.keyCode, true);
            }
            else return;
            keyboardEvent.StopPropagation();
        }

        // WASD·방향키를 놓으면 해당 카메라 이동 방향을 해제합니다.
        private void HandleKeyboardRelease(KeyUpEvent keyboardEvent)
        {
            if (IsCameraMoveKey(keyboardEvent.keyCode) == false) return;
            battleController?.CameraController?.SetMoveKey(keyboardEvent.keyCode, false);
            keyboardEvent.StopPropagation();
        }

        // 마우스 휠을 전투 카메라 확대·축소에 전달합니다.
        private void HandleMouseWheel(WheelEvent wheelEvent)
        {
            battleController?.CameraController?.Zoom(Mathf.Sign(wheelEvent.delta.y));
            wheelEvent.StopPropagation();
        }

        // HUD 버튼 영역이 아닌 전장을 클릭하면 가장 가까운 캐릭터의 정보를 표시합니다.
        private void HandleBattlefieldClick(PointerDownEvent pointerEvent)
        {
            if (pointerEvent.button != 0 || battleController?.CameraController == null) return;
            VisualElement clickedElement = pointerEvent.target as VisualElement;
            if (clickedElement is Button || clickedElement?.GetFirstAncestorOfType<Button>() != null) return;
            Vector2 screenPosition = new Vector2(pointerEvent.position.x, Screen.height - pointerEvent.position.y);
            Vector2 worldPosition = battleController.CameraController.ScreenToBattlePosition(screenPosition);
            WIBattleCharacterState selected = battleController.SelectCharacterAt(worldPosition);
            selectionInfoLabel.text = selected == null
                ? "선택 해제 · 인물 가까이를 클릭하세요."
                : $"[{(selected.Grade == ProjectWI.Administration.WICharacterGrade.Hero ? "영웅" : "일반")}] {selected.DisplayName} · " +
                  $"{selected.Role} · HP {selected.Health}/{selected.MaxHealth} · MP {selected.Mana}/{selected.MaxMana}";
        }

        // 전달된 키가 PC 카메라 이동에 사용하는 WASD 또는 방향키인지 반환합니다.
        private static bool IsCameraMoveKey(KeyCode keyCode)
        {
            return keyCode == KeyCode.W || keyCode == KeyCode.A || keyCode == KeyCode.S || keyCode == KeyCode.D ||
                   keyCode == KeyCode.UpArrow || keyCode == KeyCode.DownArrow ||
                   keyCode == KeyCode.LeftArrow || keyCode == KeyCode.RightArrow;
        }

        // 현재 페이지의 스킬을 저장된 네 슬롯에 연결하며 UI를 동적으로 생성하지 않습니다.
        private void BuildSkillButtons(WIBattleRuntimeState runtime)
        {
            List<WIBattleCharacterState> characters = runtime.Characters
                .Where(item => item.Side == playerSide && battleController.Config.GetCharacterSkill(item.HeroId, item.HeroClass) != null)
                .GroupBy(item => item.HeroId).Select(group => group.First()).ToList();
            int pageCount = Mathf.Max(1, Mathf.CeilToInt(characters.Count / (float)skillSlots.Length));
            skillPage = Mathf.Clamp(skillPage, 0, pageCount - 1);
            skillPrevious.SetEnabled(skillPage > 0);
            skillNext.SetEnabled(skillPage + 1 < pageCount);
            skillPageLabel.text = $"{skillPage + 1} / {pageCount}";
            skillButtonByHeroId.Clear();
            for (int index = 0; index < skillSlots.Length; index += 1)
            {
                Button button = skillSlots[index];
                int source = skillPage * skillSlots.Length + index;
                button.style.display = source < characters.Count ? DisplayStyle.Flex : DisplayStyle.None;
                if (source >= characters.Count)
                {
                    button.userData = null;
                    continue;
                }
                WIBattleCharacterState character = characters[source];
                WIBattleSkillDefinition skill = battleController.Config.GetCharacterSkill(character.HeroId, character.HeroClass);
                button.userData = character.HeroId;
                button.tooltip = $"{character.DisplayName}\n{skill.Description}\n마나 {skill.ManaCost} · 범위 {skill.Range:0.#} · 재사용 {skill.Cooldown:0.#}초";
                skillButtonByHeroId[character.HeroId] = button;
            }
        }

        // 매 프레임 스킬 비용·대기시간과 사용 불가 이유를 버튼에 갱신합니다.
        private void RefreshSkillButtons(WIBattleRuntimeState runtime)
        {
            foreach (KeyValuePair<string, Button> pair in skillButtonByHeroId)
            {
                WIBattleCharacterState character = runtime.Characters.Find(item => item.HeroId == pair.Key);
                WIBattleSkillDefinition skill = character == null ? null : battleController.Config.GetCharacterSkill(pair.Key, character.HeroClass);
                string reason = WIBattleSimulation.GetHeroSkillUnavailableReason(
                    battleController.Config, runtime, pair.Key, out _, out _);
                pair.Value.SetEnabled(string.IsNullOrEmpty(reason));
                pair.Value.text = string.IsNullOrEmpty(reason)
                    ? $"{skill.DisplayName}\nMP {skill.ManaCost}"
                    : $"{skill.DisplayName}\n{reason}";
            }
        }
    }
}

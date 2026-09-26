using System.Collections.Generic;
using System.Linq;
using ProjectWI.Administration;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectWI.Battle
{
    [RequireComponent(typeof(UIDocument))]
    public partial class WIBattleHUDController : MonoBehaviour
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
        // 명령 버튼 이름표에 사용하는 문자열 UID입니다.
        private static readonly Dictionary<WIBattleCommand, string> CommandLabelUids = new Dictionary<WIBattleCommand, string>
        {
            { WIBattleCommand.Advance, "UI_BATTLE_BUTTON_ADVANCE" },
            { WIBattleCommand.Hold, "UI_BATTLE_BUTTON_HOLD" },
            { WIBattleCommand.Focus, "UI_BATTLE_BUTTON_FOCUS" },
            { WIBattleCommand.Protect, "UI_BATTLE_BUTTON_PROTECT" },
            { WIBattleCommand.Spread, "UI_BATTLE_BUTTON_SPREAD" },
            { WIBattleCommand.Rally, "UI_BATTLE_BUTTON_RALLY" },
            { WIBattleCommand.Retreat, "UI_BATTLE_BUTTON_RETREAT" }
        };
        // 명령 선택 안내에 사용하는 문자열 UID입니다.
        private static readonly Dictionary<WIBattleCommand, string> CommandFeedbackUids = new Dictionary<WIBattleCommand, string>
        {
            { WIBattleCommand.Advance, "UI_BATTLE_COMMAND_ADVANCE" },
            { WIBattleCommand.Hold, "UI_BATTLE_COMMAND_HOLD" },
            { WIBattleCommand.Protect, "UI_BATTLE_COMMAND_PROTECT" },
            { WIBattleCommand.Spread, "UI_BATTLE_COMMAND_SPREAD" },
            { WIBattleCommand.Rally, "UI_BATTLE_COMMAND_RALLY" },
            { WIBattleCommand.Retreat, "UI_BATTLE_COMMAND_RETREAT" }
        };
        // 데이터베이스 문구를 고정 라벨에 한 번 적용했는지 여부입니다.
        private bool staticTextsApplied;

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
                        HandleSkillButton(heroId);
                    }
                };
            }
            RegisterCommandButton(root, "advance-command", WIBattleCommand.Advance);
            RegisterCommandButton(root, "hold-command", WIBattleCommand.Hold);
            RegisterCommandButton(root, "focus-command", WIBattleCommand.Focus);
            RegisterCommandButton(root, "protect-command", WIBattleCommand.Protect);
            RegisterCommandButton(root, "spread-command", WIBattleCommand.Spread);
            RegisterCommandButton(root, "rally-command", WIBattleCommand.Rally);
            RegisterCommandButton(root, "retreat-command", WIBattleCommand.Retreat);
            root.focusable = true;
            root.RegisterCallback<KeyDownEvent>(HandleKeyboardCommand, TrickleDown.TrickleDown);
            root.RegisterCallback<KeyUpEvent>(HandleKeyboardRelease, TrickleDown.TrickleDown);
            root.RegisterCallback<WheelEvent>(HandleMouseWheel, TrickleDown.TrickleDown);
            BindSelectionInput(root);
            BindDeploymentInput(root);
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
                return;
            }
            ApplyStaticTexts();
            RefreshDeploymentState();
            int attackers = runtime.Characters.Count(item => item.Side == WIBattleSide.Attacker && item.IsAlive);
            int defenders = runtime.Characters.Count(item => item.Side == WIBattleSide.Defender && item.IsAlive);
            statusLabel.text = Text("UI_BATTLE_STATUS", runtime.ObjectiveName, runtime.ElapsedSeconds, attackers, defenders);
            if (string.IsNullOrEmpty(battleController.ReinforcementStatus) == false)
            {
                statusLabel.text += "\n" + battleController.ReinforcementStatus;
            }
            BuildSkillButtons(runtime);
            RefreshSkillButtons(runtime);
            RefreshSkillTargeting();
            RefreshRoutingNotices(runtime);
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
                    commandFeedbackLabel.text = Text("UI_BATTLE_RETREAT_CONFIRM");
                    commandButtons[WIBattleCommand.Retreat].AddToClassList("retreat-armed");
                    return;
                }
            }

            string focusHeroId = command == WIBattleCommand.Focus ? SelectFocusTarget(battleController.Runtime) : string.Empty;
            bool squadOnly = HasSquadSelection == true && command != WIBattleCommand.Retreat;
            if (squadOnly == true)
            {
                battleController.SetSquadCommand(selectedSquadIds, command, focusHeroId);
            }
            else
            {
                battleController.SetCommand(playerSide, command, focusHeroId);
            }
            retreatConfirmUntil = 0f;
            commandButtons[WIBattleCommand.Retreat].RemoveFromClassList("retreat-armed");
            string feedback = GetCommandFeedback(command, focusHeroId, battleController.Runtime);
            commandFeedbackLabel.text = squadOnly == true
                ? Text("UI_BATTLE_COMMAND_SQUADS", feedback, selectedSquadIds.Count)
                : feedback;
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
            List<WIBattleCharacterState> allies = runtime.Characters.Where(item => item.Side == playerSide && item.IsAlive &&
                (HasSquadSelection == false || selectedSquadIds.Contains(item.SquadId) == true)).ToList();
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
        private string GetCommandFeedback(WIBattleCommand command, string focusHeroId, WIBattleRuntimeState runtime)
        {
            if (CommandFeedbackUids.TryGetValue(command, out string uid) == true)
            {
                return Text(uid);
            }
            WIBattleCharacterState target = runtime.Characters.Find(item => item.HeroId == focusHeroId);
            return target == null ? Text("UI_BATTLE_COMMAND_FOCUS_NONE") : Text("UI_BATTLE_COMMAND_FOCUS", target.DisplayName);
        }

        // 데이터베이스 UID 문구를 현재 인자로 채워 반환하며 데이터베이스가 없으면 UID를 그대로 반환합니다.
        private string Text(string uid, params object[] args)
        {
            WIAdministrationDatabaseSO database = battleController == null ? null : battleController.Database;
            if (database == null)
            {
                return uid;
            }
            string format = database.GetText(uid);
            return args.Length == 0 ? format : string.Format(format, args);
        }

        // UXML 기본 문구를 데이터베이스의 명령 버튼·안내 UID 문구로 한 번 교체합니다.
        private void ApplyStaticTexts()
        {
            if (staticTextsApplied == true || battleController.Database == null)
            {
                return;
            }
            staticTextsApplied = true;
            foreach (KeyValuePair<WIBattleCommand, Button> pair in commandButtons)
            {
                pair.Value.text = Text(CommandLabelUids[pair.Key]);
            }
            commandFeedbackLabel.text = Text("UI_BATTLE_COMMAND_HINT");
            selectionInfoLabel.text = Text("UI_BATTLE_SELECTION_HINT");
        }

        // 현재 명령 버튼을 금색으로 강조하고 후퇴 확인 제한 시간이 지나면 경고 상태를 해제합니다.
        private void RefreshCommandFeedback(WIBattleRuntimeState runtime)
        {
            WIBattleCommand current = playerSide == WIBattleSide.Attacker ? runtime.AttackerCommand : runtime.DefenderCommand;
            if (HasSquadSelection == true && current != WIBattleCommand.Retreat)
            {
                WIBattleSquadState squad = WIBattleSimulation.FindSquad(runtime, selectedSquadIds.Min());
                if (squad != null && squad.HasCommandOverride == true)
                {
                    current = squad.Command;
                }
            }
            foreach (KeyValuePair<WIBattleCommand, Button> pair in commandButtons)
            {
                pair.Value.EnableInClassList("active-command", pair.Key == current);
            }
            if (retreatConfirmUntil > 0f && Time.unscaledTime > retreatConfirmUntil)
            {
                retreatConfirmUntil = 0f;
                commandButtons[WIBattleCommand.Retreat].RemoveFromClassList("retreat-armed");
                commandFeedbackLabel.text = Text("UI_BATTLE_RETREAT_CANCELLED");
            }
        }

        // PC 키보드의 명령키(Z~N·R), 분대 선택(1~9·`·Esc), 일시정지(Space)와 카메라 키를 연결합니다.
        private void HandleKeyboardCommand(KeyDownEvent keyboardEvent)
        {
            KeyCode key = keyboardEvent.keyCode;
            bool additive = keyboardEvent.shiftKey == true;
            if (key == KeyCode.Z) SetCommand(WIBattleCommand.Advance);
            else if (key == KeyCode.X) SetCommand(WIBattleCommand.Hold);
            else if (key == KeyCode.C) SetCommand(WIBattleCommand.Focus);
            else if (key == KeyCode.V) SetCommand(WIBattleCommand.Protect);
            else if (key == KeyCode.B) SetCommand(WIBattleCommand.Spread);
            else if (key == KeyCode.N) SetCommand(WIBattleCommand.Rally);
            else if (key == KeyCode.R) SetCommand(WIBattleCommand.Retreat);
            else if (key >= KeyCode.Alpha1 && key <= KeyCode.Alpha9) SelectSquadByIndex(key - KeyCode.Alpha1, additive);
            else if (key == KeyCode.BackQuote) SelectAllSquads();
            else if (key == KeyCode.Escape && IsTargetingSkill == true) CancelSkillTargeting();
            else if (key == KeyCode.Escape) ClearSquadSelection();
            else if (key == KeyCode.Space) TogglePause();
            else if (key == KeyCode.Return || key == KeyCode.KeypadEnter) StartBattleFromDeployment();
            else if (key == KeyCode.F) ToggleHeroControl();
            else if (key == KeyCode.Q) CastControlledHeroSkill();
            else if (key == KeyCode.Home)
            {
                battleController?.CameraController?.ResetView();
                selectionInfoLabel.text = Text("UI_BATTLE_CAMERA_RESET");
            }
            else if (IsCameraMoveKey(key))
            {
                battleController?.CameraController?.SetMoveKey(key, true);
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
            battleController?.CameraController?.Zoom(Mathf.Sign(wheelEvent.delta.y), PanelToScreen(wheelEvent.mousePosition));
            wheelEvent.StopPropagation();
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
                .Where(item => item.Side == playerSide && battleController.Config.GetHeroSkill(item.HeroId) != null)
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
                WIBattleSkillDefinition skill = battleController.Config.GetHeroSkill(character.HeroId);
                button.userData = character.HeroId;
                button.tooltip = Text("UI_BATTLE_SKILL_TOOLTIP", character.DisplayName, skill.Description, skill.ManaCost, skill.Range, skill.Cooldown);
                skillButtonByHeroId[character.HeroId] = button;
            }
        }

        // 스킬 사용 가능 여부 사유를 버튼 하단 문구로 변환합니다.
        private string GetSkillStateText(WIBattleSkillBlockReason reason, WIBattleCharacterState caster, WIBattleSkillDefinition skill)
        {
            if (reason == WIBattleSkillBlockReason.None) return Text("UI_BATTLE_SKILL_READY", skill.ManaCost);
            if (reason == WIBattleSkillBlockReason.NotOnField) return Text("UI_BATTLE_SKILL_NOT_ON_FIELD");
            if (reason == WIBattleSkillBlockReason.NoSkill) return Text("UI_BATTLE_SKILL_NONE");
            if (reason == WIBattleSkillBlockReason.Incapacitated) return Text("UI_BATTLE_SKILL_INCAPACITATED");
            if (reason == WIBattleSkillBlockReason.Cooldown) return Text("UI_BATTLE_SKILL_COOLDOWN", caster.SkillCooldownRemaining);
            return Text("UI_BATTLE_SKILL_MANA", caster.Mana, skill.ManaCost);
        }

        // 매 프레임 스킬 비용·대기시간과 사용 불가 이유를 버튼에 갱신합니다.
        private void RefreshSkillButtons(WIBattleRuntimeState runtime)
        {
            foreach (KeyValuePair<string, Button> pair in skillButtonByHeroId)
            {
                WIBattleCharacterState character = runtime.Characters.Find(item => item.HeroId == pair.Key);
                WIBattleSkillDefinition skill = character == null ? null : battleController.Config.GetHeroSkill(pair.Key);
                WIBattleSkillBlockReason reason = WIBattleSimulation.GetHeroSkillBlockReason(
                    battleController.Config, runtime, pair.Key, out WIBattleCharacterState caster, out _);
                pair.Value.SetEnabled(reason == WIBattleSkillBlockReason.None);
                pair.Value.text = skill == null
                    ? Text("UI_BATTLE_SKILL_NONE")
                    : Text("UI_BATTLE_SKILL_BUTTON", skill.DisplayName, GetSkillStateText(reason, caster, skill));
            }
        }
    }
}

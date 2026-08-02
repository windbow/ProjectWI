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
        private VisualElement skillButtons;
        private readonly HashSet<string> builtSkillHeroes = new HashSet<string>();

        // 전투 세션에서 확인한 플레이어 진영을 HUD 명령 대상으로 지정합니다.
        public void SetPlayerSide(WIBattleSide side)
        {
            playerSide = side;
        }

        // 전투 HUD 요소와 부대 명령 버튼을 런타임 컨트롤러에 연결합니다.
        private void Awake()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            statusLabel = root.Q<Label>("battle-status");
            skillButtons = root.Q<VisualElement>("skill-buttons");
            root.Q<Button>("advance-command").clicked += () => SetCommand(WIBattleCommand.Advance);
            root.Q<Button>("hold-command").clicked += () => SetCommand(WIBattleCommand.Hold);
            root.Q<Button>("focus-command").clicked += () => SetCommand(WIBattleCommand.Focus);
            root.Q<Button>("retreat-command").clicked += () => SetCommand(WIBattleCommand.Retreat);
        }

        // 현재 생존 인원과 전투 시간을 표시하고 영웅 스킬 버튼을 보충합니다.
        private void Update()
        {
            WIBattleRuntimeState runtime = battleController == null ? null : battleController.Runtime;
            if (runtime == null)
            {
                statusLabel.text = "전투 세션 대기 중";
                return;
            }
            int attackers = runtime.Characters.Count(item => item.Side == WIBattleSide.Attacker && item.IsAlive);
            int defenders = runtime.Characters.Count(item => item.Side == WIBattleSide.Defender && item.IsAlive);
            statusLabel.text = $"{runtime.ElapsedSeconds:0.0}초 · 공격 {attackers} / 수비 {defenders}";
            BuildSkillButtons(runtime);
        }

        // 선택한 부대 명령을 플레이어 진영에 적용합니다.
        private void SetCommand(WIBattleCommand command)
        {
            battleController?.SetCommand(playerSide, command);
        }

        // 플레이어 진영 영웅마다 재사용 가능한 액티브 스킬 버튼을 생성합니다.
        private void BuildSkillButtons(WIBattleRuntimeState runtime)
        {
            foreach (WIBattleCharacterState character in runtime.Characters.Where(item => item.Side == playerSide))
            {
                if (builtSkillHeroes.Add(character.HeroId) == false)
                {
                    continue;
                }
                Button button = new Button(() => battleController.TryActivateHeroSkill(character.HeroId));
                button.text = $"{character.HeroId} 스킬";
                button.AddToClassList("skill-button");
                skillButtons.Add(button);
            }
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace ProjectWI.Battle
{
    public partial class WIBattleHUDController
    {
        // 위치 지정 중인 스킬 시전 영웅 ID이며 비어 있으면 지정 모드가 아닙니다.
        private string targetingHeroId = string.Empty;
        // 마지막으로 확인한 전장 위 포인터 패널 좌표입니다.
        private Vector2 lastPointerPanelPosition;
        // 직전 프레임에 무너져 있던 플레이어 분대 번호 목록입니다.
        private readonly HashSet<int> routingSquadIds = new HashSet<int>();

        // 스킬 위치 지정 모드인지 여부입니다.
        private bool IsTargetingSkill => string.IsNullOrEmpty(targetingHeroId) == false;

        // 스킬 버튼을 누르면 즉시 시전하거나 위치 지정이 필요한 스킬은 지정 모드로 들어갑니다.
        private void HandleSkillButton(string heroId)
        {
            WIBattleSkillDefinition skill = battleController.Config.GetHeroSkill(heroId);
            if (skill == null)
            {
                return;
            }
            if (skill.RequiresTarget == false)
            {
                CancelSkillTargeting();
                battleController.TryActivateHeroSkill(heroId);
                return;
            }
            WIBattleSkillBlockReason reason = WIBattleSimulation.GetHeroSkillBlockReason(
                battleController.Config, battleController.Runtime, heroId, out WIBattleCharacterState caster, out _);
            if (reason != WIBattleSkillBlockReason.None)
            {
                commandFeedbackLabel.text = GetSkillStateText(reason, caster, skill);
                return;
            }
            targetingHeroId = heroId;
            commandFeedbackLabel.text = Text("UI_BATTLE_SKILL_TARGETING", skill.DisplayName);
        }

        // 지정 모드 중 포인터 위치를 따라 시전 거리와 효과 범위를 미리 보여 줍니다.
        private void RefreshSkillTargeting()
        {
            if (IsTargetingSkill == false)
            {
                return;
            }
            WIBattleSkillBlockReason reason = WIBattleSimulation.GetHeroSkillBlockReason(
                battleController.Config, battleController.Runtime, targetingHeroId, out WIBattleCharacterState caster, out WIBattleSkillDefinition skill);
            if (reason != WIBattleSkillBlockReason.None || battleController.CameraController == null)
            {
                CancelSkillTargeting();
                return;
            }
            Vector2 world = battleController.CameraController.ScreenToBattlePosition(PanelToScreen(lastPointerPanelPosition));
            battleController.ShowSkillPreview(caster.Position, skill.Range, world, skill.AreaRadius);
        }

        // 지정한 전장 좌표에 스킬을 시전하고 지정 모드를 끝냅니다.
        private void CastTargetedSkill(Vector2 worldPosition)
        {
            string heroId = targetingHeroId;
            CancelSkillTargeting();
            if (battleController.TryActivateHeroSkill(heroId, worldPosition) == true)
            {
                commandFeedbackLabel.text = Text("UI_BATTLE_SKILL_CAST", battleController.Config.GetHeroSkill(heroId)?.DisplayName);
            }
        }

        // 스킬 위치 지정 모드와 범위 미리보기를 해제합니다.
        private void CancelSkillTargeting()
        {
            targetingHeroId = string.Empty;
            battleController?.HideSkillPreview();
        }

        // 플레이어 분대가 새로 무너지거나 전선에 복귀하면 안내합니다.
        private void RefreshRoutingNotices(WIBattleRuntimeState runtime)
        {
            foreach (WIBattleSquadState squad in runtime.Squads)
            {
                if (squad.Side != playerSide)
                {
                    continue;
                }
                string leaderName = runtime.Characters.Find(item => item.HeroId == squad.LeaderHeroId)?.DisplayName ?? squad.SquadId.ToString();
                if (squad.IsRouting == true && routingSquadIds.Add(squad.SquadId) == true)
                {
                    commandFeedbackLabel.text = Text("UI_BATTLE_SQUAD_ROUTING", leaderName);
                }
                else if (squad.IsRouting == false && routingSquadIds.Remove(squad.SquadId) == true)
                {
                    commandFeedbackLabel.text = Text("UI_BATTLE_SQUAD_RALLIED", leaderName);
                }
            }
        }
    }
}

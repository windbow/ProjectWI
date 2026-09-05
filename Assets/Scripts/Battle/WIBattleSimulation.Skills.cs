using System.Linq;
using UnityEngine;

namespace ProjectWI.Battle
{
    public static partial class WIBattleSimulation
    {
        // 영웅의 마나와 재사용 대기시간을 확인하고 설정된 액티브 스킬을 적용합니다.
        public static bool TryActivateHeroSkill(
            WIBattleConfigSO config,
            WIBattleRuntimeState runtime,
            string heroId)
        {
            if (GetHeroSkillUnavailableReason(config, runtime, heroId, out WIBattleCharacterState caster, out WIBattleSkillDefinition skill) != string.Empty)
            {
                return false;
            }
            caster.Mana -= skill.ManaCost;
            caster.SkillCooldownRemaining = skill.Cooldown;
            if (skill.SkillType == WIBattleSkillType.HealAllies)
            {
                foreach (WIBattleCharacterState ally in runtime.Characters.Where(item =>
                    item.Side == caster.Side && item.IsAlive && Vector2.Distance(item.Position, caster.Position) <= skill.Range))
                {
                    ally.Health = Mathf.Min(ally.MaxHealth, ally.Health + skill.Power);
                }
                AddSkillVisual(config, runtime, caster, skill, WIBattleVisualEffectType.SkillHeal);
            }
            else if (skill.SkillType == WIBattleSkillType.CommandBuff)
            {
                foreach (WIBattleCharacterState ally in runtime.Characters.Where(item =>
                    item.Side == caster.Side && item.IsAlive && Vector2.Distance(item.Position, caster.Position) <= skill.Range))
                {
                    ally.CooldownRemaining = Mathf.Max(0f, ally.CooldownRemaining - skill.Power / 100f);
                }
                AddSkillVisual(config, runtime, caster, skill, WIBattleVisualEffectType.SkillCommand);
            }
            else
            {
                foreach (WIBattleCharacterState enemy in runtime.Characters.Where(item =>
                    item.Side != caster.Side && item.IsAlive && Vector2.Distance(item.Position, caster.Position) <= skill.Range))
                {
                    enemy.Health = Mathf.Max(0, enemy.Health - skill.Power);
                }
                AddSkillVisual(config, runtime, caster, skill, WIBattleVisualEffectType.SkillDamage);
            }
            return true;
        }

        // 영웅 스킬을 사용할 수 없는 이유와 함께 시전자·정의를 반환합니다.
        public static string GetHeroSkillUnavailableReason(
            WIBattleConfigSO config,
            WIBattleRuntimeState runtime,
            string heroId,
            out WIBattleCharacterState caster,
            out WIBattleSkillDefinition skill)
        {
            caster = runtime?.Characters.Find(item => item.HeroId == heroId);
            skill = caster == null ? null : config?.GetCharacterSkill(heroId, caster.HeroClass);
            if (caster == null)
            {
                return "전장에 없는 영웅";
            }
            if (skill == null)
            {
                return "배정된 스킬 없음";
            }
            if (caster.IsAlive == false)
            {
                return "전투 불능";
            }
            if (caster.SkillCooldownRemaining > 0f)
            {
                return $"재사용 {caster.SkillCooldownRemaining:0.0}초";
            }
            if (caster.Mana < skill.ManaCost)
            {
                return $"마나 부족 {caster.Mana}/{skill.ManaCost}";
            }
            return string.Empty;
        }

        // 스킬 종류와 범위를 색상 도형으로 확인할 수 있도록 범위 효과를 예약합니다.
        private static void AddSkillVisual(
            WIBattleConfigSO config,
            WIBattleRuntimeState runtime,
            WIBattleCharacterState caster,
            WIBattleSkillDefinition skill,
            WIBattleVisualEffectType effectType)
        {
            runtime.VisualEffects.Add(new WIBattleVisualEffectState
            {
                EffectId = runtime.NextVisualEffectId,
                EffectType = effectType,
                Side = caster.Side,
                StartPosition = caster.Position,
                EndPosition = caster.Position,
                StartedAt = runtime.ElapsedSeconds,
                Duration = config.SkillVisualDuration,
                Radius = skill.Range
            });
            runtime.NextVisualEffectId += 1;
        }
    }
}

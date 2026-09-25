using System.Linq;
using UnityEngine;

namespace ProjectWI.Battle
{
    // 영웅 스킬을 사용할 수 없는 이유입니다.
    public enum WIBattleSkillBlockReason
    {
        None,
        NotOnField,
        NoSkill,
        Incapacitated,
        Cooldown,
        NotEnoughMana
    }

    public static partial class WIBattleSimulation
    {
        // 영웅의 마나와 재사용 대기시간을 확인하고 설정된 액티브 스킬을 적용합니다.
        public static bool TryActivateHeroSkill(
            WIBattleConfigSO config,
            WIBattleRuntimeState runtime,
            string heroId,
            Vector2? targetPoint = null)
        {
            if (GetHeroSkillBlockReason(config, runtime, heroId, out WIBattleCharacterState caster, out WIBattleSkillDefinition skill) != WIBattleSkillBlockReason.None)
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
                Vector2 center = targetPoint.HasValue == true
                    ? ClampSkillTarget(caster.Position, targetPoint.Value, skill.Range)
                    : caster.Position;
                float radius = targetPoint.HasValue == true ? skill.AreaRadius : skill.Range;
                foreach (WIBattleCharacterState enemy in runtime.Characters.Where(item =>
                    item.Side != caster.Side && item.IsAlive && Vector2.Distance(item.Position, center) <= radius).ToList())
                {
                    ApplyDamage(config, runtime, enemy, skill.Power, 1f);
                    AddHitVisual(config, runtime, enemy);
                }
                AddSkillVisual(config, runtime, caster, skill, WIBattleVisualEffectType.SkillDamage, center, radius);
            }
            return true;
        }

        // 지정 위치가 시전 가능 거리를 넘으면 시전자 방향으로 거리 안까지 당긴 위치를 반환합니다.
        public static Vector2 ClampSkillTarget(Vector2 casterPosition, Vector2 targetPoint, float castRange)
        {
            Vector2 offset = targetPoint - casterPosition;
            return offset.magnitude <= castRange ? targetPoint : casterPosition + offset.normalized * castRange;
        }

        // 영웅 스킬을 사용할 수 없는 이유와 함께 시전자·정의를 반환합니다. 표시 문구는 HUD가 문자열 UID로 변환합니다.
        public static WIBattleSkillBlockReason GetHeroSkillBlockReason(
            WIBattleConfigSO config,
            WIBattleRuntimeState runtime,
            string heroId,
            out WIBattleCharacterState caster,
            out WIBattleSkillDefinition skill)
        {
            caster = runtime?.Characters.Find(item => item.HeroId == heroId);
            skill = caster == null ? null : config?.GetHeroSkill(heroId);
            if (caster == null)
            {
                return WIBattleSkillBlockReason.NotOnField;
            }
            if (skill == null)
            {
                return WIBattleSkillBlockReason.NoSkill;
            }
            if (caster.IsAlive == false)
            {
                return WIBattleSkillBlockReason.Incapacitated;
            }
            if (caster.SkillCooldownRemaining > 0f)
            {
                return WIBattleSkillBlockReason.Cooldown;
            }
            if (caster.Mana < skill.ManaCost)
            {
                return WIBattleSkillBlockReason.NotEnoughMana;
            }
            return WIBattleSkillBlockReason.None;
        }

        // 스킬 종류와 범위를 색상 도형으로 확인할 수 있도록 범위 효과를 예약합니다.
        private static void AddSkillVisual(
            WIBattleConfigSO config,
            WIBattleRuntimeState runtime,
            WIBattleCharacterState caster,
            WIBattleSkillDefinition skill,
            WIBattleVisualEffectType effectType)
        {
            AddSkillVisual(config, runtime, caster, skill, effectType, caster.Position, skill.Range);
        }

        // 지정한 중심과 반경으로 스킬 범위 효과를 예약합니다.
        private static void AddSkillVisual(
            WIBattleConfigSO config,
            WIBattleRuntimeState runtime,
            WIBattleCharacterState caster,
            WIBattleSkillDefinition skill,
            WIBattleVisualEffectType effectType,
            Vector2 center,
            float radius)
        {
            runtime.VisualEffects.Add(new WIBattleVisualEffectState
            {
                EffectId = runtime.NextVisualEffectId,
                EffectType = effectType,
                Side = caster.Side,
                StartPosition = caster.Position,
                EndPosition = center,
                StartedAt = runtime.ElapsedSeconds,
                Duration = config.SkillVisualDuration,
                Radius = radius
            });
            runtime.NextVisualEffectId += 1;
        }
    }
}

using System.Linq;
using ProjectWI.Administration;
using UnityEngine;

namespace ProjectWI.Battle
{
    public static class WIBattleSimulation
    {
        // 한 프레임의 표적 탐색, 이동, 공격과 승패 판정을 처리합니다.
        public static WIBattleOutcome Step(WIBattleConfigSO config, WIBattleRuntimeState runtime, float deltaTime)
        {
            if (runtime == null || runtime.Finished)
            {
                return runtime == null ? WIBattleOutcome.None : runtime.AttackerOutcome;
            }
            if (runtime.AttackerCommand == WIBattleCommand.Retreat || runtime.DefenderCommand == WIBattleCommand.Retreat)
            {
                runtime.Finished = true;
                runtime.AttackerOutcome = runtime.AttackerCommand == WIBattleCommand.Retreat
                    ? WIBattleOutcome.Defeat
                    : WIBattleOutcome.Victory;
                return runtime.AttackerOutcome;
            }
            runtime.ElapsedSeconds += deltaTime;
            foreach (WIBattleCharacterState actor in runtime.Characters.Where(item => item.IsAlive).ToList())
            {
                WIBattleCharacterState target = FindTarget(runtime, actor);
                if (target == null) continue;
                actor.CooldownRemaining = Mathf.Max(0f, actor.CooldownRemaining - deltaTime);
                actor.SkillCooldownRemaining = Mathf.Max(0f, actor.SkillCooldownRemaining - deltaTime);
                float distance = Vector2.Distance(actor.Position, target.Position);
                if (distance > actor.AttackRange)
                {
                    WIBattleCommand command = GetCommand(runtime, actor.Side);
                    if (command == WIBattleCommand.Hold)
                    {
                        actor.Position = Vector2.MoveTowards(
                            actor.Position,
                            actor.FormationPosition,
                            config.FormationReturnSpeed * deltaTime);
                    }
                    else
                    {
                        float speed = command == WIBattleCommand.Advance
                            ? actor.MoveSpeed * config.AdvanceSpeedMultiplier
                            : actor.MoveSpeed;
                        Vector2 attackApproach = new Vector2(target.Position.x, actor.FormationPosition.y);
                        actor.Position = Vector2.MoveTowards(actor.Position, attackApproach, speed * deltaTime);
                    }
                }
                else if (actor.CooldownRemaining <= 0f)
                {
                    int damage = actor.AttackDamage;
                    if (GetCommand(runtime, actor.Side) == WIBattleCommand.Focus)
                    {
                        damage = Mathf.RoundToInt(damage * config.FocusDamageMultiplier);
                    }
                    if (GetCommand(runtime, target.Side) == WIBattleCommand.Hold)
                    {
                        damage = Mathf.RoundToInt(damage * (1f - config.HoldDamageReduction));
                    }
                    target.Health = Mathf.Max(0, target.Health - Mathf.Max(1, damage));
                    ApplyMeleeKnockback(config, runtime, actor, target);
                    actor.CooldownRemaining = config.AttackCooldown;
                }
            }
            ResolveCharacterCollisions(config, runtime);
            bool attackersAlive = runtime.Characters.Any(item => item.Side == WIBattleSide.Attacker && item.IsAlive);
            bool defendersAlive = runtime.Characters.Any(item => item.Side == WIBattleSide.Defender && item.IsAlive);
            if (attackersAlive && defendersAlive)
            {
                return WIBattleOutcome.None;
            }
            runtime.Finished = true;
            runtime.AttackerOutcome = attackersAlive ? WIBattleOutcome.Victory : WIBattleOutcome.Defeat;
            return runtime.AttackerOutcome;
        }

        // 근접 공격에 맞은 대상을 공격자 반대 방향으로 밀어내며 사수 진형은 밀치기를 완화합니다.
        private static void ApplyMeleeKnockback(
            WIBattleConfigSO config,
            WIBattleRuntimeState runtime,
            WIBattleCharacterState actor,
            WIBattleCharacterState target)
        {
            if (actor.AttackRange > config.MeleeRange + 0.01f || target.IsAlive == false)
            {
                return;
            }
            Vector2 direction = target.Position - actor.Position;
            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = actor.Side == WIBattleSide.Attacker ? Vector2.right : Vector2.left;
            }
            float resistance = GetCommand(runtime, target.Side) == WIBattleCommand.Hold
                ? config.HoldKnockbackResistance
                : 0f;
            target.Position += direction.normalized * config.MeleeKnockbackDistance * (1f - resistance);
            ClampToArena(config, target);
        }

        // 살아 있는 모든 인물의 겹침을 해소해 아군 진형과 적 전선이 한 점에 포개지지 않게 합니다.
        private static void ResolveCharacterCollisions(WIBattleConfigSO config, WIBattleRuntimeState runtime)
        {
            var characters = runtime.Characters.Where(item => item.IsAlive).ToList();
            for (int leftIndex = 0; leftIndex < characters.Count; leftIndex += 1)
            {
                for (int rightIndex = leftIndex + 1; rightIndex < characters.Count; rightIndex += 1)
                {
                    WIBattleCharacterState left = characters[leftIndex];
                    WIBattleCharacterState right = characters[rightIndex];
                    Vector2 delta = right.Position - left.Position;
                    float distance = delta.magnitude;
                    if (distance >= config.MinimumUnitSpacing)
                    {
                        continue;
                    }
                    Vector2 direction = distance > 0.0001f
                        ? delta / distance
                        : GetFallbackSeparationDirection(left, right, leftIndex, rightIndex);
                    float correction = (config.MinimumUnitSpacing - distance) * config.CollisionResolveStrength * 0.5f;
                    left.Position -= direction * correction;
                    right.Position += direction * correction;
                    ClampToArena(config, left);
                    ClampToArena(config, right);
                }
            }
        }

        // 완전히 같은 위치에 있는 두 인물도 결정적으로 분리할 방향을 반환합니다.
        private static Vector2 GetFallbackSeparationDirection(
            WIBattleCharacterState left,
            WIBattleCharacterState right,
            int leftIndex,
            int rightIndex)
        {
            if (left.Side != right.Side)
            {
                return left.Side == WIBattleSide.Attacker ? Vector2.right : Vector2.left;
            }
            return ((leftIndex + rightIndex) & 1) == 0 ? Vector2.up : Vector2.down;
        }

        // 밀치기와 충돌 해소 후 인물 위치를 전장 경계 안으로 제한합니다.
        private static void ClampToArena(WIBattleConfigSO config, WIBattleCharacterState character)
        {
            Vector2 halfSize = config.ArenaSize * 0.5f;
            character.Position = new Vector2(
                Mathf.Clamp(character.Position.x, -halfSize.x, halfSize.x),
                Mathf.Clamp(character.Position.y, -halfSize.y, halfSize.y));
        }

        // 영웅의 마나와 재사용 대기시간을 확인하고 설정된 액티브 스킬을 적용합니다.
        public static bool TryActivateHeroSkill(
            WIBattleConfigSO config,
            WIBattleRuntimeState runtime,
            string heroId)
        {
            WIBattleCharacterState caster = runtime?.Characters.Find(item => item.HeroId == heroId && item.IsAlive);
            WIBattleSkillDefinition skill = config?.GetHeroSkill(heroId);
            if (caster == null || skill == null || caster.Mana < skill.ManaCost || caster.SkillCooldownRemaining > 0f)
            {
                return false;
            }
            caster.Mana -= skill.ManaCost;
            caster.SkillCooldownRemaining = skill.Cooldown;
            if (skill.SkillType == WIBattleSkillType.HealAllies)
            {
                foreach (WIBattleCharacterState ally in runtime.Characters.Where(item => item.Side == caster.Side && item.IsAlive))
                {
                    ally.Health = Mathf.Min(ally.MaxHealth, ally.Health + skill.Power);
                }
            }
            else if (skill.SkillType == WIBattleSkillType.CommandBuff)
            {
                foreach (WIBattleCharacterState ally in runtime.Characters.Where(item => item.Side == caster.Side && item.IsAlive))
                {
                    ally.CooldownRemaining = Mathf.Max(0f, ally.CooldownRemaining - skill.Power / 100f);
                }
            }
            else
            {
                foreach (WIBattleCharacterState enemy in runtime.Characters.Where(item =>
                    item.Side != caster.Side && item.IsAlive && Vector2.Distance(item.Position, caster.Position) <= skill.Range))
                {
                    enemy.Health = Mathf.Max(0, enemy.Health - skill.Power);
                }
            }
            return true;
        }

        // 집중 목표가 유효하면 우선하고 없으면 가장 가까운 적을 반환합니다.
        private static WIBattleCharacterState FindTarget(WIBattleRuntimeState runtime, WIBattleCharacterState actor)
        {
            string focusHeroId = actor.Side == WIBattleSide.Attacker
                ? runtime.AttackerFocusHeroId
                : runtime.DefenderFocusHeroId;
            if (GetCommand(runtime, actor.Side) == WIBattleCommand.Focus && string.IsNullOrEmpty(focusHeroId) == false)
            {
                WIBattleCharacterState focus = runtime.Characters.Find(item => item.HeroId == focusHeroId && item.IsAlive && item.Side != actor.Side);
                if (focus != null) return focus;
            }
            return runtime.Characters
                .Where(item => item.IsAlive && item.Side != actor.Side)
                .OrderBy(item => Vector2.SqrMagnitude(item.Position - actor.Position))
                .FirstOrDefault();
        }

        // 진영에 현재 지정된 부대 명령을 반환합니다.
        private static WIBattleCommand GetCommand(WIBattleRuntimeState runtime, WIBattleSide side)
        {
            return side == WIBattleSide.Attacker ? runtime.AttackerCommand : runtime.DefenderCommand;
        }
    }
}

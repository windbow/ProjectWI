using System.Collections.Generic;
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
            WIBattleOutcome objectiveOutcome = EvaluateObjective(runtime, deltaTime);
            if (objectiveOutcome != WIBattleOutcome.None)
            {
                runtime.Finished = true;
                runtime.AttackerOutcome = objectiveOutcome;
                return objectiveOutcome;
            }
            runtime.VisualEffects.RemoveAll(item => runtime.ElapsedSeconds - item.StartedAt >= item.Duration);
            AdvanceProjectiles(config, runtime, deltaTime);
            List<WIBattleCharacterState> activeActors = new List<WIBattleCharacterState>(runtime.Characters.Count);
            foreach (WIBattleCharacterState character in runtime.Characters)
            {
                if (character.IsAlive) activeActors.Add(character);
            }
            foreach (WIBattleCharacterState actor in activeActors)
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
                    if (UsesProjectile(actor))
                    {
                        LaunchProjectile(config, runtime, actor, target, Mathf.Max(1, damage));
                    }
                    else
                    {
                        AddMeleeAttackVisual(config, runtime, actor, target);
                        target.Health = Mathf.Max(0, target.Health - Mathf.Max(1, damage));
                        ApplyMeleeKnockback(config, runtime, actor, target);
                    }
                    actor.CooldownRemaining = config.AttackCooldown;
                }
            }
            ResolveCharacterCollisions(config, runtime);
            bool attackersAlive = false;
            bool defendersAlive = false;
            foreach (WIBattleCharacterState character in runtime.Characters)
            {
                if (character.IsAlive == false) continue;
                if (character.Side == WIBattleSide.Attacker) attackersAlive = true;
                else defendersAlive = true;
            }
            if (attackersAlive && defendersAlive)
            {
                return WIBattleOutcome.None;
            }
            runtime.Finished = true;
            runtime.AttackerOutcome = attackersAlive ? WIBattleOutcome.Victory : WIBattleOutcome.Defeat;
            return runtime.AttackerOutcome;
        }

        // 제한 방어 시간과 중앙 거점 점유 시간을 계산해 목표 달성 승패를 반환합니다.
        private static WIBattleOutcome EvaluateObjective(WIBattleRuntimeState runtime, float deltaTime)
        {
            if (runtime.ObjectiveType == WIBattleObjectiveType.TimedDefense &&
                runtime.ElapsedSeconds >= runtime.ObjectiveDurationSeconds)
            {
                return WIBattleOutcome.Defeat;
            }

            if (runtime.ObjectiveType != WIBattleObjectiveType.ControlPoint)
            {
                return WIBattleOutcome.None;
            }

            float radiusSquared = runtime.ControlRadius * runtime.ControlRadius;
            bool attackerPresent = runtime.Characters.Any(character => character.IsAlive &&
                character.Side == WIBattleSide.Attacker && character.Position.sqrMagnitude <= radiusSquared);
            bool defenderPresent = runtime.Characters.Any(character => character.IsAlive &&
                character.Side == WIBattleSide.Defender && character.Position.sqrMagnitude <= radiusSquared);
            if (attackerPresent && defenderPresent == false)
            {
                runtime.AttackerControlSeconds += deltaTime;
            }
            else if (defenderPresent && attackerPresent == false)
            {
                runtime.DefenderControlSeconds += deltaTime;
            }

            if (runtime.AttackerControlSeconds >= runtime.ControlDurationSeconds)
            {
                return WIBattleOutcome.Victory;
            }
            if (runtime.DefenderControlSeconds >= runtime.ControlDurationSeconds)
            {
                return WIBattleOutcome.Defeat;
            }
            return WIBattleOutcome.None;
        }

        // 현재 역할이 실제 이동 발사체를 사용하는 원거리 계열인지 반환합니다.
        private static bool UsesProjectile(WIBattleCharacterState actor)
        {
            return actor.Role == WIUnitRole.Ranged || actor.Role == WIUnitRole.Magic || actor.Role == WIUnitRole.Support;
        }

        // 근접 공격 명중 위치에 짧은 임시 섬광 표시를 예약합니다.
        private static void AddMeleeAttackVisual(
            WIBattleConfigSO config,
            WIBattleRuntimeState runtime,
            WIBattleCharacterState actor,
            WIBattleCharacterState target)
        {
            runtime.VisualEffects.Add(new WIBattleVisualEffectState
            {
                EffectId = runtime.NextVisualEffectId,
                EffectType = WIBattleVisualEffectType.MeleeHit,
                Side = actor.Side,
                StartPosition = actor.Position,
                EndPosition = target.Position,
                StartedAt = runtime.ElapsedSeconds,
                Duration = config.PlaceholderAttackEffectDuration
            });
            runtime.NextVisualEffectId += 1;
        }

        // 원거리 공격을 즉시 피해로 처리하지 않고 충돌 판정을 가진 발사체 상태로 생성합니다.
        private static void LaunchProjectile(
            WIBattleConfigSO config,
            WIBattleRuntimeState runtime,
            WIBattleCharacterState actor,
            WIBattleCharacterState target,
            int damage)
        {
            runtime.Projectiles.Add(new WIBattleProjectileState
            {
                ProjectileId = runtime.NextProjectileId,
                ShooterHeroId = actor.HeroId,
                TargetHeroId = target.HeroId,
                Side = actor.Side,
                Position = actor.Position,
                TargetPosition = target.Position,
                Damage = damage,
                RemainingLifetime = config.ProjectileLifetime
            });
            runtime.NextProjectileId += 1;
        }

        // 모든 발사체를 이동시키고 선분 충돌, 수명 만료와 선택적 아군 오발을 처리합니다.
        private static void AdvanceProjectiles(WIBattleConfigSO config, WIBattleRuntimeState runtime, float deltaTime)
        {
            for (int projectileIndex = runtime.Projectiles.Count - 1; projectileIndex >= 0; projectileIndex -= 1)
            {
                WIBattleProjectileState projectile = runtime.Projectiles[projectileIndex];
                projectile.RemainingLifetime -= deltaTime;
                WIBattleCharacterState target = runtime.Characters.Find(item => item.HeroId == projectile.TargetHeroId && item.IsAlive);
                if (target != null) projectile.TargetPosition = target.Position;
                Vector2 previousPosition = projectile.Position;
                projectile.Position = Vector2.MoveTowards(
                    projectile.Position,
                    projectile.TargetPosition,
                    config.ProjectileSpeed * deltaTime);
                projectile.TravelledDistance += Vector2.Distance(previousPosition, projectile.Position);

                WIBattleCharacterState hit = FindProjectileHit(config, runtime, projectile, previousPosition, projectile.Position);
                if (hit != null)
                {
                    hit.Health = Mathf.Max(0, hit.Health - Mathf.Max(1, projectile.Damage));
                    runtime.Projectiles.RemoveAt(projectileIndex);
                    continue;
                }
                if (projectile.RemainingLifetime <= 0f || projectile.Position == projectile.TargetPosition && target == null)
                {
                    runtime.Projectiles.RemoveAt(projectileIndex);
                }
            }
        }

        // 발사체 이동 선분과 가장 먼저 충돌하는 유효 인물을 반환합니다.
        private static WIBattleCharacterState FindProjectileHit(
            WIBattleConfigSO config,
            WIBattleRuntimeState runtime,
            WIBattleProjectileState projectile,
            Vector2 start,
            Vector2 end)
        {
            bool allowFriendlyFire = config.ProjectileFriendlyFireEnabled &&
                projectile.TravelledDistance >= config.ProjectileFriendlyFireSafeDistance;
            WIBattleCharacterState bestHit = null;
            float bestAlong = float.MaxValue;
            foreach (WIBattleCharacterState character in runtime.Characters)
            {
                if (character.IsAlive == false || character.HeroId == projectile.ShooterHeroId) continue;
                if (character.Side == projectile.Side && allowFriendlyFire == false) continue;
                if (DistanceToSegment(character.Position, start, end) > config.ProjectileCollisionRadius) continue;
                float along = Vector2.SqrMagnitude(character.Position - start);
                if (along >= bestAlong) continue;
                bestAlong = along;
                bestHit = character;
            }
            return bestHit;
        }

        // 한 점과 발사체 이동 선분 사이의 최단 거리를 계산합니다.
        private static float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
        {
            Vector2 segment = end - start;
            if (segment.sqrMagnitude <= 0.0001f) return Vector2.Distance(point, start);
            float amount = Mathf.Clamp01(Vector2.Dot(point - start, segment) / segment.sqrMagnitude);
            return Vector2.Distance(point, start + segment * amount);
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
            List<WIBattleCharacterState> characters = new List<WIBattleCharacterState>(runtime.Characters.Count);
            foreach (WIBattleCharacterState character in runtime.Characters)
            {
                if (character.IsAlive) characters.Add(character);
            }
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
            if (caster == null) return "전장에 없는 영웅";
            if (skill == null) return "배정된 스킬 없음";
            if (caster.IsAlive == false) return "전투 불능";
            if (caster.SkillCooldownRemaining > 0f) return $"재사용 {caster.SkillCooldownRemaining:0.0}초";
            if (caster.Mana < skill.ManaCost) return $"마나 부족 {caster.Mana}/{skill.ManaCost}";
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
            WIBattleCharacterState nearest = null;
            float nearestDistance = float.MaxValue;
            foreach (WIBattleCharacterState candidate in runtime.Characters)
            {
                if (candidate.IsAlive == false || candidate.Side == actor.Side) continue;
                float distance = Vector2.SqrMagnitude(candidate.Position - actor.Position);
                if (distance >= nearestDistance) continue;
                nearestDistance = distance;
                nearest = candidate;
            }
            return nearest;
        }

        // 진영에 현재 지정된 부대 명령을 반환합니다.
        private static WIBattleCommand GetCommand(WIBattleRuntimeState runtime, WIBattleSide side)
        {
            return side == WIBattleSide.Attacker ? runtime.AttackerCommand : runtime.DefenderCommand;
        }
    }
}

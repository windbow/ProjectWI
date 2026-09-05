using System.Collections.Generic;
using System.Linq;
using ProjectWI.Administration;
using UnityEngine;

namespace ProjectWI.Battle
{
    public static partial class WIBattleSimulation
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
                if (config.UseHiddenGrid == true && actor.HasGridDestination == true)
                {
                    ContinueGridMovement(config, actor, actor.MoveSpeed, deltaTime);
                    continue;
                }
                float distance = Vector2.Distance(actor.Position, target.Position);
                bool canAttack = CanAttackFromCurrentCell(config, actor, target, distance);
                if (canAttack == false)
                {
                    WIBattleCommand command = GetCommand(runtime, actor.Side);
                    if (command == WIBattleCommand.Hold)
                    {
                        MoveCharacter(
                            config,
                            runtime,
                            actor,
                            actor.FormationPosition,
                            config.FormationReturnSpeed,
                            deltaTime);
                    }
                    else
                    {
                        float speed = command == WIBattleCommand.Advance
                            ? actor.MoveSpeed * config.AdvanceSpeedMultiplier
                            : actor.MoveSpeed;
                        Vector2 attackApproach = UsesProjectile(actor)
                            ? new Vector2(target.Position.x, actor.FormationPosition.y)
                            : FindMeleeAttackPosition(config, runtime, actor, target);
                        MoveCharacter(config, runtime, actor, attackApproach, speed, deltaTime);
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

        // 근접 캐릭터는 목표 주변 여덟 셀과 월드 사거리를 모두 만족할 때만 공격할 수 있습니다.
        private static bool CanAttackFromCurrentCell(
            WIBattleConfigSO config,
            WIBattleCharacterState actor,
            WIBattleCharacterState target,
            float worldDistance)
        {
            if (worldDistance > actor.AttackRange)
            {
                return false;
            }
            if (config.UseHiddenGrid == false || UsesProjectile(actor) == true)
            {
                return true;
            }
            int columnDistance = Mathf.Abs(actor.GridColumn - target.GridColumn);
            int rowDistance = Mathf.Abs(actor.GridRow - target.GridRow);
            return Mathf.Max(columnDistance, rowDistance) == 1;
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

        // 진영에 현재 지정된 전투단 명령을 반환합니다.
        private static WIBattleCommand GetCommand(WIBattleRuntimeState runtime, WIBattleSide side)
        {
            return side == WIBattleSide.Attacker ? runtime.AttackerCommand : runtime.DefenderCommand;
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using ProjectWI.Administration;
using UnityEngine;

namespace ProjectWI.Battle
{
    public static partial class WIBattleSimulation
    {
        // 거의 수직 이동이나 정면 위아래 표적 때 방향이 떨리지 않도록 무시하는 가로 거리입니다.
        private const float FacingDeadZone = 0.05f;
        // 한 틱의 가로 이동이 이 값보다 크면 이동 방향을 바라봅니다.
        private const float FacingMoveThreshold = 0.02f;

        // 한 프레임의 표적 탐색, 이동, 공격과 승패 판정을 처리합니다.
        public static WIBattleOutcome Step(WIBattleConfigSO config, WIBattleRuntimeState runtime, float deltaTime)
        {
            if (runtime == null || runtime.Finished)
            {
                return runtime == null ? WIBattleOutcome.None : runtime.AttackerOutcome;
            }
            if (runtime.IsDeploying == true)
            {
                return WIBattleOutcome.None;
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
            PrepareEngagements(config, runtime, deltaTime);
            UpdateMorale(config, runtime, deltaTime);
            UpdateSquadCohesion(config, runtime, deltaTime);
            foreach (WIBattleCharacterState actor in runtime.Characters)
            {
                if (actor.IsAlive == false)
                {
                    continue;
                }
                actor.CooldownRemaining = Mathf.Max(0f, actor.CooldownRemaining - deltaTime);
                actor.SkillCooldownRemaining = Mathf.Max(0f, actor.SkillCooldownRemaining - deltaTime);
                Vector2 previousPosition = actor.Position;
                ActContinuous(config, runtime, actor, deltaTime);
                UpdateFacing(actor, previousPosition.x);
                TrackChargeDistance(actor, previousPosition);
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
                Duration = config.SlashEffectDuration
            });
            runtime.NextVisualEffectId += 1;
        }

        // 실제 피해가 적용된 대상의 위치를 단발 피격 표시로 기록합니다.
        private static void AddHitVisual(WIBattleConfigSO config, WIBattleRuntimeState runtime, WIBattleCharacterState target)
        {
            runtime.VisualEffects.Add(new WIBattleVisualEffectState
            {
                EffectId = runtime.NextVisualEffectId++,
                EffectType = WIBattleVisualEffectType.Hit,
                Side = target.Side,
                StartPosition = target.Position,
                EndPosition = target.Position,
                StartedAt = runtime.ElapsedSeconds,
                Duration = config.HitEffectDuration
            });
        }

        // 이번 틱에 가로로 움직였으면 이동 방향을, 제자리면 표적 쪽을 바라보게 하고 둘 다 없으면 방향을 유지합니다.
        private static void UpdateFacing(WIBattleCharacterState actor, float previousX)
        {
            float deltaX = actor.Position.x - previousX;
            if (Mathf.Abs(deltaX) > FacingMoveThreshold)
            {
                actor.FacingRight = deltaX > 0f;
                return;
            }
            if (string.IsNullOrEmpty(actor.TargetHeroId) == false &&
                charactersById.TryGetValue(actor.TargetHeroId, out WIBattleCharacterState target) == true &&
                Mathf.Abs(target.Position.x - actor.Position.x) > FacingDeadZone)
            {
                actor.FacingRight = target.Position.x > actor.Position.x;
            }
        }

        // 진영에 현재 지정된 공통 전투단 명령을 반환합니다.
        private static WIBattleCommand GetCommand(WIBattleRuntimeState runtime, WIBattleSide side)
        {
            return side == WIBattleSide.Attacker ? runtime.AttackerCommand : runtime.DefenderCommand;
        }

        // 인물 분대의 전용 명령을 우선하고 없으면 진영 공통 명령을 반환합니다. 후퇴는 진영 전체에 우선합니다.
        private static WIBattleCommand GetCommand(WIBattleRuntimeState runtime, WIBattleCharacterState character)
        {
            WIBattleCommand sideCommand = GetCommand(runtime, character.Side);
            if (sideCommand == WIBattleCommand.Retreat)
            {
                return sideCommand;
            }
            WIBattleSquadState squad = FindSquad(runtime, character.SquadId);
            if (squad != null && squad.IsRouting == true)
            {
                return WIBattleCommand.Retreat;
            }
            return squad != null && squad.HasCommandOverride == true ? squad.Command : sideCommand;
        }

        // 인물 분대의 전용 집중 표적을 우선하고 없으면 진영 공통 집중 표적을 반환합니다.
        private static string GetFocusHeroId(WIBattleRuntimeState runtime, WIBattleCharacterState character)
        {
            WIBattleSquadState squad = FindSquad(runtime, character.SquadId);
            if (squad != null && squad.HasCommandOverride == true)
            {
                return squad.FocusHeroId;
            }
            return character.Side == WIBattleSide.Attacker ? runtime.AttackerFocusHeroId : runtime.DefenderFocusHeroId;
        }

        // 분대 번호로 분대 상태를 찾습니다.
        public static WIBattleSquadState FindSquad(WIBattleRuntimeState runtime, int squadId)
        {
            if (squadId <= 0)
            {
                return null;
            }
            foreach (WIBattleSquadState squad in runtime.Squads)
            {
                if (squad.SquadId == squadId)
                {
                    return squad;
                }
            }
            return null;
        }
    }
}

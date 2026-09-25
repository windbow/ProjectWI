using ProjectWI.Administration;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectWI.Battle
{
    public static partial class WIBattleSimulation
    {
        // 한 틱 동안 목표별 근접 교전 슬롯 점유 인원을 재사용 저장합니다.
        private static readonly Dictionary<string, int> slotClaimsByTarget = new Dictionary<string, int>();
        // 목표별로 슬롯을 요청한 근접 인물 목록을 재사용 저장합니다.
        private static readonly Dictionary<string, List<WIBattleCharacterState>> slotRequestsByTarget =
            new Dictionary<string, List<WIBattleCharacterState>>();
        // 슬롯 요청 목록 객체를 재사용하기 위한 풀입니다.
        private static readonly Stack<List<WIBattleCharacterState>> requestListPool = new Stack<List<WIBattleCharacterState>>();
        // 인물 ID로 살아 있는 전투 상태를 빠르게 찾기 위한 틱 단위 색인입니다.
        private static readonly Dictionary<string, WIBattleCharacterState> charactersById =
            new Dictionary<string, WIBattleCharacterState>();
        // 슬롯 배정 시 이미 사용한 슬롯 번호를 표시하는 임시 배열입니다.
        private static readonly bool[] usedSlots = new bool[8];

        // 누적 프레임 시간을 고정 틱으로 나누어 시뮬레이션을 진행하고 마지막 결과를 반환합니다.
        public static WIBattleOutcome Advance(WIBattleConfigSO config, WIBattleRuntimeState runtime, float deltaTime)
        {
            if (runtime == null)
            {
                return WIBattleOutcome.None;
            }
            float tick = config.FixedTickSeconds;
            runtime.TickAccumulator = Mathf.Min(runtime.TickAccumulator + deltaTime, tick * 8f);
            WIBattleOutcome outcome = runtime.Finished == true ? runtime.AttackerOutcome : WIBattleOutcome.None;
            while (runtime.TickAccumulator >= tick && runtime.Finished == false)
            {
                runtime.TickAccumulator -= tick;
                RecordPreviousPositions(runtime);
                outcome = Step(config, runtime, tick);
            }
            runtime.InterpolationAlpha = runtime.Finished == true ? 1f : Mathf.Clamp01(runtime.TickAccumulator / tick);
            return outcome;
        }

        // 틱 진행 전 인물과 발사체 위치를 저장해 화면에서 틱 사이를 부드럽게 보간하게 합니다.
        private static void RecordPreviousPositions(WIBattleRuntimeState runtime)
        {
            foreach (WIBattleCharacterState character in runtime.Characters)
            {
                character.PreviousPosition = character.Position;
                character.HasPreviousPosition = true;
            }
            foreach (WIBattleProjectileState projectile in runtime.Projectiles)
            {
                projectile.PreviousPosition = projectile.Position;
                projectile.HasPreviousPosition = true;
            }
        }

        // 연속 이동 모드에서 인물 색인, 표적과 근접 교전 슬롯을 이번 틱 기준으로 갱신합니다.
        private static void PrepareEngagements(WIBattleConfigSO config, WIBattleRuntimeState runtime, float deltaTime)
        {
            charactersById.Clear();
            foreach (WIBattleCharacterState character in runtime.Characters)
            {
                if (character.IsAlive == true)
                {
                    charactersById[character.HeroId] = character;
                }
            }
            foreach (WIBattleCharacterState actor in runtime.Characters)
            {
                if (actor.IsAlive == false)
                {
                    actor.EngagementSlot = -1;
                    continue;
                }
                actor.RetargetRemaining -= deltaTime;
                actor.TargetHeroId = SelectContinuousTarget(config, runtime, actor)?.HeroId;
            }
            AssignMeleeSlots(config);
        }

        // 집중 목표, 유지 중인 표적, 거리와 슬롯 여유 점수 순으로 연속 이동 모드 표적을 고릅니다.
        private static WIBattleCharacterState SelectContinuousTarget(
            WIBattleConfigSO config,
            WIBattleRuntimeState runtime,
            WIBattleCharacterState actor)
        {
            string focusHeroId = GetFocusHeroId(runtime, actor);
            if (GetCommand(runtime, actor) == WIBattleCommand.Focus && string.IsNullOrEmpty(focusHeroId) == false &&
                charactersById.TryGetValue(focusHeroId, out WIBattleCharacterState focus) == true && focus.Side != actor.Side)
            {
                return focus;
            }
            WIBattleCommand command = GetCommand(runtime, actor);
            if (command == WIBattleCommand.Retreat)
            {
                return null;
            }
            if (command == WIBattleCommand.Protect && UsesProjectile(actor) == false)
            {
                return FindProtectTarget(runtime, actor, config);
            }
            WIBattleCharacterState current = null;
            if (string.IsNullOrEmpty(actor.TargetHeroId) == false)
            {
                charactersById.TryGetValue(actor.TargetHeroId, out current);
            }
            if (current != null && actor.RetargetRemaining > 0f)
            {
                return current;
            }
            actor.RetargetRemaining = config.RetargetInterval;
            bool melee = UsesProjectile(actor) == false;
            WIBattleCharacterState best = null;
            float bestScore = float.MaxValue;
            foreach (WIBattleCharacterState candidate in runtime.Characters)
            {
                if (candidate.IsAlive == false || candidate.Side == actor.Side)
                {
                    continue;
                }
                float score = Vector2.Distance(candidate.Position, actor.Position);
                bool holdsSlot = candidate == current && actor.EngagementSlot >= 0;
                if (melee == true && holdsSlot == false &&
                    slotClaimsByTarget.TryGetValue(candidate.HeroId, out int claims) == true &&
                    claims >= GetMeleeSlotCountFor(config, candidate))
                {
                    score += config.FullSlotTargetPenalty;
                }
                if (score < bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }
            return best;
        }

        // 목표마다 가까운 근접 인물부터 비어 있는 교전 슬롯을 배정하고 남은 인물은 대기열로 둡니다.
        private static void AssignMeleeSlots(WIBattleConfigSO config)
        {
            foreach (List<WIBattleCharacterState> list in slotRequestsByTarget.Values)
            {
                list.Clear();
                requestListPool.Push(list);
            }
            slotRequestsByTarget.Clear();
            slotClaimsByTarget.Clear();
            foreach (WIBattleCharacterState actor in charactersById.Values)
            {
                actor.EngagementSlot = -1;
                if (UsesProjectile(actor) == true || string.IsNullOrEmpty(actor.TargetHeroId) == true)
                {
                    continue;
                }
                if (slotRequestsByTarget.TryGetValue(actor.TargetHeroId, out List<WIBattleCharacterState> requests) == false)
                {
                    requests = requestListPool.Count > 0 ? requestListPool.Pop() : new List<WIBattleCharacterState>();
                    slotRequestsByTarget[actor.TargetHeroId] = requests;
                }
                requests.Add(actor);
            }
            foreach (KeyValuePair<string, List<WIBattleCharacterState>> pair in slotRequestsByTarget)
            {
                WIBattleCharacterState target = charactersById[pair.Key];
                List<WIBattleCharacterState> requests = pair.Value;
                int slotCount = GetMeleeSlotCountFor(config, target);
                requests.Sort((left, right) =>
                {
                    int compare = Vector2.SqrMagnitude(left.Position - target.Position)
                        .CompareTo(Vector2.SqrMagnitude(right.Position - target.Position));
                    return compare != 0 ? compare : string.CompareOrdinal(left.HeroId, right.HeroId);
                });
                for (int index = 0; index < slotCount; index += 1)
                {
                    usedSlots[index] = false;
                }
                int assigned = 0;
                foreach (WIBattleCharacterState actor in requests)
                {
                    if (assigned >= slotCount)
                    {
                        break;
                    }
                    int bestSlot = -1;
                    float bestDistance = float.MaxValue;
                    for (int slot = 0; slot < slotCount; slot += 1)
                    {
                        if (usedSlots[slot] == true)
                        {
                            continue;
                        }
                        float distance = Vector2.SqrMagnitude(GetSlotPosition(config, actor.Side, target, slot) - actor.Position);
                        if (distance < bestDistance)
                        {
                            bestDistance = distance;
                            bestSlot = slot;
                        }
                    }
                    usedSlots[bestSlot] = true;
                    actor.EngagementSlot = bestSlot;
                    assigned += 1;
                }
                slotClaimsByTarget[pair.Key] = assigned;
            }
        }

        // 공격자 진영 방향을 기준으로 목표 주변에 부채꼴로 배치된 교전 슬롯 위치를 반환합니다.
        private static Vector2 GetSlotPosition(WIBattleConfigSO config, WIBattleSide attackerSide, WIBattleCharacterState target, int slot)
        {
            float baseAngle = attackerSide == WIBattleSide.Attacker ? 180f : 0f;
            int ring = (slot + 1) / 2;
            float sign = slot % 2 == 1 ? 1f : -1f;
            float angle = (baseAngle + sign * ring * config.MeleeSlotAngleStep) * Mathf.Deg2Rad;
            float distance = config.MeleeRange * config.MeleeSlotDistanceRatio;
            return target.Position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
        }

        // 슬롯을 받지 못한 근접 인물이 목표 앞 아군 쪽에서 기다릴 대기 위치를 반환합니다.
        private static Vector2 GetQueuePosition(WIBattleConfigSO config, WIBattleCharacterState actor, WIBattleCharacterState target)
        {
            float direction = actor.Side == WIBattleSide.Attacker ? -1f : 1f;
            float distance = config.MeleeRange * config.MeleeSlotDistanceRatio + config.MinimumUnitSpacing * 1.5f;
            return new Vector2(target.Position.x + direction * distance, Mathf.Lerp(actor.Position.y, target.Position.y, 0.5f));
        }

        // 연속 이동 모드에서 명령·역할에 맞게 표적을 향해 이동하거나 공격합니다.
        private static void ActContinuous(
            WIBattleConfigSO config,
            WIBattleRuntimeState runtime,
            WIBattleCharacterState actor,
            float deltaTime)
        {
            WIBattleCommand command = GetCommand(runtime, actor);
            bool ranged = UsesProjectile(actor);
            if (command == WIBattleCommand.Retreat)
            {
                MoveRetreating(config, actor, deltaTime);
                return;
            }
            if (command == WIBattleCommand.Rally && TryMoveToRallyPoint(config, runtime, actor, deltaTime) == true)
            {
                return;
            }
            if (command == WIBattleCommand.Follow && TryFollowControlledLeader(config, runtime, actor, deltaTime) == true)
            {
                return;
            }
            if (command == WIBattleCommand.MoveTo &&
                Vector2.Distance(actor.Position, actor.FormationPosition) > config.MoveOrderArrivalDistance)
            {
                actor.Position = Vector2.MoveTowards(actor.Position, actor.FormationPosition, actor.MoveSpeed * GetMoveSpeedMultiplier(config, actor) * deltaTime);
                ClampToArena(config, actor);
                return;
            }
            WIBattleCharacterState target = null;
            if (string.IsNullOrEmpty(actor.TargetHeroId) == false)
            {
                charactersById.TryGetValue(actor.TargetHeroId, out target);
            }
            if (target == null || target.IsAlive == false)
            {
                if (command == WIBattleCommand.Protect && ranged == false)
                {
                    MoveToGuardPosition(config, runtime, actor, deltaTime);
                }
                return;
            }
            float distance = Vector2.Distance(actor.Position, target.Position);
            float attackRange = GetEffectiveAttackRange(config, actor);
            if (distance <= attackRange)
            {
                if (ranged == true && command != WIBattleCommand.Hold && actor.CooldownRemaining > 0f &&
                    distance < config.RangedRetreatDistance)
                {
                    Vector2 away = actor.Position + (actor.Position - target.Position).normalized * config.RangedRetreatDistance;
                    actor.Position = Vector2.MoveTowards(actor.Position, away, actor.MoveSpeed * GetMoveSpeedMultiplier(config, actor) * deltaTime);
                    ClampToArena(config, actor);
                    return;
                }
                if (actor.CooldownRemaining <= 0f)
                {
                    PerformAttack(config, runtime, actor, target);
                }
                return;
            }
            if (command == WIBattleCommand.Rally || command == WIBattleCommand.Follow)
            {
                return;
            }
            if (command == WIBattleCommand.Hold || command == WIBattleCommand.MoveTo)
            {
                actor.Position = Vector2.MoveTowards(actor.Position, actor.FormationPosition, config.FormationReturnSpeed * deltaTime);
                return;
            }
            float speed = command == WIBattleCommand.Advance
                ? actor.MoveSpeed * config.AdvanceSpeedMultiplier
                : actor.MoveSpeed;
            Vector2 goal;
            if (ranged == true)
            {
                Vector2 toActor = (actor.Position - target.Position).normalized;
                goal = target.Position + toActor * attackRange * config.RangedPreferredRangeRatio;
            }
            else if (actor.EngagementSlot >= 0)
            {
                goal = GetSlotPosition(config, actor.Side, target, actor.EngagementSlot);
            }
            else
            {
                goal = GetQueuePosition(config, actor, target);
            }
            actor.Position = Vector2.MoveTowards(actor.Position, goal, speed * GetMoveSpeedMultiplier(config, actor) * deltaTime);
            ClampToArena(config, actor);
        }

        // 후열 보호 중 원거리·지원 아군에게 접근한 가장 가까운 적을, 없으면 사거리 안의 적을 반환합니다.
        private static WIBattleCharacterState FindProtectTarget(
            WIBattleRuntimeState runtime,
            WIBattleCharacterState actor,
            WIBattleConfigSO config)
        {
            float threatRadiusSquared = config.ProtectThreatRadius * config.ProtectThreatRadius;
            WIBattleCharacterState bestThreat = null;
            float bestThreatDistance = float.MaxValue;
            WIBattleCharacterState nearbyEnemy = null;
            float nearbyDistance = actor.AttackRange;
            foreach (WIBattleCharacterState enemy in runtime.Characters)
            {
                if (enemy.IsAlive == false || enemy.Side == actor.Side)
                {
                    continue;
                }
                float actorDistance = Vector2.Distance(enemy.Position, actor.Position);
                if (actorDistance <= nearbyDistance)
                {
                    nearbyDistance = actorDistance;
                    nearbyEnemy = enemy;
                }
                foreach (WIBattleCharacterState ally in runtime.Characters)
                {
                    if (ally.IsAlive == false || ally.Side != actor.Side || UsesProjectile(ally) == false)
                    {
                        continue;
                    }
                    if (Vector2.SqrMagnitude(enemy.Position - ally.Position) > threatRadiusSquared)
                    {
                        continue;
                    }
                    if (actorDistance < bestThreatDistance)
                    {
                        bestThreatDistance = actorDistance;
                        bestThreat = enemy;
                    }
                    break;
                }
            }
            return bestThreat ?? nearbyEnemy;
        }

        // 후열 보호 중 위협이 없으면 가장 가까운 원거리·지원 아군의 적 방향 앞에 서도록 이동합니다.
        private static void MoveToGuardPosition(
            WIBattleConfigSO config,
            WIBattleRuntimeState runtime,
            WIBattleCharacterState actor,
            float deltaTime)
        {
            WIBattleCharacterState anchor = null;
            float anchorDistance = float.MaxValue;
            foreach (WIBattleCharacterState ally in runtime.Characters)
            {
                if (ally.IsAlive == false || ally.Side != actor.Side || UsesProjectile(ally) == false)
                {
                    continue;
                }
                float distance = Vector2.SqrMagnitude(ally.Position - actor.Position);
                if (distance < anchorDistance)
                {
                    anchorDistance = distance;
                    anchor = ally;
                }
            }
            Vector2 goal = actor.FormationPosition;
            if (anchor != null)
            {
                float forward = actor.Side == WIBattleSide.Attacker ? 1f : -1f;
                goal = anchor.Position + new Vector2(forward * config.ProtectGuardDistance, 0f);
            }
            actor.Position = Vector2.MoveTowards(actor.Position, goal, actor.MoveSpeed * GetMoveSpeedMultiplier(config, actor) * deltaTime);
            ClampToArena(config, actor);
        }

        // 분대 전용 집결이면 분대장을, 진영 전체 집결이면 진영 대장·영웅·첫 생존자 순으로 집결 기준 인물을 반환합니다.
        private static WIBattleCharacterState GetRallyLeader(WIBattleRuntimeState runtime, WIBattleCharacterState actor)
        {
            WIBattleSquadState squad = FindSquad(runtime, actor.SquadId);
            if (squad != null && squad.HasCommandOverride == true && string.IsNullOrEmpty(squad.LeaderHeroId) == false &&
                charactersById.TryGetValue(squad.LeaderHeroId, out WIBattleCharacterState squadLeader) == true)
            {
                return squadLeader;
            }
            WIBattleCharacterState hero = null;
            WIBattleCharacterState first = null;
            foreach (WIBattleCharacterState character in runtime.Characters)
            {
                if (character.IsAlive == false || character.Side != actor.Side)
                {
                    continue;
                }
                if (character.Role == WIUnitRole.Commander)
                {
                    return character;
                }
                if (hero == null && character.Grade == WICharacterGrade.Hero)
                {
                    hero = character;
                }
                if (first == null)
                {
                    first = character;
                }
            }
            return hero ?? first;
        }

        // 집결 명령에서 대장 주변 축소 진형 위치로 이동 중이면 true를 반환하고 도착했거나 대장이면 false를 반환합니다.
        private static bool TryMoveToRallyPoint(
            WIBattleConfigSO config,
            WIBattleRuntimeState runtime,
            WIBattleCharacterState actor,
            float deltaTime)
        {
            WIBattleCharacterState leader = GetRallyLeader(runtime, actor);
            if (leader == null || leader == actor)
            {
                return false;
            }
            Vector2 goal = leader.Position + (actor.FormationPosition - leader.FormationPosition) * config.RallyFormationScale;
            if (Vector2.Distance(actor.Position, goal) <= config.MinimumUnitSpacing * 0.5f)
            {
                return false;
            }
            actor.Position = Vector2.MoveTowards(actor.Position, goal, actor.MoveSpeed * GetMoveSpeedMultiplier(config, actor) * deltaTime);
            ClampToArena(config, actor);
            return true;
        }

        // 직접 지휘 중인 분대장은 지정 위치로, 분대원은 분대장 곁 자기 자리로 이동하며 이동 중이면 true를 반환합니다.
        private static bool TryFollowControlledLeader(
            WIBattleConfigSO config,
            WIBattleRuntimeState runtime,
            WIBattleCharacterState actor,
            float deltaTime)
        {
            WIBattleSquadState squad = FindSquad(runtime, actor.SquadId);
            if (squad == null || charactersById.TryGetValue(squad.LeaderHeroId ?? string.Empty, out WIBattleCharacterState leader) == false)
            {
                return false;
            }
            float speed = actor.MoveSpeed * GetMoveSpeedMultiplier(config, actor) * deltaTime;
            if (leader == actor)
            {
                if (Vector2.Distance(actor.Position, actor.FormationPosition) <= config.MoveOrderArrivalDistance)
                {
                    return false;
                }
                actor.Position = Vector2.MoveTowards(actor.Position, actor.FormationPosition, speed);
                ClampToArena(config, actor);
                return true;
            }
            Vector2 goal = leader.Position + actor.FollowOffset;
            if (Vector2.Distance(actor.Position, goal) <= config.FollowSlackDistance)
            {
                return false;
            }
            actor.Position = Vector2.MoveTowards(actor.Position, goal, speed);
            ClampToArena(config, actor);
            return true;
        }

        // 후퇴 명령 인물을 자기 진영 가장자리로 이동시키고 도착하면 전장에서 이탈시킵니다.
        private static void MoveRetreating(WIBattleConfigSO config, WIBattleCharacterState actor, float deltaTime)
        {
            float edge = config.ArenaSize.x * 0.5f * (actor.Side == WIBattleSide.Attacker ? -1f : 1f);
            Vector2 goal = new Vector2(edge, actor.Position.y);
            actor.Position = Vector2.MoveTowards(actor.Position, goal, actor.MoveSpeed * GetMoveSpeedMultiplier(config, actor) * deltaTime);
            ClampToArena(config, actor);
            if (Mathf.Abs(actor.Position.x - edge) <= config.RetreatEscapeMargin)
            {
                actor.Escaped = true;
                actor.EngagementSlot = -1;
            }
        }

        // 명령 보정을 적용해 근접 즉시 피해 또는 원거리 발사체 공격을 수행합니다.
        private static void PerformAttack(
            WIBattleConfigSO config,
            WIBattleRuntimeState runtime,
            WIBattleCharacterState actor,
            WIBattleCharacterState target)
        {
            int damage = actor.AttackDamage;
            if (GetCommand(runtime, actor) == WIBattleCommand.Focus)
            {
                damage = Mathf.RoundToInt(damage * config.FocusDamageMultiplier);
            }
            float flankMultiplier = UsesProjectile(actor) == false ? GetFlankMultiplier(config, actor, target) : 1f;
            damage = Mathf.RoundToInt(damage * flankMultiplier);
            if (GetCommand(runtime, target) == WIBattleCommand.Hold)
            {
                damage = Mathf.RoundToInt(damage * (1f - config.HoldDamageReduction));
            }
            if (UsesProjectile(actor) == true)
            {
                LaunchProjectile(config, runtime, actor, target, Mathf.Max(1, damage));
            }
            else
            {
                AddMeleeAttackVisual(config, runtime, actor, target);
                ApplyDamage(config, runtime, target, damage, flankMultiplier);
                AddHitVisual(config, runtime, target);
                ApplyMeleeKnockback(config, runtime, actor, target);
            }
            actor.CooldownRemaining = config.AttackCooldown;
        }

        // 공격자가 대상의 정면·측면·후방 중 어디에 있는지에 따라 근접 피해 배율을 반환합니다.
        public static float GetFlankMultiplier(WIBattleConfigSO config, WIBattleCharacterState actor, WIBattleCharacterState target)
        {
            Vector2 toActor = actor.Position - target.Position;
            if (toActor.sqrMagnitude < 0.0001f)
            {
                return 1f;
            }
            Vector2 facing = target.FacingRight == true ? Vector2.right : Vector2.left;
            float dot = Vector2.Dot(toActor.normalized, facing);
            if (dot >= config.FlankFrontDot)
            {
                return 1f;
            }
            return dot <= -config.FlankFrontDot ? config.RearDamageMultiplier : config.FlankDamageMultiplier;
        }

        // 교전 중인 인물은 무겁게 취급해 전열이 뒤에서 미는 인물에게 쉽게 밀리지 않도록 질량을 반환합니다.
        private static float GetCollisionMass(WIBattleConfigSO config, WIBattleCharacterState character)
        {
            if (string.IsNullOrEmpty(character.TargetHeroId) == true ||
                charactersById.TryGetValue(character.TargetHeroId, out WIBattleCharacterState target) == false)
            {
                return 1f;
            }
            return Vector2.Distance(character.Position, target.Position) <= character.AttackRange
                ? config.EngagedCollisionMass
                : 1f;
        }
    }
}

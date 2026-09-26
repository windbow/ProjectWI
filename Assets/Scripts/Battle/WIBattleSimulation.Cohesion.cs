using System.Collections.Generic;
using System.Linq;
using ProjectWI.Administration;
using UnityEngine;

namespace ProjectWI.Battle
{
    public static partial class WIBattleSimulation
    {
        // 분대별 살아 있는 분대원 목록을 틱마다 재사용합니다.
        private static readonly Dictionary<int, List<WIBattleCharacterState>> squadMembers =
            new Dictionary<int, List<WIBattleCharacterState>>();
        private static readonly Stack<List<WIBattleCharacterState>> squadMemberListPool = new Stack<List<WIBattleCharacterState>>();

        // 결속 이동(블록 유지 전진)을 쓰는 명령인지 반환합니다.
        public static bool UsesCohesion(WIBattleCommand command)
        {
            return command == WIBattleCommand.Advance || command == WIBattleCommand.Focus || command == WIBattleCommand.Spread;
        }

        // 분대별로 살아 있는 분대원 순번을 다시 매기고(앞줄 결원 보충), 교전 여부와 기준점 전진을 갱신합니다.
        private static void UpdateSquadCohesion(WIBattleConfigSO config, WIBattleRuntimeState runtime, float deltaTime)
        {
            foreach (List<WIBattleCharacterState> list in squadMembers.Values)
            {
                list.Clear();
                squadMemberListPool.Push(list);
            }
            squadMembers.Clear();
            foreach (WIBattleCharacterState character in runtime.Characters)
            {
                if (character.IsAlive == false || character.SquadId <= 0)
                {
                    continue;
                }
                if (squadMembers.TryGetValue(character.SquadId, out List<WIBattleCharacterState> members) == false)
                {
                    members = squadMemberListPool.Count > 0 ? squadMemberListPool.Pop() : new List<WIBattleCharacterState>();
                    squadMembers[character.SquadId] = members;
                }
                members.Add(character);
            }
            foreach (WIBattleSquadState squad in runtime.Squads)
            {
                if (squadMembers.TryGetValue(squad.SquadId, out List<WIBattleCharacterState> members) == false)
                {
                    squad.CohesionActive = false;
                    continue;
                }
                members.Sort((left, right) => left.BlockIndex.CompareTo(right.BlockIndex));
                for (int index = 0; index < members.Count; index += 1)
                {
                    members[index].BlockIndex = index;
                }
                WIBattleCommand command = GetCommand(runtime, members[0]);
                if (squad.IsRouting == true || UsesCohesion(command) == false)
                {
                    squad.CohesionActive = false;
                    continue;
                }
                squad.CohesionActive = true;
                WIBattleCharacterState bearer = members[0];
                squad.Anchor = bearer.Position - GetBlockOffset(config, squad, bearer, members.Count, GetSpacingScale(runtime, bearer, config));
                squad.Engaged = IsSquadEngaged(config, members, IsRangedSquad(members));
                squad.AnchorMoving = false;
                if (squad.Engaged == false)
                {
                    PlanSquadAdvance(config, runtime, squad, members, command, deltaTime);
                }
            }
        }

        // 원거리 인물이 절반을 넘는 분대인지 반환합니다.
        private static bool IsRangedSquad(List<WIBattleCharacterState> members)
        {
            int ranged = 0;
            foreach (WIBattleCharacterState member in members)
            {
                ranged += UsesProjectile(member) == true ? 1 : 0;
            }
            return ranged * 2 > members.Count;
        }

        // 분대의 주력(근접 위주면 근접 인물, 원거리 위주면 원거리 인물)이 사거리 안의 적과 맞닿아 있으면 true를 반환합니다.
        private static bool IsSquadEngaged(WIBattleConfigSO config, List<WIBattleCharacterState> members, bool rangedSquad)
        {
            foreach (WIBattleCharacterState member in members)
            {
                if (UsesProjectile(member) != rangedSquad)
                {
                    continue;
                }
                if (string.IsNullOrEmpty(member.TargetHeroId) == true ||
                    charactersById.TryGetValue(member.TargetHeroId, out WIBattleCharacterState target) == false)
                {
                    continue;
                }
                if (Vector2.Distance(member.Position, target.Position) <= GetEffectiveAttackRange(config, member))
                {
                    return true;
                }
            }
            return false;
        }

        // 교전 전 분대 기준점이 향할 접촉 지점(가장 가까운 적, 집중 명령이면 집중 표적)과 속도를 정합니다.
        // 분대원이 자리에서 많이 벌어지면 잠시 기다리고, 돌격 분대는 가까워지면 속도를 올립니다.
        private static void PlanSquadAdvance(
            WIBattleConfigSO config,
            WIBattleRuntimeState runtime,
            WIBattleSquadState squad,
            List<WIBattleCharacterState> members,
            WIBattleCommand command,
            float deltaTime)
        {
            WIBattleCharacterState enemy = null;
            string focusHeroId = command == WIBattleCommand.Focus ? GetFocusHeroId(runtime, members[0]) : string.Empty;
            if (string.IsNullOrEmpty(focusHeroId) == false)
            {
                charactersById.TryGetValue(focusHeroId, out enemy);
            }
            if (enemy == null)
            {
                float nearest = float.MaxValue;
                foreach (WIBattleCharacterState candidate in charactersById.Values)
                {
                    if (candidate.Side == squad.Side)
                    {
                        continue;
                    }
                    float distance = Vector2.SqrMagnitude(candidate.Position - squad.Anchor);
                    if (distance < nearest)
                    {
                        nearest = distance;
                        enemy = candidate;
                    }
                }
            }
            if (enemy == null)
            {
                return;
            }
            float lag = 0f;
            float slowest = float.MaxValue;
            int rangedCount = 0;
            int chargerCount = 0;
            foreach (WIBattleCharacterState member in members)
            {
                lag += Vector2.Distance(member.Position, GetBlockSlot(config, runtime, squad, member, members.Count));
                slowest = Mathf.Min(slowest, member.MoveSpeed * GetMoveSpeedMultiplier(config, member));
                rangedCount += UsesProjectile(member) == true ? 1 : 0;
                chargerCount += member.Archetype == WIBattleArchetype.Charger ? 1 : 0;
            }
            if (lag / Mathf.Max(1, members.Count - 1) > config.CohesionWaitDistance && squad.WaitTime < config.CohesionMaxWaitSeconds)
            {
                squad.WaitTime += deltaTime;
                return;
            }
            squad.WaitTime = 0f;
            bool rangedSquad = rangedCount * 2 > members.Count;
            float standoff = rangedSquad == true
                ? config.RangedRange * config.RangedPreferredRangeRatio
                : config.MeleeRange * 0.8f;
            Vector2 toEnemy = enemy.Position - squad.Anchor;
            float distanceToEnemy = toEnemy.magnitude;
            if (distanceToEnemy <= standoff)
            {
                return;
            }
            float speed = slowest * (command == WIBattleCommand.Advance ? config.AdvanceSpeedMultiplier : 1f);
            if (chargerCount * 2 > members.Count && distanceToEnemy <= config.ChargeSprintDistance + standoff)
            {
                speed *= config.ChargeSprintMultiplier;
            }
            squad.AnchorGoal = enemy.Position - toEnemy / distanceToEnemy * standoff;
            squad.AnchorSpeed = speed;
            squad.AnchorMoving = true;
        }

        // 분산 명령이면 블록 간격 배율을 반환합니다.
        private static float GetSpacingScale(WIBattleRuntimeState runtime, WIBattleCharacterState member, WIBattleConfigSO config)
        {
            return GetCommand(runtime, member) == WIBattleCommand.Spread ? config.SpreadSpacingMultiplier : 1f;
        }

        // 기준점 대비 블록 안 자리 오프셋을 반환합니다. 분산 명령이면 간격을 넓힙니다.
        private static Vector2 GetBlockOffset(WIBattleConfigSO config, WIBattleSquadState squad, WIBattleCharacterState member, int memberCount, float spacingScale)
        {
            int files = Mathf.Min(config.SquadBlockFiles, Mathf.Max(1, memberCount));
            int rank = member.BlockIndex / files;
            int file = member.BlockIndex % files;
            int step = (file + 1) / 2;
            int alternating = file % 2 == 1 ? step : -step;
            float direction = squad.Side == WIBattleSide.Attacker ? 1f : -1f;
            float spacing = config.SquadMemberSpacing * spacingScale;
            return new Vector2(-direction * rank * spacing, alternating * spacing) + member.BlockJitter;
        }

        // 분대원의 현재 블록 자리 월드 좌표를 반환합니다.
        private static Vector2 GetBlockSlot(WIBattleConfigSO config, WIBattleRuntimeState runtime, WIBattleSquadState squad, WIBattleCharacterState member, int memberCount)
        {
            float spacingScale = GetSpacingScale(runtime, member, config);
            Vector2 slot = squad.Anchor + GetBlockOffset(config, squad, member, memberCount, spacingScale);
            Vector2 halfSize = config.ArenaSize * 0.5f;
            return new Vector2(Mathf.Clamp(slot.x, -halfSize.x, halfSize.x), Mathf.Clamp(slot.y, -halfSize.y, halfSize.y));
        }

        // 결속 이동 중인 분대원 행동: 사거리 안이면 공격, 블록 자리 근처로 닿는 적이면 묶인 거리 안에서 교전, 아니면 자리로 이동합니다.
        // 결속 이동을 처리했으면 true를 반환합니다.
        private static bool TryActInFormation(
            WIBattleConfigSO config,
            WIBattleRuntimeState runtime,
            WIBattleCharacterState actor,
            WIBattleCommand command,
            float deltaTime)
        {
            WIBattleSquadState squad = FindSquad(runtime, actor.SquadId);
            if (squad == null || squad.CohesionActive == false || UsesCohesion(command) == false ||
                squadMembers.TryGetValue(actor.SquadId, out List<WIBattleCharacterState> members) == false)
            {
                return false;
            }
            Vector2 slot = GetBlockSlot(config, runtime, squad, actor, members.Count);
            float speed = actor.MoveSpeed * GetMoveSpeedMultiplier(config, actor) *
                (command == WIBattleCommand.Advance ? config.AdvanceSpeedMultiplier : 1f);
            WIBattleCharacterState target = null;
            if (string.IsNullOrEmpty(actor.TargetHeroId) == false)
            {
                charactersById.TryGetValue(actor.TargetHeroId, out target);
            }
            if (target != null)
            {
                float range = GetEffectiveAttackRange(config, actor);
                float distance = Vector2.Distance(actor.Position, target.Position);
                bool ranged = UsesProjectile(actor);
                if (distance <= range)
                {
                    if (actor.CooldownRemaining <= 0f)
                    {
                        PerformAttack(config, runtime, actor, target);
                        return true;
                    }
                    if (ranged == false || squad.AnchorMoving == false)
                    {
                        return true;
                    }
                }
                bool reachable = Vector2.Distance(target.Position, slot) <= range + config.FormationEngageLeash;
                if (reachable == true && distance > range)
                {
                    Vector2 engageGoal = ranged == true
                        ? target.Position + (actor.Position - target.Position).normalized * range * 0.95f
                        : actor.EngagementSlot >= 0
                            ? GetSlotPosition(config, actor.Side, target, actor.EngagementSlot)
                            : target.Position;
                    Vector2 leashed = slot + Vector2.ClampMagnitude(engageGoal - slot, config.FormationEngageLeash);
                    actor.Position = Vector2.MoveTowards(actor.Position, leashed, speed * deltaTime);
                    ClampToArena(config, actor);
                    return true;
                }
            }
            if (actor.BlockIndex == 0)
            {
                if (squad.AnchorMoving == true)
                {
                    Vector2 leadGoal = squad.AnchorGoal + (actor.Position - squad.Anchor);
                    actor.Position = Vector2.MoveTowards(actor.Position, leadGoal, squad.AnchorSpeed * deltaTime);
                    ClampToArena(config, actor);
                }
                return true;
            }
            float catchUp = Vector2.Distance(actor.Position, slot) > config.CohesionWaitDistance ? 1.25f : 1f;
            actor.Position = Vector2.MoveTowards(actor.Position, slot, speed * catchUp * deltaTime);
            ClampToArena(config, actor);
            return true;
        }
    }
}

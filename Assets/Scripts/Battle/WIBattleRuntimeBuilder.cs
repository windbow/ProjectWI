using System.Collections.Generic;
using System.Linq;
using ProjectWI.Administration;
using UnityEngine;

namespace ProjectWI.Battle
{
    public static class WIBattleRuntimeBuilder
    {
        // 전략 전투 세션의 참가자 스냅샷을 실제 전투 캐릭터와 초기 진형으로 변환합니다.
        public static WIBattleRuntimeState Build(
            WIBattleConfigSO config,
            WIAdministrationDatabaseSO database,
            WIBattleSessionState session)
        {
            WIBattleRuntimeState runtime = new WIBattleRuntimeState { SessionId = session.SessionId };
            ApplyObjective(config, database, runtime, session.SessionId);
            AddSide(runtime, config, database, session.AttackerHeroIds, WIBattleSide.Attacker);
            AddSide(runtime, config, database, session.DefenderHeroIds, WIBattleSide.Defender);
            WIBattleSimulation.AssignSquads(config, runtime);
            LayoutSquads(config, runtime, WIBattleSide.Attacker, runtime.Squads, false);
            LayoutSquads(config, runtime, WIBattleSide.Defender, runtime.Squads, false);
            return runtime;
        }

        // 기존 생존자와 이동 예약 셀을 보존하면서 새 참가자만 진영 후방에 추가합니다.
        public static void AppendReinforcements(WIBattleConfigSO config, WIAdministrationDatabaseSO database,
            WIBattleSessionState session, WIBattleRuntimeState runtime)
        {
            var existing = new HashSet<string>(runtime.Characters.Select(item => item.HeroId));
            int previousSquadCount = runtime.Squads.Count;
            AddSide(runtime, config, database, session.AttackerHeroIds.Where(item => existing.Contains(item.HeroId) == false).ToList(),
                WIBattleSide.Attacker);
            AddSide(runtime, config, database, session.DefenderHeroIds.Where(item => existing.Contains(item.HeroId) == false).ToList(),
                WIBattleSide.Defender);
            WIBattleSimulation.AssignSquads(config, runtime);
            List<WIBattleSquadState> newSquads = runtime.Squads.Skip(previousSquadCount).ToList();
            LayoutSquads(config, runtime, WIBattleSide.Attacker, newSquads, true);
            LayoutSquads(config, runtime, WIBattleSide.Defender, newSquads, true);
        }

        // 전투 설정의 순환 규칙에 따라 이번 세션의 목표와 제한값을 런타임에 복사합니다.
        private static void ApplyObjective(WIBattleConfigSO config, WIAdministrationDatabaseSO database, WIBattleRuntimeState runtime, string sessionId)
        {
            WIBattleObjectiveDefinition objective = config.SelectBattleObjective(sessionId);
            if (objective == null)
            {
                runtime.ObjectiveType = WIBattleObjectiveType.Elimination;
                runtime.ObjectiveName = database.GetText("UI_BATTLE_OBJECTIVE_ELIMINATION");
                return;
            }

            runtime.ObjectiveId = objective.Id;
            runtime.ObjectiveName = objective.DisplayName.Korean;
            runtime.ObjectiveType = objective.ObjectiveType;
            runtime.ObjectiveDurationSeconds = objective.DurationSeconds;
            runtime.ControlDurationSeconds = objective.ControlDurationSeconds;
            runtime.ControlRadius = objective.ControlRadius;
        }

        // 한 진영 참가자를 전투 인물로 추가합니다. 위치는 분대 편성 뒤 블록 진형으로 정합니다.
        private static void AddSide(
            WIBattleRuntimeState runtime,
            WIBattleConfigSO config,
            WIAdministrationDatabaseSO database,
            List<WIBattleParticipantState> participants,
            WIBattleSide side)
        {
            foreach (WIBattleParticipantState participant in participants.OrderBy(item => item.HeroId, System.StringComparer.Ordinal))
            {
                WIHeroDefinition hero = database.GetHero(participant.HeroId);
                if (hero == null) continue;
                bool ranged = participant.Role == WIUnitRole.Ranged || participant.Role == WIUnitRole.Magic || participant.Role == WIUnitRole.Support;
                WIBattleClassProfile profile = config.GetClassProfile(hero.HeroClass);
                float healthMultiplier = profile == null ? 1f : profile.HealthMultiplier;
                float damageMultiplier = profile == null ? 1f : profile.DamageMultiplier;
                float speedMultiplier = profile == null ? 1f : profile.MoveSpeedMultiplier;
                float cooldownMultiplier = profile == null ? 1f : profile.AttackCooldownMultiplier;
                float rangeMultiplier = profile == null ? 1f : profile.AttackRangeMultiplier;
                int maxHealth = Mathf.Max(1, Mathf.RoundToInt((config.BaseHealth + hero.Might * config.HealthPerMight) * healthMultiplier));
                float speedFactor = (1f + (Hash01(hero.Id, 3) * 2f - 1f) * config.MoveSpeedVariance) * speedMultiplier;
                runtime.Characters.Add(new WIBattleCharacterState
                {
                    HeroId = hero.Id,
                    ArmyId = participant.ArmyId,
                    Side = side,
                    FacingRight = side == WIBattleSide.Attacker,
                    Role = participant.Role,
                    Grade = hero.Grade,
                    HeroClass = hero.HeroClass,
                    DisplayName = hero.DisplayName.Korean,
                    MaxHealth = maxHealth,
                    Health = maxHealth,
                    MaxMana = config.BaseMana + hero.Intelligence * config.ManaPerIntelligence,
                    Mana = config.BaseMana + hero.Intelligence * config.ManaPerIntelligence,
                    AttackDamage = Mathf.Max(1, Mathf.RoundToInt((config.BaseDamage + hero.Might / 5) * damageMultiplier)),
                    AttackRange = (ranged ? config.RangedRange : config.MeleeRange) * (ranged ? rangeMultiplier : 1f),
                    AttackInterval = config.AttackCooldown * cooldownMultiplier,
                    Archetype = profile == null ? WIBattleArchetype.Charger : profile.Archetype,
                    MoveSpeed = config.MoveSpeed * speedFactor
                });
            }
        }

        // 진영의 분대들을 블록 진형으로 세웁니다. 근접 분대는 전열, 원거리 분대는 후열에 두고
        // 전열 안에서는 중앙부터 위아래로 번갈아 배치합니다. 증원은 진영 가장자리 쪽에 세웁니다.
        private static void LayoutSquads(
            WIBattleConfigSO config,
            WIBattleRuntimeState runtime,
            WIBattleSide side,
            List<WIBattleSquadState> squads,
            bool reinforcement)
        {
            float direction = side == WIBattleSide.Attacker ? 1f : -1f;
            float halfX = config.ArenaSize.x * 0.5f - 0.3f;
            float halfY = config.ArenaSize.y * 0.5f - 0.4f;
            List<List<WIBattleCharacterState>> meleeBlocks = new List<List<WIBattleCharacterState>>();
            List<List<WIBattleCharacterState>> rangedBlocks = new List<List<WIBattleCharacterState>>();
            foreach (WIBattleSquadState squad in squads)
            {
                if (squad.Side != side)
                {
                    continue;
                }
                List<WIBattleCharacterState> members = runtime.Characters
                    .Where(item => item.SquadId == squad.SquadId)
                    .OrderBy(item => item.HeroId == squad.LeaderHeroId ? 0 : 1)
                    .ThenBy(item => GetRoleRank(item.Role))
                    .ThenBy(item => item.HeroId, System.StringComparer.Ordinal)
                    .ToList();
                if (members.Count == 0)
                {
                    continue;
                }
                int rangedCount = members.Count(item => IsRanged(item.Role));
                (rangedCount * 2 > members.Count ? rangedBlocks : meleeBlocks).Add(members);
            }
            float meleeDepth = meleeBlocks.Count == 0 ? 0f : meleeBlocks.Max(block => GetBlockDepth(config, block.Count));
            float frontX = reinforcement == true
                ? halfX - 1f - Mathf.Max(meleeDepth, rangedBlocks.Count == 0 ? 0f : rangedBlocks.Max(block => GetBlockDepth(config, block.Count)))
                : config.FormationFrontLineDistance;
            Dictionary<int, WIBattleSquadState> squadLookup = runtime.Squads.ToDictionary(item => item.SquadId);
            float nextLineX = PlaceLine(squadLookup, config, meleeBlocks, frontX, direction, halfX, halfY);
            float rangedFrontX = meleeBlocks.Count == 0 ? frontX : nextLineX + config.FormationLineGap;
            PlaceLine(squadLookup, config, rangedBlocks, rangedFrontX, direction, halfX, halfY);
        }

        // 블록들을 한 전열에 중앙부터 위아래로 번갈아 세우고, 넘치면 뒤 줄로 넘깁니다. 마지막 줄 뒤쪽 거리를 반환합니다.
        private static float PlaceLine(
            Dictionary<int, WIBattleSquadState> squadLookup,
            WIBattleConfigSO config,
            List<List<WIBattleCharacterState>> blocks,
            float frontDistance,
            float direction,
            float halfX,
            float halfY)
        {
            if (blocks.Count == 0)
            {
                return frontDistance;
            }
            float spacing = config.SquadMemberSpacing;
            float blockHeight = (Mathf.Min(config.SquadBlockFiles, blocks.Max(block => block.Count)) - 1) * spacing;
            float pitch = blockHeight + config.SquadGap + spacing;
            int perLine = Mathf.Max(1, Mathf.FloorToInt((halfY * 2f - blockHeight) / pitch) + 1);
            float lineFront = frontDistance;
            float lineBack = frontDistance;
            for (int start = 0; start < blocks.Count; start += perLine)
            {
                int count = Mathf.Min(perLine, blocks.Count - start);
                float lineDepth = 0f;
                for (int index = 0; index < count; index += 1)
                {
                    List<WIBattleCharacterState> block = blocks[start + index];
                    float centerY = GetAlternatingOffset(index) * pitch;
                    if (count % 2 == 0)
                    {
                        centerY -= pitch * 0.5f;
                    }
                    PlaceBlock(squadLookup, config, block, lineFront, centerY, direction, halfX, halfY);
                    lineDepth = Mathf.Max(lineDepth, GetBlockDepth(config, block.Count));
                }
                lineBack = lineFront + lineDepth;
                lineFront = lineBack + config.FormationLineGap;
            }
            return lineBack;
        }

        // 한 분대를 앞줄부터 채우는 블록으로 세웁니다. 각 줄은 가운데부터 좌우로 번갈아 채워 분대장이 앞줄 중앙에 섭니다.
        private static void PlaceBlock(
            Dictionary<int, WIBattleSquadState> squadLookup,
            WIBattleConfigSO config,
            List<WIBattleCharacterState> members,
            float frontDistance,
            float centerY,
            float direction,
            float halfX,
            float halfY)
        {
            int files = Mathf.Min(config.SquadBlockFiles, members.Count);
            float spacing = config.SquadMemberSpacing;
            if (squadLookup != null && squadLookup.TryGetValue(members[0].SquadId, out WIBattleSquadState squad) == true)
            {
                squad.Anchor = new Vector2(Mathf.Clamp(-direction * frontDistance, -halfX, halfX), Mathf.Clamp(centerY, -halfY, halfY));
                squad.CohesionActive = true;
            }
            for (int index = 0; index < members.Count; index += 1)
            {
                WIBattleCharacterState member = members[index];
                int rank = index / files;
                int file = index % files;
                float jitterX = (Hash01(member.HeroId, 1) * 2f - 1f) * config.FormationJitter;
                float jitterY = (Hash01(member.HeroId, 2) * 2f - 1f) * config.FormationJitter;
                member.BlockJitter = new Vector2(jitterX, jitterY);
                member.BlockIndex = index;
                float x = -direction * (frontDistance + rank * spacing) + jitterX;
                float y = centerY + GetAlternatingOffset(file) * spacing + jitterY;
                Vector2 position = new Vector2(Mathf.Clamp(x, -halfX, halfX), Mathf.Clamp(y, -halfY, halfY));
                member.Position = position;
                member.FormationPosition = position;
                member.HasPreviousPosition = false;
            }
        }

        // 분대 인원에 따른 블록 앞뒤 깊이를 반환합니다.
        private static float GetBlockDepth(WIBattleConfigSO config, int count)
        {
            int files = Mathf.Min(config.SquadBlockFiles, Mathf.Max(1, count));
            int ranks = Mathf.CeilToInt(count / (float)files);
            return (ranks - 1) * config.SquadMemberSpacing;
        }

        // 0, +1, -1, +2, -2 … 순서로 가운데부터 번갈아 나가는 자리 번호를 반환합니다.
        private static int GetAlternatingOffset(int index)
        {
            int step = (index + 1) / 2;
            return index % 2 == 1 ? step : -step;
        }

        // 블록 안 줄 순서(전위 0, 근접·대장 1, 원거리·지원 2)를 반환합니다.
        private static int GetRoleRank(WIUnitRole role)
        {
            if (role == WIUnitRole.Vanguard) return 0;
            if (IsRanged(role) == true) return 2;
            return 1;
        }

        // 원거리 계열 역할인지 반환합니다.
        private static bool IsRanged(WIUnitRole role)
        {
            return role == WIUnitRole.Ranged || role == WIUnitRole.Magic || role == WIUnitRole.Support;
        }

        // 인물 ID와 소금값으로 0~1 사이의 결정적 난수를 반환해 전투마다 같은 흔들림을 만듭니다.
        private static float Hash01(string id, int salt)
        {
            unchecked
            {
                int hash = 17 + salt * 7919;
                foreach (char character in id ?? string.Empty)
                {
                    hash = hash * 31 + character;
                }
                return (hash & 0x7fffffff) % 10007 / 10006f;
            }
        }
    }
}

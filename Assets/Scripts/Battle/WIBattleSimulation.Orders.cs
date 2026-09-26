using System.Collections.Generic;
using System.Linq;
using ProjectWI.Administration;
using UnityEngine;

namespace ProjectWI.Battle
{
    public static partial class WIBattleSimulation
    {
        // 분대가 없는 인물을 진영·전투단별로 모아 전투단 하나를 분대 하나로 만들고, 최대 인원을 넘으면 나눕니다.
        // 분대장은 대장, 영웅, 첫 인물 순으로 정합니다.
        public static void AssignSquads(WIBattleConfigSO config, WIBattleRuntimeState runtime)
        {
            Dictionary<string, List<WIBattleCharacterState>> groups = new Dictionary<string, List<WIBattleCharacterState>>();
            List<string> groupOrder = new List<string>();
            foreach (WIBattleCharacterState character in runtime.Characters)
            {
                if (character.SquadId > 0)
                {
                    continue;
                }
                string key = character.Side + "|" + (character.ArmyId ?? string.Empty);
                if (groups.TryGetValue(key, out List<WIBattleCharacterState> group) == false)
                {
                    group = new List<WIBattleCharacterState>();
                    groups[key] = group;
                    groupOrder.Add(key);
                }
                group.Add(character);
            }
            foreach (string key in groupOrder)
            {
                List<WIBattleCharacterState> group = groups[key];
                int squadCount = Mathf.CeilToInt(group.Count / (float)config.SquadMaxSize);
                List<WIBattleCharacterState> leaders = group
                    .OrderBy(item => GetLeaderPriority(item))
                    .ThenBy(item => item.HeroId, System.StringComparer.Ordinal)
                    .Take(squadCount).ToList();
                List<WIBattleSquadState> squads = new List<WIBattleSquadState>();
                foreach (WIBattleCharacterState leader in leaders)
                {
                    WIBattleSquadState squad = new WIBattleSquadState
                    {
                        SquadId = runtime.Squads.Count + 1,
                        Side = leader.Side,
                        LeaderHeroId = leader.HeroId
                    };
                    runtime.Squads.Add(squad);
                    squads.Add(squad);
                    leader.SquadId = squad.SquadId;
                }
                int next = 0;
                foreach (WIBattleCharacterState member in group)
                {
                    if (member.SquadId > 0)
                    {
                        continue;
                    }
                    member.SquadId = squads[next % squads.Count].SquadId;
                    next += 1;
                }
            }
        }

        // 분대장 선정 우선순위(대장 0, 영웅 1, 나머지 2)를 반환합니다.
        private static int GetLeaderPriority(WIBattleCharacterState character)
        {
            if (character.Role == WIUnitRole.Commander) return 0;
            if (character.Grade == WICharacterGrade.Hero) return 1;
            return 2;
        }

        // 진영 공통 명령을 바꾸고 해당 진영 분대들의 전용 명령을 해제합니다.
        public static void SetSideCommand(WIBattleRuntimeState runtime, WIBattleSide side, WIBattleCommand command, string focusHeroId)
        {
            if (side == WIBattleSide.Attacker)
            {
                runtime.AttackerCommand = command;
                runtime.AttackerFocusHeroId = focusHeroId;
            }
            else
            {
                runtime.DefenderCommand = command;
                runtime.DefenderFocusHeroId = focusHeroId;
            }
            foreach (WIBattleSquadState squad in runtime.Squads)
            {
                if (squad.Side == side)
                {
                    squad.HasCommandOverride = false;
                }
            }
        }

        // 선택한 분대에만 전용 명령과 집중 표적을 지정합니다.
        public static void SetSquadCommand(
            WIBattleRuntimeState runtime,
            IEnumerable<int> squadIds,
            WIBattleCommand command,
            string focusHeroId)
        {
            foreach (int squadId in squadIds)
            {
                WIBattleSquadState squad = FindSquad(runtime, squadId);
                if (squad == null)
                {
                    continue;
                }
                squad.HasCommandOverride = true;
                squad.Command = command;
                squad.FocusHeroId = focusHeroId;
            }
        }

        // 선택한 분대들의 현재 배치 모양을 유지한 채 목적지를 중심으로 새 진형 위치를 지정하고 이동시킵니다.
        public static void OrderSquadsMove(
            WIBattleConfigSO config,
            WIBattleRuntimeState runtime,
            ICollection<int> squadIds,
            Vector2 destination)
        {
            Vector2 center = Vector2.zero;
            int count = 0;
            foreach (WIBattleCharacterState character in runtime.Characters)
            {
                if (character.IsAlive == true && squadIds.Contains(character.SquadId) == true)
                {
                    center += character.Position;
                    count += 1;
                }
            }
            if (count == 0)
            {
                return;
            }
            center /= count;
            Vector2 halfSize = config.ArenaSize * 0.5f;
            foreach (WIBattleCharacterState character in runtime.Characters)
            {
                if (character.IsAlive == false || squadIds.Contains(character.SquadId) == false)
                {
                    continue;
                }
                Vector2 slot = destination + (character.Position - center);
                character.FormationPosition = new Vector2(
                    Mathf.Clamp(slot.x, -halfSize.x, halfSize.x),
                    Mathf.Clamp(slot.y, -halfSize.y, halfSize.y));
            }
            SetSquadCommand(runtime, squadIds, WIBattleCommand.MoveTo, string.Empty);
        }

        // 선택한 분대가 지정한 적을 집중 공격하도록 명령합니다.
        public static void OrderSquadsAttack(WIBattleRuntimeState runtime, ICollection<int> squadIds, string enemyHeroId)
        {
            SetSquadCommand(runtime, squadIds, WIBattleCommand.Focus, enemyHeroId);
            foreach (WIBattleCharacterState character in runtime.Characters)
            {
                if (squadIds.Contains(character.SquadId) == true)
                {
                    character.RetargetRemaining = 0f;
                }
            }
        }

        // 배치 단계에서 선택 분대를 모양을 유지한 채 자기 진영 배치 구역 안의 목적지로 즉시 옮깁니다.
        public static void DeploySquads(
            WIBattleConfigSO config,
            WIBattleRuntimeState runtime,
            ICollection<int> squadIds,
            Vector2 destination)
        {
            Vector2 center = Vector2.zero;
            int count = 0;
            WIBattleSide side = WIBattleSide.Attacker;
            foreach (WIBattleCharacterState character in runtime.Characters)
            {
                if (character.IsAlive == true && squadIds.Contains(character.SquadId) == true)
                {
                    center += character.Position;
                    side = character.Side;
                    count += 1;
                }
            }
            if (count == 0)
            {
                return;
            }
            center /= count;
            Vector2 rangeX = GetDeploymentRangeX(config, side);
            float halfY = config.ArenaSize.y * 0.5f;
            foreach (WIBattleCharacterState character in runtime.Characters)
            {
                if (character.IsAlive == false || squadIds.Contains(character.SquadId) == false)
                {
                    continue;
                }
                Vector2 slot = destination + (character.Position - center);
                Vector2 placed = new Vector2(Mathf.Clamp(slot.x, rangeX.x, rangeX.y), Mathf.Clamp(slot.y, -halfY, halfY));
                character.Position = placed;
                character.FormationPosition = placed;
                character.HasPreviousPosition = false;
                WIBattleSquadState squad = FindSquad(runtime, character.SquadId);
                if (squad != null)
                {
                    squad.CohesionActive = false;
                }
            }
        }

        // 분대장을 직접 지휘하기 시작하고 분대원은 현재 상대 위치를 유지하며 따라가게 합니다.
        public static bool StartHeroControl(WIBattleRuntimeState runtime, int squadId)
        {
            WIBattleSquadState squad = FindSquad(runtime, squadId);
            WIBattleCharacterState leader = squad == null ? null : runtime.Characters.Find(item => item.HeroId == squad.LeaderHeroId && item.IsAlive == true);
            if (leader == null)
            {
                return false;
            }
            leader.FormationPosition = leader.Position;
            foreach (WIBattleCharacterState character in runtime.Characters)
            {
                if (character.SquadId == squadId)
                {
                    character.FollowOffset = character.Position - leader.Position;
                }
            }
            SetSquadCommand(runtime, new[] { squadId }, WIBattleCommand.Follow, string.Empty);
            runtime.ControlledHeroId = leader.HeroId;
            return true;
        }

        // 직접 지휘 중인 영웅의 이동 목적지를 지정합니다.
        public static void OrderControlledHeroMove(WIBattleConfigSO config, WIBattleRuntimeState runtime, Vector2 destination)
        {
            WIBattleCharacterState hero = runtime.Characters.Find(item => item.HeroId == runtime.ControlledHeroId && item.IsAlive == true);
            if (hero == null)
            {
                return;
            }
            Vector2 halfSize = config.ArenaSize * 0.5f;
            hero.FormationPosition = new Vector2(
                Mathf.Clamp(destination.x, -halfSize.x, halfSize.x),
                Mathf.Clamp(destination.y, -halfSize.y, halfSize.y));
        }

        // 직접 지휘를 끝내고 분대가 현재 위치를 지키게 합니다.
        public static void StopHeroControl(WIBattleRuntimeState runtime)
        {
            WIBattleCharacterState hero = runtime.Characters.Find(item => item.HeroId == runtime.ControlledHeroId);
            runtime.ControlledHeroId = string.Empty;
            if (hero == null)
            {
                return;
            }
            foreach (WIBattleCharacterState character in runtime.Characters)
            {
                if (character.SquadId == hero.SquadId)
                {
                    character.FormationPosition = character.Position;
                }
            }
            SetSquadCommand(runtime, new[] { hero.SquadId }, WIBattleCommand.Hold, string.Empty);
        }
    }
}

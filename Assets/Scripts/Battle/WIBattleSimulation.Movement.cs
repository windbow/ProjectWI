using System.Collections.Generic;
using UnityEngine;

namespace ProjectWI.Battle
{
    public static partial class WIBattleSimulation
    {
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
            if (config.UseHiddenGrid == true)
            {
                HashSet<WIGridCoordinate> occupied = CollectOccupiedCells(runtime, target);
                WIGridCoordinate coordinate = WIHiddenBattleGrid.FindNearestAvailable(
                    target.Position,
                    config.ArenaSize,
                    config.GridCellWidth,
                    config.GridCellHeight,
                    occupied);
                target.GridColumn = coordinate.Column;
                target.GridRow = coordinate.Row;
                target.GridDestinationColumn = coordinate.Column;
                target.GridDestinationRow = coordinate.Row;
                target.HasGridDestination = false;
                target.Position = WIHiddenBattleGrid.GridToWorld(coordinate, config.GridCellWidth, config.GridCellHeight);
            }
        }

        // 살아 있는 모든 인물의 겹침을 해소해 아군 진형과 적 전선이 한 점에 포개지지 않게 합니다.
        private static void ResolveCharacterCollisions(WIBattleConfigSO config, WIBattleRuntimeState runtime)
        {
            if (config.UseHiddenGrid == true)
            {
                return;
            }
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

        // 전투 방식에 따라 연속 좌표 이동 또는 숨은 육각 셀 이동을 시작합니다.
        private static void MoveCharacter(
            WIBattleConfigSO config,
            WIBattleRuntimeState runtime,
            WIBattleCharacterState actor,
            Vector2 desiredPosition,
            float speed,
            float deltaTime)
        {
            if (config.UseHiddenGrid == false)
            {
                actor.Position = Vector2.MoveTowards(actor.Position, desiredPosition, speed * deltaTime);
                return;
            }

            WIGridCoordinate current = new WIGridCoordinate(actor.GridColumn, actor.GridRow);
            HashSet<WIGridCoordinate> occupied = CollectOccupiedCells(runtime, actor);
            WIGridCoordinate best = current;
            float bestDistance = Vector2.SqrMagnitude(actor.Position - desiredPosition);
            IReadOnlyList<WIGridCoordinate> neighbors = WIHiddenBattleGrid.GetNeighbors(current);
            for (int index = 0; index < neighbors.Count; index += 1)
            {
                WIGridCoordinate candidate = neighbors[index];
                if (occupied.Contains(candidate))
                {
                    continue;
                }
                Vector2 candidatePosition = WIHiddenBattleGrid.GridToWorld(
                    candidate,
                    config.GridCellWidth,
                    config.GridCellHeight);
                if (WIHiddenBattleGrid.IsInsideArena(candidatePosition, config.ArenaSize) == false)
                {
                    continue;
                }
                float distance = Vector2.SqrMagnitude(candidatePosition - desiredPosition);
                if (distance < bestDistance)
                {
                    best = candidate;
                    bestDistance = distance;
                }
            }
            if (best.Equals(current))
            {
                return;
            }
            actor.GridDestinationColumn = best.Column;
            actor.GridDestinationRow = best.Row;
            actor.HasGridDestination = true;
            ContinueGridMovement(config, actor, speed, deltaTime);
        }

        // 예약한 인접 육각 셀 중심까지 캐릭터를 부드럽게 이동시키고 도착 좌표를 확정합니다.
        private static void ContinueGridMovement(
            WIBattleConfigSO config,
            WIBattleCharacterState actor,
            float speed,
            float deltaTime)
        {
            WIGridCoordinate destination = new WIGridCoordinate(
                actor.GridDestinationColumn,
                actor.GridDestinationRow);
            Vector2 destinationPosition = WIHiddenBattleGrid.GridToWorld(
                destination,
                config.GridCellWidth,
                config.GridCellHeight);
            actor.Position = Vector2.MoveTowards(actor.Position, destinationPosition, speed * deltaTime);
            if (Vector2.Distance(actor.Position, destinationPosition) > config.GridArrivalDistance)
            {
                return;
            }
            actor.Position = destinationPosition;
            actor.GridColumn = destination.Column;
            actor.GridRow = destination.Row;
            actor.HasGridDestination = false;
        }

        // 현재 캐릭터를 제외하고 살아 있는 인물의 점유 셀과 이동 예약 셀을 수집합니다.
        private static HashSet<WIGridCoordinate> CollectOccupiedCells(
            WIBattleRuntimeState runtime,
            WIBattleCharacterState excluded)
        {
            HashSet<WIGridCoordinate> occupied = new HashSet<WIGridCoordinate>();
            foreach (WIBattleCharacterState character in runtime.Characters)
            {
                if (character == excluded || character.IsAlive == false)
                {
                    continue;
                }
                occupied.Add(new WIGridCoordinate(character.GridColumn, character.GridRow));
                if (character.HasGridDestination == true)
                {
                    occupied.Add(new WIGridCoordinate(
                        character.GridDestinationColumn,
                        character.GridDestinationRow));
                }
            }
            return occupied;
        }

        // 근접 공격자가 목표 주변 여덟 셀 가운데 현재 위치에서 가장 가까운 빈 공격 위치를 선택합니다.
        private static Vector2 FindMeleeAttackPosition(
            WIBattleConfigSO config,
            WIBattleRuntimeState runtime,
            WIBattleCharacterState actor,
            WIBattleCharacterState target)
        {
            if (config.UseHiddenGrid == false)
            {
                return target.Position;
            }
            WIGridCoordinate targetCoordinate = new WIGridCoordinate(target.GridColumn, target.GridRow);
            HashSet<WIGridCoordinate> occupied = CollectOccupiedCells(runtime, actor);
            IReadOnlyList<WIGridCoordinate> neighbors = WIHiddenBattleGrid.GetNeighbors(targetCoordinate);
            Vector2 bestPosition = actor.Position;
            float bestDistance = float.MaxValue;
            for (int index = 0; index < neighbors.Count; index += 1)
            {
                WIGridCoordinate candidate = neighbors[index];
                if (occupied.Contains(candidate))
                {
                    continue;
                }
                Vector2 position = WIHiddenBattleGrid.GridToWorld(
                    candidate,
                    config.GridCellWidth,
                    config.GridCellHeight);
                if (WIHiddenBattleGrid.IsInsideArena(position, config.ArenaSize) == false)
                {
                    continue;
                }
                float distance = Vector2.SqrMagnitude(position - actor.Position);
                if (distance < bestDistance)
                {
                    bestPosition = position;
                    bestDistance = distance;
                }
            }
            return bestPosition;
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

    }
}

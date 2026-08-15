using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectWI.Battle
{
    [Serializable]
    public readonly struct WIGridCoordinate : IEquatable<WIGridCoordinate>
    {
        public readonly int Column;
        public readonly int Row;

        public WIGridCoordinate(int column, int row)
        {
            Column = column;
            Row = row;
        }

        public bool Equals(WIGridCoordinate other)
        {
            return Column == other.Column && Row == other.Row;
        }

        public override bool Equals(object obj)
        {
            return obj is WIGridCoordinate other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Column, Row);
        }
    }

    public static class WIHiddenBattleGrid
    {
        private static readonly WIGridCoordinate[] NeighborOffsets =
        {
            new WIGridCoordinate(0, 1),
            new WIGridCoordinate(1, 1),
            new WIGridCoordinate(1, 0),
            new WIGridCoordinate(1, -1),
            new WIGridCoordinate(0, -1),
            new WIGridCoordinate(-1, -1),
            new WIGridCoordinate(-1, 0),
            new WIGridCoordinate(-1, 1)
        };

        // 숨은 사각 셀 좌표를 전투 화면의 월드 좌표로 변환합니다.
        public static Vector2 GridToWorld(WIGridCoordinate coordinate, float cellWidth, float cellHeight)
        {
            return new Vector2(
                coordinate.Column * Mathf.Max(0.01f, cellWidth),
                coordinate.Row * Mathf.Max(0.01f, cellHeight));
        }

        // 전투 월드 좌표에서 가장 가까운 숨은 사각 셀 좌표를 반환합니다.
        public static WIGridCoordinate WorldToGrid(Vector2 position, float cellWidth, float cellHeight)
        {
            return new WIGridCoordinate(
                Mathf.RoundToInt(position.x / Mathf.Max(0.01f, cellWidth)),
                Mathf.RoundToInt(position.y / Mathf.Max(0.01f, cellHeight)));
        }

        // 상하좌우와 네 대각선을 포함한 여덟 인접 셀을 반환합니다.
        public static IReadOnlyList<WIGridCoordinate> GetNeighbors(WIGridCoordinate coordinate)
        {
            WIGridCoordinate[] neighbors = new WIGridCoordinate[NeighborOffsets.Length];
            for (int index = 0; index < NeighborOffsets.Length; index += 1)
            {
                neighbors[index] = new WIGridCoordinate(
                    coordinate.Column + NeighborOffsets[index].Column,
                    coordinate.Row + NeighborOffsets[index].Row);
            }
            return neighbors;
        }

        // 원하는 위치와 가장 가까우면서 비어 있고 전장 안에 있는 셀을 찾습니다.
        public static WIGridCoordinate FindNearestAvailable(
            Vector2 desiredPosition,
            Vector2 arenaSize,
            float cellWidth,
            float cellHeight,
            ISet<WIGridCoordinate> occupied)
        {
            WIGridCoordinate center = WorldToGrid(desiredPosition, cellWidth, cellHeight);
            for (int radius = 0; radius <= 64; radius += 1)
            {
                WIGridCoordinate best = center;
                float bestDistance = float.MaxValue;
                bool found = false;
                for (int columnOffset = -radius; columnOffset <= radius; columnOffset += 1)
                {
                    for (int rowOffset = -radius; rowOffset <= radius; rowOffset += 1)
                    {
                        if (Mathf.Max(Mathf.Abs(columnOffset), Mathf.Abs(rowOffset)) != radius)
                        {
                            continue;
                        }
                        WIGridCoordinate candidate = new WIGridCoordinate(
                            center.Column + columnOffset,
                            center.Row + rowOffset);
                        if (occupied != null && occupied.Contains(candidate))
                        {
                            continue;
                        }
                        Vector2 world = GridToWorld(candidate, cellWidth, cellHeight);
                        if (IsInsideArena(world, arenaSize) == false)
                        {
                            continue;
                        }
                        float distance = Vector2.SqrMagnitude(world - desiredPosition);
                        if (distance < bestDistance)
                        {
                            best = candidate;
                            bestDistance = distance;
                            found = true;
                        }
                    }
                }
                if (found)
                {
                    return best;
                }
            }
            return center;
        }

        // 셀 중심이 전투 판정 영역 안에 있는지 반환합니다.
        public static bool IsInsideArena(Vector2 position, Vector2 arenaSize)
        {
            Vector2 halfSize = arenaSize * 0.5f;
            return position.x >= -halfSize.x && position.x <= halfSize.x &&
                position.y >= -halfSize.y && position.y <= halfSize.y;
        }
    }
}

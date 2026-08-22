using System.Collections.Generic;
using NUnit.Framework;
using ProjectWI.Battle;
using UnityEngine;

namespace ProjectWI.Tests.Editor
{
    public class WIHiddenBattleGridTests
    {
        // 사각 셀 좌표를 월드로 변환한 뒤 되돌려도 원래 좌표가 유지되는지 확인합니다.
        [Test]
        public void GridWorldRoundTripPreservesCoordinate()
        {
            WIGridCoordinate coordinate = new WIGridCoordinate(7, -4);
            Vector2 world = WIHiddenBattleGrid.GridToWorld(coordinate, 0.6f, 0.3f);
            WIGridCoordinate restored = WIHiddenBattleGrid.WorldToGrid(world, 0.6f, 0.3f);

            Assert.That(restored, Is.EqualTo(coordinate));
        }

        // 숨은 사각 셀이 정면과 대각선을 포함한 여덟 이동 방향을 제공하는지 확인합니다.
        [Test]
        public void GridProvidesEightMovementDirections()
        {
            IReadOnlyList<WIGridCoordinate> neighbors = WIHiddenBattleGrid.GetNeighbors(new WIGridCoordinate(0, 0));

            Assert.That(neighbors.Count, Is.EqualTo(8));
            Assert.That(neighbors, Does.Contain(new WIGridCoordinate(0, 1)));
            Assert.That(neighbors, Does.Contain(new WIGridCoordinate(0, -1)));
            Assert.That(neighbors, Does.Contain(new WIGridCoordinate(1, 0)));
            Assert.That(neighbors, Does.Contain(new WIGridCoordinate(-1, 0)));
            Assert.That(neighbors, Does.Contain(new WIGridCoordinate(1, 1)));
        }

        // 같은 희망 위치를 반복 요청해도 60명에게 서로 다른 빈 셀이 배정되는지 확인합니다.
        [Test]
        public void NearestAvailableAvoidsOccupiedCells()
        {
            HashSet<WIGridCoordinate> occupied = new HashSet<WIGridCoordinate>();
            for (int index = 0; index < 60; index += 1)
            {
                WIGridCoordinate coordinate = WIHiddenBattleGrid.FindNearestAvailable(
                    Vector2.zero,
                    new Vector2(18f, 10f),
                    0.6f,
                    0.3f,
                    occupied);
                Assert.That(occupied.Add(coordinate), Is.True);
            }

            Assert.That(occupied.Count, Is.EqualTo(60));
        }
    }
}

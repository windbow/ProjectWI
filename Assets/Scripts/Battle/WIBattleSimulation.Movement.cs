using System.Collections.Generic;
using UnityEngine;

namespace ProjectWI.Battle
{
    public static partial class WIBattleSimulation
    {
        // 충돌 해소 대상 생존 인물 목록을 틱마다 재사용합니다.
        private static readonly List<WIBattleCharacterState> collisionCharacters = new List<WIBattleCharacterState>();
        // 공간 해시 셀 키별 인물 색인 목록입니다.
        private static readonly Dictionary<long, List<int>> collisionBuckets = new Dictionary<long, List<int>>();
        // 공간 해시 셀 목록 객체를 재사용하기 위한 풀입니다.
        private static readonly Stack<List<int>> collisionBucketPool = new Stack<List<int>>();

        // 근접 공격에 맞은 대상을 공격자 반대 방향으로 밀어내며 사수 진형은 밀치기를 완화합니다.
        private static void ApplyMeleeKnockback(
            WIBattleConfigSO config,
            WIBattleRuntimeState runtime,
            WIBattleCharacterState actor,
            WIBattleCharacterState target,
            float multiplier = 1f)
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
            float resistance = GetCommand(runtime, target) == WIBattleCommand.Hold || IsBraced(target) == true
                ? config.HoldKnockbackResistance
                : 0f;
            target.Position += direction.normalized * config.MeleeKnockbackDistance * multiplier * (1f - resistance);
            ClampToArena(config, target);
        }

        // 살아 있는 모든 인물의 겹침을 해소해 아군 진형과 적 전선이 한 점에 포개지지 않게 합니다.
        private static void ResolveCharacterCollisions(WIBattleConfigSO config, WIBattleRuntimeState runtime)
        {
            collisionCharacters.Clear();
            foreach (WIBattleCharacterState character in runtime.Characters)
            {
                if (character.IsAlive == true)
                {
                    collisionCharacters.Add(character);
                }
            }
            float cellSize = config.MinimumUnitSpacing * config.SpreadSpacingMultiplier;
            BuildCollisionBuckets(cellSize);
            for (int leftIndex = 0; leftIndex < collisionCharacters.Count; leftIndex += 1)
            {
                WIBattleCharacterState left = collisionCharacters[leftIndex];
                int cellX = Mathf.FloorToInt(left.Position.x / cellSize);
                int cellY = Mathf.FloorToInt(left.Position.y / cellSize);
                for (int offsetX = -1; offsetX <= 1; offsetX += 1)
                {
                    for (int offsetY = -1; offsetY <= 1; offsetY += 1)
                    {
                        if (collisionBuckets.TryGetValue(GetCellKey(cellX + offsetX, cellY + offsetY), out List<int> bucket) == false)
                        {
                            continue;
                        }
                        foreach (int rightIndex in bucket)
                        {
                            if (rightIndex > leftIndex)
                            {
                                SeparatePair(config, runtime, leftIndex, rightIndex);
                            }
                        }
                    }
                }
            }
        }

        // 두 인물이 최소 간격보다 가까우면 질량 비율에 따라 서로 밀어냅니다. 분산 중인 같은 진영은 간격을 넓힙니다.
        private static void SeparatePair(WIBattleConfigSO config, WIBattleRuntimeState runtime, int leftIndex, int rightIndex)
        {
            WIBattleCharacterState left = collisionCharacters[leftIndex];
            WIBattleCharacterState right = collisionCharacters[rightIndex];
            float spacing = config.MinimumUnitSpacing;
            if (left.Side == right.Side &&
                (GetCommand(runtime, left) == WIBattleCommand.Spread || GetCommand(runtime, right) == WIBattleCommand.Spread))
            {
                spacing *= config.SpreadSpacingMultiplier;
            }
            Vector2 delta = right.Position - left.Position;
            float distance = delta.magnitude;
            if (distance >= spacing)
            {
                return;
            }
            Vector2 direction = distance > 0.0001f
                ? delta / distance
                : GetFallbackSeparationDirection(left, right, leftIndex, rightIndex);
            float correction = (spacing - distance) * config.CollisionResolveStrength;
            float leftMass = GetCollisionMass(config, left);
            float rightMass = GetCollisionMass(config, right);
            float totalMass = leftMass + rightMass;
            left.Position -= direction * correction * (rightMass / totalMass);
            right.Position += direction * correction * (leftMass / totalMass);
            ClampToArena(config, left);
            ClampToArena(config, right);
        }

        // 충돌 검사 대상 인물을 셀 크기 기준 공간 해시에 넣어 주변 셀만 비교하게 합니다.
        private static void BuildCollisionBuckets(float cellSize)
        {
            foreach (List<int> bucket in collisionBuckets.Values)
            {
                bucket.Clear();
                collisionBucketPool.Push(bucket);
            }
            collisionBuckets.Clear();
            for (int index = 0; index < collisionCharacters.Count; index += 1)
            {
                Vector2 position = collisionCharacters[index].Position;
                long key = GetCellKey(Mathf.FloorToInt(position.x / cellSize), Mathf.FloorToInt(position.y / cellSize));
                if (collisionBuckets.TryGetValue(key, out List<int> bucket) == false)
                {
                    bucket = collisionBucketPool.Count > 0 ? collisionBucketPool.Pop() : new List<int>();
                    collisionBuckets[key] = bucket;
                }
                bucket.Add(index);
            }
        }

        // 두 정수 셀 좌표를 공간 해시 키 하나로 합칩니다.
        private static long GetCellKey(int cellX, int cellY)
        {
            return ((long)cellX << 32) ^ (uint)cellY;
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

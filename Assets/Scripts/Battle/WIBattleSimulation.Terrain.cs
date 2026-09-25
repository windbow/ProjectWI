using System.Collections.Generic;
using UnityEngine;

namespace ProjectWI.Battle
{
    public static partial class WIBattleSimulation
    {
        // 좌표가 지정한 종류의 지형 구역 안에 있는지 반환합니다.
        public static bool IsInTerrain(WIBattleConfigSO config, Vector2 position, WIBattleTerrainType terrainType)
        {
            IReadOnlyList<WIBattleTerrainZoneDefinition> zones = config.TerrainZones;
            for (int index = 0; index < zones.Count; index += 1)
            {
                WIBattleTerrainZoneDefinition zone = zones[index];
                if (zone.TerrainType == terrainType && Vector2.Distance(position, zone.Center) <= zone.Radius)
                {
                    return true;
                }
            }
            return false;
        }

        // 좌표가 속한 첫 지형 구역 종류를 반환하며 없으면 false를 반환합니다.
        public static bool TryGetTerrainAt(WIBattleConfigSO config, Vector2 position, out WIBattleTerrainType terrainType)
        {
            IReadOnlyList<WIBattleTerrainZoneDefinition> zones = config.TerrainZones;
            for (int index = 0; index < zones.Count; index += 1)
            {
                if (Vector2.Distance(position, zones[index].Center) <= zones[index].Radius)
                {
                    terrainType = zones[index].TerrainType;
                    return true;
                }
            }
            terrainType = WIBattleTerrainType.HighGround;
            return false;
        }

        // 고지에 선 원거리 인물은 사거리가 늘어난 실제 공격 사거리를 반환합니다.
        public static float GetEffectiveAttackRange(WIBattleConfigSO config, WIBattleCharacterState actor)
        {
            if (UsesProjectile(actor) == true && IsInTerrain(config, actor.Position, WIBattleTerrainType.HighGround) == true)
            {
                return actor.AttackRange * config.HighGroundRangeMultiplier;
            }
            return actor.AttackRange;
        }

        // 숲 안에서는 이동 속도를 줄이는 배율을 반환합니다.
        private static float GetMoveSpeedMultiplier(WIBattleConfigSO config, WIBattleCharacterState actor)
        {
            return IsInTerrain(config, actor.Position, WIBattleTerrainType.Forest) == true
                ? config.ForestMoveSpeedMultiplier
                : 1f;
        }

        // 좁은 길 안의 대상은 근접 교전 슬롯 수가 줄어든 값을 반환합니다.
        public static int GetMeleeSlotCountFor(WIBattleConfigSO config, WIBattleCharacterState target)
        {
            return IsInTerrain(config, target.Position, WIBattleTerrainType.Narrow) == true
                ? Mathf.Min(config.MeleeSlotCount, config.NarrowMeleeSlotCount)
                : config.MeleeSlotCount;
        }

        // 고지 원거리 공격자와 숲 안 대상에 따른 투사체 피해 배율을 반환합니다.
        public static float GetProjectileTerrainMultiplier(
            WIBattleConfigSO config,
            WIBattleCharacterState shooter,
            WIBattleCharacterState target)
        {
            float multiplier = 1f;
            if (shooter != null && IsInTerrain(config, shooter.Position, WIBattleTerrainType.HighGround) == true)
            {
                multiplier *= config.HighGroundDamageMultiplier;
            }
            if (IsInTerrain(config, target.Position, WIBattleTerrainType.Forest) == true)
            {
                multiplier *= config.ForestProjectileDamageMultiplier;
            }
            return multiplier;
        }

        // 진영별 배치 가능 구역의 가로 범위를 반환합니다.
        public static Vector2 GetDeploymentRangeX(WIBattleConfigSO config, WIBattleSide side)
        {
            float half = config.ArenaSize.x * 0.5f;
            float depth = config.ArenaSize.x * config.DeploymentZoneDepthRatio;
            return side == WIBattleSide.Attacker
                ? new Vector2(-half, -half + depth)
                : new Vector2(half - depth, half);
        }
    }
}

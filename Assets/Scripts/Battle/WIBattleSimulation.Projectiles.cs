using ProjectWI.Administration;
using UnityEngine;

namespace ProjectWI.Battle
{
    public static partial class WIBattleSimulation
    {
        // 현재 역할이 실제 이동 발사체를 사용하는 원거리 계열인지 반환합니다.
        private static bool UsesProjectile(WIBattleCharacterState actor)
        {
            return actor.Role == WIUnitRole.Ranged || actor.Role == WIUnitRole.Magic || actor.Role == WIUnitRole.Support;
        }

        // 원거리 공격을 즉시 피해로 처리하지 않고 충돌 판정을 가진 발사체 상태로 생성합니다.
        private static void LaunchProjectile(
            WIBattleConfigSO config,
            WIBattleRuntimeState runtime,
            WIBattleCharacterState actor,
            WIBattleCharacterState target,
            int damage)
        {
            runtime.Projectiles.Add(new WIBattleProjectileState
            {
                ProjectileId = runtime.NextProjectileId,
                ShooterHeroId = actor.HeroId,
                TargetHeroId = target.HeroId,
                Side = actor.Side,
                Position = actor.Position,
                TargetPosition = target.Position,
                Damage = damage,
                RemainingLifetime = config.ProjectileLifetime
            });
            runtime.NextProjectileId += 1;
        }

        // 모든 발사체를 이동시키고 선분 충돌, 수명 만료와 선택적 아군 오발을 처리합니다.
        private static void AdvanceProjectiles(WIBattleConfigSO config, WIBattleRuntimeState runtime, float deltaTime)
        {
            for (int projectileIndex = runtime.Projectiles.Count - 1; projectileIndex >= 0; projectileIndex -= 1)
            {
                WIBattleProjectileState projectile = runtime.Projectiles[projectileIndex];
                projectile.RemainingLifetime -= deltaTime;
                WIBattleCharacterState target = runtime.Characters.Find(item =>
                    item.HeroId == projectile.TargetHeroId && item.IsAlive);
                if (target != null)
                {
                    projectile.TargetPosition = target.Position;
                }
                Vector2 previousPosition = projectile.Position;
                projectile.Position = Vector2.MoveTowards(
                    projectile.Position,
                    projectile.TargetPosition,
                    config.ProjectileSpeed * deltaTime);
                projectile.TravelledDistance += Vector2.Distance(previousPosition, projectile.Position);

                WIBattleCharacterState hit = FindProjectileHit(
                    config, runtime, projectile, previousPosition, projectile.Position);
                if (hit != null)
                {
                    hit.Health = Mathf.Max(0, hit.Health - Mathf.Max(1, projectile.Damage));
                    runtime.Projectiles.RemoveAt(projectileIndex);
                    continue;
                }
                if (projectile.RemainingLifetime <= 0f ||
                    projectile.Position == projectile.TargetPosition && target == null)
                {
                    runtime.Projectiles.RemoveAt(projectileIndex);
                }
            }
        }

        // 발사체 이동 선분과 가장 먼저 충돌하는 유효 인물을 반환합니다.
        private static WIBattleCharacterState FindProjectileHit(
            WIBattleConfigSO config,
            WIBattleRuntimeState runtime,
            WIBattleProjectileState projectile,
            Vector2 start,
            Vector2 end)
        {
            bool allowFriendlyFire = config.ProjectileFriendlyFireEnabled &&
                projectile.TravelledDistance >= config.ProjectileFriendlyFireSafeDistance;
            WIBattleCharacterState bestHit = null;
            float bestAlong = float.MaxValue;
            foreach (WIBattleCharacterState character in runtime.Characters)
            {
                if (character.IsAlive == false || character.HeroId == projectile.ShooterHeroId)
                {
                    continue;
                }
                if (character.Side == projectile.Side && allowFriendlyFire == false)
                {
                    continue;
                }
                if (DistanceToSegment(character.Position, start, end) > config.ProjectileCollisionRadius)
                {
                    continue;
                }
                float along = Vector2.SqrMagnitude(character.Position - start);
                if (along >= bestAlong)
                {
                    continue;
                }
                bestAlong = along;
                bestHit = character;
            }
            return bestHit;
        }

        // 한 점과 발사체 이동 선분 사이의 최단 거리를 계산합니다.
        private static float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
        {
            Vector2 segment = end - start;
            if (segment.sqrMagnitude <= 0.0001f)
            {
                return Vector2.Distance(point, start);
            }
            float amount = Mathf.Clamp01(Vector2.Dot(point - start, segment) / segment.sqrMagnitude);
            return Vector2.Distance(point, start + segment * amount);
        }
    }
}

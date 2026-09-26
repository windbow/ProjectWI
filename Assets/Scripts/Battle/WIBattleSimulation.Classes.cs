using System.Collections.Generic;
using UnityEngine;

namespace ProjectWI.Battle
{
    public static partial class WIBattleSimulation
    {
        // 이만큼 이하로 움직인 틱은 멈춰 선 것으로 보고 돌격 거리를 초기화합니다.
        private const float StationaryStepDistance = 0.01f;

        // 이번 틱 이동 거리로 돌격 거리를 누적하고, 멈춰 서면 초기화합니다.
        private static void TrackChargeDistance(WIBattleCharacterState actor, Vector2 previousPosition)
        {
            float moved = Vector2.Distance(actor.Position, previousPosition);
            actor.ChargeDistance = moved <= StationaryStepDistance ? 0f : actor.ChargeDistance + moved;
        }

        // 방진 병과가 멈춰 서서 방어 태세를 갖췄는지 반환합니다.
        public static bool IsBraced(WIBattleCharacterState character)
        {
            return character.Archetype == WIBattleArchetype.Shield && character.ChargeDistance <= StationaryStepDistance;
        }

        // 돌격 병과가 충분히 달려온 상태인지 반환합니다.
        public static bool IsChargeReady(WIBattleConfigSO config, WIBattleCharacterState actor)
        {
            return actor.Archetype == WIBattleArchetype.Charger && actor.ChargeDistance >= config.ChargeMinDistance;
        }

        // 병과 규칙(측면·후방, 방진, 돌격, 유격)을 반영한 근접 피해 배율을 반환합니다. 돌격 충격이면 isCharge가 참입니다.
        public static float GetMeleeClassMultiplier(
            WIBattleConfigSO config,
            WIBattleCharacterState actor,
            WIBattleCharacterState target,
            out float flankMultiplier,
            out bool isCharge)
        {
            flankMultiplier = GetFlankMultiplier(config, actor, target);
            bool rear = flankMultiplier >= config.RearDamageMultiplier && config.RearDamageMultiplier > 1f;
            if (rear == true && actor.Archetype == WIBattleArchetype.Skirmisher)
            {
                flankMultiplier = config.SkirmisherRearMultiplier;
            }
            float multiplier = flankMultiplier;
            bool braced = IsBraced(target);
            bool frontal = flankMultiplier <= 1f;
            if (braced == true && frontal == true)
            {
                multiplier *= config.BraceFrontDamageMultiplier;
            }
            else if (target.Archetype == WIBattleArchetype.Shield && frontal == false)
            {
                multiplier *= config.BraceFlankExtraMultiplier;
            }
            isCharge = IsChargeReady(config, actor) == true && (braced == false || frontal == false);
            if (isCharge == true)
            {
                multiplier *= config.ChargeDamageMultiplier;
            }
            return multiplier;
        }

        // 지원 병과가 사거리 안에서 가장 많이 다친 아군을 치료했으면 true를 반환합니다.
        private static bool TryHealAlly(WIBattleConfigSO config, WIBattleRuntimeState runtime, WIBattleCharacterState actor)
        {
            if (actor.Archetype != WIBattleArchetype.Support || actor.CooldownRemaining > 0f || config.SupportHealPower <= 0)
            {
                return false;
            }
            WIBattleCharacterState best = null;
            float bestRatio = config.SupportHealThreshold;
            float range = GetEffectiveAttackRange(config, actor);
            foreach (WIBattleCharacterState ally in runtime.Characters)
            {
                if (ally.IsAlive == false || ally.Side != actor.Side || ally.MaxHealth <= 0)
                {
                    continue;
                }
                float ratio = (float)ally.Health / ally.MaxHealth;
                if (ratio >= bestRatio || Vector2.Distance(ally.Position, actor.Position) > range)
                {
                    continue;
                }
                bestRatio = ratio;
                best = ally;
            }
            if (best == null)
            {
                return false;
            }
            best.Health = Mathf.Min(best.MaxHealth, best.Health + config.SupportHealPower);
            actor.CooldownRemaining = GetAttackInterval(config, actor);
            runtime.VisualEffects.Add(new WIBattleVisualEffectState
            {
                EffectId = runtime.NextVisualEffectId,
                EffectType = WIBattleVisualEffectType.SkillHeal,
                Side = actor.Side,
                StartPosition = actor.Position,
                EndPosition = best.Position,
                StartedAt = runtime.ElapsedSeconds,
                Duration = config.SkillVisualDuration * 0.6f,
                Radius = 0.45f
            });
            runtime.NextVisualEffectId += 1;
            return true;
        }

        // 궁병이 쏠 때 같은 분대의 준비된 궁병도 같은 표적에 함께 쏘게 합니다(일제 사격).
        private static void FireVolley(
            WIBattleConfigSO config,
            WIBattleRuntimeState runtime,
            WIBattleCharacterState leader,
            WIBattleCharacterState target,
            int damage)
        {
            foreach (WIBattleCharacterState archer in runtime.Characters)
            {
                if (archer == leader || archer.IsAlive == false || archer.SquadId != leader.SquadId ||
                    archer.Archetype != WIBattleArchetype.Archer || archer.CooldownRemaining > 0f ||
                    UsesProjectile(archer) == false ||
                    Vector2.Distance(archer.Position, target.Position) > GetEffectiveAttackRange(config, archer))
                {
                    continue;
                }
                LaunchProjectile(config, runtime, archer, target, Mathf.Max(1, Mathf.RoundToInt(damage * (float)archer.AttackDamage / Mathf.Max(1, leader.AttackDamage))));
                archer.CooldownRemaining = GetAttackInterval(config, archer);
            }
        }

        // 인물의 공격 간격(직업 배율 반영)을 반환합니다.
        public static float GetAttackInterval(WIBattleConfigSO config, WIBattleCharacterState actor)
        {
            return actor.AttackInterval > 0f ? actor.AttackInterval : config.AttackCooldown;
        }

        // 지휘 병과 주변의 아군 분대를 사기 보호 상태로 표시합니다.
        private static void UpdateCommandAuras(WIBattleConfigSO config, WIBattleRuntimeState runtime)
        {
            float radiusSquared = config.CommandAuraRadius * config.CommandAuraRadius;
            foreach (WIBattleSquadState squad in runtime.Squads)
            {
                squad.InCommandAura = false;
            }
            foreach (WIBattleCharacterState commander in runtime.Characters)
            {
                if (commander.IsAlive == false || commander.Archetype != WIBattleArchetype.Commander)
                {
                    continue;
                }
                foreach (WIBattleCharacterState ally in runtime.Characters)
                {
                    if (ally.IsAlive == false || ally.Side != commander.Side ||
                        Vector2.SqrMagnitude(ally.Position - commander.Position) > radiusSquared)
                    {
                        continue;
                    }
                    WIBattleSquadState squad = FindSquad(runtime, ally.SquadId);
                    if (squad != null)
                    {
                        squad.InCommandAura = true;
                    }
                }
            }
        }

        // 지정 지점 주변 적에게 범위 피해를 주고 맞은 수를 반환합니다. excluded는 이미 직접 맞은 인물입니다.
        private static int ApplySplashDamage(
            WIBattleConfigSO config,
            WIBattleRuntimeState runtime,
            WIBattleSide attackerSide,
            Vector2 center,
            float radius,
            int damage,
            WIBattleCharacterState excluded)
        {
            if (radius <= 0f || damage <= 0)
            {
                return 0;
            }
            List<WIBattleCharacterState> victims = new List<WIBattleCharacterState>();
            float radiusSquared = radius * radius;
            foreach (WIBattleCharacterState character in runtime.Characters)
            {
                if (character != excluded && character.IsAlive == true && character.Side != attackerSide &&
                    Vector2.SqrMagnitude(character.Position - center) <= radiusSquared)
                {
                    victims.Add(character);
                }
            }
            foreach (WIBattleCharacterState victim in victims)
            {
                int scaled = Mathf.RoundToInt(damage * GetProjectileTerrainMultiplier(config, null, victim));
                ApplyDamage(config, runtime, victim, scaled, 1f);
                AddHitVisual(config, runtime, victim);
            }
            return victims.Count;
        }
    }
}

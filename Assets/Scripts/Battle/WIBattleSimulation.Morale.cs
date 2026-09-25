using UnityEngine;

namespace ProjectWI.Battle
{
    public static partial class WIBattleSimulation
    {
        // 분대 사기 상한입니다.
        public const float MaxMorale = 100f;

        // 피해를 적용하고 측면·후방 피격과 전투 불능에 따른 분대 사기 변화를 처리합니다.
        public static void ApplyDamage(
            WIBattleConfigSO config,
            WIBattleRuntimeState runtime,
            WIBattleCharacterState target,
            int amount,
            float flankMultiplier)
        {
            bool wasAlive = target.IsAlive;
            target.Health = Mathf.Max(0, target.Health - Mathf.Max(1, amount));
            WIBattleSquadState squad = FindSquad(runtime, target.SquadId);
            if (squad == null)
            {
                return;
            }
            if (flankMultiplier >= config.RearDamageMultiplier && config.RearDamageMultiplier > 1f)
            {
                ChangeMorale(squad, -config.MoraleLossRearHit);
            }
            else if (flankMultiplier > 1f)
            {
                ChangeMorale(squad, -config.MoraleLossFlankHit);
            }
            if (wasAlive == false || target.IsAlive == true)
            {
                return;
            }
            ChangeMorale(squad, -config.MoraleLossPerAllyDown);
            if (squad.LeaderHeroId != target.HeroId)
            {
                return;
            }
            ChangeMorale(squad, -config.MoraleLossLeaderDown);
            foreach (WIBattleSquadState other in runtime.Squads)
            {
                if (other != squad && other.Side == squad.Side)
                {
                    ChangeMorale(other, -config.MoraleLossSideOnLeaderDown);
                }
            }
        }

        // 분대 사기를 회복시키고 0이 된 분대는 무너뜨리며, 무너진 분대는 분대장이 살아 있으면 기준 사기에서 복귀시킵니다.
        private static void UpdateMorale(WIBattleConfigSO config, WIBattleRuntimeState runtime, float deltaTime)
        {
            foreach (WIBattleSquadState squad in runtime.Squads)
            {
                if (squad.IsRouting == true)
                {
                    ChangeMorale(squad, config.RoutRecoveryPerSecond * deltaTime);
                    bool leaderAlive = charactersById.ContainsKey(squad.LeaderHeroId ?? string.Empty);
                    if (leaderAlive == true && squad.Morale >= config.RoutRecoverThreshold)
                    {
                        squad.IsRouting = false;
                    }
                }
                else if (squad.Morale <= 0f)
                {
                    squad.IsRouting = true;
                }
                else
                {
                    ChangeMorale(squad, config.MoraleRegenPerSecond * deltaTime);
                }
            }
            foreach (WIBattleCharacterState character in runtime.Characters)
            {
                WIBattleSquadState squad = FindSquad(runtime, character.SquadId);
                character.IsRouting = squad != null && squad.IsRouting == true;
            }
        }

        // 분대 사기를 0~상한 사이로 변경합니다.
        private static void ChangeMorale(WIBattleSquadState squad, float delta)
        {
            squad.Morale = Mathf.Clamp(squad.Morale + delta, 0f, MaxMorale);
        }
    }
}

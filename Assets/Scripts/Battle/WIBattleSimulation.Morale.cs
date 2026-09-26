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
            if (flankMultiplier > 1f)
            {
                AddFeedback(runtime, WIBattleFeedbackType.FlankHit, target.Position, target.Side);
            }
            if (flankMultiplier >= config.RearDamageMultiplier && config.RearDamageMultiplier > 1f)
            {
                ChangeMorale(squad, -config.MoraleLossRearHit * GetMoraleLossScale(config, squad));
            }
            else if (flankMultiplier > 1f)
            {
                ChangeMorale(squad, -config.MoraleLossFlankHit * GetMoraleLossScale(config, squad));
            }
            if (wasAlive == false || target.IsAlive == true)
            {
                return;
            }
            ChangeMorale(squad, -config.MoraleLossPerAllyDown * GetMoraleLossScale(config, squad));
            if (squad.LeaderHeroId != target.HeroId)
            {
                AddFeedback(runtime, WIBattleFeedbackType.Kill, target.Position, target.Side);
                return;
            }
            AddFeedback(runtime, WIBattleFeedbackType.LeaderKill, target.Position, target.Side);
            ChangeMorale(squad, -config.MoraleLossLeaderDown * GetMoraleLossScale(config, squad));
            foreach (WIBattleSquadState other in runtime.Squads)
            {
                if (other != squad && other.Side == squad.Side)
                {
                    ChangeMorale(other, -config.MoraleLossSideOnLeaderDown * GetMoraleLossScale(config, other));
                }
            }
        }

        // 분대 사기를 회복시키고 0이 된 분대는 무너뜨리며, 무너진 분대는 분대장이 살아 있으면 기준 사기에서 복귀시킵니다.
        private static void UpdateMorale(WIBattleConfigSO config, WIBattleRuntimeState runtime, float deltaTime)
        {
            UpdateCommandAuras(config, runtime);
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
                    if (charactersById.TryGetValue(squad.LeaderHeroId ?? string.Empty, out WIBattleCharacterState leader) == true)
                    {
                        AddFeedback(runtime, WIBattleFeedbackType.SquadRouted, leader.Position, squad.Side);
                    }
                }
                else
                {
                    float regen = config.MoraleRegenPerSecond + (squad.InCommandAura == true ? config.CommandAuraMoraleRegen : 0f);
                    ChangeMorale(squad, regen * deltaTime);
                }
            }
            foreach (WIBattleCharacterState character in runtime.Characters)
            {
                WIBattleSquadState squad = FindSquad(runtime, character.SquadId);
                character.IsRouting = squad != null && squad.IsRouting == true;
            }
        }

        // 표시 계층용 연출 사건을 기록하며 소비되지 않을 때를 대비해 개수를 제한합니다.
        private static void AddFeedback(WIBattleRuntimeState runtime, WIBattleFeedbackType type, Vector2 position, WIBattleSide side)
        {
            if (runtime.FeedbackEvents.Count >= 128)
            {
                runtime.FeedbackEvents.RemoveRange(0, 64);
            }
            runtime.FeedbackEvents.Add(new WIBattleFeedbackEvent { Type = type, Position = position, Side = side });
        }

        // 지휘 병과 보호 범위 안 분대는 사기 손실이 줄어드는 배율을 반환합니다.
        private static float GetMoraleLossScale(WIBattleConfigSO config, WIBattleSquadState squad)
        {
            return squad.InCommandAura == true ? config.CommandAuraLossMultiplier : 1f;
        }

        // 인물이 속한 분대의 사기를 깎습니다(지휘 보호 반영).
        private static void ApplyMoraleLoss(WIBattleConfigSO config, WIBattleRuntimeState runtime, WIBattleCharacterState target, float amount)
        {
            WIBattleSquadState squad = FindSquad(runtime, target.SquadId);
            if (squad != null)
            {
                ChangeMorale(squad, -amount * GetMoraleLossScale(config, squad));
            }
        }

        // 분대 사기를 0~상한 사이로 변경합니다.
        private static void ChangeMorale(WIBattleSquadState squad, float delta)
        {
            squad.Morale = Mathf.Clamp(squad.Morale + delta, 0f, MaxMorale);
        }
    }
}

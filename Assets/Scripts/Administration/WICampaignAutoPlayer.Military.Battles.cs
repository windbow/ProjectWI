using System.Linq;
using UnityEngine;

namespace ProjectWI.Administration
{
    public static partial class WICampaignAutoPlayer
    {
        // 플레이어 참가 대기 전투를 전력 스냅샷에 따른 전략 판정으로 모두 해소합니다.
        private static void ResolvePendingBattles(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WIAutoCampaignMetrics metrics,
            WIAutoStrategicPlan plan,
            int month,
            ref int lastBattleMonth)
        {
            foreach (WIBattleSessionState session in state.BattleSessions.Where(item =>
                         item.PlayerInvolved && item.Status != WIBattleSessionStatus.Resolved).ToList())
            {
                WIBattleOutcome attackerOutcome = ResolveStrategicBattleOutcome(state, session);
                int capturedBefore = state.Characters.Count(character => character.Captured);
                int defectedBefore = state.Characters.Count(character =>
                    string.IsNullOrEmpty(character.JoinedEnemyFactionId) == false);
                if (WIAdministrationTurnSystem.SubmitBattleResult(
                        database, state, session.SessionId, attackerOutcome,
                        WIBattleResolutionSource.StrategicFallback, state.LastMonthlyReport) == false)
                {
                    continue;
                }
                metrics.CharacterCaptures += Mathf.Max(0,
                    state.Characters.Count(character => character.Captured) - capturedBefore);
                metrics.CharacterDefections += Mathf.Max(0,
                    state.Characters.Count(character =>
                        string.IsNullOrEmpty(character.JoinedEnemyFactionId) == false) - defectedBefore);

                metrics.BattlesResolved += 1;
                if (session.AttackerFactionId == state.PlayerFactionId &&
                    session.AttackerArmyIds != null && session.AttackerArmyIds.Count >= 2)
                {
                    metrics.JointAttackBattles += 1;
                    metrics.RecordDecision(month, plan, "JOINT_ATTACK_RESOLVED",
                        $"{session.AttackerArmyIds.Count}개 전투단이 {session.CastleId} 공동 공격에 참가했습니다.");
                }
                metrics.BattleIntervals.Add(lastBattleMonth == 0 ? month : month - lastBattleMonth);
                lastBattleMonth = month;
                bool playerWon = session.AttackerFactionId == state.PlayerFactionId
                    ? attackerOutcome == WIBattleOutcome.Victory
                    : attackerOutcome == WIBattleOutcome.Defeat;
                int playerPower = session.AttackerFactionId == state.PlayerFactionId
                    ? session.AttackerPowerSnapshot : session.DefenderPowerSnapshot;
                int enemyPower = session.AttackerFactionId == state.PlayerFactionId
                    ? session.DefenderPowerSnapshot : session.AttackerPowerSnapshot;
                metrics.PlayerBattlePowerMargins.Add(playerPower - enemyPower);
                if (playerWon)
                {
                    metrics.PlayerVictories += 1;
                    plan.ConsecutiveDefeats = 0;
                    plan.RecoveryUntilTurn = 0;
                    if (session.AttackerFactionId == state.PlayerFactionId)
                    {
                        plan.SameTargetDefeats = 0;
                        plan.LastAttackTargetCastleId = string.Empty;
                    }
                }
                else
                {
                    metrics.PlayerDefeats += 1;
                    plan.LastDefeatTurn = state.Turn;
                    plan.ConsecutiveDefeats += 1;
                    plan.RecoveryUntilTurn = state.Turn + Mathf.Min(6, 2 + plan.ConsecutiveDefeats);
                    plan.Phase = WIAutoStrategicPhase.Recover;
                    metrics.RecordDecision(month, plan, "BATTLE_DEFEAT", $"패전 후 {plan.RecoveryUntilTurn - state.Turn}개월 회복 계획을 세웠습니다.");
                    if (session.AttackerFactionId == state.PlayerFactionId)
                    {
                        plan.SameTargetDefeats = plan.LastAttackTargetCastleId == session.CastleId
                            ? plan.SameTargetDefeats + 1 : 1;
                        plan.LastAttackTargetCastleId = session.CastleId;
                        if (plan.SameTargetDefeats >= 2)
                        {
                            plan.AvoidedTargetCastleId = session.CastleId;
                            plan.AvoidedTargetUntilTurn = state.Turn + 18;
                            plan.TargetCastleId = string.Empty;
                            plan.AssemblyCastleId = string.Empty;
                            plan.SameTargetDefeats = 0;
                            metrics.GoalsAbandoned += 1;
                            metrics.RecordDecision(month, plan, "GOAL_ABANDONED",
                                $"{session.CastleId} 연속 패배로 18개월 동안 다른 목표를 검토합니다.");
                        }
                    }
                }
            }
        }

        // 전략 자동 전투에 난이도별 전술 변동을 적용해 우세한 공격도 항상 확정 승리가 되지 않게 합니다.
        public static WIBattleOutcome ResolveStrategicBattleOutcome(
            WIAdministrationState state,
            WIBattleSessionState session)
        {
            if (state == null || session == null)
            {
                return WIBattleOutcome.Defeat;
            }

            int variation = state.Difficulty == WICampaignDifficulty.Relaxed ? 5 :
                state.Difficulty == WICampaignDifficulty.Hard ? 15 : 12;
            int attackerPercent = 100 + GetStableBattleVariation(state, session.SessionId, "attacker", variation);
            int defenderPercent = 100 + GetStableBattleVariation(state, session.SessionId, "defender", variation);
            int adjustedAttackerPower = session.AttackerPowerSnapshot * attackerPercent / 100;
            int adjustedDefenderPower = session.DefenderPowerSnapshot * defenderPercent / 100;
            bool attackerWon = adjustedAttackerPower > adjustedDefenderPower;
            bool closeBattle = session.AttackerPowerSnapshot * 100 <=
                               session.DefenderPowerSnapshot * 125;
            int setbackChance = state.Difficulty == WICampaignDifficulty.Relaxed ? 5 :
                state.Difficulty == WICampaignDifficulty.Hard ? 25 : 18;
            if (attackerWon && closeBattle &&
                GetStableBattleRoll(state, session.SessionId, "setback") < setbackChance)
            {
                attackerWon = false;
            }
            return attackerWon ? WIBattleOutcome.Victory : WIBattleOutcome.Defeat;
        }

        // 턴·전투 ID·진영 구분으로 저장 후에도 동일한 전술 변동값을 계산합니다.
        private static int GetStableBattleVariation(
            WIAdministrationState state,
            string sessionId,
            string side,
            int variation)
        {
            unchecked
            {
                int hash = state.Turn * 397 + state.SimulationSeed * 7919;
                string key = $"{sessionId}|{side}";
                foreach (char character in key)
                {
                    hash = hash * 31 + character;
                }
                int width = variation * 2 + 1;
                return Mathf.Abs(hash % width) - variation;
            }
        }

        // 전술적 패착 여부에 사용하는 재현 가능한 0~99 값을 반환합니다.
        private static int GetStableBattleRoll(
            WIAdministrationState state,
            string sessionId,
            string salt)
        {
            unchecked
            {
                int hash = state.Turn * 397 + state.SimulationSeed * 7919;
                string key = $"{sessionId}|{salt}";
                foreach (char character in key)
                {
                    hash = hash * 31 + character;
                }
                return (hash & int.MaxValue) % 100;
            }
        }
    }
}

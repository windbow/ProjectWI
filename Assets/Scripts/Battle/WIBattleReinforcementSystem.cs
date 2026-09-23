using System.Collections.Generic;
using System.Linq;
using ProjectWI.Administration;

namespace ProjectWI.Battle
{
    public sealed class WIBattleReinforcementSystem
    {
        // 전투 개시 시 이미 전장으로 이동 중인 전투단의 도착 시각입니다.
        private readonly Dictionary<string, float> arrivals = new Dictionary<string, float>();
        private readonly WIAdministrationState state;
        private readonly WIAdministrationDatabaseSO database;
        private readonly WIBattleSessionState session;
        private string lastArrival = string.Empty;

        // 월 턴을 진행하지 않고 남은 이동 기간을 전투 시간으로 환산해 예약합니다.
        public WIBattleReinforcementSystem(WIBattleConfigSO config, WIAdministrationDatabaseSO database,
            WIAdministrationState state, WIBattleSessionState session)
        {
            this.state = state;
            this.database = database;
            this.session = session;
            foreach (var army in state.Armies.Where(item => item.IsMoving == true && item.TargetCastleId == session.CastleId &&
                (item.FactionId == session.AttackerFactionId || item.FactionId == session.DefenderFactionId)))
            {
                arrivals.Add(army.ArmyId, army.RemainingTravelMonths * config.ReinforcementSecondsPerMonth);
            }
        }

        // 전투 종료나 후퇴 전까지만 도착을 처리하고 기존 캐릭터 상태를 보존해 증원을 추가합니다.
        public void Advance(WIBattleConfigSO config, WIBattleRuntimeState runtime)
        {
            if (runtime.Finished == true || session.Status == WIBattleSessionStatus.Resolved ||
                runtime.AttackerCommand == WIBattleCommand.Retreat || runtime.DefenderCommand == WIBattleCommand.Retreat)
            {
                return;
            }
            foreach (var arrival in arrivals.Where(item => item.Value <= runtime.ElapsedSeconds).ToList())
            {
                arrivals.Remove(arrival.Key);
                var army = state.Armies.Find(item => item.ArmyId == arrival.Key);
                if (army == null || army.IsMoving == false || army.TargetCastleId != session.CastleId ||
                    (army.FactionId != session.AttackerFactionId && army.FactionId != session.DefenderFactionId))
                {
                    continue;
                }
                WIAdministrationTurnSystem.CompleteArmyMarchArrival(database, state, army, state.LastMonthlyReport ?? new WITurnSummary());
                if (WIAdministrationTurnSystem.JoinBattleReinforcement(database, state, session, army) == true)
                {
                    lastArrival = string.Format(database.GetText("UI_BATTLE_REINFORCEMENT_ARRIVED"), army.DisplayName);
                }
            }
            WIBattleRuntimeBuilder.AppendReinforcements(config, database, session, runtime);
        }

        // 기존 전투 상태 문구에 붙일 다음 증원 도착 시간 또는 마지막 도착 안내를 제공합니다.
        public string GetStatus(float elapsedSeconds)
        {
            if (arrivals.Count == 0)
            {
                return lastArrival;
            }
            var next = arrivals.OrderBy(item => item.Value).First();
            var army = state.Armies.Find(item => item.ArmyId == next.Key);
            return string.Format(database.GetText("UI_BATTLE_REINFORCEMENT_PENDING"), army?.DisplayName,
                UnityEngine.Mathf.Max(0f, next.Value - elapsedSeconds));
        }
    }
}

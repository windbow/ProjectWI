using System.Collections.Generic;
using System.Linq;

namespace ProjectWI.Administration
{
    public static partial class WIAdministrationTurnSystem
    {
        // 아군 영토만 통과하는 최단 경로의 다음 성을 너비 우선 탐색으로 찾습니다.
        private static string GetNextFriendlyStep(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            string originId,
            string destinationId,
            string factionId)
        {
            Queue<string> queue = new Queue<string>();
            Dictionary<string, string> previous = new Dictionary<string, string>();
            queue.Enqueue(originId);
            previous[originId] = string.Empty;
            while (queue.Count > 0)
            {
                string current = queue.Dequeue();
                if (current == destinationId)
                {
                    break;
                }
                WICastleRuntimeState currentCastle = state.GetCastle(current);
                if (currentCastle == null)
                {
                    continue;
                }
                foreach (string adjacentId in currentCastle.AdjacentCastleIds)
                {
                    if (previous.ContainsKey(adjacentId) || state.GetCastle(adjacentId)?.FactionId != factionId)
                    {
                        continue;
                    }
                    previous[adjacentId] = current;
                    queue.Enqueue(adjacentId);
                }
            }

            if (previous.ContainsKey(destinationId) == false)
            {
                return string.Empty;
            }
            string step = destinationId;
            while (previous[step] != originId && string.IsNullOrEmpty(previous[step]) == false)
            {
                step = previous[step];
            }
            return step == originId ? string.Empty : step;
        }

        // 현재 이동 및 전투 대기 상태를 기준으로 플레이어 성의 침공 경고를 다시 계산합니다.
        private static void RefreshInvasionWarnings(WIAdministrationDatabaseSO database, WIAdministrationState state)
        {
            foreach (WICastleRuntimeState castle in state.Castles)
            {
                castle.InvasionWarning = false;
            }
            foreach (WIArmyState army in state.Armies.Where(item => item.FactionId != state.PlayerFactionId))
            {
                string threatenedId = army.IsMoving ? army.TargetCastleId : (army.AwaitingBattle ? army.CurrentCastleId : string.Empty);
                WICastleRuntimeState target = state.GetCastle(threatenedId);
                if (target != null && target.FactionId == state.PlayerFactionId)
                {
                    target.InvasionWarning = true;
                }
            }
        }

        // 해당 성이 플레이어 영토와 인접했는지 확인합니다.
        private static bool IsAdjacentToPlayer(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WICastleRuntimeState castle)
        {
            return castle.AdjacentCastleIds.Any(id => state.GetCastle(id)?.FactionId == state.PlayerFactionId);
        }

    }
}

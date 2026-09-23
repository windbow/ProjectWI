using System.Collections.Generic;
using System.Linq;

namespace ProjectWI.Administration
{
    public static partial class WIAdministrationTurnSystem
    {
        // 같은 성의 전투단 전체와 총 비용을 먼저 검사한 뒤 동일한 목표로 함께 출정시킵니다.
        public static bool BeginArmyGroupMarch(WIAdministrationDatabaseSO database, WIAdministrationState state,
            IEnumerable<string> armyIds, string targetCastleId)
        {
            if (database == null || state == null || armyIds == null)
            {
                return false;
            }
            var ids = armyIds.Distinct().ToList();
            var armies = ids.Select(id => state.Armies.Find(item => item.ArmyId == id)).ToList();
            if (armies.Count == 0 || armies.Any(army => army == null || army.IsOperational == false || army.Members.Count == 0))
            {
                return false;
            }
            WIArmyState first = armies[0];
            WICastleRuntimeState origin = state.GetCastle(first.CurrentCastleId);
            WICastleRuntimeState target = state.GetCastle(targetCastleId);
            WIFactionRuntimeState faction = state.GetFactionState(first.FactionId);
            if (origin == null || target == null || faction == null || origin.FactionId != first.FactionId ||
                origin.AdjacentCastleIds.Contains(targetCastleId) == false ||
                armies.Any(army => army.FactionId != first.FactionId || army.CurrentCastleId != origin.CastleId))
            {
                return false;
            }
            bool attack = target.FactionId != first.FactionId;
            if (attack == true && (AreFactionsAtWar(state, first.FactionId, target.FactionId) == false ||
                faction.Influence < 20 * armies.Count))
            {
                return false;
            }
            foreach (WIArmyState army in armies)
            {
                BeginArmyMarch(database, state, army, targetCastleId);
                army.StrategicTargetCastleId = targetCastleId;
                army.Mission = attack == true ? WIArmyMission.Attack : WIArmyMission.Reinforce;
            }
            return true;
        }
    }
}

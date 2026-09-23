using System.Collections.Generic;
using System.Linq;

namespace ProjectWI.Administration
{
    public partial class WIAdministrationUIController
    {
        // 선택한 출발 성에 있는 아군의 출정 가능한 전투단을 기존 카드용으로 반환합니다.
        public bool TryGetUGUIGroupMarchPanel(string originArmyId, string targetId,
            out WIAdministrationMilitarySnapshot snapshot, out string error)
        {
            snapshot = new WIAdministrationMilitarySnapshot();
            error = database.GetText("UI_GROUP_MARCH_INVALID");
            var originArmy = state.Armies.Find(item => item.ArmyId == originArmyId);
            var target = state.GetCastle(targetId);
            if (originArmy == null || target == null || originArmy.FactionId != state.PlayerFactionId)
            {
                return false;
            }
            snapshot.Title = database.GetText("UI_GROUP_MARCH_TITLE");
            snapshot.Summary = string.Format(database.GetText("UI_GROUP_MARCH_ROUTE"),
                database.GetCastle(originArmy.CurrentCastleId).DisplayName.Get(database.UseEnglish),
                database.GetCastle(targetId).DisplayName.Get(database.UseEnglish));
            snapshot.MarchInfluencePerArmy = target.FactionId == originArmy.FactionId ? 0 : 20;
            foreach (var army in state.Armies.Where(item => item.FactionId == state.PlayerFactionId &&
                         item.CurrentCastleId == originArmy.CurrentCastleId && item.IsOperational == true && item.Members.Count > 0))
            {
                snapshot.Items.Add(new WIAdministrationMilitaryItemSnapshot
                {
                    Id = army.ArmyId, Kind = "select-army", Title = army.DisplayName,
                    Description = string.Format(database.GetText("UI_GROUP_MARCH_ARMY"), army.Members.Count,
                        WIAdministrationTurnSystem.GetRecommendedArmySize(database, army),
                        WIAdministrationTurnSystem.GetArmyBattlePower(database, state, army))
                });
            }
            snapshot.AvailableMemberSlots = snapshot.Items.Count;
            error = string.Empty;
            return true;
        }

        // 플레이어 소유권과 일괄 출정 조건을 검증하고 성공한 경우에만 화면을 갱신합니다.
        public bool MarchUGUIArmies(IEnumerable<string> armyIds, string targetId, out string error)
        {
            error = database.GetText("UI_GROUP_MARCH_INVALID");
            var ids = armyIds?.Distinct().ToList();
            if (ids == null || ids.Any(id => state.Armies.Find(item => item.ArmyId == id)?.FactionId != state.PlayerFactionId) ||
                WIAdministrationTurnSystem.BeginArmyGroupMarch(database, state, ids, targetId) == false)
            {
                return false;
            }
            error = string.Empty;
            RefreshAll();
            return true;
        }
    }
}

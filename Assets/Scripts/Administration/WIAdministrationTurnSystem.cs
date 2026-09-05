using System.Linq;

namespace ProjectWI.Administration
{
    public static partial class WIAdministrationTurnSystem
    {
        public const int ImproveRelationsGoldCost = 100;
        public const int ImproveRelationsInfluenceCost = 15;
        public const int NonAggressionInfluenceCost = 25;
        public const int AllianceInfluenceCost = 40;
        public const int DeclareWarInfluenceCost = 10;
        public const int AllianceAidGold = 200;

        // 지정한 성이 현재 플레이어가 직접 명령할 수 있는 소유 영지인지 확인합니다.
        public static bool CanPlayerManageCastle(WIAdministrationState state, WICastleRuntimeState castle)
        {
            return state != null && castle != null && castle.FactionId == state.PlayerFactionId;
        }

        // 플레이어가 참가해야 하는 전투가 남아 있어 다음 턴을 진행할 수 없는지 확인합니다.
        public static bool HasUnresolvedPlayerBattles(WIAdministrationState state)
        {
            return state?.BattleSessions != null && state.BattleSessions.Any(session =>
                session.PlayerInvolved && session.Status != WIBattleSessionStatus.Resolved);
        }

    }
}

using System.Linq;

namespace ProjectWI.Administration
{
    public static class WIInformationVisibility
    {
        // 소유 성, 동맹 성 또는 유효한 조사 정보가 있는 성의 상세 정보를 공개합니다.
        public static bool CanViewCastleDetails(
            WIAdministrationState state,
            string observerFactionId,
            WICastleRuntimeState castle)
        {
            if (state == null || castle == null || string.IsNullOrEmpty(observerFactionId)) return false;
            if (castle.FactionId == observerFactionId) return true;
            WIDiplomaticRelationState relation = state.DiplomaticRelations.FirstOrDefault(item =>
                (item.FirstFactionId == observerFactionId && item.SecondFactionId == castle.FactionId) ||
                (item.SecondFactionId == observerFactionId && item.FirstFactionId == castle.FactionId));
            if (relation?.Status == WIDiplomaticStatus.Alliance) return true;
            return HasActiveInvestigation(state, observerFactionId, castle.CastleId);
        }

        // 지정 세력이 해당 성에 남은 조사 정보를 가지고 있는지 반환합니다.
        public static bool HasActiveInvestigation(
            WIAdministrationState state,
            string observerFactionId,
            string castleId)
        {
            return state?.SchemeIntel != null && state.SchemeIntel.Any(item =>
                item.ObserverFactionId == observerFactionId && item.TargetCastleId == castleId && item.RemainingMonths > 0);
        }

        // 상세 조사 외에도 플레이어가 참가한 미결 전투는 해당 전장의 군사 정보만 공개합니다.
        public static bool CanViewMilitaryDetails(
            WIAdministrationState state,
            string observerFactionId,
            WICastleRuntimeState castle)
        {
            if (CanViewCastleDetails(state, observerFactionId, castle)) return true;
            return state?.BattleSessions != null && state.BattleSessions.Any(session =>
                session.CastleId == castle.CastleId && session.PlayerInvolved &&
                session.Status != WIBattleSessionStatus.Resolved);
        }
    }
}

using UnityEngine;

namespace ProjectWI.Administration
{
    public static class WIOccupationEventSystem
    {
        // 플레이어가 새로 점령한 성의 통치 방침 선택 사건을 중복 없이 등록합니다.
        public static void CreatePending(WIAdministrationState state, string castleId, string defeatedFactionId,
            WITurnSummary summary, WIAdministrationDatabaseSO database)
        {
            if (state.PendingOccupationEvents.Exists(item => item.CastleId == castleId)) return;
            state.PendingOccupationEvents.Add(new WIPendingOccupationEvent
            {
                CastleId = castleId,
                DefeatedFactionId = defeatedFactionId
            });
            string castleName = database.GetCastle(castleId)?.DisplayName.Get(database.UseEnglish) ?? castleId;
            summary?.News.Add($"점령 통치 선택 대기 · {castleName}");
        }

        // 점령 방침의 자원 비용을 감당할 수 있는지 확인합니다.
        public static bool CanChoose(WIAdministrationState state, WIOccupationChoiceDefinition choice)
        {
            return state != null && choice != null && state.Gold + choice.GoldDelta >= 0 &&
                   state.Influence + choice.InfluenceDelta >= 0;
        }

        // 선택한 점령 방침을 자원·성 수치·불안 기간에 적용합니다.
        public static bool Resolve(WIAdministrationDatabaseSO database, WIAdministrationState state,
            WIPendingOccupationEvent pending, int choiceIndex, WITurnSummary summary)
        {
            if (pending == null || choiceIndex < 0 || choiceIndex >= database.OccupationChoices.Count ||
                state.PendingOccupationEvents.Contains(pending) == false) return false;
            WICastleRuntimeState castle = state.GetCastle(pending.CastleId);
            WIOccupationChoiceDefinition choice = database.OccupationChoices[choiceIndex];
            if (castle == null || castle.FactionId != state.PlayerFactionId || CanChoose(state, choice) == false) return false;

            state.Gold += choice.GoldDelta;
            state.Influence += choice.InfluenceDelta;
            castle.Stability = Mathf.Clamp(castle.Stability + choice.StabilityDelta, 0, 100);
            castle.Defense = Mathf.Clamp(castle.Defense + choice.DefenseDelta, 0, 100);
            castle.Prosperity = Mathf.Clamp(castle.Prosperity + choice.ProsperityDelta, 0, 100);
            castle.OccupationUnrestMonths = choice.UnrestMonths;
            state.OccupationPolicyHistory.Add(choice.Id);
            state.PendingOccupationEvents.Remove(pending);
            string castleName = database.GetCastle(castle.CastleId)?.DisplayName.Get(database.UseEnglish) ?? castle.CastleId;
            summary?.News.Add($"점령 통치 결정 · {castleName} · {choice.ResultDescription.Get(database.UseEnglish)}");
            return true;
        }
    }
}

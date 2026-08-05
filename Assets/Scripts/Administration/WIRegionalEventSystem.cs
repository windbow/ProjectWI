using UnityEngine;

namespace ProjectWI.Administration
{
    public static class WIRegionalEventSystem
    {
        // 선택 결과의 자원 비용을 현재 세력이 감당할 수 있는지 확인합니다.
        public static bool CanChoose(WIAdministrationState state, WIRegionalEventChoiceDefinition choice)
        {
            return state != null && choice != null &&
                   state.Gold + choice.GoldDelta >= 0 &&
                   state.ManaCrystal + choice.ManaDelta >= 0 &&
                   state.Influence + choice.InfluenceDelta >= 0;
        }

        // 소유권과 최소 턴 조건을 만족한 미발생 지역 사건을 한 건 예약합니다.
        public static WIRegionalEventDefinition CreateCandidate(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WITurnSummary summary)
        {
            if (database == null || state == null || state.PendingRegionalEvents.Count > 0) return null;
            foreach (WIRegionalEventDefinition definition in database.RegionalEventDefinitions)
            {
                if (state.Turn < definition.MinimumTurn ||
                    state.CompletedRegionalEventIds.Contains(definition.Id)) continue;
                WICastleRuntimeState castle = state.GetCastle(definition.TargetCastleId);
                if (castle == null || castle.FactionId != state.PlayerFactionId) continue;
                state.PendingRegionalEvents.Add(new WIPendingRegionalEvent { EventId = definition.Id });
                summary?.News.Add($"지역 사건 발생 · {definition.Title.Get(database.UseEnglish)}");
                return definition;
            }
            return null;
        }

        // 선택 결과를 플레이어 자원과 대상 성 수치에 적용하고 사건을 완료합니다.
        public static bool Resolve(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WIPendingRegionalEvent pending,
            int choiceIndex,
            WITurnSummary summary)
        {
            WIRegionalEventDefinition definition = pending == null ? null : database.GetRegionalEvent(pending.EventId);
            if (definition == null || choiceIndex < 0 || choiceIndex >= definition.Choices.Count ||
                state.PendingRegionalEvents.Contains(pending) == false) return false;
            WIRegionalEventChoiceDefinition choice = definition.Choices[choiceIndex];
            WICastleRuntimeState castle = state.GetCastle(definition.TargetCastleId);
            if (castle == null || castle.FactionId != state.PlayerFactionId || CanChoose(state, choice) == false) return false;

            state.Gold = Mathf.Max(0, state.Gold + choice.GoldDelta);
            state.ManaCrystal = Mathf.Max(0, state.ManaCrystal + choice.ManaDelta);
            state.Influence = Mathf.Max(0, state.Influence + choice.InfluenceDelta);
            castle.Prosperity = Mathf.Clamp(castle.Prosperity + choice.ProsperityDelta, 0, 100);
            castle.Technology = Mathf.Clamp(castle.Technology + choice.TechnologyDelta, 0, 100);
            castle.Stability = Mathf.Clamp(castle.Stability + choice.StabilityDelta, 0, 100);
            castle.Defense = Mathf.Clamp(castle.Defense + choice.DefenseDelta, 0, 100);
            state.PendingRegionalEvents.Remove(pending);
            if (state.CompletedRegionalEventIds.Contains(definition.Id) == false)
                state.CompletedRegionalEventIds.Add(definition.Id);
            summary?.News.Add($"지역 사건 해결 · {definition.Title.Get(database.UseEnglish)} · {choice.ResultDescription.Get(database.UseEnglish)}");
            return true;
        }
    }
}

using System.Linq;

namespace ProjectWI.Administration
{
    public static class WICampaignObjectiveSystem
    {
        // 아직 완료되지 않은 가장 앞의 캠페인 목표를 반환합니다.
        public static WICampaignObjectiveDefinition GetCurrent(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state)
        {
            if (database == null || state == null) return null;
            return database.CampaignObjectives.FirstOrDefault(
                item => state.CompletedCampaignObjectiveIds.Contains(item.Id) == false);
        }

        // 목표 유형에 맞는 현재 진행 수치를 반환합니다.
        public static int GetProgress(WIAdministrationState state, WICampaignObjectiveDefinition objective)
        {
            if (state == null || objective == null) return 0;
            switch (objective.ObjectiveType)
            {
                case WICampaignObjectiveType.CastleProsperity:
                    return state.GetCastle(objective.TargetCastleId)?.Prosperity ?? 0;
                case WICampaignObjectiveType.PlayerCastleCount:
                    return state.Castles.Count(item => item.FactionId == state.PlayerFactionId);
                case WICampaignObjectiveType.FactionEliminated:
                    return state.GetFactionState(objective.TargetFactionId)?.Eliminated == true ? 1 : 0;
                case WICampaignObjectiveType.ContinentalUnification:
                    return state.Castles.Count > 0 && state.Castles.All(item => item.FactionId == state.PlayerFactionId) ? 1 : 0;
                default:
                    return 0;
            }
        }

        // 달성된 목표를 완료 처리하고 보상과 월간 보고 소식을 적용합니다.
        public static bool Evaluate(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WITurnSummary summary)
        {
            WICampaignObjectiveDefinition objective = GetCurrent(database, state);
            if (objective == null || GetProgress(state, objective) < objective.TargetValue) return false;

            state.CompletedCampaignObjectiveIds.Add(objective.Id);
            state.Gold += objective.RewardGold;
            state.ManaCrystal += objective.RewardMana;
            state.Influence += objective.RewardInfluence;
            summary?.News.Add($"캠페인 목표 완료 · {objective.Title.Get(database.UseEnglish)} · 보상 금화 {objective.RewardGold}, 마나 {objective.RewardMana}, 영향력 {objective.RewardInfluence}");
            return true;
        }
    }
}

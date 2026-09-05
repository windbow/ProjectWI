using System.Linq;

namespace ProjectWI.Administration
{
    public static class WICampaignResultSystem
    {
        // 아직 종료되지 않은 캠페인의 대륙 통일 또는 진영 소멸 조건을 한 번만 판정합니다.
        public static bool Evaluate(WIAdministrationDatabaseSO database, WIAdministrationState state)
        {
            if (database == null || state == null || state.CampaignResult != WICampaignResult.Ongoing)
            {
                return false;
            }

            int playerCastles = state.Castles.Count(castle => castle.FactionId == state.PlayerFactionId);
            WICampaignRuleDefinition rules = database.CampaignRules;
            bool aresMainVictory = state.CampaignVariant == WICampaignVariant.AresMain &&
                                   state.Castles.Any(castle => castle.FactionId == "valdor") == false;
            bool freeScenarioVictory = state.CampaignVariant != WICampaignVariant.AresMain &&
                                       rules.VictoryRequiresAllCastles && state.Castles.Count > 0 &&
                                       playerCastles == state.Castles.Count;
            if (aresMainVictory || freeScenarioVictory)
            {
                state.CampaignEnding = DetermineEnding(state);
                SetResult(state, WICampaignResult.Victory);
                return true;
            }
            if (rules.DefeatWhenNoCastles && playerCastles == 0)
            {
                SetResult(state, WICampaignResult.Defeat);
                return true;
            }
            return false;
        }

        // 점령 통치 선택 누적에서 화합 통일 또는 군정 통일 결말을 판정합니다.
        public static WICampaignEndingType DetermineEnding(WIAdministrationState state)
        {
            int martialLaw = state.OccupationPolicyHistory.Count(item => item == "martial_law");
            int concord = state.OccupationPolicyHistory.Count(item =>
                item == "conciliation" || item == "local_autonomy");
            return martialLaw > concord ? WICampaignEndingType.Dominion : WICampaignEndingType.Concord;
        }

        // 캠페인 결과와 최초 판정 턴을 저장합니다.
        private static void SetResult(WIAdministrationState state, WICampaignResult result)
        {
            state.CampaignResult = result;
            state.CampaignResultTurn = state.Turn;
            state.CampaignResultAcknowledged = false;
        }
    }
}

using System.Linq;

namespace ProjectWI.Administration
{
    public static class WICampaignResultSystem
    {
        // 아직 종료되지 않은 캠페인의 대륙 통일 또는 세력 소멸 조건을 한 번만 판정합니다.
        public static bool Evaluate(WIAdministrationDatabaseSO database, WIAdministrationState state)
        {
            if (database == null || state == null || state.CampaignResult != WICampaignResult.Ongoing)
            {
                return false;
            }

            int playerCastles = state.Castles.Count(castle => castle.FactionId == state.PlayerFactionId);
            WICampaignRuleDefinition rules = database.CampaignRules;
            if (rules.VictoryRequiresAllCastles && state.Castles.Count > 0 && playerCastles == state.Castles.Count)
            {
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

        // 캠페인 결과와 최초 판정 턴을 저장합니다.
        private static void SetResult(WIAdministrationState state, WICampaignResult result)
        {
            state.CampaignResult = result;
            state.CampaignResultTurn = state.Turn;
            state.CampaignResultAcknowledged = false;
        }
    }
}

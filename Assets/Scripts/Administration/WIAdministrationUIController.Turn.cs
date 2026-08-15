using System.Collections;
using ProjectWI.Systems;

namespace ProjectWI.Administration
{
    public partial class WIAdministrationUIController
    {
        // 미결 전투를 확인한 뒤 UGUI 턴 처리 흐름을 시작합니다.
        private void BeginTurn()
        {
            if (WIAdministrationTurnSystem.HasUnresolvedPlayerBattles(state))
            {
                UGUIMonthlyReportRequested?.Invoke();
                return;
            }
            StartCoroutine(ExecuteUGUITurnRoutine());
        }

        // UGUI 연산 안내 뒤 기존 턴 계산을 실행하고 월간 보고 UGUI를 엽니다.
        private IEnumerator ExecuteUGUITurnRoutine()
        {
            ShowUGUITurnProcessing();
            yield return null;
            WIAdministrationTurnSystem.ExecuteTurn(database, state);
            WICampaignRuntimeService.Instance?.AutoSave();
            RefreshAll();
            ShowUGUITurnReport();
        }
    }
}

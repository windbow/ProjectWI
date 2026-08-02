using System.Linq;

namespace ProjectWI.Administration
{
    public static class WITutorialSystem
    {
        // 현재 월에 아직 확인하지 않은 첫해 안내를 반환합니다.
        public static WITutorialDefinition GetPending(WIAdministrationDatabaseSO database, WIAdministrationState state)
        {
            if (database == null || state == null || state.TutorialSkipped || state.Year != database.StartingYear)
            {
                return null;
            }

            return database.TutorialDefinitions.FirstOrDefault(item =>
                item.Month == state.Month && state.CompletedTutorialIds.Contains(item.Id) == false);
        }

        // 지정 안내를 확인 완료 상태로 기록합니다.
        public static void Complete(WIAdministrationState state, string tutorialId)
        {
            if (state == null || string.IsNullOrEmpty(tutorialId) || state.CompletedTutorialIds.Contains(tutorialId))
            {
                return;
            }
            state.CompletedTutorialIds.Add(tutorialId);
        }

        // 남은 첫해 안내를 모두 건너뛰도록 기록합니다.
        public static void SkipAll(WIAdministrationState state)
        {
            if (state != null)
            {
                state.TutorialSkipped = true;
            }
        }
    }
}

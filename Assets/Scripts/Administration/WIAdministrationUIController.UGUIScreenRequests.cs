using System;

namespace ProjectWI.Administration
{
    public partial class WIAdministrationUIController
    {
        // 요청할 화면을 최초 한 번 생성한 뒤 새 구독자를 포함한 표시 이벤트를 읽어 전달합니다.
        private void RaiseUGUIScreenRequest<TController>(Func<Action> getRequest)
            where TController : WIAdministrationUGUIPanelController
        {
            if (WIUIScreenManager.Active != null &&
                WIUIScreenManager.Active.EnsureScreen<TController>() == false)
            {
                return;
            }

            getRequest()?.Invoke();
        }

        // 요청할 화면을 최초 한 번 생성한 뒤 새 구독자를 포함한 두 매개변수 표시 이벤트를 읽어 전달합니다.
        private void RaiseUGUIScreenRequest<TController, TFirst, TSecond>(
            Func<Action<TFirst, TSecond>> getRequest,
            TFirst first,
            TSecond second)
            where TController : WIAdministrationUGUIPanelController
        {
            if (WIUIScreenManager.Active != null &&
                WIUIScreenManager.Active.EnsureScreen<TController>() == false)
            {
                return;
            }

            getRequest()?.Invoke(first, second);
        }
    }
}

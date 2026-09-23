using UnityEngine;

namespace ProjectWI.Administration
{
    public abstract class WIAdministrationUGUIPanelController : MonoBehaviour
    {
        [SerializeField] protected WIAdministrationUIController administrationController;

        // 중앙 화면 관리자가 이 화면이 사용할 행정 UI 컨트롤러를 명시적으로 연결합니다.
        public void BindAdministrationController(WIAdministrationUIController controller)
        {
            administrationController = controller;
        }

        /// <summary>
        /// 프리팹에 연결된 내정 화면 관리자를 반환하고, 참조가 없으면 중앙 화면 관리자의 명시적 참조를 사용합니다.
        /// </summary>
        protected WIAdministrationUIController ResolveAdministrationController()
        {
            if (administrationController == null)
            {
                administrationController = WIUIScreenManager.Active?.AdministrationController;
            }

            return administrationController;
        }
    }
}

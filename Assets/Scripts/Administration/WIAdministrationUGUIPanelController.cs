using UnityEngine;

namespace ProjectWI.Administration
{
    public abstract class WIAdministrationUGUIPanelController : MonoBehaviour
    {
        [SerializeField] protected WIAdministrationUIController administrationController;

        /// <summary>
        /// 프리팹에 연결된 내정 화면 관리자를 반환하고, 참조가 없을 때만 현재 씬에서 탐색합니다.
        /// </summary>
        protected WIAdministrationUIController ResolveAdministrationController()
        {
            if (administrationController == null)
            {
                administrationController = FindFirstObjectByType<WIAdministrationUIController>();
            }

            return administrationController;
        }
    }
}

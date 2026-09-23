using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectWI.Administration
{
    public sealed class WIUIScreenManager : MonoBehaviour
    {
        private const int ModalSortingOrderBase = 200;
        private static WIUIScreenManager active;
        [SerializeField] private WIAdministrationUIController administrationController;
        [SerializeField] private GameObject[] screenPrefabs;
        private readonly Dictionary<GameObject, GameObject> screenInstances = new();
        private readonly Dictionary<Type, GameObject> prefabsByControllerType = new();
        private readonly Dictionary<Type, WIAdministrationUGUIPanelController> screensByControllerType = new();
        private readonly List<WIAdministrationModalUGUIController> modalStack = new();

        public static WIUIScreenManager Active => active;
        public WIAdministrationUIController AdministrationController => administrationController;
        public bool HasVisibleModal => modalStack.Count > 0;

        // 이 씬의 화면 인스턴스와 모달 표시 순서를 관리할 활성 관리자로 등록합니다.
        private void Awake()
        {
            if (active != null && active != this)
            {
                Debug.LogError("활성 UGUI 화면 관리자가 씬에 둘 이상 존재합니다.", this);
                enabled = false;
                return;
            }

            active = this;
            if (administrationController == null)
            {
                Debug.LogError("UGUI 화면 관리자에 행정 UI 컨트롤러가 연결되지 않았습니다.", this);
                enabled = false;
                return;
            }
            RegisterScreenPrefabs();
        }

        // 씬이 종료될 때 정적 활성 참조와 화면 캐시를 정리합니다.
        private void OnDestroy()
        {
            if (active == this)
            {
                active = null;
            }

            modalStack.Clear();
            screenInstances.Clear();
            prefabsByControllerType.Clear();
            screensByControllerType.Clear();
        }

        // 등록된 완성 UGUI 프리팹을 타입별로 색인하고 상시 화면만 미리 생성합니다.
        private void RegisterScreenPrefabs()
        {
            if (screenPrefabs == null)
            {
                Debug.LogError("UGUI 화면 관리자에 화면 프리팹 배열이 연결되지 않았습니다.", this);
                return;
            }

            HashSet<GameObject> uniquePrefabs = new();
            foreach (GameObject prefab in screenPrefabs)
            {
                if (prefab == null)
                {
                    Debug.LogError("UGUI 화면 관리자에 비어 있는 프리팹 등록이 있습니다.", this);
                    continue;
                }
                if (uniquePrefabs.Add(prefab) == false)
                {
                    Debug.LogError($"UGUI 화면 프리팹이 중복 등록되었습니다: {prefab.name}", this);
                    continue;
                }

                WIAdministrationUGUIPanelController controller =
                    prefab.GetComponentInChildren<WIAdministrationUGUIPanelController>(true);
                if (controller == null)
                {
                    InstantiateScreen(prefab);
                    continue;
                }

                Type controllerType = controller.GetType();
                if (prefabsByControllerType.ContainsKey(controllerType))
                {
                    Debug.LogError($"같은 UGUI 화면 컨트롤러 타입의 프리팹이 중복 등록되었습니다: {controllerType.Name}", this);
                    continue;
                }

                prefabsByControllerType.Add(controllerType, prefab);
                if (ShouldPreload(controllerType))
                {
                    InstantiateScreen(prefab);
                }
            }
        }

        // 타이틀·월드·영지처럼 화면 전환의 바탕이 되는 상시 화면인지 반환합니다.
        private static bool ShouldPreload(Type controllerType)
        {
            return controllerType == typeof(WICampaignTitleUGUIController) ||
                   controllerType == typeof(WIAdministrationWorldUGUIController) ||
                   controllerType == typeof(WIAdministrationTerritoryUGUIController);
        }

        // 완성 화면 프리팹을 한 번 생성해 프리팹·컨트롤러 타입 캐시에 등록합니다.
        private GameObject InstantiateScreen(GameObject prefab)
        {
            if (prefab == null)
            {
                return null;
            }
            if (screenInstances.TryGetValue(prefab, out GameObject existing) && existing != null)
            {
                return existing;
            }

            GameObject screen = Instantiate(prefab, transform);
            screenInstances[prefab] = screen;
            RegisterScreenController(prefab, screen);
#if UNITY_EDITOR
            // 플레이마다 생성되는 전체 화면 Canvas 루트만 Scene 피킹에서 제외하고 자식 UI 선택은 유지합니다.
            UnityEditor.SceneVisibilityManager.instance.DisablePicking(screen, false);
#endif
            return screen;
        }

        // 특정 등록 프리팹에서 생성된 화면 인스턴스를 반환합니다.
        public bool TryGetScreenInstance(GameObject prefab, out GameObject instance)
        {
            return screenInstances.TryGetValue(prefab, out instance) && instance != null;
        }

        // 컨트롤러 타입으로 생성된 화면을 조회합니다.
        public bool TryGetScreen<TController>(out TController controller)
            where TController : WIAdministrationUGUIPanelController
        {
            if (screensByControllerType.TryGetValue(typeof(TController), out WIAdministrationUGUIPanelController found) &&
                found != null)
            {
                controller = (TController)found;
                return true;
            }

            controller = null;
            return false;
        }

        // 요청된 화면이 아직 없다면 등록 프리팹에서 생성하고 타입 캐시에 보관합니다.
        public bool EnsureScreen<TController>()
            where TController : WIAdministrationUGUIPanelController
        {
            if (TryGetScreen(out TController _))
            {
                return true;
            }
            if (prefabsByControllerType.TryGetValue(typeof(TController), out GameObject prefab) == false)
            {
                Debug.LogError($"요청한 UGUI 화면 프리팹이 등록되지 않았습니다: {typeof(TController).Name}", this);
                return false;
            }

            InstantiateScreen(prefab);
            return TryGetScreen(out TController _);
        }

        // 프리팹의 대표 화면 컨트롤러를 타입 레지스트리에 등록하고 공용 의존성을 연결합니다.
        private void RegisterScreenController(GameObject prefab, GameObject instance)
        {
            WIAdministrationUGUIPanelController controller =
                instance.GetComponentInChildren<WIAdministrationUGUIPanelController>(true);
            if (controller == null)
            {
                return;
            }

            Type controllerType = controller.GetType();
            if (screensByControllerType.ContainsKey(controllerType))
            {
                Debug.LogError($"같은 UGUI 화면 컨트롤러 타입이 중복 등록되었습니다: {controllerType.Name} ({prefab.name})", this);
                return;
            }

            if (administrationController == null)
            {
                Debug.LogError("UGUI 화면 관리자가 사용할 행정 UI 컨트롤러를 찾지 못했습니다.", this);
                return;
            }

            controller.BindAdministrationController(administrationController);
            screensByControllerType.Add(controllerType, controller);
        }

        // 열린 모달을 스택 최상단으로 이동하고 실제 표시 순서를 부여합니다.
        public void NotifyModalShown(WIAdministrationModalUGUIController modal)
        {
            if (modal == null)
            {
                return;
            }

            modalStack.Remove(modal);
            modalStack.Add(modal);
            RefreshModalSortingOrders();
        }

        // 닫히거나 비활성화된 모달을 표시 스택에서 제거합니다.
        public void NotifyModalHidden(WIAdministrationModalUGUIController modal)
        {
            if (modalStack.Remove(modal))
            {
                RefreshModalSortingOrders();
            }
        }

        // 현재 가장 나중에 열린 모달을 닫습니다.
        public bool TryHideTopModal()
        {
            RemoveInvalidModalEntries();
            if (modalStack.Count == 0)
            {
                return false;
            }

            modalStack[modalStack.Count - 1].Hide();
            return true;
        }

        // 파괴되거나 이미 숨겨진 모달을 표시 스택에서 제거합니다.
        private void RemoveInvalidModalEntries()
        {
            for (int index = modalStack.Count - 1; index >= 0; index -= 1)
            {
                WIAdministrationModalUGUIController modal = modalStack[index];
                if (modal == null || modal.IsVisible == false)
                {
                    modalStack.RemoveAt(index);
                }
            }
        }

        // 프리팹의 고정 정렬값과 무관하게 열린 순서대로 모달 Canvas를 정렬합니다.
        private void RefreshModalSortingOrders()
        {
            RemoveInvalidModalEntries();
            for (int index = 0; index < modalStack.Count; index += 1)
            {
                modalStack[index].SetNavigationSortingOrder(ModalSortingOrderBase + index);
            }
        }
    }
}

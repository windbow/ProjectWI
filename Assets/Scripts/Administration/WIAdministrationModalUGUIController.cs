using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectWI.Administration
{
    public sealed class WIAdministrationModalUGUIController : MonoBehaviour
    {
        private static readonly HashSet<WIAdministrationModalUGUIController> Instances = new();
        [SerializeField] private GameObject modalRoot;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private Button closeButton;
        private Canvas rootCanvas;
        private GraphicRaycaster rootRaycaster;

        public event Action Closed;

        public bool IsVisible => modalRoot != null && modalRoot.activeInHierarchy;

        public static bool AnyVisible
        {
            // 활성 UGUI 모달이 하나라도 표시되는지 반환합니다.
            get
            {
                if (WIUIScreenManager.Active != null)
                {
                    return WIUIScreenManager.Active.HasVisibleModal;
                }

                foreach (WIAdministrationModalUGUIController instance in Instances)
                {
                    if (instance != null && instance.modalRoot != null && instance.modalRoot.activeInHierarchy)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        // 공통 닫기 버튼을 연결하고 초기에는 모달을 숨깁니다.
        private void Awake()
        {
            rootCanvas = GetComponent<Canvas>();
            rootRaycaster = GetComponent<GraphicRaycaster>();
            closeButton.onClick.AddListener(Hide);
            modalRoot.SetActive(false);
            SetCanvasVisible(false);
        }

        // 활성 모달 목록에 이 프리팹의 공통 모달을 등록합니다.
        private void OnEnable()
        {
            Instances.Add(this);
        }

        // 비활성화된 프리팹의 공통 모달을 활성 목록에서 제거합니다.
        private void OnDisable()
        {
            Instances.Remove(this);
            WIUIScreenManager.Active?.NotifyModalHidden(this);
        }

        // 공통 모달 프레임과 제목을 표시합니다.
        public void Show(string title)
        {
            titleLabel.text = title;
            SetCanvasVisible(true);
            modalRoot.SetActive(true);
            WIUIScreenManager.Active?.NotifyModalShown(this);
        }

        // 열린 공통 모달의 제목만 현재 단계에 맞게 변경합니다.
        public void SetTitle(string title)
        {
            titleLabel.text = title;
        }

        // 공통 모달 프레임을 숨기고 화면별 컨트롤러에 종료를 알립니다.
        public void Hide()
        {
            if (modalRoot.activeSelf == false)
            {
                return;
            }

            modalRoot.SetActive(false);
            SetCanvasVisible(false);
            WIUIScreenManager.Active?.NotifyModalHidden(this);
            Closed?.Invoke();
        }

        // 중앙 화면 관리자가 부여한 모달 표시 순서를 Canvas에 적용합니다.
        public void SetNavigationSortingOrder(int sortingOrder)
        {
            if (rootCanvas == null)
            {
                rootCanvas = GetComponent<Canvas>();
            }
            if (rootCanvas == null)
            {
                return;
            }

            rootCanvas.overrideSorting = true;
            rootCanvas.sortingOrder = sortingOrder;
        }

        // 숨긴 모달의 전체 화면 Canvas가 Scene 선택과 런타임 입력을 가로막지 않도록 표시 상태를 동기화합니다.
        private void SetCanvasVisible(bool visible)
        {
            if (rootCanvas != null)
            {
                rootCanvas.enabled = visible;
            }
            if (rootRaycaster != null)
            {
                rootRaycaster.enabled = visible;
            }
        }

        // 가장 높은 Canvas 순서로 표시 중인 모달을 닫습니다.
        public static bool TryHideTopmost()
        {
            if (WIUIScreenManager.Active != null)
            {
                return WIUIScreenManager.Active.TryHideTopModal();
            }

            WIAdministrationModalUGUIController topmost = null;
            int highestOrder = int.MinValue;
            foreach (WIAdministrationModalUGUIController instance in Instances)
            {
                if (instance == null || instance.modalRoot == null || instance.modalRoot.activeInHierarchy == false)
                {
                    continue;
                }
                Canvas canvas = instance.GetComponent<Canvas>();
                int order = canvas == null ? 0 : canvas.sortingOrder;
                if (order < highestOrder)
                {
                    continue;
                }
                highestOrder = order;
                topmost = instance;
            }
            if (topmost == null)
            {
                return false;
            }
            topmost.Hide();
            return true;
        }
    }
}

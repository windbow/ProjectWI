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

        public event Action Closed;

        public static bool AnyVisible
        {
            // 활성 UGUI 모달이 하나라도 표시되는지 반환합니다.
            get
            {
                foreach (WIAdministrationModalUGUIController instance in Instances)
                {
                    if (instance != null && instance.modalRoot != null && instance.modalRoot.activeInHierarchy) return true;
                }
                return false;
            }
        }

        // 공통 닫기 버튼을 연결하고 초기에는 모달을 숨깁니다.
        private void Awake()
        {
            closeButton.onClick.AddListener(Hide);
            modalRoot.SetActive(false);
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
        }

        // 공통 모달 프레임과 제목을 표시합니다.
        public void Show(string title)
        {
            titleLabel.text = title;
            modalRoot.SetActive(true);
        }

        // 열린 공통 모달의 제목만 현재 단계에 맞게 변경합니다.
        public void SetTitle(string title)
        {
            titleLabel.text = title;
        }

        // 공통 모달 프레임을 숨기고 화면별 컨트롤러에 종료를 알립니다.
        public void Hide()
        {
            modalRoot.SetActive(false);
            Closed?.Invoke();
        }

        // 가장 높은 Canvas 순서로 표시 중인 모달을 닫습니다.
        public static bool TryHideTopmost()
        {
            WIAdministrationModalUGUIController topmost = null;
            int highestOrder = int.MinValue;
            foreach (WIAdministrationModalUGUIController instance in Instances)
            {
                if (instance == null || instance.modalRoot == null || instance.modalRoot.activeInHierarchy == false) continue;
                Canvas canvas = instance.GetComponent<Canvas>();
                int order = canvas == null ? 0 : canvas.sortingOrder;
                if (order < highestOrder) continue;
                highestOrder = order;
                topmost = instance;
            }
            if (topmost == null) return false;
            topmost.Hide();
            return true;
        }
    }
}

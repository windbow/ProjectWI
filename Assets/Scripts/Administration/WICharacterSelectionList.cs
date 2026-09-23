using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectWI.Administration
{
    public sealed class WICharacterSelectionList : MonoBehaviour
    {
        // 런타임에는 이 완성 프리팹만 복제하며 UI 컴포넌트나 구조를 새로 조립하지 않습니다.
        [SerializeField] private WICharacterSelectionRow rowPrefab;
        [SerializeField] private RectTransform content;
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private TMP_InputField search;
        [SerializeField] private Button availableButton;
        [SerializeField] private Button sortButton;
        [SerializeField] private TMP_Text countLabel;
        [SerializeField] private WICharacterSelectionRow[] initialRows;
        private readonly List<WICharacterSelectionRow> rows = new();
        private int count;
        private bool availableOnly;
        private bool sortByName;
        private bool pending;
        private Action<int> select;
        private WIAdministrationUIController controller;

        // 저장된 툴바에만 입력 이벤트를 연결합니다.
        private void Awake()
        {
            if (rows.Count == 0)
            {
                rows.AddRange(initialRows);
            }
            search.onValueChanged.AddListener(_ => ApplyView(true));
            availableButton.onClick.AddListener(() => { availableOnly = !availableOnly; ApplyView(true); });
            sortButton.onClick.AddListener(() => { sortByName = !sortByName; ApplyView(true); });
        }

        // 화면을 새로 열면 이전 검색 조건과 스크롤을 초기화합니다.
        private void OnEnable()
        {
            search.SetTextWithoutNotify(string.Empty);
            availableOnly = false;
            sortByName = false;
            pending = true;
            scroll.verticalNormalizedPosition = 1f;
        }

        // 후보 수에 맞게 기존 행을 재사용하고 부족한 행만 완성 프리팹에서 복제합니다.
        public void Prepare(int candidateCount, Action<int> callback, WIAdministrationUIController owner,
            out Button[] buttons, out TMP_Text[] labels, out Image[] portraits)
        {
            controller = owner;
            select = callback;
            count = candidateCount;
            if (rows.Count == 0)
            {
                rows.AddRange(initialRows);
            }
            if (rowPrefab == null || content == null)
            {
                Debug.LogError("인물 목록 프리팹 참조가 누락되었습니다.", this);
                buttons = Array.Empty<Button>();
                labels = Array.Empty<TMP_Text>();
                portraits = Array.Empty<Image>();
                return;
            }
            while (rows.Count < Mathf.Max(8, count))
            {
                rows.Add(Instantiate(rowPrefab, content));
            }
            int capacity = Mathf.Max(8, count);
            buttons = new Button[capacity];
            labels = new TMP_Text[capacity];
            portraits = new Image[capacity];
            for (int index = 0; index < rows.Count; index++)
            {
                var row = rows[index];
                row.gameObject.SetActive(index < count);
                if (index >= capacity)
                {
                    continue;
                }
                int captured = index;
                row.Button.onClick.RemoveAllListeners();
                row.Button.onClick.AddListener(() => select?.Invoke(captured));
                buttons[index] = row.Button;
                labels[index] = row.Label;
                portraits[index] = row.Portrait;
            }
            pending = true;
        }

        // 컨트롤러의 데이터 바인딩 뒤 한 번만 텍스트와 검색 결과를 갱신합니다.
        private void LateUpdate()
        {
            if (pending == true)
            {
                pending = false;
                for (int index = 0; index < count; index++)
                {
                    rows[index].Format();
                }
                ApplyView(false);
            }
        }

        // 필터와 정렬은 원본 후보 인덱스를 바꾸지 않아 다중 선택을 보존합니다.
        public void ApplyView(bool resetScroll)
        {
            string query = search.text.Trim();
            IEnumerable<int> indices = Enumerable.Range(0, count);
            if (sortByName == true)
            {
                indices = indices.OrderBy(index => rows[index].SortName, StringComparer.CurrentCulture);
            }
            int visible = 0;
            foreach (int index in indices)
            {
                var row = rows[index];
                bool matches = (availableOnly == false || row.Button.interactable == true) &&
                    row.SearchText.IndexOf(query, StringComparison.CurrentCultureIgnoreCase) >= 0;
                row.gameObject.SetActive(matches);
                row.transform.SetAsLastSibling();
                if (matches == true)
                {
                    visible++;
                }
            }
            if (controller != null)
            {
                search.placeholder.GetComponent<TMP_Text>().text = controller.GetAdministrationText("UI_SELECTION_SEARCH");
                availableButton.GetComponentInChildren<TMP_Text>().text = controller.GetAdministrationText(
                    availableOnly ? "UI_SELECTION_AVAILABLE" : "UI_SELECTION_ALL");
                sortButton.GetComponentInChildren<TMP_Text>().text = controller.GetAdministrationText(
                    sortByName ? "UI_SELECTION_NAME" : "UI_SELECTION_DEFAULT");
                countLabel.text = string.Format(controller.GetAdministrationText("UI_SELECTION_COUNT"), visible, count);
            }
            if (resetScroll == true)
            {
                scroll.verticalNormalizedPosition = 1f;
            }
        }

        // 단계 전환 시 검색과 스크롤을 초기화합니다.
        public void ResetView()
        {
            search.SetTextWithoutNotify(string.Empty);
            availableOnly = false;
            sortByName = false;
            scroll.verticalNormalizedPosition = 1f;
            pending = true;
        }
    }
}

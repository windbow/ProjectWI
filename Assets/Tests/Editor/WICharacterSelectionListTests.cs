using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ProjectWI.Administration;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectWI.Tests.Editor
{
    public sealed class WICharacterSelectionListTests
    {
        // 검사 중 생성한 완성 목록 프리팹 인스턴스입니다.
        private GameObject instance;
        private WICharacterSelectionList list;
        // 편집 모드에서 TMP의 플레이 전용 활성화 경로를 실행하지 않는 검사 부모입니다.
        private GameObject host;

        // 실제 화면에 저장된 목록 프리팹으로 바인딩을 검증합니다.
        [SetUp]
        public void SetUp()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Administration/WIAdministrationResearchUGUI.prefab");
            host = new GameObject("SelectionListTest");
            host.SetActive(false);
            instance = Object.Instantiate(prefab.GetComponentInChildren<WICharacterSelectionList>(true).gameObject, host.transform);
            list = instance.GetComponent<WICharacterSelectionList>();
            typeof(WICharacterSelectionList).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(list, null);
        }

        // 검사 인스턴스를 해제합니다.
        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(host);
        }

        // 기존 8명 한계를 넘은 행도 검색·이름 정렬 후 원본 인덱스로 선택됩니다.
        [Test]
        public void MoreThanEightCandidates_SearchAndSortKeepOriginalSelection()
        {
            int chosen = -1;
            list.Prepare(23, index => chosen = index, null, out var buttons, out var labels, out _);
            for (int index = 0; index < 23; index++)
            {
                labels[index].text = $"인물 {22 - index:00}\n정보 {index}";
            }
            typeof(WICharacterSelectionList).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(list, null);
            var input = instance.GetComponentInChildren<TMP_InputField>(true);
            input.SetTextWithoutNotify("인물 00");
            list.ApplyView(true);
            Assert.IsTrue(buttons[22].gameObject.activeSelf);
            Assert.IsFalse(buttons[0].gameObject.activeSelf);
            var serialized = new SerializedObject(list);
            ((Button)serialized.FindProperty("sortButton").objectReferenceValue).onClick.Invoke();
            buttons[22].onClick.Invoke();
            Assert.AreEqual(22, chosen);
            input.SetTextWithoutNotify(string.Empty);
            list.ApplyView(true);
            Assert.AreEqual(23, buttons.Count(button => button.gameObject.activeSelf));
        }

        // 줄어든 목록에서 오래된 행과 클릭 콜백이 남지 않고 풀을 재사용합니다.
        [Test]
        public void ShrinkingAndRebinding_ReusesRowsWithoutStaleCallbacks()
        {
            int oldCalls = 0;
            int newIndex = -1;
            list.Prepare(19, _ => oldCalls++, null, out var first, out var labels, out _);
            for (int index = 0; index < 19; index++)
            {
                labels[index].text = $"인물 {index}\n대기";
            }
            typeof(WICharacterSelectionList).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(list, null);
            list.Prepare(2, index => newIndex = index, null, out var next, out var nextLabels, out _);
            nextLabels[0].text = "새 인물 0\n대기";
            nextLabels[1].text = "새 인물 1\n대기";
            typeof(WICharacterSelectionList).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(list, null);
            Assert.AreSame(first[0], next[0]);
            Assert.IsFalse(first[18].gameObject.activeSelf);
            next[1].onClick.Invoke();
            Assert.AreEqual(0, oldCalls);
            Assert.AreEqual(1, newIndex);
        }

        // 선택 가능 필터는 비활성 후보만 숨기며 원래 상호작용 조건은 바꾸지 않습니다.
        [Test]
        public void AvailableFilter_PreservesDisabledReasonsAndSelection()
        {
            list.Prepare(12, _ => { }, null, out var buttons, out var labels, out _);
            for (int index = 0; index < 12; index++)
            {
                labels[index].text = $"인물 {index}\n" + (index == 11 ? "다른 전투단 소속" : "편성 가능");
            }
            buttons[11].interactable = false;
            typeof(WICharacterSelectionList).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(list, null);
            var filter = (Button)new SerializedObject(list).FindProperty("availableButton").objectReferenceValue;
            filter.onClick.Invoke();
            Assert.IsFalse(buttons[11].gameObject.activeSelf);
            filter.onClick.Invoke();
            Assert.IsTrue(buttons[11].gameObject.activeSelf);
            Assert.IsFalse(buttons[11].interactable);
            Assert.That(labels[11].text, Does.Contain("다른 전투단"));
        }

        // 모든 배정 화면은 공통 행 프리팹·스크롤·입력 참조를 편집 시점에 보유합니다.
        [TestCase("HeroAssignment")]
        [TestCase("FocusProject")]
        [TestCase("Delegation")]
        [TestCase("CharacterActivity")]
        [TestCase("Research")]
        [TestCase("Scheme")]
        [TestCase("Military")]
        [TestCase("March")]
        public void SelectionScreen_HasSerializedListAndReusableRows(string screen)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Administration/WIAdministration" + screen + "UGUI.prefab");
            var component = prefab.GetComponentInChildren<WICharacterSelectionList>(true);
            Assert.IsNotNull(component);
            var serialized = new SerializedObject(component);
            Assert.IsNotNull(serialized.FindProperty("rowPrefab").objectReferenceValue);
            var scroll = (ScrollRect)serialized.FindProperty("scroll").objectReferenceValue;
            Assert.IsTrue(scroll.vertical);
            Assert.IsFalse(scroll.horizontal);
            Assert.IsNotNull(scroll.viewport.GetComponent<RectMask2D>());
            Assert.IsNotNull(scroll.content.GetComponent<VerticalLayoutGroup>());
            Assert.AreEqual(Vector2.one, ((RectTransform)component.transform).anchorMax);
            Assert.AreEqual(8, serialized.FindProperty("initialRows").arraySize);
        }
    }
}

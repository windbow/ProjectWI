using System;
using System.Linq;
using ProjectWI.Administration;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectWI.EditorTools
{
    public static class WICharacterSelectionListSetup
    {
        // 공통 행과 적용 화면은 에디터에서만 생성하여 에셋으로 저장합니다.
        private const string Root = "Assets/Prefabs/Administration/";
        private const string RowPath = Root + "WICharacterSelectionRow.prefab";
        private static TMP_FontAsset font;
        private static readonly (string Name, string Buttons, string Labels, string Portraits)[] Targets =
        {
            ("HeroAssignment", "candidateButtons", "candidateLabels", "candidatePortraits"),
            ("FocusProject", "managerButtons", "managerLabels", "managerPortraits"),
            ("Delegation", "governorButtons", "governorLabels", "governorPortraits"),
            ("CharacterActivity", "cardButtons", "cardLabels", "cardPortraits"),
            ("Research", "cardButtons", "cardLabels", "cardPortraits"),
            ("Scheme", "cardButtons", "cardLabels", "cardPortraits"),
            ("Military", "itemButtons", "itemLabels", "itemPortraits"),
            ("March", "cardButtons", "cardLabels", "cardImages")
        };

        // 8종 선택 화면에 공통 행 프리팹과 스크롤·검색 도구를 저장합니다.
        [MenuItem("WI/UI/Build Character Selection Lists")]
        public static void Build()
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "WIAdministrationDelegationUGUI.prefab");
            font = source.GetComponentInChildren<TMP_Text>(true).font;
            BuildRow();
            foreach (var target in Targets)
            {
                BuildScreen(target.Name, target.Buttons, target.Labels, target.Portraits);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("인물 선택 화면 8종의 공통 스크롤 목록 저장 완료");
        }

        // 넓은 이름·요약 칸과 작은 초상을 갖는 재사용 행을 만듭니다.
        private static void BuildRow()
        {
            var root = Rect("WICharacterSelectionRow", null);
            root.sizeDelta = new Vector2(1400, 88);
            var image = root.gameObject.AddComponent<Image>();
            image.color = new Color32(24, 36, 51, 255);
            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.highlightedColor = new Color(.7f, .85f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(.5f, .5f, .5f, .7f);
            button.colors = colors;
            var layout = root.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 88;
            layout.minHeight = 88;
            var portrait = Rect("Portrait", root);
            Place(portrait, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(12, -32), new Vector2(76, 32));
            var portraitImage = portrait.gameObject.AddComponent<Image>();
            portraitImage.preserveAspect = true;
            portraitImage.raycastTarget = false;
            var title = Text("Name", root, 24);
            Place(title.rectTransform, new Vector2(0, 0), new Vector2(.31f, 1), new Vector2(90, 8), new Vector2(-12, -8));
            title.fontStyle = FontStyles.Bold;
            var detail = Text("Detail", root, 22);
            Place(detail.rectTransform, new Vector2(.31f, 0), Vector2.one, new Vector2(8, 8), new Vector2(-18, -8));
            detail.color = new Color32(183, 202, 217, 255);
            var component = root.gameObject.AddComponent<WICharacterSelectionRow>();
            var serialized = new SerializedObject(component);
            Set(serialized, "button", button);
            Set(serialized, "portrait", portraitImage);
            Set(serialized, "title", title);
            Set(serialized, "detail", detail);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root.gameObject, RowPath);
            UnityEngine.Object.DestroyImmediate(root.gameObject);
        }

        // 기존 카드가 점유하던 영역만 목록으로 교체하여 고정 하단 명령을 유지합니다.
        private static void BuildScreen(string name, string buttonField, string labelField, string portraitField)
        {
            string path = Root + "WIAdministration" + name + "UGUI.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var type = typeof(WIAdministrationUIController).Assembly.GetType("ProjectWI.Administration.WIAdministration" + name + "UGUIController");
                var serialized = new SerializedObject(root.GetComponent(type));
                var existing = serialized.FindProperty("selectionList").objectReferenceValue as WICharacterSelectionList;
                RectTransform parent;
                var oldButtons = serialized.FindProperty(buttonField);
                if (existing != null)
                {
                    var rect = (RectTransform)existing.transform;
                    parent = (RectTransform)rect.parent;
                    UnityEngine.Object.DestroyImmediate(existing.gameObject);
                }
                else
                {
                    var first = (Button)oldButtons.GetArrayElementAtIndex(0).objectReferenceValue;
                    parent = (RectTransform)first.transform.parent;
                    for (int index = 0; index < oldButtons.arraySize; index++)
                    {
                        var button = (Button)oldButtons.GetArrayElementAtIndex(index).objectReferenceValue;
                        UnityEngine.Object.DestroyImmediate(button.gameObject);
                    }
                }
                var listRoot = Rect("CharacterSelectionList", parent);
                // 카드 부모 자체가 화면별 목록 영역이므로 비활성 프리팹의 월드 경계에 의존하지 않습니다.
                Place(listRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                var list = listRoot.gameObject.AddComponent<WICharacterSelectionList>();
                var toolbar = Rect("Toolbar", listRoot);
                Place(toolbar, new Vector2(0, 1), Vector2.one, new Vector2(0, -52), Vector2.zero);
                var search = CreateSearch(toolbar);
                var available = CreateButton("AvailableFilter", toolbar, "전체", .46f, .65f);
                var sort = CreateButton("Sort", toolbar, "기본순", .66f, .83f);
                var count = Text("Count", toolbar, 20);
                Place(count.rectTransform, new Vector2(.84f, 0), Vector2.one, Vector2.zero, Vector2.zero);

                var scrollRoot = Rect("Scroll", listRoot);
                Place(scrollRoot, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, -60));
                if (name == "CharacterActivity")
                {
                    // 인재 활동은 기존 검색·등급 필터·정렬 도구막대를 유지합니다.
                    toolbar.gameObject.SetActive(false);
                    Place(scrollRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                }
                var scroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
                scroll.horizontal = false;
                scroll.movementType = ScrollRect.MovementType.Clamped;
                scroll.scrollSensitivity = 40;
                var viewport = Rect("Viewport", scrollRoot);
                Place(viewport, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-18, 0));
                viewport.gameObject.AddComponent<RectMask2D>();
                var background = viewport.gameObject.AddComponent<Image>();
                background.color = new Color32(12, 21, 32, 230);
                var content = Rect("Content", viewport);
                content.anchorMin = new Vector2(0, 1);
                content.anchorMax = Vector2.one;
                content.pivot = new Vector2(.5f, 1);
                content.sizeDelta = Vector2.zero;
                var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
                layout.spacing = 6;
                layout.childControlHeight = true;
                layout.childControlWidth = true;
                layout.childForceExpandHeight = false;
                layout.childForceExpandWidth = true;
                var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                scroll.viewport = viewport;
                scroll.content = content;
                var barRect = Rect("Scrollbar", scrollRoot);
                Place(barRect, new Vector2(1, 0), Vector2.one, new Vector2(-12, 0), Vector2.zero);
                barRect.gameObject.AddComponent<Image>().color = new Color32(15, 25, 36, 255);
                var handle = Rect("Handle", barRect);
                Place(handle, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                var handleImage = handle.gameObject.AddComponent<Image>();
                handleImage.color = new Color32(89, 131, 158, 255);
                var bar = barRect.gameObject.AddComponent<Scrollbar>();
                bar.handleRect = handle;
                bar.targetGraphic = handleImage;
                bar.direction = Scrollbar.Direction.BottomToTop;
                scroll.verticalScrollbar = bar;
                scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

                var prefab = AssetDatabase.LoadAssetAtPath<WICharacterSelectionRow>(RowPath);
                var rowRefs = new WICharacterSelectionRow[8];
                for (int index = 0; index < rowRefs.Length; index++)
                {
                    var rowObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject, content);
                    rowRefs[index] = rowObject.GetComponent<WICharacterSelectionRow>();
                }
                var listData = new SerializedObject(list);
                Set(listData, "rowPrefab", prefab);
                Set(listData, "content", content);
                Set(listData, "scroll", scroll);
                Set(listData, "search", search);
                Set(listData, "availableButton", available);
                Set(listData, "sortButton", sort);
                Set(listData, "countLabel", count);
                SetArray(listData, "initialRows", rowRefs);
                listData.ApplyModifiedPropertiesWithoutUndo();
                serialized.Update();
                Set(serialized, "selectionList", list);
                SetArray(serialized, buttonField, rowRefs.Select(row => row.Button).ToArray());
                SetArray(serialized, labelField, rowRefs.Select(row => row.Label).ToArray());
                SetArray(serialized, portraitField, rowRefs.Select(row => row.Portrait).ToArray());
                serialized.ApplyModifiedPropertiesWithoutUndo();
                if (name == "Military")
                {
                    // 고정 하단 확정·이전 버튼의 흰 바탕을 목록과 같은 명암으로 맞춥니다.
                    var footer = (Button)serialized.FindProperty("createButton").objectReferenceValue;
                    footer.image.color = new Color32(35, 54, 73, 255);
                    var label = footer.GetComponentInChildren<TMP_Text>();
                    label.color = new Color32(224, 233, 242, 255);
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // 기존 글꼴을 사용한 검색 입력을 편집 시점에 구성합니다.
        private static TMP_InputField CreateSearch(RectTransform parent)
        {
            var rect = Rect("Search", parent);
            Place(rect, Vector2.zero, new Vector2(.45f, 1), Vector2.zero, Vector2.zero);
            rect.gameObject.AddComponent<Image>().color = new Color32(29, 43, 60, 255);
            var input = rect.gameObject.AddComponent<TMP_InputField>();
            var viewport = Rect("TextArea", rect);
            Place(viewport, Vector2.zero, Vector2.one, new Vector2(12, 4), new Vector2(-12, -4));
            viewport.gameObject.AddComponent<RectMask2D>();
            var text = Text("Text", viewport, 22);
            var placeholder = Text("Placeholder", viewport, 22);
            Place(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Place(placeholder.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            placeholder.text = "이름·정보 검색";
            placeholder.color = new Color32(141, 165, 184, 255);
            input.textViewport = viewport;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.fontAsset = font;
            return input;
        }

        // 툴바 버튼과 글꼴을 저장합니다.
        private static Button CreateButton(string name, RectTransform parent, string label, float left, float right)
        {
            var rect = Rect(name, parent);
            Place(rect, new Vector2(left, 0), new Vector2(right, 1), Vector2.zero, Vector2.zero);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color32(35, 54, 73, 255);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var text = Text("Label", rect, 20);
            text.text = label;
            text.alignment = TextAlignmentOptions.Center;
            Place(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(4, 2), new Vector2(-4, -2));
            return button;
        }

        // 공통 글꼴·색상·줄바꿈을 가진 텍스트를 생성합니다.
        private static TMP_Text Text(string name, Transform parent, float size)
        {
            var rect = Rect(name, parent);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = size;
            text.fontSizeMin = 18;
            text.fontSizeMax = size;
            text.enableAutoSizing = true;
            text.color = new Color32(224, 233, 242, 255);
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.raycastTarget = false;
            return text;
        }

        // 에디터 전용 UI 오브젝트를 부모 아래 생성합니다.
        private static RectTransform Rect(string name, Transform parent)
        {
            var result = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            result.SetParent(parent, false);
            result.gameObject.layer = 5;
            return result;
        }

        // 앵커와 여백을 명시해 프리팹에 레이아웃을 저장합니다.
        private static void Place(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = offsetMin; rect.offsetMax = offsetMax;
        }

        // 직렬화된 단일 참조를 설정합니다.
        private static void Set(SerializedObject target, string name, UnityEngine.Object value)
        {
            target.FindProperty(name).objectReferenceValue = value;
        }

        // 직렬화 배열에 편집 시점의 초기 행 참조를 저장합니다.
        private static void SetArray<T>(SerializedObject target, string name, T[] values) where T : UnityEngine.Object
        {
            var array = target.FindProperty(name);
            array.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++)
            {
                array.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            }
        }
    }
}

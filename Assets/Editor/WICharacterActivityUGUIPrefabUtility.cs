using System.Collections.Generic;
using ProjectWI.Administration;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectWI.EditorTools
{
    public static class WICharacterActivityUGUIPrefabUtility
    {
        private const string PrefabPath = "Assets/Prefabs/Administration/WIAdministrationCharacterActivityUGUI.prefab";
        private const string AssetRoot = "Assets/Resources/UI/Generated/CharacterActivity/";
        private const string NormalButtonPath = AssetRoot + "character_activity_button_normal_v1.png";
        private const string PrimaryButtonPath = AssetRoot + "character_activity_button_selected_v1.png";
        private const string SearchFieldPath = AssetRoot + "character_activity_search_field_v1.png";
        private const string FilterNormalPath = AssetRoot + "character_activity_filter_normal_v1.png";
        private const string FilterSelectedPath = AssetRoot + "character_activity_filter_selected_v1.png";

        // 인재 활동 프리팹을 4열 2행 후보 목록과 고정 검색 도구막대 구조로 정리합니다.
        [MenuItem("WI/UI/Rebuild Character Activity Grid")]
        public static void RebuildCharacterActivityGrid()
        {
            ConfigureSpriteImport(NormalButtonPath, new Vector4(36f, 28f, 36f, 28f));
            ConfigureSpriteImport(PrimaryButtonPath, new Vector4(36f, 28f, 36f, 28f));
            ConfigureSpriteImport(SearchFieldPath, new Vector4(36f, 28f, 36f, 28f));
            ConfigureSpriteImport(FilterNormalPath, new Vector4(32f, 28f, 32f, 28f));
            ConfigureSpriteImport(FilterSelectedPath, new Vector4(32f, 28f, 32f, 28f));
            WICharacterSelectionCardPrefabUtility.ConfigureSharedSpriteImports();
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                WIAdministrationCharacterActivityUGUIController controller =
                    root.GetComponent<WIAdministrationCharacterActivityUGUIController>();
                RectTransform panel = Find(root.transform, "ModalPanel") as RectTransform;
                RectTransform header = Find(root.transform, "Header") as RectTransform;
                RectTransform context = Find(root.transform, "ContextLabel") as RectTransform;
                RectTransform cards = Find(root.transform, "CandidateCards") as RectTransform;
                RectTransform previous = Find(root.transform, "PreviousButton") as RectTransform;
                RectTransform next = Find(root.transform, "NextButton") as RectTransform;
                RectTransform page = Find(root.transform, "PageLabel") as RectTransform;
                TMP_Text styleText = Find(root.transform, "ContextLabel").GetComponent<TMP_Text>();

                ConfigureRect(panel, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1760f, 940f));
                ConfigureRect(header, new Vector2(0.5f, 1f), new Vector2(0f, -42f), new Vector2(1760f, 84f));
                ConfigureRect(context, new Vector2(0.5f, 1f), new Vector2(0f, -112f), new Vector2(1540f, 42f));
                ConfigureRect(cards, new Vector2(0.5f, 0.5f), new Vector2(0f, -35f), new Vector2(1540f, 590f));
                ConfigureRect(previous, new Vector2(0f, 0f), new Vector2(160f, 42f), new Vector2(250f, 58f));
                ConfigureRect(next, new Vector2(1f, 0f), new Vector2(-160f, 42f), new Vector2(250f, 58f));
                ConfigureRect(page, new Vector2(0.5f, 0f), new Vector2(0f, 42f), new Vector2(180f, 48f));

                Button[] candidateButtons = cards.GetComponentsInChildren<Button>(true);
                System.Array.Sort(candidateButtons, (left, right) => string.CompareOrdinal(left.name, right.name));
                for (int index = 0; index < candidateButtons.Length; index += 1)
                {
                    ConfigureCandidateCard(candidateButtons[index], index);
                }

                Transform existingToolbar = Find(root.transform, "CandidateToolbar");
                if (existingToolbar != null)
                {
                    Object.DestroyImmediate(existingToolbar.gameObject);
                }
                GameObject toolbar = CreateUIObject("CandidateToolbar", panel);
                ConfigureRect(toolbar.GetComponent<RectTransform>(), new Vector2(0.5f, 1f),
                    new Vector2(0f, -174f), new Vector2(1540f, 62f));
                TMP_InputField searchInput = CreateSearchField(toolbar.transform, styleText);
                Button[] filters = new Button[3];
                TMP_Text[] filterLabels = new TMP_Text[3];
                string[] filterNames = { "전체", "영웅", "일반" };
                for (int index = 0; index < filters.Length; index += 1)
                {
                    filters[index] = CreateToolbarButton(toolbar.transform, "Filter-" + index,
                        filterNames[index], styleText, new Vector2(-110f + index * 154f, 0f), new Vector2(142f, 52f));
                    filterLabels[index] = filters[index].GetComponentInChildren<TMP_Text>();
                }
                Button sortButton = CreateToolbarButton(toolbar.transform, "SortButton", "추천순",
                    styleText, new Vector2(650f, 0f), new Vector2(240f, 52f));

                SerializedObject serialized = new SerializedObject(controller);
                serialized.FindProperty("toolbarRoot").objectReferenceValue = toolbar;
                serialized.FindProperty("searchInput").objectReferenceValue = searchInput;
                SetArray(serialized.FindProperty("filterButtons"), filters);
                SetArray(serialized.FindProperty("filterLabels"), filterLabels);
                serialized.FindProperty("sortButton").objectReferenceValue = sortButton;
                serialized.FindProperty("sortLabel").objectReferenceValue = sortButton.GetComponentInChildren<TMP_Text>();
                serialized.FindProperty("filterNormalSprite").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<Sprite>(FilterNormalPath);
                serialized.FindProperty("filterSelectedSprite").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<Sprite>(FilterSelectedPath);
                serialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log("인재 활동 UGUI를 4열 2행 후보 목록과 소형 공용 버튼 에셋 구조로 갱신했습니다.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // 후보 카드를 동일한 4열 2행 규격과 좌측 초상화 구조로 배치합니다.
        private static void ConfigureCandidateCard(Button button, int index)
        {
            RectTransform card = button.GetComponent<RectTransform>();
            int column = index % 4;
            int row = index / 4;
            ConfigureRect(card, new Vector2(0.5f, 0.5f),
                new Vector2(-577.5f + column * 385f, 147.5f - row * 295f), new Vector2(360f, 270f));
            WICharacterSelectionCardPrefabUtility.ApplyCardVisual(button);
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color32(185, 215, 235, 255);
            colors.pressedColor = new Color32(135, 185, 220, 255);
            colors.disabledColor = new Color32(90, 94, 98, 180);
            colors.colorMultiplier = 1f;
            button.colors = colors;

            RectTransform portrait = Find(button.transform, "Portrait") as RectTransform;
            RectTransform label = Find(button.transform, "CandidateLabel") as RectTransform;
            ConfigureRect(portrait, new Vector2(0f, 0.5f), new Vector2(90f, 0f), new Vector2(170f, 246f));
            ConfigureRect(label, new Vector2(1f, 0.5f), new Vector2(-91f, 0f), new Vector2(170f, 224f));
            Image portraitImage = portrait.GetComponent<Image>();
            portraitImage.preserveAspect = true;
            TMP_Text labelText = label.GetComponent<TMP_Text>();
            labelText.color = new Color32(225, 229, 232, 255);
            labelText.fontSize = 21f;
            labelText.alignment = TextAlignmentOptions.MidlineLeft;
            labelText.textWrappingMode = TextWrappingModes.Normal;
            labelText.margin = new Vector4(8f, 8f, 8f, 8f);
        }

        // 이름 검색용 고정 TMP 입력 필드를 생성합니다.
        private static TMP_InputField CreateSearchField(Transform parent, TMP_Text styleText)
        {
            GameObject fieldObject = CreateUIObject("SearchField", parent);
            ConfigureRect(fieldObject.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f),
                new Vector2(-570f, 0f), new Vector2(400f, 52f));
            Image background = fieldObject.AddComponent<Image>();
            background.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SearchFieldPath);
            background.type = Image.Type.Sliced;
            background.color = Color.white;
            TMP_InputField input = fieldObject.AddComponent<TMP_InputField>();

            GameObject textArea = CreateUIObject("TextArea", fieldObject.transform);
            Stretch(textArea.GetComponent<RectTransform>(), new Vector4(20f, 6f, 20f, 6f));
            textArea.AddComponent<RectMask2D>();
            TMP_Text text = CreateText("Text", textArea.transform, styleText, "", 20f, TextAlignmentOptions.MidlineLeft);
            Stretch(text.rectTransform, Vector4.zero);
            TMP_Text placeholder = CreateText("Placeholder", textArea.transform, styleText,
                "이름 검색", 20f, TextAlignmentOptions.MidlineLeft);
            placeholder.color = new Color32(130, 140, 148, 255);
            Stretch(placeholder.rectTransform, Vector4.zero);
            input.textViewport = textArea.GetComponent<RectTransform>();
            input.textComponent = text;
            input.placeholder = placeholder;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.characterLimit = 20;
            return input;
        }

        // 공용 소형 스프라이트를 재사용하는 도구막대 버튼을 생성합니다.
        private static Button CreateToolbarButton(Transform parent, string name, string label,
            TMP_Text styleText, Vector2 position, Vector2 size)
        {
            GameObject buttonObject = CreateUIObject(name, parent);
            ConfigureRect(buttonObject.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), position, size);
            Image image = buttonObject.AddComponent<Image>();
            bool selectedFilter = name == "Filter-0";
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(selectedFilter ? FilterSelectedPath :
                name.StartsWith("Filter-") ? FilterNormalPath : NormalButtonPath);
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            TMP_Text text = CreateText("Label", buttonObject.transform, styleText, label, 20f,
                TextAlignmentOptions.Center);
            Stretch(text.rectTransform, new Vector4(8f, 4f, 8f, 4f));
            return button;
        }

        // 기준 TMP 문자의 폰트와 재질을 복사해 고정 문구를 생성합니다.
        private static TMP_Text CreateText(string name, Transform parent, TMP_Text styleText,
            string value, float size, TextAlignmentOptions alignment)
        {
            GameObject textObject = CreateUIObject(name, parent);
            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.font = styleText.font;
            text.fontSharedMaterial = styleText.fontSharedMaterial;
            text.color = new Color32(213, 217, 220, 255);
            text.fontSize = size;
            text.alignment = alignment;
            text.text = value;
            text.raycastTarget = false;
            return text;
        }

        // 직렬화 배열에 Unity 오브젝트 참조를 순서대로 기록합니다.
        private static void SetArray<T>(SerializedProperty property, T[] values) where T : Object
        {
            property.arraySize = values.Length;
            for (int index = 0; index < values.Length; index += 1)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            }
        }

        // 생성한 소형 PNG를 투명 Sprite/Single과 지정 9-Slice 테두리로 가져옵니다.
        private static void ConfigureSpriteImport(string path, Vector4 border)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return;
            }
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Compressed;
            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteBorder = border;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        // 지정 이름의 자식 Transform을 재귀적으로 찾습니다.
        private static Transform Find(Transform root, string name)
        {
            if (root.name == name)
            {
                return root;
            }
            foreach (Transform child in root)
            {
                Transform found = Find(child, name);
                if (found != null)
                {
                    return found;
                }
            }
            return null;
        }

        // RectTransform이 포함된 고정 UGUI 오브젝트를 생성합니다.
        private static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject result = new GameObject(name, typeof(RectTransform));
            result.layer = parent.gameObject.layer;
            result.transform.SetParent(parent, false);
            return result;
        }

        // 중앙 기준 고정 앵커와 크기를 설정합니다.
        private static void ConfigureRect(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
        }

        // 부모 영역에 맞춰 늘이고 지정 여백을 적용합니다.
        private static void Stretch(RectTransform rect, Vector4 margin)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(margin.x, margin.y);
            rect.offsetMax = new Vector2(-margin.z, -margin.w);
            rect.localScale = Vector3.one;
        }
    }
}

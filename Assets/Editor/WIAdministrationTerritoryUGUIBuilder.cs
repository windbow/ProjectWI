using System.Collections.Generic;
using ProjectWI.Administration;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ProjectWI.EditorTools
{
    public static class WIAdministrationTerritoryUGUIBuilder
    {
        private const string PrefabPath = "Assets/Prefabs/Administration/WIAdministrationTerritoryUGUI.prefab";
        private static TMP_FontAsset sansFont;
        private static TMP_FontAsset serifFont;

        // 고정 배치된 영지 상세 UGUI 프리팹을 생성하고 MainScene에 배치합니다.
        [MenuItem("WI/UI/Build Administration Territory UGUI")]
        public static void Build()
        {
            sansFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/TMP/NotoSansCJKkr-Dynamic.asset");
            serifFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/TMP/NotoSerifCJKkr-Dynamic.asset");
            GameObject root = new GameObject("WIAdministrationTerritoryUGUI", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(WIAdministrationTerritoryUGUIController));
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject content = Panel(root.transform, "TerritoryContent", null, new Color32(8, 14, 18, 255),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image background = Image(content.transform, "CastleBackground", null, new Color32(255, 255, 255, 72),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, UnityEngine.UI.Image.Type.Simple);
            background.preserveAspect = false;

            GameObject top = Panel(content.transform, "TopStrip", Sprite("bg_type_c"), Color.white,
                new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -92f), Vector2.zero);
            Button back = Button(top.transform, "BackButton", "← 대륙 지도", Sprite("button_normal"),
                new Vector2(0.012f, 0.17f), new Vector2(0.15f, 0.83f), Vector2.zero, Vector2.zero, out _);
            TMP_Text title = Text(top.transform, "CastleTitle", "성 이름", 34f, TextAlignmentOptions.MidlineLeft,
                new Vector2(0.17f, 0.42f), new Vector2(0.98f, 0.96f), Vector2.zero, Vector2.zero, true);
            TMP_Text info = Text(top.transform, "CastleInfo", "성 정보", 17f, TextAlignmentOptions.MidlineLeft,
                new Vector2(0.17f, 0.05f), new Vector2(0.98f, 0.45f), Vector2.zero, Vector2.zero, false);

            GameObject left = Panel(content.transform, "OverviewPanel", Sprite("bg_type_a"), Color.white,
                new Vector2(0f, 0f), new Vector2(0.22f, 1f), new Vector2(12f, 14f), new Vector2(-6f, -104f));
            Text(left.transform, "OverviewHeading", "성 현황", 22f, TextAlignmentOptions.Center,
                new Vector2(0f, 0.91f), Vector2.one, new Vector2(18f, 0f), new Vector2(-18f, 0f), true);
            Image governor = Image(left.transform, "GovernorPortrait", null, Color.white,
                new Vector2(0f, 0.48f), new Vector2(1f, 0.90f), new Vector2(32f, 8f), new Vector2(-32f, -8f),
                UnityEngine.UI.Image.Type.Simple);
            governor.preserveAspect = true;
            TMP_Text prosperity = Stat(left.transform, "Prosperity", "번영 0", 0.36f, "castle_stat_prosperity");
            TMP_Text technology = Stat(left.transform, "Technology", "기술 0", 0.27f, "castle_stat_technology");
            TMP_Text stability = Stat(left.transform, "Stability", "질서 0", 0.18f, "castle_stat_stability");
            TMP_Text defense = Stat(left.transform, "Defense", "방어 0", 0.09f, "castle_stat_defense");

            GameObject center = Panel(content.transform, "CastleDetailPanel", Sprite("bg_type_a"), Color.white,
                new Vector2(0.22f, 0f), new Vector2(0.78f, 1f), new Vector2(5f, 14f), new Vector2(-5f, -104f));
            Text(center.transform, "ProjectHeading", "이번 달 중점", 20f, TextAlignmentOptions.MidlineLeft,
                new Vector2(0f, 0.89f), new Vector2(1f, 0.97f), new Vector2(24f, 0f), new Vector2(-24f, 0f), true);
            TMP_Text project = Text(center.transform, "ProjectStatus", "미지정", 17f, TextAlignmentOptions.TopLeft,
                new Vector2(0f, 0.78f), new Vector2(1f, 0.89f), new Vector2(24f, 4f), new Vector2(-24f, -4f), false);
            Text(center.transform, "HeroHeading", "주둔 영웅", 20f, TextAlignmentOptions.MidlineLeft,
                new Vector2(0f, 0.70f), new Vector2(0.7f, 0.78f), new Vector2(24f, 0f), Vector2.zero, true);
            List<GameObject> heroSlots = new List<GameObject>();
            List<Image> heroImages = new List<Image>();
            List<TMP_Text> heroCaptions = new List<TMP_Text>();
            for (int index = 0; index < 8; index += 1)
            {
                int column = index % 4;
                int row = index / 4;
                float xMin = 0.025f + column * 0.17f;
                float yMax = 0.69f - row * 0.29f;
                GameObject slot = Panel(center.transform, $"HeroSlot-{index}", Sprite("hero_slot_card"), Color.white,
                    new Vector2(xMin, yMax - 0.27f), new Vector2(xMin + 0.16f, yMax), Vector2.zero, Vector2.zero);
                Image portrait = Image(slot.transform, "Portrait", null, Color.white,
                    new Vector2(0f, 0.26f), Vector2.one, new Vector2(7f, 7f), new Vector2(-7f, -7f),
                    UnityEngine.UI.Image.Type.Simple);
                portrait.preserveAspect = true;
                TMP_Text caption = Text(slot.transform, "Caption", "빈 영웅 슬롯", 13f, TextAlignmentOptions.Center,
                    Vector2.zero, new Vector2(1f, 0.28f), new Vector2(4f, 2f), new Vector2(-4f, -2f), false);
                caption.color = new Color32(31, 37, 42, 255);
                heroSlots.Add(slot);
                heroImages.Add(portrait);
                heroCaptions.Add(caption);
            }
            Text(center.transform, "FacilityHeading", "특화 시설", 20f, TextAlignmentOptions.MidlineLeft,
                new Vector2(0.71f, 0.70f), new Vector2(0.98f, 0.78f), Vector2.zero, Vector2.zero, true);
            List<GameObject> facilitySlots = new List<GameObject>();
            List<Image> facilityImages = new List<Image>();
            List<TMP_Text> facilityCaptions = new List<TMP_Text>();
            for (int index = 0; index < 2; index += 1)
            {
                float yMax = 0.69f - index * 0.29f;
                GameObject slot = Panel(center.transform, $"FacilitySlot-{index}", Sprite("facility_slot_card"), Color.white,
                    new Vector2(0.72f, yMax - 0.27f), new Vector2(0.97f, yMax), Vector2.zero, Vector2.zero);
                Image icon = Image(slot.transform, "Icon", null, Color.white,
                    new Vector2(0f, 0.26f), Vector2.one, new Vector2(8f, 8f), new Vector2(-8f, -8f),
                    UnityEngine.UI.Image.Type.Simple);
                icon.preserveAspect = true;
                TMP_Text caption = Text(slot.transform, "Caption", "확장 완료 시 선택", 13f, TextAlignmentOptions.Center,
                    Vector2.zero, new Vector2(1f, 0.28f), new Vector2(4f, 2f), new Vector2(-4f, -2f), false);
                caption.color = new Color32(31, 37, 42, 255);
                facilitySlots.Add(slot);
                facilityImages.Add(icon);
                facilityCaptions.Add(caption);
            }

            GameObject right = Panel(content.transform, "CommandPanel", Sprite("bg_type_a"), Color.white,
                new Vector2(0.78f, 0f), Vector2.one, new Vector2(6f, 14f), new Vector2(-12f, -104f));
            Text(right.transform, "CommandHeading", "영지 관리 명령", 22f, TextAlignmentOptions.Center,
                new Vector2(0f, 0.89f), Vector2.one, new Vector2(18f, 0f), new Vector2(-18f, 0f), true);
            string[] commandLabels = { "중점 사업", "영웅 배치", "인재 활동", "특화 시설 선택",
                "기본 시설", "영지관 위임", "진격 / 원정", "성 상세" };
            WIAdministrationTerritoryCommand[] actions = {
                WIAdministrationTerritoryCommand.FocusProject, WIAdministrationTerritoryCommand.AssignHero,
                WIAdministrationTerritoryCommand.CharacterActivity, WIAdministrationTerritoryCommand.ChooseSpecialFacility,
                WIAdministrationTerritoryCommand.BasicFacility, WIAdministrationTerritoryCommand.Delegation,
                WIAdministrationTerritoryCommand.March, WIAdministrationTerritoryCommand.CastleRecord
            };
            List<Button> commands = new List<Button>();
            for (int index = 0; index < commandLabels.Length; index += 1)
            {
                float yMax = 0.87f - index * 0.10f;
                Sprite buttonSprite = index == 6 ? Sprite("button_danger") : Sprite("button_normal");
                commands.Add(Button(right.transform, $"Command-{index}", commandLabels[index], buttonSprite,
                    new Vector2(0f, yMax - 0.078f), new Vector2(1f, yMax),
                    new Vector2(24f, 0f), new Vector2(-24f, 0f), out _));
            }

            WIAdministrationTerritoryUGUIController controller = root.GetComponent<WIAdministrationTerritoryUGUIController>();
            SerializedObject serialized = new SerializedObject(controller);
            SetObject(serialized, "contentRoot", content);
            SetObject(serialized, "backButton", back);
            SetObject(serialized, "castleTitle", title);
            SetObject(serialized, "castleInfo", info);
            SetObject(serialized, "castleBackground", background);
            SetObject(serialized, "governorPortrait", governor);
            SetObject(serialized, "prosperityLabel", prosperity);
            SetObject(serialized, "technologyLabel", technology);
            SetObject(serialized, "stabilityLabel", stability);
            SetObject(serialized, "defenseLabel", defense);
            SetObject(serialized, "projectStatusLabel", project);
            Array(serialized.FindProperty("heroSlots"), heroSlots);
            ImageArray(serialized.FindProperty("heroImages"), heroImages);
            TextArray(serialized.FindProperty("heroCaptions"), heroCaptions);
            Array(serialized.FindProperty("facilitySlots"), facilitySlots);
            ImageArray(serialized.FindProperty("facilityImages"), facilityImages);
            TextArray(serialized.FindProperty("facilityCaptions"), facilityCaptions);
            ButtonArray(serialized.FindProperty("commandButtons"), commands);
            SerializedProperty actionProperty = serialized.FindProperty("commandActions");
            actionProperty.arraySize = actions.Length;
            for (int index = 0; index < actions.Length; index += 1)
            {
                actionProperty.GetArrayElementAtIndex(index).enumValueIndex = (int)actions[index];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            GameObject existing = GameObject.Find("WIAdministrationTerritoryUGUI");
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }
            WIAdministrationUGUISceneUtility.InstantiateUnderSceneRoot(prefab);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("행정 영지 상세 UGUI 프리팹을 생성하고 MainScene에 배치했습니다.");
        }

        // 아이콘이 포함된 성 능력치 행을 생성합니다.
        private static TMP_Text Stat(Transform parent, string name, string text, float yMin, string iconName)
        {
            GameObject row = Panel(parent, name + "Row", Sprite("button_flat_normal"), Color.white,
                new Vector2(0f, yMin), new Vector2(1f, yMin + 0.075f), new Vector2(24f, 0f), new Vector2(-24f, 0f));
            Image(row.transform, "Icon", Sprite(iconName), Color.white, new Vector2(0f, 0f), new Vector2(0.22f, 1f),
                new Vector2(8f, 4f), Vector2.zero, UnityEngine.UI.Image.Type.Simple).preserveAspect = true;
            return Text(row.transform, name, text, 16f, TextAlignmentOptions.MidlineLeft,
                new Vector2(0.22f, 0f), Vector2.one, new Vector2(6f, 0f), new Vector2(-6f, 0f), false);
        }

        // 패널 이미지와 RectTransform을 생성합니다.
        private static GameObject Panel(Transform parent, string name, Sprite sprite, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            Rect(gameObject.GetComponent<RectTransform>(), anchorMin, anchorMax, offsetMin, offsetMax);
            Image image = gameObject.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.type = sprite == null ? UnityEngine.UI.Image.Type.Simple : UnityEngine.UI.Image.Type.Sliced;
            image.raycastTarget = false;
            return gameObject;
        }

        // 스프라이트 버튼과 TMP 글자를 생성합니다.
        private static Button Button(Transform parent, string name, string text, Sprite sprite,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, out TMP_Text label)
        {
            GameObject gameObject = Panel(parent, name, sprite, Color.white, anchorMin, anchorMax, offsetMin, offsetMax);
            gameObject.GetComponent<Image>().raycastTarget = true;
            Button button = gameObject.AddComponent<Button>();
            label = Text(gameObject.transform, "Label", text, 16f, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, new Vector2(10f, 3f), new Vector2(-10f, -3f), false);
            return button;
        }

        // Dynamic TMP 폰트를 사용하는 텍스트를 생성합니다.
        private static TMP_Text Text(Transform parent, string name, string text, float size,
            TextAlignmentOptions alignment, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin,
            Vector2 offsetMax, bool serif)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            gameObject.transform.SetParent(parent, false);
            Rect(gameObject.GetComponent<RectTransform>(), anchorMin, anchorMax, offsetMin, offsetMax);
            TMP_Text label = gameObject.GetComponent<TMP_Text>();
            label.text = text;
            label.font = serif ? serifFont : sansFont;
            label.fontSize = size;
            label.color = new Color32(234, 238, 240, 255);
            label.alignment = alignment;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Overflow;
            label.raycastTarget = false;
            return label;
        }

        // 슬롯용 이미지를 생성합니다.
        private static Image Image(Transform parent, string name, Sprite sprite, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax,
            UnityEngine.UI.Image.Type type)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            Rect(gameObject.GetComponent<RectTransform>(), anchorMin, anchorMax, offsetMin, offsetMax);
            Image image = gameObject.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.type = type;
            image.raycastTarget = false;
            return image;
        }

        // RectTransform의 앵커와 여백을 설정합니다.
        private static void Rect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        // Resources의 생성 UI 스프라이트를 불러옵니다.
        private static Sprite Sprite(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Resources/UI/Generated/{name}.png");
        }

        // 단일 오브젝트 참조를 직렬화합니다.
        private static void SetObject(SerializedObject serialized, string name, Object value)
        {
            serialized.FindProperty(name).objectReferenceValue = value;
        }

        // GameObject 배열을 직렬화합니다.
        private static void Array(SerializedProperty property, IReadOnlyList<GameObject> values)
        {
            property.arraySize = values.Count;
            for (int index = 0; index < values.Count; index += 1)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            }
        }

        // Image 배열을 직렬화합니다.
        private static void ImageArray(SerializedProperty property, IReadOnlyList<Image> values)
        {
            property.arraySize = values.Count;
            for (int index = 0; index < values.Count; index += 1)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            }
        }

        // TMP 배열을 직렬화합니다.
        private static void TextArray(SerializedProperty property, IReadOnlyList<TMP_Text> values)
        {
            property.arraySize = values.Count;
            for (int index = 0; index < values.Count; index += 1)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            }
        }

        // Button 배열을 직렬화합니다.
        private static void ButtonArray(SerializedProperty property, IReadOnlyList<Button> values)
        {
            property.arraySize = values.Count;
            for (int index = 0; index < values.Count; index += 1)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            }
        }
    }
}

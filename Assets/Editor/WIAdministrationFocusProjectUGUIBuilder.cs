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
    public static class WIAdministrationFocusProjectUGUIBuilder
    {
        private const string PrefabPath = "Assets/Prefabs/Administration/WIAdministrationFocusProjectUGUI.prefab";
        private static TMP_FontAsset sansFont;
        private static TMP_FontAsset serifFont;

        // 공통 모달 프레임과 중점 사업 2단계 UGUI를 고정 프리팹으로 생성합니다.
        [MenuItem("WI/UI/Build Focus Project UGUI")]
        public static void Build()
        {
            sansFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/TMP/NotoSansCJKkr-Dynamic.asset");
            serifFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/TMP/NotoSerifCJKkr-Dynamic.asset");
            GameObject root = new GameObject("WIAdministrationFocusProjectUGUI", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(WIAdministrationModalUGUIController),
                typeof(WIAdministrationFocusProjectUGUIController));
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject modalRoot = Panel(root.transform, "ModalRoot", null, new Color32(0, 0, 0, 165),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            modalRoot.GetComponent<Image>().raycastTarget = true;
            GameObject panel = Panel(modalRoot.transform, "ModalPanel", Sprite("popup_panel"), Color.white,
                new Vector2(0.20f, 0.09f), new Vector2(0.80f, 0.91f), Vector2.zero, Vector2.zero);
            GameObject header = Panel(panel.transform, "Header", Sprite("popup_header"), Color.white,
                new Vector2(0f, 0.87f), Vector2.one, new Vector2(12f, 0f), new Vector2(-12f, -8f));
            TMP_Text title = Text(header.transform, "Title", "이번 달 중점 사업", 30f, TextAlignmentOptions.MidlineLeft,
                new Vector2(0f, 0f), new Vector2(0.78f, 1f), new Vector2(34f, 0f), Vector2.zero, true);
            title.color = new Color32(31, 37, 42, 255);
            Button close = Button(header.transform, "CloseButton", "닫기", Sprite("button_normal"),
                new Vector2(0.80f, 0.18f), new Vector2(0.98f, 0.82f), Vector2.zero, Vector2.zero, out _);
            TMP_Text message = Text(panel.transform, "Message", string.Empty, 20f, TextAlignmentOptions.Center,
                new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.85f), Vector2.zero, Vector2.zero, false);
            message.gameObject.SetActive(false);

            GameObject projectPage = Panel(panel.transform, "ProjectPage", null, Color.clear,
                new Vector2(0.04f, 0.06f), new Vector2(0.96f, 0.86f), Vector2.zero, Vector2.zero);
            List<TMP_Text> projectNames = new List<TMP_Text>();
            List<TMP_Text> basicLabels = new List<TMP_Text>();
            List<TMP_Text> intensiveLabels = new List<TMP_Text>();
            List<Button> basicButtons = new List<Button>();
            List<Button> intensiveButtons = new List<Button>();
            for (int index = 0; index < 8; index += 1)
            {
                float yMax = 0.975f - index * 0.12f;
                GameObject row = Panel(projectPage.transform, $"ProjectRow-{index}", Sprite("bg_type_c"), Color.white,
                    new Vector2(0f, yMax - 0.105f), new Vector2(1f, yMax), Vector2.zero, Vector2.zero);
                projectNames.Add(Text(row.transform, "ProjectName", "사업", 20f, TextAlignmentOptions.MidlineLeft,
                    new Vector2(0f, 0f), new Vector2(0.48f, 1f), new Vector2(24f, 0f), Vector2.zero, true));
                Button basic = Button(row.transform, "BasicButton", "기본", Sprite("button_normal"),
                    new Vector2(0.50f, 0.14f), new Vector2(0.73f, 0.86f), Vector2.zero, Vector2.zero, out TMP_Text basicLabel);
                Button intensive = Button(row.transform, "IntensiveButton", "집중", Sprite("button_primary"),
                    new Vector2(0.75f, 0.14f), new Vector2(0.98f, 0.86f), Vector2.zero, Vector2.zero, out TMP_Text intensiveLabel);
                basicButtons.Add(basic);
                intensiveButtons.Add(intensive);
                basicLabels.Add(basicLabel);
                intensiveLabels.Add(intensiveLabel);
            }

            GameObject managerPage = Panel(panel.transform, "ManagerPage", null, Color.clear,
                new Vector2(0.04f, 0.06f), new Vector2(0.96f, 0.86f), Vector2.zero, Vector2.zero);
            Button managerBack = Button(managerPage.transform, "ManagerBackButton", "← 사업 다시 선택", Sprite("button_normal"),
                new Vector2(0f, 0.90f), new Vector2(0.28f, 0.99f), Vector2.zero, Vector2.zero, out _);
            Text(managerPage.transform, "ManagerHeading", "담당 인물 선택", 24f, TextAlignmentOptions.Center,
                new Vector2(0.3f, 0.90f), new Vector2(0.75f, 0.99f), Vector2.zero, Vector2.zero, true);
            List<Button> managerButtons = new List<Button>();
            List<Image> managerPortraits = new List<Image>();
            List<TMP_Text> managerLabels = new List<TMP_Text>();
            for (int index = 0; index < 8; index += 1)
            {
                int column = index % 4;
                int row = index / 4;
                float xMin = 0.015f + column * 0.247f;
                float yMax = 0.87f - row * 0.43f;
                Button manager = Button(managerPage.transform, $"Manager-{index}", string.Empty, Sprite("hero_slot_card"),
                    new Vector2(xMin, yMax - 0.39f), new Vector2(xMin + 0.225f, yMax), Vector2.zero, Vector2.zero, out TMP_Text unused);
                Object.DestroyImmediate(unused.gameObject);
                Image portrait = Image(manager.transform, "Portrait", null, Color.white,
                    new Vector2(0f, 0.28f), Vector2.one, new Vector2(10f, 10f), new Vector2(-10f, -10f));
                portrait.preserveAspect = true;
                TMP_Text label = Text(manager.transform, "ManagerLabel", "인물\n예상 성과", 15f, TextAlignmentOptions.Center,
                    Vector2.zero, new Vector2(1f, 0.31f), new Vector2(6f, 4f), new Vector2(-6f, -4f), false);
                label.color = new Color32(30, 36, 42, 255);
                managerButtons.Add(manager);
                managerPortraits.Add(portrait);
                managerLabels.Add(label);
            }
            managerPage.SetActive(false);

            WIAdministrationModalUGUIController modal = root.GetComponent<WIAdministrationModalUGUIController>();
            SerializedObject modalSerialized = new SerializedObject(modal);
            Set(modalSerialized, "modalRoot", modalRoot);
            Set(modalSerialized, "titleLabel", title);
            Set(modalSerialized, "closeButton", close);
            modalSerialized.ApplyModifiedPropertiesWithoutUndo();

            WIAdministrationFocusProjectUGUIController focus = root.GetComponent<WIAdministrationFocusProjectUGUIController>();
            SerializedObject serialized = new SerializedObject(focus);
            Set(serialized, "modal", modal);
            Set(serialized, "projectPage", projectPage);
            Set(serialized, "managerPage", managerPage);
            Set(serialized, "messageLabel", message);
            TextArray(serialized.FindProperty("projectNames"), projectNames);
            TextArray(serialized.FindProperty("basicLabels"), basicLabels);
            TextArray(serialized.FindProperty("intensiveLabels"), intensiveLabels);
            ButtonArray(serialized.FindProperty("basicButtons"), basicButtons);
            ButtonArray(serialized.FindProperty("intensiveButtons"), intensiveButtons);
            ButtonArray(serialized.FindProperty("managerButtons"), managerButtons);
            ImageArray(serialized.FindProperty("managerPortraits"), managerPortraits);
            TextArray(serialized.FindProperty("managerLabels"), managerLabels);
            Set(serialized, "managerBackButton", managerBack);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            GameObject existing = GameObject.Find("WIAdministrationFocusProjectUGUI");
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }
            WIAdministrationUGUISceneUtility.InstantiateUnderSceneRoot(prefab);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("공통 모달 프레임과 중점 사업 UGUI를 생성하고 MainScene에 배치했습니다.");
        }

        // 패널 이미지와 앵커를 생성합니다.
        private static GameObject Panel(Transform parent, string name, Sprite sprite, Color color,
            Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            Rect(gameObject.GetComponent<RectTransform>(), min, max, offsetMin, offsetMax);
            Image image = gameObject.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.type = sprite == null ? UnityEngine.UI.Image.Type.Simple : UnityEngine.UI.Image.Type.Sliced;
            image.raycastTarget = false;
            return gameObject;
        }

        // 공통 스프라이트 버튼을 생성합니다.
        private static Button Button(Transform parent, string name, string text, Sprite sprite,
            Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax, out TMP_Text label)
        {
            GameObject gameObject = Panel(parent, name, sprite, Color.white, min, max, offsetMin, offsetMax);
            gameObject.GetComponent<Image>().raycastTarget = true;
            Button button = gameObject.AddComponent<Button>();
            label = Text(gameObject.transform, "Label", text, 17f, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, new Vector2(8f, 3f), new Vector2(-8f, -3f), false);
            return button;
        }

        // Dynamic TMP 폰트 텍스트를 생성합니다.
        private static TMP_Text Text(Transform parent, string name, string text, float size,
            TextAlignmentOptions alignment, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax, bool serif)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            gameObject.transform.SetParent(parent, false);
            Rect(gameObject.GetComponent<RectTransform>(), min, max, offsetMin, offsetMax);
            TMP_Text label = gameObject.GetComponent<TMP_Text>();
            label.text = text;
            label.font = serif ? serifFont : sansFont;
            label.fontSize = size;
            label.color = new Color32(235, 239, 241, 255);
            label.alignment = alignment;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Overflow;
            label.raycastTarget = false;
            return label;
        }

        // 인물 초상 이미지 슬롯을 생성합니다.
        private static Image Image(Transform parent, string name, Sprite sprite, Color color,
            Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            Rect(gameObject.GetComponent<RectTransform>(), min, max, offsetMin, offsetMax);
            Image image = gameObject.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        // RectTransform의 앵커와 여백을 설정합니다.
        private static void Rect(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        // 생성 UI 스프라이트를 불러옵니다.
        private static Sprite Sprite(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Resources/UI/Generated/{name}.png");
        }

        // 단일 참조를 직렬화합니다.
        private static void Set(SerializedObject serialized, string name, Object value)
        {
            serialized.FindProperty(name).objectReferenceValue = value;
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

        // Image 배열을 직렬화합니다.
        private static void ImageArray(SerializedProperty property, IReadOnlyList<Image> values)
        {
            property.arraySize = values.Count;
            for (int index = 0; index < values.Count; index += 1)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            }
        }
    }
}

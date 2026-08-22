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
    public static class WIAdministrationHeroAssignmentUGUIBuilder
    {
        private const string PrefabPath = "Assets/Prefabs/Administration/WIAdministrationHeroAssignmentUGUI.prefab";
        private static TMP_FontAsset sansFont;
        private static TMP_FontAsset serifFont;

        // 공통 모달 프레임을 사용하는 영웅 배치 UGUI를 고정 프리팹으로 생성합니다.
        [MenuItem("WI/UI/Build Hero Assignment UGUI")]
        public static void Build()
        {
            sansFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/TMP/NotoSansCJKkr-Dynamic.asset");
            serifFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/TMP/NotoSerifCJKkr-Dynamic.asset");
            GameObject root = new GameObject("WIAdministrationHeroAssignmentUGUI", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(WIAdministrationModalUGUIController),
                typeof(WIAdministrationHeroAssignmentUGUIController));
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 101;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject modalRoot = Panel(root.transform, "ModalRoot", null, new Color32(0, 0, 0, 165),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            modalRoot.GetComponent<Image>().raycastTarget = true;
            GameObject panel = Panel(modalRoot.transform, "ModalPanel", Sprite("popup_panel"), Color.white,
                new Vector2(0.18f, 0.08f), new Vector2(0.82f, 0.92f), Vector2.zero, Vector2.zero);
            GameObject header = Panel(panel.transform, "Header", Sprite("popup_header"), Color.white,
                new Vector2(0f, 0.87f), Vector2.one, new Vector2(12f, 0f), new Vector2(-12f, -8f));
            TMP_Text title = Text(header.transform, "Title", "영웅 배치", 30f, TextAlignmentOptions.MidlineLeft,
                new Vector2(0f, 0f), new Vector2(0.78f, 1f), new Vector2(34f, 0f), Vector2.zero, true);
            title.color = new Color32(31, 37, 42, 255);
            Button close = Button(header.transform, "CloseButton", "닫기", Sprite("button_normal"),
                new Vector2(0.80f, 0.18f), new Vector2(0.98f, 0.82f), Vector2.zero, Vector2.zero, out _);
            TMP_Text status = Text(panel.transform, "SlotStatus", "주둔 인물 0/0", 20f,
                TextAlignmentOptions.MidlineLeft, new Vector2(0.05f, 0.80f), new Vector2(0.65f, 0.87f),
                Vector2.zero, Vector2.zero, true);
            TMP_Text message = Text(panel.transform, "Message", string.Empty, 18f, TextAlignmentOptions.Center,
                new Vector2(0.10f, 0.42f), new Vector2(0.90f, 0.58f), Vector2.zero, Vector2.zero, false);
            message.gameObject.SetActive(false);

            GameObject cards = Panel(panel.transform, "CandidateCards", null, Color.clear,
                new Vector2(0.04f, 0.12f), new Vector2(0.96f, 0.80f), Vector2.zero, Vector2.zero);
            List<Button> candidateButtons = new List<Button>();
            List<Image> candidatePortraits = new List<Image>();
            List<TMP_Text> candidateLabels = new List<TMP_Text>();
            for (int index = 0; index < 8; index += 1)
            {
                int column = index % 4;
                int row = index / 4;
                float xMin = 0.015f + column * 0.247f;
                float yMax = 0.98f - row * 0.49f;
                Button candidate = Button(cards.transform, $"Candidate-{index}", string.Empty, Sprite("hero_slot_card"),
                    new Vector2(xMin, yMax - 0.45f), new Vector2(xMin + 0.225f, yMax), Vector2.zero, Vector2.zero,
                    out TMP_Text unused);
                Object.DestroyImmediate(unused.gameObject);
                Image portrait = Image(candidate.transform, "Portrait", null, Color.white,
                    new Vector2(0f, 0.28f), Vector2.one, new Vector2(10f, 10f), new Vector2(-10f, -10f));
                portrait.preserveAspect = true;
                TMP_Text label = Text(candidate.transform, "CandidateLabel", "영웅\n특기", 14f,
                    TextAlignmentOptions.Center, Vector2.zero, new Vector2(1f, 0.31f),
                    new Vector2(6f, 4f), new Vector2(-6f, -4f), false);
                label.color = new Color32(30, 36, 42, 255);
                candidateButtons.Add(candidate);
                candidatePortraits.Add(portrait);
                candidateLabels.Add(label);
            }

            Button previous = Button(panel.transform, "PreviousButton", "← 이전", Sprite("button_normal"),
                new Vector2(0.05f, 0.035f), new Vector2(0.22f, 0.105f), Vector2.zero, Vector2.zero, out _);
            TMP_Text page = Text(panel.transform, "PageLabel", "1 / 1", 17f, TextAlignmentOptions.Center,
                new Vector2(0.40f, 0.035f), new Vector2(0.60f, 0.105f), Vector2.zero, Vector2.zero, false);
            Button next = Button(panel.transform, "NextButton", "다음 →", Sprite("button_normal"),
                new Vector2(0.78f, 0.035f), new Vector2(0.95f, 0.105f), Vector2.zero, Vector2.zero, out _);

            WIAdministrationModalUGUIController modal = root.GetComponent<WIAdministrationModalUGUIController>();
            SerializedObject modalSerialized = new SerializedObject(modal);
            Set(modalSerialized, "modalRoot", modalRoot);
            Set(modalSerialized, "titleLabel", title);
            Set(modalSerialized, "closeButton", close);
            modalSerialized.ApplyModifiedPropertiesWithoutUndo();

            WIAdministrationHeroAssignmentUGUIController assignment =
                root.GetComponent<WIAdministrationHeroAssignmentUGUIController>();
            SerializedObject serialized = new SerializedObject(assignment);
            Set(serialized, "modal", modal);
            Set(serialized, "slotStatusLabel", status);
            Set(serialized, "messageLabel", message);
            ButtonArray(serialized.FindProperty("candidateButtons"), candidateButtons);
            ImageArray(serialized.FindProperty("candidatePortraits"), candidatePortraits);
            TextArray(serialized.FindProperty("candidateLabels"), candidateLabels);
            Set(serialized, "previousButton", previous);
            Set(serialized, "nextButton", next);
            Set(serialized, "pageLabel", page);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            WIAdministrationModalVisualUtility.Apply(root);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            GameObject existing = GameObject.Find("WIAdministrationHeroAssignmentUGUI");
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }
            WIAdministrationUGUISceneUtility.InstantiateUnderSceneRoot(prefab);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("영웅 배치 UGUI를 생성하고 MainScene에 배치했습니다.");
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

        // Dynamic TMP 텍스트를 생성합니다.
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

        // 영웅 초상 이미지 슬롯을 생성합니다.
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

        // RectTransform 앵커와 여백을 설정합니다.
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

        // TMP 배열을 직렬화합니다.
        private static void TextArray(SerializedProperty property, IReadOnlyList<TMP_Text> values)
        {
            property.arraySize = values.Count;
            for (int index = 0; index < values.Count; index += 1)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            }
        }
    }
}

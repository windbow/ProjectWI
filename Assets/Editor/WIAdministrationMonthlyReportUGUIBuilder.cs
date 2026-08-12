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
    public static class WIAdministrationMonthlyReportUGUIBuilder
    {
        private const string SourcePath = "Assets/Prefabs/Administration/WIAdministrationCastleRecordUGUI.prefab";
        private const string PrefabPath = "Assets/Prefabs/Administration/WIAdministrationMonthlyReportUGUI.prefab";

        // 스크롤 보고 본문과 고정 사건·전투 행동 슬롯을 가진 월간 보고 프리팹을 생성합니다.
        [MenuItem("WI/UI/Build Monthly Report UGUI")]
        public static void Build()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(SourcePath);
            root.name = "WIAdministrationMonthlyReportUGUI";
            root.GetComponent<Canvas>().sortingOrder = 109;
            Object.DestroyImmediate(root.GetComponent<WIAdministrationCastleRecordUGUIController>());
            Transform panel = root.transform.Find("ModalRoot/ModalPanel");
            panel.Find("CastleImage").gameObject.SetActive(false);
            RectTransform reportScroll = panel.Find("RecordScroll").GetComponent<RectTransform>();
            reportScroll.anchorMin = new Vector2(0.05f, 0.10f);
            reportScroll.anchorMax = new Vector2(0.61f, 0.82f);
            TMP_Text body = reportScroll.Find("Viewport/Body").GetComponent<TMP_Text>();
            body.fontSize = 17f;

            Button template = panel.Find("Header/CloseButton").GetComponent<Button>();
            List<Button> actions = new();
            for (int index = 0; index < 6; index += 1)
            {
                float yMax = 0.82f - index * 0.105f;
                actions.Add(CloneButton(template, panel, $"Action-{index}", "처리할 사건",
                    new Vector2(0.64f, yMax - 0.085f), new Vector2(0.95f, yMax)));
            }
            Button previous = CloneButton(template, panel, "PreviousButton", "← 이전",
                new Vector2(0.64f, 0.08f), new Vector2(0.74f, 0.15f));
            Button next = CloneButton(template, panel, "NextButton", "다음 →",
                new Vector2(0.85f, 0.08f), new Vector2(0.95f, 0.15f));
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/TMP/NotoSansCJKkr-Dynamic.asset");
            TMP_Text page = CreateText(panel, "PageLabel", "처리할 사건 없음", font,
                new Vector2(0.745f, 0.08f), new Vector2(0.845f, 0.15f));

            WIAdministrationMonthlyReportUGUIController controller = root.AddComponent<WIAdministrationMonthlyReportUGUIController>();
            SerializedObject serialized = new SerializedObject(controller);
            serialized.FindProperty("modal").objectReferenceValue = root.GetComponent<WIAdministrationModalUGUIController>();
            serialized.FindProperty("bodyLabel").objectReferenceValue = body;
            serialized.FindProperty("scrollRect").objectReferenceValue = panel.Find("RecordScroll").GetComponent<ScrollRect>();
            ButtonArray(serialized.FindProperty("actionButtons"), actions);
            serialized.FindProperty("previousButton").objectReferenceValue = previous;
            serialized.FindProperty("nextButton").objectReferenceValue = next;
            serialized.FindProperty("pageLabel").objectReferenceValue = page;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            GameObject existing = GameObject.Find("WIAdministrationMonthlyReportUGUI");
            if (existing != null) Object.DestroyImmediate(existing);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            WIAdministrationUGUISceneUtility.InstantiateUnderSceneRoot(prefab);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("월간 보고 UGUI를 생성하고 MainScene에 배치했습니다.");
        }

        // 공통 버튼을 복제해 지정한 영역에 배치합니다.
        private static Button CloneButton(Button template, Transform parent, string name, string caption, Vector2 min, Vector2 max)
        {
            Button button = Object.Instantiate(template, parent);
            button.name = name;
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            button.GetComponentInChildren<TMP_Text>().text = caption;
            return button;
        }

        // 행동 페이지 상태를 표시할 TMP 텍스트를 생성합니다.
        private static TMP_Text CreateText(Transform parent, string name, string caption, TMP_FontAsset font, Vector2 min, Vector2 max)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            gameObject.transform.SetParent(parent, false);
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            TMP_Text text = gameObject.GetComponent<TMP_Text>();
            text.text = caption;
            text.font = font;
            text.fontSize = 13f;
            text.color = new Color32(235, 239, 241, 255);
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            return text;
        }

        // 버튼 배열을 직렬화합니다.
        private static void ButtonArray(SerializedProperty property, IReadOnlyList<Button> values)
        {
            property.arraySize = values.Count;
            for (int index = 0; index < values.Count; index += 1) property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
        }
    }
}

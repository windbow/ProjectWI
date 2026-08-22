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
    public static class WIAdministrationDelegationUGUIBuilder
    {
        private const string SourcePath = "Assets/Prefabs/Administration/WIAdministrationHeroAssignmentUGUI.prefab";
        private const string PrefabPath = "Assets/Prefabs/Administration/WIAdministrationDelegationUGUI.prefab";

        // 영지관 후보와 운영 설정을 한 화면에 배치한 고정 UGUI 프리팹을 생성합니다.
        [MenuItem("WI/UI/Build Delegation UGUI")]
        public static void Build()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(SourcePath);
            root.name = "WIAdministrationDelegationUGUI";
            root.GetComponent<Canvas>().sortingOrder = 105;
            Object.DestroyImmediate(root.GetComponent<WIAdministrationHeroAssignmentUGUIController>());
            Transform panel = root.transform.Find("ModalRoot/ModalPanel");
            TMP_Text status = panel.Find("SlotStatus").GetComponent<TMP_Text>();
            panel.Find("PreviousButton").gameObject.SetActive(false);
            panel.Find("NextButton").gameObject.SetActive(false);
            panel.Find("PageLabel").gameObject.SetActive(false);
            RectTransform cardsRoot = panel.Find("CandidateCards").GetComponent<RectTransform>();
            cardsRoot.anchorMin = new Vector2(0.04f, 0.47f);
            cardsRoot.anchorMax = new Vector2(0.96f, 0.80f);

            List<Button> governorButtons = new();
            List<Image> portraits = new();
            List<TMP_Text> labels = new();
            for (int index = 0; index < 8; index += 1)
            {
                Transform card = cardsRoot.Find($"Candidate-{index}");
                governorButtons.Add(card.GetComponent<Button>());
                portraits.Add(card.Find("Portrait").GetComponent<Image>());
                labels.Add(card.Find("CandidateLabel").GetComponent<TMP_Text>());
            }

            GameObject controls = new GameObject("DelegationControls", typeof(RectTransform));
            controls.transform.SetParent(panel, false);
            RectTransform controlsRect = controls.GetComponent<RectTransform>();
            controlsRect.anchorMin = new Vector2(0.05f, 0.04f);
            controlsRect.anchorMax = new Vector2(0.95f, 0.45f);
            controlsRect.offsetMin = Vector2.zero;
            controlsRect.offsetMax = Vector2.zero;
            Button template = panel.Find("NextButton").GetComponent<Button>();
            string[] policyNames = { "균형", "번영", "연구", "전선", "인재" };
            List<Button> policies = new();
            for (int index = 0; index < policyNames.Length; index += 1)
            {
                float xMin = index * 0.2f + 0.005f;
                policies.Add(CloneButton(template, controls.transform, $"Policy-{index}", policyNames[index],
                    new Vector2(xMin, 0.74f), new Vector2(xMin + 0.19f, 0.94f)));
            }
            Button dismiss = CloneButton(template, controls.transform, "DismissGovernor", "영지관 해임",
                new Vector2(0.005f, 0.50f), new Vector2(0.25f, 0.69f));
            Button basic = CloneButton(template, controls.transform, "BasicBudget", "기본 예산",
                new Vector2(0.27f, 0.50f), new Vector2(0.62f, 0.69f));
            Button intensive = CloneButton(template, controls.transform, "IntensiveBudget", "집중 예산",
                new Vector2(0.64f, 0.50f), new Vector2(0.995f, 0.69f));
            TMP_Text preview = CreateText(controls.transform, "Preview", "예상 운영 결과", status.font,
                new Vector2(0.02f, 0.22f), new Vector2(0.98f, 0.46f), 16f);
            Button toggle = CloneButton(template, controls.transform, "ToggleDelegation", "영지관에게 위임",
                new Vector2(0.20f, 0f), new Vector2(0.80f, 0.18f));

            WIAdministrationDelegationUGUIController controller = root.AddComponent<WIAdministrationDelegationUGUIController>();
            SerializedObject serialized = new SerializedObject(controller);
            Set(serialized, "modal", root.GetComponent<WIAdministrationModalUGUIController>());
            Set(serialized, "statusLabel", status);
            Set(serialized, "messageLabel", panel.Find("Message").GetComponent<TMP_Text>());
            ButtonArray(serialized.FindProperty("governorButtons"), governorButtons);
            ImageArray(serialized.FindProperty("governorPortraits"), portraits);
            TextArray(serialized.FindProperty("governorLabels"), labels);
            Set(serialized, "dismissButton", dismiss);
            ButtonArray(serialized.FindProperty("policyButtons"), policies);
            Set(serialized, "basicBudgetButton", basic);
            Set(serialized, "intensiveBudgetButton", intensive);
            Set(serialized, "previewLabel", preview);
            Set(serialized, "toggleButton", toggle);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            WIAdministrationModalVisualUtility.Apply(root);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            GameObject existing = GameObject.Find("WIAdministrationDelegationUGUI");
            if (existing != null) Object.DestroyImmediate(existing);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            WIAdministrationUGUISceneUtility.InstantiateUnderSceneRoot(prefab);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("영지관 위임 UGUI를 생성하고 MainScene에 배치했습니다.");
        }

        // 버튼 원본을 복제해 지정한 앵커에 고정 배치합니다.
        private static Button CloneButton(Button template, Transform parent, string name, string caption, Vector2 min, Vector2 max)
        {
            Button button = Object.Instantiate(template, parent);
            button.name = name;
            button.gameObject.SetActive(true);
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            button.GetComponentInChildren<TMP_Text>().text = caption;
            return button;
        }

        // 예상 결과를 표시할 고정 TMP 텍스트를 생성합니다.
        private static TMP_Text CreateText(Transform parent, string name, string caption, TMP_FontAsset font,
            Vector2 min, Vector2 max, float size)
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
            text.fontSize = size;
            text.color = new Color32(235, 239, 241, 255);
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            return text;
        }

        // 단일 오브젝트 참조를 직렬화합니다.
        private static void Set(SerializedObject serialized, string name, Object value) => serialized.FindProperty(name).objectReferenceValue = value;

        // 버튼 배열을 직렬화합니다.
        private static void ButtonArray(SerializedProperty property, IReadOnlyList<Button> values)
        {
            property.arraySize = values.Count;
            for (int index = 0; index < values.Count; index += 1) property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
        }

        // 이미지 배열을 직렬화합니다.
        private static void ImageArray(SerializedProperty property, IReadOnlyList<Image> values)
        {
            property.arraySize = values.Count;
            for (int index = 0; index < values.Count; index += 1) property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
        }

        // TMP 텍스트 배열을 직렬화합니다.
        private static void TextArray(SerializedProperty property, IReadOnlyList<TMP_Text> values)
        {
            property.arraySize = values.Count;
            for (int index = 0; index < values.Count; index += 1) property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
        }
    }
}

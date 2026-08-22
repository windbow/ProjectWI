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
    public static class WIAdministrationCharacterActivityUGUIBuilder
    {
        private const string SourcePath = "Assets/Prefabs/Administration/WIAdministrationHeroAssignmentUGUI.prefab";
        private const string PrefabPath = "Assets/Prefabs/Administration/WIAdministrationCharacterActivityUGUI.prefab";

        // 영웅 카드 레이아웃을 재사용해 인재 활동 3단계 UGUI 프리팹을 생성합니다.
        [MenuItem("WI/UI/Build Character Activity UGUI")]
        public static void Build()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(SourcePath);
            root.name = "WIAdministrationCharacterActivityUGUI";
            root.GetComponent<Canvas>().sortingOrder = 102;
            Object.DestroyImmediate(root.GetComponent<WIAdministrationHeroAssignmentUGUIController>());

            Transform panel = root.transform.Find("ModalRoot/ModalPanel");
            TMP_Text context = panel.Find("SlotStatus").GetComponent<TMP_Text>();
            context.name = "ContextLabel";
            context.text = "이번 달 개인 활동을 수행할 인물을 선택하십시오.";
            GameObject cardRoot = panel.Find("CandidateCards").gameObject;
            List<Button> cardButtons = new();
            List<Image> portraits = new();
            List<TMP_Text> labels = new();
            for (int index = 0; index < 8; index += 1)
            {
                Transform card = cardRoot.transform.Find($"Candidate-{index}");
                cardButtons.Add(card.GetComponent<Button>());
                portraits.Add(card.Find("Portrait").GetComponent<Image>());
                labels.Add(card.Find("CandidateLabel").GetComponent<TMP_Text>());
            }

            GameObject activityRoot = new GameObject("ActivityRoot", typeof(RectTransform));
            activityRoot.transform.SetParent(panel, false);
            RectTransform activityRect = activityRoot.GetComponent<RectTransform>();
            activityRect.anchorMin = new Vector2(0.22f, 0.18f);
            activityRect.anchorMax = new Vector2(0.78f, 0.78f);
            activityRect.offsetMin = Vector2.zero;
            activityRect.offsetMax = Vector2.zero;
            Button template = panel.Find("NextButton").GetComponent<Button>();
            string[] captions =
            {
                "탐색 · 방랑 인재와 단서 발견",
                "교류 · 주둔 인물과 관계 개선",
                "영입 · 발견한 인재 설득",
                "훈련 · 경험과 공훈 획득",
                "휴식 · 피로와 부상 회복",
                "이동 · 같은 진영의 인접 성"
            };
            List<Button> activityButtons = new();
            for (int index = 0; index < captions.Length; index += 1)
            {
                Button button = Object.Instantiate(template, activityRoot.transform);
                button.name = $"Activity-{index}";
                RectTransform rect = button.GetComponent<RectTransform>();
                float yMax = 1f - index / 6f;
                rect.anchorMin = new Vector2(0f, yMax - 0.125f);
                rect.anchorMax = new Vector2(1f, yMax);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                button.GetComponentInChildren<TMP_Text>().text = captions[index];
                activityButtons.Add(button);
            }
            activityRoot.SetActive(false);

            WIAdministrationCharacterActivityUGUIController controller =
                root.AddComponent<WIAdministrationCharacterActivityUGUIController>();
            SerializedObject serialized = new SerializedObject(controller);
            Set(serialized, "modal", root.GetComponent<WIAdministrationModalUGUIController>());
            Set(serialized, "contextLabel", context);
            Set(serialized, "messageLabel", panel.Find("Message").GetComponent<TMP_Text>());
            Set(serialized, "cardRoot", cardRoot);
            ButtonArray(serialized.FindProperty("cardButtons"), cardButtons);
            ImageArray(serialized.FindProperty("cardPortraits"), portraits);
            TextArray(serialized.FindProperty("cardLabels"), labels);
            Set(serialized, "activityRoot", activityRoot);
            ButtonArray(serialized.FindProperty("activityButtons"), activityButtons);
            Set(serialized, "previousButton", panel.Find("PreviousButton").GetComponent<Button>());
            Set(serialized, "nextButton", panel.Find("NextButton").GetComponent<Button>());
            Set(serialized, "pageLabel", panel.Find("PageLabel").GetComponent<TMP_Text>());
            serialized.ApplyModifiedPropertiesWithoutUndo();

            WIAdministrationModalVisualUtility.Apply(root);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            GameObject existing = GameObject.Find("WIAdministrationCharacterActivityUGUI");
            if (existing != null) Object.DestroyImmediate(existing);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            WIAdministrationUGUISceneUtility.InstantiateUnderSceneRoot(prefab);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("인재 활동 UGUI를 생성하고 MainScene에 배치했습니다.");
        }

        // 단일 오브젝트 참조를 직렬화합니다.
        private static void Set(SerializedObject serialized, string name, Object value)
        {
            serialized.FindProperty(name).objectReferenceValue = value;
        }

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

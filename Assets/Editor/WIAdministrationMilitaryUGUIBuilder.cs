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
    public static class WIAdministrationMilitaryUGUIBuilder
    {
        private const string SourcePath = "Assets/Prefabs/Administration/WIAdministrationHeroAssignmentUGUI.prefab";
        private const string PrefabPath = "Assets/Prefabs/Administration/WIAdministrationMilitaryUGUI.prefab";

        // 전투 세션과 전투단을 카드로 표시하는 고정 UGUI 프리팹을 생성합니다.
        [MenuItem("WI/UI/Build Military UGUI")]
        public static void Build()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(SourcePath);
            root.name = "WIAdministrationMilitaryUGUI";
            root.GetComponent<Canvas>().sortingOrder = 106;
            Object.DestroyImmediate(root.GetComponent<WIAdministrationHeroAssignmentUGUIController>());
            Transform panel = root.transform.Find("ModalRoot/ModalPanel");
            panel.Find("PreviousButton").gameObject.SetActive(false);
            panel.Find("PageLabel").gameObject.SetActive(false);
            RectTransform cardsRoot = panel.Find("CandidateCards").GetComponent<RectTransform>();
            cardsRoot.anchorMin = new Vector2(0.04f, 0.19f);
            cardsRoot.anchorMax = new Vector2(0.96f, 0.79f);
            List<Button> buttons = new();
            List<TMP_Text> labels = new();
            for (int index = 0; index < 8; index += 1)
            {
                Transform card = cardsRoot.Find($"Candidate-{index}");
                card.Find("Portrait").gameObject.SetActive(false);
                TMP_Text label = card.Find("CandidateLabel").GetComponent<TMP_Text>();
                RectTransform labelRect = label.rectTransform;
                labelRect.anchorMin = new Vector2(0.06f, 0.08f);
                labelRect.anchorMax = new Vector2(0.94f, 0.92f);
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
                label.alignment = TextAlignmentOptions.MidlineLeft;
                buttons.Add(card.GetComponent<Button>());
                labels.Add(label);
            }
            Button create = panel.Find("NextButton").GetComponent<Button>();
            create.name = "CreateArmyButton";
            create.gameObject.SetActive(true);
            create.GetComponentInChildren<TMP_Text>().text = "새 전투단 편성";
            RectTransform createRect = create.GetComponent<RectTransform>();
            createRect.anchorMin = new Vector2(0.34f, 0.07f);
            createRect.anchorMax = new Vector2(0.66f, 0.14f);
            createRect.offsetMin = Vector2.zero;
            createRect.offsetMax = Vector2.zero;

            WIAdministrationMilitaryUGUIController controller = root.AddComponent<WIAdministrationMilitaryUGUIController>();
            SerializedObject serialized = new SerializedObject(controller);
            Set(serialized, "modal", root.GetComponent<WIAdministrationModalUGUIController>());
            Set(serialized, "statusLabel", panel.Find("SlotStatus").GetComponent<TMP_Text>());
            Set(serialized, "messageLabel", panel.Find("Message").GetComponent<TMP_Text>());
            SetArray(serialized.FindProperty("itemButtons"), buttons);
            SetArray(serialized.FindProperty("itemLabels"), labels);
            Set(serialized, "createButton", create);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            GameObject existing = GameObject.Find("WIAdministrationMilitaryUGUI");
            if (existing != null) Object.DestroyImmediate(existing);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            WIAdministrationUGUISceneUtility.InstantiateUnderSceneRoot(prefab);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("군사 전투단 UGUI를 생성하고 MainScene에 배치했습니다.");
        }

        // 단일 오브젝트 참조를 직렬화합니다.
        private static void Set(SerializedObject serialized, string name, Object value) => serialized.FindProperty(name).objectReferenceValue = value;

        // 고정 컨트롤 배열을 직렬화합니다.
        private static void SetArray<T>(SerializedProperty property, IReadOnlyList<T> values) where T : Object
        {
            property.arraySize = values.Count;
            for (int index = 0; index < values.Count; index += 1) property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
        }
    }
}

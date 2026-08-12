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
    public static class WIAdministrationHeroesUGUIBuilder
    {
        private const string SourcePath = "Assets/Prefabs/Administration/WIAdministrationHeroAssignmentUGUI.prefab";
        private const string PrefabPath = "Assets/Prefabs/Administration/WIAdministrationHeroesUGUI.prefab";

        // 전체 영웅과 작위를 페이지 카드로 관리하는 고정 UGUI 프리팹을 생성합니다.
        [MenuItem("WI/UI/Build Heroes UGUI")]
        public static void Build()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(SourcePath);
            root.name = "WIAdministrationHeroesUGUI";
            root.GetComponent<Canvas>().sortingOrder = 107;
            Object.DestroyImmediate(root.GetComponent<WIAdministrationHeroAssignmentUGUIController>());
            Transform panel = root.transform.Find("ModalRoot/ModalPanel");
            RectTransform cardsRoot = panel.Find("CandidateCards").GetComponent<RectTransform>();
            cardsRoot.anchorMin = new Vector2(0.04f, 0.18f);
            cardsRoot.anchorMax = new Vector2(0.96f, 0.80f);
            List<Button> buttons = new();
            List<Image> portraits = new();
            List<TMP_Text> labels = new();
            for (int index = 0; index < 8; index += 1)
            {
                Transform card = cardsRoot.Find($"Candidate-{index}");
                buttons.Add(card.GetComponent<Button>());
                portraits.Add(card.Find("Portrait").GetComponent<Image>());
                labels.Add(card.Find("CandidateLabel").GetComponent<TMP_Text>());
            }
            Button previous = panel.Find("PreviousButton").GetComponent<Button>();
            Button next = panel.Find("NextButton").GetComponent<Button>();
            TMP_Text page = panel.Find("PageLabel").GetComponent<TMP_Text>();
            Button back = Object.Instantiate(next, panel);
            back.name = "BackButton";
            back.gameObject.SetActive(false);
            back.GetComponentInChildren<TMP_Text>().text = "영웅 목록으로";
            RectTransform backRect = back.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0.39f, 0.07f);
            backRect.anchorMax = new Vector2(0.61f, 0.14f);
            backRect.offsetMin = Vector2.zero;
            backRect.offsetMax = Vector2.zero;

            WIAdministrationHeroesUGUIController controller = root.AddComponent<WIAdministrationHeroesUGUIController>();
            SerializedObject serialized = new SerializedObject(controller);
            Set(serialized, "modal", root.GetComponent<WIAdministrationModalUGUIController>());
            Set(serialized, "statusLabel", panel.Find("SlotStatus").GetComponent<TMP_Text>());
            Set(serialized, "messageLabel", panel.Find("Message").GetComponent<TMP_Text>());
            SetArray(serialized.FindProperty("cardButtons"), buttons);
            SetArray(serialized.FindProperty("cardPortraits"), portraits);
            SetArray(serialized.FindProperty("cardLabels"), labels);
            Set(serialized, "previousButton", previous);
            Set(serialized, "nextButton", next);
            Set(serialized, "pageLabel", page);
            Set(serialized, "backButton", back);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            GameObject existing = GameObject.Find("WIAdministrationHeroesUGUI");
            if (existing != null) Object.DestroyImmediate(existing);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            WIAdministrationUGUISceneUtility.InstantiateUnderSceneRoot(prefab);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("전역 영웅 UGUI를 생성하고 MainScene에 배치했습니다.");
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

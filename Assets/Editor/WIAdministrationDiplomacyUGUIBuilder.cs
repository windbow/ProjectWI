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
    public static class WIAdministrationDiplomacyUGUIBuilder
    {
        private const string SourcePath = "Assets/Prefabs/Administration/WIAdministrationHeroesUGUI.prefab";
        private const string PrefabPath = "Assets/Prefabs/Administration/WIAdministrationDiplomacyUGUI.prefab";

        // 외교 대상과 실행 명령을 페이지 카드로 표시하는 고정 UGUI 프리팹을 생성합니다.
        [MenuItem("WI/UI/Build Diplomacy UGUI")]
        public static void Build()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(SourcePath);
            root.name = "WIAdministrationDiplomacyUGUI";
            root.GetComponent<Canvas>().sortingOrder = 108;
            Object.DestroyImmediate(root.GetComponent<WIAdministrationHeroesUGUIController>());
            Transform panel = root.transform.Find("ModalRoot/ModalPanel");
            Transform cardsRoot = panel.Find("CandidateCards");
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
                buttons.Add(card.GetComponent<Button>());
                labels.Add(label);
            }
            Button previous = panel.Find("PreviousButton").GetComponent<Button>();
            Button next = panel.Find("NextButton").GetComponent<Button>();
            TMP_Text page = panel.Find("PageLabel").GetComponent<TMP_Text>();
            Button back = panel.Find("BackButton").GetComponent<Button>();
            back.GetComponentInChildren<TMP_Text>().text = "진영 목록으로";

            WIAdministrationDiplomacyUGUIController controller = root.AddComponent<WIAdministrationDiplomacyUGUIController>();
            SerializedObject serialized = new SerializedObject(controller);
            Set(serialized, "modal", root.GetComponent<WIAdministrationModalUGUIController>());
            Set(serialized, "statusLabel", panel.Find("SlotStatus").GetComponent<TMP_Text>());
            Set(serialized, "messageLabel", panel.Find("Message").GetComponent<TMP_Text>());
            SetArray(serialized.FindProperty("cardButtons"), buttons);
            SetArray(serialized.FindProperty("cardLabels"), labels);
            Set(serialized, "previousButton", previous);
            Set(serialized, "nextButton", next);
            Set(serialized, "pageLabel", page);
            Set(serialized, "backButton", back);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            GameObject existing = GameObject.Find("WIAdministrationDiplomacyUGUI");
            if (existing != null) Object.DestroyImmediate(existing);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            WIAdministrationUGUISceneUtility.InstantiateUnderSceneRoot(prefab);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("전역 외교 UGUI를 생성하고 MainScene 정리 루트에 배치했습니다.");
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

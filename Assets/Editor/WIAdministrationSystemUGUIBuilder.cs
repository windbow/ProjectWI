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
    public static class WIAdministrationSystemUGUIBuilder
    {
        private const string SourcePath = "Assets/Prefabs/Administration/WIAdministrationCouncilUGUI.prefab";
        private const string PrefabPath = "Assets/Prefabs/Administration/WIAdministrationSystemUGUI.prefab";

        // 설정과 저장 기능을 두 페이지의 고정 카드로 제공하는 UGUI 프리팹을 생성합니다.
        [MenuItem("WI/UI/Build System UGUI")]
        public static void Build()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(SourcePath);
            root.name = "WIAdministrationSystemUGUI";
            root.GetComponent<Canvas>().sortingOrder = 116;
            Object.DestroyImmediate(root.GetComponent<WIAdministrationCouncilUGUIController>());
            Transform panel = root.transform.Find("ModalRoot/ModalPanel");
            panel.Find("PreviousButton").gameObject.SetActive(true);
            panel.Find("NextButton").gameObject.SetActive(true);
            panel.Find("PageLabel").gameObject.SetActive(true);
            panel.Find("BackButton").gameObject.SetActive(false);
            Transform cardsRoot = panel.Find("CandidateCards");
            List<Button> buttons = new();
            List<TMP_Text> labels = new();
            for (int index = 0; index < 8; index += 1)
            {
                Transform card = cardsRoot.Find($"Candidate-{index}");
                buttons.Add(card.GetComponent<Button>());
                labels.Add(card.Find("CandidateLabel").GetComponent<TMP_Text>());
            }

            WIAdministrationSystemUGUIController controller = root.AddComponent<WIAdministrationSystemUGUIController>();
            SerializedObject serialized = new SerializedObject(controller);
            Set(serialized, "modal", root.GetComponent<WIAdministrationModalUGUIController>());
            Set(serialized, "statusLabel", panel.Find("SlotStatus").GetComponent<TMP_Text>());
            Set(serialized, "messageLabel", panel.Find("Message").GetComponent<TMP_Text>());
            Set(serialized, "previousButton", panel.Find("PreviousButton").GetComponent<Button>());
            Set(serialized, "nextButton", panel.Find("NextButton").GetComponent<Button>());
            Set(serialized, "pageLabel", panel.Find("PageLabel").GetComponent<TMP_Text>());
            SetArray(serialized.FindProperty("cardButtons"), buttons);
            SetArray(serialized.FindProperty("cardLabels"), labels);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            GameObject existing = GameObject.Find("WIAdministrationSystemUGUI");
            if (existing != null) Object.DestroyImmediate(existing);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            WIAdministrationUGUISceneUtility.InstantiateUnderSceneRoot(prefab);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("시스템 설정·저장 UGUI를 생성하고 MainScene 정리 루트에 배치했습니다.");
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

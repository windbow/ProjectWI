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
    public static class WIAdministrationSpecialFacilityUGUIBuilder
    {
        private const string SourcePath = "Assets/Prefabs/Administration/WIAdministrationHeroAssignmentUGUI.prefab";
        private const string PrefabPath = "Assets/Prefabs/Administration/WIAdministrationSpecialFacilityUGUI.prefab";

        // 영웅 카드 프레임을 재사용해 8개 특화 시설 선택 프리팹을 생성합니다.
        [MenuItem("WI/UI/Build Special Facility UGUI")]
        public static void Build()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(SourcePath);
            root.name = "WIAdministrationSpecialFacilityUGUI";
            root.GetComponent<Canvas>().sortingOrder = 103;
            Object.DestroyImmediate(root.GetComponent<WIAdministrationHeroAssignmentUGUIController>());
            Transform panel = root.transform.Find("ModalRoot/ModalPanel");
            TMP_Text status = panel.Find("SlotStatus").GetComponent<TMP_Text>();
            status.text = "특화 시설 0/0 · 새 시설 하나를 선택하십시오.";
            panel.Find("PreviousButton").gameObject.SetActive(false);
            panel.Find("NextButton").gameObject.SetActive(false);
            panel.Find("PageLabel").gameObject.SetActive(false);

            List<Button> buttons = new();
            List<Image> icons = new();
            List<TMP_Text> labels = new();
            Transform cards = panel.Find("CandidateCards");
            for (int index = 0; index < 8; index += 1)
            {
                Transform card = cards.Find($"Candidate-{index}");
                card.name = $"Facility-{index}";
                buttons.Add(card.GetComponent<Button>());
                Image icon = card.Find("Portrait").GetComponent<Image>();
                icon.name = "Icon";
                icons.Add(icon);
                TMP_Text label = card.Find("CandidateLabel").GetComponent<TMP_Text>();
                label.name = "FacilityLabel";
                label.fontSize = 13f;
                labels.Add(label);
            }

            WIAdministrationSpecialFacilityUGUIController controller =
                root.AddComponent<WIAdministrationSpecialFacilityUGUIController>();
            SerializedObject serialized = new SerializedObject(controller);
            Set(serialized, "modal", root.GetComponent<WIAdministrationModalUGUIController>());
            Set(serialized, "statusLabel", status);
            Set(serialized, "messageLabel", panel.Find("Message").GetComponent<TMP_Text>());
            ButtonArray(serialized.FindProperty("optionButtons"), buttons);
            ImageArray(serialized.FindProperty("optionIcons"), icons);
            TextArray(serialized.FindProperty("optionLabels"), labels);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            GameObject existing = GameObject.Find("WIAdministrationSpecialFacilityUGUI");
            if (existing != null) Object.DestroyImmediate(existing);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            WIAdministrationUGUISceneUtility.InstantiateUnderSceneRoot(prefab);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("특화 시설 선택 UGUI를 생성하고 MainScene에 배치했습니다.");
        }

        // 단일 오브젝트 참조를 직렬화합니다.
        private static void Set(SerializedObject serialized, string name, Object value) =>
            serialized.FindProperty(name).objectReferenceValue = value;

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

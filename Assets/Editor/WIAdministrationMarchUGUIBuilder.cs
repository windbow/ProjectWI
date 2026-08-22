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
    public static class WIAdministrationMarchUGUIBuilder
    {
        private const string SourcePath = "Assets/Prefabs/Administration/WIAdministrationHeroAssignmentUGUI.prefab";
        private const string PrefabPath = "Assets/Prefabs/Administration/WIAdministrationMarchUGUI.prefab";

        // 전투단·대장·인접 목표를 같은 고정 카드 영역에서 선택하는 원정 프리팹을 생성합니다.
        [MenuItem("WI/UI/Build March UGUI")]
        public static void Build()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(SourcePath);
            root.name = "WIAdministrationMarchUGUI";
            root.GetComponent<Canvas>().sortingOrder = 106;
            Object.DestroyImmediate(root.GetComponent<WIAdministrationHeroAssignmentUGUIController>());
            Transform panel = root.transform.Find("ModalRoot/ModalPanel");
            TMP_Text status = panel.Find("SlotStatus").GetComponent<TMP_Text>();
            RectTransform statusRect = status.rectTransform;
            statusRect.anchorMax = new Vector2(0.65f, 0.87f);
            Button createArmy = Object.Instantiate(panel.Find("NextButton").GetComponent<Button>(), panel);
            createArmy.name = "CreateArmyButton";
            createArmy.gameObject.SetActive(true);
            RectTransform createRect = createArmy.GetComponent<RectTransform>();
            createRect.anchorMin = new Vector2(0.68f, 0.805f);
            createRect.anchorMax = new Vector2(0.95f, 0.865f);
            createRect.offsetMin = Vector2.zero;
            createRect.offsetMax = Vector2.zero;
            createArmy.GetComponentInChildren<TMP_Text>().text = "새 전투단 편성";

            Transform cardsRoot = panel.Find("CandidateCards");
            List<Button> buttons = new();
            List<Image> images = new();
            List<TMP_Text> labels = new();
            for (int index = 0; index < 8; index += 1)
            {
                Transform card = cardsRoot.Find($"Candidate-{index}");
                buttons.Add(card.GetComponent<Button>());
                images.Add(card.Find("Portrait").GetComponent<Image>());
                labels.Add(card.Find("CandidateLabel").GetComponent<TMP_Text>());
            }

            WIAdministrationMarchUGUIController controller = root.AddComponent<WIAdministrationMarchUGUIController>();
            SerializedObject serialized = new SerializedObject(controller);
            Set(serialized, "modal", root.GetComponent<WIAdministrationModalUGUIController>());
            Set(serialized, "statusLabel", status);
            Set(serialized, "messageLabel", panel.Find("Message").GetComponent<TMP_Text>());
            ButtonArray(serialized.FindProperty("cardButtons"), buttons);
            ImageArray(serialized.FindProperty("cardImages"), images);
            TextArray(serialized.FindProperty("cardLabels"), labels);
            Set(serialized, "createArmyButton", createArmy);
            Set(serialized, "previousButton", panel.Find("PreviousButton").GetComponent<Button>());
            Set(serialized, "nextButton", panel.Find("NextButton").GetComponent<Button>());
            Set(serialized, "pageLabel", panel.Find("PageLabel").GetComponent<TMP_Text>());
            serialized.ApplyModifiedPropertiesWithoutUndo();

            WIAdministrationModalVisualUtility.Apply(root);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            GameObject existing = GameObject.Find("WIAdministrationMarchUGUI");
            if (existing != null) Object.DestroyImmediate(existing);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            WIAdministrationUGUISceneUtility.InstantiateUnderSceneRoot(prefab);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("원정 UGUI를 생성하고 MainScene에 배치했습니다.");
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

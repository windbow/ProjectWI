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
    public static class WIAdministrationBasicFacilityUGUIBuilder
    {
        private const string SourcePath = "Assets/Prefabs/Administration/WIAdministrationHeroAssignmentUGUI.prefab";
        private const string PrefabPath = "Assets/Prefabs/Administration/WIAdministrationBasicFacilityUGUI.prefab";

        // 공통 카드와 모달을 재사용해 기본 시설·선술집 의뢰 UGUI 프리팹을 생성합니다.
        [MenuItem("WI/UI/Build Basic Facility UGUI")]
        public static void Build()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(SourcePath);
            root.name = "WIAdministrationBasicFacilityUGUI";
            root.GetComponent<Canvas>().sortingOrder = 104;
            Object.DestroyImmediate(root.GetComponent<WIAdministrationHeroAssignmentUGUIController>());
            Transform panel = root.transform.Find("ModalRoot/ModalPanel");
            TMP_Text status = panel.Find("SlotStatus").GetComponent<TMP_Text>();
            panel.Find("PreviousButton").gameObject.SetActive(false);
            panel.Find("NextButton").gameObject.SetActive(false);
            panel.Find("PageLabel").gameObject.SetActive(false);
            GameObject cardRoot = panel.Find("CandidateCards").gameObject;

            List<Button> cards = new();
            List<Image> images = new();
            List<TMP_Text> labels = new();
            for (int index = 0; index < 8; index += 1)
            {
                Transform card = cardRoot.transform.Find($"Candidate-{index}");
                cards.Add(card.GetComponent<Button>());
                images.Add(card.Find("Portrait").GetComponent<Image>());
                TMP_Text label = card.Find("CandidateLabel").GetComponent<TMP_Text>();
                label.fontSize = 13f;
                labels.Add(label);
            }

            GameObject facilityRoot = new GameObject("FacilityRoot", typeof(RectTransform));
            facilityRoot.transform.SetParent(panel, false);
            RectTransform facilityRect = facilityRoot.GetComponent<RectTransform>();
            facilityRect.anchorMin = new Vector2(0.16f, 0.18f);
            facilityRect.anchorMax = new Vector2(0.84f, 0.78f);
            facilityRect.offsetMin = Vector2.zero;
            facilityRect.offsetMax = Vector2.zero;
            string[] descriptions =
            {
                "성관 · 영지관 임명과 성 운영",
                "시장 · 기본 거래와 영지 수입",
                "훈련소 · 개인 및 합동 훈련",
                "선술집 · 소문, 인재 단서와 월간 의뢰",
                "기본 시설은 모든 성이 보유하며 건설하거나 강화하지 않습니다."
            };
            for (int index = 0; index < descriptions.Length; index += 1)
            {
                GameObject textObject = new GameObject($"Description-{index}", typeof(RectTransform), typeof(TextMeshProUGUI));
                textObject.transform.SetParent(facilityRoot.transform, false);
                RectTransform rect = textObject.GetComponent<RectTransform>();
                float yMax = 1f - index * 0.145f;
                rect.anchorMin = new Vector2(0f, yMax - 0.11f);
                rect.anchorMax = new Vector2(1f, yMax);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                TMP_Text text = textObject.GetComponent<TMP_Text>();
                text.text = descriptions[index];
                text.font = status.font;
                text.fontSize = index == 4 ? 16f : 20f;
                text.color = new Color32(235, 239, 241, 255);
                text.alignment = TextAlignmentOptions.MidlineLeft;
                text.raycastTarget = false;
            }
            Button tavern = Object.Instantiate(panel.Find("NextButton").GetComponent<Button>(), facilityRoot.transform);
            tavern.name = "TavernQuestButton";
            tavern.gameObject.SetActive(true);
            RectTransform tavernRect = tavern.GetComponent<RectTransform>();
            tavernRect.anchorMin = new Vector2(0.18f, 0f);
            tavernRect.anchorMax = new Vector2(0.82f, 0.14f);
            tavernRect.offsetMin = Vector2.zero;
            tavernRect.offsetMax = Vector2.zero;
            tavern.GetComponentInChildren<TMP_Text>().text = "선술집 월간 의뢰 확인";
            cardRoot.SetActive(false);

            WIAdministrationBasicFacilityUGUIController controller =
                root.AddComponent<WIAdministrationBasicFacilityUGUIController>();
            SerializedObject serialized = new SerializedObject(controller);
            Set(serialized, "modal", root.GetComponent<WIAdministrationModalUGUIController>());
            Set(serialized, "statusLabel", status);
            Set(serialized, "messageLabel", panel.Find("Message").GetComponent<TMP_Text>());
            Set(serialized, "facilityRoot", facilityRoot);
            Set(serialized, "tavernButton", tavern);
            Set(serialized, "cardRoot", cardRoot);
            ButtonArray(serialized.FindProperty("cardButtons"), cards);
            ImageArray(serialized.FindProperty("cardImages"), images);
            TextArray(serialized.FindProperty("cardLabels"), labels);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            WIAdministrationModalVisualUtility.Apply(root);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            GameObject existing = GameObject.Find("WIAdministrationBasicFacilityUGUI");
            if (existing != null) Object.DestroyImmediate(existing);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            WIAdministrationUGUISceneUtility.InstantiateUnderSceneRoot(prefab);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("기본 시설 UGUI를 생성하고 MainScene에 배치했습니다.");
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

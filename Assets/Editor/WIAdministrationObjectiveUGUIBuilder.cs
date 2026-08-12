using ProjectWI.Administration;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ProjectWI.EditorTools
{
    public static class WIAdministrationObjectiveUGUIBuilder
    {
        private const string SourcePath = "Assets/Prefabs/Administration/WIAdministrationHeroAssignmentUGUI.prefab";
        private const string PrefabPath = "Assets/Prefabs/Administration/WIAdministrationObjectiveUGUI.prefab";

        // 캠페인 목표의 정세·조건·진행도·보상을 표시하는 고정 UGUI 프리팹을 생성합니다.
        [MenuItem("WI/UI/Build Objective UGUI")]
        public static void Build()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(SourcePath);
            root.name = "WIAdministrationObjectiveUGUI";
            root.GetComponent<Canvas>().sortingOrder = 108;
            Object.DestroyImmediate(root.GetComponent<WIAdministrationHeroAssignmentUGUIController>());
            Transform panel = root.transform.Find("ModalRoot/ModalPanel");
            panel.Find("SlotStatus").gameObject.SetActive(false);
            panel.Find("Message").gameObject.SetActive(false);
            panel.Find("CandidateCards").gameObject.SetActive(false);
            panel.Find("PreviousButton").gameObject.SetActive(false);
            panel.Find("NextButton").gameObject.SetActive(false);
            panel.Find("PageLabel").gameObject.SetActive(false);
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/TMP/NotoSansCJKkr-Dynamic.asset");
            panel.Find("Header/Title").GetComponent<TMP_Text>().font = font;

            TMP_Text situation = CreateText(panel, "Situation", "시작 정세", font, 22f,
                new Vector2(0.08f, 0.59f), new Vector2(0.92f, 0.80f), TextAlignmentOptions.TopLeft);
            TMP_Text description = CreateText(panel, "Description", "목표 조건", font, 20f,
                new Vector2(0.08f, 0.31f), new Vector2(0.92f, 0.57f), TextAlignmentOptions.TopLeft);
            TMP_Text progress = CreateText(panel, "Progress", "진행과 보상", font, 21f,
                new Vector2(0.08f, 0.20f), new Vector2(0.92f, 0.29f), TextAlignmentOptions.Center);
            Button confirm = Object.Instantiate(panel.Find("NextButton").GetComponent<Button>(), panel);
            confirm.name = "ConfirmButton";
            confirm.gameObject.SetActive(true);
            RectTransform confirmRect = confirm.GetComponent<RectTransform>();
            confirmRect.anchorMin = new Vector2(0.30f, 0.07f);
            confirmRect.anchorMax = new Vector2(0.70f, 0.15f);
            confirmRect.offsetMin = Vector2.zero;
            confirmRect.offsetMax = Vector2.zero;
            confirm.GetComponentInChildren<TMP_Text>().text = "목표 확인";

            WIAdministrationObjectiveUGUIController controller = root.AddComponent<WIAdministrationObjectiveUGUIController>();
            SerializedObject serialized = new SerializedObject(controller);
            serialized.FindProperty("modal").objectReferenceValue = root.GetComponent<WIAdministrationModalUGUIController>();
            serialized.FindProperty("situationLabel").objectReferenceValue = situation;
            serialized.FindProperty("descriptionLabel").objectReferenceValue = description;
            serialized.FindProperty("progressLabel").objectReferenceValue = progress;
            serialized.FindProperty("confirmButton").objectReferenceValue = confirm;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            GameObject existing = GameObject.Find("WIAdministrationObjectiveUGUI");
            if (existing != null) Object.DestroyImmediate(existing);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            WIAdministrationUGUISceneUtility.InstantiateUnderSceneRoot(prefab);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("캠페인 목표 UGUI를 생성하고 MainScene에 배치했습니다.");
        }

        // 지정한 영역에 자동 줄바꿈 TMP 텍스트를 생성합니다.
        private static TMP_Text CreateText(Transform parent, string name, string caption, TMP_FontAsset font,
            float size, Vector2 min, Vector2 max, TextAlignmentOptions alignment)
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
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            return text;
        }
    }
}

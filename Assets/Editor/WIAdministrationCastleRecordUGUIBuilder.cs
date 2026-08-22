using ProjectWI.Administration;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ProjectWI.EditorTools
{
    public static class WIAdministrationCastleRecordUGUIBuilder
    {
        private const string SourcePath = "Assets/Prefabs/Administration/WIAdministrationHeroAssignmentUGUI.prefab";
        private const string PrefabPath = "Assets/Prefabs/Administration/WIAdministrationCastleRecordUGUI.prefab";

        // 성 이미지와 스크롤 기록 본문을 가진 읽기 전용 상세 프리팹을 생성합니다.
        [MenuItem("WI/UI/Build Castle Record UGUI")]
        public static void Build()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(SourcePath);
            root.name = "WIAdministrationCastleRecordUGUI";
            root.GetComponent<Canvas>().sortingOrder = 107;
            Object.DestroyImmediate(root.GetComponent<WIAdministrationHeroAssignmentUGUIController>());
            Transform panel = root.transform.Find("ModalRoot/ModalPanel");
            panel.Find("SlotStatus").gameObject.SetActive(false);
            panel.Find("Message").gameObject.SetActive(false);
            panel.Find("CandidateCards").gameObject.SetActive(false);
            panel.Find("PreviousButton").gameObject.SetActive(false);
            panel.Find("NextButton").gameObject.SetActive(false);
            panel.Find("PageLabel").gameObject.SetActive(false);
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/Fonts/TMP/NotoSansCJKkr-Dynamic.asset");
            panel.Find("Header/Title").GetComponent<TMP_Text>().font = font;

            GameObject imageObject = new GameObject("CastleImage", typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(panel, false);
            RectTransform imageRect = imageObject.GetComponent<RectTransform>();
            imageRect.anchorMin = new Vector2(0.05f, 0.12f);
            imageRect.anchorMax = new Vector2(0.43f, 0.82f);
            imageRect.offsetMin = Vector2.zero;
            imageRect.offsetMax = Vector2.zero;
            Image castleImage = imageObject.GetComponent<Image>();
            castleImage.preserveAspect = true;
            castleImage.raycastTarget = false;

            GameObject scrollObject = new GameObject("RecordScroll", typeof(RectTransform), typeof(ScrollRect));
            scrollObject.transform.SetParent(panel, false);
            RectTransform scrollRectTransform = scrollObject.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = new Vector2(0.47f, 0.12f);
            scrollRectTransform.anchorMax = new Vector2(0.95f, 0.82f);
            scrollRectTransform.offsetMin = Vector2.zero;
            scrollRectTransform.offsetMax = Vector2.zero;
            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            viewport.transform.SetParent(scrollObject.transform, false);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            viewport.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.18f);
            GameObject content = new GameObject("Content", typeof(RectTransform), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;
            ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            GameObject bodyObject = new GameObject("Body", typeof(RectTransform), typeof(TextMeshProUGUI));
            bodyObject.transform.SetParent(content.transform, false);
            RectTransform bodyRect = bodyObject.GetComponent<RectTransform>();
            bodyRect.anchorMin = new Vector2(0f, 1f);
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.pivot = new Vector2(0.5f, 1f);
            bodyRect.offsetMin = new Vector2(18f, 0f);
            bodyRect.offsetMax = new Vector2(-18f, 0f);
            TMP_Text body = bodyObject.GetComponent<TMP_Text>();
            body.font = font;
            body.fontSize = 19f;
            body.color = new Color32(235, 239, 241, 255);
            body.alignment = TextAlignmentOptions.TopLeft;
            body.textWrappingMode = TextWrappingModes.Normal;
            body.raycastTarget = false;
            bodyObject.transform.SetParent(viewport.transform, false);
            bodyRect.anchorMin = new Vector2(0f, 1f);
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.pivot = new Vector2(0.5f, 1f);
            bodyRect.anchoredPosition = new Vector2(0f, -16f);
            bodyRect.sizeDelta = new Vector2(-36f, 900f);
            Object.DestroyImmediate(content);
            ScrollRect scroll = scrollObject.GetComponent<ScrollRect>();
            scroll.viewport = viewportRect;
            scroll.content = bodyRect;
            scroll.horizontal = false;
            scroll.vertical = true;

            WIAdministrationCastleRecordUGUIController controller = root.AddComponent<WIAdministrationCastleRecordUGUIController>();
            SerializedObject serialized = new SerializedObject(controller);
            serialized.FindProperty("modal").objectReferenceValue = root.GetComponent<WIAdministrationModalUGUIController>();
            serialized.FindProperty("castleImage").objectReferenceValue = castleImage;
            serialized.FindProperty("bodyLabel").objectReferenceValue = body;
            serialized.FindProperty("scrollRect").objectReferenceValue = scroll;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            WIAdministrationModalVisualUtility.Apply(root);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            GameObject existing = GameObject.Find("WIAdministrationCastleRecordUGUI");
            if (existing != null) Object.DestroyImmediate(existing);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            WIAdministrationUGUISceneUtility.InstantiateUnderSceneRoot(prefab);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("성 상세 기록 UGUI를 생성하고 MainScene에 배치했습니다.");
        }
    }
}

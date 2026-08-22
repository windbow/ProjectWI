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
        private const string ObjectiveFramePath = "Assets/Resources/UI/Generated/objective_modal_frame_v1.png";
        private const string ConfirmButtonPath = "Assets/Resources/UI/Generated/objective_confirm_button_v1.png";
        private const string ProgressTrackPath = "Assets/Resources/UI/Generated/objective_progress_track_v1.png";
        private const string ProgressFillPath = "Assets/Resources/UI/Generated/objective_progress_fill_v1.png";
        private const string RewardStripPath = "Assets/Resources/UI/Generated/objective_reward_strip_v1.png";

        // 캠페인 목표의 정세·조건·진행도·보상을 표시하는 고정 UGUI 프리팹을 생성합니다.
        [MenuItem("WI/UI/Build Objective UGUI")]
        public static void Build()
        {
            ConfigureObjectiveAssetImporters();
            GameObject root = PrefabUtility.LoadPrefabContents(SourcePath);
            root.name = "WIAdministrationObjectiveUGUI";
            root.GetComponent<Canvas>().sortingOrder = 108;
            Object.DestroyImmediate(root.GetComponent<WIAdministrationHeroAssignmentUGUIController>());
            Transform panel = root.transform.Find("ModalRoot/ModalPanel");
            panel.GetComponent<Image>().sprite = LoadSprite(ObjectiveFramePath);
            panel.GetComponent<Image>().type = Image.Type.Simple;
            panel.GetComponent<Image>().color = Color.white;
            string[] obsoleteNames = { "SlotStatus", "Message", "CandidateCards", "PreviousButton", "PageLabel" };
            foreach (string obsoleteName in obsoleteNames)
            {
                Object.DestroyImmediate(panel.Find(obsoleteName).gameObject);
            }
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/TMP/NotoSansCJKkr-Dynamic.asset");
            Image headerImage = panel.Find("Header").GetComponent<Image>();
            headerImage.enabled = false;
            TMP_Text title = panel.Find("Header/Title").GetComponent<TMP_Text>();
            title.font = font;
            title.fontSize = 29f;
            title.alignment = TextAlignmentOptions.MidlineLeft;
            RectTransform titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.18f, 0.12f);
            titleRect.anchorMax = new Vector2(0.72f, 0.88f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            Button close = panel.Find("Header/CloseButton").GetComponent<Button>();
            RectTransform closeRect = close.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.87f, 0.18f);
            closeRect.anchorMax = new Vector2(0.94f, 0.82f);
            closeRect.offsetMin = Vector2.zero;
            closeRect.offsetMax = Vector2.zero;
            TMP_Text closeLabel = close.GetComponentInChildren<TMP_Text>(true);
            if (closeLabel != null)
            {
                closeLabel.font = font;
            }

            CreateText(panel, "SituationHeading", "현재 상황", font, 18f,
                new Vector2(0.09f, 0.73f), new Vector2(0.28f, 0.79f), TextAlignmentOptions.MidlineLeft);
            CreateText(panel, "ConditionHeading", "달성 조건", font, 18f,
                new Vector2(0.09f, 0.46f), new Vector2(0.28f, 0.52f), TextAlignmentOptions.MidlineLeft);
            CreateText(panel, "ProgressHeading", "진행 상황", font, 17f,
                new Vector2(0.09f, 0.23f), new Vector2(0.28f, 0.28f), TextAlignmentOptions.MidlineLeft);
            CreateText(panel, "RewardHeading", "보상", font, 17f,
                new Vector2(0.55f, 0.23f), new Vector2(0.70f, 0.28f), TextAlignmentOptions.MidlineLeft);

            TMP_Text situation = CreateText(panel, "Situation", "시작 정세", font, 20f,
                new Vector2(0.10f, 0.56f), new Vector2(0.86f, 0.72f), TextAlignmentOptions.TopLeft);
            TMP_Text description = CreateText(panel, "Description", "목표 조건", font, 20f,
                new Vector2(0.10f, 0.32f), new Vector2(0.86f, 0.45f), TextAlignmentOptions.TopLeft);
            TMP_Text progress = CreateText(panel, "Progress", "0 / 0", font, 22f,
                new Vector2(0.38f, 0.15f), new Vector2(0.51f, 0.21f), TextAlignmentOptions.Center);
            Image rewardStrip = CreateImage(panel, "RewardStrip", Color.white,
                new Vector2(0.55f, 0.145f), new Vector2(0.91f, 0.205f));
            rewardStrip.sprite = LoadSprite(RewardStripPath);
            rewardStrip.type = Image.Type.Simple;
            TMP_Text rewardGold = CreateText(panel, "RewardGold", "G 0", font, 18f,
                new Vector2(0.585f, 0.15f), new Vector2(0.665f, 0.20f), TextAlignmentOptions.Center);
            TMP_Text rewardMana = CreateText(panel, "RewardMana", "M 0", font, 18f,
                new Vector2(0.705f, 0.15f), new Vector2(0.785f, 0.20f), TextAlignmentOptions.Center);
            TMP_Text rewardInfluence = CreateText(panel, "RewardInfluence", "I 0", font, 18f,
                new Vector2(0.825f, 0.15f), new Vector2(0.905f, 0.20f), TextAlignmentOptions.Center);
            Image progressBackground = CreateImage(panel, "ProgressBackground", Color.white,
                new Vector2(0.10f, 0.15f), new Vector2(0.37f, 0.20f));
            progressBackground.sprite = LoadSprite(ProgressTrackPath);
            progressBackground.type = Image.Type.Simple;
            Image progressFill = CreateImage(progressBackground.transform, "ProgressFill", Color.white,
                new Vector2(0.045f, 0.25f), new Vector2(0.955f, 0.75f));
            progressFill.sprite = LoadSprite(ProgressFillPath);
            progressFill.type = Image.Type.Filled;
            progressFill.fillMethod = Image.FillMethod.Horizontal;
            progressFill.fillOrigin = 0;
            progressFill.fillAmount = 0f;
            progressBackground.transform.SetAsFirstSibling();

            Button confirm = panel.Find("NextButton").GetComponent<Button>();
            confirm.name = "ConfirmButton";
            confirm.gameObject.SetActive(true);
            RectTransform confirmRect = confirm.GetComponent<RectTransform>();
            confirmRect.anchorMin = new Vector2(0.34f, 0.035f);
            confirmRect.anchorMax = new Vector2(0.66f, 0.105f);
            confirmRect.offsetMin = Vector2.zero;
            confirmRect.offsetMax = Vector2.zero;
            confirm.GetComponentInChildren<TMP_Text>().text = "목표 확인";
            confirm.GetComponentInChildren<TMP_Text>().font = font;
            confirm.image.sprite = LoadSprite(ConfirmButtonPath);
            confirm.image.type = Image.Type.Simple;

            WIAdministrationObjectiveUGUIController controller = root.AddComponent<WIAdministrationObjectiveUGUIController>();
            SerializedObject serialized = new SerializedObject(controller);
            serialized.FindProperty("modal").objectReferenceValue = root.GetComponent<WIAdministrationModalUGUIController>();
            serialized.FindProperty("situationLabel").objectReferenceValue = situation;
            serialized.FindProperty("descriptionLabel").objectReferenceValue = description;
            serialized.FindProperty("progressLabel").objectReferenceValue = progress;
            serialized.FindProperty("rewardGoldLabel").objectReferenceValue = rewardGold;
            serialized.FindProperty("rewardManaLabel").objectReferenceValue = rewardMana;
            serialized.FindProperty("rewardInfluenceLabel").objectReferenceValue = rewardInfluence;
            serialized.FindProperty("progressFill").objectReferenceValue = progressFill;
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

        // 목표 모달에 사용하는 전용 이미지들을 UGUI용 단일 Sprite로 임포트합니다.
        private static void ConfigureObjectiveAssetImporters()
        {
            string[] paths = { ObjectiveFramePath, ConfirmButtonPath, ProgressTrackPath, ProgressFillPath, RewardStripPath };
            foreach (string path in paths)
            {
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    continue;
                }
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }

        // 지정한 경로에서 첫 번째 Sprite를 불러옵니다.
        private static Sprite LoadSprite(string path)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (Object asset in assets)
            {
                if (asset is Sprite sprite) return sprite;
            }
            return null;
        }

        // 프리팹에 고정 배치할 단색 Image를 생성합니다.
        private static Image CreateImage(Transform parent, string name, Color color, Vector2 min, Vector2 max)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = gameObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
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

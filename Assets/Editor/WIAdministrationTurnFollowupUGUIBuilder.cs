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
    public static class WIAdministrationTurnFollowupUGUIBuilder
    {
        private const string SourcePath = "Assets/Prefabs/Administration/WIAdministrationCouncilUGUI.prefab";
        private const string PrefabPath = "Assets/Prefabs/Administration/WIAdministrationTurnFollowupUGUI.prefab";
        private const string FramePath = "Assets/Resources/UI/Generated/turn_followup_frame_v1.png";
        private const string InfoPanelPath = "Assets/Resources/UI/Generated/turn_followup_info_panel_v1.png";
        private const string HeaderEmblemPath = "Assets/Resources/UI/Generated/turn_followup_header_emblem_v1.png";
        private const string MapCompassPath = "Assets/Resources/UI/Generated/turn_followup_map_compass_v1.png";
        private const string CheckBadgePath = "Assets/Resources/UI/Generated/turn_followup_check_badge_v1.png";
        private const string CloseButtonPath = "Assets/Resources/UI/Generated/turn_followup_close_button_v1.png";
        private const string ChoiceDividerPath = "Assets/Resources/UI/Generated/turn_followup_choice_divider_v1.png";
        private const string SecondaryButtonPath = "Assets/Resources/UI/Generated/turn_followup_button_normal_v1.png";
        private const string PrimaryButtonPath = "Assets/Resources/UI/Generated/turn_followup_button_primary_v1.png";
        private static readonly Vector4 InfoPanelBorder = new Vector4(48f, 48f, 48f, 48f);

        // 턴 연산 안내, 캠페인 결과와 튜토리얼에 공용으로 쓰는 고정 UGUI 프리팹을 생성합니다.
        [MenuItem("WI/UI/Build Turn Followup UGUI")]
        public static void Build()
        {
            ConfigureAssetImporters();
            GameObject root = PrefabUtility.LoadPrefabContents(SourcePath);
            root.name = "WIAdministrationTurnFollowupUGUI";
            root.GetComponent<Canvas>().sortingOrder = 114;
            Object.DestroyImmediate(root.GetComponent<WIAdministrationCouncilUGUIController>());
            Transform panel = root.transform.Find("ModalRoot/ModalPanel");
            Image panelImage = panel.GetComponent<Image>();
            panelImage.sprite = LoadSprite(FramePath);
            panelImage.type = Image.Type.Simple;
            panelImage.color = Color.white;

            Transform header = panel.Find("Header");
            header.GetComponent<Image>().enabled = false;
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/TMP/NotoSansCJKkr-Dynamic.asset");
            ConfigureHeader(header, font);
            RemoveLegacyChildren(panel, header);

            Image descriptionPanel = CreateSlicedPanelImage(panel, "DescriptionPanel", LoadSprite(InfoPanelPath),
                new Vector2(0.10f, 0.66f), new Vector2(0.90f, 0.82f));
            Image descriptionIcon = CreateImage(descriptionPanel.transform, "DescriptionIcon", LoadSprite(HeaderEmblemPath),
                new Vector2(0.04f, 0.12f), new Vector2(0.16f, 0.88f));
            descriptionIcon.preserveAspect = true;
            TMP_Text description = CreateText(descriptionPanel.transform, "Description", "안내 내용", font, 20f,
                new Vector2(0.19f, 0.18f), new Vector2(0.94f, 0.82f), TextAlignmentOptions.MidlineLeft);

            GameObject tutorialContent = new GameObject("TutorialContent", typeof(RectTransform));
            tutorialContent.transform.SetParent(panel, false);
            SetRect(tutorialContent.GetComponent<RectTransform>(), new Vector2(0.10f, 0.30f), new Vector2(0.90f, 0.62f));
            Image tutorialPanel = CreateSlicedPanelImage(tutorialContent.transform, "ChecklistPanel", LoadSprite(InfoPanelPath), Vector2.zero, Vector2.one);
            Image compass = CreateImage(tutorialPanel.transform, "Compass", LoadSprite(MapCompassPath),
                new Vector2(0.04f, 0.08f), new Vector2(0.34f, 0.92f));
            compass.preserveAspect = true;
            CreateText(tutorialPanel.transform, "ChecklistTitle", "다음 턴 준비", font, 26f,
                new Vector2(0.39f, 0.68f), new Vector2(0.88f, 0.88f), TextAlignmentOptions.Center);
            CreateChecklistRow(tutorialPanel.transform, "MapCheck", "대륙 지도 확인 완료", font,
                new Vector2(0.40f, 0.40f), new Vector2(0.91f, 0.62f));
            CreateChecklistRow(tutorialPanel.transform, "CommandCheck", "세력 명령 확인 완료", font,
                new Vector2(0.40f, 0.14f), new Vector2(0.91f, 0.36f));

            TMP_Text message = CreateText(panel, "Message", string.Empty, font, 24f,
                new Vector2(0.20f, 0.37f), new Vector2(0.80f, 0.55f), TextAlignmentOptions.Center);

            Button primary = CreateButton(panel, "Choice-0", LoadSprite(PrimaryButtonPath), font,
                new Vector2(0.52f, 0.075f), new Vector2(0.88f, 0.19f));
            Button secondary = CreateButton(panel, "Choice-1", LoadSprite(SecondaryButtonPath), font,
                new Vector2(0.12f, 0.075f), new Vector2(0.48f, 0.19f));
            TMP_Text primaryDescription = CreateText(panel, "ChoiceDescription-0", "이번 안내를 완료합니다.", font, 16f,
                new Vector2(0.52f, 0.195f), new Vector2(0.88f, 0.245f), TextAlignmentOptions.Center);
            TMP_Text secondaryDescription = CreateText(panel, "ChoiceDescription-1", "남은 튜토리얼을 표시하지 않습니다.", font, 16f,
                new Vector2(0.12f, 0.195f), new Vector2(0.48f, 0.245f), TextAlignmentOptions.Center);
            CreateImage(panel, "ChoiceDivider-0", LoadSprite(ChoiceDividerPath),
                new Vector2(0.52f, 0.245f), new Vector2(0.88f, 0.275f));
            CreateImage(panel, "ChoiceDivider-1", LoadSprite(ChoiceDividerPath),
                new Vector2(0.12f, 0.245f), new Vector2(0.48f, 0.275f));
            List<Button> buttons = new() { primary, secondary };
            List<TMP_Text> labels = new() { primary.GetComponentInChildren<TMP_Text>(), secondary.GetComponentInChildren<TMP_Text>() };
            List<TMP_Text> descriptions = new() { primaryDescription, secondaryDescription };

            WIAdministrationTurnFollowupUGUIController controller = root.AddComponent<WIAdministrationTurnFollowupUGUIController>();
            SerializedObject serialized = new SerializedObject(controller);
            Set(serialized, "modal", root.GetComponent<WIAdministrationModalUGUIController>());
            Set(serialized, "descriptionLabel", description);
            Set(serialized, "messageLabel", message);
            Set(serialized, "tutorialContent", tutorialContent);
            SetArray(serialized.FindProperty("choiceButtons"), buttons);
            SetArray(serialized.FindProperty("choiceLabels"), labels);
            SetArray(serialized.FindProperty("choiceDescriptionLabels"), descriptions);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            GameObject existing = GameObject.Find("WIAdministrationTurnFollowupUGUI");
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            WIAdministrationUGUISceneUtility.InstantiateUnderSceneRoot(prefab);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("턴 후속 전용 UGUI를 생성하고 MainScene 정리 루트에 배치했습니다.");
        }

        // 전용 이미지 에셋을 UGUI Sprite 설정으로 통일합니다.
        private static void ConfigureAssetImporters()
        {
            string[] paths = { FramePath, InfoPanelPath, HeaderEmblemPath, MapCompassPath, CheckBadgePath,
                CloseButtonPath, ChoiceDividerPath, SecondaryButtonPath, PrimaryButtonPath };
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
                importer.filterMode = FilterMode.Bilinear;
                if (path == SecondaryButtonPath || path == PrimaryButtonPath)
                {
                    importer.spriteBorder = Vector4.zero;
                }
                else if (path == InfoPanelPath)
                {
                    importer.spriteBorder = InfoPanelBorder;
                }
                importer.SaveAndReimport();
            }
        }

        // 전용 배경과 겹치는 기존 헤더 이미지를 숨기고 제목과 닫기 버튼을 배치합니다.
        private static void ConfigureHeader(Transform header, TMP_FontAsset font)
        {
            TMP_Text title = header.Find("Title").GetComponent<TMP_Text>();
            title.font = font;
            title.fontSize = 30f;
            title.alignment = TextAlignmentOptions.Center;
            SetRect(title.GetComponent<RectTransform>(), new Vector2(0.22f, 0.18f), new Vector2(0.78f, 0.82f));
            Button close = header.Find("CloseButton").GetComponent<Button>();
            SetRect(close.GetComponent<RectTransform>(), new Vector2(0.91f, 0.20f), new Vector2(0.965f, 0.80f));
            close.image.sprite = LoadSprite(CloseButtonPath);
            close.image.type = Image.Type.Simple;
            TMP_Text closeLabel = close.GetComponentInChildren<TMP_Text>();
            closeLabel.text = string.Empty;
            closeLabel.gameObject.SetActive(false);
        }

        // 공용 의회 프리팹에서 턴 후속 화면에 쓰지 않는 기존 자식을 제거합니다.
        private static void RemoveLegacyChildren(Transform panel, Transform preservedHeader)
        {
            List<GameObject> obsolete = new();
            foreach (Transform child in panel)
            {
                if (child != preservedHeader)
                {
                    obsolete.Add(child.gameObject);
                }
            }
            foreach (GameObject gameObject in obsolete)
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        // 완료 배지와 설명을 포함한 체크 행을 생성합니다.
        private static void CreateChecklistRow(Transform parent, string name, string caption, TMP_FontAsset font, Vector2 min, Vector2 max)
        {
            Image row = CreateSlicedPanelImage(parent, name, LoadSprite(InfoPanelPath), min, max);
            Image badge = CreateImage(row.transform, "Badge", LoadSprite(CheckBadgePath),
                new Vector2(0.04f, 0.08f), new Vector2(0.18f, 0.92f));
            badge.preserveAspect = true;
            CreateText(row.transform, "Label", caption, font, 20f,
                new Vector2(0.20f, 0.10f), new Vector2(0.94f, 0.90f), TextAlignmentOptions.MidlineLeft);
        }

        // 고정 선택 버튼과 TMP 라벨을 생성합니다.
        private static Button CreateButton(Transform parent, string name, Sprite sprite, TMP_FontAsset font, Vector2 min, Vector2 max)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            gameObject.transform.SetParent(parent, false);
            SetRect(gameObject.GetComponent<RectTransform>(), min, max);
            Image image = gameObject.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.color = Color.white;
            TMP_Text label = CreateText(gameObject.transform, "Label", "선택", font, 23f,
                new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.92f), TextAlignmentOptions.Center);
            label.textWrappingMode = TextWrappingModes.Normal;
            return gameObject.GetComponent<Button>();
        }

        // 지정한 Sprite를 사용하는 비입력 Image를 생성합니다.
        private static Image CreateImage(Transform parent, string name, Sprite sprite, Vector2 min, Vector2 max)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            SetRect(gameObject.GetComponent<RectTransform>(), min, max);
            Image image = gameObject.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.color = Color.white;
            image.raycastTarget = false;
            return image;
        }

        // 9-Slice 테두리를 유지해야 하는 정보 패널 이미지를 생성합니다.
        private static Image CreateSlicedPanelImage(Transform parent, string name, Sprite sprite, Vector2 min, Vector2 max)
        {
            Image image = CreateImage(parent, name, sprite, min, max);
            image.type = Image.Type.Sliced;
            return image;
        }

        // 지정한 영역에 자동 줄바꿈 TMP 텍스트를 생성합니다.
        private static TMP_Text CreateText(Transform parent, string name, string caption, TMP_FontAsset font, float size,
            Vector2 min, Vector2 max, TextAlignmentOptions alignment)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            gameObject.transform.SetParent(parent, false);
            SetRect(gameObject.GetComponent<RectTransform>(), min, max);
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

        // RectTransform의 앵커 영역과 오프셋을 고정합니다.
        private static void SetRect(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        // 지정한 경로에서 첫 번째 Sprite를 불러옵니다.
        private static Sprite LoadSprite(string path)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (Object asset in assets)
            {
                if (asset is Sprite sprite)
                {
                    return sprite;
                }
            }
            return null;
        }

        // 단일 오브젝트 참조를 직렬화합니다.
        private static void Set(SerializedObject serialized, string name, Object value)
        {
            serialized.FindProperty(name).objectReferenceValue = value;
        }

        // 고정 컨트롤 배열을 직렬화합니다.
        private static void SetArray<T>(SerializedProperty property, IReadOnlyList<T> values) where T : Object
        {
            property.arraySize = values.Count;
            for (int index = 0; index < values.Count; index += 1)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            }
        }
    }
}

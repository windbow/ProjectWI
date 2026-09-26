using System.Collections.Generic;
using ProjectWI.Administration;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectWI.EditorTools
{
    public static class WIScenarioSelectionPrefabUtility
    {
        // 선택 화면 제작에 사용하는 에셋 경로와 편집 중 임시 참조입니다.
        private const string PrefabPath = "Assets/Prefabs/Administration/WICampaignTitleUGUI.prefab";
        private const string DatabasePath = "Assets/Data/ScriptableObject/Administration/WI_AdministrationDatabase.asset";
        private const string ArtPath = "Assets/Art/UI/ScenarioSelection/";
        private static TMP_FontAsset font;
        private static WIAdministrationDatabaseSO database;
        private static readonly List<TMP_Text> fixedLabels = new List<TMP_Text>();
        private static readonly List<string> fixedUids = new List<string>();

        // 완성된 두 단계 화면을 프리팹에 저장하며 런타임 UI 제작을 사용하지 않습니다.
        [MenuItem("WI/UI/Rebuild Scenario Selection")]
        public static void Rebuild()
        {
            if (EditorApplication.isPlaying == true)
            {
                Debug.LogError("플레이 모드를 종료한 뒤 시나리오 프리팹을 제작하세요.");
                return;
            }
            ImportArt("Scenario_Ares_V1.png");
            ImportArt("Scenario_Free_V1.png");
            database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            SeedPresentation();
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                font = root.GetComponentInChildren<TMP_Text>(true).font;
                var controller = root.GetComponent<WICampaignTitleUGUIController>();
                var serialized = new SerializedObject(controller);
                fixedLabels.Clear();
                fixedUids.Clear();
                for (int index = root.transform.childCount - 1; index >= 0; index -= 1)
                {
                    Object.DestroyImmediate(root.transform.GetChild(index).gameObject);
                }
                var scaler = root.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
                Image backdrop = Image("Backdrop", root.transform, 0, 0, 1920, 1080, null, new Color32(9, 17, 26, 255));
                Stretch(backdrop.rectTransform);
                var panel = Rect("CampaignPanel", root.transform, 0, 0, 1920, 1080);
                panel.anchorMin = panel.anchorMax = new Vector2(.5f, .5f);
                panel.pivot = new Vector2(.5f, .5f);
                panel.anchoredPosition = Vector2.zero;
                Image header = Artwork("HeaderArt", panel, 0, 0, 1920, 155, Art(1));
                header.color = new Color32(70, 87, 108, 255);
                Image("HeaderShade", panel, 0, 0, 1920, 155, null, new Color(0.02f, .04f, .07f, .7f));
                var title = Text("Title", panel, 350, 22, 1220, 65, 48, database.GetText("UI_SCENARIO_TITLE"));
                title.alignment = TextAlignmentOptions.Center;
                var subtitle = Text("Description", panel, 350, 94, 1220, 40, 24, database.GetText("UI_SCENARIO_SUBTITLE"));
                subtitle.alignment = TextAlignmentOptions.Center;
                Rule(panel, 40, 150, 1840);
                var page = Rect("ScenarioPage", panel, 0, 165, 1920, 795);
                Frame("ListPanel", page, 32, 0, 552, 795);
                Fixed("ListHeading", page, 58, 20, 480, 46, 30, "UI_SCENARIO_LIST");
                var buttons = new Button[3];
                var labels = new TMP_Text[3];
                var images = new Image[3];
                var subtitles = new TMP_Text[3];
                var badges = new TMP_Text[3];
                for (int index = 0; index < 3; index += 1)
                {
                    var definition = database.CampaignVariants[index];
                    var card = Image("VariantCard_" + index, page, 52, 87 + index * 344, 512, 324, null,
                        index == 0 ? new Color32(99, 199, 247, 255) : new Color32(111, 129, 146, 255));
                    var button = card.gameObject.AddComponent<Button>();
                    card.raycastTarget = true;
                    button.targetGraphic = card;
                    button.transition = Selectable.Transition.ColorTint;
                    buttons[index] = button;
                    images[index] = Image("Artwork", card.transform, 3, 3, 506, 318, definition.SelectionArtwork, Color.white);
                    Image("TextShade", card.transform, 3, 211, 506, 110, null, new Color(.025f, .045f, .075f, .94f));
                    Image("BadgeBackground", card.transform, 16, 15, 230, 44, null, new Color(.025f, .065f, .11f, .95f));
                    badges[index] = Text("Category", card.transform, 22, 19, 450, 38, 22, definition.DisplayName.Get(false));
                    badges[index].fontStyle = FontStyles.Bold;
                    labels[index] = Text("Label", card.transform, 22, 220, 468, 50, 35, database.GetText(definition.SelectionTitleUid));
                    subtitles[index] = Text("Subtitle", card.transform, 22, 275, 468, 33, 21, database.GetText(definition.SelectionSubtitleUid));
                    card.gameObject.SetActive(index < 2);
                }
                Frame("DetailPanel", page, 600, 0, 1288, 795);
                var artwork = Artwork("DetailArtwork", page, 603, 3, 1282, 528, Art(0));
                Image("StoryShade", page, 603, 348, 1282, 211, null, new Color(.025f, .045f, .075f, .90f));
                var detailTitle = Text("DetailTitle", page, 630, 358, 1170, 60, 42, database.GetText("UI_SCENARIO_ARES_TITLE"));
                var detailSubtitle = Text("DetailSubtitle", page, 630, 423, 1200, 38, 25, database.GetText("UI_SCENARIO_ARES_SUBTITLE"));
                var story = Text("Story", page, 630, 471, 1200, 83, 24, database.GetText("UI_SCENARIO_ARES_STORY"));
                var faction = Info(page, "Faction", 626, "UI_SCENARIO_FACTION", "림가르드", "Assets/Art/Factions/Emblem_Avalon_V1.png");
                var protagonist = Info(page, "Protagonist", 1044, "UI_SCENARIO_PROTAGONIST", "키리엔", "Assets/Art/Characters/Ares/Ares_Portrait_Face_V1.png");
                var castle = Info(page, "Castle", 1462, "UI_SCENARIO_CASTLE", "프로스트혼", "Assets/Art/Castles/Castle_Avalon_V1.png");
                Frame("ObjectivePanel", page, 626, 692, 1236, 78);
                Fixed("ObjectiveHeading", page, 650, 710, 180, 40, 26, "UI_SCENARIO_OBJECTIVE");
                var objective = Text("Objective", page, 855, 710, 970, 40, 26, database.GetText("UI_SCENARIO_ARES_OBJECTIVE"));

                var settings = Rect("SettingsPage", panel, 32, 165, 1856, 795);
                Frame("SettingsFrame", settings, 0, 0, 1856, 795);
                var summary = Text("SelectedScenario", settings, 64, 42, 1728, 65, 42, database.GetText("UI_SCENARIO_ARES_TITLE"));
                summary.alignment = TextAlignmentOptions.Center;
                Fixed("DifficultyHeading", settings, 64, 139, 1728, 54, 30, "UI_SCENARIO_DIFFICULTY").alignment = TextAlignmentOptions.Center;
                var difficultyButtons = new Button[3];
                var difficultyLabels = new TMP_Text[3];
                for (int index = 0; index < 3; index += 1)
                {
                    difficultyButtons[index] = Button("DifficultyCard_" + index, settings, 64 + index * 583, 244, 560, 360, false);
                    difficultyLabels[index] = Text("Label", difficultyButtons[index].transform, 30, 30, 500, 300, 28,
                        database.DifficultyDefinitions[index].DisplayName.Get(false) + "\n\n" + database.DifficultyDefinitions[index].Description.Get(false));
                }
                settings.gameObject.SetActive(false);
                Rule(panel, 40, 985, 1840);
                var resume = Button("ContinueCampaignButton", panel, 40, 1000, 280, 64, false);
                Fixed("Label", resume.transform, 10, 6, 260, 52, 26, "UI_SCENARIO_CONTINUE").alignment = TextAlignmentOptions.Center;
                var back = Button("BackButton", panel, 40, 1000, 280, 64, false);
                Fixed("Label", back.transform, 10, 6, 260, 52, 26, "UI_SCENARIO_BACK").alignment = TextAlignmentOptions.Center;
                back.gameObject.SetActive(false);
                var next = Button("NewCampaignButton", panel, 1560, 1000, 320, 64, true);
                var nextLabel = Text("Label", next.transform, 10, 6, 300, 52, 28, database.GetText("UI_SCENARIO_NEXT"));
                nextLabel.alignment = TextAlignmentOptions.Center;
                var hint = Text("FooterHint", panel, 370, 1013, 1130, 40, 22, database.GetText("UI_SCENARIO_NEXT_HINT"));
                hint.alignment = TextAlignmentOptions.Center;
                Set(serialized, "database", database);
                Set(serialized, "scenarioPage", page.gameObject);
                Set(serialized, "settingsPage", settings.gameObject);
                Set(serialized, "backButton", back);
                Set(serialized, "pageTitle", title);
                Set(serialized, "pageSubtitle", subtitle);
                Set(serialized, "footerHint", hint);
                Set(serialized, "nextLabel", nextLabel);
                Set(serialized, "newCampaignButton", next);
                Set(serialized, "continueCampaignButton", resume);
                Set(serialized, "detailArtwork", artwork);
                Set(serialized, "detailTitle", detailTitle);
                Set(serialized, "detailSubtitle", detailSubtitle);
                Set(serialized, "detailStory", story);
                Set(serialized, "detailFaction", faction);
                Set(serialized, "detailProtagonist", protagonist);
                Set(serialized, "detailCastle", castle);
                Set(serialized, "detailObjective", objective);
                Set(serialized, "settingsSummary", summary);
                Array(serialized, "variantButtons", buttons);
                Array(serialized, "variantLabels", labels);
                Array(serialized, "variantArtwork", images);
                Array(serialized, "variantSubtitles", subtitles);
                Array(serialized, "variantBadges", badges);
                Array(serialized, "difficultyButtons", difficultyButtons);
                Array(serialized, "difficultyLabels", difficultyLabels);
                Array(serialized, "fixedLabels", fixedLabels.ToArray());
                var uids = serialized.FindProperty("fixedLabelUids");
                uids.arraySize = fixedUids.Count;
                for (int index = 0; index < fixedUids.Count; index += 1)
                {
                    uids.GetArrayElementAtIndex(index).stringValue = fixedUids[index];
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("시나리오 선택 화면 프리팹 저장 완료");
        }

        // 시작 조건의 표시 문구와 삽화만 저장하고 캠페인 밸런스는 보존합니다.
        private static void SeedPresentation()
        {
            var so = new SerializedObject(database);
            string[,] rows =
            {
                {"TITLE", "시나리오 선택", "Select Scenario"},
                {"SUBTITLE", "당신의 이야기가 시작될 무대를 선택하세요", "Choose where your story begins"},
                {"LIST", "시나리오", "Scenarios"},
                {"SETTINGS", "시작 설정", "Campaign Settings"},
                {"SETTINGS_HINT", "선택한 시나리오의 난이도를 설정하세요", "Choose a difficulty for your scenario"},
                {"NEXT_HINT", "선택한 시나리오의 설정으로 이동합니다", "Continue to scenario settings"},
                {"START_HINT", "선택한 시나리오와 난이도로 새 캠페인을 시작합니다", "Begin a new campaign with these settings"},
                {"NEXT", "다음  →", "Next  →"},
                {"START", "게임 시작  →", "Start Campaign  →"},
                {"CONTINUE", "이어하기", "Continue"},
                {"BACK", "←  이전", "←  Back"},
                {"DIFFICULTY", "난이도 선택", "Select Difficulty"},
                {"FACTION", "시작 세력", "Starting Faction"},
                {"PROTAGONIST", "주인공", "Protagonist"},
                {"CASTLE", "시작 거점", "Starting Castle"},
                {"OBJECTIVE", "승리 목표", "Victory Goal"},
                {"ARES_TITLE", "황혼의 귀환", "Return of Twilight"},
                {"ARES_SUBTITLE", "키리엔의 림가르드 재건", "Kyrien rebuilds Rimgard"},
                {"ARES_STORY", "발도르의 장군으로 자란 키리엔.\n잃어버린 이름을 되찾고 림가르드의 재건을 시작합니다.", "Raised as a general of Valdor, Kyrien reclaims his lost name\nand begins the restoration of Rimgard."},
                {"ARES_OBJECTIVE", "발도르의 모든 성 점령", "Capture all castles held by Valdor"},
                {"ARES_PROTAGONIST", "키리엔", "Kyrien"},
                {"FREE_TITLE", "자유 정복", "Free Conquest"},
                {"FREE_SUBTITLE", "나만의 대륙 통일", "Unite the continent your way"},
                {"FREE_STORY", "각 세력이 기본 성 배치에서 시작합니다.\n림가르드의 영지를 넓히고 대륙 통일을 이루세요.", "Each faction begins with its standard territories.\nExpand Rimgard and unite the continent."},
                {"FREE_OBJECTIVE", "대륙의 모든 성 점령", "Capture every castle on the continent"},
                {"FREE_PROTAGONIST", "림가르드 세력", "Rimgard"}
            };
            var strings = so.FindProperty("uiStrings");
            for (int row = 0; row < rows.GetLength(0); row += 1)
            {
                string uid = "UI_SCENARIO_" + rows[row, 0];
                SerializedProperty item = null;
                for (int index = 0; index < strings.arraySize; index += 1)
                {
                    var candidate = strings.GetArrayElementAtIndex(index);
                    if (candidate.FindPropertyRelative("uid").stringValue == uid)
                    {
                        item = candidate;
                        break;
                    }
                }
                if (item == null)
                {
                    strings.arraySize += 1;
                    item = strings.GetArrayElementAtIndex(strings.arraySize - 1);
                }
                item.FindPropertyRelative("uid").stringValue = uid;
                item.FindPropertyRelative("korean").stringValue = rows[row, 1];
                item.FindPropertyRelative("english").stringValue = rows[row, 2];
            }
            var variants = so.FindProperty("campaignVariants");
            for (int index = 0; index < 2; index += 1)
            {
                var variant = variants.GetArrayElementAtIndex(index);
                string prefix = index == 0 ? "UI_SCENARIO_ARES_" : "UI_SCENARIO_FREE_";
                variant.FindPropertyRelative("selectionArtwork").objectReferenceValue = Art(index);
                variant.FindPropertyRelative("selectionTitleUid").stringValue = prefix + "TITLE";
                variant.FindPropertyRelative("selectionSubtitleUid").stringValue = prefix + "SUBTITLE";
                variant.FindPropertyRelative("selectionStoryUid").stringValue = prefix + "STORY";
                variant.FindPropertyRelative("selectionObjectiveUid").stringValue = prefix + "OBJECTIVE";
                variant.FindPropertyRelative("selectionProtagonistUid").stringValue = prefix + "PROTAGONIST";
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // 새 삽화를 개별 UI 스프라이트로 가져옵니다.
        private static void ImportArt(string name)
        {
            string path = ArtPath + name;
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        // 시나리오별 전용 삽화를 가져옵니다.
        private static Sprite Art(int index)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath + (index == 0 ? "Scenario_Ares_V1.png" : "Scenario_Free_V1.png"));
        }

        // 기준 해상도의 좌상단 좌표로 편집 가능한 사각 영역을 만듭니다.
        private static RectTransform Rect(string name, Transform parent, float x, float y, float width, float height)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        // 배경만 화면 전체에 맞추어 늘립니다.
        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        // 삽화의 원래 비율을 보존하고 영역 밖 부분만 편집 시점 마스크로 자릅니다.
        private static Image Artwork(string name, Transform parent, float x, float y, float w, float h, Sprite sprite)
        {
            var viewport = Rect(name + "Viewport", parent, x, y, w, h);
            viewport.gameObject.AddComponent<RectMask2D>();
            return Image(name, viewport, 0, 0, w, w * sprite.rect.height / sprite.rect.width, sprite, Color.white);
        }

        // 에디터에서 이미지 슬롯을 만들고 장식의 입력 차단을 끕니다.
        private static Image Image(string name, Transform parent, float x, float y, float w, float h, Sprite sprite, Color color)
        {
            var image = Rect(name, parent, x, y, w, h).gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        // 기존 은색 장식 프레임을 재사용하여 섹션을 만듭니다.
        private static void Frame(string name, Transform parent, float x, float y, float w, float h)
        {
            var image = Image(name, parent, x, y, w, h,
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/MonthlyReport/Widgets/monthly_report_section_panel_v1.png"), Color.white);
            image.type = UnityEngine.UI.Image.Type.Sliced;
        }

        // 구역 사이의 얇은 은색 구분선을 저장합니다.
        private static void Rule(Transform parent, float x, float y, float width)
        {
            Image("Divider", parent, x, y, width, 1, null, new Color32(112, 136, 156, 255));
        }

        // 한글 폰트를 사용하는 독립 텍스트 요소를 저장합니다.
        private static TMP_Text Text(string name, Transform parent, float x, float y, float w, float h, float size, string value)
        {
            var text = Rect(name, parent, x, y, w, h).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = size;
            text.text = value;
            text.color = new Color32(232, 239, 245, 255);
            text.raycastTarget = false;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.enableAutoSizing = true;
            text.fontSizeMin = size * .8f;
            text.fontSizeMax = size;
            return text;
        }

        // 고정 문구와 UID의 연결을 저장합니다.
        private static TMP_Text Fixed(string name, Transform parent, float x, float y, float w, float h, float size, string uid)
        {
            TMP_Text text = Text(name, parent, x, y, w, h, size, database.GetText(uid));
            fixedLabels.Add(text);
            fixedUids.Add(uid);
            return text;
        }

        // 기존 버튼 스프라이트로 실제 클릭 가능한 버튼을 제작합니다.
        private static Button Button(string name, Transform parent, float x, float y, float w, float h, bool primary)
        {
            var image = Image(name, parent, x, y, w, h,
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/UI/Generated/button_" + (primary ? "primary" : "normal") + ".png"), Color.white);
            image.type = UnityEngine.UI.Image.Type.Sliced;
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            return button;
        }

        // 시작 조건을 아이콘·항목명·값의 독립 요소로 제작합니다.
        private static TMP_Text Info(Transform parent, string name, float x, string uid, string value, string iconPath)
        {
            Frame(name + "Panel", parent, x, 580, 400, 94);
            var icon = Image(name + "Icon", parent, x + 18, 596, 62, 62, AssetDatabase.LoadAssetAtPath<Sprite>(iconPath), Color.white);
            icon.preserveAspect = true;
            Fixed(name + "Heading", parent, x + 100, 590, 280, 32, 21, uid);
            return Text(name + "Value", parent, x + 100, 628, 280, 36, 26, value);
        }

        // 단일 직렬화 참조를 설정합니다.
        private static void Set(SerializedObject target, string name, Object value)
        {
            target.FindProperty(name).objectReferenceValue = value;
        }

        // 동일 역할의 요소 참조 배열을 저장합니다.
        private static void Array<T>(SerializedObject target, string name, T[] values) where T : Object
        {
            var array = target.FindProperty(name);
            array.arraySize = values.Length;
            for (int index = 0; index < values.Length; index += 1)
            {
                array.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            }
        }
    }
}

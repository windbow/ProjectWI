using ProjectWI.Administration;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectWI.EditorTools
{
    public static class WIAdministrationMonthlyReportPrefabUtility
    {
        private const string PrefabPath = "Assets/Prefabs/Administration/WIAdministrationMonthlyReportUGUI.prefab";
        private const string WidgetRoot = "Assets/Art/UI/MonthlyReport/Widgets/";
        private const string ArtRoot = "Assets/Art/UI/MonthlyReport/";
        private const string GeneratedRoot = "Assets/Resources/UI/Generated/";

        // 월간 보고 프리팹을 카드 위젯형 고정 레이아웃으로 다시 구성합니다.
        [MenuItem("WI/UI/Rebuild Monthly Report Cards")]
        public static void Rebuild()
        {
            ConfigureImports();
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                WIAdministrationMonthlyReportUGUIController controller =
                    root.GetComponent<WIAdministrationMonthlyReportUGUIController>();
                WIAdministrationModalUGUIController modal =
                    root.GetComponent<WIAdministrationModalUGUIController>();
                RectTransform panel = Find(root.transform, "ModalPanel") as RectTransform;
                RectTransform header = Find(root.transform, "Header") as RectTransform;
                RectTransform close = Find(root.transform, "CloseButton") as RectTransform;
                TMP_Text style = Find(root.transform, "Title").GetComponent<TMP_Text>();

                for (int index = panel.childCount - 1; index >= 0; index -= 1)
                {
                    Transform child = panel.GetChild(index);
                    if (child != header && child != close)
                    {
                        Object.DestroyImmediate(child.gameObject);
                    }
                }

                ConfigureRect(panel, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1800f, 970f));
                Image panelImage = panel.GetComponent<Image>();
                panelImage.sprite = Sprite("monthly_report_modal_frame_v1.png");
                panelImage.type = Image.Type.Sliced;
                panelImage.color = Color.white;
                ConfigureRect(header, new Vector2(0.5f, 1f), new Vector2(0f, -46f), new Vector2(1100f, 70f));
                header.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
                TMP_Text title = Find(header, "Title").GetComponent<TMP_Text>();
                title.fontSize = 34f;
                title.color = new Color32(232, 232, 225, 255);
                AddTitleOrnaments(header, style);
                ConfigureRect(close, new Vector2(1f, 1f), new Vector2(-48f, -48f), new Vector2(58f, 58f));
                close.GetComponent<Image>().sprite = Sprite("monthly_report_close_button_v1.png");
                close.GetComponent<Image>().type = Image.Type.Simple;

                CreateSectionBackground(panel, "LeftSection", new Vector2(-480f, -502f), new Vector2(790f, 708f));
                CreateSectionBackground(panel, "RightSection", new Vector2(440f, -502f), new Vector2(850f, 708f));
                TMP_Text[] summaries = CreateSummaryRow(panel, style);
                TMP_Text[] resources = CreateResourceRow(panel, style);
                CreateSectionTitle(panel, style, "TerritoryTitle", "▣  영지 운영", new Vector2(-480f, -280f), 760f);
                GameObject[] operationCards = new GameObject[4];
                TMP_Text[] operationLabels = new TMP_Text[4];
                CreateOperationCards(panel, style, operationCards, operationLabels);
                CreateSectionTitle(panel, style, "NewsTitle", "◆  주요 소식", new Vector2(-480f, -636f), 760f);
                GameObject[] newsCards = new GameObject[2];
                TMP_Text[] newsLabels = new TMP_Text[2];
                CreateNewsCards(panel, style, newsCards, newsLabels);
                CreateSectionTitle(panel, style, "DecisionTitle", "결정 필요", new Vector2(440f, -156f), 820f);

                Button[] actions = new Button[3];
                TMP_Text[] categories = new TMP_Text[3];
                TMP_Text[] actionTitles = new TMP_Text[3];
                TMP_Text[] details = new TMP_Text[3];
                TMP_Text[] urgency = new TMP_Text[3];
                Image[] thumbnails = new Image[3];
                CreateDecisionCards(panel, style, actions, categories, actionTitles, details, urgency, thumbnails);
                CreateFilters(panel, style, out Button[] filters, out TMP_Text[] filterLabels);
                CreatePager(panel, style, out Button previous, out Button next, out TMP_Text page);
                Button confirm = CreateConfirmButton(panel, style);

                SerializedObject serialized = new SerializedObject(controller);
                serialized.FindProperty("modal").objectReferenceValue = modal;
                SetArray(serialized.FindProperty("summaryLabels"), summaries);
                SetArray(serialized.FindProperty("resourceLabels"), resources);
                SetArray(serialized.FindProperty("operationCards"), operationCards);
                SetArray(serialized.FindProperty("operationLabels"), operationLabels);
                SetArray(serialized.FindProperty("newsCards"), newsCards);
                SetArray(serialized.FindProperty("newsLabels"), newsLabels);
                SetArray(serialized.FindProperty("actionButtons"), actions);
                SetArray(serialized.FindProperty("actionCategoryLabels"), categories);
                SetArray(serialized.FindProperty("actionTitleLabels"), actionTitles);
                SetArray(serialized.FindProperty("actionDetailLabels"), details);
                SetArray(serialized.FindProperty("actionUrgencyLabels"), urgency);
                SetArray(serialized.FindProperty("actionThumbnails"), thumbnails);
                serialized.FindProperty("occupationSprite").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot + "monthly_event_occupation_governance_v1.png");
                serialized.FindProperty("recruitmentSprite").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot + "monthly_event_wandering_scholar_v1.png");
                serialized.FindProperty("relationshipSprite").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot + "monthly_event_relationship_commanders_v1.png");
                serialized.FindProperty("militarySprite").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot + "monthly_news_enemy_scouts_v1.png");
                serialized.FindProperty("defaultActionSprite").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot + "monthly_news_wall_reinforced_v1.png");
                SetArray(serialized.FindProperty("filterButtons"), filters);
                SetArray(serialized.FindProperty("filterLabels"), filterLabels);
                serialized.FindProperty("filterNormalSprite").objectReferenceValue =
                    Sprite("monthly_report_filter_normal_v1.png");
                serialized.FindProperty("filterSelectedSprite").objectReferenceValue =
                    Sprite("monthly_report_filter_selected_v1.png");
                serialized.FindProperty("confirmButton").objectReferenceValue = confirm;
                serialized.FindProperty("previousButton").objectReferenceValue = previous;
                serialized.FindProperty("nextButton").objectReferenceValue = next;
                serialized.FindProperty("pageLabel").objectReferenceValue = page;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                WIUIAuditRepairUtility.RepairCommon(root);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log("월간 보고 UGUI를 카드 위젯 시안 구조로 갱신했습니다.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // 좌우 정보 영역의 고정 테두리 패널을 생성합니다.
        private static void CreateSectionBackground(Transform parent, string name, Vector2 position, Vector2 size)
        {
            GameObject section = CreateImage(name, parent, Sprite("monthly_report_section_panel_v1.png"), Image.Type.Sliced);
            ConfigureRect(section.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), position, size);
            section.transform.SetAsFirstSibling();
        }

        // 상단의 영지·인물·전투단·질서·연구 요약 칩을 생성합니다.
        private static TMP_Text[] CreateSummaryRow(Transform parent, TMP_Text style)
        {
            TMP_Text[] result = new TMP_Text[5];
            string[] values = { "영지 수  0", "인물  0", "전투단  0", "평균 질서  0", "연구  대기" };
            for (int index = 0; index < result.Length; index += 1)
            {
                GameObject card = CreateImage("Summary-" + index, parent,
                    Sprite("monthly_report_summary_chip_v1.png"), Image.Type.Sliced);
                ConfigureRect(card.GetComponent<RectTransform>(), new Vector2(0.5f, 1f),
                    new Vector2(-584f + index * 292f, -112f), new Vector2(270f, 52f));
                result[index] = CreateText("Label", card.transform, style, values[index], 18f,
                    TextAlignmentOptions.Center);
                Stretch(result[index].rectTransform, new Vector4(12f, 4f, 12f, 4f));
            }
            return result;
        }

        // 금화·마나·영향력 자원 카드를 생성합니다.
        private static TMP_Text[] CreateResourceRow(Transform parent, TMP_Text style)
        {
            TMP_Text[] result = new TMP_Text[3];
            string[] values = { "금화\n+0", "마나\n+0", "영향력\n+0" };
            Sprite[] icons = {
                AssetDatabase.LoadAssetAtPath<Sprite>(GeneratedRoot + "hud_flat_gold.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>(GeneratedRoot + "hud_flat_mana.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>(GeneratedRoot + "hud_flat_influence.png") };
            for (int index = 0; index < result.Length; index += 1)
            {
                GameObject card = CreateImage("Resource-" + index, parent,
                    Sprite("monthly_report_resource_card_v1.png"), Image.Type.Sliced);
                ConfigureRect(card.GetComponent<RectTransform>(), new Vector2(0.5f, 1f),
                    new Vector2(-716f + index * 238f, -212f), new Vector2(220f, 112f));
                GameObject icon = CreateImage("Icon", card.transform, icons[index], Image.Type.Simple);
                ConfigureRect(icon.GetComponent<RectTransform>(), new Vector2(0f, 0.5f),
                    new Vector2(49f, 0f), new Vector2(70f, 70f));
                icon.GetComponent<Image>().preserveAspect = true;
                result[index] = CreateText("Label", card.transform, style, values[index], 22f,
                    TextAlignmentOptions.MidlineLeft);
                ConfigureRect(result[index].rectTransform, new Vector2(1f, 0.5f),
                    new Vector2(-73f, 0f), new Vector2(118f, 82f));
            }
            return result;
        }

        // 두 줄 두 칸의 영지 운영 결과 카드를 생성합니다.
        private static void CreateOperationCards(Transform parent, TMP_Text style,
            GameObject[] cards, TMP_Text[] labels)
        {
            Sprite[] icons = {
                AssetDatabase.LoadAssetAtPath<Sprite>(GeneratedRoot + "castle_stat_prosperity.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>(GeneratedRoot + "castle_stat_defense.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>(GeneratedRoot + "castle_stat_stability.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>(GeneratedRoot + "castle_stat_technology.png") };
            for (int index = 0; index < cards.Length; index += 1)
            {
                int column = index % 2;
                int row = index / 2;
                cards[index] = CreateImage("Operation-" + index, parent,
                    Sprite("monthly_report_resource_card_v1.png"), Image.Type.Sliced);
                ConfigureRect(cards[index].GetComponent<RectTransform>(), new Vector2(0.5f, 1f),
                    new Vector2(-672f + column * 384f, -372f - row * 130f), new Vector2(360f, 112f));
                GameObject icon = CreateImage("Icon", cards[index].transform, icons[index], Image.Type.Simple);
                ConfigureRect(icon.GetComponent<RectTransform>(), new Vector2(0f, 0.5f),
                    new Vector2(52f, 0f), new Vector2(76f, 76f));
                icon.GetComponent<Image>().preserveAspect = true;
                labels[index] = CreateText("Label", cards[index].transform, style,
                    "영지 운영 결과", 17f, TextAlignmentOptions.MidlineLeft);
                ConfigureRect(labels[index].rectTransform, new Vector2(1f, 0.5f),
                    new Vector2(-132f, 0f), new Vector2(238f, 88f));
                labels[index].textWrappingMode = TextWrappingModes.Normal;
            }
        }

        // 주요 소식 두 건을 삽화가 포함된 가로 카드로 생성합니다.
        private static void CreateNewsCards(Transform parent, TMP_Text style,
            GameObject[] cards, TMP_Text[] labels)
        {
            Sprite[] images = {
                AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot + "monthly_news_enemy_scouts_v1.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot + "monthly_news_wall_reinforced_v1.png") };
            for (int index = 0; index < cards.Length; index += 1)
            {
                cards[index] = CreateImage("News-" + index, parent,
                    Sprite("monthly_report_resource_card_v1.png"), Image.Type.Sliced);
                ConfigureRect(cards[index].GetComponent<RectTransform>(), new Vector2(0.5f, 1f),
                    new Vector2(-672f + index * 384f, -742f), new Vector2(360f, 150f));
                GameObject picture = CreateImage("Picture", cards[index].transform, images[index], Image.Type.Simple);
                ConfigureRect(picture.GetComponent<RectTransform>(), new Vector2(0f, 0.5f),
                    new Vector2(82f, 0f), new Vector2(140f, 122f));
                picture.GetComponent<Image>().preserveAspect = false;
                labels[index] = CreateText("Label", cards[index].transform, style,
                    "주요 소식", 18f, TextAlignmentOptions.MidlineLeft);
                ConfigureRect(labels[index].rectTransform, new Vector2(1f, 0.5f),
                    new Vector2(-102f, 0f), new Vector2(174f, 112f));
                labels[index].textWrappingMode = TextWrappingModes.Normal;
            }
        }

        // 플레이어가 처리할 선택 사건 세 건을 큰 행동 카드로 생성합니다.
        private static void CreateDecisionCards(Transform parent, TMP_Text style, Button[] buttons,
            TMP_Text[] categories, TMP_Text[] titles, TMP_Text[] details, TMP_Text[] urgency, Image[] thumbnails)
        {
            for (int index = 0; index < buttons.Length; index += 1)
            {
                GameObject card = CreateImage("Decision-" + index, parent,
                    Sprite("monthly_report_decision_card_v1.png"), Image.Type.Sliced);
                ConfigureRect(card.GetComponent<RectTransform>(), new Vector2(0.5f, 1f),
                    new Vector2(440f, -280f - index * 205f), new Vector2(820f, 184f));
                buttons[index] = card.AddComponent<Button>();
                buttons[index].targetGraphic = card.GetComponent<Image>();
                GameObject picture = CreateImage("Thumbnail", card.transform, null, Image.Type.Simple);
                ConfigureRect(picture.GetComponent<RectTransform>(), new Vector2(0f, 0.5f),
                    new Vector2(90f, 0f), new Vector2(154f, 150f));
                thumbnails[index] = picture.GetComponent<Image>();
                thumbnails[index].preserveAspect = false;
                GameObject badge = CreateImage("CategoryBadge", card.transform,
                    Sprite("monthly_report_badge_background_v1.png"), Image.Type.Sliced);
                ConfigureRect(badge.GetComponent<RectTransform>(), new Vector2(0f, 1f),
                    new Vector2(220f, -34f), new Vector2(116f, 36f));
                categories[index] = CreateText("Label", badge.transform, style, "분류", 15f,
                    TextAlignmentOptions.Center);
                Stretch(categories[index].rectTransform, new Vector4(8f, 2f, 8f, 2f));
                titles[index] = CreateText("Title", card.transform, style, "결정 제목", 23f,
                    TextAlignmentOptions.MidlineLeft);
                ConfigureRect(titles[index].rectTransform, new Vector2(0f, 1f),
                    new Vector2(390f, -78f), new Vector2(440f, 42f));
                details[index] = CreateText("Detail", card.transform, style, "결정의 예상 결과", 17f,
                    TextAlignmentOptions.TopLeft);
                ConfigureRect(details[index].rectTransform, new Vector2(0f, 1f),
                    new Vector2(390f, -128f), new Vector2(440f, 52f));
                urgency[index] = CreateText("Urgency", card.transform, style, "! 확인", 16f,
                    TextAlignmentOptions.Center);
                ConfigureRect(urgency[index].rectTransform, new Vector2(1f, 1f),
                    new Vector2(-72f, -34f), new Vector2(110f, 32f));
                GameObject actionVisual = CreateImage("ActionVisual", card.transform,
                    Sprite("monthly_report_primary_button_v1.png"), Image.Type.Sliced);
                ConfigureRect(actionVisual.GetComponent<RectTransform>(), new Vector2(1f, 0.5f),
                    new Vector2(-94f, 20f), new Vector2(156f, 54f));
                TMP_Text actionLabel = CreateText("Label", actionVisual.transform, style, "결정하기", 17f,
                    TextAlignmentOptions.Center);
                Stretch(actionLabel.rectTransform, new Vector4(8f, 2f, 8f, 2f));
            }
        }

        // 결정 카드가 네 건 이상일 때 사용할 페이지 이동 버튼을 생성합니다.
        private static void CreatePager(Transform parent, TMP_Text style,
            out Button previous, out Button next, out TMP_Text page)
        {
            previous = CreateButton("PreviousButton", parent, style, "◀", new Vector2(260f, 38f));
            next = CreateButton("NextButton", parent, style, "▶", new Vector2(620f, 38f));
            page = CreateText("PageLabel", parent, style, "처리할 사건 없음", 16f,
                TextAlignmentOptions.Center);
            ConfigureRect(page.rectTransform, new Vector2(0.5f, 0f), new Vector2(440f, 38f), new Vector2(230f, 42f));
        }

        // 전체·영지·군사·인물 결정 필터를 고정 버튼으로 생성합니다.
        private static void CreateFilters(Transform parent, TMP_Text style,
            out Button[] buttons, out TMP_Text[] labels)
        {
            buttons = new Button[4];
            labels = new TMP_Text[4];
            string[] values = { "전체", "영지", "군사", "인물" };
            for (int index = 0; index < buttons.Length; index += 1)
            {
                GameObject item = CreateImage("Filter-" + index, parent,
                    Sprite(index == 0 ? "monthly_report_filter_selected_v1.png" : "monthly_report_filter_normal_v1.png"),
                    Image.Type.Sliced);
                ConfigureRect(item.GetComponent<RectTransform>(), new Vector2(0.5f, 0f),
                    new Vector2(-720f + index * 150f, 38f), new Vector2(142f, 44f));
                buttons[index] = item.AddComponent<Button>();
                buttons[index].targetGraphic = item.GetComponent<Image>();
                labels[index] = CreateText("Label", item.transform, style, values[index], 17f,
                    TextAlignmentOptions.Center);
                Stretch(labels[index].rectTransform, new Vector4(8f, 2f, 8f, 2f));
            }
        }

        // 보고 확인 후 턴 결과 흐름을 이어가는 기본 버튼을 생성합니다.
        private static Button CreateConfirmButton(Transform parent, TMP_Text style)
        {
            GameObject item = CreateImage("ConfirmButton", parent,
                Sprite("monthly_report_primary_button_v1.png"), Image.Type.Sliced);
            ConfigureRect(item.GetComponent<RectTransform>(), new Vector2(0.5f, 0f),
                new Vector2(760f, 38f), new Vector2(220f, 58f));
            Button button = item.AddComponent<Button>();
            button.targetGraphic = item.GetComponent<Image>();
            TMP_Text label = CreateText("Label", item.transform, style, "보고 확인", 19f,
                TextAlignmentOptions.Center);
            Stretch(label.rectTransform, new Vector4(10f, 4f, 10f, 4f));
            return button;
        }

        // 섹션 제목을 고정 위치에 생성합니다.
        private static void CreateSectionTitle(Transform parent, TMP_Text style, string name,
            string value, Vector2 position, float width)
        {
            TMP_Text text = CreateText(name, parent, style, value, 23f, TextAlignmentOptions.MidlineLeft);
            ConfigureRect(text.rectTransform, new Vector2(0.5f, 1f), position, new Vector2(width, 42f));
        }

        // 제목 좌우에 같은 장식 Sprite를 반전 배치합니다.
        private static void AddTitleOrnaments(Transform header, TMP_Text style)
        {
            Sprite ornament = Sprite("monthly_report_title_ornament_v1.png");
            for (int index = 0; index < 2; index += 1)
            {
                GameObject item = CreateImage("TitleOrnament-" + index, header, ornament, Image.Type.Simple);
                ConfigureRect(item.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f),
                    new Vector2(index == 0 ? -390f : 390f, 0f), new Vector2(230f, 36f));
                item.GetComponent<RectTransform>().localScale = new Vector3(index == 0 ? 1f : -1f, 1f, 1f);
            }
        }

        // 페이지 이동용 소형 버튼을 생성합니다.
        private static Button CreateButton(string name, Transform parent, TMP_Text style,
            string value, Vector2 position)
        {
            GameObject item = CreateImage(name, parent, Sprite("monthly_report_summary_chip_v1.png"), Image.Type.Sliced);
            ConfigureRect(item.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), position, new Vector2(86f, 42f));
            Button button = item.AddComponent<Button>();
            button.targetGraphic = item.GetComponent<Image>();
            TMP_Text label = CreateText("Label", item.transform, style, value, 18f, TextAlignmentOptions.Center);
            Stretch(label.rectTransform, Vector4.zero);
            return button;
        }

        // 월간 보고 전용 Sprite의 9-Slice와 투명 가져오기 설정을 적용합니다.
        private static void ConfigureImports()
        {
            ConfigureSprite("monthly_report_modal_frame_v1.png", new Vector4(52f, 52f, 52f, 52f));
            ConfigureSprite("monthly_report_section_panel_v1.png", new Vector4(32f, 32f, 32f, 32f));
            ConfigureSprite("monthly_report_summary_chip_v1.png", new Vector4(56f, 36f, 56f, 36f));
            ConfigureSprite("monthly_report_resource_card_v1.png", new Vector4(42f, 42f, 42f, 42f));
            ConfigureSprite("monthly_report_decision_card_v1.png", new Vector4(48f, 48f, 48f, 48f));
            ConfigureSprite("monthly_report_primary_button_v1.png", new Vector4(56f, 42f, 56f, 42f));
            ConfigureSprite("monthly_report_badge_background_v1.png", new Vector4(42f, 28f, 42f, 28f));
            ConfigureSprite("monthly_report_filter_normal_v1.png", new Vector4(48f, 36f, 48f, 36f));
            ConfigureSprite("monthly_report_filter_selected_v1.png", new Vector4(48f, 36f, 48f, 36f));
        }

        // 지정 Sprite를 단일 UI Sprite로 가져오고 테두리를 저장합니다.
        private static void ConfigureSprite(string file, Vector4 border)
        {
            string path = WidgetRoot + file;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteBorder = border;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        // 전용 위젯 Sprite를 불러옵니다.
        private static Sprite Sprite(string file) => AssetDatabase.LoadAssetAtPath<Sprite>(WidgetRoot + file);

        // 지정 이름의 자식 Transform을 재귀적으로 찾습니다.
        private static Transform Find(Transform root, string name)
        {
            if (root.name == name) return root;
            foreach (Transform child in root)
            {
                Transform found = Find(child, name);
                if (found != null) return found;
            }
            return null;
        }

        // Image가 포함된 고정 UGUI 오브젝트를 생성합니다.
        private static GameObject CreateImage(string name, Transform parent, Sprite sprite, Image.Type type)
        {
            GameObject result = new GameObject(name, typeof(RectTransform), typeof(Image));
            result.layer = parent.gameObject.layer;
            result.transform.SetParent(parent, false);
            Image image = result.GetComponent<Image>();
            image.sprite = sprite;
            image.type = type;
            image.color = Color.white;
            return result;
        }

        // 기준 TMP 문자의 폰트와 재질을 복사해 고정 문구를 생성합니다.
        private static TMP_Text CreateText(string name, Transform parent, TMP_Text style,
            string value, float size, TextAlignmentOptions alignment)
        {
            GameObject item = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            item.layer = parent.gameObject.layer;
            item.transform.SetParent(parent, false);
            TMP_Text text = item.GetComponent<TMP_Text>();
            text.font = style.font;
            text.fontSharedMaterial = style.fontSharedMaterial;
            text.color = new Color32(222, 225, 226, 255);
            text.fontSize = size;
            text.alignment = alignment;
            text.text = value;
            text.raycastTarget = false;
            return text;
        }

        // 직렬화 배열에 Unity 오브젝트 참조를 기록합니다.
        private static void SetArray<T>(SerializedProperty property, T[] values) where T : Object
        {
            property.arraySize = values.Length;
            for (int index = 0; index < values.Length; index += 1)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            }
        }

        // RectTransform의 중앙 기준 위치와 크기를 지정합니다.
        private static void ConfigureRect(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
        }

        // RectTransform을 부모 영역에 지정 여백으로 채웁니다.
        private static void Stretch(RectTransform rect, Vector4 margin)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(margin.x, margin.y);
            rect.offsetMax = new Vector2(-margin.z, -margin.w);
            rect.localScale = Vector3.one;
        }
    }
}

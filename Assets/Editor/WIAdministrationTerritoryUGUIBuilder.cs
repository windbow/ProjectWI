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
    public static class WIAdministrationTerritoryUGUIBuilder
    {
        private const string PrefabPath = "Assets/Prefabs/Administration/WIAdministrationTerritoryUGUI.prefab";
        private static TMP_FontAsset sansFont;
        private static TMP_FontAsset serifFont;

        // 시안 구조에 맞춘 고정 성 내정 UGUI 프리팹을 생성하고 MainScene에 배치합니다.
        [MenuItem("WI/UI/Build Administration Territory UGUI")]
        public static void Build()
        {
            ConfigureSlicedAsset("Assets/Resources/UI/Generated/territory_bottom_panel_v1.png",
                new Vector4(18f, 18f, 18f, 18f));
            ConfigureSlicedAsset("Assets/Resources/UI/Generated/strategy_command_button_v2.png",
                new Vector4(34f, 24f, 34f, 24f));
            ConfigureSlicedAsset("Assets/Resources/UI/Generated/territory_back_button_v1.png",
                new Vector4(12f, 10f, 12f, 10f));
            ConfigureSlicedAsset("Assets/Resources/UI/Generated/strategy_next_turn_button_v3.png",
                new Vector4(88f, 24f, 42f, 24f));
            sansFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/TMP/NotoSansCJKkr-Dynamic.asset");
            serifFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/TMP/NotoSerifCJKkr-Dynamic.asset");

            GameObject root = new GameObject("WIAdministrationTerritoryUGUI", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(WIAdministrationTerritoryUGUIController));
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject content = Panel(root.transform, "TerritoryContent", null, new Color32(5, 13, 18, 255),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image background = Image(content.transform, "CastleBackground", null, Color.white,
                new Vector2(0f, 0.10f), new Vector2(1f, 0.935f), Vector2.zero, Vector2.zero, UnityEngine.UI.Image.Type.Simple);
            background.preserveAspect = false;
            Image(content.transform, "CastleShade", null, new Color32(3, 10, 15, 44),
                new Vector2(0f, 0.10f), new Vector2(1f, 0.935f), Vector2.zero, Vector2.zero,
                UnityEngine.UI.Image.Type.Simple);

            TMP_Text faction;
            TMP_Text date;
            TMP_Text gold;
            TMP_Text mana;
            TMP_Text influence;
            List<Button> topCommands;
            Button topSystemButton;
            CreateTopHud(content.transform, out faction, out date, out gold, out mana, out influence,
                out topCommands, out topSystemButton);

            GameObject left = Panel(content.transform, "OverviewPanel", Sprite("right_panel_frame"), Color.white,
                new Vector2(0.008f, 0.16f), new Vector2(0.213f, 0.92f), Vector2.zero, Vector2.zero);
            Image crest = Image(left.transform, "CastleCrest", Sprite("strategy_avalon_crest_v1"), Color.white,
                new Vector2(0.045f, 0.84f), new Vector2(0.25f, 0.975f), Vector2.zero, Vector2.zero,
                UnityEngine.UI.Image.Type.Simple);
            crest.preserveAspect = true;
            TMP_Text title = Text(left.transform, "CastleTitle", "별빛 수도", 27f, TextAlignmentOptions.MidlineLeft,
                new Vector2(0.25f, 0.90f), new Vector2(0.95f, 0.98f), Vector2.zero, Vector2.zero, true);
            TMP_Text info = Text(left.transform, "CastleInfo", "수도 · 아발론 왕국", 14f,
                TextAlignmentOptions.MidlineLeft, new Vector2(0.25f, 0.84f), new Vector2(0.95f, 0.91f),
                Vector2.zero, Vector2.zero, false);
            Image governor = Image(left.transform, "GovernorPortrait", null, Color.white,
                new Vector2(0.045f, 0.51f), new Vector2(0.955f, 0.835f), Vector2.zero, Vector2.zero,
                UnityEngine.UI.Image.Type.Simple);
            governor.preserveAspect = false;
            TMP_Text governorName = Text(left.transform, "GovernorName", "영지관 미배치", 17f,
                TextAlignmentOptions.BottomRight, new Vector2(0.08f, 0.515f), new Vector2(0.92f, 0.60f),
                Vector2.zero, Vector2.zero, true);
            governorName.textWrappingMode = TextWrappingModes.NoWrap;

            TMP_Text prosperity = Stat(left.transform, "Prosperity", "번영 0", 0.445f, "castle_stat_prosperity");
            TMP_Text technology = Stat(left.transform, "Technology", "기술 0", 0.385f, "castle_stat_technology");
            TMP_Text stability = Stat(left.transform, "Stability", "질서 0", 0.325f, "castle_stat_stability");
            TMP_Text defense = Stat(left.transform, "Defense", "방어 0", 0.265f, "castle_stat_defense");
            Text(left.transform, "IncomeHeading", "현재 수입 (월)", 14f, TextAlignmentOptions.MidlineLeft,
                new Vector2(0.06f, 0.215f), new Vector2(0.94f, 0.265f), Vector2.zero, Vector2.zero, false);
            TMP_Text income = Text(left.transform, "Income", "금화 0 · 마나 0 · 영향력 0", 15f,
                TextAlignmentOptions.Center, new Vector2(0.06f, 0.165f), new Vector2(0.94f, 0.22f),
                Vector2.zero, Vector2.zero, false);
            GameObject projectCard = Panel(left.transform, "ProjectCard", Sprite("button_flat_normal"), Color.white,
                new Vector2(0.055f, 0.025f), new Vector2(0.945f, 0.155f), Vector2.zero, Vector2.zero);
            Text(projectCard.transform, "ProjectHeading", "진행 중 사업", 13f, TextAlignmentOptions.MidlineLeft,
                new Vector2(0.05f, 0.58f), new Vector2(0.95f, 0.96f), Vector2.zero, Vector2.zero, false);
            TMP_Text project = Text(projectCard.transform, "ProjectStatus", "이번 달 중점 미지정", 15f,
                TextAlignmentOptions.MidlineLeft, new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.62f),
                Vector2.zero, Vector2.zero, true);

            GameObject right = Panel(content.transform, "CommandPanel", Sprite("bg_type_d"), Color.white,
                new Vector2(0.825f, 0.20f), new Vector2(0.992f, 0.92f), Vector2.zero, Vector2.zero);
            Text(right.transform, "CommandHeading", "성 내정", 23f, TextAlignmentOptions.Center,
                new Vector2(0f, 0.91f), Vector2.one, new Vector2(18f, 0f), new Vector2(-18f, 0f), true);
            string[] commandLabels = {
                "중점 사업", "인사 배치", "인재 활동", "특화 시설",
                "기본 시설", "태수 위임", "진격 / 출정", "성 상세"
            };
            string[] commandIcons = {
                "castle_stat_stability", "icon_flat_heroes", "icon_flat_report", "icon_flat_research",
                "icon_flat_faction", "strategy_avalon_crest_v1", "icon_flat_military", "icon_flat_council"
            };
            WIAdministrationTerritoryCommand[] actions = {
                WIAdministrationTerritoryCommand.FocusProject, WIAdministrationTerritoryCommand.AssignHero,
                WIAdministrationTerritoryCommand.CharacterActivity, WIAdministrationTerritoryCommand.ChooseSpecialFacility,
                WIAdministrationTerritoryCommand.BasicFacility, WIAdministrationTerritoryCommand.Delegation,
                WIAdministrationTerritoryCommand.March, WIAdministrationTerritoryCommand.CastleRecord
            };
            List<Button> commands = new List<Button>();
            for (int index = 0; index < commandLabels.Length; index += 1)
            {
                float yMax = 0.88f - index * 0.105f;
                Sprite buttonSprite = index == 6 ? Sprite("button_flat_danger") : Sprite("strategy_command_button_v2");
                Button command = Button(right.transform, $"Command-{index}", commandLabels[index], buttonSprite,
                    new Vector2(0.055f, yMax - 0.078f), new Vector2(0.945f, yMax), Vector2.zero, Vector2.zero,
                    out TMP_Text label);
                label.alignment = TextAlignmentOptions.MidlineLeft;
                label.fontSize = 17f;
                label.rectTransform.offsetMin = new Vector2(62f, 2f);
                label.rectTransform.offsetMax = new Vector2(-12f, -2f);
                Image icon = Image(command.transform, "Icon", Sprite(commandIcons[index]), Color.white,
                    new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(17f, 13f), new Vector2(55f, -13f),
                    UnityEngine.UI.Image.Type.Simple);
                icon.preserveAspect = true;
                commands.Add(command);
            }

            Button back = Button(content.transform, "BackButton", "◀  대륙 지도", Sprite("territory_back_button_v1"),
                new Vector2(0.015f, 0.03f), new Vector2(0.17f, 0.12f), Vector2.zero, Vector2.zero, out TMP_Text backLabel);
            backLabel.fontSize = 20f;
            Button nextTurn = Button(content.transform, "NextTurnButton", "다음 턴   ›",
                Sprite("strategy_next_turn_button_v3"), new Vector2(0.825f, 0.025f), new Vector2(0.985f, 0.15f),
                Vector2.zero, Vector2.zero, out TMP_Text nextTurnLabel);
            nextTurnLabel.fontSize = 29f;
            nextTurnLabel.rectTransform.offsetMin = new Vector2(70f, 3f);
            Image nextTurnCrest = Image(nextTurn.transform, "Crest", Sprite("strategy_avalon_crest_v1"), Color.white,
                new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(42f, 8f), new Vector2(102f, -8f),
                UnityEngine.UI.Image.Type.Simple);
            nextTurnCrest.preserveAspect = true;

            GameObject bottom = Panel(content.transform, "BottomSummaryPanel", Sprite("territory_bottom_panel_v1"),
                Color.white, new Vector2(0.225f, 0.025f), new Vector2(0.815f, 0.32f), Vector2.zero, Vector2.zero);
            Text(bottom.transform, "HeroHeading", "주둔 영웅", 19f, TextAlignmentOptions.Center,
                new Vector2(0.02f, 0.83f), new Vector2(0.39f, 0.98f), Vector2.zero, Vector2.zero, true);
            Text(bottom.transform, "FacilityHeading", "특화 시설", 19f, TextAlignmentOptions.Center,
                new Vector2(0.40f, 0.83f), new Vector2(0.67f, 0.98f), Vector2.zero, Vector2.zero, true);
            Text(bottom.transform, "FocusHeading", "이번 달 중점", 19f, TextAlignmentOptions.Center,
                new Vector2(0.68f, 0.83f), new Vector2(0.98f, 0.98f), Vector2.zero, Vector2.zero, true);
            Divider(bottom.transform, 0.395f);
            Divider(bottom.transform, 0.675f);

            List<GameObject> heroSlots = new List<GameObject>();
            List<Image> heroImages = new List<Image>();
            List<TMP_Text> heroCaptions = new List<TMP_Text>();
            for (int index = 0; index < 4; index += 1)
            {
                float xMin = 0.02f + index * 0.092f;
                GameObject slot = Panel(bottom.transform, $"HeroSlot-{index}", Sprite("strategy_hero_card_v1"), Color.white,
                    new Vector2(xMin, 0.08f), new Vector2(xMin + 0.082f, 0.80f), Vector2.zero, Vector2.zero);
                Image portrait = Image(slot.transform, "Portrait", null, Color.white,
                    new Vector2(0.04f, 0.26f), new Vector2(0.96f, 0.96f), Vector2.zero, Vector2.zero,
                    UnityEngine.UI.Image.Type.Simple);
                portrait.preserveAspect = false;
                TMP_Text caption = Text(slot.transform, "Caption", "빈 슬롯", 12f, TextAlignmentOptions.Center,
                    new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.28f), Vector2.zero, Vector2.zero, false);
                heroSlots.Add(slot);
                heroImages.Add(portrait);
                heroCaptions.Add(caption);
            }

            List<GameObject> facilitySlots = new List<GameObject>();
            List<Image> facilityImages = new List<Image>();
            List<TMP_Text> facilityCaptions = new List<TMP_Text>();
            for (int index = 0; index < 2; index += 1)
            {
                float xMin = 0.415f + index * 0.125f;
                GameObject slot = Panel(bottom.transform, $"FacilitySlot-{index}", Sprite("strategy_hero_card_v1"), Color.white,
                    new Vector2(xMin, 0.08f), new Vector2(xMin + 0.112f, 0.80f), Vector2.zero, Vector2.zero);
                Image icon = Image(slot.transform, "Icon", null, Color.white,
                    new Vector2(0.08f, 0.28f), new Vector2(0.92f, 0.94f), Vector2.zero, Vector2.zero,
                    UnityEngine.UI.Image.Type.Simple);
                icon.preserveAspect = true;
                TMP_Text caption = Text(slot.transform, "Caption", "미지정", 12f, TextAlignmentOptions.Center,
                    new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.30f), Vector2.zero, Vector2.zero, false);
                facilitySlots.Add(slot);
                facilityImages.Add(icon);
                facilityCaptions.Add(caption);
            }
            GameObject focusCard = Panel(bottom.transform, "FocusCard", Sprite("button_flat_normal"), Color.white,
                new Vector2(0.695f, 0.10f), new Vector2(0.965f, 0.79f), Vector2.zero, Vector2.zero);
            TMP_Text bottomProject = Text(focusCard.transform, "FocusStatus", "중점 사업 미지정", 18f,
                TextAlignmentOptions.Center, new Vector2(0.06f, 0.12f), new Vector2(0.94f, 0.88f),
                Vector2.zero, Vector2.zero, true);

            WIAdministrationTerritoryUGUIController controller = root.GetComponent<WIAdministrationTerritoryUGUIController>();
            SerializedObject serialized = new SerializedObject(controller);
            SetObject(serialized, "contentRoot", content);
            SetObject(serialized, "backButton", back);
            SetObject(serialized, "nextTurnButton", nextTurn);
            SetObject(serialized, "factionLabel", faction);
            SetObject(serialized, "dateLabel", date);
            SetObject(serialized, "goldLabel", gold);
            SetObject(serialized, "manaLabel", mana);
            SetObject(serialized, "influenceLabel", influence);
            SetObject(serialized, "systemButton", topSystemButton);
            ButtonArray(serialized.FindProperty("topCommandButtons"), topCommands);
            WIAdministrationShortcutAction[] topActions = {
                WIAdministrationShortcutAction.MonthlyReport,
                WIAdministrationShortcutAction.Council,
                WIAdministrationShortcutAction.Research
            };
            SerializedProperty topActionProperty = serialized.FindProperty("topCommandActions");
            topActionProperty.arraySize = topActions.Length;
            for (int index = 0; index < topActions.Length; index += 1)
            {
                topActionProperty.GetArrayElementAtIndex(index).enumValueIndex = (int)topActions[index];
            }
            SetObject(serialized, "castleTitle", title);
            SetObject(serialized, "castleInfo", info);
            SetObject(serialized, "castleBackground", background);
            SetObject(serialized, "governorPortrait", governor);
            SetObject(serialized, "governorNameLabel", governorName);
            SetObject(serialized, "prosperityLabel", prosperity);
            SetObject(serialized, "technologyLabel", technology);
            SetObject(serialized, "stabilityLabel", stability);
            SetObject(serialized, "defenseLabel", defense);
            SetObject(serialized, "incomeLabel", income);
            SetObject(serialized, "projectStatusLabel", project);
            SetObject(serialized, "bottomProjectStatusLabel", bottomProject);
            Array(serialized.FindProperty("heroSlots"), heroSlots);
            ImageArray(serialized.FindProperty("heroImages"), heroImages);
            TextArray(serialized.FindProperty("heroCaptions"), heroCaptions);
            Array(serialized.FindProperty("facilitySlots"), facilitySlots);
            ImageArray(serialized.FindProperty("facilityImages"), facilityImages);
            TextArray(serialized.FindProperty("facilityCaptions"), facilityCaptions);
            ButtonArray(serialized.FindProperty("commandButtons"), commands);
            SerializedProperty actionProperty = serialized.FindProperty("commandActions");
            actionProperty.arraySize = actions.Length;
            for (int index = 0; index < actions.Length; index += 1)
            {
                actionProperty.GetArrayElementAtIndex(index).enumValueIndex = (int)actions[index];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            GameObject existing = GameObject.Find("WIAdministrationTerritoryUGUI");
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }
            WIAdministrationUGUISceneUtility.InstantiateUnderSceneRoot(prefab);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("시안형 행정 영지 상세 UGUI 프리팹을 생성하고 MainScene에 배치했습니다.");
        }

        // 월드 화면과 구조·크기·기능이 같은 상단 HUD를 생성합니다.
        private static void CreateTopHud(Transform parent, out TMP_Text faction, out TMP_Text date,
            out TMP_Text gold, out TMP_Text mana, out TMP_Text influence, out List<Button> topCommands,
            out Button topSystemButton)
        {
            GameObject top = Panel(parent, "TopHUD", null, new Color32(6, 13, 18, 255),
                new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -92f), Vector2.zero);
            Image(top.transform, "BottomMetalLine", null, new Color32(91, 91, 85, 255),
                new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 2f),
                UnityEngine.UI.Image.Type.Simple);
            Image crest = Image(top.transform, "FactionCrest", Sprite("strategy_avalon_crest_v1"), Color.white,
                new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(24f, 8f), new Vector2(88f, -8f),
                UnityEngine.UI.Image.Type.Simple);
            crest.preserveAspect = true;
            faction = Text(top.transform, "Faction", "아발론 왕국", 28f, TextAlignmentOptions.MidlineLeft,
                new Vector2(0f, 0f), new Vector2(0.18f, 1f), new Vector2(98f, 0f), new Vector2(-8f, 0f), true);
            date = Text(top.transform, "Date", "1년 03월", 22f, TextAlignmentOptions.Center,
                new Vector2(0.19f, 0f), new Vector2(0.31f, 1f), new Vector2(18f, 0f), Vector2.zero, false);
            TMP_Text turnDescription = Text(top.transform, "TurnDescription", string.Empty, 1f,
                TextAlignmentOptions.Center, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, false);
            turnDescription.gameObject.SetActive(false);
            gold = Text(top.transform, "Gold", "금화 1,240 (+320/월)", 19f, TextAlignmentOptions.Center,
                new Vector2(0.32f, 0f), new Vector2(0.47f, 1f), new Vector2(18f, 0f), Vector2.zero, false);
            mana = Text(top.transform, "Mana", "마나 360 (+90/월)", 19f, TextAlignmentOptions.Center,
                new Vector2(0.47f, 0f), new Vector2(0.61f, 1f), new Vector2(18f, 0f), Vector2.zero, false);
            influence = Text(top.transform, "Influence", "영향력 125 (+15/월)", 19f, TextAlignmentOptions.Center,
                new Vector2(0.61f, 0f), new Vector2(0.76f, 1f), new Vector2(18f, 0f), Vector2.zero, false);
            CreateHudIcon(top.transform, "DateIcon", "hud_flat_date", 0.205f);
            CreateHudIcon(top.transform, "GoldIcon", "hud_flat_gold", 0.335f);
            CreateHudIcon(top.transform, "ManaIcon", "hud_flat_mana", 0.485f);
            CreateHudIcon(top.transform, "InfluenceIcon", "hud_flat_influence", 0.625f);
            CreateTopSeparator(top.transform, "FactionSeparator", 0.185f);
            CreateTopSeparator(top.transform, "DateSeparator", 0.315f);
            CreateTopSeparator(top.transform, "GoldSeparator", 0.47f);
            CreateTopSeparator(top.transform, "ManaSeparator", 0.61f);
            CreateTopSeparator(top.transform, "InfluenceSeparator", 0.76f);

            string[] topIconNames = {
                "icon_flat_report", "icon_flat_faction", "icon_flat_research", "strategy_top_settings_v1"
            };
            for (int index = 0; index < topIconNames.Length; index += 1)
            {
                float topIconCenter = 0.845625f + index * 0.04125f;
                Image topIcon = Image(top.transform, $"TopIcon{index + 1}", Sprite(topIconNames[index]),
                    new Color32(205, 209, 210, 255), new Vector2(topIconCenter, 0.5f),
                    new Vector2(topIconCenter, 0.5f), new Vector2(-22f, -22f), new Vector2(22f, 22f),
                    UnityEngine.UI.Image.Type.Simple);
                topIcon.preserveAspect = true;
            }

            topCommands = new List<Button>();
            for (int index = 0; index < 3; index += 1)
            {
                float minimum = 0.825f + index * 0.04125f;
                Button topCommand = Button(top.transform, $"TopCommand{index + 1}", string.Empty, null,
                    new Vector2(minimum, 0.08f), new Vector2(minimum + 0.04125f, 0.92f), Vector2.zero,
                    Vector2.zero, out TMP_Text topCommandLabel);
                Object.DestroyImmediate(topCommandLabel.gameObject);
                topCommand.image.color = Color.clear;
                topCommands.Add(topCommand);
            }

            topSystemButton = Button(top.transform, "TopSystemButton", string.Empty, null,
                new Vector2(0.94875f, 0.08f), new Vector2(0.99f, 0.92f), Vector2.zero, Vector2.zero,
                out TMP_Text topSystemLabel);
            Object.DestroyImmediate(topSystemLabel.gameObject);
            topSystemButton.image.color = Color.clear;
        }

        // 월드 화면과 같은 크기의 상단 자원 아이콘을 배치합니다.
        private static void CreateHudIcon(Transform parent, string name, string spriteName, float horizontalAnchor)
        {
            Image icon = Image(parent, name, Sprite(spriteName), Color.white,
                new Vector2(horizontalAnchor, 0.5f), new Vector2(horizontalAnchor, 0.5f),
                new Vector2(-18f, -18f), new Vector2(18f, 18f), UnityEngine.UI.Image.Type.Simple);
            icon.preserveAspect = true;
        }

        // 월드 화면과 같은 위치에 상단 정보 구획용 세로 금속선을 배치합니다.
        private static void CreateTopSeparator(Transform parent, string name, float horizontalAnchor)
        {
            Image(parent, name, null, new Color32(42, 50, 54, 210),
                new Vector2(horizontalAnchor, 0.20f), new Vector2(horizontalAnchor, 0.80f),
                new Vector2(-1f, 0f), new Vector2(1f, 0f), UnityEngine.UI.Image.Type.Simple);
        }

        // 세로 구획을 나누는 얇은 금속선을 생성합니다.
        private static void Divider(Transform parent, float x)
        {
            Image(parent, $"Divider-{x}", null, new Color32(92, 101, 105, 90),
                new Vector2(x, 0.08f), new Vector2(x, 0.92f), new Vector2(-1f, 0f), new Vector2(1f, 0f),
                UnityEngine.UI.Image.Type.Simple);
        }

        // 아이콘이 포함된 성 능력치 행을 생성합니다.
        private static TMP_Text Stat(Transform parent, string name, string text, float yMin, string iconName)
        {
            GameObject row = Panel(parent, name + "Row", Sprite("button_flat_normal"), Color.white,
                new Vector2(0.055f, yMin), new Vector2(0.945f, yMin + 0.052f), Vector2.zero, Vector2.zero);
            Image(row.transform, "Icon", Sprite(iconName), Color.white, new Vector2(0f, 0f), new Vector2(0.18f, 1f),
                new Vector2(8f, 5f), Vector2.zero, UnityEngine.UI.Image.Type.Simple).preserveAspect = true;
            return Text(row.transform, name, text, 15f, TextAlignmentOptions.MidlineLeft,
                new Vector2(0.18f, 0f), Vector2.one, new Vector2(5f, 0f), new Vector2(-8f, 0f), false);
        }

        // 패널 이미지를 생성하고 Sprite가 있으면 9-Slice로 표시합니다.
        private static GameObject Panel(Transform parent, string name, Sprite sprite, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            Rect(gameObject.GetComponent<RectTransform>(), anchorMin, anchorMax, offsetMin, offsetMax);
            Image image = gameObject.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.type = sprite == null ? UnityEngine.UI.Image.Type.Simple : UnityEngine.UI.Image.Type.Sliced;
            image.raycastTarget = false;
            return gameObject;
        }

        // 스프라이트 버튼과 TMP 글자를 생성합니다.
        private static Button Button(Transform parent, string name, string text, Sprite sprite,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, out TMP_Text label)
        {
            GameObject gameObject = Panel(parent, name, sprite, Color.white, anchorMin, anchorMax, offsetMin, offsetMax);
            gameObject.GetComponent<Image>().raycastTarget = true;
            Button button = gameObject.AddComponent<Button>();
            label = Text(gameObject.transform, "Label", text, 16f, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, new Vector2(10f, 3f), new Vector2(-10f, -3f), false);
            return button;
        }

        // 지정한 TMP 폰트를 사용하는 텍스트를 생성합니다.
        private static TMP_Text Text(Transform parent, string name, string text, float size,
            TextAlignmentOptions alignment, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin,
            Vector2 offsetMax, bool serif)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            gameObject.transform.SetParent(parent, false);
            Rect(gameObject.GetComponent<RectTransform>(), anchorMin, anchorMax, offsetMin, offsetMax);
            TMP_Text label = gameObject.GetComponent<TMP_Text>();
            label.text = text;
            label.font = serif ? serifFont : sansFont;
            label.fontSize = size;
            label.color = new Color32(234, 238, 240, 255);
            label.alignment = alignment;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Overflow;
            label.raycastTarget = false;
            return label;
        }

        // 슬롯 또는 장식용 이미지를 생성합니다.
        private static Image Image(Transform parent, string name, Sprite sprite, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax,
            UnityEngine.UI.Image.Type type)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            Rect(gameObject.GetComponent<RectTransform>(), anchorMin, anchorMax, offsetMin, offsetMax);
            Image image = gameObject.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.type = type;
            image.raycastTarget = false;
            return image;
        }

        // RectTransform의 앵커와 여백을 설정합니다.
        private static void Rect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        // Resources의 생성 UI 스프라이트를 불러옵니다.
        private static Sprite Sprite(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Resources/UI/Generated/{name}.png");
        }

        // 생성한 공통 UI 이미지를 무압축 Sprite/Single과 지정 Border로 설정합니다.
        private static void ConfigureSlicedAsset(string path, Vector4 border)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return;
            }
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.spriteBorder = border;
            importer.SaveAndReimport();
        }

        // 단일 오브젝트 참조를 직렬화합니다.
        private static void SetObject(SerializedObject serialized, string name, Object value)
        {
            serialized.FindProperty(name).objectReferenceValue = value;
        }

        // GameObject 배열을 직렬화합니다.
        private static void Array(SerializedProperty property, IReadOnlyList<GameObject> values)
        {
            property.arraySize = values.Count;
            for (int index = 0; index < values.Count; index += 1)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            }
        }

        // Image 배열을 직렬화합니다.
        private static void ImageArray(SerializedProperty property, IReadOnlyList<Image> values)
        {
            property.arraySize = values.Count;
            for (int index = 0; index < values.Count; index += 1)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            }
        }

        // TMP 배열을 직렬화합니다.
        private static void TextArray(SerializedProperty property, IReadOnlyList<TMP_Text> values)
        {
            property.arraySize = values.Count;
            for (int index = 0; index < values.Count; index += 1)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            }
        }

        // Button 배열을 직렬화합니다.
        private static void ButtonArray(SerializedProperty property, IReadOnlyList<Button> values)
        {
            property.arraySize = values.Count;
            for (int index = 0; index < values.Count; index += 1)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            }
        }
    }
}

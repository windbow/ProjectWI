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
    public static class WIAdministrationWorldUGUIBuilder
    {
        private const string PrefabPath = "Assets/Prefabs/Administration/WIAdministrationWorldUGUI.prefab";
        private static TMP_FontAsset sansFont;
        private static TMP_FontAsset serifFont;

        // 월드 화면의 고정 UGUI 계층을 프리팹으로 만들고 MainScene에 배치합니다.
        [MenuItem("WI/UI/Build Administration World UGUI")]
        public static void Build()
        {
            ConfigureStrategyButtonAsset("Assets/Resources/UI/Generated/bg_type_d.png",
                new Vector4(16f, 16f, 16f, 16f));
            ConfigureStrategyButtonAsset("Assets/Resources/UI/Generated/strategy_command_button_v2.png",
                new Vector4(34f, 24f, 34f, 24f));
            ConfigureStrategyButtonAsset("Assets/Resources/UI/Generated/strategy_next_turn_button_v2.png",
                new Vector4(92f, 24f, 92f, 24f));
            ConfigureStrategyButtonAsset("Assets/Resources/UI/Generated/strategy_next_turn_button_v3.png",
                new Vector4(88f, 24f, 42f, 24f));
            CreatePersistentDynamicFont("Assets/Fonts/NotoSansCJKkr-Regular.otf",
                "Assets/Fonts/TMP/NotoSansCJKkr-Dynamic.asset");
            CreatePersistentDynamicFont("Assets/Fonts/NotoSerifCJKkr-Regular.otf",
                "Assets/Fonts/TMP/NotoSerifCJKkr-Dynamic.asset");
            sansFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/TMP/NotoSansCJKkr-Dynamic.asset");
            serifFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/TMP/NotoSerifCJKkr-Dynamic.asset");
            ReconnectCampaignTitleFonts();

            GameObject root = new GameObject("WIAdministrationWorldUGUI", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(WIAdministrationWorldUGUIController),
                typeof(WIAdministrationMapUGUIController));
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject content = CreatePanel(root.transform, "WorldContent", null, new Color32(8, 15, 20, 255),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            GameObject top = CreatePanel(content.transform, "TopHUD", null, new Color32(6, 13, 18, 255),
                new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -92f), Vector2.zero);
            CreateImage(top.transform, "BottomMetalLine", null, new Color32(91, 91, 85, 255),
                new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 2f), Image.Type.Simple);
            Image factionCrest = CreateImage(top.transform, "FactionCrest", LoadSprite("strategy_avalon_crest_v1"), Color.white,
                new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(24f, 8f), new Vector2(88f, -8f), Image.Type.Simple);
            factionCrest.preserveAspect = true;
            TMP_Text faction = CreateText(top.transform, "Faction", "아발론 왕국", 28f, TextAlignmentOptions.MidlineLeft,
                new Vector2(0f, 0f), new Vector2(0.18f, 1f), new Vector2(98f, 0f), new Vector2(-8f, 0f), true);
            TMP_Text date = CreateText(top.transform, "Date", "1년 03월", 22f, TextAlignmentOptions.Center,
                new Vector2(0.19f, 0f), new Vector2(0.31f, 1f), new Vector2(18f, 0f), Vector2.zero, false);
            TMP_Text turnDescription = CreateText(top.transform, "TurnDescription", string.Empty, 1f,
                TextAlignmentOptions.Center, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, false);
            turnDescription.gameObject.SetActive(false);
            TMP_Text gold = CreateText(top.transform, "Gold", "금화 1,240 (+320/월)", 19f, TextAlignmentOptions.Center,
                new Vector2(0.32f, 0f), new Vector2(0.47f, 1f), new Vector2(18f, 0f), Vector2.zero, false);
            TMP_Text mana = CreateText(top.transform, "Mana", "마나 360 (+90/월)", 19f, TextAlignmentOptions.Center,
                new Vector2(0.47f, 0f), new Vector2(0.61f, 1f), new Vector2(18f, 0f), Vector2.zero, false);
            TMP_Text influence = CreateText(top.transform, "Influence", "영향력 125 (+15/월)", 19f, TextAlignmentOptions.Center,
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
                Image topIcon = CreateImage(top.transform, $"TopIcon{index + 1}", LoadSprite(topIconNames[index]),
                    new Color32(205, 209, 210, 255), new Vector2(topIconCenter, 0.5f), new Vector2(topIconCenter, 0.5f),
                    new Vector2(-22f, -22f), new Vector2(22f, 22f), Image.Type.Simple);
                topIcon.preserveAspect = true;
            }
            List<Button> topCommands = new List<Button>();
            WIAdministrationShortcutAction[] topActions = {
                WIAdministrationShortcutAction.MonthlyReport, WIAdministrationShortcutAction.Council,
                WIAdministrationShortcutAction.Research
            };
            for (int index = 0; index < topActions.Length; index += 1)
            {
                float minimum = 0.825f + index * 0.04125f;
                Button topCommand = CreateButton(top.transform, $"TopCommand{index + 1}", string.Empty, null,
                    new Vector2(minimum, 0.08f), new Vector2(minimum + 0.04125f, 0.92f), Vector2.zero, Vector2.zero,
                    out TMP_Text topCommandLabel);
                Object.DestroyImmediate(topCommandLabel.gameObject);
                topCommand.image.color = Color.clear;
                topCommands.Add(topCommand);
            }
            Button topSystemButton = CreateButton(top.transform, "TopSystemButton", string.Empty, null,
                new Vector2(0.94875f, 0.08f), new Vector2(0.99f, 0.92f), Vector2.zero, Vector2.zero,
                out TMP_Text topSystemLabel);
            Object.DestroyImmediate(topSystemLabel.gameObject);
            topSystemButton.image.color = Color.clear;

            GameObject body = CreatePanel(content.transform, "WorldBody", null, Color.clear,
                new Vector2(0f, 0f), Vector2.one, new Vector2(0f, 112f), new Vector2(0f, -90f));
            GameObject left = CreatePanel(body.transform, "CastleSummaryPanel", LoadSprite("right_panel_frame"), Color.white,
                Vector2.zero, new Vector2(0.22f, 1f), new Vector2(14f, 12f), new Vector2(-6f, -12f));
            Image castleCrest = CreateImage(left.transform, "CastleCrest", LoadSprite("strategy_avalon_crest_v1"), Color.white,
                new Vector2(0.04f, 0.855f), new Vector2(0.23f, 0.985f), Vector2.zero, Vector2.zero, Image.Type.Simple);
            castleCrest.preserveAspect = true;
            TMP_Text castleName = CreateText(left.transform, "CastleName", "별빛 수도", 27f, TextAlignmentOptions.MidlineLeft,
                new Vector2(0.23f, 0.915f), new Vector2(0.88f, 0.985f), Vector2.zero, Vector2.zero, true);
            TMP_Text castleOwner = CreateText(left.transform, "CastleOwner", "수도 · 아발론 왕국", 15f,
                TextAlignmentOptions.MidlineLeft, new Vector2(0.23f, 0.855f), new Vector2(0.88f, 0.925f),
                Vector2.zero, Vector2.zero, false);
            Image castleRankIcon = CreateImage(left.transform, "CastleRankIcon", LoadSprite("icon_flat_faction"),
                new Color32(210, 205, 185, 255), new Vector2(0.88f, 0.90f), new Vector2(0.97f, 0.98f),
                Vector2.zero, Vector2.zero, Image.Type.Simple);
            castleRankIcon.preserveAspect = true;
            Image castleImage = CreateImage(left.transform, "CastleImage", null, Color.white,
                new Vector2(0.04f, 0.62f), new Vector2(0.96f, 0.84f), Vector2.zero, Vector2.zero, Image.Type.Simple);
            castleImage.preserveAspect = true;
            List<TMP_Text> castleDetailRows = new List<TMP_Text>();
            for (int index = 0; index < 6; index += 1)
            {
                float maximum = 0.61f - index * 0.05f;
                TMP_Text detailRow = CreateText(left.transform, $"CastleDetailRow{index + 1}", string.Empty, 16f,
                    TextAlignmentOptions.MidlineLeft, new Vector2(0.05f, maximum - 0.05f),
                    new Vector2(0.95f, maximum), Vector2.zero, Vector2.zero, false);
                detailRow.textWrappingMode = TextWrappingModes.NoWrap;
                castleDetailRows.Add(detailRow);
                CreateImage(left.transform, $"CastleDetailLine{index + 1}", null, new Color32(44, 53, 58, 190),
                    new Vector2(0.05f, maximum - 0.05f), new Vector2(0.95f, maximum - 0.05f),
                    Vector2.zero, new Vector2(0f, 1f), Image.Type.Simple);
            }
            CreateText(left.transform, "HeroHeading", "영웅", 17f, TextAlignmentOptions.MidlineLeft,
                new Vector2(0.05f, 0.265f), new Vector2(0.95f, 0.31f), Vector2.zero, Vector2.zero, true);
            List<Image> heroPortraits = new List<Image>();
            List<TMP_Text> heroLabels = new List<TMP_Text>();
            for (int index = 0; index < 4; index += 1)
            {
                float minimum = 0.045f + index * 0.238f;
                GameObject card = CreatePanel(left.transform, $"HeroCard{index + 1}", LoadSprite("strategy_hero_card_v1"), Color.white,
                    new Vector2(minimum, 0.085f), new Vector2(minimum + 0.215f, 0.265f), Vector2.zero, Vector2.zero);
                Image portrait = CreateImage(card.transform, "Portrait", null, Color.white,
                    new Vector2(0.08f, 0.27f), new Vector2(0.92f, 0.93f), Vector2.zero, Vector2.zero, Image.Type.Simple);
                portrait.preserveAspect = true;
                portrait.transform.SetAsFirstSibling();
                TMP_Text heroLabel = CreateText(card.transform, "HeroLabel", string.Empty, 12f, TextAlignmentOptions.Bottom,
                    new Vector2(0.05f, 0.01f), new Vector2(0.95f, 0.28f), Vector2.zero, Vector2.zero, false);
                heroLabel.fontStyle = FontStyles.Bold;
                heroPortraits.Add(portrait);
                heroLabels.Add(heroLabel);
            }
            Button castleManage = CreateButton(left.transform, "CastleManageButton", "성 관리", LoadSprite("button_primary"),
                new Vector2(0.04f, 0.015f), new Vector2(0.80f, 0.075f), Vector2.zero, Vector2.zero, out _);
            Button castleRecords = CreateButton(left.transform, "CastleRecordButton", string.Empty, LoadSprite("button_normal"),
                new Vector2(0.83f, 0.015f), new Vector2(0.96f, 0.075f), Vector2.zero, Vector2.zero,
                out TMP_Text castleRecordsLabel);
            Object.DestroyImmediate(castleRecordsLabel.gameObject);
            Image castleRecordsIcon = CreateImage(castleRecords.transform, "Icon", LoadSprite("icon_flat_council"),
                new Color32(210, 205, 185, 255), new Vector2(0.18f, 0.12f), new Vector2(0.82f, 0.88f),
                Vector2.zero, Vector2.zero, Image.Type.Simple);
            castleRecordsIcon.preserveAspect = true;

            GameObject center = CreatePanel(body.transform, "MapPanel", null, new Color32(13, 23, 27, 255),
                new Vector2(0.22f, 0f), new Vector2(0.81f, 1f), new Vector2(4f, 12f), new Vector2(-4f, -12f));
            Image mapImage = CreateImage(center.transform, "MapImage", null, Color.white, Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, Image.Type.Simple);
            mapImage.preserveAspect = false;
            GameObject connectionLayer = new GameObject("MapConnections", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(WIAdministrationMapConnectionGraphic));
            connectionLayer.transform.SetParent(center.transform, false);
            SetRect(connectionLayer.GetComponent<RectTransform>(), Vector2.zero, Vector2.one,
                new Vector2(8f, 8f), new Vector2(-8f, -8f));
            WIAdministrationMapConnectionGraphic connectionGraphic =
                connectionLayer.GetComponent<WIAdministrationMapConnectionGraphic>();
            connectionGraphic.raycastTarget = false;
            GameObject mapNodes = CreatePanel(center.transform, "MapNodes", null, Color.clear, Vector2.zero, Vector2.one,
                new Vector2(8f, 8f), new Vector2(-8f, -8f));
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(
                "Assets/Data/ScriptableObject/Administration/WI_AdministrationDatabase.asset");
            List<Button> castleButtons = new List<Button>();
            List<Image> castleMarkers = new List<Image>();
            List<TMP_Text> castleLabels = new List<TMP_Text>();
            List<string> castleIds = new List<string>();
            foreach (WICastleDefinition castle in database.Castles)
            {
                Vector2 position = new Vector2(castle.NormalizedMapPosition.x, 1f - castle.NormalizedMapPosition.y);
                Button castleButton = CreateButton(mapNodes.transform, "Castle-" + castle.Id, string.Empty, null,
                    position, position, new Vector2(-48f, -34f), new Vector2(48f, 34f), out TMP_Text unusedLabel);
                Object.DestroyImmediate(unusedLabel.gameObject);
                castleButton.image.color = Color.clear;
                Image marker = CreateImage(castleButton.transform, "Marker", LoadSprite("map_castle_" + castle.FactionId),
                    Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(-20f, -10f), new Vector2(20f, 30f), Image.Type.Simple);
                marker.preserveAspect = true;
                TMP_Text castleLabel = CreateText(castleButton.transform, "CastleName",
                    castle.DisplayName.Get(database.UseEnglish), 12f, TextAlignmentOptions.Center,
                    new Vector2(0f, 0f), new Vector2(1f, 0.38f), Vector2.zero, Vector2.zero, false);
                castleLabel.color = new Color32(242, 240, 223, 255);
                castleLabel.fontStyle = FontStyles.Bold;
                castleButtons.Add(castleButton);
                castleMarkers.Add(marker);
                castleLabels.Add(castleLabel);
                castleIds.Add(castle.Id);
            }
            CreateText(center.transform, "MapTitle", "천하 전략도", 28f, TextAlignmentOptions.Top,
                new Vector2(0f, 0.90f), Vector2.one, new Vector2(26f, 0f), new Vector2(-26f, -14f), true);

            GameObject right = CreatePanel(body.transform, "CampaignSidePanel", LoadSprite("bg_type_d"), Color.white,
                new Vector2(0.81f, 0f), Vector2.one, new Vector2(6f, 12f), new Vector2(-14f, -12f));
            CreateText(right.transform, "MonthKicker", "이번 달", 16f, TextAlignmentOptions.Center,
                new Vector2(0f, 0.92f), Vector2.one, new Vector2(18f, 0f), new Vector2(-18f, 0f), true);
            CreateText(right.transform, "ObjectiveHeading", "주요 목표", 19f, TextAlignmentOptions.MidlineLeft,
                new Vector2(0f, 0.84f), new Vector2(1f, 0.92f), new Vector2(22f, 0f), new Vector2(-18f, 0f), true);
            Button objective = CreateButton(right.transform, "ObjectiveButton", "첫 목표", LoadSprite("right_objective_card"),
                new Vector2(0f, 0.69f), new Vector2(1f, 0.84f), new Vector2(18f, 0f), new Vector2(-18f, 0f), out TMP_Text objectiveTitle);
            TMP_Text objectiveProgress = CreateText(right.transform, "ObjectiveProgress", "진행 상황", 16f,
                TextAlignmentOptions.TopLeft, new Vector2(0f, 0.53f), new Vector2(1f, 0.69f),
                new Vector2(22f, 8f), new Vector2(-18f, -4f), false);
            Image progressTrack = CreateImage(right.transform, "ProgressTrack", null, new Color32(38, 52, 61, 255),
                new Vector2(0f, 0.50f), new Vector2(1f, 0.525f), new Vector2(22f, 0f), new Vector2(-22f, 0f), Image.Type.Simple);
            Image progressFill = CreateImage(progressTrack.transform, "ProgressFill", null, new Color32(110, 184, 224, 255),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Image.Type.Filled);
            progressFill.fillMethod = Image.FillMethod.Horizontal;
            progressFill.fillAmount = 0.45f;
            CreateText(right.transform, "AlertHeading", "알림", 19f, TextAlignmentOptions.MidlineLeft,
                new Vector2(0f, 0.41f), new Vector2(1f, 0.49f), new Vector2(22f, 0f), new Vector2(-18f, 0f), true);
            Button battleAlert = CreateButton(right.transform, "BattleAlertButton", "현재 전투 없음", LoadSprite("right_danger_row"),
                new Vector2(0f, 0.30f), new Vector2(1f, 0.41f), new Vector2(18f, 0f), new Vector2(-18f, 0f), out TMP_Text battleAlertLabel);
            TMP_Text monthlyNews = CreateText(right.transform, "MonthlyNews", "새로운 월간 보고가 없습니다.", 16f,
                TextAlignmentOptions.TopLeft, new Vector2(0f, 0.05f), new Vector2(1f, 0.29f),
                new Vector2(22f, 8f), new Vector2(-18f, -8f), false);

            GameObject bottom = CreatePanel(content.transform, "CommandBar", null, new Color32(5, 12, 16, 255),
                Vector2.zero, new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 112f));
            CreateImage(bottom.transform, "CommandTopLine", null, new Color32(39, 50, 55, 255),
                new Vector2(0f, 1f), Vector2.one, Vector2.zero, new Vector2(0f, 2f), Image.Type.Simple);
            CreateImage(bottom.transform, "CommandBottomLine", null, new Color32(48, 48, 45, 255),
                Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 4f), Image.Type.Simple);
            string[] names = { "Military", "Heroes", "Diplomacy", "Scheme", "Research", "Faction", "Report" };
            string[] labels = { "군사", "인사", "외교", "계략", "연구", "평정", "월보" };
            string[] icons = { "icon_flat_military", "icon_flat_heroes", "icon_flat_diplomacy", "icon_flat_scheme",
                "icon_flat_research", "icon_flat_faction", "icon_flat_report" };
            List<Button> commands = new List<Button>(topCommands);
            for (int index = 0; index < names.Length; index += 1)
            {
                float min = 0.018f + index * 0.105f;
                Button command = CreateButton(bottom.transform, names[index] + "Button", labels[index], LoadSprite("strategy_command_button_v2"),
                    new Vector2(min, 0.11f), new Vector2(min + 0.098f, 0.89f), Vector2.zero, Vector2.zero,
                    out TMP_Text commandLabel);
                commandLabel.fontSize = 21f;
                commandLabel.font = serifFont;
                commandLabel.color = new Color32(214, 210, 196, 255);
                commandLabel.alignment = TextAlignmentOptions.MidlineLeft;
                commandLabel.rectTransform.offsetMin = new Vector2(70f, 4f);
                commandLabel.rectTransform.offsetMax = new Vector2(-8f, -4f);
                CreateImage(command.transform, "Icon", LoadSprite(icons[index]), new Color32(204, 201, 187, 255),
                    new Vector2(0f, 0.08f), new Vector2(0.36f, 0.92f), new Vector2(12f, 0f), Vector2.zero,
                    Image.Type.Simple).preserveAspect = true;
                commands.Add(command);
            }
            Button endTurn = CreateButton(bottom.transform, "EndTurnButton", "다음 턴", LoadSprite("strategy_next_turn_button_v3"),
                new Vector2(0.78f, 0.02f), new Vector2(0.982f, 0.98f), Vector2.zero, Vector2.zero, out TMP_Text endTurnLabel);
            ColorBlock endTurnColors = endTurn.colors;
            endTurnColors.normalColor = Color.white;
            endTurnColors.highlightedColor = Color.white;
            endTurnColors.selectedColor = Color.white;
            endTurn.colors = endTurnColors;
            endTurnLabel.fontSize = 29f;
            endTurnLabel.font = serifFont;
            endTurnLabel.rectTransform.offsetMin = new Vector2(112f, 4f);
            endTurnLabel.rectTransform.offsetMax = new Vector2(-46f, -4f);
            Image endTurnCrest = CreateImage(endTurn.transform, "Crest", LoadSprite("strategy_avalon_crest_v1"), Color.white,
                new Vector2(0f, 0.08f), new Vector2(0.32f, 0.92f), new Vector2(42f, 0f), new Vector2(-2f, 0f),
                Image.Type.Simple);
            endTurnCrest.preserveAspect = true;
            TMP_Text endTurnArrow = CreateText(endTurn.transform, "Arrow", "›", 38f, TextAlignmentOptions.Center,
                new Vector2(0.89f, 0f), new Vector2(0.98f, 1f), Vector2.zero, Vector2.zero, true);
            endTurnArrow.color = new Color32(178, 184, 184, 255);

            WIAdministrationWorldUGUIController controller = root.GetComponent<WIAdministrationWorldUGUIController>();
            SerializedObject serialized = new SerializedObject(controller);
            SetObject(serialized, "contentRoot", content);
            SetObject(serialized, "factionLabel", faction);
            SetObject(serialized, "dateLabel", date);
            SetObject(serialized, "turnDescriptionLabel", turnDescription);
            SetObject(serialized, "goldLabel", gold);
            SetObject(serialized, "manaLabel", mana);
            SetObject(serialized, "influenceLabel", influence);
            SetObject(serialized, "castleNameLabel", castleName);
            SetObject(serialized, "castleOwnerLabel", castleOwner);
            SetTextArray(serialized.FindProperty("castleDetailRows"), castleDetailRows);
            SetImageArray(serialized.FindProperty("castleHeroPortraits"), heroPortraits);
            SetTextArray(serialized.FindProperty("castleHeroLabels"), heroLabels);
            SetObject(serialized, "mapImage", mapImage);
            SetObject(serialized, "castleImage", castleImage);
            SetObject(serialized, "objectiveTitleLabel", objectiveTitle);
            SetObject(serialized, "objectiveProgressLabel", objectiveProgress);
            SetObject(serialized, "objectiveProgressFill", progressFill);
            SetObject(serialized, "battleAlertLabel", battleAlertLabel);
            SetObject(serialized, "battleAlertButton", battleAlert);
            SetObject(serialized, "monthlyNewsLabel", monthlyNews);
            SetObject(serialized, "castleManageButton", castleManage);
            SetObject(serialized, "castleRecordButton", castleRecords);
            SetObject(serialized, "objectiveButton", objective);
            SetObject(serialized, "endTurnButton", endTurn);
            SetObject(serialized, "endTurnLabel", endTurnLabel);
            SetObject(serialized, "systemButton", topSystemButton);
            SetArray(serialized.FindProperty("commandButtons"), commands);
            WIAdministrationShortcutAction[] actions = {
                WIAdministrationShortcutAction.MonthlyReport, WIAdministrationShortcutAction.Council,
                WIAdministrationShortcutAction.Research,
                WIAdministrationShortcutAction.Military, WIAdministrationShortcutAction.Heroes,
                WIAdministrationShortcutAction.Diplomacy, WIAdministrationShortcutAction.Scheme,
                WIAdministrationShortcutAction.Research, WIAdministrationShortcutAction.Faction,
                WIAdministrationShortcutAction.MonthlyReport
            };
            SerializedProperty actionProperty = serialized.FindProperty("commandActions");
            actionProperty.arraySize = actions.Length;
            for (int index = 0; index < actions.Length; index += 1)
            {
                actionProperty.GetArrayElementAtIndex(index).enumValueIndex = (int)actions[index];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();

            WIAdministrationMapUGUIController mapController = root.GetComponent<WIAdministrationMapUGUIController>();
            SerializedObject mapSerialized = new SerializedObject(mapController);
            SetArray(mapSerialized.FindProperty("castleButtons"), castleButtons);
            SetImageArray(mapSerialized.FindProperty("castleMarkers"), castleMarkers);
            SetTextArray(mapSerialized.FindProperty("castleLabels"), castleLabels);
            SetStringArray(mapSerialized.FindProperty("castleIds"), castleIds);
            SetObject(mapSerialized, "connectionGraphic", connectionGraphic);
            SetObject(mapSerialized, "avalonMarker", LoadSprite("map_castle_avalon"));
            SetObject(mapSerialized, "valdorMarker", LoadSprite("map_castle_valdor"));
            SetObject(mapSerialized, "ironheartMarker", LoadSprite("map_castle_ironheart"));
            SetObject(mapSerialized, "sylvanroadMarker", LoadSprite("map_castle_sylvanroad"));
            SetObject(mapSerialized, "necropolisMarker", LoadSprite("map_castle_necropolis"));
            mapSerialized.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            RemoveExistingSceneInstance();
            WIAdministrationUGUISceneUtility.InstantiateUnderSceneRoot(prefab);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("행정 월드 UGUI 프리팹을 생성하고 MainScene에 배치했습니다.");
        }

        // 지정한 스프라이트와 앵커로 패널을 생성합니다.
        private static GameObject CreatePanel(Transform parent, string name, Sprite sprite, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            SetRect(rect, anchorMin, anchorMax, offsetMin, offsetMax);
            Image image = gameObject.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.type = sprite == null ? Image.Type.Simple : Image.Type.Sliced;
            image.raycastTarget = false;
            return gameObject;
        }

        // 고정 UGUI 버튼과 TMP 글자 자식을 생성합니다.
        private static Button CreateButton(Transform parent, string name, string text, Sprite sprite,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, out TMP_Text label)
        {
            GameObject gameObject = CreatePanel(parent, name, sprite, Color.white, anchorMin, anchorMax, offsetMin, offsetMax);
            Image image = gameObject.GetComponent<Image>();
            image.raycastTarget = true;
            Button button = gameObject.AddComponent<Button>();
            label = CreateText(gameObject.transform, "Label", text, 17f, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, new Vector2(12f, 4f), new Vector2(-12f, -4f), false);
            return button;
        }

        // Dynamic TMP 폰트를 사용하는 고정 텍스트 요소를 생성합니다.
        private static TMP_Text CreateText(Transform parent, string name, string text, float size, TextAlignmentOptions alignment,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, bool serif)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            gameObject.transform.SetParent(parent, false);
            SetRect(gameObject.GetComponent<RectTransform>(), anchorMin, anchorMax, offsetMin, offsetMax);
            TMP_Text label = gameObject.GetComponent<TMP_Text>();
            label.text = text;
            label.font = serif ? serifFont : sansFont;
            label.fontSize = size;
            label.color = new Color32(232, 236, 238, 255);
            label.alignment = alignment;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Overflow;
            label.raycastTarget = false;
            return label;
        }

        // 지정한 스프라이트와 앵커로 이미지 요소를 생성합니다.
        private static Image CreateImage(Transform parent, string name, Sprite sprite, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Image.Type type)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            SetRect(gameObject.GetComponent<RectTransform>(), anchorMin, anchorMax, offsetMin, offsetMax);
            Image image = gameObject.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.type = type;
            image.raycastTarget = false;
            return image;
        }

        // 상단 자원 정보 앞에 작은 고정 아이콘을 배치합니다.
        private static void CreateHudIcon(Transform parent, string name, string spriteName, float horizontalAnchor)
        {
            Image icon = CreateImage(parent, name, LoadSprite(spriteName), Color.white,
                new Vector2(horizontalAnchor, 0.5f), new Vector2(horizontalAnchor, 0.5f),
                new Vector2(-18f, -18f), new Vector2(18f, 18f), Image.Type.Simple);
            icon.preserveAspect = true;
        }

        // 상단 정보 구획 사이에 시안의 얇은 세로 금속선을 배치합니다.
        private static void CreateTopSeparator(Transform parent, string name, float horizontalAnchor)
        {
            CreateImage(parent, name, null, new Color32(42, 50, 54, 210),
                new Vector2(horizontalAnchor, 0.20f), new Vector2(horizontalAnchor, 0.80f),
                new Vector2(-1f, 0f), new Vector2(1f, 0f), Image.Type.Simple);
        }

        // RectTransform의 앵커와 여백을 일괄 적용합니다.
        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        // 생성된 UGUI 컴포넌트 참조를 직렬화 필드에 연결합니다.
        private static void SetObject(SerializedObject serialized, string propertyName, Object value)
        {
            serialized.FindProperty(propertyName).objectReferenceValue = value;
        }

        // 버튼 목록을 직렬화 배열에 연결합니다.
        private static void SetArray(SerializedProperty property, IReadOnlyList<Button> values)
        {
            property.arraySize = values.Count;
            for (int index = 0; index < values.Count; index += 1)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            }
        }

        // 이미지 목록을 직렬화 배열에 연결합니다.
        private static void SetImageArray(SerializedProperty property, IReadOnlyList<Image> values)
        {
            property.arraySize = values.Count;
            for (int index = 0; index < values.Count; index += 1)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            }
        }

        // TMP 글자 목록을 직렬화 배열에 연결합니다.
        private static void SetTextArray(SerializedProperty property, IReadOnlyList<TMP_Text> values)
        {
            property.arraySize = values.Count;
            for (int index = 0; index < values.Count; index += 1)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            }
        }

        // 문자열 목록을 직렬화 배열에 연결합니다.
        private static void SetStringArray(SerializedProperty property, IReadOnlyList<string> values)
        {
            property.arraySize = values.Count;
            for (int index = 0; index < values.Count; index += 1)
            {
                property.GetArrayElementAtIndex(index).stringValue = values[index];
            }
        }

        // Resources/UI/Generated의 스프라이트를 불러옵니다.
        private static Sprite LoadSprite(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Resources/UI/Generated/{name}.png");
        }

        // 생성된 전략 버튼을 Sprite와 9-Slice용 Border로 고정해 축소 상태의 모서리 장식을 보존합니다.
        private static void ConfigureStrategyButtonAsset(string assetPath, Vector4 border)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 1024;
            importer.spriteBorder = border;
            importer.SaveAndReimport();
        }

        // TMP 아틀라스와 머티리얼을 폰트 자산의 영구 서브에셋으로 저장합니다.
        private static void CreatePersistentDynamicFont(string sourcePath, string assetPath)
        {
            Font source = AssetDatabase.LoadAssetAtPath<Font>(sourcePath);
            AssetDatabase.DeleteAsset(assetPath);
            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(source);
            fontAsset.name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            Texture2D atlas = fontAsset.atlasTexture;
            Material material = fontAsset.material;
            AssetDatabase.CreateAsset(fontAsset, assetPath);
            atlas.name = fontAsset.name + " Atlas";
            material.name = fontAsset.name + " Material";
            AssetDatabase.AddObjectToAsset(atlas, fontAsset);
            AssetDatabase.AddObjectToAsset(material, fontAsset);
            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
        }

        // 재생성된 TMP 폰트 자산을 기존 캠페인 타이틀 프리팹에 다시 연결합니다.
        private static void ReconnectCampaignTitleFonts()
        {
            GameObject titleRoot = PrefabUtility.LoadPrefabContents(
                "Assets/Prefabs/Administration/WICampaignTitleUGUI.prefab");
            foreach (TMP_Text label in titleRoot.GetComponentsInChildren<TMP_Text>(true))
            {
                label.font = label.name == "Title" || label.name == "Kicker" ? serifFont : sansFont;
                EditorUtility.SetDirty(label);
            }
            PrefabUtility.SaveAsPrefabAsset(titleRoot,
                "Assets/Prefabs/Administration/WICampaignTitleUGUI.prefab");
            PrefabUtility.UnloadPrefabContents(titleRoot);
        }

        // 재생성 전에 MainScene의 기존 월드 UGUI 인스턴스를 제거합니다.
        private static void RemoveExistingSceneInstance()
        {
            GameObject existing = GameObject.Find("WIAdministrationWorldUGUI");
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }
        }
    }
}

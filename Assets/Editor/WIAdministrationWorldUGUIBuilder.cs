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
            GameObject top = CreatePanel(content.transform, "TopHUD", LoadSprite("bg_type_c"), Color.white,
                new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -82f), Vector2.zero);
            TMP_Text faction = CreateText(top.transform, "Faction", "아발론 왕국", 25f, TextAlignmentOptions.MidlineLeft,
                new Vector2(0f, 0f), new Vector2(0.23f, 1f), new Vector2(26f, 0f), new Vector2(-8f, 0f), true);
            TMP_Text date = CreateText(top.transform, "Date", "1000년 01월 · 제 1턴", 20f, TextAlignmentOptions.Center,
                new Vector2(0.23f, 0f), new Vector2(0.45f, 1f), Vector2.zero, Vector2.zero, false);
            TMP_Text turnDescription = CreateText(top.transform, "TurnDescription", "명령을 검토하십시오.", 17f,
                TextAlignmentOptions.Center, new Vector2(0.45f, 0f), new Vector2(0.68f, 1f), Vector2.zero, Vector2.zero, false);
            TMP_Text gold = CreateText(top.transform, "Gold", "금화", 17f, TextAlignmentOptions.Center,
                new Vector2(0.68f, 0f), new Vector2(0.79f, 1f), Vector2.zero, Vector2.zero, false);
            TMP_Text mana = CreateText(top.transform, "Mana", "마나", 17f, TextAlignmentOptions.Center,
                new Vector2(0.79f, 0f), new Vector2(0.89f, 1f), Vector2.zero, Vector2.zero, false);
            TMP_Text influence = CreateText(top.transform, "Influence", "영향력", 17f, TextAlignmentOptions.Center,
                new Vector2(0.89f, 0f), Vector2.one, Vector2.zero, Vector2.zero, false);

            GameObject body = CreatePanel(content.transform, "WorldBody", null, Color.clear,
                new Vector2(0f, 0f), Vector2.one, new Vector2(0f, 104f), new Vector2(0f, -88f));
            GameObject left = CreatePanel(body.transform, "CastleSummaryPanel", LoadSprite("bg_type_a"), Color.white,
                Vector2.zero, new Vector2(0.18f, 1f), new Vector2(12f, 12f), new Vector2(-6f, -12f));
            CreateText(left.transform, "Kicker", "아발론 영지", 15f, TextAlignmentOptions.MidlineLeft,
                new Vector2(0f, 0.92f), Vector2.one, new Vector2(22f, 0f), new Vector2(-18f, 0f), false);
            TMP_Text castleName = CreateText(left.transform, "CastleName", "별빛 수도", 28f, TextAlignmentOptions.MidlineLeft,
                new Vector2(0f, 0.83f), new Vector2(1f, 0.93f), new Vector2(22f, 0f), new Vector2(-18f, 0f), true);
            Image castleImage = CreateImage(left.transform, "CastleImage", null, Color.white,
                new Vector2(0f, 0.51f), new Vector2(1f, 0.83f), new Vector2(22f, 8f), new Vector2(-22f, -8f), Image.Type.Simple);
            castleImage.preserveAspect = true;
            TMP_Text castleOwner = CreateText(left.transform, "CastleOwner", "수도 · 아발론 왕국", 16f,
                TextAlignmentOptions.MidlineLeft, new Vector2(0f, 0.44f), new Vector2(1f, 0.52f),
                new Vector2(22f, 0f), new Vector2(-18f, 0f), false);
            TMP_Text castleStats = CreateText(left.transform, "CastleStats", "번영 45 · 기술 40\n질서 50 · 방어 40", 18f,
                TextAlignmentOptions.TopLeft, new Vector2(0f, 0.29f), new Vector2(1f, 0.44f),
                new Vector2(22f, 0f), new Vector2(-18f, 0f), false);
            TMP_Text castleHeroes = CreateText(left.transform, "CastleHeroes", "주둔 영웅 4명", 16f,
                TextAlignmentOptions.TopLeft, new Vector2(0f, 0.19f), new Vector2(1f, 0.29f),
                new Vector2(22f, 0f), new Vector2(-18f, 0f), false);
            Button castleManage = CreateButton(left.transform, "CastleManageButton", "수도 관리", LoadSprite("button_primary"),
                new Vector2(0f, 0.05f), new Vector2(1f, 0.14f), new Vector2(22f, 0f), new Vector2(-22f, 0f), out _);

            GameObject center = CreatePanel(body.transform, "MapPanel", null, new Color32(13, 23, 27, 255),
                new Vector2(0.18f, 0f), new Vector2(0.82f, 1f), new Vector2(5f, 12f), new Vector2(-5f, -12f));
            Image mapImage = CreateImage(center.transform, "MapImage", null, Color.white, Vector2.zero, Vector2.one,
                new Vector2(8f, 8f), new Vector2(-8f, -8f), Image.Type.Simple);
            mapImage.preserveAspect = true;
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
                    position, position, new Vector2(-44f, -30f), new Vector2(44f, 30f), out TMP_Text unusedLabel);
                Object.DestroyImmediate(unusedLabel.gameObject);
                castleButton.image.color = Color.clear;
                Image marker = CreateImage(castleButton.transform, "Marker", LoadSprite("map_castle_" + castle.FactionId),
                    Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(-17f, -9f), new Vector2(17f, 25f), Image.Type.Simple);
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
                new Vector2(0f, 0.88f), Vector2.one, Vector2.zero, new Vector2(0f, -12f), true);

            GameObject right = CreatePanel(body.transform, "CampaignSidePanel", LoadSprite("bg_type_a"), Color.white,
                new Vector2(0.82f, 0f), Vector2.one, new Vector2(6f, 12f), new Vector2(-12f, -12f));
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

            GameObject bottom = CreatePanel(content.transform, "CommandBar", LoadSprite("bg_type_c"), Color.white,
                Vector2.zero, new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 104f));
            string[] names = { "Military", "Heroes", "Diplomacy", "Scheme", "Research", "Faction", "Council", "Report" };
            string[] labels = { "군사 M", "영웅 H", "외교 D", "첩보 S", "연구 R", "통치 G", "의회 C", "월간 보고 L" };
            string[] icons = { "icon_flat_military", "icon_flat_heroes", "icon_flat_diplomacy", "icon_flat_scheme",
                "icon_flat_research", "icon_flat_faction", "icon_flat_council", "icon_flat_report" };
            List<Button> commands = new List<Button>();
            for (int index = 0; index < names.Length; index += 1)
            {
                float min = 0.012f + index * 0.087f;
                Button command = CreateButton(bottom.transform, names[index] + "Button", labels[index], LoadSprite("button_flat_normal"),
                    new Vector2(min, 0.16f), new Vector2(min + 0.081f, 0.84f), Vector2.zero, Vector2.zero, out _);
                CreateImage(command.transform, "Icon", LoadSprite(icons[index]), Color.white,
                    new Vector2(0f, 0.12f), new Vector2(0.30f, 0.88f), new Vector2(8f, 0f), Vector2.zero, Image.Type.Simple).preserveAspect = true;
                commands.Add(command);
            }
            Button endTurn = CreateButton(bottom.transform, "EndTurnButton", "다음 턴 T", LoadSprite("button_type_h"),
                new Vector2(0.76f, 0.10f), new Vector2(0.985f, 0.90f), Vector2.zero, Vector2.zero, out TMP_Text endTurnLabel);
            Button systemButton = CreateButton(bottom.transform, "SystemButton", "설정", LoadSprite("button_flat_normal"),
                new Vector2(0.715f, 0.16f), new Vector2(0.755f, 0.84f), Vector2.zero, Vector2.zero, out _);

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
            SetObject(serialized, "castleStatsLabel", castleStats);
            SetObject(serialized, "castleHeroesLabel", castleHeroes);
            SetObject(serialized, "mapImage", mapImage);
            SetObject(serialized, "castleImage", castleImage);
            SetObject(serialized, "objectiveTitleLabel", objectiveTitle);
            SetObject(serialized, "objectiveProgressLabel", objectiveProgress);
            SetObject(serialized, "objectiveProgressFill", progressFill);
            SetObject(serialized, "battleAlertLabel", battleAlertLabel);
            SetObject(serialized, "battleAlertButton", battleAlert);
            SetObject(serialized, "monthlyNewsLabel", monthlyNews);
            SetObject(serialized, "castleManageButton", castleManage);
            SetObject(serialized, "objectiveButton", objective);
            SetObject(serialized, "endTurnButton", endTurn);
            SetObject(serialized, "endTurnLabel", endTurnLabel);
            SetObject(serialized, "systemButton", systemButton);
            SetArray(serialized.FindProperty("commandButtons"), commands);
            WIAdministrationShortcutAction[] actions = {
                WIAdministrationShortcutAction.Military, WIAdministrationShortcutAction.Heroes,
                WIAdministrationShortcutAction.Diplomacy, WIAdministrationShortcutAction.Scheme,
                WIAdministrationShortcutAction.Research, WIAdministrationShortcutAction.Faction,
                WIAdministrationShortcutAction.Council, WIAdministrationShortcutAction.MonthlyReport
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

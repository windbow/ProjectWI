using System.Collections.Generic;
using ProjectWI.Administration;
using UnityEditor;
using UnityEngine;

namespace ProjectWI.Editor
{
    /// <summary>
    /// 월드맵 위에서 성 좌표를 직접 배치하고 명시적으로 데이터베이스에 저장하는 편집 도구입니다.
    /// </summary>
    public class WICastleMapPlacementWindow : EditorWindow
    {
        private const string DatabasePath = "Assets/Data/ScriptableObject/Administration/WI_AdministrationDatabase.asset";
        private const float ToolbarHeight = 76f;
        private const float MarkerRadius = 9f;

        private readonly List<Vector2> workingPositions = new List<Vector2>();

        private WIAdministrationDatabaseSO database;
        private SerializedObject serializedDatabase;
        private SerializedProperty castlesProperty;
        private int selectedCastleIndex = -1;
        private int draggingCastleIndex = -1;
        private bool showConnections = true;
        private bool showNames = true;
        private bool syncCampaignOverrides = true;
        private bool hasUnsavedChanges;
        private string searchText = string.Empty;

        [MenuItem("ProjectWI/Tools/Castle Map Placement")]
        public static void OpenWindow()
        {
            WICastleMapPlacementWindow window = GetWindow<WICastleMapPlacementWindow>("성 지도 배치");
            window.minSize = new Vector2(900f, 620f);
        }

        // 창이 열릴 때 데이터베이스와 편집용 좌표 복사본을 불러옵니다.
        private void OnEnable()
        {
            LoadDatabase();
        }

        // 저장하지 않은 변경이 있을 때 실수로 창을 닫는 것을 방지합니다.
        private void OnDestroy()
        {
            if (hasUnsavedChanges == true)
            {
                Debug.LogWarning("[성 지도 배치] 저장하지 않은 좌표 변경은 데이터베이스에 반영되지 않았습니다.");
            }
        }

        // 데이터베이스의 현재 좌표를 작업용 메모리에 복사합니다.
        private void LoadDatabase()
        {
            database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            workingPositions.Clear();
            selectedCastleIndex = -1;
            draggingCastleIndex = -1;
            hasUnsavedChanges = false;

            if (database == null)
            {
                serializedDatabase = null;
                castlesProperty = null;
                return;
            }

            serializedDatabase = new SerializedObject(database);
            castlesProperty = serializedDatabase.FindProperty("castles");
            foreach (WICastleDefinition castle in database.Castles)
            {
                workingPositions.Add(castle.NormalizedMapPosition);
            }

            Repaint();
        }

        // 도구 설명과 저장 옵션을 그립니다.
        private void DrawToolbar()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Height(ToolbarHeight));
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("월드맵에서 성 마커를 드래그한 뒤 저장하세요.", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            GUI.enabled = hasUnsavedChanges;
            if (GUILayout.Button("변경 취소", GUILayout.Width(90f)))
            {
                LoadDatabase();
            }

            if (GUILayout.Button("좌표 저장", GUILayout.Width(100f)))
            {
                SavePositions();
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            showNames = GUILayout.Toggle(showNames, "성 이름", GUILayout.Width(70f));
            showConnections = GUILayout.Toggle(showConnections, "연결선", GUILayout.Width(70f));
            syncCampaignOverrides = GUILayout.Toggle(syncCampaignOverrides, "기존 캠페인 위치 덮어쓰기에도 동기화", GUILayout.Width(245f));
            GUILayout.Space(12f);
            GUILayout.Label("검색", GUILayout.Width(32f));
            searchText = EditorGUILayout.TextField(searchText, GUILayout.Width(150f));
            if (GUILayout.Button("찾기", GUILayout.Width(48f)))
            {
                SelectCastleBySearch();
            }
            GUILayout.FlexibleSpace();
            GUILayout.Label(hasUnsavedChanges ? "● 저장하지 않은 변경 있음" : "저장된 상태",
                hasUnsavedChanges ? EditorStyles.boldLabel : EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        // IMGUI 창과 지도 Sprite의 비율을 유지하는 지도 영역을 계산합니다.
        private Rect CalculateMapRect(Rect availableRect, Sprite mapSprite)
        {
            float sourceAspect = mapSprite.rect.width / mapSprite.rect.height;
            float targetAspect = availableRect.width / Mathf.Max(1f, availableRect.height);
            if (targetAspect > sourceAspect)
            {
                float width = availableRect.height * sourceAspect;
                return new Rect(availableRect.center.x - width * 0.5f, availableRect.y, width, availableRect.height);
            }

            float height = availableRect.width / sourceAspect;
            return new Rect(availableRect.x, availableRect.center.y - height * 0.5f, availableRect.width, height);
        }

        // Sprite가 아틀라스에 포함되어 있어도 올바른 영역만 지도에 표시합니다.
        private void DrawMap(Sprite mapSprite, Rect mapRect)
        {
            Texture2D texture = mapSprite.texture;
            Rect textureRect = mapSprite.textureRect;
            Rect uv = new Rect(
                textureRect.x / texture.width,
                textureRect.y / texture.height,
                textureRect.width / texture.width,
                textureRect.height / texture.height);
            GUI.DrawTextureWithTexCoords(mapRect, texture, uv, true);
        }

        // 정규화 좌표를 현재 지도 창의 픽셀 좌표로 변환합니다.
        private static Vector2 NormalizedToCanvas(Vector2 normalizedPosition, Rect mapRect)
        {
            return new Vector2(
                mapRect.x + normalizedPosition.x * mapRect.width,
                mapRect.y + normalizedPosition.y * mapRect.height);
        }

        // 현재 지도 창의 픽셀 좌표를 위쪽이 0인 정규화 좌표로 변환합니다.
        private static Vector2 CanvasToNormalized(Vector2 canvasPosition, Rect mapRect)
        {
            return new Vector2(
                Mathf.Clamp01((canvasPosition.x - mapRect.x) / mapRect.width),
                Mathf.Clamp01((canvasPosition.y - mapRect.y) / mapRect.height));
        }

        // 성 연결 관계를 현재 작업 좌표 기준으로 표시합니다.
        private void DrawConnections(Rect mapRect)
        {
            if (showConnections == false)
            {
                return;
            }

            Dictionary<string, int> indicesById = new Dictionary<string, int>();
            for (int index = 0; index < database.Castles.Count; index++)
            {
                indicesById[database.Castles[index].Id] = index;
            }

            Handles.BeginGUI();
            Handles.color = new Color(0.72f, 0.18f, 0.18f, 0.58f);
            for (int originIndex = 0; originIndex < database.Castles.Count; originIndex++)
            {
                WICastleDefinition origin = database.Castles[originIndex];
                foreach (string adjacentId in origin.AdjacentCastleIds)
                {
                    if (indicesById.TryGetValue(adjacentId, out int targetIndex) == false || targetIndex <= originIndex)
                    {
                        continue;
                    }

                    Handles.DrawAAPolyLine(2f,
                        NormalizedToCanvas(workingPositions[originIndex], mapRect),
                        NormalizedToCanvas(workingPositions[targetIndex], mapRect));
                }
            }
            Handles.EndGUI();
        }

        // 성 마커, 이름과 선택 상태를 지도 위에 표시합니다.
        private void DrawCastleMarkers(Rect mapRect)
        {
            for (int index = 0; index < database.Castles.Count; index++)
            {
                WICastleDefinition castle = database.Castles[index];
                Vector2 center = NormalizedToCanvas(workingPositions[index], mapRect);
                bool selected = index == selectedCastleIndex;
                Color markerColor = selected ? new Color(0.2f, 0.78f, 1f, 1f) : new Color(0.9f, 0.9f, 0.86f, 0.95f);

                Handles.BeginGUI();
                Handles.color = new Color(0.04f, 0.05f, 0.06f, 0.95f);
                Handles.DrawSolidDisc(center, Vector3.forward, selected ? MarkerRadius + 4f : MarkerRadius + 2f);
                Handles.color = markerColor;
                Handles.DrawSolidDisc(center, Vector3.forward, selected ? MarkerRadius + 1f : MarkerRadius);
                Handles.EndGUI();

                if (showNames == true)
                {
                    string castleName = castle.DisplayName != null ? castle.DisplayName.Korean : castle.Id;
                    GUIStyle labelStyle = new GUIStyle(EditorStyles.miniBoldLabel)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        normal = { textColor = selected ? Color.cyan : Color.white }
                    };
                    GUI.Label(new Rect(center.x - 55f, center.y + 9f, 110f, 18f), castleName, labelStyle);
                }
            }
        }

        // 마우스로 가장 가까운 마커를 선택하고 작업 좌표만 이동합니다.
        private void HandleMapInput(Rect mapRect)
        {
            Event current = Event.current;
            if (current.button != 0)
            {
                return;
            }

            if (current.type == EventType.MouseDown && mapRect.Contains(current.mousePosition) == true)
            {
                int nearestIndex = FindNearestCastle(current.mousePosition, mapRect);
                if (nearestIndex >= 0)
                {
                    selectedCastleIndex = nearestIndex;
                    draggingCastleIndex = nearestIndex;
                    current.Use();
                }
            }
            else if (current.type == EventType.MouseDrag && draggingCastleIndex >= 0)
            {
                workingPositions[draggingCastleIndex] = CanvasToNormalized(current.mousePosition, mapRect);
                hasUnsavedChanges = true;
                Repaint();
                current.Use();
            }
            else if (current.type == EventType.MouseUp && draggingCastleIndex >= 0)
            {
                draggingCastleIndex = -1;
                current.Use();
            }
        }

        // 클릭 위치에서 실제로 잡을 수 있는 가장 가까운 성을 찾습니다.
        private int FindNearestCastle(Vector2 mousePosition, Rect mapRect)
        {
            int nearestIndex = -1;
            float nearestDistance = 18f;
            for (int index = 0; index < workingPositions.Count; index++)
            {
                float distance = Vector2.Distance(mousePosition, NormalizedToCanvas(workingPositions[index], mapRect));
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestIndex = index;
                }
            }

            return nearestIndex;
        }

        // 한글 이름, 영문 이름 또는 ID로 성을 검색해 선택합니다.
        private void SelectCastleBySearch()
        {
            if (string.IsNullOrWhiteSpace(searchText) == true)
            {
                return;
            }

            string search = searchText.Trim().ToLowerInvariant();
            for (int index = 0; index < database.Castles.Count; index++)
            {
                WICastleDefinition castle = database.Castles[index];
                string korean = castle.DisplayName != null ? castle.DisplayName.Korean : string.Empty;
                string english = castle.DisplayName != null ? castle.DisplayName.English : string.Empty;
                if (castle.Id.ToLowerInvariant().Contains(search) == true ||
                    korean.ToLowerInvariant().Contains(search) == true ||
                    english.ToLowerInvariant().Contains(search) == true)
                {
                    selectedCastleIndex = index;
                    Repaint();
                    return;
                }
            }
        }

        // 작업 좌표를 마스터 데이터와 선택한 캠페인 위치 덮어쓰기에 한 번에 저장합니다.
        private void SavePositions()
        {
            if (database == null || serializedDatabase == null || castlesProperty == null)
            {
                return;
            }

            Undo.RecordObject(database, "성 지도 좌표 저장");
            serializedDatabase.Update();
            for (int index = 0; index < castlesProperty.arraySize && index < workingPositions.Count; index++)
            {
                SerializedProperty castleProperty = castlesProperty.GetArrayElementAtIndex(index);
                castleProperty.FindPropertyRelative("normalizedMapPosition").vector2Value = workingPositions[index];
            }

            if (syncCampaignOverrides == true)
            {
                SyncExistingCampaignOverrides();
            }

            serializedDatabase.ApplyModifiedProperties();
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            hasUnsavedChanges = false;
            ShowNotification(new GUIContent("성 좌표를 저장했습니다."));
        }

        // 기존에 위치 덮어쓰기를 사용하는 캠페인 항목만 새 마스터 좌표와 맞춥니다.
        private void SyncExistingCampaignOverrides()
        {
            Dictionary<string, Vector2> positionsById = new Dictionary<string, Vector2>();
            for (int index = 0; index < database.Castles.Count; index++)
            {
                positionsById[database.Castles[index].Id] = workingPositions[index];
            }

            SerializedProperty variantsProperty = serializedDatabase.FindProperty("campaignVariants");
            if (variantsProperty == null)
            {
                return;
            }

            for (int variantIndex = 0; variantIndex < variantsProperty.arraySize; variantIndex++)
            {
                SerializedProperty placementsProperty = variantsProperty.GetArrayElementAtIndex(variantIndex)
                    .FindPropertyRelative("castlePlacements");
                for (int placementIndex = 0; placementIndex < placementsProperty.arraySize; placementIndex++)
                {
                    SerializedProperty placement = placementsProperty.GetArrayElementAtIndex(placementIndex);
                    if (placement.FindPropertyRelative("overrideMapPosition").boolValue == false)
                    {
                        continue;
                    }

                    string castleId = placement.FindPropertyRelative("castleId").stringValue;
                    if (positionsById.TryGetValue(castleId, out Vector2 position) == true)
                    {
                        placement.FindPropertyRelative("normalizedMapPosition").vector2Value = position;
                    }
                }
            }
        }

        // 선택한 성의 정규화 좌표와 게임 UI 기준 로컬 좌표를 함께 보여줍니다.
        private void DrawSelectionInfo()
        {
            if (selectedCastleIndex < 0 || selectedCastleIndex >= database.Castles.Count)
            {
                return;
            }

            WICastleDefinition castle = database.Castles[selectedCastleIndex];
            Vector2 normalized = workingPositions[selectedCastleIndex];
            Vector2 local = new Vector2((normalized.x - 0.5f) * 1108.8f, (0.5f - normalized.y) * 838f);
            Rect infoRect = new Rect(10f, position.height - 42f, 430f, 30f);
            GUI.Box(infoRect, $"{castle.DisplayName.Korean} ({castle.Id})   정규화 {normalized.x:F4}, {normalized.y:F4}   UI {local.x:F1}, {local.y:F1}");
        }

        // 전체 편집 창을 그리고 드래그 입력을 처리합니다.
        private void OnGUI()
        {
            if (database == null || database.GlobalMapImage == null || workingPositions.Count == 0)
            {
                EditorGUILayout.HelpBox("월드맵 또는 성 데이터베이스를 불러올 수 없습니다.", MessageType.Error);
                if (GUILayout.Button("다시 불러오기", GUILayout.Width(120f)))
                {
                    LoadDatabase();
                }
                return;
            }

            DrawToolbar();
            Rect availableRect = new Rect(8f, ToolbarHeight + 10f, position.width - 16f, position.height - ToolbarHeight - 60f);
            Rect mapRect = CalculateMapRect(availableRect, database.GlobalMapImage);
            DrawMap(database.GlobalMapImage, mapRect);
            DrawConnections(mapRect);
            DrawCastleMarkers(mapRect);
            HandleMapInput(mapRect);
            DrawSelectionInfo();
        }
    }
}

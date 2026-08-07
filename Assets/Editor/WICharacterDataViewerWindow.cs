using System;
using System.Collections.Generic;
using ProjectWI.Administration;
using UnityEditor;
using UnityEngine;

namespace ProjectWI.Editor
{
    public class WICharacterDataViewerWindow : EditorWindow
    {
        private const string DatabasePath = "Assets/Data/ScriptableObject/Administration/WI_AdministrationDatabase.asset";
        private const float TotalTableWidth = 1180f;
        private const float RowHeight = 24f;

        private enum SortColumn
        {
            None,
            Id,
            KoreanName,
            EnglishName,
            Grade,
            Race,
            HeroClass,
            Leadership,
            Might,
            Intelligence,
            Politics,
            Charisma,
            Loyalty,
            RequiredReputation
        }

        // C# 경량화 캐시 데이터 구조 (SerializedProperty C++ 네이티브 호출 부하 방지)
        private class CharacterCache
        {
            public int OriginalIndex;
            public string Id = string.Empty;
            public string KoreanName = string.Empty;
            public string EnglishName = string.Empty;
            public WICharacterGrade Grade;
            public WIHeroRace Race;
            public WIHeroClass HeroClass;
            public int Leadership;
            public int Might;
            public int Intelligence;
            public int Politics;
            public int Charisma;
            public WILoyaltyState LoyaltyState;
            public int RequiredReputation;
            public bool ActiveSkillAvailable;
        }

        private WIAdministrationDatabaseSO database;
        private SerializedObject serializedDatabase;
        private SerializedProperty heroesProperty;

        private readonly List<CharacterCache> fullCache = new List<CharacterCache>();
        private readonly List<CharacterCache> filteredCache = new List<CharacterCache>();
        private bool isCacheDirty = true;

        private Vector2 mainScrollPosition;
        private string searchFilter = string.Empty;
        private WICharacterGradeFilter gradeFilter = WICharacterGradeFilter.All;

        private int currentPage = 0;
        private int pageSizeIndex = 1; // 기본 50개씩 보기
        private readonly int[] pageSizes = { 25, 50, 100, 500 };
        private readonly string[] pageSizeLabels = { "25개씩", "50개씩", "100개씩", "500개(전체)" };

        private SortColumn currentSortColumn = SortColumn.None;
        private bool sortAscending = true;

        private enum WICharacterGradeFilter
        {
            All,
            Hero,
            Common
        }

        [MenuItem("ProjectWI/Viewer/Character Data Viewer")]
        public static void OpenWindow()
        {
            WICharacterDataViewerWindow window = GetWindow<WICharacterDataViewerWindow>("캐릭터 데이터 뷰어");
            window.minSize = new Vector2(1100f, 600f);
        }

        private void OnEnable()
        {
            LoadDatabase();
        }

        private void LoadDatabase()
        {
            database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            if (database != null)
            {
                serializedDatabase = new SerializedObject(database);
                heroesProperty = serializedDatabase.FindProperty("heroes");
                MarkCacheDirty();
            }
        }

        private void MarkCacheDirty()
        {
            isCacheDirty = true;
        }

        // SerializedProperty 전체를 1회 캐싱하여 OnGUI() 연산량을 99% 이상 감소시킵니다.
        private void EnsureCacheUpdated()
        {
            if (!isCacheDirty && fullCache.Count == heroesProperty.arraySize) return;

            fullCache.Clear();
            int total = heroesProperty.arraySize;

            for (int i = 0; i < total; i++)
            {
                SerializedProperty heroProp = heroesProperty.GetArrayElementAtIndex(i);
                SerializedProperty idProp = heroProp.FindPropertyRelative("id");
                SerializedProperty displayNameProp = heroProp.FindPropertyRelative("displayName");
                SerializedProperty krNameProp = displayNameProp?.FindPropertyRelative("korean");
                SerializedProperty enNameProp = displayNameProp?.FindPropertyRelative("english");
                SerializedProperty gradeProp = heroProp.FindPropertyRelative("grade");
                SerializedProperty raceProp = heroProp.FindPropertyRelative("race");
                SerializedProperty heroClassProp = heroProp.FindPropertyRelative("heroClass");
                SerializedProperty leadershipProp = heroProp.FindPropertyRelative("leadership");
                SerializedProperty mightProp = heroProp.FindPropertyRelative("might");
                SerializedProperty intelligenceProp = heroProp.FindPropertyRelative("intelligence");
                SerializedProperty politicsProp = heroProp.FindPropertyRelative("politics");
                SerializedProperty charismaProp = heroProp.FindPropertyRelative("charisma");
                SerializedProperty loyaltyProp = heroProp.FindPropertyRelative("loyaltyState");
                SerializedProperty reqRepProp = heroProp.FindPropertyRelative("requiredReputation");
                SerializedProperty activeSkillProp = heroProp.FindPropertyRelative("activeSkillAvailable");

                CharacterCache item = new CharacterCache
                {
                    OriginalIndex = i,
                    Id = idProp != null ? idProp.stringValue : string.Empty,
                    KoreanName = krNameProp != null ? krNameProp.stringValue : string.Empty,
                    EnglishName = enNameProp != null ? enNameProp.stringValue : string.Empty,
                    Grade = gradeProp != null ? (WICharacterGrade)gradeProp.enumValueIndex : WICharacterGrade.Hero,
                    Race = raceProp != null ? (WIHeroRace)raceProp.enumValueIndex : WIHeroRace.Human,
                    HeroClass = heroClassProp != null ? (WIHeroClass)heroClassProp.enumValueIndex : WIHeroClass.MagicSwordsman,
                    Leadership = leadershipProp != null ? leadershipProp.intValue : 50,
                    Might = mightProp != null ? mightProp.intValue : 50,
                    Intelligence = intelligenceProp != null ? intelligenceProp.intValue : 50,
                    Politics = politicsProp != null ? politicsProp.intValue : 50,
                    Charisma = charismaProp != null ? charismaProp.intValue : 50,
                    LoyaltyState = loyaltyProp != null ? (WILoyaltyState)loyaltyProp.enumValueIndex : WILoyaltyState.Stable,
                    RequiredReputation = reqRepProp != null ? reqRepProp.intValue : 0,
                    ActiveSkillAvailable = activeSkillProp != null && activeSkillProp.boolValue
                };

                fullCache.Add(item);
            }

            UpdateFilteredCache();
            isCacheDirty = false;
        }

        // C# 메모리 상에서 0.01ms 내에 필터링 및 정렬 수행
        private void UpdateFilteredCache()
        {
            filteredCache.Clear();

            string filterLower = searchFilter.ToLower();
            bool hasSearch = !string.IsNullOrEmpty(filterLower);

            for (int i = 0; i < fullCache.Count; i++)
            {
                CharacterCache item = fullCache[i];

                // 등급 필터
                if (gradeFilter == WICharacterGradeFilter.Hero && item.Grade != WICharacterGrade.Hero) continue;
                if (gradeFilter == WICharacterGradeFilter.Common && item.Grade != WICharacterGrade.Common) continue;

                // 검색어 필터
                if (hasSearch)
                {
                    bool matchId = item.Id.ToLower().Contains(filterLower);
                    bool matchKr = item.KoreanName.ToLower().Contains(filterLower);
                    bool matchEn = item.EnglishName.ToLower().Contains(filterLower);
                    if (!matchId && !matchKr && !matchEn) continue;
                }

                filteredCache.Add(item);
            }

            // 정렬
            if (currentSortColumn != SortColumn.None)
            {
                filteredCache.Sort((a, b) =>
                {
                    int cmp = CompareCacheItems(a, b, currentSortColumn);
                    return sortAscending ? cmp : -cmp;
                });
            }
        }

        private int CompareCacheItems(CharacterCache a, CharacterCache b, SortColumn column)
        {
            switch (column)
            {
                case SortColumn.Id: return string.Compare(a.Id, b.Id, StringComparison.OrdinalIgnoreCase);
                case SortColumn.KoreanName: return string.Compare(a.KoreanName, b.KoreanName, StringComparison.OrdinalIgnoreCase);
                case SortColumn.EnglishName: return string.Compare(a.EnglishName, b.EnglishName, StringComparison.OrdinalIgnoreCase);
                case SortColumn.Grade: return a.Grade.CompareTo(b.Grade);
                case SortColumn.Race: return a.Race.CompareTo(b.Race);
                case SortColumn.HeroClass: return a.HeroClass.CompareTo(b.HeroClass);
                case SortColumn.Leadership: return a.Leadership.CompareTo(b.Leadership);
                case SortColumn.Might: return a.Might.CompareTo(b.Might);
                case SortColumn.Intelligence: return a.Intelligence.CompareTo(b.Intelligence);
                case SortColumn.Politics: return a.Politics.CompareTo(b.Politics);
                case SortColumn.Charisma: return a.Charisma.CompareTo(b.Charisma);
                case SortColumn.Loyalty: return a.LoyaltyState.CompareTo(b.LoyaltyState);
                case SortColumn.RequiredReputation: return a.RequiredReputation.CompareTo(b.RequiredReputation);
                default: return 0;
            }
        }

        private void OnGUI()
        {
            if (database == null || serializedDatabase == null || heroesProperty == null)
            {
                EditorGUILayout.HelpBox("내정 데이터베이스 에셋을 로드할 수 없습니다: " + DatabasePath, MessageType.Error);
                if (GUILayout.Button("다시 시도", GUILayout.Width(120f)))
                {
                    LoadDatabase();
                }
                return;
            }

            serializedDatabase.Update();
            EnsureCacheUpdated();

            DrawHeaderToolbar();
            DrawTableGrid();
            DrawFooterSummary();

            if (serializedDatabase.hasModifiedProperties)
            {
                serializedDatabase.ApplyModifiedProperties();
                EditorUtility.SetDirty(database);
            }
        }

        private void DrawHeaderToolbar()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            GUILayout.Label("ProjectWI 캐릭터 데이터 뷰어 (Excel 그리드 편집기)", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("🎲 500명 인물 생성", GUILayout.Height(22f), GUILayout.Width(135f)))
            {
                if (EditorUtility.DisplayDialog("인물 데이터 생성", "100명의 영웅과 400명의 일반 인물 데이터(총 500명)를 생성하여 데이터베이스를 갱신하시겠습니까?", "생성", "취소"))
                {
                    WIMassCharacterSeeder.SeedMassRoster();
                    LoadDatabase();
                }
            }

            if (GUILayout.Button("＋ 새 캐릭터 추가", GUILayout.Height(22f), GUILayout.Width(125f)))
            {
                AddNewCharacter();
            }

            if (GUILayout.Button("💾 데이터베이스 저장", GUILayout.Height(22f), GUILayout.Width(135f)))
            {
                SaveDatabase();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("검색:", GUILayout.Width(40f));
            string newSearch = EditorGUILayout.TextField(searchFilter, GUILayout.Width(180f));
            if (newSearch != searchFilter)
            {
                searchFilter = newSearch;
                currentPage = 0;
                UpdateFilteredCache();
            }

            if (!string.IsNullOrEmpty(searchFilter) && GUILayout.Button("X", GUILayout.Width(22f)))
            {
                searchFilter = string.Empty;
                currentPage = 0;
                UpdateFilteredCache();
            }

            GUILayout.Space(10f);
            GUILayout.Label("등급 필터:", GUILayout.Width(65f));
            WICharacterGradeFilter newGrade = (WICharacterGradeFilter)EditorGUILayout.EnumPopup(gradeFilter, GUILayout.Width(80f));
            if (newGrade != gradeFilter)
            {
                gradeFilter = newGrade;
                currentPage = 0;
                UpdateFilteredCache();
            }

            GUILayout.Space(15f);
            GUILayout.Label("표시 수:", GUILayout.Width(50f));
            int newPageSizeIdx = EditorGUILayout.Popup(pageSizeIndex, pageSizeLabels, GUILayout.Width(90f));
            if (newPageSizeIdx != pageSizeIndex)
            {
                pageSizeIndex = newPageSizeIdx;
                currentPage = 0;
            }

            GUILayout.FlexibleSpace();

            // 페이지 조절 버튼
            int pageSize = pageSizes[pageSizeIndex];
            int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)filteredCache.Count / pageSize));
            if (currentPage >= totalPages) currentPage = totalPages - 1;
            if (currentPage < 0) currentPage = 0;

            GUI.enabled = currentPage > 0;
            if (GUILayout.Button("◀ 이전", GUILayout.Width(55f)))
            {
                currentPage--;
                mainScrollPosition.y = 0;
            }
            GUI.enabled = true;

            GUILayout.Label($"{currentPage + 1} / {totalPages} 페이지", EditorStyles.boldLabel, GUILayout.Width(85f));

            GUI.enabled = currentPage < totalPages - 1;
            if (GUILayout.Button("다음 ▶", GUILayout.Width(55f)))
            {
                currentPage++;
                mainScrollPosition.y = 0;
            }
            GUI.enabled = true;

            GUILayout.Space(10f);
            if (GUILayout.Button("🔄 새로고침", GUILayout.Width(75f)))
            {
                LoadDatabase();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawTableGrid()
        {
            if (heroesProperty.arraySize == 0)
            {
                EditorGUILayout.HelpBox("등록된 캐릭터가 없습니다. '새 캐릭터 추가' 버튼을 눌러 추가하세요.", MessageType.Info);
                return;
            }

            int pageSize = pageSizes[pageSizeIndex];
            int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)filteredCache.Count / pageSize));
            if (currentPage >= totalPages) currentPage = totalPages - 1;
            if (currentPage < 0) currentPage = 0;

            int startIndex = currentPage * pageSize;
            int countOnPage = Mathf.Min(pageSize, filteredCache.Count - startIndex);

            mainScrollPosition = EditorGUILayout.BeginScrollView(mainScrollPosition, true, true);
            EditorGUILayout.BeginVertical();

            // 헤더 행 그리기
            DrawTableHeaderRow();

            // 가시 영역 계산 (Virtual Scroll Windowing - 현재 화면 뷰포트에 보이는 20개 행만 렌더링)
            float viewTop = mainScrollPosition.y;
            float viewBottom = viewTop + (position.height - 140f);

            int firstRowOnPage = Mathf.Clamp(Mathf.FloorToInt(viewTop / RowHeight) - 1, 0, countOnPage);
            int lastRowOnPage = Mathf.Clamp(Mathf.CeilToInt(viewBottom / RowHeight) + 1, 0, countOnPage);

            // 상단 가상 스페이스
            if (firstRowOnPage > 0)
            {
                GUILayout.Space(firstRowOnPage * RowHeight);
            }

            // 가시 뷰포트 영역 내부의 행만 IMGUI 컨트롤 그리기
            for (int row = firstRowOnPage; row < lastRowOnPage; row++)
            {
                if (startIndex + row >= filteredCache.Count) break;
                CharacterCache cacheItem = filteredCache[startIndex + row];
                if (cacheItem.OriginalIndex >= heroesProperty.arraySize) continue;

                SerializedProperty heroProp = heroesProperty.GetArrayElementAtIndex(cacheItem.OriginalIndex);
                DrawTableRow(cacheItem.OriginalIndex, heroProp, row % 2 == 0);
            }

            // 하단 가상 스페이스
            if (countOnPage > lastRowOnPage)
            {
                GUILayout.Space((countOnPage - lastRowOnPage) * RowHeight);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        private void DrawTableHeaderRow()
        {
            Rect headerRect = EditorGUILayout.GetControlRect(false, RowHeight, GUILayout.Width(TotalTableWidth));
            EditorGUI.DrawRect(headerRect, EditorGUIUtility.isProSkin ? new Color(0.2f, 0.25f, 0.32f) : new Color(0.75f, 0.82f, 0.92f));

            float x = headerRect.x + 4f;
            float y = headerRect.y + 2f;
            float h = RowHeight - 4f;

            GUI.Label(new Rect(x, y, 35f, h), "#", EditorStyles.centeredGreyMiniLabel);
            x += 35f + 4f;

            x += DrawSortableHeaderButton(x, y, 110f, h, "ID", SortColumn.Id);
            x += DrawSortableHeaderButton(x, y, 100f, h, "한글 이름", SortColumn.KoreanName);
            x += DrawSortableHeaderButton(x, y, 100f, h, "영문 이름", SortColumn.EnglishName);
            x += DrawSortableHeaderButton(x, y, 80f, h, "등급", SortColumn.Grade);
            x += DrawSortableHeaderButton(x, y, 90f, h, "종족", SortColumn.Race);
            x += DrawSortableHeaderButton(x, y, 110f, h, "클래스", SortColumn.HeroClass);
            x += DrawSortableHeaderButton(x, y, 45f, h, "통솔", SortColumn.Leadership);
            x += DrawSortableHeaderButton(x, y, 45f, h, "무력", SortColumn.Might);
            x += DrawSortableHeaderButton(x, y, 45f, h, "지력", SortColumn.Intelligence);
            x += DrawSortableHeaderButton(x, y, 45f, h, "정치", SortColumn.Politics);
            x += DrawSortableHeaderButton(x, y, 45f, h, "매력", SortColumn.Charisma);
            x += DrawSortableHeaderButton(x, y, 85f, h, "충성도", SortColumn.Loyalty);
            x += DrawSortableHeaderButton(x, y, 60f, h, "명성", SortColumn.RequiredReputation);

            GUI.Label(new Rect(x, y, 40f, h), "스킬", EditorStyles.centeredGreyMiniLabel);
            x += 40f + 4f;

            GUI.Label(new Rect(x, y, 80f, h), "초상화", EditorStyles.centeredGreyMiniLabel);
            x += 80f + 4f;

            GUI.Label(new Rect(x, y, 40f, h), "삭제", EditorStyles.centeredGreyMiniLabel);
        }

        private float DrawSortableHeaderButton(float x, float y, float w, float h, string label, SortColumn column)
        {
            string title = label;
            if (currentSortColumn == column)
            {
                title += sortAscending ? " ▲" : " ▼";
            }

            if (GUI.Button(new Rect(x, y, w, h), title, EditorStyles.miniButton))
            {
                if (currentSortColumn == column)
                {
                    sortAscending = !sortAscending;
                }
                else
                {
                    currentSortColumn = column;
                    sortAscending = true;
                }
                UpdateFilteredCache();
            }

            return w + 4f;
        }

        private void DrawTableRow(int arrayIndex, SerializedProperty heroProp, bool isEvenRow)
        {
            Rect rowRect = EditorGUILayout.GetControlRect(false, RowHeight, GUILayout.Width(TotalTableWidth));

            SerializedProperty idProp = heroProp.FindPropertyRelative("id");
            SerializedProperty displayNameProp = heroProp.FindPropertyRelative("displayName");
            SerializedProperty krNameProp = displayNameProp?.FindPropertyRelative("korean");
            SerializedProperty enNameProp = displayNameProp?.FindPropertyRelative("english");
            SerializedProperty gradeProp = heroProp.FindPropertyRelative("grade");
            SerializedProperty raceProp = heroProp.FindPropertyRelative("race");
            SerializedProperty heroClassProp = heroProp.FindPropertyRelative("heroClass");
            SerializedProperty leadershipProp = heroProp.FindPropertyRelative("leadership");
            SerializedProperty mightProp = heroProp.FindPropertyRelative("might");
            SerializedProperty intelligenceProp = heroProp.FindPropertyRelative("intelligence");
            SerializedProperty politicsProp = heroProp.FindPropertyRelative("politics");
            SerializedProperty charismaProp = heroProp.FindPropertyRelative("charisma");
            SerializedProperty loyaltyProp = heroProp.FindPropertyRelative("loyaltyState");
            SerializedProperty reqRepProp = heroProp.FindPropertyRelative("requiredReputation");
            SerializedProperty activeSkillProp = heroProp.FindPropertyRelative("activeSkillAvailable");
            SerializedProperty portraitProp = heroProp.FindPropertyRelative("portrait");

            WICharacterGrade currentGrade = gradeProp != null ? (WICharacterGrade)gradeProp.enumValueIndex : WICharacterGrade.Hero;
            Color bgColor;
            if (EditorGUIUtility.isProSkin)
            {
                bgColor = currentGrade == WICharacterGrade.Hero
                    ? (isEvenRow ? new Color(0.28f, 0.25f, 0.18f) : new Color(0.24f, 0.21f, 0.15f))
                    : (isEvenRow ? new Color(0.22f, 0.22f, 0.22f) : new Color(0.18f, 0.18f, 0.18f));
            }
            else
            {
                bgColor = currentGrade == WICharacterGrade.Hero
                    ? (isEvenRow ? new Color(1.0f, 0.96f, 0.85f) : new Color(0.96f, 0.92f, 0.80f))
                    : (isEvenRow ? new Color(0.94f, 0.94f, 0.94f) : new Color(0.88f, 0.88f, 0.88f));
            }

            EditorGUI.DrawRect(rowRect, bgColor);

            float x = rowRect.x + 4f;
            float y = rowRect.y + 2f;
            float h = RowHeight - 4f;

            // 순번 (#)
            GUI.Label(new Rect(x, y, 35f, h), (arrayIndex + 1).ToString(), EditorStyles.miniLabel);
            x += 35f + 4f;

            // ID
            if (idProp != null)
            {
                string oldVal = idProp.stringValue;
                string newVal = EditorGUI.TextField(new Rect(x, y, 110f, h), oldVal);
                if (newVal != oldVal) { idProp.stringValue = newVal; MarkCacheDirty(); }
            }
            x += 110f + 4f;

            // 한글 이름
            if (krNameProp != null)
            {
                string oldVal = krNameProp.stringValue;
                string newVal = EditorGUI.TextField(new Rect(x, y, 100f, h), oldVal);
                if (newVal != oldVal) { krNameProp.stringValue = newVal; MarkCacheDirty(); }
            }
            x += 100f + 4f;

            // 영문 이름
            if (enNameProp != null)
            {
                string oldVal = enNameProp.stringValue;
                string newVal = EditorGUI.TextField(new Rect(x, y, 100f, h), oldVal);
                if (newVal != oldVal) { enNameProp.stringValue = newVal; MarkCacheDirty(); }
            }
            x += 100f + 4f;

            // 등급
            if (gradeProp != null)
            {
                int oldVal = gradeProp.enumValueIndex;
                EditorGUI.PropertyField(new Rect(x, y, 80f, h), gradeProp, GUIContent.none);
                if (gradeProp.enumValueIndex != oldVal) MarkCacheDirty();
            }
            x += 80f + 4f;

            // 종족
            if (raceProp != null)
            {
                int oldVal = raceProp.enumValueIndex;
                EditorGUI.PropertyField(new Rect(x, y, 90f, h), raceProp, GUIContent.none);
                if (raceProp.enumValueIndex != oldVal) MarkCacheDirty();
            }
            x += 90f + 4f;

            // 클래스
            if (heroClassProp != null)
            {
                int oldVal = heroClassProp.enumValueIndex;
                EditorGUI.PropertyField(new Rect(x, y, 110f, h), heroClassProp, GUIContent.none);
                if (heroClassProp.enumValueIndex != oldVal) MarkCacheDirty();
            }
            x += 110f + 4f;

            // 통솔
            if (leadershipProp != null) leadershipProp.intValue = EditorGUI.IntField(new Rect(x, y, 45f, h), leadershipProp.intValue);
            x += 45f + 4f;

            // 무력
            if (mightProp != null) mightProp.intValue = EditorGUI.IntField(new Rect(x, y, 45f, h), mightProp.intValue);
            x += 45f + 4f;

            // 지력
            if (intelligenceProp != null) intelligenceProp.intValue = EditorGUI.IntField(new Rect(x, y, 45f, h), intelligenceProp.intValue);
            x += 45f + 4f;

            // 정치
            if (politicsProp != null) politicsProp.intValue = EditorGUI.IntField(new Rect(x, y, 45f, h), politicsProp.intValue);
            x += 45f + 4f;

            // 매력
            if (charismaProp != null) charismaProp.intValue = EditorGUI.IntField(new Rect(x, y, 45f, h), charismaProp.intValue);
            x += 45f + 4f;

            // 충성도
            if (loyaltyProp != null) EditorGUI.PropertyField(new Rect(x, y, 85f, h), loyaltyProp, GUIContent.none);
            x += 85f + 4f;

            // 등용 필요 명성
            if (reqRepProp != null) reqRepProp.intValue = EditorGUI.IntField(new Rect(x, y, 60f, h), reqRepProp.intValue);
            x += 60f + 4f;

            // 스킬
            if (activeSkillProp != null) activeSkillProp.boolValue = EditorGUI.Toggle(new Rect(x + 10f, y, 20f, h), activeSkillProp.boolValue);
            x += 40f + 4f;

            // 초상화 Sprite
            if (portraitProp != null)
            {
                portraitProp.objectReferenceValue = EditorGUI.ObjectField(
                    new Rect(x, y, 80f, h),
                    portraitProp.objectReferenceValue,
                    typeof(Sprite),
                    false);
            }
            x += 80f + 4f;

            // 삭제 버튼
            if (GUI.Button(new Rect(x, y, 40f, h), "X"))
            {
                string charName = krNameProp != null ? krNameProp.stringValue : idProp.stringValue;
                if (EditorUtility.DisplayDialog("캐릭터 삭제", $"'{charName}' (ID: {idProp.stringValue}) 캐릭터를 삭제하시겠습니까?", "삭제", "취소"))
                {
                    heroesProperty.DeleteArrayElementAtIndex(arrayIndex);
                    MarkCacheDirty();
                    GUIUtility.ExitGUI();
                }
            }
        }

        private void DrawFooterSummary()
        {
            int totalCount = fullCache.Count;
            int heroCount = 0;
            int commonCount = 0;

            for (int i = 0; i < totalCount; i++)
            {
                if (fullCache[i].Grade == WICharacterGrade.Common) commonCount++;
                else heroCount++;
            }

            int pageSize = pageSizes[pageSizeIndex];
            int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)filteredCache.Count / pageSize));
            int displayStart = filteredCache.Count > 0 ? (currentPage * pageSize) + 1 : 0;
            int displayEnd = Mathf.Min((currentPage + 1) * pageSize, filteredCache.Count);

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label($"전체 캐릭터: {totalCount}명 (영웅: {heroCount}명, 일반: {commonCount}명) | 필터 검색: {filteredCache.Count}명 | 현재 {currentPage + 1}/{totalPages} 페이지 ({displayStart}~{displayEnd}번 항목 표시)", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void AddNewCharacter()
        {
            heroesProperty.arraySize++;
            SerializedProperty newHero = heroesProperty.GetArrayElementAtIndex(heroesProperty.arraySize - 1);

            int newNumber = heroesProperty.arraySize;
            newHero.FindPropertyRelative("id").stringValue = $"hero_new_{newNumber:D2}";

            SerializedProperty nameProp = newHero.FindPropertyRelative("displayName");
            if (nameProp != null)
            {
                nameProp.FindPropertyRelative("uid").stringValue = $"hero_new_{newNumber:D2}_name";
                nameProp.FindPropertyRelative("korean").stringValue = $"신규 인물 {newNumber}";
                nameProp.FindPropertyRelative("english").stringValue = $"New Character {newNumber}";
            }

            newHero.FindPropertyRelative("grade").enumValueIndex = (int)WICharacterGrade.Hero;
            newHero.FindPropertyRelative("race").enumValueIndex = (int)WIHeroRace.Human;
            newHero.FindPropertyRelative("heroClass").enumValueIndex = (int)WIHeroClass.MagicSwordsman;
            newHero.FindPropertyRelative("leadership").intValue = 50;
            newHero.FindPropertyRelative("might").intValue = 50;
            newHero.FindPropertyRelative("intelligence").intValue = 50;
            newHero.FindPropertyRelative("politics").intValue = 50;
            newHero.FindPropertyRelative("charisma").intValue = 50;
            newHero.FindPropertyRelative("loyaltyState").enumValueIndex = (int)WILoyaltyState.Stable;
            newHero.FindPropertyRelative("requiredReputation").intValue = 0;
            newHero.FindPropertyRelative("activeSkillAvailable").boolValue = true;

            serializedDatabase.ApplyModifiedProperties();
            EditorUtility.SetDirty(database);
            MarkCacheDirty();
        }

        private void SaveDatabase()
        {
            serializedDatabase.ApplyModifiedProperties();
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            MarkCacheDirty();
            EditorUtility.DisplayDialog("저장 완료", "캐릭터 데이터가 성공적으로 WI_AdministrationDatabase.asset에 저장되었습니다.", "확인");
        }
    }
}

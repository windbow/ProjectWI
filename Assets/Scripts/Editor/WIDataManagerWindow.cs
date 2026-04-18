#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using ProjectWI.Data;

namespace ProjectWI.Editor
{
    public enum WIDataType
    {
        Dungeon,
        DungeonEvent,
        Job,
        Skill,
        Monster
    }
    
    public class WIDataManagerWindow : EditorWindow
    {
        private int selectedTab = 0;
        private string[] tabNames = new string[] { "Dungeon Data", "Dungeon Events", "Job Data", "Skill Data", "Monster Data" };
        private Vector2 scrollPos;

        // 에셋 캐시 리스트
        private List<WIDungeonDataSO> dungeons = new List<WIDungeonDataSO>();
        private List<WIDungeonEventSO> dungeonEvents = new List<WIDungeonEventSO>();
        private List<WIJobDataSO> jobs = new List<WIJobDataSO>();
        private List<WISkillDataSO> skills = new List<WISkillDataSO>();
        private List<WIMonsterDataSO> monsters = new List<WIMonsterDataSO>();

        [MenuItem("WI Tools/통합 데이터 관리자 (Table Viewer)")]
        public static void ShowWindow()
        {
            GetWindow<WIDataManagerWindow>("통합 데이터 관리자").Show();
        }

        private void OnEnable()
        {
            ReloadData();
        }

        private void ReloadData()
        {
            dungeonEvents.Clear();
            monsters.Clear();
            jobs.Clear();
            dungeons.Clear();
            skills.Clear();

            // Load Dungeon Events
            string[] eventGuids = AssetDatabase.FindAssets("t:WIDungeonEventSO");
            foreach (string guid in eventGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                dungeonEvents.Add(AssetDatabase.LoadAssetAtPath<WIDungeonEventSO>(path));
            }
            
            // Load Dungeons
            string[] dungeonGuids = AssetDatabase.FindAssets("t:WIDungeonDataSO");
            foreach (string guid in dungeonGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                dungeons.Add(AssetDatabase.LoadAssetAtPath<WIDungeonDataSO>(path));
            }

            // Load Jobs
            string[] jobGuids = AssetDatabase.FindAssets("t:WIJobDataSO");
            foreach (string guid in jobGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                jobs.Add(AssetDatabase.LoadAssetAtPath<WIJobDataSO>(path));
            }
            
            // Load Skills
            string[] skillGuids = AssetDatabase.FindAssets("t:WISkillDataSO");
            foreach (string guid in skillGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                skills.Add(AssetDatabase.LoadAssetAtPath<WISkillDataSO>(path));
            }
            
            // Load Monsters
            string[] monsterGuids = AssetDatabase.FindAssets("t:WIMonsterDataSO");
            foreach (string guid in monsterGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                monsters.Add(AssetDatabase.LoadAssetAtPath<WIMonsterDataSO>(path));
            }
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            
            // 상단 툴바 및 새로고침 버튼
            GUILayout.BeginHorizontal();
            int newTab = GUILayout.Toolbar(selectedTab, tabNames, GUILayout.Height(30));
            WIDataType dataType = (WIDataType)newTab;
            if (newTab != selectedTab)
            {
                selectedTab = newTab;
                GUI.FocusControl(null); // 입력 포커스 해제
            }
            
            if (GUILayout.Button("새로고침", GUILayout.Width(80), GUILayout.Height(30)))
            {
                ReloadData();
                GUI.FocusControl(null);
            }
            GUILayout.EndHorizontal();
            
            GUILayout.Space(10);

            scrollPos = GUILayout.BeginScrollView(scrollPos);

            switch (dataType)
            {
                case WIDataType.Dungeon:
                    DrawDungeonDataGrid();
                    break;
                case WIDataType.DungeonEvent:
                    DrawDungeonEventsGrid();
                    break;
                case WIDataType.Job:
                    DrawJobsGrid();
                    break;
                case WIDataType.Skill:
                    DrawSkillsGrid();
                    break;
                case WIDataType.Monster:
                    DrawMonstersGrid();
                    break;
            }

            GUILayout.EndScrollView();

            GUILayout.Space(10);
            if (GUILayout.Button("선택된 타입의 임시 데이터 하나 강제 생성하기", GUILayout.Height(30)))
            {
                CreateDummyData(dataType);
            }
        }

        // ===========================================
        // Dungeon Data Grid
        // ===========================================
        private void DrawDungeonDataGrid()
        {
            GUILayout.BeginHorizontal("box");
            GUILayout.Label("Asset Name", EditorStyles.boldLabel, GUILayout.Width(150));
            GUILayout.Label("Dungeon ID", EditorStyles.boldLabel, GUILayout.Width(80));
            GUILayout.Label("Dungeon Name", EditorStyles.boldLabel, GUILayout.Width(200));
            GUILayout.Label("Events (Items)", EditorStyles.boldLabel, GUILayout.Width(100)); // List count only mapping
            GUILayout.EndHorizontal();

            foreach (var so in dungeons)
            {
                if (so == null) continue;

                EditorGUI.BeginChangeCheck();
                GUILayout.BeginHorizontal();

                if (GUILayout.Button(so.name, EditorStyles.label, GUILayout.Width(150)))
                {
                    Selection.activeObject = so;
                }

                so.dungeonId = EditorGUILayout.IntField(so.dungeonId, GUILayout.Width(80));
                so.dungeonName = EditorGUILayout.TextField(so.dungeonName, GUILayout.Width(200));
                
                // Show count of events
                int eventCount = so.eventPool != null ? so.eventPool.Count : 0;
                GUILayout.Label($"({eventCount} Events)", GUILayout.Width(100));

                GUILayout.EndHorizontal();

                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(so);
                }
            }
        }
        
        // ===========================================
        // Dungeon Events Grid
        // ===========================================
        private void DrawDungeonEventsGrid()
        {
            // 헤더
            GUILayout.BeginHorizontal("box");
            GUILayout.Label("Asset Name", EditorStyles.boldLabel, GUILayout.Width(150));
            GUILayout.Label("ID", EditorStyles.boldLabel, GUILayout.Width(80));
            GUILayout.Label("Event Type", EditorStyles.boldLabel, GUILayout.Width(120));
            GUILayout.Label("Log Message", EditorStyles.boldLabel, GUILayout.Width(300));
            GUILayout.Label("Weight", EditorStyles.boldLabel, GUILayout.Width(60));
            GUILayout.Label("R.Gold", EditorStyles.boldLabel, GUILayout.Width(60));
            GUILayout.Label("R.Exp", EditorStyles.boldLabel, GUILayout.Width(60));
            GUILayout.EndHorizontal();

            // 데이터 표기
            foreach (var so in dungeonEvents)
            {
                if (so == null) continue;

                EditorGUI.BeginChangeCheck();
                GUILayout.BeginHorizontal();

                // 텍스트를 누르면 프로젝트 창에서 에셋 선택됨
                if (GUILayout.Button(so.name, EditorStyles.label, GUILayout.Width(150)))
                {
                    Selection.activeObject = so;
                }

                so.id = EditorGUILayout.TextField(so.id, GUILayout.Width(80));
                so.eventType = (WIDungeonEventType)EditorGUILayout.EnumPopup(so.eventType, GUILayout.Width(120));
                so.logMessage = EditorGUILayout.TextField(so.logMessage, GUILayout.Width(300));
                so.weight = EditorGUILayout.IntField(so.weight, GUILayout.Width(60));
                so.rewardGold = EditorGUILayout.IntField(so.rewardGold, GUILayout.Width(60));
                so.rewardExp = EditorGUILayout.IntField(so.rewardExp, GUILayout.Width(60));

                GUILayout.EndHorizontal();

                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(so); // 변경사항 감지하여 저장 대기 처리
                }
            }
        }

        // ===========================================
        // Job Data Grid
        // ===========================================
        private void DrawJobsGrid()
        {
            GUILayout.BeginHorizontal("box");
            GUILayout.Label("Asset Name", EditorStyles.boldLabel, GUILayout.Width(150));
            GUILayout.Label("ID", EditorStyles.boldLabel, GUILayout.Width(80));
            GUILayout.Label("Job Name", EditorStyles.boldLabel, GUILayout.Width(120));
            GUILayout.Label("Description", EditorStyles.boldLabel, GUILayout.Width(250));
            GUILayout.Label("Base HP", EditorStyles.boldLabel, GUILayout.Width(60));
            GUILayout.Label("Speed", EditorStyles.boldLabel, GUILayout.Width(60));
            GUILayout.Label("Atk", EditorStyles.boldLabel, GUILayout.Width(60));
            GUILayout.EndHorizontal();

            foreach (var so in jobs)
            {
                if (so == null) continue;

                EditorGUI.BeginChangeCheck();
                GUILayout.BeginHorizontal();

                if (GUILayout.Button(so.name, EditorStyles.label, GUILayout.Width(150)))
                {
                    Selection.activeObject = so;
                }

                so.id = EditorGUILayout.TextField(so.id, GUILayout.Width(80));
                so.jobName = EditorGUILayout.TextField(so.jobName, GUILayout.Width(120));
                so.description = EditorGUILayout.TextField(so.description, GUILayout.Width(250));
                so.baseHp = EditorGUILayout.FloatField(so.baseHp, GUILayout.Width(60));
                so.baseSpeed = EditorGUILayout.FloatField(so.baseSpeed, GUILayout.Width(60));
                so.baseAttack = EditorGUILayout.FloatField(so.baseAttack, GUILayout.Width(60));

                GUILayout.EndHorizontal();

                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(so);
                }
            }
        }

        // ===========================================
        // Skill Data Grid
        // ===========================================
        private void DrawSkillsGrid()
        {
            GUILayout.BeginHorizontal("box");
            GUILayout.Label("Asset Name", EditorStyles.boldLabel, GUILayout.Width(150));
            GUILayout.Label("ID", EditorStyles.boldLabel, GUILayout.Width(80));
            GUILayout.Label("Skill Name", EditorStyles.boldLabel, GUILayout.Width(120));
            GUILayout.Label("Description", EditorStyles.boldLabel, GUILayout.Width(200));
            GUILayout.Label("Cooldown", EditorStyles.boldLabel, GUILayout.Width(70));
            GUILayout.Label("Multiplier", EditorStyles.boldLabel, GUILayout.Width(70));
            GUILayout.Label("Mana Cost", EditorStyles.boldLabel, GUILayout.Width(70));
            GUILayout.EndHorizontal();

            foreach (var so in skills)
            {
                if (so == null) continue;

                EditorGUI.BeginChangeCheck();
                GUILayout.BeginHorizontal();

                if (GUILayout.Button(so.name, EditorStyles.label, GUILayout.Width(150)))
                {
                    Selection.activeObject = so;
                }

                so.id = EditorGUILayout.TextField(so.id, GUILayout.Width(80));
                so.skillName = EditorGUILayout.TextField(so.skillName, GUILayout.Width(120));
                so.description = EditorGUILayout.TextField(so.description, GUILayout.Width(200));
                so.cooldown = EditorGUILayout.FloatField(so.cooldown, GUILayout.Width(70));
                so.damageMultiplier = EditorGUILayout.FloatField(so.damageMultiplier, GUILayout.Width(70));
                so.manaCost = EditorGUILayout.IntField(so.manaCost, GUILayout.Width(70));

                GUILayout.EndHorizontal();

                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(so);
                }
            }
        }
        
        // ===========================================
        // Monster Data Grid
        // ===========================================
        private void DrawMonstersGrid()
        {
            GUILayout.BeginHorizontal("box");
            GUILayout.Label("Asset Name", EditorStyles.boldLabel, GUILayout.Width(150));
            GUILayout.Label("ID", EditorStyles.boldLabel, GUILayout.Width(80));
            GUILayout.Label("Mob Name", EditorStyles.boldLabel, GUILayout.Width(120));
            GUILayout.Label("HP", EditorStyles.boldLabel, GUILayout.Width(60));
            GUILayout.Label("Speed", EditorStyles.boldLabel, GUILayout.Width(60));
            GUILayout.Label("Atk", EditorStyles.boldLabel, GUILayout.Width(60));
            GUILayout.Label("R.Gold", EditorStyles.boldLabel, GUILayout.Width(60));
            GUILayout.Label("R.Exp", EditorStyles.boldLabel, GUILayout.Width(60));
            GUILayout.EndHorizontal();

            foreach (var so in monsters)
            {
                if (so == null) continue;

                EditorGUI.BeginChangeCheck();
                GUILayout.BeginHorizontal();

                if (GUILayout.Button(so.name, EditorStyles.label, GUILayout.Width(150)))
                {
                    Selection.activeObject = so;
                }

                so.id = EditorGUILayout.TextField(so.id, GUILayout.Width(80));
                so.monsterName = EditorGUILayout.TextField(so.monsterName, GUILayout.Width(120));
                so.maxHp = EditorGUILayout.FloatField(so.maxHp, GUILayout.Width(60));
                so.speed = EditorGUILayout.FloatField(so.speed, GUILayout.Width(60));
                so.attackPower = EditorGUILayout.FloatField(so.attackPower, GUILayout.Width(60));
                so.killRewardGold = EditorGUILayout.IntField(so.killRewardGold, GUILayout.Width(60));
                so.killRewardExp = EditorGUILayout.IntField(so.killRewardExp, GUILayout.Width(60));

                GUILayout.EndHorizontal();

                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(so);
                }
            }
        }

        private void CreateDummyData(WIDataType dataType)
        {
            string timeStamp = System.DateTime.Now.ToString("HHmmss");
            
            if (dataType == WIDataType.Dungeon)
            {
                string path = $"Assets/Data/ScriptableObject/Dungeons/DummyDungeon_{timeStamp}.asset";
                EnsureFolderExists("Assets/Data/ScriptableObject/Dungeons");
                var so = CreateInstance<WIDungeonDataSO>();
                so.dungeonId = 999;
                so.dungeonName = "Dummy Dungeon";
                AssetDatabase.CreateAsset(so, path);
            }
            else if (dataType == WIDataType.DungeonEvent)
            {
                string path = $"Assets/Data/ScriptableObject/Events/DummyEvent_{timeStamp}.asset";
                EnsureFolderExists("Assets/Data/ScriptableObject/Events");
                var so = CreateInstance<WIDungeonEventSO>();
                so.id = "E_DUMMY";
                AssetDatabase.CreateAsset(so, path);
            }
            else if (dataType == WIDataType.Job)
            {
                string path = $"Assets/Data/ScriptableObject/Jobs/DummyJob_{timeStamp}.asset";
                EnsureFolderExists("Assets/Data/ScriptableObject/Jobs");
                var so = CreateInstance<WIJobDataSO>();
                so.id = "J_DUMMY";
                AssetDatabase.CreateAsset(so, path);
            }
            else if (dataType == WIDataType.Skill)
            {
                string path = $"Assets/Data/ScriptableObject/Skills/DummySkill_{timeStamp}.asset";
                EnsureFolderExists("Assets/Data/ScriptableObject/Skills");
                var so = CreateInstance<WISkillDataSO>();
                so.id = "S_DUMMY";
                AssetDatabase.CreateAsset(so, path);
            }
            else if (dataType == WIDataType.Monster)
            {
                string path = $"Assets/Data/ScriptableObject/Monsters/DummyMonster_{timeStamp}.asset";
                EnsureFolderExists("Assets/Data/ScriptableObject/Monsters");
                var so = CreateInstance<WIMonsterDataSO>();
                so.id = "M_DUMMY";
                AssetDatabase.CreateAsset(so, path);
            }
            
            AssetDatabase.SaveAssets();
            ReloadData();
        }

        private void EnsureFolderExists(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string[] parts = path.Split('/');
                string currPath = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    string parent = currPath;
                    currPath += "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(currPath))
                    {
                        AssetDatabase.CreateFolder(parent, parts[i]);
                    }
                }
            }
        }
    }
}
#endif

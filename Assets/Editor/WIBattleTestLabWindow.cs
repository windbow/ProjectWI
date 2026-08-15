using System;
using System.Collections.Generic;
using System.Linq;
using ProjectWI.Administration;
using ProjectWI.Battle;
using ProjectWI.Systems;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ProjectWI.Editor
{
    public class WIBattleTestLabWindow : EditorWindow
    {
        private const string DatabasePath = "Assets/Data/ScriptableObject/Administration/WI_AdministrationDatabase.asset";
        private const string MainScenePath = "Assets/Scenes/MainScene.unity";
        private const string LaunchDataKey = "ProjectWI.BattleTestLab.LaunchData";

        [Serializable]
        private class TestMember
        {
            public string heroId;
            public WIUnitRole role = WIUnitRole.Melee;
        }

        [Serializable]
        private class LaunchData
        {
            public List<TestMember> allies = new List<TestMember>();
            public List<TestMember> enemies = new List<TestMember>();
        }

        private WIAdministrationDatabaseSO database;
        private readonly List<TestMember> allies = new List<TestMember>();
        private readonly List<TestMember> enemies = new List<TestMember>();
        private Vector2 rosterScroll;
        private Vector2 allyScroll;
        private Vector2 enemyScroll;

        [MenuItem("ProjectWI/Tools/Battle Test Lab")]
        // 아군과 적군 편성을 설정하고 실제 전투 씬을 실행하는 테스트 창을 엽니다.
        public static void OpenWindow()
        {
            GetWindow<WIBattleTestLabWindow>("전투 테스트 랩").minSize = new Vector2(760f, 520f);
        }

        // 아레스와 커먼급 29명 대 상대 영웅과 커먼급 29명의 전투 밀도 검증을 바로 시작합니다.
        [MenuItem("ProjectWI/Verification/Start 30v30 Battle Density Test")]
        public static void StartThirtyVsThirtyBattleDensityTest()
        {
            WIAdministrationDatabaseSO battleDatabase =
                AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            if (battleDatabase == null)
            {
                Debug.LogError($"데이터베이스를 찾을 수 없습니다: {DatabasePath}");
                return;
            }

            List<WIHeroDefinition> heroes = battleDatabase.Heroes
                .Where(item => item.Grade == WICharacterGrade.Hero)
                .ToList();
            List<WIHeroDefinition> commons = battleDatabase.Heroes
                .Where(item => item.Grade == WICharacterGrade.Common)
                .ToList();
            if (heroes.Count < 2 || commons.Count < 58)
            {
                Debug.LogError("30대30 전투 밀도 검증에는 영웅 2명과 커먼급 58명이 필요합니다.");
                return;
            }

            WIHeroDefinition allyHero = battleDatabase.GetHero("ares") ?? heroes[0];
            WIHeroDefinition enemyHero = heroes.First(item => item.Id != allyHero.Id);
            LaunchData data = new LaunchData();
            data.allies.Add(CreateTestMember(battleDatabase, allyHero));
            data.enemies.Add(CreateTestMember(battleDatabase, enemyHero));
            for (int index = 0; index < 29; index++)
            {
                data.allies.Add(CreateTestMember(battleDatabase, commons[index]));
                data.enemies.Add(CreateTestMember(battleDatabase, commons[index + 29]));
            }

            EditorPrefs.SetString(LaunchDataKey, JsonUtility.ToJson(data));
            EditorSceneManager.OpenScene(MainScenePath);
            EditorApplication.isPlaying = true;
            Debug.Log("30대30 전투 밀도 검증을 예약했습니다. 양측은 영웅 1명과 커먼급 29명으로 구성됩니다.");
        }

        [MenuItem("ProjectWI/Verification/Battle Zoom/A Near")]
        // 실행 중인 전투 카메라를 근거리 A 단계로 변경합니다.
        public static void SetBattleZoomA()
        {
            SetBattleZoom(WIBattleZoomLevel.A);
        }

        [MenuItem("ProjectWI/Verification/Battle Zoom/B Middle")]
        // 실행 중인 전투 카메라를 중거리 B 단계로 변경합니다.
        public static void SetBattleZoomB()
        {
            SetBattleZoom(WIBattleZoomLevel.B);
        }

        [MenuItem("ProjectWI/Verification/Battle Zoom/C Far")]
        // 실행 중인 전투 카메라를 원거리 C 단계로 변경합니다.
        public static void SetBattleZoomC()
        {
            SetBattleZoom(WIBattleZoomLevel.C);
        }

        // 플레이 모드의 전투 카메라를 지정한 고정 줌 단계로 전환합니다.
        private static void SetBattleZoom(WIBattleZoomLevel zoomLevel)
        {
            if (EditorApplication.isPlaying == false)
            {
                Debug.LogWarning("전투 줌 단계는 플레이 모드에서 검증할 수 있습니다.");
                return;
            }

            WIBattleCameraController cameraController =
                UnityEngine.Object.FindFirstObjectByType<WIBattleCameraController>();
            if (cameraController == null)
            {
                Debug.LogWarning("실행 중인 전투 카메라를 찾을 수 없습니다.");
                return;
            }

            cameraController.SetZoomLevel(zoomLevel);
            Debug.Log($"전투 카메라를 {zoomLevel} 단계로 변경했습니다.");
        }

        // 캐릭터 클래스의 권장 역할을 적용한 전투 테스트 참가 데이터를 만듭니다.
        private static TestMember CreateTestMember(WIAdministrationDatabaseSO battleDatabase, WIHeroDefinition character)
        {
            WIHeroClassDefinition classDefinition = battleDatabase.GetHeroClass(character.HeroClass);
            return new TestMember
            {
                heroId = character.Id,
                role = classDefinition == null ? WIUnitRole.Melee : classDefinition.RecommendedRole
            };
        }

        // 창이 열릴 때 전투에 사용할 내정 데이터베이스를 불러옵니다.
        private void OnEnable()
        {
            database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            if (database != null && allies.Count == 0 && enemies.Count == 0)
            {
                AddDefaultMembers();
            }
        }

        // 편성 목록, 전체 인물 명단과 전투 실행 버튼을 그립니다.
        private void OnGUI()
        {
            EditorGUILayout.LabelField("ProjectWI 전투 테스트 랩", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "영웅과 일반 인물을 양 진영에 배치하고 역할을 지정한 뒤 BattleScene을 바로 실행합니다. 테스트 결과는 캠페인 저장에 반영되지 않습니다.",
                MessageType.Info);
            if (database == null)
            {
                EditorGUILayout.HelpBox($"데이터베이스를 찾을 수 없습니다: {DatabasePath}", MessageType.Error);
                return;
            }

            EditorGUILayout.BeginHorizontal();
            DrawSide("아군 편성", allies, ref allyScroll);
            DrawSide("적군 편성", enemies, ref enemyScroll);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(8f);
            DrawRoster();
            EditorGUILayout.Space(8f);

            EditorGUI.BeginDisabledGroup(allies.Count == 0 || enemies.Count == 0 || EditorApplication.isPlayingOrWillChangePlaymode);
            if (GUILayout.Button("편성대로 전투 시작", GUILayout.Height(38f)))
            {
                StartBattleTest();
            }
            EditorGUI.EndDisabledGroup();
        }

        // 한 진영에 배치된 인물과 전투 역할 변경 UI를 표시합니다.
        private void DrawSide(string title, List<TestMember> members, ref Vector2 scroll)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(position.width * 0.49f), GUILayout.Height(210f));
            EditorGUILayout.LabelField($"{title} · {members.Count}명", EditorStyles.boldLabel);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            for (int index = 0; index < members.Count; index += 1)
            {
                TestMember member = members[index];
                WIHeroDefinition definition = database.GetHero(member.heroId);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(GetCharacterLabel(definition), GUILayout.Width(175f));
                member.role = (WIUnitRole)EditorGUILayout.EnumPopup(member.role, GUILayout.Width(105f));
                if (GUILayout.Button("제거", GUILayout.Width(45f)))
                {
                    members.RemoveAt(index);
                    index -= 1;
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        // 데이터베이스의 영웅·일반 인물 전체 명단과 양측 추가 버튼을 표시합니다.
        private void DrawRoster()
        {
            EditorGUILayout.LabelField("인물 명단", EditorStyles.boldLabel);
            rosterScroll = EditorGUILayout.BeginScrollView(rosterScroll, EditorStyles.helpBox, GUILayout.Height(205f));
            foreach (WIHeroDefinition hero in database.Heroes.OrderBy(item => item.Grade).ThenBy(item => item.DisplayName.Korean))
            {
                bool assigned = allies.Any(item => item.heroId == hero.Id) || enemies.Any(item => item.heroId == hero.Id);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(GetCharacterLabel(hero), GUILayout.Width(230f));
                EditorGUILayout.LabelField($"{hero.HeroClass} · 통솔 {hero.Leadership} · 무력 {hero.Might}");
                EditorGUI.BeginDisabledGroup(assigned);
                if (GUILayout.Button("아군 추가", GUILayout.Width(75f))) AddMember(allies, hero);
                if (GUILayout.Button("적군 추가", GUILayout.Width(75f))) AddMember(enemies, hero);
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        // 인물 등급과 이름을 테스트 편성용 한 줄 문구로 반환합니다.
        private static string GetCharacterLabel(WIHeroDefinition hero)
        {
            if (hero == null) return "알 수 없는 인물";
            return $"[{(hero.Grade == WICharacterGrade.Hero ? "영웅" : "일반")}] {hero.DisplayName.Korean}";
        }

        // 인물 클래스에 어울리는 기본 전투 역할로 선택 진영에 추가합니다.
        private void AddMember(List<TestMember> members, WIHeroDefinition hero)
        {
            WIHeroClassDefinition classDefinition = database.GetHeroClass(hero.HeroClass);
            WIUnitRole role = classDefinition == null ? WIUnitRole.Melee : classDefinition.RecommendedRole;
            members.Add(new TestMember { heroId = hero.Id, role = role });
        }

        // 처음 열었을 때 영웅과 일반 인물이 섞인 3대3 기본 편성을 구성합니다.
        private void AddDefaultMembers()
        {
            List<WIHeroDefinition> heroes = database.Heroes.Where(item => item.Grade == WICharacterGrade.Hero).ToList();
            List<WIHeroDefinition> commons = database.Heroes.Where(item => item.Grade == WICharacterGrade.Common).ToList();
            if (heroes.Count > 0) AddMember(allies, heroes[0]);
            if (commons.Count > 0) AddMember(allies, commons[0]);
            if (commons.Count > 1) AddMember(allies, commons[1]);
            if (heroes.Count > 1) AddMember(enemies, heroes[1]);
            if (commons.Count > 2) AddMember(enemies, commons[2]);
            if (commons.Count > 3) AddMember(enemies, commons[3]);
        }

        // 현재 편성을 임시 저장하고 MainScene 플레이 모드 진입 후 자동 실행을 예약합니다.
        private void StartBattleTest()
        {
            LaunchData data = new LaunchData { allies = allies.ToList(), enemies = enemies.ToList() };
            EditorPrefs.SetString(LaunchDataKey, JsonUtility.ToJson(data));
            EditorSceneManager.OpenScene(MainScenePath);
            EditorApplication.isPlaying = true;
        }

        // 플레이 모드에서 임시 편성을 캠페인 서비스의 테스트 세션으로 변환합니다.
        internal static bool TryLaunchPendingBattle()
        {
            if (EditorPrefs.HasKey(LaunchDataKey) == false || WICampaignRuntimeService.Instance == null)
            {
                return false;
            }
            LaunchData data = JsonUtility.FromJson<LaunchData>(EditorPrefs.GetString(LaunchDataKey));
            EditorPrefs.DeleteKey(LaunchDataKey);
            WIBattleSessionState session = new WIBattleSessionState
            {
                SessionId = "editor_battle_test",
                CastleId = "castle_00",
                AttackerFactionId = "avalon",
                DefenderFactionId = "valdor",
                PlayerInvolved = true,
                Status = WIBattleSessionStatus.Pending
            };
            session.AttackerHeroIds.AddRange(data.allies.Select(item => new WIBattleParticipantState
            {
                HeroId = item.heroId, ArmyId = "test_allies", Role = item.role
            }));
            session.DefenderHeroIds.AddRange(data.enemies.Select(item => new WIBattleParticipantState
            {
                HeroId = item.heroId, ArmyId = "test_enemies", Role = item.role
            }));
            return WICampaignRuntimeService.Instance.StartTestBattle(session);
        }
    }

    [InitializeOnLoad]
    internal static class WIBattleTestLabLauncher
    {
        private static int remainingFrames;

        // 에디터 시작 시 플레이 모드 변경 이벤트를 한 번 연결합니다.
        static WIBattleTestLabLauncher()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            if (EditorApplication.isPlaying)
            {
                BeginLaunchWait();
            }
        }

        // 플레이 모드 진입 후 MainScene 서비스가 생성될 시간을 두고 실행 대기를 시작합니다.
        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode) return;
            BeginLaunchWait();
        }

        // 도메인 재로딩 시점과 관계없이 최대 120 프레임 동안 임시 전투 실행을 재시도합니다.
        private static void BeginLaunchWait()
        {
            EditorApplication.update -= TryLaunch;
            remainingFrames = 120;
            EditorApplication.update += TryLaunch;
        }

        // 캠페인 서비스가 준비되면 예약된 테스트 전투를 시작합니다.
        private static void TryLaunch()
        {
            remainingFrames -= 1;
            if (WIBattleTestLabWindow.TryLaunchPendingBattle() || remainingFrames <= 0)
            {
                EditorApplication.update -= TryLaunch;
            }
        }
    }
}

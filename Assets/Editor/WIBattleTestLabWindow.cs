using System;
using System.Collections.Generic;
using System.Linq;
using ProjectWI.Administration;
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
        private static void AddMember(List<TestMember> members, WIHeroDefinition hero)
        {
            WIUnitRole role = hero.HeroClass == WIHeroClass.Archer ? WIUnitRole.Ranged
                : hero.HeroClass == WIHeroClass.Archmage ? WIUnitRole.Magic
                : hero.HeroClass == WIHeroClass.Priest || hero.HeroClass == WIHeroClass.Druid ? WIUnitRole.Support
                : members.Count == 0 ? WIUnitRole.Commander : WIUnitRole.Melee;
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
        }

        // 플레이 모드 진입 후 MainScene 서비스가 생성될 시간을 두고 실행 대기를 시작합니다.
        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode) return;
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

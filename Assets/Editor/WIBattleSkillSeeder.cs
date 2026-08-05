using ProjectWI.Administration;
using UnityEditor;
using UnityEngine;

namespace ProjectWI.Editor
{
    public static class WIBattleSkillSeeder
    {
        private const string BattleConfigPath = "Assets/Data/ScriptableObject/Battle/WI_BattleConfig.asset";
        private const string DatabasePath = "Assets/Data/ScriptableObject/Administration/WI_AdministrationDatabase.asset";

        [MenuItem("ProjectWI/Data/Seed Hero Battle Skills")]
        // 고유 영웅 8명의 액티브 스킬과 전투 사용 가능 상태를 ScriptableObject에 기록합니다.
        public static void SeedHeroBattleSkills()
        {
            Object configAsset = AssetDatabase.LoadMainAssetAtPath(BattleConfigPath);
            Object databaseAsset = AssetDatabase.LoadMainAssetAtPath(DatabasePath);
            if (configAsset == null || databaseAsset == null)
            {
                Debug.LogError("전투 설정 또는 내정 데이터베이스를 찾을 수 없습니다.");
                return;
            }

            SerializedObject config = new SerializedObject(configAsset);
            SerializedProperty skills = config.FindProperty("heroSkills");
            skills.arraySize = 8;
            WriteSkill(skills.GetArrayElementAtIndex(0), "ares", "왕가의 검진", "주변 적에게 검기 피해를 줍니다.", 0, 40, 45, 5f, 10f);
            WriteSkill(skills.GetArrayElementAtIndex(1), "lyria", "전장의 격려", "주변 아군의 공격 대기시간을 줄입니다.", 2, 35, 45, 6f, 12f);
            WriteSkill(skills.GetArrayElementAtIndex(2), "brom", "불굴의 함성", "주변 아군의 공격 대기시간을 크게 줄입니다.", 2, 30, 55, 4.5f, 13f);
            WriteSkill(skills.GetArrayElementAtIndex(3), "selene", "은월의 치유", "주변 아군의 체력을 회복합니다.", 1, 40, 48, 5.5f, 12f);
            WriteSkill(skills.GetArrayElementAtIndex(4), "kael", "그림자 난무", "주변 적에게 연속 참격 피해를 줍니다.", 0, 45, 52, 3.8f, 11f);
            WriteSkill(skills.GetArrayElementAtIndex(5), "morrigan", "황혼 폭발", "넓은 범위의 적에게 마력 피해를 줍니다.", 0, 50, 58, 6f, 14f);
            WriteSkill(skills.GetArrayElementAtIndex(6), "theron", "성전의 맹세", "주변 아군의 공격 대기시간을 줄입니다.", 2, 42, 60, 5f, 14f);
            WriteSkill(skills.GetArrayElementAtIndex(7), "elwyn", "고대 숲의 숨결", "넓은 범위의 아군 체력을 회복합니다.", 1, 48, 55, 6.5f, 15f);
            config.FindProperty("skillVisualDuration").floatValue = 0.65f;
            config.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject database = new SerializedObject(databaseAsset);
            SerializedProperty heroes = database.FindProperty("heroes");
            int enabledCount = 0;
            for (int index = 0; index < heroes.arraySize; index += 1)
            {
                SerializedProperty hero = heroes.GetArrayElementAtIndex(index);
                if (hero.FindPropertyRelative("grade").enumValueIndex != (int)WICharacterGrade.Hero) continue;
                hero.FindPropertyRelative("activeSkillAvailable").boolValue = true;
                enabledCount += 1;
            }
            database.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(configAsset);
            EditorUtility.SetDirty(databaseAsset);
            AssetDatabase.SaveAssets();
            Debug.Log($"ProjectWI 영웅 전투 스킬 8종과 사용 가능 영웅 {enabledCount}명을 갱신했습니다.");
        }

        // 액티브 스킬 한 항목의 표시·효과·비용·범위·재사용 시간을 기록합니다.
        private static void WriteSkill(SerializedProperty skill, string heroId, string displayName, string description,
            int skillType, int manaCost, int power, float range, float cooldown)
        {
            skill.FindPropertyRelative("heroId").stringValue = heroId;
            skill.FindPropertyRelative("displayName").stringValue = displayName;
            skill.FindPropertyRelative("description").stringValue = description;
            skill.FindPropertyRelative("skillType").enumValueIndex = skillType;
            skill.FindPropertyRelative("manaCost").intValue = manaCost;
            skill.FindPropertyRelative("power").intValue = power;
            skill.FindPropertyRelative("range").floatValue = range;
            skill.FindPropertyRelative("cooldown").floatValue = cooldown;
        }
    }
}

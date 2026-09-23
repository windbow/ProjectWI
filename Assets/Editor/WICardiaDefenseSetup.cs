using System.Linq;
using ProjectWI.Administration;
using UnityEditor;

namespace ProjectWI.EditorTools
{
    public static class WICardiaDefenseSetup
    {
        // 메인 시나리오의 기존 인물 세 명만 재배치하고 접경 수비 설정과 보고 문구를 저장합니다.
        [MenuItem("WI/Data/Apply Cardia Defense")]
        public static void Apply()
        {
            var database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>("Assets/Data/ScriptableObject/Administration/WI_AdministrationDatabase.asset");
            var serialized = new SerializedObject(database);
            var variants = serialized.FindProperty("campaignVariants");
            for (int index = 0; index < variants.arraySize; index++)
            {
                var variant = variants.GetArrayElementAtIndex(index);
                if (variant.FindPropertyRelative("variant").enumValueIndex != (int)WICampaignVariant.AresMain)
                {
                    continue;
                }
                variant.FindPropertyRelative("aiBorderReserveCount").intValue = 2;
                variant.FindPropertyRelative("aiFrontlineReinforcementTarget").intValue = 6;
                var placements = variant.FindPropertyRelative("characterPlacements");
                string[] defenders = { "hero_072", "common_049", "common_048" };
                for (int row = 0; row < placements.arraySize; row++)
                {
                    var placement = placements.GetArrayElementAtIndex(row);
                    if (defenders.Contains(placement.FindPropertyRelative("heroId").stringValue) == true)
                    {
                        placement.FindPropertyRelative("castleId").stringValue = "castle_04";
                        placement.FindPropertyRelative("governor").boolValue = false;
                    }
                }
            }
            var strings = serialized.FindProperty("uiStrings");
            SerializedProperty message = null;
            for (int index = 0; index < strings.arraySize; index++)
            {
                var entry = strings.GetArrayElementAtIndex(index);
                if (entry.FindPropertyRelative("uid").stringValue == "REPORT_UNOPPOSED_OCCUPATION")
                {
                    message = entry;
                    break;
                }
            }
            if (message == null)
            {
                strings.arraySize++;
                message = strings.GetArrayElementAtIndex(strings.arraySize - 1);
            }
            message.FindPropertyRelative("uid").stringValue = "REPORT_UNOPPOSED_OCCUPATION";
            message.FindPropertyRelative("korean").stringValue = "무혈 점령 · {0}에 방어 병력이 없어 {1}이 전투 없이 점령했습니다.";
            message.FindPropertyRelative("english").stringValue = "Unopposed occupation · {1} occupied {0} without a battle because no defenders remained.";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(database);
        }
    }
}

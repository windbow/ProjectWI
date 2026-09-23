using ProjectWI.Administration;
using UnityEditor;
using UnityEngine;

namespace ProjectWI.EditorTools
{
    public static class WIMusterSetup
    {
        // 모병 SO와 고정 문구를 편집 시점에 저장하며 기존 UI 프리팹을 재사용합니다.
        [MenuItem("WI/Data/Apply Muster System")]
        public static void Apply()
        {
            const string folder = "Assets/Data/ScriptableObject/Administration/";
            WIMusterConfigSO config = AssetDatabase.LoadAssetAtPath<WIMusterConfigSO>(folder + "WI_MusterConfig.asset");
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<WIMusterConfigSO>();
                AssetDatabase.CreateAsset(config, folder + "WI_MusterConfig.asset");
            }
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(folder + "WI_AdministrationDatabase.asset");
            SerializedObject serialized = new SerializedObject(database);
            serialized.FindProperty("musterConfig").objectReferenceValue = config;
            SerializedProperty strings = serialized.FindProperty("uiStrings");
            string[,] entries =
            {
                { "UI_MUSTER_TITLE", "모병 · 일반 인물 충원", "Muster · Common Characters" },
                { "UI_MUSTER_HINT", "성에서 역할과 합류 대상을 정해 일반 인물을 모집합니다. 영웅 담당자는 필요하지 않습니다.", "Choose a castle, role and destination. No hero officer is required." },
                { "UI_MUSTER_INVALID", "모병 설정 또는 성 소유권을 확인하세요.", "Check muster settings and castle ownership." },
                { "UI_MUSTER_ALREADY", "이 성에는 이미 다음 달 모집 예약이 있습니다.", "This castle already has a pending muster order." },
                { "UI_MUSTER_LIMIT", "성의 월 모집 한도 또는 세력 전체 인원 상한에 도달했습니다.", "Monthly castle limit or faction roster capacity reached." },
                { "UI_MUSTER_SPACE", "합류할 빈자리가 없거나 전투 중입니다. 전투단 편성 또는 다른 합류 대상을 확인하세요.", "No arrival space, or a battle is pending. Check the destination." },
                { "UI_MUSTER_GOLD", "모집 비용을 지불할 금화가 부족합니다.", "Not enough gold for muster." },
                { "UI_MUSTER_POOL", "이 역할로 모집할 수 있는 일반 인물이 부족합니다.", "Not enough available common characters for this role." },
                { "UI_MUSTER_CASTLE", "월 최대 {0}명 · 모집 중 {1}명", "Up to {0}/month · {1} pending" },
                { "UI_MUSTER_STATUS", "금화 {0} · 세력 인원 {1}/{2}\n선택 · {3} / {4} · 모집 중 {5}명\n자동 충원 {6} · 목표 {7}명 · 월 예산 {8}G", "Gold {0} · Roster {1}/{2}\nSelected: {3} / {4} · {5} pending\nAuto {6} · Target {7} · Budget {8}G/month" },
                { "UI_MUSTER_RESERVE", "성에서 대기", "Castle reserve" },
                { "UI_MUSTER_RESERVE_HINT", "합류 후 원하는 전투단에 직접 편성합니다. 거주 공간이 필요합니다.", "Arrive as residents for manual assignment. Requires resident space." },
                { "UI_MUSTER_CANCEL", "모집 취소", "Cancel muster" },
                { "UI_MUSTER_REFUND", "{0}G 반환 · 자동 충원도 중지", "Refund {0}G · also stop automatic replenishment" },
                { "UI_MUSTER_QUEUE", "{0}명 모집 · {1}G", "Muster {0} · {1}G" },
                { "UI_MUSTER_NEXT_MONTH", "선택한 역할과 합류 대상으로 다음 달에 합류합니다.", "Arrives next month with the selected role and destination." },
                { "UI_MUSTER_ROLE_1", "전위", "Vanguard" },
                { "UI_MUSTER_ROLE_2", "근접", "Melee" },
                { "UI_MUSTER_ROLE_3", "원거리", "Ranged" },
                { "UI_MUSTER_ROLE_4", "마법", "Magic" },
                { "UI_MUSTER_ROLE_5", "지원", "Support" },
                { "UI_MUSTER_SELECTED", "현재 선택한 모집 역할", "Selected muster role" },
                { "UI_MUSTER_CHOOSE_ROLE", "앞으로 모집할 인물의 전투 역할을 선택합니다.", "Select the role for future orders." },
                { "UI_MUSTER_DESTINATION", "합류 대상 · {0}", "Destination · {0}" },
                { "UI_MUSTER_ARMY_SIZE", "전투단 {0}/{1}명 · 같은 성에 머물러야 자동 편성됩니다.", "Army {0}/{1} · Must remain here for automatic assignment." },
                { "UI_MUSTER_START", "자동 충원 시작", "Start automatic replenishment" },
                { "UI_MUSTER_STOP", "자동 충원 중지", "Stop automatic replenishment" },
                { "UI_MUSTER_AUTO_HINT", "선택 전투단이 이 성에 있을 때 목표까지 모집합니다. 자금·공간·후보 부족 시 대기합니다. 예약된 모집은 별도로 취소하세요.", "Muster to the target while the army is here. Waits for funds, space or candidates. Cancel pending orders separately." },
                { "UI_MUSTER_TARGET_LESS", "충원 목표 1명 줄이기", "Decrease target by 1" },
                { "UI_MUSTER_TARGET_MORE", "충원 목표 1명 늘리기", "Increase target by 1" },
                { "UI_MUSTER_TARGET", "전투단 전체 목표 {0}명", "Target army size: {0}" },
                { "UI_MUSTER_BUDGET_LESS", "월 예산 한 명분 줄이기", "Decrease monthly budget" },
                { "UI_MUSTER_BUDGET_MORE", "월 예산 한 명분 늘리기", "Increase monthly budget" },
                { "UI_MUSTER_BUDGET", "월 모병 예산 상한 {0}G", "Monthly muster budget: {0}G" },
                { "UI_MUSTER_ON", "사용", "On" },
                { "UI_MUSTER_OFF", "중지", "Off" },
                { "REPORT_MUSTER_DONE", "모병 완료 · {0} · {1} 합류", "Muster completed · {0} · {1} arrived" },
                { "REPORT_MUSTER_CANCEL", "모집 취소 · {0} · 합류 조건 변경으로 {1}G 반환", "Muster cancelled · {0} · conditions changed, refunded {1}G" }
            };
            for (int row = 0; row < entries.GetLength(0); row++)
            {
                SerializedProperty entry = null;
                for (int index = 0; index < strings.arraySize; index++)
                {
                    SerializedProperty candidate = strings.GetArrayElementAtIndex(index);
                    if (candidate.FindPropertyRelative("uid").stringValue == entries[row, 0])
                    {
                        entry = candidate;
                        break;
                    }
                }
                if (entry == null)
                {
                    strings.arraySize++;
                    entry = strings.GetArrayElementAtIndex(strings.arraySize - 1);
                }
                entry.FindPropertyRelative("uid").stringValue = entries[row, 0];
                entry.FindPropertyRelative("korean").stringValue = entries[row, 1];
                entry.FindPropertyRelative("english").stringValue = entries[row, 2];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(database);
            AssetDatabase.SaveAssetIfDirty(config);
            Debug.Log("모병 SO와 UI 문구 저장 완료");
        }
    }
}

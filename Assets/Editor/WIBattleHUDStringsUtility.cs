using ProjectWI.Administration;
using UnityEditor;

namespace ProjectWI.EditorTools
{
    public static class WIBattleHUDStringsUtility
    {
        private const string DatabasePath = "Assets/Data/ScriptableObject/Administration/WI_AdministrationDatabase.asset";

        // 전투 HUD 명령·상태·스킬 문구를 데이터베이스 문자열 UID로 저장하거나 갱신합니다.
        [MenuItem("WI/UI/Apply Battle HUD Strings")]
        public static void Apply()
        {
            WIAdministrationDatabaseSO asset = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            SerializedObject serialized = new SerializedObject(asset);
            SerializedProperty list = serialized.FindProperty("uiStrings");
            string[,] entries =
            {
                { "UI_BATTLE_STATUS", "{0} · {1:0.0}초 · 공격 {2} / 수비 {3}", "{0} · {1:0.0}s · Attackers {2} / Defenders {3}" },
                { "UI_BATTLE_OBJECTIVE_ELIMINATION", "섬멸", "Elimination" },
                { "UI_BATTLE_BUTTON_ADVANCE", "전진", "Advance" },
                { "UI_BATTLE_BUTTON_HOLD", "위치 사수", "Hold" },
                { "UI_BATTLE_BUTTON_FOCUS", "집중 공격", "Focus" },
                { "UI_BATTLE_BUTTON_PROTECT", "후열 보호", "Protect" },
                { "UI_BATTLE_BUTTON_SPREAD", "분산", "Spread" },
                { "UI_BATTLE_BUTTON_RALLY", "집결", "Rally" },
                { "UI_BATTLE_BUTTON_RETREAT", "후퇴", "Retreat" },
                { "UI_BATTLE_COMMAND_HINT", "명령 대기 · Z 전진 X 사수 C 집중 V 보호 B 분산 N 집결 R 후퇴 · 분대를 고르면 그 분대에만 명령합니다.", "Awaiting orders · Z Advance X Hold C Focus V Protect B Spread N Rally R Retreat · Orders apply to selected squads only." },
                { "UI_BATTLE_SELECTION_HINT", "아군 클릭·드래그로 분대 선택 · 1~9 분대 · ` 전체 · 우클릭/빈 곳 클릭 이동 · 적 클릭 공격 · F 영웅 직접 지휘 · Q 스킬 · Space 일시정지 · Esc 해제", "Click/drag allies to select squads · 1-9 squads · ` all · Right-click/click ground to move · Click enemy to attack · F command hero · Q skill · Space pause · Esc clear" },
                { "UI_BATTLE_ORDER_NEED_SELECTION", "먼저 분대를 선택하세요.", "Select a squad first." },
                { "UI_BATTLE_ORDER_MOVE", "이동 명령 · 분대 {0}개가 지정 위치로 이동해 자리를 지킵니다.", "Move · {0} squads move and hold the position." },
                { "UI_BATTLE_ORDER_ATTACK", "공격 명령 · 분대 {1}개가 {0}을 집중 공격합니다.", "Attack · {1} squads focus {0}." },
                { "UI_BATTLE_SQUAD_SELECTED", "분대 {0}개 선택 · {1}명 · 평균 사기 {2:0}", "{0} squads selected · {1} members · Avg morale {2:0}" },
                { "UI_BATTLE_SKILL_TARGETING", "{0} · 시전할 위치를 클릭하세요. 우클릭/Esc 취소", "{0} · Click a target location. Right-click/Esc to cancel" },
                { "UI_BATTLE_SKILL_TARGETING_CANCELLED", "스킬 시전을 취소했습니다.", "Skill cast cancelled." },
                { "UI_BATTLE_SKILL_CAST", "{0} 시전", "{0} cast" },
                { "UI_BATTLE_SQUAD_ROUTING", "{0} 분대의 사기가 무너져 퇴각합니다!", "{0}'s squad breaks and flees!" },
                { "UI_BATTLE_SQUAD_RALLIED", "{0} 분대가 전열에 복귀했습니다.", "{0}'s squad has rallied." },
                { "UI_BATTLE_SELECTION_CLEARED", "분대 선택 해제 · 명령은 진영 전체에 적용됩니다.", "Selection cleared · Orders apply to the whole side." },
                { "UI_BATTLE_PAUSED", "일시정지 · Space로 재개", "Paused · Space to resume" },
                { "UI_BATTLE_COMMAND_SQUADS", "{0} (분대 {1}개)", "{0} ({1} squads)" },
                { "UI_BATTLE_COMMAND_ADVANCE", "전진 명령 · 진형 행을 유지하며 적에게 접근합니다.", "Advance · Close in on the enemy while keeping formation rows." },
                { "UI_BATTLE_COMMAND_HOLD", "위치 사수 · 원래 진형으로 복귀하며 피해와 밀치기를 줄입니다.", "Hold · Return to formation and reduce damage and knockback." },
                { "UI_BATTLE_COMMAND_PROTECT", "후열 보호 · 전위가 원거리·지원 인물 앞을 지키며 접근한 적을 막습니다.", "Protect · Frontliners guard ranged and support allies from approaching enemies." },
                { "UI_BATTLE_COMMAND_SPREAD", "분산 · 아군 간격을 넓혀 광역 피해를 줄입니다.", "Spread · Widen spacing to reduce area damage." },
                { "UI_BATTLE_COMMAND_RALLY", "집결 · 교전을 끊고 대장 주변으로 모여 진형을 회복합니다.", "Rally · Break off and regroup around the commander." },
                { "UI_BATTLE_COMMAND_RETREAT", "후퇴 · 진영 끝으로 철수합니다. 전원이 이탈하면 전투가 끝납니다.", "Retreat · Withdraw to your edge. The battle ends when everyone escapes." },
                { "UI_BATTLE_COMMAND_FOCUS", "집중 공격 · {0}을 우선 공격합니다.", "Focus · Prioritize {0}." },
                { "UI_BATTLE_COMMAND_FOCUS_NONE", "집중 공격 · 유효한 표적이 없습니다.", "Focus · No valid target." },
                { "UI_BATTLE_RETREAT_CONFIRM", "후퇴하면 전원이 진영 끝으로 철수하고 패배로 처리됩니다. 3초 안에 다시 눌러 확정하세요.", "Retreating withdraws everyone and counts as a defeat. Press again within 3 seconds to confirm." },
                { "UI_BATTLE_RETREAT_CANCELLED", "후퇴 확인이 취소되었습니다.", "Retreat confirmation cancelled." },
                { "UI_BATTLE_CAMERA_RESET", "카메라를 전장 중앙 기본 시점으로 복원했습니다.", "Camera reset to the battlefield center." },
                { "UI_BATTLE_SELECT_NONE", "선택 해제 · 인물 가까이를 클릭하세요.", "Nothing selected · Click near a character." },
                { "UI_BATTLE_SELECT_INFO", "[{0}] {1} · {9} · HP {3}/{4} · MP {5}/{6} · 분대 사기 {7:0} · {8}", "[{0}] {1} · {9} · HP {3}/{4} · MP {5}/{6} · Squad morale {7:0} · {8}" },
                { "UI_BATTLE_ARCHETYPE_SHIELD", "방진 · 멈추면 정면 방어·돌격 저지, 측면 취약", "Shield · Braced front, stops charges, weak flanks" },
                { "UI_BATTLE_ARCHETYPE_CHARGER", "돌격 · 달려와 치면 돌격 충격", "Charger · Running hits deliver a charge impact" },
                { "UI_BATTLE_ARCHETYPE_SKIRMISHER", "유격 · 빠름, 후방 공격 강화, 후열 노림", "Skirmisher · Fast, deadly from behind, hunts the backline" },
                { "UI_BATTLE_ARCHETYPE_ARCHER", "궁병 · 분대 일제 사격, 착탄 범위 피해", "Archer · Squad volleys with splash on impact" },
                { "UI_BATTLE_ARCHETYPE_CASTER", "술사 · 느리고 강한 마법탄, 주변 피해", "Caster · Slow heavy bolts with splash" },
                { "UI_BATTLE_ARCHETYPE_SUPPORT", "지원 · 다친 아군 자동 치료", "Support · Heals wounded allies" },
                { "UI_BATTLE_ARCHETYPE_COMMANDER", "지휘 · 주변 분대 사기 보호", "Commander · Protects nearby squads' morale" },
                { "UI_BATTLE_TERRAIN_NONE", "평지", "Open ground" },
                { "UI_BATTLE_TERRAIN_HIGH_GROUND", "고지 · 원거리 사거리·피해 증가", "High ground · ranged range and damage up" },
                { "UI_BATTLE_TERRAIN_FOREST", "숲 · 투사체 피해 감소, 이동 감소", "Forest · less projectile damage, slower" },
                { "UI_BATTLE_TERRAIN_NARROW", "좁은 길 · 동시 근접 공격 수 제한", "Narrow pass · fewer melee attackers" },
                { "UI_BATTLE_DEPLOY_START", "전투 시작", "Start Battle" },
                { "UI_BATTLE_DEPLOY_HINT", "배치 단계 · 분대를 선택하고 파란 배치 구역 안을 클릭해 옮기세요. Enter 또는 전투 시작으로 개시합니다.", "Deployment · Select squads and click inside the blue zone to place them. Press Enter or Start Battle to begin." },
                { "UI_BATTLE_DEPLOY_MOVED", "분대 {0}개 배치", "{0} squads placed" },
                { "UI_BATTLE_DEPLOY_STARTED", "전투 개시!", "Battle begins!" },
                { "UI_BATTLE_CONTROL_ON", "{0} 직접 지휘 · 클릭한 곳으로 이동, 분대원이 따라갑니다. Q 스킬 · F 해제", "Commanding {0} · Click to move; the squad follows. Q skill · F release" },
                { "UI_BATTLE_CONTROL_OFF", "직접 지휘 해제 · 분대가 현재 위치를 지킵니다.", "Direct control released · The squad holds its position." },
                { "UI_BATTLE_CONTROL_NEED_SQUAD", "직접 지휘할 분대를 먼저 선택하세요.", "Select a squad to command directly first." },
                { "UI_BATTLE_SKILL_TOOLTIP", "{0}\n{1}\n마나 {2} · 범위 {3:0.#} · 재사용 {4:0.#}초", "{0}\n{1}\nMana {2} · Range {3:0.#} · Cooldown {4:0.#}s" },
                { "UI_BATTLE_SKILL_BUTTON", "{0}\n{1}", "{0}\n{1}" },
                { "UI_BATTLE_SKILL_READY", "MP {0}", "MP {0}" },
                { "UI_BATTLE_SKILL_NOT_ON_FIELD", "전장에 없는 영웅", "Not on the battlefield" },
                { "UI_BATTLE_SKILL_NONE", "배정된 스킬 없음", "No skill assigned" },
                { "UI_BATTLE_SKILL_INCAPACITATED", "전투 불능", "Incapacitated" },
                { "UI_BATTLE_SKILL_COOLDOWN", "재사용 {0:0.0}초", "Cooldown {0:0.0}s" },
                { "UI_BATTLE_SKILL_MANA", "마나 부족 {0}/{1}", "Not enough mana {0}/{1}" }
            };
            for (int row = 0; row < entries.GetLength(0); row += 1)
            {
                SerializedProperty item = null;
                for (int index = 0; index < list.arraySize; index += 1)
                {
                    SerializedProperty candidate = list.GetArrayElementAtIndex(index);
                    if (candidate.FindPropertyRelative("uid").stringValue == entries[row, 0])
                    {
                        item = candidate;
                        break;
                    }
                }
                if (item == null)
                {
                    list.InsertArrayElementAtIndex(list.arraySize);
                    item = list.GetArrayElementAtIndex(list.arraySize - 1);
                }
                item.FindPropertyRelative("uid").stringValue = entries[row, 0];
                item.FindPropertyRelative("korean").stringValue = entries[row, 1];
                item.FindPropertyRelative("english").stringValue = entries[row, 2];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(asset);
        }
    }
}

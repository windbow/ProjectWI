using System.Collections.Generic;
using ProjectWI.Administration;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectWI.EditorTools
{
    public static class WIActivityRetirementSetup
    {
        // 기존 화면 프리팹과 문자열 DB만 편집하는 도구입니다.
        private const string Root = "Assets/Prefabs/Administration/";

        // 일반 활동 메뉴와 버튼을 에셋에서 제거하고 인재실 및 군사 이동 안내를 저장합니다.
        [MenuItem("WI/UI/Retire Character Activities")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying == true)
            {
                Debug.LogError("플레이 모드를 종료한 뒤 메뉴를 정리하세요.");
                return;
            }
            SeedStrings();
            EditTerritory();
            EditTalentOffice();
            AssetDatabase.SaveAssets();
            Debug.Log("인재 활동 제거 및 군사 인물 이동 메뉴 적용 완료");
        }

        // 영지 명령에서 인재 활동을 제거하고 훈련소 바로가기는 군사로 연결합니다.
        private static void EditTerritory()
        {
            string path = Root + "WIAdministrationTerritoryUGUI.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var so = new SerializedObject(root.GetComponent<WIAdministrationTerritoryUGUIController>());
                var buttons = so.FindProperty("commandButtons");
                var actions = so.FindProperty("commandActions");
                var positions = new List<Vector2>();
                var remainingButtons = new List<Button>();
                var remainingActions = new List<int>();
                for (int i = 0; i < actions.arraySize; i += 1)
                {
                    var button = (Button)buttons.GetArrayElementAtIndex(i).objectReferenceValue;
                    positions.Add(((RectTransform)button.transform).anchoredPosition);
                    int action = actions.GetArrayElementAtIndex(i).intValue;
                    if (action == (int)WIAdministrationTerritoryCommand.CharacterActivity)
                    {
                        Object.DestroyImmediate(button.gameObject);
                        continue;
                    }
                    remainingButtons.Add(button);
                    remainingActions.Add(action);
                }
                buttons.arraySize = actions.arraySize = remainingButtons.Count;
                for (int i = 0; i < remainingButtons.Count; i += 1)
                {
                    ((RectTransform)remainingButtons[i].transform).anchoredPosition = positions[i];
                    buttons.GetArrayElementAtIndex(i).objectReferenceValue = remainingButtons[i];
                    actions.GetArrayElementAtIndex(i).intValue = remainingActions[i];
                }
                var basic = so.FindProperty("basicFacilityActions");
                for (int i = 0; i < basic.arraySize; i += 1)
                {
                    if (basic.GetArrayElementAtIndex(i).intValue == (int)WIAdministrationTerritoryCommand.CharacterActivity)
                    {
                        basic.GetArrayElementAtIndex(i).intValue = (int)WIAdministrationTerritoryCommand.Military;
                    }
                }
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // 공용 활동 프리팹을 탐색·영입 두 버튼만 가진 인재실로 정리합니다.
        private static void EditTalentOffice()
        {
            string path = Root + "WIAdministrationCharacterActivityUGUI.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var so = new SerializedObject(root.GetComponent<WIAdministrationCharacterActivityUGUIController>());
                var buttons = so.FindProperty("activityButtons");
                if (buttons.arraySize > 2)
                {
                    var search = buttons.GetArrayElementAtIndex(0).objectReferenceValue;
                    var recruit = buttons.GetArrayElementAtIndex(2).objectReferenceValue;
                    for (int i = 0; i < buttons.arraySize; i += 1)
                    {
                        if (i != 0 && i != 2)
                        {
                            Object.DestroyImmediate(((Button)buttons.GetArrayElementAtIndex(i).objectReferenceValue).gameObject);
                        }
                    }
                    buttons.arraySize = 2;
                    buttons.GetArrayElementAtIndex(0).objectReferenceValue = search;
                    buttons.GetArrayElementAtIndex(1).objectReferenceValue = recruit;
                }
                for (int i = 0; i < 2; i += 1)
                {
                    var button = (Button)buttons.GetArrayElementAtIndex(i).objectReferenceValue;
                    var rect = (RectTransform)button.transform;
                    rect.anchorMin = new Vector2(0, .65f - i * .5f);
                    rect.anchorMax = new Vector2(1, .95f - i * .5f);
                    rect.offsetMin = rect.offsetMax = Vector2.zero;
                }
                var db = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>("Assets/Data/ScriptableObject/Administration/WI_AdministrationDatabase.asset");
                foreach (var label in root.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (label.text.Contains("인재 활동") == true)
                    {
                        label.text = db.GetText("UI_TALENT_OFFICE_ACTORS");
                    }
                }
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // 사용자에게 표시할 메뉴명과 빈 목록·자동 회복 안내를 UID로 등록합니다.
        private static void SeedStrings()
        {
            var db = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>("Assets/Data/ScriptableObject/Administration/WI_AdministrationDatabase.asset");
            var so = new SerializedObject(db);
            var list = so.FindProperty("uiStrings");
            string[,] rows =
            {
                {"UI_ACTIVITY_ACTOR_TITLE", "{0} · 인재실 업무 선택", "{0} · Select Talent Office Task"},
                {"UI_ACTIVITY_TARGET_HINT", "담당자는 {0}입니다. 영입할 영웅을 선택하세요.", "Agent: {0}. Choose the hero to recruit."},
                {"UI_TRANSFER_TITLE", "인물 이동", "Character Transfer"},
                {"UI_TRANSFER_HINT", "출발 성 → 대기 인물 → 목적지 순서로 선택합니다.", "Choose an origin castle, a resident, then a destination."},
                {"UI_TRANSFER_ACTOR_HINT", "이동할 대기 인물을 선택하세요. 전투단원은 전투단 이동을 이용합니다.", "Select an idle resident. Army members move with their army."},
                {"UI_TRANSFER_DESTINATION_HINT", "목적지를 선택하세요. 아군 성 경로를 따라 매달 한 성씩 이동합니다.", "Choose a destination. Travel one friendly castle per month."},
                {"UI_TRANSFER_RESIDENTS", "거주 인물 {0}명", "{0} residents"},
                {"UI_TRANSFER_READY", "이동 가능 · 목적지 선택", "Available · choose destination"},
                {"UI_TRANSFER_BUSY", "이동 불가 · 임무 또는 영지관직 해제 필요", "Unavailable · release duty or governor assignment"},
                {"UI_TRANSFER_NO_ACTORS", "이동할 거주 인물이 없습니다. 전투단에 편성된 인물은 목록에서 제외됩니다.", "No residents to transfer. Army members are excluded."},
                {"UI_TRANSFER_UNAVAILABLE", "플레이어 성의 이동 가능한 인물을 선택하세요.", "Choose an available resident of your castle."},
                {"UI_AUTO_RECOVERY_TITLE", "자동 훈련·회복", "Automatic Training and Recovery"},
                {"UI_AUTO_RECOVERY_HINT", "주둔 인물은 자동으로 훈련하며 과로·부상 시 자동 회복합니다. 별도의 교류·훈련·휴식 지시는 사용하지 않습니다.", "Residents train automatically and recover when fatigued or injured. Manual socializing, training and rest orders are not used."},
                {"UI_TALENT_OFFICE_ACTORS", "인재실 · 담당자 선택", "Talent Office · Select Agent"},
                {"UI_TALENT_OFFICE_NO_ACTOR", "배정 가능한 담당자가 없습니다. 이 성의 대기 영웅 중 [인재영입] 특성을 가진 인물이 필요합니다. 전투단·다른 임무 배정과 인재실 정원을 확인하세요.", "No agent is available. An idle hero with Talent Recruitment is required. Check army assignments, other duties and office capacity."}
            };
            for (int row = 0; row < rows.GetLength(0); row += 1)
            {
                SerializedProperty item = null;
                for (int i = 0; i < list.arraySize; i += 1)
                {
                    var candidate = list.GetArrayElementAtIndex(i);
                    if (candidate.FindPropertyRelative("uid").stringValue == rows[row, 0])
                    {
                        item = candidate;
                        break;
                    }
                }
                if (item == null)
                {
                    list.arraySize += 1;
                    item = list.GetArrayElementAtIndex(list.arraySize - 1);
                }
                item.FindPropertyRelative("uid").stringValue = rows[row, 0];
                item.FindPropertyRelative("korean").stringValue = rows[row, 1];
                item.FindPropertyRelative("english").stringValue = rows[row, 2];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}

using UnityEditor;
using UnityEngine;
using ProjectWI.Administration;

namespace ProjectWI.Editor
{
    public static class WICampaignDifficultySeeder
    {
        private const string DatabasePath = "Assets/Data/ScriptableObject/Administration/WI_AdministrationDatabase.asset";

        [MenuItem("ProjectWI/Data/Seed Campaign Difficulties")]
        // 세 난이도의 표시 문구와 AI 후보 선택 범위를 ScriptableObject에 기록합니다.
        public static void SeedCampaignDifficulties()
        {
            Object database = AssetDatabase.LoadMainAssetAtPath(DatabasePath);
            if (database == null)
            {
                Debug.LogError($"내정 데이터베이스를 찾을 수 없습니다: {DatabasePath}");
                return;
            }

            SerializedObject serializedDatabase = new SerializedObject(database);
            SerializedProperty definitions = serializedDatabase.FindProperty("difficultyDefinitions");
            definitions.arraySize = 3;
            WriteDefinition(definitions.GetArrayElementAtIndex(0), 0, "표준", "Standard",
                "추천 난이도입니다. AI가 상위 두 후보 안에서 상황에 맞는 결정을 내립니다.", 2);
            WriteDefinition(definitions.GetArrayElementAtIndex(1), 1, "여유", "Relaxed",
                "전략 학습에 적합합니다. AI가 상위 세 후보를 폭넓게 검토해 최적 선택 빈도가 낮습니다.", 3);
            WriteDefinition(definitions.GetArrayElementAtIndex(2), 2, "도전", "Hard",
                "숙련자를 위한 난이도입니다. AI가 숨은 보너스 없이 항상 최상위 후보를 선택합니다.", 1);
            serializedDatabase.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            Debug.Log("ProjectWI 캠페인 난이도 3종을 갱신했습니다.");
        }

        [MenuItem("ProjectWI/Verification/Start Standard Campaign")]
        // 플레이 모드에서 표준 난이도 시작 버튼과 동일한 캠페인 진입 경로를 검증합니다.
        public static void StartStandardCampaign()
        {
            if (Application.isPlaying == false)
            {
                Debug.LogWarning("캠페인 시작 검증은 플레이 모드에서 실행해야 합니다.");
                return;
            }

            WIAdministrationUIController controller = Object.FindFirstObjectByType<WIAdministrationUIController>();
            if (controller == null)
            {
                Debug.LogError("내정 UI 컨트롤러를 찾을 수 없습니다.");
                return;
            }
            controller.BeginCampaign(WICampaignDifficulty.Standard);
            Debug.Log("ProjectWI 표준 난이도 캠페인 시작 흐름을 실행했습니다.");
        }

        // 난이도 한 항목의 이름·설명·AI 후보 범위를 기록합니다.
        private static void WriteDefinition(SerializedProperty definition, int difficulty, string korean,
            string english, string description, int candidateWindow)
        {
            definition.FindPropertyRelative("difficulty").enumValueIndex = difficulty;
            WriteLocalized(definition.FindPropertyRelative("displayName"), $"DIFFICULTY_{english.ToUpperInvariant()}", korean, english);
            WriteLocalized(definition.FindPropertyRelative("description"), $"DIFFICULTY_{english.ToUpperInvariant()}_DESC", description, description);
            definition.FindPropertyRelative("aiCandidateWindow").intValue = candidateWindow;
        }

        // 현지화 문자열의 UID와 한글·영문 값을 기록합니다.
        private static void WriteLocalized(SerializedProperty property, string uid, string korean, string english)
        {
            property.FindPropertyRelative("uid").stringValue = uid;
            property.FindPropertyRelative("korean").stringValue = korean;
            property.FindPropertyRelative("english").stringValue = english;
        }
    }
}

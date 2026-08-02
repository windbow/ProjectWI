using UnityEditor;
using UnityEngine;

namespace ProjectWI.Editor
{
    public static class WICastleSpecialtyEffectSeeder
    {
        private const string DatabasePath = "Assets/Data/ScriptableObject/Administration/WI_AdministrationDatabase.asset";

        [MenuItem("ProjectWI/Data/Classify Castle Specialty Effects")]
        // 60개 성의 기존 전문 분야 문구를 공통 효과 유형과 수치로 분류합니다.
        public static void ClassifyCastleSpecialtyEffects()
        {
            Object database = AssetDatabase.LoadMainAssetAtPath(DatabasePath);
            SerializedObject serializedDatabase = new SerializedObject(database);
            SerializedProperty castles = serializedDatabase.FindProperty("castles");
            for (int index = 0; index < castles.arraySize; index += 1)
            {
                SerializedProperty castle = castles.GetArrayElementAtIndex(index);
                string text = castle.FindPropertyRelative("specialty").FindPropertyRelative("korean").stringValue;
                Classify(text, out int effectType, out int projectType, out int value);
                castle.FindPropertyRelative("specialtyEffectType").enumValueIndex = effectType;
                castle.FindPropertyRelative("specialtyProjectType").enumValueIndex = projectType;
                castle.FindPropertyRelative("specialtyEffectValue").intValue = value;
            }
            serializedDatabase.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            Debug.Log($"ProjectWI 성 전문 분야 효과 {castles.arraySize}개를 분류했습니다.");
        }

        // 전문 분야 설명의 핵심어를 실제 수입·사업·방어 효과로 변환합니다.
        private static void Classify(string text, out int effectType, out int projectType, out int value)
        {
            projectType = 0;
            if (ContainsAny(text, "성벽", "성채", "방어전", "요새", "피해", "관문"))
            {
                effectType = 4;
                value = 20;
                return;
            }
            if (ContainsAny(text, "마나", "마법", "마도", "연구", "도서관", "수정탑"))
            {
                effectType = 2;
                value = 8;
                return;
            }
            if (ContainsAny(text, "금화", "시장", "교역", "무역", "상단", "항구", "곡창", "거래"))
            {
                effectType = 1;
                value = 12;
                return;
            }
            if (ContainsAny(text, "영향력", "인재", "영웅", "외교", "명성", "사절", "방문"))
            {
                effectType = 3;
                value = 3;
                return;
            }

            effectType = 0;
            value = 2;
            if (ContainsAny(text, "치안", "순찰", "첩자", "갈등")) projectType = 2;
            else if (ContainsAny(text, "훈련", "숙련", "연무")) projectType = 5;
            else if (ContainsAny(text, "회복", "치유", "부상", "피로")) projectType = 6;
            else if (ContainsAny(text, "건축", "공사", "확장")) projectType = 3;
        }

        // 문장에 지정한 핵심어 중 하나라도 포함되는지 확인합니다.
        private static bool ContainsAny(string text, params string[] keywords)
        {
            foreach (string keyword in keywords)
            {
                if (text.Contains(keyword)) return true;
            }
            return false;
        }
    }
}

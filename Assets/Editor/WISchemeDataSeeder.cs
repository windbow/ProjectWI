using UnityEditor;
using UnityEngine;

namespace ProjectWI.Editor
{
    public static class WISchemeDataSeeder
    {
        private const string DatabasePath = "Assets/Data/ScriptableObject/Administration/WI_AdministrationDatabase.asset";

        private readonly struct SchemeSeed
        {
            public SchemeSeed(string id, string korean, string english, string description, int type, int cost, int chance, int effect, int duration)
            {
                Id = id; Korean = korean; English = english; Description = description;
                Type = type; Cost = cost; Chance = chance; Effect = effect; Duration = duration;
            }

            public string Id { get; }
            public string Korean { get; }
            public string English { get; }
            public string Description { get; }
            public int Type { get; }
            public int Cost { get; }
            public int Chance { get; }
            public int Effect { get; }
            public int Duration { get; }
        }

        private static readonly SchemeSeed[] Seeds =
        {
            new SchemeSeed("scheme_investigation", "조사", "Investigation", "적 성의 수치와 주둔 인물을 3개월간 확인합니다.", 0, 10, 50, 0, 3),
            new SchemeSeed("scheme_counterintelligence", "방첩", "Counterintelligence", "아군 성을 3개월간 적 조사와 계략으로부터 보호합니다.", 1, 10, 100, 0, 3),
            new SchemeSeed("scheme_rumor", "유언비어", "Rumor", "적 성의 치안을 10 낮춥니다.", 2, 15, 45, 10, 0),
            new SchemeSeed("scheme_alienation", "인재 이간", "Alienation", "적 성 인물의 충성 상태를 한 단계 흔듭니다.", 3, 20, 35, 1, 0)
        };

        [MenuItem("ProjectWI/Data/Seed Scheme Definitions")]
        // 기획에 정의된 네 가지 초기 계략을 내정 ScriptableObject에 기록합니다.
        public static void SeedSchemeDefinitions()
        {
            Object database = AssetDatabase.LoadMainAssetAtPath(DatabasePath);
            if (database == null)
            {
                Debug.LogError($"내정 데이터베이스를 찾을 수 없습니다: {DatabasePath}");
                return;
            }

            SerializedObject serializedDatabase = new SerializedObject(database);
            SerializedProperty definitions = serializedDatabase.FindProperty("schemeDefinitions");
            definitions.arraySize = Seeds.Length;
            for (int index = 0; index < Seeds.Length; index++)
            {
                SchemeSeed seed = Seeds[index];
                SerializedProperty definition = definitions.GetArrayElementAtIndex(index);
                definition.FindPropertyRelative("id").stringValue = seed.Id;
                WriteLocalized(definition.FindPropertyRelative("displayName"), seed.Id.ToUpperInvariant(), seed.Korean, seed.English);
                WriteLocalized(definition.FindPropertyRelative("description"), seed.Id.ToUpperInvariant() + "_DESC", seed.Description, seed.Description);
                definition.FindPropertyRelative("schemeType").enumValueIndex = seed.Type;
                definition.FindPropertyRelative("influenceCost").intValue = seed.Cost;
                definition.FindPropertyRelative("baseSuccessChance").intValue = seed.Chance;
                definition.FindPropertyRelative("effectValue").intValue = seed.Effect;
                definition.FindPropertyRelative("durationMonths").intValue = seed.Duration;
            }

            serializedDatabase.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            Debug.Log($"ProjectWI 계략 정의 {Seeds.Length}종을 갱신했습니다.");
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

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProjectWI.Editor
{
    public static class WICommonRosterSeeder
    {
        private const string DatabasePath = "Assets/Data/ScriptableObject/Administration/WI_AdministrationDatabase.asset";

        private readonly struct CommonCharacterSeed
        {
            public CommonCharacterSeed(string id, string korean, string english, int race, int heroClass,
                int leadership, int might, int intelligence, int charisma, int politics, int reputation)
            {
                Id = id;
                Korean = korean;
                English = english;
                Race = race;
                HeroClass = heroClass;
                Leadership = leadership;
                Might = might;
                Intelligence = intelligence;
                Charisma = charisma;
                Politics = politics;
                Reputation = reputation;
            }

            public string Id { get; }
            public string Korean { get; }
            public string English { get; }
            public int Race { get; }
            public int HeroClass { get; }
            public int Leadership { get; }
            public int Might { get; }
            public int Intelligence { get; }
            public int Charisma { get; }
            public int Politics { get; }
            public int Reputation { get; }
        }

        private static readonly CommonCharacterSeed[] Seeds =
        {
            new CommonCharacterSeed("common_gareth", "가레스", "Gareth", 0, 1, 54, 62, 28, 36, 32, 0),
            new CommonCharacterSeed("common_mira", "미라", "Mira", 0, 7, 35, 28, 58, 56, 42, 0),
            new CommonCharacterSeed("common_thane", "테인", "Thane", 2, 1, 48, 65, 30, 28, 44, 5),
            new CommonCharacterSeed("common_nym", "님", "Nym", 1, 4, 42, 55, 47, 38, 34, 5),
            new CommonCharacterSeed("common_raska", "라스카", "Raska", 4, 3, 45, 68, 25, 34, 26, 10),
            new CommonCharacterSeed("common_veil", "베일", "Veil", 6, 5, 40, 60, 52, 32, 30, 10)
        };

        [MenuItem("ProjectWI/Data/Seed Common Roster")]
        // 일반 인물 원본 데이터를 중복 없이 전략 데이터베이스에 추가하거나 갱신합니다.
        public static void SeedCommonRoster()
        {
            Object database = AssetDatabase.LoadMainAssetAtPath(DatabasePath);
            if (database == null)
            {
                Debug.LogError($"ProjectWI 데이터베이스를 찾을 수 없습니다: {DatabasePath}");
                return;
            }

            SerializedObject serializedDatabase = new SerializedObject(database);
            SerializedProperty heroes = serializedDatabase.FindProperty("heroes");
            Dictionary<string, int> indexes = BuildIndex(heroes);

            foreach (CommonCharacterSeed seed in Seeds)
            {
                int index;
                if (indexes.TryGetValue(seed.Id, out index) == false)
                {
                    index = heroes.arraySize;
                    heroes.InsertArrayElementAtIndex(index);
                    indexes.Add(seed.Id, index);
                }

                WriteCharacter(heroes.GetArrayElementAtIndex(index), seed);
            }

            serializedDatabase.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            Debug.Log($"ProjectWI 일반 인물 {Seeds.Length}명 데이터를 갱신했습니다.");
        }

        // 기존 인물 식별자와 배열 위치를 조회합니다.
        private static Dictionary<string, int> BuildIndex(SerializedProperty heroes)
        {
            Dictionary<string, int> result = new Dictionary<string, int>();
            for (int index = 0; index < heroes.arraySize; index++)
            {
                string id = heroes.GetArrayElementAtIndex(index).FindPropertyRelative("id").stringValue;
                if (string.IsNullOrEmpty(id) == false)
                    result[id] = index;
            }

            return result;
        }

        // 일반 인물 한 명의 직렬화 필드를 기획 기본값으로 기록합니다.
        private static void WriteCharacter(SerializedProperty character, CommonCharacterSeed seed)
        {
            character.FindPropertyRelative("id").stringValue = seed.Id;
            SerializedProperty name = character.FindPropertyRelative("displayName");
            name.FindPropertyRelative("uid").stringValue = $"CHARACTER_{seed.Id.ToUpperInvariant()}";
            name.FindPropertyRelative("korean").stringValue = seed.Korean;
            name.FindPropertyRelative("english").stringValue = seed.English;
            character.FindPropertyRelative("race").enumValueIndex = seed.Race;
            character.FindPropertyRelative("heroClass").enumValueIndex = seed.HeroClass;
            character.FindPropertyRelative("grade").enumValueIndex = 1;
            character.FindPropertyRelative("traits").arraySize = 0;
            character.FindPropertyRelative("portrait").objectReferenceValue = null;
            character.FindPropertyRelative("leadership").intValue = seed.Leadership;
            character.FindPropertyRelative("might").intValue = seed.Might;
            character.FindPropertyRelative("intelligence").intValue = seed.Intelligence;
            character.FindPropertyRelative("charisma").intValue = seed.Charisma;
            character.FindPropertyRelative("politics").intValue = seed.Politics;
            character.FindPropertyRelative("loyaltyState").enumValueIndex = 0;
            character.FindPropertyRelative("requiredReputation").intValue = seed.Reputation;
            character.FindPropertyRelative("recruitmentEventId").stringValue = seed.Reputation >= 15
                ? "recruit_faction"
                : (seed.Reputation >= 5 ? "recruit_service" : "recruit_trust");
            SerializedProperty request = character.FindPropertyRelative("recruitmentRequest");
            request.FindPropertyRelative("uid").stringValue = $"CHARACTER_{seed.Id.ToUpperInvariant()}_REQUEST";
            request.FindPropertyRelative("korean").stringValue = "세력의 실력을 증명";
            request.FindPropertyRelative("english").stringValue = "Prove the faction's strength";
            character.FindPropertyRelative("activeSkillAvailable").boolValue = false;
        }
    }
}

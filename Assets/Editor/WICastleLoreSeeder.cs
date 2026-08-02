using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace ProjectWI.Editor
{
    public static class WICastleLoreSeeder
    {
        private const string DatabasePath = "Assets/Data/ScriptableObject/Administration/WI_AdministrationDatabase.asset";
        private const string DesignDocumentPath = "GameDocuments/AdministrationDesign.md";
        private static readonly Regex CastleRowPattern = new Regex(
            @"^\| `(?<id>castle_\d{2})` \| (?<ko>[^|]+) \| (?<en>[^|]+) \| (?<type>[^|]+) \| (?<landmark>[^|]+) \| (?<personality>[^|]+) \|$",
            RegexOptions.Compiled);

        private readonly struct CastleLore
        {
            public CastleLore(string id, string korean, string english, string type, string landmark, string personality)
            {
                Id = id;
                Korean = korean;
                English = english;
                Type = type;
                Landmark = landmark;
                Personality = personality;
            }

            public string Id { get; }
            public string Korean { get; }
            public string English { get; }
            public string Type { get; }
            public string Landmark { get; }
            public string Personality { get; }
        }

        [MenuItem("ProjectWI/Data/Seed Castle Names And Lore")]
        // 기획 문서의 60개 성 이름·유형·랜드마크·내정 개성을 ScriptableObject에 반영합니다.
        public static void SeedCastleNamesAndLore()
        {
            Dictionary<string, CastleLore> loreById = ReadCastleLore();
            if (loreById.Count != 60)
            {
                Debug.LogError($"성 기획 데이터가 60개가 아닙니다: {loreById.Count}");
                return;
            }

            UnityEngine.Object database = AssetDatabase.LoadMainAssetAtPath(DatabasePath);
            if (database == null)
            {
                Debug.LogError($"내정 데이터베이스를 찾을 수 없습니다: {DatabasePath}");
                return;
            }

            SerializedObject serializedDatabase = new SerializedObject(database);
            SerializedProperty castles = serializedDatabase.FindProperty("castles");
            int updatedCount = 0;
            for (int index = 0; index < castles.arraySize; index++)
            {
                SerializedProperty castle = castles.GetArrayElementAtIndex(index);
                string id = castle.FindPropertyRelative("id").stringValue;
                if (loreById.TryGetValue(id, out CastleLore lore) == false)
                {
                    Debug.LogError($"기획 데이터가 없는 성입니다: {id}");
                    continue;
                }

                string uid = id.ToUpperInvariant();
                WriteLocalized(castle.FindPropertyRelative("displayName"), uid, lore.Korean, lore.English);
                WriteLocalized(castle.FindPropertyRelative("terrainTrait"), uid + "_TERRAIN",
                    BuildTerrainTrait(id, lore.Type, false), BuildTerrainTrait(id, lore.Type, true));
                WriteLocalized(castle.FindPropertyRelative("specialty"), uid + "_SPECIALTY",
                    $"{lore.Landmark} · {lore.Personality}", $"{lore.Landmark} · {lore.Personality}");
                updatedCount++;
            }

            serializedDatabase.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            Debug.Log($"ProjectWI 성 고유 명칭·지형·전문 분야 {updatedCount}개를 갱신했습니다.");
        }

        // AdministrationDesign 표에서 성 한 행씩 읽어 기획 데이터 사전을 만듭니다.
        private static Dictionary<string, CastleLore> ReadCastleLore()
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            string fullPath = Path.Combine(projectRoot ?? string.Empty, DesignDocumentPath);
            Dictionary<string, CastleLore> result = new Dictionary<string, CastleLore>();
            foreach (string line in File.ReadAllLines(fullPath))
            {
                Match match = CastleRowPattern.Match(line.Trim());
                if (match.Success == false)
                {
                    continue;
                }

                string id = match.Groups["id"].Value.Trim();
                result[id] = new CastleLore(
                    id,
                    match.Groups["ko"].Value.Trim(),
                    match.Groups["en"].Value.Trim(),
                    match.Groups["type"].Value.Trim(),
                    match.Groups["landmark"].Value.Trim(),
                    match.Groups["personality"].Value.Trim());
            }
            return result;
        }

        // 성 ID의 세력권과 지도 위치를 바탕으로 지역·유형을 합친 지형 특성을 반환합니다.
        private static string BuildTerrainTrait(string id, string type, bool english)
        {
            int number = int.Parse(id.Substring(id.Length - 2));
            string region;
            if (number == 0) region = english ? "Western Starlit Lakeshore" : "서부 별빛 호반";
            else if (number <= 9) region = english ? "Northern Imperial Highlands" : "제국 북부 구릉";
            else if (number <= 19) region = english ? "Central Imperial Plains" : "제국 중앙 평원";
            else if (number <= 25) region = english ? "Southern Imperial Border" : "제국 남부 변경";
            else if (number <= 37) region = english ? "Northern Mountain Range" : "북부 산악 지대";
            else if (number <= 49) region = english ? "Western Ancient Forest" : "서부 고대 수림";
            else region = english ? "Southern Volcanic Wasteland" : "남부 화산 황무지";

            return $"{region} · {(english ? TranslateCastleType(type) : type)}";
        }

        // 한글 성 유형을 영문 지형 설명에 사용할 이름으로 변환합니다.
        private static string TranslateCastleType(string type)
        {
            switch (type)
            {
                case "수도": return "Capital";
                case "요새": return "Fortress";
                case "성역": return "Sanctuary";
                case "마도성": return "Arcane City";
                default: return "City";
            }
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

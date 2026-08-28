using System.Collections.Generic;
using System.Linq;
using ProjectWI.Administration;
using UnityEditor;
using UnityEngine;

namespace ProjectWI.Editor
{
    public static class WICampaignCharacterPlacementSeeder
    {
        private const string DatabasePath =
            "Assets/Data/ScriptableObject/Administration/WI_AdministrationDatabase.asset";
        private const int TargetPlacementCount = 250;

        // 현재 500명 로스터의 절반을 시나리오별 시작 성에 결정적으로 배치합니다.
        [MenuItem("ProjectWI/Data/Build Scenario Character Placements")]
        public static void BuildScenarioCharacterPlacements()
        {
            WIAdministrationDatabaseSO database =
                AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            if (database == null)
            {
                Debug.LogError("행정 데이터베이스를 찾을 수 없습니다.");
                return;
            }

            SerializedObject serialized = new SerializedObject(database);
            SerializedProperty variants = serialized.FindProperty("campaignVariants");
            foreach (WICampaignVariantDefinition definition in database.CampaignVariants)
            {
                int index = database.CampaignVariants.ToList().IndexOf(definition);
                SerializedProperty variant = variants.GetArrayElementAtIndex(index);
                variant.FindPropertyRelative("nonPlayerRecruitmentEnabled").boolValue =
                    definition.Variant == WICampaignVariant.Free;
                BuildVariantPlacements(database, definition, variant);
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            Debug.Log("시나리오 시작 인물 배치 완료 · 각 시나리오 최대 250명");
        }

        // 한 시나리오의 핵심 인물을 먼저 고정하고 남은 인물을 소유 성 비율대로 분배합니다.
        private static void BuildVariantPlacements(
            WIAdministrationDatabaseSO database,
            WICampaignVariantDefinition definition,
            SerializedProperty variant)
        {
            Dictionary<string, string> owners = database.Castles.ToDictionary(castle => castle.Id, castle => castle.FactionId);
            foreach (WICampaignCastlePlacement placement in definition.CastlePlacements)
            {
                if (string.IsNullOrEmpty(placement.FactionId) == false)
                {
                    owners[placement.CastleId] = placement.FactionId;
                }
            }

            List<(string CastleId, string HeroId)> placements = new List<(string, string)>();
            if (definition.Variant == WICampaignVariant.AresMain)
            {
                AddPinned(placements, "castle_28", "ares", "lyria", "hero_009", "hero_010",
                    "common_alden", "common_sable", "common_013", "common_014", "common_015",
                    "common_016", "common_017", "common_018", "common_019", "common_020");
                AddPinned(placements, "castle_00", "elwyn", "selene", "common_gareth", "common_varek");
                AddPinned(placements, "castle_26", "brom", "common_thane", "common_010");
                AddPinned(placements, "castle_38", "morrigan", "common_nym", "common_011");
                AddPinned(placements, "castle_50", "theron", "kael", "common_raska", "common_mira", "common_veil");
            }
            else
            {
                foreach (WIStartingHeroPlacement placement in database.StartingHeroes)
                {
                    placements.Add((placement.CastleId, placement.HeroId));
                }
            }

            HashSet<string> used = new HashSet<string>(placements.Select(item => item.HeroId));
            List<string> eligibleCastles = owners
                .Where(pair => definition.Variant != WICampaignVariant.AresMain ||
                               (pair.Value != "avalon" && pair.Key != "castle_04"))
                .Select(pair => pair.Key)
                .OrderBy(id => id)
                .ToList();
            int castleIndex = 0;
            foreach (WIHeroDefinition hero in database.Heroes.Where(hero => used.Contains(hero.Id) == false))
            {
                if (placements.Count >= TargetPlacementCount || eligibleCastles.Count == 0)
                {
                    break;
                }
                placements.Add((eligibleCastles[castleIndex % eligibleCastles.Count], hero.Id));
                used.Add(hero.Id);
                castleIndex += 1;
            }

            SerializedProperty list = variant.FindPropertyRelative("characterPlacements");
            list.ClearArray();
            HashSet<string> governedCastles = new HashSet<string>();
            foreach ((string castleId, string heroId) in placements)
            {
                list.arraySize += 1;
                SerializedProperty item = list.GetArrayElementAtIndex(list.arraySize - 1);
                item.FindPropertyRelative("castleId").stringValue = castleId;
                item.FindPropertyRelative("heroId").stringValue = heroId;
                bool governor = governedCastles.Add(castleId);
                item.FindPropertyRelative("governor").boolValue = governor;
            }
        }

        // 고정 배치할 핵심 인물 목록을 지정 성에 추가합니다.
        private static void AddPinned(
            List<(string CastleId, string HeroId)> placements,
            string castleId,
            params string[] heroIds)
        {
            placements.AddRange(heroIds.Select(heroId => (castleId, heroId)));
        }
    }
}

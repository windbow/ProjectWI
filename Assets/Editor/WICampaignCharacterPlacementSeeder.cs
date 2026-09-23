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
        private const int TargetPlacementCount = 150;
        private const int TargetHeroPlacementCount = 60;
        private const int TargetCommonPlacementCount = 90;

        // 1200명 중 영웅 60명과 일반 90명만 시나리오 시작 성에 결정적으로 배치합니다.
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
            Debug.Log("시나리오 시작 인물 배치 완료 · 각 시나리오 최대 150명");
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
                AddPinned(placements, "castle_28", "ares", "lyria", "common_alden", "common_sable");
                AddPinned(placements, "castle_00", "elwyn", "selene", "common_gareth", "common_varek");
                AddPinned(placements, "castle_04", "hero_072", "common_049", "common_048");
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
            AddGradePlacements(database, placements, used, eligibleCastles,
                WICharacterGrade.Hero, TargetHeroPlacementCount, ref castleIndex);
            AddGradePlacements(database, placements, used, eligibleCastles,
                WICharacterGrade.Common, TargetCommonPlacementCount, ref castleIndex);

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

        // 지정 등급의 시작 배치 수가 목표에 도달할 때까지 성에 순환 배치합니다.
        private static void AddGradePlacements(
            WIAdministrationDatabaseSO database,
            List<(string CastleId, string HeroId)> placements,
            HashSet<string> used,
            List<string> eligibleCastles,
            WICharacterGrade grade,
            int targetCount,
            ref int castleIndex)
        {
            int placedGradeCount = placements.Count(item => database.GetHero(item.HeroId)?.Grade == grade);
            foreach (WIHeroDefinition hero in database.Heroes.Where(hero =>
                         hero.Grade == grade && used.Contains(hero.Id) == false))
            {
                if (placedGradeCount >= targetCount || placements.Count >= TargetPlacementCount ||
                    eligibleCastles.Count == 0)
                {
                    break;
                }
                placements.Add((eligibleCastles[castleIndex % eligibleCastles.Count], hero.Id));
                used.Add(hero.Id);
                placedGradeCount += 1;
                castleIndex += 1;
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

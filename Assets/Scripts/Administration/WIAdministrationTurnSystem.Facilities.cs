using System.Linq;
using UnityEngine;

namespace ProjectWI.Administration
{
    public static partial class WIAdministrationTurnSystem
    {
        public const int FacilityProjectBonus = 3;
        public const int FacilityTrainingExperienceBonus = 5;
        public const int FacilitySanctuaryRecovery = 15;
        public const int FacilityEmbassyCostDiscount = 5;

        // 성이 지정한 특화 시설을 보유하고 있는지 확인합니다.
        public static bool HasSpecialFacility(WICastleRuntimeState castle, string facilityId)
        {
            return castle?.SpecialFacilityIds != null && castle.SpecialFacilityIds.Contains(facilityId);
        }

        // 진영이 소유한 성 중 하나라도 지정 특화 시설을 보유하는지 확인합니다.
        public static bool HasFactionFacility(WIAdministrationState state, string factionId, string facilityId)
        {
            return state != null && state.Castles.Any(castle =>
                castle.FactionId == factionId && HasSpecialFacility(castle, facilityId));
        }

        // 특화 시설이 해당 월간 사업에 제공하는 성과 보너스를 반환합니다.
        public static int GetFacilityProjectBonus(WICastleRuntimeState castle, WICastleProjectType projectType)
        {
            int bonus = 0;
            if (HasSpecialFacility(castle, "grand_forge") &&
                (projectType == WICastleProjectType.Technology || projectType == WICastleProjectType.Fortification))
            {
                bonus += FacilityProjectBonus;
            }
            if (HasSpecialFacility(castle, "knightly_order") &&
                (projectType == WICastleProjectType.Training || projectType == WICastleProjectType.Fortification))
            {
                bonus += FacilityProjectBonus;
            }
            if (HasSpecialFacility(castle, "adventurers_guild") && projectType == WICastleProjectType.Recruitment)
            {
                bonus += FacilityProjectBonus;
            }
            if (HasSpecialFacility(castle, "sanctuary") &&
                (projectType == WICastleProjectType.Recovery || projectType == WICastleProjectType.Stability))
            {
                bonus += FacilityProjectBonus;
            }
            return bonus;
        }

        // 비플레이어 진영이 확장으로 얻은 시설 선택권을 성향에 맞춰 자동 처리합니다.
        private static void ResolveAISpecialFacilityChoices(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state)
        {
            foreach (WICastleRuntimeState castle in state.Castles.Where(castle =>
                         castle.FactionId != state.PlayerFactionId && castle.PendingSpecialFacilityChoice))
            {
                if (castle.SpecialFacilityIds.Count >= castle.GetSpecialFacilitySlotCount())
                {
                    castle.PendingSpecialFacilityChoice = false;
                    continue;
                }

                WIFactionDefinition faction = database.GetFaction(castle.FactionId);
                string[] preferences = GetAIFacilityPreferences(faction == null
                    ? WIAIStrategy.Prosperity : faction.AIStrategy);
                string facilityId = preferences.FirstOrDefault(id =>
                    castle.SpecialFacilityIds.Contains(id) == false && database.GetSpecialFacility(id) != null);
                if (string.IsNullOrEmpty(facilityId))
                {
                    facilityId = database.SpecialFacilities
                        .Select(facility => facility.Id)
                        .FirstOrDefault(id => castle.SpecialFacilityIds.Contains(id) == false);
                }
                if (string.IsNullOrEmpty(facilityId))
                {
                    continue;
                }

                castle.SpecialFacilityIds.Add(facilityId);
                castle.PendingSpecialFacilityChoice = false;
            }
        }

        // AI 전략별 특화 시설 우선순위를 반환합니다.
        private static string[] GetAIFacilityPreferences(WIAIStrategy strategy)
        {
            switch (strategy)
            {
                case WIAIStrategy.Development:
                    return new[] { "mage_tower", "grand_forge", "grand_market", "sanctuary" };
                case WIAIStrategy.Defense:
                    return new[] { "knightly_order", "sanctuary", "grand_forge", "embassy" };
                case WIAIStrategy.Aggressive:
                    return new[] { "grand_forge", "knightly_order", "adventurers_guild", "spy_outpost" };
                case WIAIStrategy.Scheme:
                    return new[] { "spy_outpost", "adventurers_guild", "embassy", "mage_tower" };
                default:
                    return new[] { "grand_market", "embassy", "adventurers_guild", "sanctuary" };
            }
        }

        // 성소와 기사단의 상시 효과를 주둔 인물과 영지 상태에 적용합니다.
        private static void ResolveFacilityPassives(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WITurnSummary summary)
        {
            foreach (WICastleRuntimeState castle in state.Castles)
            {
                if (HasSpecialFacility(castle, "sanctuary"))
                {
                    castle.Stability = Mathf.Clamp(castle.Stability + 1, 0, 100);
                    foreach (string heroId in castle.HeroIds)
                    {
                        WICharacterRuntimeState character = state.GetCharacter(heroId);
                        if (character == null || character.Recruited == false)
                        {
                            continue;
                        }
                        character.Fatigue = Mathf.Max(0, character.Fatigue - FacilitySanctuaryRecovery);
                        character.InjuryMonths = Mathf.Max(0, character.InjuryMonths - 1);
                    }
                }

                if (HasSpecialFacility(castle, "knightly_order"))
                {
                    foreach (string heroId in castle.HeroIds)
                    {
                        WICharacterRuntimeState character = state.GetCharacter(heroId);
                        if (character != null && character.Recruited && character.Activity == WICharacterActivityType.Training)
                        {
                            character.Experience += FacilityTrainingExperienceBonus;
                        }
                    }
                }
            }
        }
    }
}

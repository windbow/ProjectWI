using System.Linq;
using UnityEngine;

namespace ProjectWI.Administration
{
    public static partial class WIAdministrationTurnSystem
    {
        // 사업과 일치하는 담당 인물 특기의 고유 결과를 적용하고 월간 보고 문구를 추가합니다.
        public static void ApplyTraitUniqueEffects(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WICastleRuntimeState castleState,
            WICastleProjectState project,
            WIHeroDefinition manager,
            WITurnSummary summary)
        {
            if (manager == null || castleState == null || project == null)
            {
                return;
            }

            WIFactionRuntimeState faction = state.GetFactionState(castleState.FactionId);
            foreach (WITraitType traitType in manager.Traits)
            {
                WITraitDefinition trait = database.GetTrait(traitType);
                if (trait == null || trait.ProjectTypes.Contains(project.ProjectType) == false)
                {
                    continue;
                }

                int value = Mathf.Max(1, trait.UniqueEffectValue);
                string result = string.Empty;
                switch (traitType)
                {
                    case WITraitType.Agronomist:
                        foreach (WIArmyState army in state.Armies.Where(item =>
                                     item.FactionId == castleState.FactionId && item.CurrentCastleId == castleState.CastleId))
                        {
                            army.Supply = WISupplyState.Sufficient;
                        }
                        result = "주둔 전투단 보급 충분";
                        break;
                    case WITraitType.Merchant:
                        if (faction != null && faction.FactionId == state.PlayerFactionId)
                        {
                            summary.GoldGained += value;
                        }
                        else if (faction != null)
                        {
                            faction.Gold += value;
                        }
                        result = $"추가 금화 +{value}";
                        break;
                    case WITraitType.Architect:
                        int cost = GetProjectCost(database, project.ProjectType, project.Investment);
                        int refund = Mathf.Max(1, cost * value / 100);
                        if (faction != null && faction.FactionId == state.PlayerFactionId)
                        {
                            summary.GoldGained += refund;
                        }
                        else if (faction != null)
                        {
                            faction.Gold += refund;
                        }
                        result = $"공사비 절감 {refund}G";
                        break;
                    case WITraitType.Constable:
                        castleState.CounterintelligenceMonths = Mathf.Max(castleState.CounterintelligenceMonths, value);
                        result = $"방첩 강화 {value}개월";
                        break;
                    case WITraitType.Scholar:
                        if (faction != null && string.IsNullOrEmpty(faction.ActiveResearchId) == false)
                        {
                            faction.ResearchRemainingMonths = Mathf.Max(0, faction.ResearchRemainingMonths - value);
                            result = $"연구 기간 {value}개월 단축";
                        }
                        else
                        {
                            if (faction != null && faction.FactionId == state.PlayerFactionId)
                            {
                                summary.ManaGained += value;
                            }
                            else if (faction != null)
                            {
                                faction.ManaCrystal += value;
                            }
                            result = $"연구 마나 +{value}";
                        }
                        break;
                    case WITraitType.Negotiator:
                        if (faction != null && faction.FactionId == state.PlayerFactionId)
                        {
                            summary.InfluenceGained += value;
                        }
                        else if (faction != null)
                        {
                            faction.Influence += value;
                        }
                        result = $"교섭 영향력 +{value}";
                        break;
                    case WITraitType.Doctor:
                        foreach (string heroId in castleState.HeroIds)
                        {
                            WICharacterRuntimeState character = state.GetCharacter(heroId);
                            if (character == null)
                            {
                                continue;
                            }
                            character.InjuryMonths = Mathf.Max(0, character.InjuryMonths - value);
                            character.Fatigue = Mathf.Max(0, character.Fatigue - value * 10);
                        }
                        result = $"주둔 인물 부상 {value}개월·피로 {value * 10} 회복";
                        break;
                    case WITraitType.Instructor:
                        foreach (WIArmyState army in state.Armies.Where(item =>
                                     item.FactionId == castleState.FactionId && item.CurrentCastleId == castleState.CastleId))
                        {
                            army.CohesionExperience += value;
                            UpdateArmyProficiency(army);
                        }
                        result = $"주둔 전투단 숙련 경험 +{value}";
                        break;
                }

                if (string.IsNullOrEmpty(result) == false && faction != null && faction.FactionId == state.PlayerFactionId)
                {
                    summary.News.Add($"특기 발동 · {trait.DisplayName.Get(database.UseEnglish)} · {result}");
                }
            }
        }

        // 성에 남은 영웅의 흔적 중 현재 사업과 일치하는 성과 보너스를 합산합니다.
        public static int GetLegacyBonus(WICastleRuntimeState castleState, WICastleProjectType projectType)
        {
            int bonus = 0;
            foreach (WIHeroLegacyState legacy in castleState.HeroLegacies)
            {
                if (legacy.ProjectType == projectType)
                {
                    bonus += legacy.Bonus;
                }
            }

            return bonus;
        }

        // 높은 성과를 낸 사업에서 선택 사건과 영웅의 흔적 후보를 생성합니다.
        private static void CreateProjectEventAndLegacyChoice(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WICastleRuntimeState castleState,
            WICastleProjectState project,
            WIHeroDefinition manager,
            int gain,
            WITurnSummary summary)
        {
            // 후방 위임은 통상 사건을 영지관이 처리하여 월별 선택 창을 누적시키지 않습니다.
            if (project.Delegated)
            {
                return;
            }
            bool supportsEvent = project.ProjectType == WICastleProjectType.Prosperity ||
                                 project.ProjectType == WICastleProjectType.Technology ||
                                 project.ProjectType == WICastleProjectType.Stability ||
                                 project.ProjectType == WICastleProjectType.Fortification ||
                                 project.ProjectType == WICastleProjectType.Recruitment;
            if (gain >= 10 && supportsEvent && state.PendingProjectEvents.Count < 2 &&
                state.PendingProjectEvents.Exists(item => item.CastleId == castleState.CastleId) == false)
            {
                state.PendingProjectEvents.Add(new WIPendingProjectEvent
                {
                    CastleId = castleState.CastleId,
                    ProjectType = project.ProjectType,
                    HeroId = project.ManagerHeroId,
                    Title = GetProjectEventTitle(project.ProjectType),
                    HeroChoiceAvailable = GetProjectTraitBonus(database, project.ProjectType, manager) > 0
                });
                summary.News.Add($"선택 사건 발생 · {GetProjectEventTitle(project.ProjectType)}");
            }

            if (gain < 14 || manager == null || state.PendingLegacyChoices.Count >= 1 ||
                castleState.HeroLegacies.Exists(item => item.HeroId == manager.Id && item.ProjectType == project.ProjectType))
            {
                return;
            }

            string heroName = manager.DisplayName.Get(database.UseEnglish);
            WIHeroLegacyDefinition legacyDefinition = database.GetHeroLegacy(project.ProjectType);
            if (legacyDefinition == null)
            {
                return;
            }
            state.PendingLegacyChoices.Add(new WIPendingLegacyChoice
            {
                CastleId = castleState.CastleId,
                Legacy = new WIHeroLegacyState
                {
                    DefinitionId = legacyDefinition.Id,
                    HeroId = manager.Id,
                    ProjectType = project.ProjectType,
                    DisplayName = $"{heroName}의 {legacyDefinition.DisplayName.Get(database.UseEnglish)}",
                    Description = legacyDefinition.Description.Get(database.UseEnglish),
                    Bonus = legacyDefinition.ProjectGainBonus
                }
            });
            summary.News.Add($"영웅의 흔적 후보 · {heroName}");
        }

        // 새 흔적을 설치하고 교체 대상은 효과 없는 기념 기록으로 보존합니다.
        public static bool InstallHeroLegacy(
            WIAdministrationState state,
            WICastleRuntimeState castle,
            WIPendingLegacyChoice pendingChoice,
            WIHeroLegacyState replacedLegacy = null)
        {
            if (castle == null || pendingChoice == null || state.PendingLegacyChoices.Contains(pendingChoice) == false)
            {
                return false;
            }
            if (castle.HeroLegacies.Count >= 2 && replacedLegacy == null)
            {
                return false;
            }
            if (replacedLegacy != null)
            {
                if (castle.HeroLegacies.Remove(replacedLegacy) == false)
                {
                    return false;
                }
                castle.CommemoratedHeroLegacies.Add(replacedLegacy);
            }
            castle.HeroLegacies.Add(pendingChoice.Legacy);
            state.PendingLegacyChoices.Remove(pendingChoice);
            return true;
        }

        // 사업 종류에 대응하는 사건 제목을 반환합니다.
        private static string GetProjectEventTitle(WICastleProjectType projectType)
        {
            switch (projectType)
            {
                case WICastleProjectType.Prosperity: return "상단과 주민의 분쟁";
                case WICastleProjectType.Technology: return "불안정한 고대 유물";
                case WICastleProjectType.Stability: return "도적과 숨은 첩자";
                case WICastleProjectType.Fortification: return "공사 중 발견된 비밀 통로";
                case WICastleProjectType.Recruitment: return "인재를 둘러싼 경쟁자";
                default: return "사업 현장의 뜻밖의 기회";
            }
        }

        // 공훈과 영향력 조건을 확인해 인물에게 작위를 수여합니다.
        public static bool AwardTitle(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            string factionId,
            string heroId,
            string titleId)
        {
            WIFactionRuntimeState faction = state.GetFactionState(factionId);
            WICharacterRuntimeState character = state.GetCharacter(heroId);
            WITitleDefinition title = database.GetTitle(titleId);
            if (faction == null || character == null || title == null || character.Recruited == false ||
                character.Merit < title.RequiredMerit || faction.Influence < title.InfluenceCost ||
                IsHeroInFaction(state, heroId, factionId) == false || character.TitleId == titleId)
            {
                return false;
            }
            faction.Influence -= title.InfluenceCost;
            character.TitleId = title.Id;
            character.LoyaltyState = WILoyaltyState.Stable;
            return true;
        }

        // 담당 인물의 현재 작위가 제공하는 사업 보너스를 반환합니다.
        private static int GetTitleProjectBonus(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WIHeroDefinition hero)
        {
            if (hero == null)
            {
                return 0;
            }
            WITitleDefinition title = database.GetTitle(state.GetCharacter(hero.Id)?.TitleId);
            return title == null ? 0 : title.ProjectBonus;
        }

    }
}

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProjectWI.Administration
{
    public static partial class WIAdministrationTurnSystem
    {
        // AI가 성향, 선행 조건과 실제 마나 보유량에 맞춰 연구를 선택합니다.
        private static void PlanAIResearch(WIAdministrationDatabaseSO database, WIAdministrationState state, WITurnSummary summary)
        {
            foreach (WIFactionDefinition factionDefinition in database.Factions.Where(item => item.PlayerFaction == false))
            {
                WIFactionRuntimeState faction = state.GetFactionState(factionDefinition.Id);
                if (faction == null || faction.Eliminated || string.IsNullOrEmpty(faction.ActiveResearchId) == false)
                {
                    continue;
                }
                WICharacterRuntimeState researcher = state.Characters.FirstOrDefault(character =>
                    character.Recruited && IsHeroInFaction(state, character.HeroId, faction.FactionId) &&
                    CanResearch(state, character.HeroId) &&
                    state.IsCharacterBusy(character.HeroId) == false);
                if (researcher == null)
                {
                    continue;
                }
                int maximumTechnology = state.Castles.Where(castle => castle.FactionId == faction.FactionId)
                    .Select(castle => castle.Technology).DefaultIfEmpty(0).Max();
                List<WIResearchDefinition> candidates = database.ResearchDefinitions
                    .Where(research => faction.CompletedResearchIds.Contains(research.Id) == false)
                    .Where(research => faction.ManaCrystal >= research.ManaCost && maximumTechnology >= research.RequiredTechnology)
                    .Where(research => string.IsNullOrEmpty(research.PrerequisiteResearchId) ||
                                       faction.CompletedResearchIds.Contains(research.PrerequisiteResearchId))
                    .OrderByDescending(research => GetAIResearchPreference(factionDefinition.AIStrategy, research.EffectType)).ToList();
                if (candidates.Count == 0)
                {
                    continue;
                }
                int factionSalt = database.Factions.ToList().FindIndex(item => item.Id == factionDefinition.Id);
                int selectedIndex = GetAICandidateIndex(database, state, candidates.Count, factionSalt);
                WIResearchDefinition research = candidates[selectedIndex];
                if (BeginResearch(database, state, faction.FactionId, research.Id, researcher.HeroId))
                {
                    WIResearchDefinition alternative = candidates.FirstOrDefault(item => item.Id != research.Id);
                    int selectedScore = GetAIResearchPreference(factionDefinition.AIStrategy, research.EffectType);
                    int alternativeScore = alternative == null ? 0 : GetAIResearchPreference(factionDefinition.AIStrategy, alternative.EffectType);
                    AddAIReasonReport(summary, factionDefinition.Id, factionDefinition.DisplayName.Get(database.UseEnglish), "연구",
                        $"상위 {GetAICandidateWindow(database, state, candidates.Count)}개 중 {selectedIndex + 1}순위 " +
                        $"{research.DisplayName.Get(database.UseEnglish)} 선택 · 성향 적합 {selectedScore}점" +
                        (alternative == null ? string.Empty : $" · 비교 {alternative.DisplayName.Get(database.UseEnglish)} {alternativeScore}점"));
                }
            }
        }

        // AI 성향과 연구 효과가 일치하는 정도를 우선순위 점수로 반환합니다.
        private static int GetAIResearchPreference(WIAIStrategy strategy, WIResearchEffectType effectType)
        {
            if (strategy == WIAIStrategy.Prosperity && effectType == WIResearchEffectType.GoldIncomePercent)
            {
                return 10;
            }
            if (strategy == WIAIStrategy.Development && (effectType == WIResearchEffectType.ManaIncomePerCastle || effectType == WIResearchEffectType.ProjectGain))
            {
                return 10;
            }
            if (strategy == WIAIStrategy.Defense && effectType == WIResearchEffectType.InfluenceIncomePerCastle)
            {
                return 10;
            }
            if (strategy == WIAIStrategy.Aggressive && effectType == WIResearchEffectType.BattlePower)
            {
                return 10;
            }
            if (strategy == WIAIStrategy.Scheme && effectType == WIResearchEffectType.ProjectGain)
            {
                return 10;
            }
            return 1;
        }

        // 조건과 비용을 확인해 진영의 연구를 시작합니다.
        public static bool BeginResearch(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            string factionId,
            string researchId,
            string researcherHeroId)
        {
            WIFactionRuntimeState faction = state.GetFactionState(factionId);
            WIResearchDefinition research = database.GetResearch(researchId);
            WICharacterRuntimeState researcher = state.GetCharacter(researcherHeroId);
            if (faction == null || research == null || researcher == null || researcher.Recruited == false ||
                CanResearch(state, researcherHeroId) == false ||
                string.IsNullOrEmpty(faction.ActiveResearchId) == false || faction.CompletedResearchIds.Contains(researchId) ||
                faction.ManaCrystal < research.ManaCost || IsHeroInFaction(state, researcherHeroId, factionId) == false ||
                state.IsCharacterBusy(researcherHeroId, ignoreArmy: true) == true)
            {
                return false;
            }
            if (string.IsNullOrEmpty(research.PrerequisiteResearchId) == false &&
                faction.CompletedResearchIds.Contains(research.PrerequisiteResearchId) == false)
            {
                return false;
            }
            int maximumTechnology = state.Castles
                .Where(castle => castle.FactionId == factionId)
                .Select(castle => castle.Technology)
                .DefaultIfEmpty(0).Max();
            if (maximumTechnology < research.RequiredTechnology)
            {
                return false;
            }
            faction.ManaCrystal -= research.ManaCost;
            faction.ActiveResearchId = research.Id;
            faction.ResearcherHeroId = researcherHeroId;
            int mageTowerReduction = HasFactionFacility(state, factionId, "mage_tower") ? 1 : 0;
            faction.ResearchRemainingMonths = Mathf.Max(1, research.DurationMonths - mageTowerReduction);
            return true;
        }

        // 진행 중인 진영 연구의 남은 기간을 계산하고 완료 효과를 활성화합니다.
        private static void ResolveFactionResearch(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WITurnSummary summary)
        {
            foreach (WIFactionRuntimeState factionState in state.Factions)
            {
                if (string.IsNullOrEmpty(factionState.ActiveResearchId))
                {
                    continue;
                }
                if (CanResearch(state, factionState.ResearcherHeroId) == false ||
                    IsHeroInFaction(state, factionState.ResearcherHeroId, factionState.FactionId) == false)
                {
                    factionState.ResearcherHeroId = state.Characters
                        .Where(character => character.Recruited == true && CanResearch(state, character.HeroId) == true &&
                            IsHeroInFaction(state, character.HeroId, factionState.FactionId) == true &&
                            state.IsCharacterBusy(character.HeroId, ignoreArmy: true) == false)
                        .OrderByDescending(character => database.GetHero(character.HeroId)?.Intelligence ?? 0)
                        .Select(character => character.HeroId).FirstOrDefault() ?? string.Empty;
                    if (string.IsNullOrEmpty(factionState.ResearcherHeroId) == true)
                    {
                        continue;
                    }
                }
                factionState.ResearchRemainingMonths -= 1;
                if (factionState.ResearchRemainingMonths > 0)
                {
                    continue;
                }
                WIResearchDefinition research = database.GetResearch(factionState.ActiveResearchId);
                if (research != null && factionState.CompletedResearchIds.Contains(research.Id) == false)
                {
                    factionState.CompletedResearchIds.Add(research.Id);
                    if (factionState.FactionId == state.PlayerFactionId)
                    {
                        summary.News.Add($"연구 완료 · {research.DisplayName.Get(database.UseEnglish)}");
                    }
                }
                factionState.ActiveResearchId = string.Empty;
                factionState.ResearcherHeroId = string.Empty;
                factionState.ResearchRemainingMonths = 0;
            }
        }

        // 완료 연구의 자원 수입 효과를 성별 월간 수입에 적용합니다.
        private static void ApplyResearchIncomeBonus(
            WIAdministrationDatabaseSO database,
            WIFactionRuntimeState faction,
            WITurnSummary income)
        {
            if (faction == null)
            {
                return;
            }
            int goldPercent = GetResearchEffectBonus(database, faction, WIResearchEffectType.GoldIncomePercent);
            income.GoldGained += income.GoldGained * goldPercent / 100;
            income.ManaGained += GetResearchEffectBonus(database, faction, WIResearchEffectType.ManaIncomePerCastle);
            income.InfluenceGained += GetResearchEffectBonus(database, faction, WIResearchEffectType.InfluenceIncomePerCastle);
        }

        // 완료한 연구 중 지정한 효과 유형의 총 보너스를 반환합니다.
        private static int GetResearchEffectBonus(
            WIAdministrationDatabaseSO database,
            WIFactionRuntimeState faction,
            WIResearchEffectType effectType)
        {
            if (faction == null)
            {
                return 0;
            }
            return faction.CompletedResearchIds
                .Select(database.GetResearch)
                .Where(research => research != null && research.EffectType == effectType)
                .Sum(research => research.EffectValue);
        }
    }
}

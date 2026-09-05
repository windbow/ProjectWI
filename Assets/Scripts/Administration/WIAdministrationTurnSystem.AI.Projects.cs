using System.Collections.Generic;
using System.Linq;

namespace ProjectWI.Administration
{
    public static partial class WIAdministrationTurnSystem
    {
        // AI 성향과 성 상태에 따라 비용을 지불하고 중점 사업을 배치합니다.
        private static void AssignAIProject(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WICastleRuntimeState castleState,
            WIFactionDefinition faction,
            WIFactionRuntimeState factionState,
            WITurnSummary summary)
        {
            int projectCost = GetProjectCost(database, WICastleProjectType.Prosperity, WIProjectInvestment.Basic);
            if (castleState.ActiveProject != null || factionState.Gold < projectCost)
            {
                return;
            }

            WICastleProjectType projectType = ChooseAIProject(castleState, faction.AIStrategy);
            WICampaignVariantDefinition variant = database.GetCampaignVariant(state.CampaignVariant);
            if (projectType == WICastleProjectType.Recruitment &&
                variant?.NonPlayerRecruitmentEnabled == false)
            {
                projectType = WICastleProjectType.Prosperity;
            }
            WIHeroDefinition manager = SelectAIProjectManager(database, state, castleState, projectType);
            if (manager == null)
            {
                return;
            }

            factionState.Gold -= projectCost;
            castleState.ActiveProject = new WICastleProjectState
            {
                ProjectType = projectType,
                Investment = WIProjectInvestment.Basic,
                ManagerHeroId = manager.Id,
                RemainingMonths = 1
            };
            string reason = castleState.Stability < 35 ? $"질서 {castleState.Stability} 보완" :
                castleState.Defense < 35 ? $"방어 {castleState.Defense} 보완" : $"{GetAIStrategyReason(faction.AIStrategy)} 성향 우선";
            AddAIReasonReport(summary, faction.Id, faction.DisplayName.Get(database.UseEnglish), "사업",
                $"{database.GetCastle(castleState.CastleId).DisplayName.Get(database.UseEnglish)}에서 {projectType} 선택 · {reason}");
        }

        // 현재 난이도의 후보 범위 안에서 AI 사업 담당 인물을 결정합니다.
        public static WIHeroDefinition SelectAIProjectManager(WIAdministrationDatabaseSO database,
            WIAdministrationState state, WICastleRuntimeState castleState, WICastleProjectType projectType)
        {
            List<WIHeroDefinition> candidates = castleState.HeroIds
                .Where(heroId => state.IsCharacterBusy(heroId) == false)
                .Where(heroId => IsAdministrationCapable(state, heroId))
                .Select(database.GetHero)
                .Where(hero => hero != null)
                .OrderByDescending(hero => GetExpectedProjectGain(database, projectType, hero, WIProjectInvestment.Basic))
                .ToList();
            if (candidates.Count == 0)
            {
                return null;
            }

            int castleNumber = int.Parse(castleState.CastleId.Substring(castleState.CastleId.Length - 2));
            return candidates[GetAICandidateIndex(database, state, candidates.Count, castleNumber)];
        }

        // AI 성향과 가장 취약한 성 수치를 조합해 사업 종류를 선택합니다.
        private static WICastleProjectType ChooseAIProject(WICastleRuntimeState castle, WIAIStrategy strategy)
        {
            if (castle.Stability < 35)
            {
                return WICastleProjectType.Stability;
            }
            if (castle.Defense < 35)
            {
                return WICastleProjectType.Fortification;
            }
            switch (strategy)
            {
                case WIAIStrategy.Development: return WICastleProjectType.Technology;
                case WIAIStrategy.Defense: return WICastleProjectType.Fortification;
                case WIAIStrategy.Aggressive: return WICastleProjectType.Training;
                case WIAIStrategy.Scheme: return WICastleProjectType.Recruitment;
                default: return WICastleProjectType.Prosperity;
            }
        }
    }
}

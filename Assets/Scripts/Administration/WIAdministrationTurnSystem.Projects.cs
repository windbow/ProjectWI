using System.Linq;
using UnityEngine;

namespace ProjectWI.Administration
{
    public static partial class WIAdministrationTurnSystem
    {
        // 성에 지정된 월간 중점 사업을 계산하고 관련 성 수치를 반영합니다.
        private static void ResolveCastleProject(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WICastleRuntimeState castleState,
            WITurnSummary summary)
        {
            WICastleProjectState project = castleState.ActiveProject;
            if (project == null)
            {
                return;
            }

            WIHeroDefinition manager = database.GetHero(project.ManagerHeroId);
            if (IsAdministrationCapable(state, project.ManagerHeroId) == false ||
                castleState.HeroIds.Contains(project.ManagerHeroId) == false)
            {
                castleState.ActiveProject = null;
                castleState.StandingProject = null;
                return;
            }
            WIFactionRuntimeState factionState = state.GetFactionState(castleState.FactionId);
            WIFactionPolicy policy = factionState == null ? WIFactionPolicy.Prosperity : factionState.Policy;
            int gain = GetExpectedProjectGain(database, project.ProjectType, manager, project.Investment) +
                       GetFactionPolicyBonus(database, policy, project.ProjectType) +
                       GetResearchEffectBonus(database, factionState, WIResearchEffectType.ProjectGain) +
                       GetTitleProjectBonus(database, state, manager) +
                       GetLegacyBonus(castleState, project.ProjectType) +
                       GetCastleSpecialtyProjectBonus(database.GetCastle(castleState.CastleId), project.ProjectType) +
                       GetFacilityProjectBonus(castleState, project.ProjectType);

            ApplyProjectGain(castleState, project.ProjectType, gain);
            if (project.ProjectType == WICastleProjectType.Training)
            {
                ApplyTrainingProjectGain(state, castleState, gain);
            }
            project.RemainingMonths -= 1;
            if (project.RemainingMonths > 0)
            {
                return;
            }

            ApplyTraitUniqueEffects(database, state, castleState, project, manager, summary);

            // 확장 사업이 완료되면 성 규모를 한 단계 올립니다.
            if (project.ProjectType == WICastleProjectType.Expansion &&
                castleState.CastleSize < WICastleSize.Large)
            {
                castleState.CastleSize += 1;
                castleState.PendingSpecialFacilityChoice = true;
            }

            if (project.Delegated)
            {
                string castleName = database.GetCastle(castleState.CastleId).DisplayName.Get(database.UseEnglish);
                summary.DelegationReports.Add($"[위임 결과] {castleName} · {project.ProjectType} · 예상 +{project.ExpectedGain} / 실제 +{gain} · 비용 {project.GoldCost}G");
            }

            WICastleDefinition castle = database.GetCastle(castleState.CastleId);
            string managerName = manager == null
                ? database.GetText("UI_NO_MANAGER")
                : manager.DisplayName.Get(database.UseEnglish);
            string resultText = project.ProjectType == WICastleProjectType.Expansion
                ? $"{castleState.CastleSize} 규모 확장 완료 · 특화 시설 선택 가능"
                : $"{project.ProjectType} +{gain}";
            WIFactionDefinition faction = database.GetFaction(castleState.FactionId);
            if (faction != null && faction.PlayerFaction)
            {
                summary.News.Add($"{castle.DisplayName.Get(database.UseEnglish)} · {resultText} · {managerName}");
                CreateProjectEventAndLegacyChoice(database, state, castleState, project, manager, gain, summary);
            }
            else
            {
                summary.AIProjectsCompleted += 1;
                if (summary.AIProjectFactionIds.Contains(faction.Id) == false)
                {
                    summary.AIProjectFactionIds.Add(faction.Id);
                }
                if (IsAdjacentToPlayer(database, state, castleState))
                {
                    summary.News.Add($"AI 동향 · {castle.DisplayName.Get(database.UseEnglish)}에서 {project.ProjectType} 사업 완료");
                }
            }
            castleState.ActiveProject = null;
        }


        // 영지관 위임 성에 이번 달 사업과 담당자를 자동으로 배치합니다.
        private static void AssignDelegatedProject(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WICastleRuntimeState castleState,
            WITurnSummary summary)
        {
            if (castleState.DelegatedToGovernor == false || castleState.ActiveProject != null)
            {
                return;
            }

            WIHeroDefinition governor = database.GetHero(castleState.GovernorHeroId);
            if (governor == null || castleState.HeroIds.Contains(governor.Id) == false ||
                IsAdministrationCapable(state, governor.Id) == false)
            {
                summary.DelegationReports.Add($"{database.GetCastle(castleState.CastleId).DisplayName.Get(database.UseEnglish)} · 영지관 부재 · 주둔 인물을 영지관로 임명하면 다음 달부터 위임 가능");
                return;
            }

            if (state.IsCharacterBusy(governor.Id))
            {
                summary.DelegationReports.Add($"{database.GetCastle(castleState.CastleId).DisplayName.Get(database.UseEnglish)} · 영지관이 다른 임무 수행 중 · 임무 종료 후 위임 재개");
                return;
            }

            int budget = castleState.GovernorMonthlyBudget >= database.ProjectBalance.IntensiveCost
                ? database.ProjectBalance.IntensiveCost
                : database.ProjectBalance.BasicCost;
            if (state.Gold < budget)
            {
                summary.DelegationReports.Add($"{database.GetCastle(castleState.CastleId).DisplayName.Get(database.UseEnglish)} · 금화 부족 · 최소 {budget}G 필요 / 현재 {state.Gold}G");
                return;
            }

            WICastleProjectType projectType = ChooseGovernorProject(castleState);
            if (HasUsefulProjectWork(state, castleState, projectType) == false)
            {
                return;
            }
            WIProjectInvestment investment = budget >= database.ProjectBalance.IntensiveCost
                ? WIProjectInvestment.Intensive
                : WIProjectInvestment.Basic;
            state.Gold -= budget;
            summary.GoldSpent += budget;
            int expectedGain = GetExpectedProjectGain(database, projectType, governor, investment) +
                               GetFactionPolicyBonus(database, state.GetPlayerFactionState().Policy, projectType) +
                               GetResearchEffectBonus(database, state.GetPlayerFactionState(), WIResearchEffectType.ProjectGain) +
                               GetTitleProjectBonus(database, state, governor) + GetLegacyBonus(castleState, projectType) +
                               GetCastleSpecialtyProjectBonus(database.GetCastle(castleState.CastleId), projectType) +
                               GetFacilityProjectBonus(castleState, projectType);
            string reason = GetGovernorReason(database, castleState, projectType, governor);
            castleState.ActiveProject = new WICastleProjectState
            {
                ProjectType = projectType,
                Investment = investment,
                ManagerHeroId = governor.Id,
                RemainingMonths = 1,
                Delegated = true,
                ExpectedGain = expectedGain,
                GoldCost = budget,
                PlanReason = reason
            };

            string castleName = database.GetCastle(castleState.CastleId).DisplayName.Get(database.UseEnglish);
            summary.DelegationReports.Add($"[위임 계획] {castleName} · {reason} · 예상 +{expectedGain} · 비용 {budget}G · 기간 1개월");
        }

        // 영지관 운영 방침과 성 상태에 따라 이번 달 사업을 선택합니다.
        public static WICastleProjectType ChooseGovernorProject(WICastleRuntimeState castleState)
        {
            switch (castleState.GovernorPolicy)
            {
                case WIGovernorPolicy.Prosperity:
                    return castleState.Prosperity < 100 ? WICastleProjectType.Prosperity : ChooseLowestProject(castleState);
                case WIGovernorPolicy.Research:
                    return castleState.Technology < 100 ? WICastleProjectType.Technology : ChooseLowestProject(castleState);
                case WIGovernorPolicy.Frontline:
                    if (castleState.Stability < 50)
                    {
                        return WICastleProjectType.Stability;
                    }
                    if (castleState.Defense < 50)
                    {
                        return WICastleProjectType.Fortification;
                    }
                    return WICastleProjectType.Training;
                case WIGovernorPolicy.Talent:
                    return WICastleProjectType.Recruitment;
                default:
                    int minimum = Mathf.Min(castleState.Prosperity, castleState.Technology, castleState.Stability, castleState.Defense);
                    if (minimum == castleState.Prosperity)
                    {
                        return WICastleProjectType.Prosperity;
                    }
                    if (minimum == castleState.Technology)
                    {
                        return WICastleProjectType.Technology;
                    }
                    if (minimum == castleState.Stability)
                    {
                        return WICastleProjectType.Stability;
                    }
                    return WICastleProjectType.Fortification;
            }
        }

        // 우선 사업이 완료되면 아직 부족한 성 수치로 투자 대상을 전환합니다.
        private static WICastleProjectType ChooseLowestProject(WICastleRuntimeState castle)
        {
            int minimum = Mathf.Min(castle.Prosperity, castle.Technology, castle.Stability, castle.Defense);
            if (minimum == castle.Stability)
            {
                return WICastleProjectType.Stability;
            }
            if (minimum == castle.Defense)
            {
                return WICastleProjectType.Fortification;
            }
            if (minimum == castle.Technology)
            {
                return WICastleProjectType.Technology;
            }
            return WICastleProjectType.Prosperity;
        }

        // 월간 보고에 표시할 영지관의 사업 선택 이유를 만듭니다.
        public static string GetGovernorReason(
            WIAdministrationDatabaseSO database,
            WICastleRuntimeState castleState,
            WICastleProjectType projectType,
            WIHeroDefinition governor)
        {
            string reason;
            switch (castleState.GovernorPolicy)
            {
                case WIGovernorPolicy.Prosperity:
                    reason = "번영 방침으로 금화 수입과 보급 기반 우선";
                    break;
                case WIGovernorPolicy.Research:
                    reason = "연구 방침으로 기술과 마나 기반 우선";
                    break;
                case WIGovernorPolicy.Frontline:
                    if (projectType == WICastleProjectType.Training)
                    {
                        reason = "전선 방침 · 기본 방어선 확보 후 전투단 훈련";
                    }
                    else
                    {
                        reason = projectType == WICastleProjectType.Stability
                            ? $"전선 방침 · 질서 {castleState.Stability}이 50 미만"
                            : $"전선 방침 · 방어 {castleState.Defense}이 50 미만";
                    }
                    break;
                case WIGovernorPolicy.Talent:
                    reason = "인재 방침으로 탐색과 영입 기반 우선";
                    break;
                default:
                    reason = $"균형 방침 · 네 핵심 수치 중 {projectType} 대응 수치가 가장 낮음";
                    break;
            }
            int traitBonus = GetProjectTraitBonus(database, projectType, governor);
            if (traitBonus > 0)
            {
                WITraitDefinition trait = governor.Traits.Select(database.GetTrait)
                    .FirstOrDefault(item => item != null && item.ProjectTypes.Contains(projectType));
                reason += $" · {trait.DisplayName.Get(database.UseEnglish)} 특기 +{traitBonus}";
            }
            return reason;
        }

        // 현재 위임 설정으로 다음 달 선택할 사업·이유·예상 비용과 성과를 반환합니다.
        public static string GetDelegationPreview(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WICastleRuntimeState castleState)
        {
            WIHeroDefinition governor = database.GetHero(castleState.GovernorHeroId);
            if (governor == null)
            {
                return "영지관을 임명해야 위임 계획을 계산할 수 있습니다.";
            }
            WICastleProjectType projectType = ChooseGovernorProject(castleState);
            if (HasUsefulProjectWork(state, castleState, projectType) == false)
            {
                return database.GetText("UI_ADMIN_MAINTENANCE");
            }
            WIProjectInvestment investment = castleState.GovernorMonthlyBudget >= database.ProjectBalance.IntensiveCost
                ? WIProjectInvestment.Intensive : WIProjectInvestment.Basic;
            int cost = investment == WIProjectInvestment.Intensive
                ? database.ProjectBalance.IntensiveCost : database.ProjectBalance.BasicCost;
            int gain = GetExpectedProjectGain(database, projectType, governor, investment) +
                       GetFactionPolicyBonus(database, state.GetPlayerFactionState().Policy, projectType) +
                       GetResearchEffectBonus(database, state.GetPlayerFactionState(), WIResearchEffectType.ProjectGain) +
                       GetTitleProjectBonus(database, state, governor) + GetLegacyBonus(castleState, projectType) +
                       GetCastleSpecialtyProjectBonus(database.GetCastle(castleState.CastleId), projectType) +
                       GetFacilityProjectBonus(castleState, projectType);
            return $"예상 계획 · {projectType} · {GetGovernorReason(database, castleState, projectType, governor)} · 성과 +{gain} · 비용 {cost}G · 1개월";
        }

        // 진영 방침과 일치하는 사업에 적용할 소규모 성과 보너스를 반환합니다.
        public static int GetFactionPolicyBonus(WIAdministrationDatabaseSO database, WIFactionPolicy policy, WICastleProjectType projectType)
        {
            bool matched =
                (policy == WIFactionPolicy.Prosperity && projectType == WICastleProjectType.Prosperity) ||
                (policy == WIFactionPolicy.Development && projectType == WICastleProjectType.Technology) ||
                (policy == WIFactionPolicy.Stability && projectType == WICastleProjectType.Stability) ||
                (policy == WIFactionPolicy.Defense && (projectType == WICastleProjectType.Fortification || projectType == WICastleProjectType.Recovery)) ||
                (policy == WIFactionPolicy.Expedition && projectType == WICastleProjectType.Training) ||
                (policy == WIFactionPolicy.Talent && projectType == WICastleProjectType.Recruitment);
            return matched ? database.ProjectBalance.PolicyGainBonus : 0;
        }

        // 사업 종류에 알맞은 담당 인물 능력치를 반환합니다.
        public static int GetProjectRelevantStat(WICastleProjectType projectType, WIHeroDefinition manager)
        {
            if (manager == null)
            {
                return 0;
            }

            switch (projectType)
            {
                case WICastleProjectType.Prosperity:
                case WICastleProjectType.Fortification:
                case WICastleProjectType.Expansion:
                    return manager.Politics;
                case WICastleProjectType.Technology:
                case WICastleProjectType.Recruitment:
                    return manager.Intelligence;
                case WICastleProjectType.Stability:
                    return Mathf.Max(manager.Might, manager.Charisma);
                case WICastleProjectType.Training:
                    return manager.Leadership;
                case WICastleProjectType.Recovery:
                    return manager.Charisma;
                default:
                    return 0;
            }
        }

        // 사업 결과를 해당 성 수치에 적용합니다.
        private static void ApplyProjectGain(
            WICastleRuntimeState castleState,
            WICastleProjectType projectType,
            int gain)
        {
            switch (projectType)
            {
                case WICastleProjectType.Prosperity:
                    castleState.Prosperity = Mathf.Clamp(castleState.Prosperity + gain, 0, 100);
                    break;
                case WICastleProjectType.Technology:
                    castleState.Technology = Mathf.Clamp(castleState.Technology + gain, 0, 100);
                    break;
                case WICastleProjectType.Stability:
                    castleState.Stability = Mathf.Clamp(castleState.Stability + gain, 0, 100);
                    break;
                case WICastleProjectType.Fortification:
                    castleState.Defense = Mathf.Clamp(castleState.Defense + gain, 0, 100);
                    break;
            }
        }

        // 훈련 사업 성과를 주둔 전투단 숙련과 소속 인물 경험으로 분배합니다.
        private static void ApplyTrainingProjectGain(
            WIAdministrationState state,
            WICastleRuntimeState castleState,
            int gain)
        {
            int cohesionGain = Mathf.Max(1, gain * 2);
            int experienceGain = Mathf.Max(1, gain);
            foreach (WIArmyState army in state.Armies.Where(army =>
                         army.FactionId == castleState.FactionId &&
                         army.CurrentCastleId == castleState.CastleId &&
                         army.IsMoving == false && army.AwaitingBattle == false))
            {
                army.CohesionExperience += cohesionGain;
                foreach (WIArmyMemberState member in army.Members)
                {
                    WICharacterRuntimeState character = state.GetCharacter(member.HeroId);
                    if (character != null)
                    {
                        character.Experience += experienceGain;
                    }
                }
                UpdateArmyProficiency(army);
            }
        }

        // 사업 종류, 담당 인물과 투자 등급으로 예상 성과를 계산합니다.
        public static int GetExpectedProjectGain(
            WIAdministrationDatabaseSO database,
            WICastleProjectType projectType,
            WIHeroDefinition manager,
            WIProjectInvestment investment)
        {
            int relevantStat = GetProjectRelevantStat(projectType, manager);
            WIProjectBalanceDefinition balance = database.ProjectBalance;
            int investmentBonus = investment == WIProjectInvestment.Intensive ? balance.IntensiveGainBonus : 0;
            int traitBonus = GetProjectTraitBonus(database, projectType, manager);
            return Mathf.Clamp(balance.BaseGain + relevantStat / Mathf.Max(1, balance.StatDivisor) + investmentBonus + traitBonus,
                balance.MinimumGain, balance.MaximumGain);
        }

        // 사업 종류와 투자 등급에 맞는 금화 비용을 반환합니다.
        public static int GetProjectCost(WIAdministrationDatabaseSO database, WICastleProjectType projectType, WIProjectInvestment investment)
        {
            WIProjectBalanceDefinition balance = database.ProjectBalance;
            int basicCost = projectType == WICastleProjectType.Expansion
                ? balance.ExpansionBasicCost
                : balance.BasicCost;
            return investment == WIProjectInvestment.Intensive
                ? Mathf.RoundToInt(basicCost * (balance.IntensiveCost / (float)Mathf.Max(1, balance.BasicCost)))
                : basicCost;
        }

        // 담당 인물의 특기가 사업과 일치할 때 적용할 성과 보너스를 반환합니다.
        public static int GetProjectTraitBonus(WIAdministrationDatabaseSO database, WICastleProjectType projectType, WIHeroDefinition manager)
        {
            if (manager == null)
            {
                return 0;
            }

            bool matched = manager.Traits.Any(trait =>
            {
                WITraitDefinition definition = database.GetTrait(trait);
                return definition != null && definition.ProjectTypes.Contains(projectType);
            });
            return matched ? database.ProjectBalance.TraitGainBonus : 0;
        }

        // 성 전문 분야가 지정 사업에 제공하는 성과 보너스를 반환합니다.
        public static int GetCastleSpecialtyProjectBonus(WICastleDefinition castle, WICastleProjectType projectType)
        {
            return castle != null && castle.SpecialtyEffectType == WICastleSpecialtyEffectType.ProjectGain &&
                   castle.SpecialtyProjectType == projectType
                ? castle.SpecialtyEffectValue
                : 0;
        }
    }
}

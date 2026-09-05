using System;
using System.Linq;

namespace ProjectWI.Administration
{
    public partial class WIAdministrationUIController
    {
        // 선택 성에서 지정할 수 있는 중점 사업과 투자 비용을 UGUI에 제공합니다.
        public bool TryGetUGUIFocusProjectSnapshot(out WIAdministrationFocusProjectSnapshot snapshot, out string error)
        {
            snapshot = null;
            error = string.Empty;
            if (WIAdministrationTurnSystem.CanPlayerManageCastle(state, selectedCastle) == false)
            {
                error = "다른 진영의 성에는 영지 관리 명령을 내릴 수 없습니다.";
                return false;
            }
            if (selectedCastle.ActiveProject != null)
            {
                error = "이번 달 중점 사업이 이미 지정되어 있습니다.";
                return false;
            }
            if (selectedCastle.DelegatedToGovernor)
            {
                error = "영지관 위임을 해제한 뒤 직접 중점 사업을 지정할 수 있습니다.";
                return false;
            }

            WICastleDefinition castle = database.GetCastle(selectedCastle.CastleId);
            snapshot = new WIAdministrationFocusProjectSnapshot
            {
                CastleName = castle.DisplayName.Get(database.UseEnglish)
            };
            foreach (WICastleProjectType projectType in Enum.GetValues(typeof(WICastleProjectType)))
            {
                snapshot.Projects.Add(new WIAdministrationProjectOptionSnapshot
                {
                    ProjectType = projectType,
                    DisplayName = GetProjectDisplayName(projectType) + GetProjectCombatHint(projectType),
                    BasicCost = WIAdministrationTurnSystem.GetProjectCost(database, projectType, WIProjectInvestment.Basic),
                    IntensiveCost = WIAdministrationTurnSystem.GetProjectCost(database, projectType, WIProjectInvestment.Intensive)
                });
            }
            return true;
        }

        // 선택 사업을 담당할 수 있는 대기 영웅과 기존 계산 시스템의 예상 성과를 제공합니다.
        public bool TryGetUGUIProjectManagers(WICastleProjectType projectType, WIProjectInvestment investment,
            out System.Collections.Generic.List<WIAdministrationProjectManagerSnapshot> managers, out string error)
        {
            managers = new System.Collections.Generic.List<WIAdministrationProjectManagerSnapshot>();
            error = string.Empty;
            foreach (string heroId in selectedCastle.HeroIds)
            {
                WIHeroDefinition hero = database.GetHero(heroId);
                if (hero == null || state.IsCharacterBusy(hero.Id) ||
                    WIAdministrationTurnSystem.IsAdministrationCapable(state, heroId) == false)
                {
                    continue;
                }

                int expectedGain = WIAdministrationTurnSystem.GetExpectedProjectGain(database, projectType, hero, investment) +
                    WIAdministrationTurnSystem.GetCastleSpecialtyProjectBonus(
                        database.GetCastle(selectedCastle.CastleId), projectType) +
                    WIAdministrationTurnSystem.GetFactionPolicyBonus(database, state.FactionPolicy, projectType);
                int traitBonus = WIAdministrationTurnSystem.GetProjectTraitBonus(database, projectType, hero);
                managers.Add(new WIAdministrationProjectManagerSnapshot
                {
                    HeroId = hero.Id,
                    DisplayName = hero.DisplayName.Get(database.UseEnglish),
                    Portrait = hero.Portrait,
                    ExpectedGain = expectedGain,
                    TraitText = traitBonus > 0 ? $"특기 적용 +{traitBonus}" : "특기 미적용"
                });
            }

            if (managers.Count > 0)
            {
                return true;
            }
            error = "이 성에 사업을 맡길 대기 인물이 없습니다.";
            return false;
        }

        // UGUI에서 선택한 담당자와 투자 단계로 기존 중점 사업 배정 로직을 실행합니다.
        public bool AssignUGUIFocusProject(WICastleProjectType projectType, WIProjectInvestment investment,
            string heroId, out string error)
        {
            error = string.Empty;
            WIHeroDefinition hero = database.GetHero(heroId);
            if (hero == null || selectedCastle.HeroIds.Contains(heroId) == false || state.IsCharacterBusy(heroId) ||
                WIAdministrationTurnSystem.IsAdministrationCapable(state, heroId) == false)
            {
                error = "선택한 인물을 현재 사업 담당자로 배정할 수 없습니다.";
                return false;
            }
            if (projectType == WICastleProjectType.Expansion && CanStartExpansion(selectedCastle) == false)
            {
                error = "확장에는 성 규모에 맞는 번영과 기술이 필요하며 대형 성은 더 확장할 수 없습니다.";
                return false;
            }
            int cost = WIAdministrationTurnSystem.GetProjectCost(database, projectType, investment);
            if (state.Gold < cost)
            {
                error = database.GetText("UI_NOT_ENOUGH_RESOURCE");
                return false;
            }

            AssignCastleProject(projectType, investment, hero);
            return true;
        }

        // 현재 성의 슬롯 상태와 배치 가능한 미배치 영웅을 UGUI에 제공합니다.
        public bool TryGetUGUIHeroAssignmentSnapshot(out WIAdministrationHeroAssignmentSnapshot snapshot,
            out string error)
        {
            snapshot = null;
            error = string.Empty;
            if (WIAdministrationTurnSystem.CanPlayerManageCastle(state, selectedCastle) == false)
            {
                error = "다른 진영의 성에는 영웅을 배치할 수 없습니다.";
                return false;
            }
            if (selectedCastle.HeroIds.Count >= selectedCastle.GetHeroSlotCount())
            {
                error = "현재 성 규모의 주둔 인물 슬롯이 가득 찼습니다.";
                return false;
            }

            WICastleDefinition castle = database.GetCastle(selectedCastle.CastleId);
            snapshot = new WIAdministrationHeroAssignmentSnapshot
            {
                CastleName = castle.DisplayName.Get(database.UseEnglish),
                OccupiedSlots = selectedCastle.HeroIds.Count,
                MaximumSlots = selectedCastle.GetHeroSlotCount()
            };
            foreach (WICharacterRuntimeState character in state.Characters)
            {
                if (character.Recruited == false || state.IsCharacterBusy(character.HeroId))
                {
                    continue;
                }
                bool assigned = state.Castles.Exists(item => item.HeroIds.Contains(character.HeroId));
                WIHeroDefinition hero = database.GetHero(character.HeroId);
                if (assigned || hero == null)
                {
                    continue;
                }

                snapshot.Candidates.Add(new WIAdministrationHeroAssignmentCandidateSnapshot
                {
                    HeroId = hero.Id,
                    DisplayName = hero.DisplayName.Get(database.UseEnglish),
                    Portrait = hero.Portrait,
                    Summary = $"{hero.HeroClass} · {GetTraitDisplayText(hero)}"
                });
            }

            if (snapshot.Candidates.Count > 0)
            {
                return true;
            }
            error = "배치 가능한 대기 인물이 없습니다. 진행 중인 임무가 끝나거나 전투단을 해산한 뒤 다시 시도하십시오.";
            return false;
        }

        // 선택한 미배치 영웅을 현재 성에 배치하고 영지 화면을 갱신합니다.
        public bool AssignUGUIHeroToSelectedCastle(string heroId, out string error)
        {
            error = string.Empty;
            if (selectedCastle.HeroIds.Count >= selectedCastle.GetHeroSlotCount())
            {
                error = "현재 성의 주둔 인물 슬롯이 가득 찼습니다.";
                return false;
            }
            WICharacterRuntimeState character = state.GetCharacter(heroId);
            bool alreadyAssigned = state.Castles.Exists(item => item.HeroIds.Contains(heroId));
            if (character == null || character.Recruited == false || state.IsCharacterBusy(heroId) || alreadyAssigned)
            {
                error = "선택한 영웅을 현재 성에 배치할 수 없습니다.";
                return false;
            }

            selectedCastle.HeroIds.Add(heroId);
            SelectCastle(selectedCastle.CastleId);
            RefreshAll();
            return true;
        }

        // 현재 성에서 개인 활동을 수행할 수 있는 주둔 인물을 UGUI에 제공합니다.
        public bool TryGetUGUICharacterActivityActors(out WIAdministrationCharacterActivitySnapshot snapshot,
            out string error)
        {
            snapshot = null;
            error = string.Empty;
            if (WIAdministrationTurnSystem.CanPlayerManageCastle(state, selectedCastle) == false)
            {
                error = "다른 진영의 성에는 인재 활동을 지정할 수 없습니다.";
                return false;
            }
            WICastleDefinition castle = database.GetCastle(selectedCastle.CastleId);
            snapshot = new WIAdministrationCharacterActivitySnapshot
            {
                CastleName = castle.DisplayName.Get(database.UseEnglish)
            };
            foreach (string heroId in selectedCastle.HeroIds)
            {
                WICharacterRuntimeState character = state.GetCharacter(heroId);
                WIHeroDefinition hero = database.GetHero(heroId);
                if (character == null || hero == null || state.IsCharacterBusy(heroId))
                {
                    continue;
                }
                snapshot.Candidates.Add(new WIAdministrationCharacterActivityCandidateSnapshot
                {
                    HeroId = heroId,
                    DisplayName = hero.DisplayName.Get(database.UseEnglish),
                    Summary = $"{GetTraitNameText(hero)}\n피로 {character.Fatigue} · 명성 {character.Reputation}",
                    ClassName = database.GetHeroClass(hero.HeroClass)?.DisplayName.Get(database.UseEnglish) ??
                        hero.HeroClass.ToString(),
                    Grade = hero.Grade,
                    Portrait = hero.Portrait
                });
            }
            if (snapshot.Candidates.Count > 0) return true;
            error = "이 성에 활동 가능한 대기 인물이 없습니다.";
            return false;
        }

        // 교류 또는 영입 활동에 필요한 대상 목록과 선택 가능 상태를 UGUI에 제공합니다.
        public bool TryGetUGUICharacterActivityTargets(string actorHeroId, WICharacterActivityType activity,
            out WIAdministrationCharacterActivitySnapshot snapshot, out string error)
        {
            snapshot = new WIAdministrationCharacterActivitySnapshot();
            error = string.Empty;
            WICharacterRuntimeState actor = state.GetCharacter(actorHeroId);
            if (actor == null || selectedCastle.HeroIds.Contains(actorHeroId) == false || state.IsCharacterBusy(actorHeroId) ||
                WIAdministrationTurnSystem.CanPerformCharacterActivity(state, actorHeroId, activity,
                    database.Automation.TalentOfficeCapacity) == false)
            {
                error = "활동을 수행할 인물을 다시 선택하십시오.";
                return false;
            }
            if (activity == WICharacterActivityType.Socialize)
            {
                foreach (string targetId in selectedCastle.HeroIds)
                {
                    if (targetId == actorHeroId) continue;
                    WIHeroDefinition hero = database.GetHero(targetId);
                    if (hero == null) continue;
                    snapshot.Candidates.Add(new WIAdministrationCharacterActivityCandidateSnapshot
                    {
                        HeroId = targetId,
                        DisplayName = hero.DisplayName.Get(database.UseEnglish),
                        Summary = "같은 성 주둔 인물",
                        ClassName = database.GetHeroClass(hero.HeroClass)?.DisplayName.Get(database.UseEnglish) ??
                            hero.HeroClass.ToString(),
                        Grade = hero.Grade,
                        Portrait = hero.Portrait
                    });
                }
                if (snapshot.Candidates.Count > 0) return true;
                error = "교류할 다른 주둔 인물이 없습니다.";
                return false;
            }
            if (activity == WICharacterActivityType.Recruit)
            {
                foreach (WICharacterRuntimeState candidate in state.Characters)
                {
                    if (candidate.Discovered == false || candidate.Recruited) continue;
                    WIHeroDefinition hero = database.GetHero(candidate.HeroId);
                    if (hero == null) continue;
                    snapshot.Candidates.Add(new WIAdministrationCharacterActivityCandidateSnapshot
                    {
                        HeroId = candidate.HeroId,
                        DisplayName = hero.DisplayName.Get(database.UseEnglish),
                        Summary = $"설득 {candidate.RecruitmentProgress}% · 필요 명성 {hero.RequiredReputation}",
                        ClassName = database.GetHeroClass(hero.HeroClass)?.DisplayName.Get(database.UseEnglish) ??
                            hero.HeroClass.ToString(),
                        Grade = hero.Grade,
                        Portrait = hero.Portrait,
                        Interactable = actor.Reputation >= hero.RequiredReputation
                    });
                }
                if (snapshot.Candidates.Count > 0) return true;
                error = "먼저 탐색이나 인재 사업으로 인재를 발견해야 합니다.";
                return false;
            }
            error = "대상이 필요한 활동이 아닙니다.";
            return false;
        }

        // 기존 개인 활동 상태 필드를 사용해 UGUI에서 선택한 활동과 대상을 배정합니다.
        public bool AssignUGUICharacterActivity(string actorHeroId, WICharacterActivityType activity,
            string targetHeroId, out string error)
        {
            error = string.Empty;
            WICharacterRuntimeState actor = state.GetCharacter(actorHeroId);
            if (actor == null || selectedCastle.HeroIds.Contains(actorHeroId) == false ||
                state.IsCharacterBusy(actorHeroId) || (actor.InjuryMonths > 0 && activity != WICharacterActivityType.Rest) ||
                WIAdministrationTurnSystem.CanPerformCharacterActivity(state, actorHeroId, activity,
                    database.Automation.TalentOfficeCapacity) == false)
            {
                error = "선택한 인물은 현재 개인 활동을 수행할 수 없습니다.";
                return false;
            }
            if (activity == WICharacterActivityType.Socialize &&
                (targetHeroId == actorHeroId || selectedCastle.HeroIds.Contains(targetHeroId) == false))
            {
                error = "같은 성에 있는 다른 인물을 교류 대상으로 선택하십시오.";
                return false;
            }
            if (activity == WICharacterActivityType.Recruit)
            {
                WICharacterRuntimeState target = state.GetCharacter(targetHeroId);
                WIHeroDefinition targetHero = database.GetHero(targetHeroId);
                if (target == null || targetHero == null || target.Discovered == false || target.Recruited ||
                    actor.Reputation < targetHero.RequiredReputation)
                {
                    error = "현재 조건으로 해당 인재를 영입 대상으로 지정할 수 없습니다.";
                    return false;
                }
            }
            actor.Activity = activity;
            actor.ActivityTargetHeroId = targetHeroId;
            actor.StandingActivity = actor.RepeatActivity ? activity : WICharacterActivityType.None;
            actor.StandingActivityTargetHeroId = targetHeroId;
            actor.AutomaticRecovery = false;
            SelectCastle(selectedCastle.CastleId);
            RefreshAll();
            return true;
        }

    }
}

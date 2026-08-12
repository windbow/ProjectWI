using System;
using System.Linq;
using ProjectWI.Systems;

namespace ProjectWI.Administration
{
    public partial class WIAdministrationUIController
    {
        public event Action UGUIWorldChanged;
        public event Action UGUIFocusProjectRequested;
        public event Action UGUIHeroAssignmentRequested;
        public event Action UGUICharacterActivityRequested;
        public event Action UGUISpecialFacilityRequested;
        public event Action UGUIBasicFacilityRequested;
        public event Action UGUIDelegationRequested;
        public event Action UGUIMarchRequested;
        public event Action UGUICastleRecordRequested;
        public event Action UGUIObjectiveRequested;
        public event Action UGUIMonthlyReportRequested;
        public event Action UGUIMilitaryRequested;
        public event Action UGUIHeroesRequested;
        public event Action UGUIDiplomacyRequested;
        public event Action UGUISchemeRequested;
        public event Action UGUIResearchRequested;
        public event Action UGUIFactionRequested;
        public event Action UGUICouncilRequested;
        public event Action UGUISystemRequested;
        public event Action<WIAdministrationReportActionType, string> UGUIEventChoiceRequested;
        private bool uguiWorldVisible = true;
        private bool uguiSuspendedForLegacyModal;

        // 이행 중인 UGUI 월드 화면에 현재 게임 상태를 읽기 전용 스냅샷으로 전달합니다.
        public bool TryGetUGUIWorldSnapshot(out WIAdministrationWorldSnapshot snapshot)
        {
            snapshot = null;
            if (state == null || database == null)
            {
                return false;
            }

            WIFactionDefinition playerFaction = database.Factions.FirstOrDefault(faction => faction.PlayerFaction == true);
            WITurnSummary forecast = WIAdministrationTurnSystem.GetFactionMonthlyIncome(database, state, state.PlayerFactionId);
            WICampaignObjectiveDefinition objective = WICampaignObjectiveSystem.GetCurrent(database, state);
            WICastleRuntimeState castle = GetGlobalSummaryCastle();
            int unresolvedBattleCount = state.BattleSessions.Count(session =>
                session.PlayerInvolved && session.Status != WIBattleSessionStatus.Resolved);

            int objectiveProgress = objective == null ? 0 : WICampaignObjectiveSystem.GetProgress(state, objective);
            int objectiveTarget = objective == null ? 0 : objective.TargetValue;
            string latestNews = state.LastMonthlyReport?.News?.LastOrDefault();
            WICastleDefinition castleDefinition = castle == null ? null : database.GetCastle(castle.CastleId);
            WIFactionDefinition castleOwner = castle == null ? null : database.GetFaction(castle.FactionId);
            int armyCount = castle == null ? 0 : state.Armies.Count(army => army.CurrentCastleId == castle.CastleId);

            snapshot = new WIAdministrationWorldSnapshot
            {
                CampaignStarted = WICampaignRuntimeService.Instance != null && WICampaignRuntimeService.Instance.HasCampaignStarted,
                WorldVisible = uguiWorldVisible && uguiSuspendedForLegacyModal == false,
                FactionName = playerFaction == null ? database.GetText("UI_GAME_TITLE") : playerFaction.DisplayName.Get(database.UseEnglish),
                Date = $"{state.Year}년 {state.Month:00}월 · 제 {state.Turn}턴",
                TurnDescription = $"진영 방침: {GetFactionPolicyDisplayName(state.FactionPolicy)} · {state.Month:00}월 명령을 검토하십시오.",
                Gold = $"금화  {FormatHudNumber(state.Gold, database.UseEnglish)}  (+{FormatHudNumber(forecast.GoldGained, database.UseEnglish)})",
                Mana = $"마나  {FormatHudNumber(state.ManaCrystal, database.UseEnglish)}  (+{FormatHudNumber(forecast.ManaGained, database.UseEnglish)})",
                Influence = $"영향력  {FormatHudNumber(state.Influence, database.UseEnglish)}  (+{FormatHudNumber(forecast.InfluenceGained, database.UseEnglish)})",
                CastleName = castleDefinition?.DisplayName.Get(database.UseEnglish) ?? castle?.CastleId ?? string.Empty,
                CastleOwner = castle == null ? string.Empty : $"{castle.CastleSize} · {castleOwner?.DisplayName.Get(database.UseEnglish) ?? castle.FactionId}",
                CastleStats = castle == null ? string.Empty : $"번영 {castle.Prosperity}  ·  기술 {castle.Technology}\n질서 {castle.Stability}  ·  방어 {castle.Defense}",
                CastleHeroes = castle == null ? string.Empty : $"주둔 영웅 {castle.HeroIds.Count}명  ·  주둔 전투단 {armyCount}개",
                MapImage = database.GlobalMapImage,
                CastleImage = castleDefinition?.CastleImage,
                ObjectiveTitle = objective == null ? "현재 목표 없음" : objective.Title.Get(database.UseEnglish),
                ObjectiveProgress = objective == null
                    ? "현재 진행 중인 목표가 없습니다."
                    : $"진행 {objectiveProgress}/{objectiveTarget}\n{objective.Description.Get(database.UseEnglish)}",
                ObjectiveProgressNormalized = objectiveTarget <= 0 ? 0f : UnityEngine.Mathf.Clamp01((float)objectiveProgress / objectiveTarget),
                BattleAlert = unresolvedBattleCount > 0 ? $"전투 발생 {unresolvedBattleCount}건 · 확인" : "현재 전투 없음",
                HasBattleAlert = unresolvedBattleCount > 0 || state.LastMonthlyReport != null,
                MonthlyNews = string.IsNullOrEmpty(latestNews) ? "새로운 월간 보고가 없습니다." : $"최근 소식\n{latestNews}",
                CanEndTurn = unresolvedBattleCount == 0,
                EndTurnText = unresolvedBattleCount == 0 ? "다음 턴  T" : $"전투 해결 필요 · {unresolvedBattleCount}건"
            };
            foreach (WICastleDefinition definition in database.Castles)
            {
                WICastleRuntimeState castleState = state.GetCastle(definition.Id);
                WIFactionDefinition owner = castleState == null ? null : database.GetFaction(castleState.FactionId);
                snapshot.MapNodes.Add(new WIAdministrationMapNodeSnapshot
                {
                    CastleId = definition.Id,
                    DisplayName = definition.DisplayName.Get(database.UseEnglish),
                    FactionId = castleState?.FactionId ?? definition.FactionId,
                    Selected = selectedCastle != null && selectedCastle.CastleId == definition.Id,
                    Tooltip = $"{owner?.DisplayName.Get(database.UseEnglish) ?? castleState?.FactionId}\n{definition.DisplayName.Get(database.UseEnglish)}"
                });
            }
            return true;
        }

        // 선택한 성의 영지 상세 정보를 신규 UGUI가 사용할 읽기 전용 스냅샷으로 변환합니다.
        public bool TryGetUGUITerritorySnapshot(out WIAdministrationTerritorySnapshot snapshot)
        {
            snapshot = null;
            if (state == null || database == null || selectedCastle == null)
            {
                return false;
            }

            WICastleDefinition definition = database.GetCastle(selectedCastle.CastleId);
            WIFactionDefinition owner = database.GetFaction(selectedCastle.FactionId);
            bool manageable = WIAdministrationTurnSystem.CanPlayerManageCastle(state, selectedCastle);
            bool detailed = WIInformationVisibility.CanViewCastleDetails(state, state.PlayerFactionId, selectedCastle);
            WIHeroDefinition governor = detailed ? database.GetHero(selectedCastle.GovernorHeroId) : null;
            string ownerName = owner == null ? selectedCastle.FactionId : owner.DisplayName.Get(database.UseEnglish);

            snapshot = new WIAdministrationTerritorySnapshot
            {
                Visible = uguiWorldVisible == false && uguiSuspendedForLegacyModal == false,
                Manageable = manageable,
                CanChooseSpecialFacility = manageable && selectedCastle.PendingSpecialFacilityChoice,
                CastleTitle = definition?.DisplayName.Get(database.UseEnglish) ?? selectedCastle.CastleId,
                CastleInfo = detailed
                    ? $"{ownerName} · {selectedCastle.CastleSize} · {definition?.TerrainTrait.Get(database.UseEnglish)} · 인물 {selectedCastle.HeroIds.Count}/{selectedCastle.GetHeroSlotCount()}"
                    : $"{ownerName} 소유 · 상세 정보 미확보",
                CastleImage = definition?.CastleImage,
                GovernorPortrait = governor?.Portrait,
                Prosperity = detailed ? $"번영 {selectedCastle.Prosperity} · {GetCastleStatusName(selectedCastle.Prosperity)}" : "번영 ??",
                Technology = detailed ? $"기술 {selectedCastle.Technology} · {GetCastleStatusName(selectedCastle.Technology)}" : "기술 ??",
                Stability = detailed ? $"질서 {selectedCastle.Stability} · {GetCastleStatusName(selectedCastle.Stability)}" : "질서 ??",
                Defense = detailed ? $"방어 {selectedCastle.Defense} · {GetCastleStatusName(selectedCastle.Defense)}" : "방어 ??",
                ProjectStatus = detailed
                    ? selectedCastle.DelegatedToGovernor && selectedCastle.ActiveProject == null
                        ? $"영지관 위임 · {GetGovernorPolicyDisplayName(selectedCastle.GovernorPolicy)} · 월 {selectedCastle.GovernorMonthlyBudget}G"
                        : GetProjectStatusText(selectedCastle.ActiveProject)
                    : "첩보 조사를 성공하면 상세 정보가 공개됩니다."
            };

            for (int index = 0; index < 8; index += 1)
            {
                bool visible = detailed && index < selectedCastle.GetHeroSlotCount();
                bool occupied = visible && index < selectedCastle.HeroIds.Count;
                WIHeroDefinition hero = occupied ? database.GetHero(selectedCastle.HeroIds[index]) : null;
                WICharacterRuntimeState character = occupied ? state.GetCharacter(selectedCastle.HeroIds[index]) : null;
                string activity = character == null || character.Activity == WICharacterActivityType.None
                    ? "대기"
                    : character.Activity.ToString();
                snapshot.HeroSlots.Add(new WIAdministrationSlotSnapshot
                {
                    Visible = visible,
                    Occupied = occupied,
                    Caption = occupied
                        ? hero == null ? selectedCastle.HeroIds[index] : $"{hero.DisplayName.Get(database.UseEnglish)}\n{activity}"
                        : database.GetText("UI_EMPTY_HERO"),
                    Image = hero?.Portrait
                });
            }

            for (int index = 0; index < 2; index += 1)
            {
                bool visible = detailed && index < selectedCastle.GetSpecialFacilitySlotCount();
                bool occupied = visible && index < selectedCastle.SpecialFacilityIds.Count;
                WISpecialFacilityDefinition facility = occupied
                    ? database.GetSpecialFacility(selectedCastle.SpecialFacilityIds[index])
                    : null;
                snapshot.FacilitySlots.Add(new WIAdministrationSlotSnapshot
                {
                    Visible = visible,
                    Occupied = occupied,
                    Caption = occupied
                        ? facility == null ? selectedCastle.SpecialFacilityIds[index] : facility.DisplayName.Get(database.UseEnglish)
                        : "확장 완료 시 선택",
                    Image = facility?.Icon
                });
            }
            return true;
        }

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
                    DisplayName = GetProjectDisplayName(projectType),
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
                if (hero == null || state.IsCharacterBusy(hero.Id))
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
            if (hero == null || selectedCastle.HeroIds.Contains(heroId) == false || state.IsCharacterBusy(heroId))
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
                if (character == null || hero == null || state.IsCharacterBusy(heroId) || character.InjuryMonths > 0)
                {
                    continue;
                }
                snapshot.Candidates.Add(new WIAdministrationCharacterActivityCandidateSnapshot
                {
                    HeroId = heroId,
                    DisplayName = hero.DisplayName.Get(database.UseEnglish),
                    Summary = $"피로 {character.Fatigue} · 명성 {character.Reputation}",
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
            if (actor == null || selectedCastle.HeroIds.Contains(actorHeroId) == false || state.IsCharacterBusy(actorHeroId))
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
                state.IsCharacterBusy(actorHeroId) || actor.InjuryMonths > 0)
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
            SelectCastle(selectedCastle.CastleId);
            RefreshAll();
            return true;
        }

        // 확장 완료 후 현재 성에서 선택할 수 있는 미보유 특화 시설을 UGUI에 제공합니다.
        public bool TryGetUGUISpecialFacilitySnapshot(out WIAdministrationSpecialFacilitySnapshot snapshot,
            out string error)
        {
            snapshot = null;
            error = string.Empty;
            if (WIAdministrationTurnSystem.CanPlayerManageCastle(state, selectedCastle) == false)
            {
                error = "다른 진영의 성에는 특화 시설을 선택할 수 없습니다.";
                return false;
            }
            if (selectedCastle.PendingSpecialFacilityChoice == false)
            {
                error = "특화 시설은 성 확장 사업을 완료할 때 선택할 수 있습니다.";
                return false;
            }
            if (selectedCastle.SpecialFacilityIds.Count >= selectedCastle.GetSpecialFacilitySlotCount())
            {
                error = "현재 성의 특화 시설 슬롯이 가득 찼습니다.";
                return false;
            }
            WICastleDefinition castle = database.GetCastle(selectedCastle.CastleId);
            snapshot = new WIAdministrationSpecialFacilitySnapshot
            {
                CastleName = castle.DisplayName.Get(database.UseEnglish),
                OccupiedSlots = selectedCastle.SpecialFacilityIds.Count,
                MaximumSlots = selectedCastle.GetSpecialFacilitySlotCount()
            };
            foreach (WISpecialFacilityDefinition facility in database.SpecialFacilities)
            {
                if (selectedCastle.SpecialFacilityIds.Contains(facility.Id)) continue;
                snapshot.Options.Add(new WIAdministrationSpecialFacilityOptionSnapshot
                {
                    FacilityId = facility.Id,
                    DisplayName = facility.DisplayName.Get(database.UseEnglish),
                    Description = facility.Description.Get(database.UseEnglish),
                    Icon = facility.Icon
                });
            }
            if (snapshot.Options.Count > 0) return true;
            error = "선택 가능한 특화 시설이 없습니다.";
            return false;
        }

        // 선택한 특화 시설을 현재 성의 빈 슬롯에 배치합니다.
        public bool SelectUGUISpecialFacility(string facilityId, out string error)
        {
            error = string.Empty;
            WISpecialFacilityDefinition facility = database.GetSpecialFacility(facilityId);
            if (WIAdministrationTurnSystem.CanPlayerManageCastle(state, selectedCastle) == false ||
                selectedCastle.PendingSpecialFacilityChoice == false || facility == null ||
                selectedCastle.SpecialFacilityIds.Contains(facilityId) ||
                selectedCastle.SpecialFacilityIds.Count >= selectedCastle.GetSpecialFacilitySlotCount())
            {
                error = "현재 이 특화 시설을 선택할 수 없습니다.";
                return false;
            }
            selectedCastle.SpecialFacilityIds.Add(facilityId);
            selectedCastle.PendingSpecialFacilityChoice = false;
            SelectCastle(selectedCastle.CastleId);
            RefreshAll();
            return true;
        }

        // 현재 성 선술집에 게시된 월간 의뢰를 UGUI에 제공합니다.
        public bool TryGetUGUITavernQuests(out WIAdministrationBasicFacilitySnapshot snapshot, out string error)
        {
            snapshot = null;
            error = string.Empty;
            if (WIAdministrationTurnSystem.CanPlayerManageCastle(state, selectedCastle) == false)
            {
                error = "다른 진영의 성에서는 선술집 의뢰를 확인할 수 없습니다.";
                return false;
            }
            WICastleDefinition castle = database.GetCastle(selectedCastle.CastleId);
            snapshot = new WIAdministrationBasicFacilitySnapshot
            {
                CastleName = castle.DisplayName.Get(database.UseEnglish)
            };
            foreach (WITavernQuestState quest in selectedCastle.TavernQuests)
            {
                WITavernQuestDefinition definition = database.GetTavernQuest(quest.QuestType);
                if (definition == null) continue;
                snapshot.Quests.Add(new WIAdministrationTavernQuestSnapshot
                {
                    QuestId = quest.QuestId,
                    DisplayName = definition.DisplayName.Get(database.UseEnglish),
                    Summary = quest.Status == WIQuestStatus.Accepted
                        ? $"진행 중 · {quest.RemainingMonths}개월"
                        : $"{definition.DurationMonths}개월 · 금화 {definition.GoldReward} · 공훈 {definition.MeritReward} · 명성 {definition.ReputationReward}",
                    Description = definition.Description.Get(database.UseEnglish),
                    Available = quest.Status == WIQuestStatus.Available
                });
            }
            if (snapshot.Quests.Count > 0) return true;
            error = "다음 달부터 새로운 의뢰가 게시됩니다.";
            return false;
        }

        // 선택한 의뢰를 맡을 수 있는 대기 주둔 인물과 적성 수치를 UGUI에 제공합니다.
        public bool TryGetUGUIQuestHeroes(string questId, out WIAdministrationBasicFacilitySnapshot snapshot,
            out string error)
        {
            snapshot = new WIAdministrationBasicFacilitySnapshot();
            error = string.Empty;
            WITavernQuestState quest = selectedCastle.TavernQuests.Find(item => item.QuestId == questId);
            WITavernQuestDefinition definition = quest == null ? null : database.GetTavernQuest(quest.QuestType);
            if (quest == null || definition == null || quest.Status != WIQuestStatus.Available)
            {
                error = "현재 수락할 수 없는 의뢰입니다.";
                return false;
            }
            WICastleDefinition castle = database.GetCastle(selectedCastle.CastleId);
            snapshot.CastleName = castle.DisplayName.Get(database.UseEnglish);
            foreach (string heroId in selectedCastle.HeroIds)
            {
                if (state.IsCharacterBusy(heroId)) continue;
                WIHeroDefinition hero = database.GetHero(heroId);
                if (hero == null) continue;
                int aptitude = WIAdministrationTurnSystem.GetQuestAptitude(hero, definition.Aptitude);
                snapshot.Heroes.Add(new WIAdministrationQuestHeroSnapshot
                {
                    HeroId = heroId,
                    DisplayName = hero.DisplayName.Get(database.UseEnglish),
                    Portrait = hero.Portrait,
                    Summary = $"{definition.Aptitude} {aptitude}/{definition.AptitudeThreshold}" +
                        (aptitude >= definition.AptitudeThreshold ? $" · 적합 · 금화 +{definition.AptitudeBonusGold}" : string.Empty)
                });
            }
            if (snapshot.Heroes.Count > 0) return true;
            error = "이 성에 의뢰를 맡길 대기 인물이 없습니다.";
            return false;
        }

        // 기존 선술집 의뢰 상태에 선택한 담당 인물을 배정합니다.
        public bool AssignUGUITavernQuest(string questId, string heroId, out string error)
        {
            error = string.Empty;
            WITavernQuestState quest = selectedCastle.TavernQuests.Find(item => item.QuestId == questId);
            WITavernQuestDefinition definition = quest == null ? null : database.GetTavernQuest(quest.QuestType);
            if (quest == null || definition == null || quest.Status != WIQuestStatus.Available ||
                selectedCastle.HeroIds.Contains(heroId) == false || state.IsCharacterBusy(heroId))
            {
                error = "현재 선택한 인물에게 이 의뢰를 배정할 수 없습니다.";
                return false;
            }
            quest.AssignedHeroId = heroId;
            quest.Status = WIQuestStatus.Accepted;
            quest.RemainingMonths = definition.DurationMonths;
            SelectCastle(selectedCastle.CastleId);
            RefreshAll();
            return true;
        }

        // 현재 성의 영지관 후보와 자동 운영 설정·예상 결과를 UGUI에 제공합니다.
        public bool TryGetUGUIDelegationSnapshot(out WIAdministrationDelegationSnapshot snapshot, out string error)
        {
            snapshot = null;
            error = string.Empty;
            if (WIAdministrationTurnSystem.CanPlayerManageCastle(state, selectedCastle) == false)
            {
                error = "다른 진영의 성에는 영지관을 설정할 수 없습니다.";
                return false;
            }
            WICastleDefinition castle = database.GetCastle(selectedCastle.CastleId);
            snapshot = new WIAdministrationDelegationSnapshot
            {
                CastleName = castle.DisplayName.Get(database.UseEnglish),
                GovernorHeroId = selectedCastle.GovernorHeroId,
                Policy = selectedCastle.GovernorPolicy,
                MonthlyBudget = selectedCastle.GovernorMonthlyBudget,
                BasicBudget = database.ProjectBalance.BasicCost,
                IntensiveBudget = database.ProjectBalance.IntensiveCost,
                Delegated = selectedCastle.DelegatedToGovernor,
                Preview = WIAdministrationTurnSystem.GetDelegationPreview(database, state, selectedCastle)
            };
            foreach (string heroId in selectedCastle.HeroIds)
            {
                WIHeroDefinition hero = database.GetHero(heroId);
                if (hero == null) continue;
                snapshot.Candidates.Add(new WIAdministrationGovernorCandidateSnapshot
                {
                    HeroId = heroId,
                    DisplayName = hero.DisplayName.Get(database.UseEnglish),
                    Summary = GetTraitDisplayText(hero),
                    Portrait = hero.Portrait,
                    Interactable = state.IsCharacterBusy(heroId) == false || heroId == selectedCastle.GovernorHeroId
                });
            }
            return true;
        }

        // 기존 영지관 임명 규칙으로 선택한 인물을 임명합니다.
        public bool AssignUGUIGovernor(string heroId, out string error)
        {
            error = string.Empty;
            if (selectedCastle.HeroIds.Contains(heroId) == false ||
                WIAdministrationTurnSystem.AssignGovernor(state, selectedCastle.CastleId, heroId) == false)
            {
                error = "다른 임무를 수행 중인 인물은 영지관으로 임명할 수 없습니다.";
                return false;
            }
            RefreshAll();
            return true;
        }

        // 현재 영지관을 해임하고 직접 관리 상태로 되돌립니다.
        public bool DismissUGUIGovernor(out string error)
        {
            error = string.Empty;
            selectedCastle.GovernorHeroId = string.Empty;
            selectedCastle.DelegatedToGovernor = false;
            RefreshAll();
            return true;
        }

        // 선택 성의 영지관 자동 운영 방침을 변경합니다.
        public bool SetUGUIGovernorPolicy(WIGovernorPolicy policy, out string error)
        {
            error = string.Empty;
            selectedCastle.GovernorPolicy = policy;
            RefreshAll();
            return true;
        }

        // 기본 또는 집중 단계의 월간 영지관 예산을 지정합니다.
        public bool SetUGUIGovernorBudget(bool intensive, out string error)
        {
            error = string.Empty;
            selectedCastle.GovernorMonthlyBudget = intensive
                ? database.ProjectBalance.IntensiveCost : database.ProjectBalance.BasicCost;
            RefreshAll();
            return true;
        }

        // 영지관 임명 여부를 검증하고 위임·직접 관리 상태를 전환합니다.
        public bool ToggleUGUIDelegation(out string error)
        {
            error = string.Empty;
            if (selectedCastle.DelegatedToGovernor == false && string.IsNullOrEmpty(selectedCastle.GovernorHeroId))
            {
                error = "먼저 영지관을 임명해야 합니다.";
                return false;
            }
            selectedCastle.DelegatedToGovernor = selectedCastle.DelegatedToGovernor == false;
            SelectCastle(selectedCastle.CastleId);
            RefreshAll();
            return true;
        }

        // 현재 성에서 원정을 시작할 수 있는 주둔 전투단을 UGUI에 제공합니다.
        public bool TryGetUGUIMarchArmies(out WIAdministrationMarchSnapshot snapshot, out string error)
        {
            snapshot = new WIAdministrationMarchSnapshot();
            error = string.Empty;
            if (WIAdministrationTurnSystem.CanPlayerManageCastle(state, selectedCastle) == false)
            {
                error = "다른 진영의 성에서는 원정을 시작할 수 없습니다.";
                return false;
            }
            WICastleDefinition castle = database.GetCastle(selectedCastle.CastleId);
            snapshot.CastleName = castle.DisplayName.Get(database.UseEnglish);
            foreach (WIArmyState army in state.Armies)
            {
                if (army.CurrentCastleId != selectedCastle.CastleId || army.IsMoving || army.AwaitingBattle) continue;
                snapshot.Options.Add(new WIAdministrationMarchOptionSnapshot
                {
                    Id = army.ArmyId,
                    DisplayName = army.DisplayName,
                    Summary = $"{army.Members.Count}명 · {army.Proficiency}" +
                        (army.ReorganizationMonths > 0 ? $" · 재편성 {army.ReorganizationMonths}개월" : string.Empty),
                    Interactable = army.ReorganizationMonths <= 0
                });
            }
            if (snapshot.Options.Count > 0) return true;
            error = "주둔 전투단이 없습니다. 이 성의 대기 인물로 새 전투단을 편성하십시오.";
            return false;
        }

        // 새 전투단의 대장이 될 수 있는 현재 성의 대기 인물을 UGUI에 제공합니다.
        public bool TryGetUGUIMarchCommanders(out WIAdministrationMarchSnapshot snapshot, out string error)
        {
            snapshot = new WIAdministrationMarchSnapshot();
            error = string.Empty;
            WICastleDefinition castle = database.GetCastle(selectedCastle.CastleId);
            snapshot.CastleName = castle.DisplayName.Get(database.UseEnglish);
            foreach (string heroId in selectedCastle.HeroIds)
            {
                if (state.IsCharacterBusy(heroId)) continue;
                WIHeroDefinition hero = database.GetHero(heroId);
                if (hero == null) continue;
                snapshot.Options.Add(new WIAdministrationMarchOptionSnapshot
                {
                    Id = heroId,
                    DisplayName = hero.DisplayName.Get(database.UseEnglish),
                    Summary = $"통솔 {hero.Leadership} · {GetTraitDisplayText(hero)}",
                    Image = hero.Portrait
                });
            }
            if (snapshot.Options.Count > 0) return true;
            error = "새 전투단의 대장으로 지정할 대기 인물이 없습니다.";
            return false;
        }

        // 지정한 전투단이 선택할 수 있는 모든 인접 이동·원정 목표를 UGUI에 제공합니다.
        public bool TryGetUGUIMarchTargets(string armyId, out WIAdministrationMarchSnapshot snapshot,
            out string error)
        {
            snapshot = new WIAdministrationMarchSnapshot();
            error = string.Empty;
            WIArmyState army = state.Armies.Find(item => item.ArmyId == armyId);
            WICastleDefinition origin = army == null ? null : database.GetCastle(army.CurrentCastleId);
            if (army == null || origin == null || army.IsOperational == false)
            {
                error = "현재 이동 또는 원정할 수 없는 전투단입니다.";
                return false;
            }
            foreach (string targetId in origin.AdjacentCastleIds)
            {
                WICastleRuntimeState target = state.GetCastle(targetId);
                WICastleDefinition targetDefinition = database.GetCastle(targetId);
                if (target == null || targetDefinition == null) continue;
                bool friendly = target.FactionId == army.FactionId;
                snapshot.Options.Add(new WIAdministrationMarchOptionSnapshot
                {
                    Id = targetId,
                    DisplayName = (friendly ? "이동 · " : "원정 · ") + targetDefinition.DisplayName.Get(database.UseEnglish),
                    Summary = friendly ? "같은 진영 · 영향력 소모 없음" : "적대 진영 · 영향력 20",
                    Image = targetDefinition.CastleImage,
                    Interactable = friendly || (WIAdministrationTurnSystem.AreFactionsAtWar(state, army.FactionId, target.FactionId) &&
                        state.GetFactionState(army.FactionId).Influence >= 20)
                });
            }
            if (snapshot.Options.Count > 0) return true;
            error = "현재 성에 연결된 이동 경로가 없습니다.";
            return false;
        }

        // 선택한 대기 인물을 대장으로 기존 전투단 생성 로직을 실행합니다.
        public bool CreateUGUIMarchArmy(string commanderHeroId, out string armyId, out string error)
        {
            armyId = string.Empty;
            error = string.Empty;
            WIArmyState army = WIAdministrationTurnSystem.CreateArmy(database, state, selectedCastle, commanderHeroId);
            if (army == null)
            {
                error = "선택한 인물을 대장으로 새 전투단을 편성할 수 없습니다.";
                return false;
            }
            armyId = army.ArmyId;
            RefreshAll();
            return true;
        }

        // 기존 이동 규칙으로 선택 전투단의 이동 또는 원정을 시작합니다.
        public bool BeginUGUIArmyMarch(string armyId, string targetCastleId, out string error)
        {
            error = string.Empty;
            WIArmyState army = state.Armies.Find(item => item.ArmyId == armyId);
            WICastleRuntimeState target = state.GetCastle(targetCastleId);
            if (WIAdministrationTurnSystem.BeginArmyMarch(database, state, army, targetCastleId) == false)
            {
                error = GetArmyMarchFailureMessage(army, target);
                return false;
            }
            RefreshAll();
            return true;
        }

        // 현재 선택 성의 정보 공개 수준에 맞는 상세 기록 문자열을 UGUI에 제공합니다.
        public bool TryGetUGUICastleRecordSnapshot(out WIAdministrationCastleRecordSnapshot snapshot)
        {
            snapshot = null;
            if (selectedCastle == null || database == null || state == null) return false;
            WICastleDefinition castle = database.GetCastle(selectedCastle.CastleId);
            if (castle == null) return false;
            System.Text.StringBuilder body = new System.Text.StringBuilder();
            bool detailed = WIInformationVisibility.CanViewCastleDetails(state, state.PlayerFactionId, selectedCastle);
            if (detailed == false)
            {
                body.AppendLine("소유 진영과 지형 외 상세 정보가 확인되지 않았습니다.");
                body.AppendLine();
                body.AppendLine("첩보 메뉴에서 조사를 성공시키면 일정 기간 성 수치·주둔·시설 정보를 볼 수 있습니다.");
                if (WIInformationVisibility.CanViewMilitaryDetails(state, state.PlayerFactionId, selectedCastle))
                {
                    body.AppendLine();
                    body.AppendLine("현재 전투 접촉으로 전투 세션의 양측 전력만 확인할 수 있습니다.");
                }
            }
            else
            {
                body.AppendLine($"규모 · {selectedCastle.CastleSize}");
                body.AppendLine($"지형 · {castle.TerrainTrait.Get(database.UseEnglish)}");
                body.AppendLine($"특산 · {castle.Specialty.Get(database.UseEnglish)}");
                body.AppendLine($"전문 분야 효과 · {GetSpecialtyEffectDescription(castle)}");
                body.AppendLine();
                body.AppendLine($"번영 {selectedCastle.Prosperity} · 기술 {selectedCastle.Technology}");
                body.AppendLine($"질서 {selectedCastle.Stability} · 방어 {selectedCastle.Defense}");
                body.AppendLine($"주둔 인물 {selectedCastle.HeroIds.Count}/{selectedCastle.GetHeroSlotCount()}");
                body.AppendLine($"특화 시설 {selectedCastle.SpecialFacilityIds.Count}/{selectedCastle.GetSpecialFacilitySlotCount()}");
                if (selectedCastle.OccupationUnrestMonths > 0)
                {
                    body.AppendLine();
                    body.AppendLine($"점령 불안 · {selectedCastle.OccupationUnrestMonths}개월 · 영지관과 주둔 전투단 필요");
                }
                foreach (WIHeroLegacyState legacy in selectedCastle.HeroLegacies)
                {
                    body.AppendLine();
                    body.AppendLine($"영웅의 흔적 · {legacy.DisplayName} · {GetProjectDisplayName(legacy.ProjectType)} +{legacy.Bonus}");
                    if (string.IsNullOrEmpty(legacy.Description) == false) body.AppendLine(legacy.Description);
                }
                foreach (WIHeroLegacyState legacy in selectedCastle.CommemoratedHeroLegacies)
                {
                    body.AppendLine();
                    body.AppendLine($"기념 기록 · {legacy.DisplayName} · 효과 없음");
                }
                foreach (WIArmyState army in state.Armies)
                {
                    if (army.CurrentCastleId != selectedCastle.CastleId) continue;
                    body.AppendLine();
                    body.AppendLine($"주둔 전투단 · {army.DisplayName} · {army.Members.Count}명 · {army.Proficiency} · 보급 {army.Supply}");
                }
            }
            snapshot = new WIAdministrationCastleRecordSnapshot
            {
                CastleName = castle.DisplayName.Get(database.UseEnglish),
                CastleImage = castle.CastleImage,
                Body = body.ToString().TrimEnd()
            };
            return true;
        }

        // 현재 캠페인 목표의 정세·조건·진행도와 보상을 UGUI에 제공합니다.
        public bool TryGetUGUIObjectiveSnapshot(out WIAdministrationObjectiveSnapshot snapshot)
        {
            snapshot = null;
            WICampaignObjectiveDefinition objective = WICampaignObjectiveSystem.GetCurrent(database, state);
            if (objective == null) return false;
            int progress = WICampaignObjectiveSystem.GetProgress(state, objective);
            snapshot = new WIAdministrationObjectiveSnapshot
            {
                Title = objective.Title.Get(database.UseEnglish),
                Situation = objective.Situation.Get(database.UseEnglish),
                Description = objective.Description.Get(database.UseEnglish),
                Progress = $"진행 {progress}/{objective.TargetValue} · 보상 G {objective.RewardGold} / M {objective.RewardMana} / I {objective.RewardInfluence}"
            };
            return true;
        }

        // 지난달 자원·위임·AI 판단·뉴스와 처리할 사건·전투를 UGUI에 제공합니다.
        public bool TryGetUGUIMonthlyReportSnapshot(out WIAdministrationMonthlyReportSnapshot snapshot)
        {
            snapshot = null;
            WITurnSummary report = state?.LastMonthlyReport;
            if (report == null) return false;
            System.Text.StringBuilder body = new System.Text.StringBuilder();
            body.AppendLine($"금화 +{report.GoldGained} / 지출 -{report.GoldSpent}");
            body.AppendLine($"마나 +{report.ManaGained} / 영향력 +{report.InfluenceGained}");
            foreach (string entry in report.DelegationReports) body.AppendLine(entry);
            if (report.AIReasonReports != null && report.AIReasonReports.Count > 0)
            {
                body.AppendLine();
                body.AppendLine("AI 진영 판단 근거");
                foreach (string entry in report.AIReasonReports) body.AppendLine(entry);
            }
            if (report.News.Count > 0) body.AppendLine();
            foreach (string news in report.News) body.AppendLine(news);
            snapshot = new WIAdministrationMonthlyReportSnapshot { Body = body.ToString().TrimEnd() };
            for (int index = 0; index < state.PendingProjectEvents.Count; index += 1)
                snapshot.Actions.Add(new WIAdministrationReportActionSnapshot { Type = WIAdministrationReportActionType.ProjectEvent, Id = index.ToString(), Caption = "선택 사건 · " + state.PendingProjectEvents[index].Title });
            for (int index = 0; index < state.PendingRelationshipEvents.Count; index += 1)
            {
                WIRelationshipEventDefinition definition = database.GetRelationshipEvent(state.PendingRelationshipEvents[index].EventId);
                if (definition != null) snapshot.Actions.Add(new WIAdministrationReportActionSnapshot { Type = WIAdministrationReportActionType.RelationshipEvent, Id = index.ToString(), Caption = "관계 사건 · " + definition.Title.Get(database.UseEnglish) });
            }
            for (int index = 0; index < state.PendingRegionalEvents.Count; index += 1)
            {
                WIRegionalEventDefinition definition = database.GetRegionalEvent(state.PendingRegionalEvents[index].EventId);
                if (definition != null) snapshot.Actions.Add(new WIAdministrationReportActionSnapshot { Type = WIAdministrationReportActionType.RegionalEvent, Id = index.ToString(), Caption = "지역 사건 · " + definition.Title.Get(database.UseEnglish) });
            }
            for (int index = 0; index < state.PendingOccupationEvents.Count; index += 1)
            {
                WICastleDefinition castle = database.GetCastle(state.PendingOccupationEvents[index].CastleId);
                snapshot.Actions.Add(new WIAdministrationReportActionSnapshot { Type = WIAdministrationReportActionType.OccupationEvent, Id = index.ToString(), Caption = "점령 통치 · " + (castle?.DisplayName.Get(database.UseEnglish) ?? state.PendingOccupationEvents[index].CastleId) });
            }
            for (int index = 0; index < state.PendingRecruitmentEvents.Count; index += 1)
            {
                WIRecruitmentEventDefinition definition = database.GetRecruitmentEvent(state.PendingRecruitmentEvents[index].EventId);
                if (definition != null) snapshot.Actions.Add(new WIAdministrationReportActionSnapshot { Type = WIAdministrationReportActionType.RecruitmentEvent, Id = index.ToString(), Caption = "영입 요구 · " + definition.Title.Get(database.UseEnglish) });
            }
            for (int index = 0; index < state.PendingLegacyChoices.Count; index += 1)
                snapshot.Actions.Add(new WIAdministrationReportActionSnapshot { Type = WIAdministrationReportActionType.LegacyChoice, Id = index.ToString(), Caption = "영웅의 흔적 · " + state.PendingLegacyChoices[index].Legacy.DisplayName });
            foreach (WIBattleSessionState battle in state.BattleSessions.FindAll(item => item.PlayerInvolved && item.Status == WIBattleSessionStatus.Pending))
            {
                WICastleDefinition castle = database.GetCastle(battle.CastleId);
                snapshot.Actions.Add(new WIAdministrationReportActionSnapshot
                {
                    Type = WIAdministrationReportActionType.Battle,
                    Id = battle.SessionId,
                    Caption = $"전투 시작 · {castle?.DisplayName.Get(database.UseEnglish) ?? battle.CastleId} · 아군 {battle.AttackerPowerSnapshot} / 적군 {battle.DefenderPowerSnapshot}",
                    Danger = true
                });
            }
            return true;
        }

        // 월간 보고에서 선택한 사건은 기존 모달로, 전투는 기존 런타임 서비스로 전달합니다.
        public void ExecuteUGUIReportAction(WIAdministrationReportActionType type, string id)
        {
            if (type == WIAdministrationReportActionType.Battle)
            {
                WICampaignRuntimeService.Instance?.StartBattle(id);
                return;
            }
            UGUIEventChoiceRequested?.Invoke(type, id);
        }

        // QA에서 아발론 영지와 UGUI 중점 사업 모달을 바로 표시합니다.
        public void OpenFocusProjectUGUIForQA()
        {
            OpenCastlePreviewForQA();
            UGUIFocusProjectRequested?.Invoke();
        }

        // QA에서 아발론 영지와 UGUI 영웅 배치 모달을 바로 표시합니다.
        public void OpenHeroAssignmentUGUIForQA()
        {
            OpenCastlePreviewForQA();
            UGUIHeroAssignmentRequested?.Invoke();
        }

        // QA에서 아발론 영지와 UGUI 인재 활동 모달을 바로 표시합니다.
        public void OpenCharacterActivityUGUIForQA()
        {
            OpenCastlePreviewForQA();
            UGUICharacterActivityRequested?.Invoke();
        }

        // QA에서 아발론 영지의 특화 시설 선택 권한과 UGUI 모달을 바로 표시합니다.
        public void OpenSpecialFacilityUGUIForQA()
        {
            OpenCastlePreviewForQA();
            selectedCastle.PendingSpecialFacilityChoice = true;
            UGUISpecialFacilityRequested?.Invoke();
        }

        // QA에서 아발론 영지의 기본 시설 UGUI를 바로 표시합니다.
        public void OpenBasicFacilityUGUIForQA()
        {
            OpenCastlePreviewForQA();
            UGUIBasicFacilityRequested?.Invoke();
        }

        // QA에서 아발론 영지의 영지관 위임 UGUI를 바로 표시합니다.
        public void OpenDelegationUGUIForQA()
        {
            OpenCastlePreviewForQA();
            UGUIDelegationRequested?.Invoke();
        }

        // QA에서 아발론 영지의 원정 UGUI를 바로 표시합니다.
        public void OpenMarchUGUIForQA()
        {
            OpenCastlePreviewForQA();
            UGUIMarchRequested?.Invoke();
        }

        // QA에서 아발론 영지의 성 상세 기록 UGUI를 바로 표시합니다.
        public void OpenCastleRecordUGUIForQA()
        {
            OpenCastlePreviewForQA();
            UGUICastleRecordRequested?.Invoke();
        }

        // QA에서 현재 캠페인 목표 UGUI를 바로 표시합니다.
        public void OpenObjectiveUGUIForQA()
        {
            OpenCastlePreviewForQA();
            UGUIObjectiveRequested?.Invoke();
        }

        // QA에서 지난달 월간 보고 UGUI를 바로 표시합니다.
        public void OpenMonthlyReportUGUIForQA()
        {
            OpenCastlePreviewForQA();
            if (state.LastMonthlyReport == null)
            {
                state.LastMonthlyReport = new WITurnSummary
                {
                    GoldGained = 320,
                    GoldSpent = 100,
                    ManaGained = 45,
                    InfluenceGained = 20
                };
                state.LastMonthlyReport.DelegationReports.Add("[위임 결과] 아발론 · 번영 사업 · 예상 +5 / 실제 +6 · 비용 100G");
                state.LastMonthlyReport.AIReasonReports.Add("적대 진영은 접경 방어를 우선하고 전투단을 재편성했습니다.");
                state.LastMonthlyReport.News.Add("아발론의 시장이 활기를 되찾아 주민들의 왕래가 늘었습니다.");
                state.LastMonthlyReport.News.Add("북부 국경에서 적 진영의 정찰 움직임이 보고되었습니다.");
            }
            UGUIMonthlyReportRequested?.Invoke();
        }

        // UGUI 하단 명령 버튼을 기존 게임 기능에 연결합니다.
        public void ExecuteUGUIShortcut(WIAdministrationShortcutAction action)
        {
            switch (action)
            {
                case WIAdministrationShortcutAction.Military:
                    UGUIMilitaryRequested?.Invoke();
                    return;
                case WIAdministrationShortcutAction.Heroes:
                    UGUIHeroesRequested?.Invoke();
                    return;
                case WIAdministrationShortcutAction.Diplomacy:
                    UGUIDiplomacyRequested?.Invoke();
                    return;
                case WIAdministrationShortcutAction.Scheme:
                    UGUISchemeRequested?.Invoke();
                    return;
                case WIAdministrationShortcutAction.Research:
                    UGUIResearchRequested?.Invoke();
                    return;
                case WIAdministrationShortcutAction.Faction:
                    UGUIFactionRequested?.Invoke();
                    return;
                case WIAdministrationShortcutAction.Council:
                    UGUICouncilRequested?.Invoke();
                    return;
                case WIAdministrationShortcutAction.MonthlyReport:
                    UGUIMonthlyReportRequested?.Invoke();
                    return;
                case WIAdministrationShortcutAction.EndTurn:
                    BeginTurn();
                    return;
            }
            SuspendUGUIForLegacyModal();
        }

        // 현재 플레이어의 전투 세션과 전투단을 UGUI 목록용 스냅샷으로 반환합니다.
        public bool TryGetUGUIMilitarySnapshot(out WIAdministrationMilitarySnapshot snapshot)
        {
            snapshot = null;
            if (state == null || database == null) return false;
            snapshot = new WIAdministrationMilitarySnapshot();
            int battleCount = 0;
            foreach (WIBattleSessionState session in state.BattleSessions.Where(item =>
                         item.Status != WIBattleSessionStatus.Resolved && item.PlayerInvolved))
            {
                battleCount += 1;
                snapshot.Items.Add(new WIAdministrationMilitaryItemSnapshot
                {
                    Kind = "battle",
                    Id = session.SessionId,
                    Title = "전투 세션 · " + database.GetCastle(session.CastleId).DisplayName.Get(database.UseEnglish),
                    Description = $"{session.Status} · 공격 {session.AttackerPowerSnapshot} / 수비 {session.DefenderPowerSnapshot}"
                });
            }
            int armyCount = 0;
            foreach (WIArmyState army in state.Armies.Where(item => item.FactionId == state.PlayerFactionId))
            {
                armyCount += 1;
                string status = army.AwaitingBattle ? "전투 대기" : army.IsMoving ? $"이동 {army.RemainingTravelMonths}개월" : "주둔";
                snapshot.Items.Add(new WIAdministrationMilitaryItemSnapshot
                {
                    Kind = "army",
                    Id = army.ArmyId,
                    Title = army.DisplayName,
                    Description = $"{army.Members.Count}/{WIAdministrationTurnSystem.GetRecommendedArmySize(database, army)}명 · {army.Proficiency} · 보급 {army.Supply} · {status}"
                });
            }
            snapshot.Summary = $"진행 중인 전투 {battleCount}건 · 플레이어 전투단 {armyCount}개";
            return true;
        }

        // 선택 인물이 이동할 수 있는 같은 진영의 인접 성과 목적지 슬롯 상태를 UGUI에 제공합니다.
        public bool TryGetUGUICharacterTransferTargets(string actorHeroId,
            out WIAdministrationCharacterActivitySnapshot snapshot, out string error)
        {
            snapshot = new WIAdministrationCharacterActivitySnapshot();
            error = string.Empty;
            WICharacterRuntimeState actor = state.GetCharacter(actorHeroId);
            WICastleRuntimeState originState = state.Castles.FirstOrDefault(castle => castle.HeroIds.Contains(actorHeroId));
            WICastleDefinition origin = originState == null ? null : database.GetCastle(originState.CastleId);
            if (actor == null || originState == null || origin == null || state.IsCharacterBusy(actorHeroId))
            {
                error = "이동할 인물을 다시 선택하십시오.";
                return false;
            }
            foreach (string targetId in origin.AdjacentCastleIds)
            {
                WICastleRuntimeState target = state.GetCastle(targetId);
                WICastleDefinition targetDefinition = database.GetCastle(targetId);
                if (target == null || targetDefinition == null || target.FactionId != originState.FactionId) continue;
                int reservedSlots = state.CharacterTransfers.Count(transfer => transfer.TargetCastleId == targetId);
                int occupiedSlots = target.HeroIds.Count + reservedSlots;
                snapshot.Candidates.Add(new WIAdministrationCharacterActivityCandidateSnapshot
                {
                    HeroId = targetId,
                    DisplayName = targetDefinition.DisplayName.Get(database.UseEnglish),
                    Summary = $"인물 슬롯 {occupiedSlots}/{target.GetHeroSlotCount()} · 이동 1개월",
                    Interactable = occupiedSlots < target.GetHeroSlotCount()
                });
            }
            if (snapshot.Candidates.Count > 0) return true;
            error = "이동 가능한 같은 진영의 인접 성이 없습니다.";
            return false;
        }

        // 기존 인물 이동 시스템으로 한 달 이동을 시작하고 영지 화면을 갱신합니다.
        public bool StartUGUICharacterTransfer(string actorHeroId, string targetCastleId, out string message)
        {
            message = string.Empty;
            if (WIAdministrationTurnSystem.StartCharacterTransfer(database, state, actorHeroId, targetCastleId) == false)
            {
                message = "이동할 수 없습니다. 임무, 영지관직, 인접 경로와 목적지 슬롯을 확인하십시오.";
                return false;
            }
            WICastleDefinition target = database.GetCastle(targetCastleId);
            SelectCastle(selectedCastle.CastleId);
            RefreshAll();
            message = $"{target?.DisplayName.Get(database.UseEnglish) ?? targetCastleId} 이동을 시작했습니다. 다음 달에 도착합니다.";
            return true;
        }

        // UGUI 좌측 영지 요약에서 대표 성의 기존 상세 화면을 엽니다.
        public void OpenUGUIGlobalSummaryCastle()
        {
            OpenGlobalSummaryCastle();
        }

        // UGUI 지도에서 선택한 성을 기존 영지 상세 흐름으로 열고 월드 UGUI를 숨깁니다.
        public void OpenCastleFromUGUI(string castleId)
        {
            OpenCastle(castleId);
        }

        // 기존 영지 화면에서 대륙 화면으로 돌아올 때 월드 UGUI 표시 상태를 복원합니다.
        private void RestoreUGUIWorldVisibility()
        {
            uguiWorldVisible = true;
            NotifyUGUIWorldChanged();
        }

        // 영지 상세 화면 전환 시 신규 월드 UGUI를 숨기고 영지 UGUI 갱신을 알립니다.
        private void ActivateUGUITerritoryVisibility()
        {
            uguiWorldVisible = false;
            NotifyUGUIWorldChanged();
        }

        // UGUI 우측 목표 카드에서 기존 목표 상세 화면을 엽니다.
        public void OpenUGUIObjective()
        {
            OpenUGUICampaignObjective(false);
        }

        // UGUI 우측 전투 알림에서 기존 전투 또는 월간 보고 화면을 엽니다.
        public void OpenUGUIBattleAlert()
        {
            UGUIMonthlyReportRequested?.Invoke();
        }

        // 신규 UGUI 영지 화면에서 대륙 지도로 돌아갑니다.
        public void ReturnToUGUIWorld()
        {
            ShowGlobalView();
        }

        // UGUI 영지 명령을 기존 게임 기능에 연결합니다.
        public void ExecuteUGUITerritoryCommand(WIAdministrationTerritoryCommand command)
        {
            switch (command)
            {
                case WIAdministrationTerritoryCommand.FocusProject:
                    UGUIFocusProjectRequested?.Invoke();
                    return;
                case WIAdministrationTerritoryCommand.AssignHero:
                    UGUIHeroAssignmentRequested?.Invoke();
                    return;
                case WIAdministrationTerritoryCommand.CharacterActivity:
                    UGUICharacterActivityRequested?.Invoke();
                    return;
                case WIAdministrationTerritoryCommand.ChooseSpecialFacility:
                    UGUISpecialFacilityRequested?.Invoke();
                    return;
                case WIAdministrationTerritoryCommand.BasicFacility:
                    UGUIBasicFacilityRequested?.Invoke();
                    return;
                case WIAdministrationTerritoryCommand.Delegation:
                    UGUIDelegationRequested?.Invoke();
                    return;
                case WIAdministrationTerritoryCommand.March:
                    UGUIMarchRequested?.Invoke();
                    return;
                case WIAdministrationTerritoryCommand.CastleRecord:
                    UGUICastleRecordRequested?.Invoke();
                    return;
            }
            SuspendUGUIForLegacyModal();
        }

        // 기존 UI Toolkit 모달이 표시되는 동안 UGUI 화면을 일시적으로 숨깁니다.
        private void SuspendUGUIForLegacyModal()
        {
            uguiSuspendedForLegacyModal = true;
            NotifyUGUIWorldChanged();
        }

        // 기존 UI Toolkit 모달이 닫히면 현재 화면의 UGUI 표시를 복원합니다.
        private void ResumeUGUIAfterLegacyModal()
        {
            uguiSuspendedForLegacyModal = false;
            NotifyUGUIWorldChanged();
        }

        // 월드 상태가 바뀌었음을 신규 UGUI 화면에 알립니다.
        private void NotifyUGUIWorldChanged()
        {
            UGUIWorldChanged?.Invoke();
        }
    }
}

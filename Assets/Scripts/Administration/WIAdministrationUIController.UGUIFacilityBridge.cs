using System.Linq;

namespace ProjectWI.Administration
{
    public partial class WIAdministrationUIController
    {
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
                bool administrationCapable = WIAdministrationTurnSystem.IsAdministrationCapable(state, heroId);
                snapshot.Candidates.Add(new WIAdministrationGovernorCandidateSnapshot
                {
                    HeroId = heroId,
                    DisplayName = hero.DisplayName.Get(database.UseEnglish),
                    Summary = GetTraitDisplayText(hero),
                    Portrait = hero.Portrait,
                    Interactable = administrationCapable &&
                                   (state.IsCharacterBusy(heroId) == false || heroId == selectedCastle.GovernorHeroId)
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
                error = "영웅 등급의 대기 인물만 영지관으로 임명할 수 있습니다.";
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

    }
}


using System.Collections.Generic;

namespace ProjectWI.Administration
{
    public partial class WIAdministrationUIController
    {
        // 월간 보고의 사건 종류와 현재 목록 위치를 UGUI 선택지로 변환합니다.
        public bool TryGetUGUIEventChoiceSnapshot(WIAdministrationReportActionType type, string id,
            out WIAdministrationEventChoiceSnapshot snapshot)
        {
            snapshot = null;
            if (state == null || database == null || int.TryParse(id, out int index) == false) return false;
            switch (type)
            {
                case WIAdministrationReportActionType.ProjectEvent:
                    return TryBuildProjectEventSnapshot(index, out snapshot);
                case WIAdministrationReportActionType.RelationshipEvent:
                    return TryBuildRelationshipEventSnapshot(index, out snapshot);
                case WIAdministrationReportActionType.RegionalEvent:
                    return TryBuildRegionalEventSnapshot(index, out snapshot);
                case WIAdministrationReportActionType.OccupationEvent:
                    return TryBuildOccupationEventSnapshot(index, out snapshot);
                case WIAdministrationReportActionType.RecruitmentEvent:
                    return TryBuildRecruitmentEventSnapshot(index, out snapshot);
                case WIAdministrationReportActionType.LegacyChoice:
                    return TryBuildLegacyChoiceSnapshot(index, out snapshot);
                default:
                    return false;
            }
        }

        // 선택한 UGUI 사건 결과를 기존 판정 API에 전달합니다.
        public bool ResolveUGUIEventChoice(WIAdministrationReportActionType type, string id, int choiceIndex)
        {
            if (state == null || int.TryParse(id, out int index) == false || choiceIndex < 0) return false;
            bool resolved;
            switch (type)
            {
                case WIAdministrationReportActionType.ProjectEvent:
                    if (index >= state.PendingProjectEvents.Count) return false;
                    int[] gains = { 2, 4, 5 };
                    if (choiceIndex >= gains.Length || (choiceIndex == 2 && state.PendingProjectEvents[index].HeroChoiceAvailable == false)) return false;
                    resolved = WIAdministrationTurnSystem.ResolveProjectEvent(state, state.PendingProjectEvents[index], gains[choiceIndex], choiceIndex == 1, state.LastMonthlyReport);
                    break;
                case WIAdministrationReportActionType.RelationshipEvent:
                    if (index >= state.PendingRelationshipEvents.Count) return false;
                    resolved = WIAdministrationTurnSystem.ResolveRelationshipEvent(database, state, state.PendingRelationshipEvents[index], choiceIndex, state.LastMonthlyReport);
                    break;
                case WIAdministrationReportActionType.RegionalEvent:
                    if (index >= state.PendingRegionalEvents.Count) return false;
                    resolved = WIRegionalEventSystem.Resolve(database, state, state.PendingRegionalEvents[index], choiceIndex, state.LastMonthlyReport);
                    break;
                case WIAdministrationReportActionType.OccupationEvent:
                    if (index >= state.PendingOccupationEvents.Count) return false;
                    resolved = WIOccupationEventSystem.Resolve(database, state, state.PendingOccupationEvents[index], choiceIndex, state.LastMonthlyReport);
                    break;
                case WIAdministrationReportActionType.RecruitmentEvent:
                    if (index >= state.PendingRecruitmentEvents.Count) return false;
                    resolved = WIAdministrationTurnSystem.ResolveRecruitmentEvent(database, state, state.PendingRecruitmentEvents[index], choiceIndex, state.LastMonthlyReport);
                    break;
                case WIAdministrationReportActionType.LegacyChoice:
                    if (index >= state.PendingLegacyChoices.Count) return false;
                    WIPendingLegacyChoice pending = state.PendingLegacyChoices[index];
                    WICastleRuntimeState castle = state.GetCastle(pending.CastleId);
                    if (castle == null) return false;
                    if (castle.HeroLegacies.Count < 2)
                    {
                        WIAdministrationTurnSystem.InstallHeroLegacy(state, castle, pending);
                        resolved = true;
                    }
                    else
                    {
                        if (choiceIndex >= castle.HeroLegacies.Count) return false;
                        WIAdministrationTurnSystem.InstallHeroLegacy(state, castle, pending, castle.HeroLegacies[choiceIndex]);
                        resolved = true;
                    }
                    break;
                default:
                    return false;
            }
            if (resolved) RefreshAll();
            return resolved;
        }

        // 사업 선택 사건의 안전·과감·영웅 결과를 구성합니다.
        private bool TryBuildProjectEventSnapshot(int index, out WIAdministrationEventChoiceSnapshot snapshot)
        {
            snapshot = null;
            if (index >= state.PendingProjectEvents.Count) return false;
            WIPendingProjectEvent pending = state.PendingProjectEvents[index];
            snapshot = new WIAdministrationEventChoiceSnapshot { Title = pending.Title, Description = "이번 달 사업의 돌발 결과를 선택하십시오." };
            snapshot.Choices.Add(new WIAdministrationEventChoiceCardSnapshot { Label = "안전한 선택\n관련 성 수치 +2" });
            snapshot.Choices.Add(new WIAdministrationEventChoiceCardSnapshot { Label = "과감한 선택\n관련 성 수치 +4 · 질서 -2" });
            if (pending.HeroChoiceAvailable)
            {
                WIHeroDefinition hero = database.GetHero(pending.HeroId);
                snapshot.Choices.Add(new WIAdministrationEventChoiceCardSnapshot { Label = $"영웅 선택지\n{hero?.DisplayName.Get(database.UseEnglish) ?? pending.HeroId}의 특기로 +5" });
            }
            return true;
        }

        // 인물 관계 사건의 등장인물과 데이터 선택지를 구성합니다.
        private bool TryBuildRelationshipEventSnapshot(int index, out WIAdministrationEventChoiceSnapshot snapshot)
        {
            snapshot = null;
            if (index >= state.PendingRelationshipEvents.Count) return false;
            WIPendingRelationshipEvent pending = state.PendingRelationshipEvents[index];
            WIRelationshipEventDefinition definition = database.GetRelationshipEvent(pending.EventId);
            WIHeroDefinition first = database.GetHero(pending.FirstHeroId);
            WIHeroDefinition second = database.GetHero(pending.SecondHeroId);
            if (definition == null || first == null || second == null) return false;
            snapshot = new WIAdministrationEventChoiceSnapshot
            {
                Title = definition.Title.Get(database.UseEnglish),
                Description = $"{first.DisplayName.Get(database.UseEnglish)} · {second.DisplayName.Get(database.UseEnglish)}\n{definition.Description.Get(database.UseEnglish)}"
            };
            foreach (WIRelationshipEventChoiceDefinition choice in definition.Choices)
                snapshot.Choices.Add(new WIAdministrationEventChoiceCardSnapshot { Label = choice.Label.Get(database.UseEnglish) + "\n" + choice.ResultDescription.Get(database.UseEnglish) });
            return true;
        }

        // 지역 사건의 설명과 자원·영지 결과 선택지를 구성합니다.
        private bool TryBuildRegionalEventSnapshot(int index, out WIAdministrationEventChoiceSnapshot snapshot)
        {
            snapshot = null;
            if (index >= state.PendingRegionalEvents.Count) return false;
            WIRegionalEventDefinition definition = database.GetRegionalEvent(state.PendingRegionalEvents[index].EventId);
            if (definition == null) return false;
            snapshot = new WIAdministrationEventChoiceSnapshot { Title = definition.Title.Get(database.UseEnglish), Description = definition.Description.Get(database.UseEnglish) };
            foreach (WIRegionalEventChoiceDefinition choice in definition.Choices)
                snapshot.Choices.Add(new WIAdministrationEventChoiceCardSnapshot { Label = choice.Label.Get(database.UseEnglish) + "\n" + choice.ResultDescription.Get(database.UseEnglish), Interactable = WIRegionalEventSystem.CanChoose(state, choice) });
            return true;
        }

        // 점령지의 배경과 통치 방침 선택지를 구성합니다.
        private bool TryBuildOccupationEventSnapshot(int index, out WIAdministrationEventChoiceSnapshot snapshot)
        {
            snapshot = null;
            if (index >= state.PendingOccupationEvents.Count) return false;
            WIPendingOccupationEvent pending = state.PendingOccupationEvents[index];
            WICastleDefinition castle = database.GetCastle(pending.CastleId);
            WIFactionDefinition defeated = database.GetFaction(pending.DefeatedFactionId);
            snapshot = new WIAdministrationEventChoiceSnapshot
            {
                Title = "점령 통치 · " + (castle?.DisplayName.Get(database.UseEnglish) ?? pending.CastleId),
                Description = $"구 소유 진영 · {defeated?.DisplayName.Get(database.UseEnglish) ?? pending.DefeatedFactionId}\n점령 불안을 줄일 통치 방침을 선택하십시오."
            };
            foreach (WIOccupationChoiceDefinition choice in database.OccupationChoices)
                snapshot.Choices.Add(new WIAdministrationEventChoiceCardSnapshot { Label = $"{choice.Label.Get(database.UseEnglish)}\n{choice.ResultDescription.Get(database.UseEnglish)} · 불안 {choice.UnrestMonths}개월", Interactable = WIOccupationEventSystem.CanChoose(state, choice) });
            return true;
        }

        // 영입 요구 사건의 대상과 조건별 선택지를 구성합니다.
        private bool TryBuildRecruitmentEventSnapshot(int index, out WIAdministrationEventChoiceSnapshot snapshot)
        {
            snapshot = null;
            if (index >= state.PendingRecruitmentEvents.Count) return false;
            WIPendingRecruitmentEvent pending = state.PendingRecruitmentEvents[index];
            WIRecruitmentEventDefinition definition = database.GetRecruitmentEvent(pending.EventId);
            WIHeroDefinition candidate = database.GetHero(pending.CandidateHeroId);
            if (definition == null || candidate == null) return false;
            snapshot = new WIAdministrationEventChoiceSnapshot
            {
                Title = definition.Title.Get(database.UseEnglish),
                Description = $"영입 대상 · {candidate.DisplayName.Get(database.UseEnglish)}\n{definition.Description.Get(database.UseEnglish)}"
            };
            foreach (WIRecruitmentEventChoiceDefinition choice in definition.Choices)
                snapshot.Choices.Add(new WIAdministrationEventChoiceCardSnapshot { Label = $"{choice.Label.Get(database.UseEnglish)}\n{GetRecruitmentChoiceCondition(choice)} · {choice.ResultDescription.Get(database.UseEnglish)}", Interactable = WIAdministrationTurnSystem.CanChooseRecruitmentEventOption(database, state, pending, choice) });
            return true;
        }

        // 새 영웅의 흔적 기록 또는 기존 흔적 교체 선택지를 구성합니다.
        private bool TryBuildLegacyChoiceSnapshot(int index, out WIAdministrationEventChoiceSnapshot snapshot)
        {
            snapshot = null;
            if (index >= state.PendingLegacyChoices.Count) return false;
            WIPendingLegacyChoice pending = state.PendingLegacyChoices[index];
            WICastleRuntimeState castle = state.GetCastle(pending.CastleId);
            if (castle == null) return false;
            snapshot = new WIAdministrationEventChoiceSnapshot
            {
                Title = castle.HeroLegacies.Count < 2 ? "영웅의 흔적 기록" : "교체할 영웅의 흔적 선택",
                Description = $"새 흔적 · {pending.Legacy.DisplayName} · {GetProjectDisplayName(pending.Legacy.ProjectType)} +{pending.Legacy.Bonus}\n{pending.Legacy.Description}"
            };
            if (castle.HeroLegacies.Count < 2)
            {
                snapshot.Choices.Add(new WIAdministrationEventChoiceCardSnapshot { Label = "새 흔적 기록\n빈 기록 슬롯에 영웅의 흔적을 남깁니다." });
                return true;
            }
            foreach (WIHeroLegacyState oldLegacy in new List<WIHeroLegacyState>(castle.HeroLegacies))
                snapshot.Choices.Add(new WIAdministrationEventChoiceCardSnapshot { Label = $"교체 · {oldLegacy.DisplayName}\n{GetProjectDisplayName(oldLegacy.ProjectType)} +{oldLegacy.Bonus} → 기념 기록" });
            return true;
        }
    }
}

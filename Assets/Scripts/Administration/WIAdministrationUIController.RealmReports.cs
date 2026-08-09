using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using ProjectWI.Systems;

namespace ProjectWI.Administration
{
    public partial class WIAdministrationUIController
    {
        // 선택한 성의 영지관, 운영 방침과 월간 예산을 설정합니다.
        private void OpenDelegationModal()
        {
            if (EnsureSelectedCastleManageable() == false)
            {
                return;
            }

            VisualElement panel = CreateModal("영지관 위임 설정");
            panel.Add(new Label("영지관 임명"));
            foreach (string heroId in selectedCastle.HeroIds)
            {
                WIHeroDefinition hero = database.GetHero(heroId);
                Button governorButton = new Button(() =>
                {
                    if (WIAdministrationTurnSystem.AssignGovernor(state, selectedCastle.CastleId, hero.Id) == false)
                    {
                        ShowMessage("다른 임무를 수행 중인 인물은 영지관로 임명할 수 없습니다.");
                        return;
                    }
                    CloseModal();
                    OpenDelegationModal();
                });
                governorButton.text = hero.Id == selectedCastle.GovernorHeroId
                    ? $"● {hero.DisplayName.Get(database.UseEnglish)}"
                    : hero.DisplayName.Get(database.UseEnglish);
                panel.Add(governorButton);
            }

            Button dismissGovernor = new Button(() =>
            {
                selectedCastle.GovernorHeroId = string.Empty;
                selectedCastle.DelegatedToGovernor = false;
                CloseModal();
                OpenDelegationModal();
            });
            dismissGovernor.text = "영지관 해임";
            panel.Add(dismissGovernor);

            panel.Add(new Label("운영 방침"));
            foreach (WIGovernorPolicy policy in System.Enum.GetValues(typeof(WIGovernorPolicy)))
            {
                Button policyButton = new Button(() =>
                {
                    selectedCastle.GovernorPolicy = policy;
                    CloseModal();
                    OpenDelegationModal();
                });
                policyButton.text = policy == selectedCastle.GovernorPolicy
                    ? $"● {GetGovernorPolicyDisplayName(policy)}"
                    : GetGovernorPolicyDisplayName(policy);
                panel.Add(policyButton);
            }

            Button basicBudget = new Button(() => SetGovernorBudget(database.ProjectBalance.BasicCost));
            basicBudget.text = $"기본 예산 · 월 {database.ProjectBalance.BasicCost}G";
            panel.Add(basicBudget);
            Button intensiveBudget = new Button(() => SetGovernorBudget(database.ProjectBalance.IntensiveCost));
            intensiveBudget.text = $"집중 예산 · 월 {database.ProjectBalance.IntensiveCost}G";
            panel.Add(intensiveBudget);

            panel.Add(new Label(WIAdministrationTurnSystem.GetDelegationPreview(database, state, selectedCastle)));

            Button toggle = new Button(() =>
            {
                if (selectedCastle.DelegatedToGovernor == false && string.IsNullOrEmpty(selectedCastle.GovernorHeroId))
                {
                    ShowMessage("먼저 영지관을 임명해야 합니다.");
                    return;
                }
                selectedCastle.DelegatedToGovernor = selectedCastle.DelegatedToGovernor == false;
                CloseModal();
                SelectCastle(selectedCastle.CastleId);
            });
            toggle.text = selectedCastle.DelegatedToGovernor ? "직접 관리로 전환" : "영지관에게 위임";
            panel.Add(toggle);
        }

        // 영지관에게 허용할 월간 사업 예산을 변경합니다.
        private void SetGovernorBudget(int budget)
        {
            selectedCastle.GovernorMonthlyBudget = budget;
            CloseModal();
            OpenDelegationModal();
        }

        // 최근 완료된 턴의 자원, 사업과 위임 결과를 월간 보고로 표시합니다.
        private void OpenMonthlyReportModal()
        {
            if (state.LastMonthlyReport == null)
            {
                ShowMessage("아직 작성된 월간 보고가 없습니다. 다음 턴을 진행하십시오.");
                return;
            }

            WITurnSummary report = state.LastMonthlyReport;
            VisualElement panel = CreateModalWithFooter("지난달 월간 보고", out VisualElement battleFooter);
            panel.Add(new Label($"금화 +{report.GoldGained} / 지출 -{report.GoldSpent}"));
            panel.Add(new Label($"마나 +{report.ManaGained} / 영향력 +{report.InfluenceGained}"));
            foreach (string delegationReport in report.DelegationReports)
            {
                panel.Add(new Label(delegationReport));
            }

            if (report.AIReasonReports != null && report.AIReasonReports.Count > 0) panel.Add(new Label("AI 진영 판단 근거"));
            foreach (string aiReport in report.AIReasonReports ?? new List<string>())
            {
                panel.Add(new Label(aiReport));
            }

            foreach (string news in report.News)
            {
                panel.Add(new Label(news));
            }

            foreach (WIPendingProjectEvent pendingEvent in new List<WIPendingProjectEvent>(state.PendingProjectEvents))
            {
                Button eventButton = new Button(() => OpenProjectEventModal(pendingEvent));
                eventButton.text = $"선택 사건 · {pendingEvent.Title}";
                panel.Add(eventButton);
            }

            foreach (WIPendingRelationshipEvent pendingEvent in new List<WIPendingRelationshipEvent>(state.PendingRelationshipEvents))
            {
                WIRelationshipEventDefinition definition = database.GetRelationshipEvent(pendingEvent.EventId);
                if (definition == null) continue;
                Button eventButton = new Button(() => OpenRelationshipEventModal(pendingEvent));
                eventButton.text = $"관계 사건 · {definition.Title.Get(database.UseEnglish)}";
                panel.Add(eventButton);
            }

            foreach (WIPendingRegionalEvent pendingEvent in new List<WIPendingRegionalEvent>(state.PendingRegionalEvents))
            {
                WIRegionalEventDefinition definition = database.GetRegionalEvent(pendingEvent.EventId);
                if (definition == null) continue;
                Button eventButton = new Button(() => OpenRegionalEventModal(pendingEvent));
                eventButton.text = $"지역 사건 · {definition.Title.Get(database.UseEnglish)}";
                panel.Add(eventButton);
            }

            foreach (WIPendingOccupationEvent pendingEvent in new List<WIPendingOccupationEvent>(state.PendingOccupationEvents))
            {
                WICastleDefinition castle = database.GetCastle(pendingEvent.CastleId);
                Button eventButton = new Button(() => OpenOccupationEventModal(pendingEvent));
                eventButton.text = $"점령 통치 · {castle?.DisplayName.Get(database.UseEnglish) ?? pendingEvent.CastleId}";
                panel.Add(eventButton);
            }

            foreach (WIPendingRecruitmentEvent pendingEvent in new List<WIPendingRecruitmentEvent>(state.PendingRecruitmentEvents))
            {
                WIRecruitmentEventDefinition definition = database.GetRecruitmentEvent(pendingEvent.EventId);
                if (definition == null) continue;
                Button eventButton = new Button(() => OpenRecruitmentEventModal(pendingEvent));
                eventButton.text = $"영입 요구 · {definition.Title.Get(database.UseEnglish)}";
                panel.Add(eventButton);
            }

            foreach (WIPendingLegacyChoice legacyChoice in new List<WIPendingLegacyChoice>(state.PendingLegacyChoices))
            {
                Button legacyButton = new Button(() => ConfirmLegacyChoice(legacyChoice));
                legacyButton.text = $"영웅의 흔적 · {legacyChoice.Legacy.DisplayName}";
                panel.Add(legacyButton);
            }
            AddPendingBattleActions(battleFooter);
        }

        // 지역 사건의 배경과 ScriptableObject 선택 결과를 표시합니다.
        private void OpenRegionalEventModal(WIPendingRegionalEvent pendingEvent)
        {
            WIRegionalEventDefinition definition = database.GetRegionalEvent(pendingEvent.EventId);
            if (definition == null) return;
            VisualElement panel = CreateModal(definition.Title.Get(database.UseEnglish));
            panel.Add(new Label(definition.Description.Get(database.UseEnglish)));
            for (int index = 0; index < definition.Choices.Count; index += 1)
            {
                int selectedIndex = index;
                WIRegionalEventChoiceDefinition choice = definition.Choices[index];
                Button button = new Button(() =>
                {
                    if (WIRegionalEventSystem.Resolve(database, state, pendingEvent, selectedIndex, state.LastMonthlyReport))
                    {
                        CloseModal();
                        RefreshAll();
                    }
                });
                button.text = $"{choice.Label.Get(database.UseEnglish)} · {choice.ResultDescription.Get(database.UseEnglish)}";
                button.SetEnabled(WIRegionalEventSystem.CanChoose(state, choice));
                panel.Add(button);
            }
        }

        // 새 점령지의 통치 방침과 예상 자원·불안 결과를 표시합니다.
        private void OpenOccupationEventModal(WIPendingOccupationEvent pendingEvent)
        {
            WICastleDefinition castle = database.GetCastle(pendingEvent.CastleId);
            WIFactionDefinition defeatedFaction = database.GetFaction(pendingEvent.DefeatedFactionId);
            VisualElement panel = CreateModal($"점령 통치 · {castle?.DisplayName.Get(database.UseEnglish) ?? pendingEvent.CastleId}");
            panel.Add(new Label($"구 소유 진영 · {defeatedFaction?.DisplayName.Get(database.UseEnglish) ?? pendingEvent.DefeatedFactionId}\n점령 불안을 줄일 통치 방침을 선택하십시오."));
            for (int index = 0; index < database.OccupationChoices.Count; index += 1)
            {
                int selectedIndex = index;
                WIOccupationChoiceDefinition choice = database.OccupationChoices[index];
                Button button = new Button(() =>
                {
                    if (WIOccupationEventSystem.Resolve(database, state, pendingEvent, selectedIndex, state.LastMonthlyReport))
                    {
                        CloseModal();
                        RefreshAll();
                    }
                });
                button.text = $"{choice.Label.Get(database.UseEnglish)} · {choice.ResultDescription.Get(database.UseEnglish)} · 불안 {choice.UnrestMonths}개월";
                button.SetEnabled(WIOccupationEventSystem.CanChoose(state, choice));
                panel.Add(button);
            }
        }

        // 영입 요구 사건의 조건과 선택 결과를 표시합니다.
        private void OpenRecruitmentEventModal(WIPendingRecruitmentEvent pendingEvent)
        {
            WIRecruitmentEventDefinition definition = database.GetRecruitmentEvent(pendingEvent.EventId);
            WIHeroDefinition candidate = database.GetHero(pendingEvent.CandidateHeroId);
            if (definition == null || candidate == null) return;
            VisualElement panel = CreateModal(definition.Title.Get(database.UseEnglish));
            panel.Add(new Label($"영입 대상 · {candidate.DisplayName.Get(database.UseEnglish)}"));
            panel.Add(new Label(definition.Description.Get(database.UseEnglish)));
            for (int index = 0; index < definition.Choices.Count; index += 1)
            {
                int selectedIndex = index;
                WIRecruitmentEventChoiceDefinition choice = definition.Choices[index];
                Button button = new Button(() => ResolveRecruitmentEventChoice(pendingEvent, selectedIndex));
                button.text = $"{choice.Label.Get(database.UseEnglish)} · {GetRecruitmentChoiceCondition(choice)} · {choice.ResultDescription.Get(database.UseEnglish)}";
                button.SetEnabled(WIAdministrationTurnSystem.CanChooseRecruitmentEventOption(database, state, pendingEvent, choice));
                panel.Add(button);
            }
        }

        // 영입 요구 선택 결과를 적용하고 화면을 갱신합니다.
        private void ResolveRecruitmentEventChoice(WIPendingRecruitmentEvent pendingEvent, int choiceIndex)
        {
            if (WIAdministrationTurnSystem.ResolveRecruitmentEvent(
                    database, state, pendingEvent, choiceIndex, state.LastMonthlyReport))
            {
                CloseModal();
                RefreshAll();
            }
        }

        // 영입 사건 선택지의 잠금 조건을 짧은 문장으로 반환합니다.
        private string GetRecruitmentChoiceCondition(WIRecruitmentEventChoiceDefinition choice)
        {
            List<string> conditions = new List<string>();
            if (choice.RequiredReputation > 0) conditions.Add($"명성 {choice.RequiredReputation}");
            if (choice.RequiredMerit > 0) conditions.Add($"공훈 {choice.RequiredMerit}");
            if (choice.RequiredFactionCastleCount > 0) conditions.Add($"영토 {choice.RequiredFactionCastleCount}성");
            if (choice.RequiresNegotiator) conditions.Add("교섭가 특기");
            return conditions.Count == 0 ? "조건 없음" : string.Join(" · ", conditions);
        }

        // 관계 사건의 인물과 데이터 기반 선택지·예상 결과를 표시합니다.
        private void OpenRelationshipEventModal(WIPendingRelationshipEvent pendingEvent)
        {
            WIRelationshipEventDefinition definition = database.GetRelationshipEvent(pendingEvent.EventId);
            if (definition == null) return;
            WIHeroDefinition first = database.GetHero(pendingEvent.FirstHeroId);
            WIHeroDefinition second = database.GetHero(pendingEvent.SecondHeroId);
            VisualElement panel = CreateModal(definition.Title.Get(database.UseEnglish));
            panel.Add(new Label($"{first.DisplayName.Get(database.UseEnglish)} · {second.DisplayName.Get(database.UseEnglish)}"));
            panel.Add(new Label(definition.Description.Get(database.UseEnglish)));
            for (int index = 0; index < definition.Choices.Count; index += 1)
            {
                int selectedIndex = index;
                WIRelationshipEventChoiceDefinition choice = definition.Choices[index];
                Button choiceButton = new Button(() => ResolveRelationshipEventChoice(pendingEvent, selectedIndex));
                choiceButton.text = $"{choice.Label.Get(database.UseEnglish)} · {choice.ResultDescription.Get(database.UseEnglish)}";
                panel.Add(choiceButton);
            }
        }

        // 선택한 관계 사건 결과를 적용하고 화면을 갱신합니다.
        private void ResolveRelationshipEventChoice(WIPendingRelationshipEvent pendingEvent, int choiceIndex)
        {
            if (WIAdministrationTurnSystem.ResolveRelationshipEvent(
                    database, state, pendingEvent, choiceIndex, state.LastMonthlyReport))
            {
                CloseModal();
                RefreshAll();
            }
        }

        // 사업에서 발생한 사건의 안전, 과감, 영웅 선택지를 표시합니다.
        private void OpenProjectEventModal(WIPendingProjectEvent pendingEvent)
        {
            VisualElement panel = CreateModal(pendingEvent.Title);
            Button safe = new Button(() => ResolveProjectEvent(pendingEvent, 2, false));
            safe.text = "안전한 선택 · 관련 성 수치 +2";
            panel.Add(safe);
            Button bold = new Button(() => ResolveProjectEvent(pendingEvent, 4, true));
            bold.text = "과감한 선택 · 관련 성 수치 +4 / 질서 -2";
            panel.Add(bold);
            if (pendingEvent.HeroChoiceAvailable)
            {
                WIHeroDefinition hero = database.GetHero(pendingEvent.HeroId);
                Button heroChoice = new Button(() => ResolveProjectEvent(pendingEvent, 5, false));
                heroChoice.text = $"영웅 선택지 · {hero.DisplayName.Get(database.UseEnglish)}의 특기로 +5";
                panel.Add(heroChoice);
            }
        }

        // 사건 선택 결과를 성 수치에 적용하고 대기 목록에서 제거합니다.
        private void ResolveProjectEvent(WIPendingProjectEvent pendingEvent, int gain, bool boldRisk)
        {
            if (WIAdministrationTurnSystem.ResolveProjectEvent(
                    state, pendingEvent, gain, boldRisk, state.LastMonthlyReport))
            {
                CloseModal();
                RefreshAll();
            }
        }

        // 대성공으로 생성된 영웅의 흔적을 성에 기록하거나 교체 대상을 선택합니다.
        private void ConfirmLegacyChoice(WIPendingLegacyChoice pendingChoice)
        {
            WICastleRuntimeState castle = state.GetCastle(pendingChoice.CastleId);
            if (castle.HeroLegacies.Count < 2)
            {
                WIAdministrationTurnSystem.InstallHeroLegacy(state, castle, pendingChoice);
                CloseModal();
                RefreshAll();
                return;
            }

            VisualElement panel = CreateModal("교체할 영웅의 흔적 선택");
            panel.Add(new Label($"새 흔적 · {pendingChoice.Legacy.DisplayName} · {GetProjectDisplayName(pendingChoice.Legacy.ProjectType)} +{pendingChoice.Legacy.Bonus}"));
            panel.Add(new Label(pendingChoice.Legacy.Description));
            foreach (WIHeroLegacyState oldLegacy in new List<WIHeroLegacyState>(castle.HeroLegacies))
            {
                Button replace = new Button(() =>
                {
                    WIAdministrationTurnSystem.InstallHeroLegacy(state, castle, pendingChoice, oldLegacy);
                    CloseModal();
                    RefreshAll();
                });
                replace.text = $"교체 · {oldLegacy.DisplayName} · {GetProjectDisplayName(oldLegacy.ProjectType)} +{oldLegacy.Bonus} → 기념 기록";
                panel.Add(replace);
            }
        }

        // 진영 방침의 UI 표시명을 반환합니다.
        private string GetFactionPolicyDisplayName(WIFactionPolicy policy)
        {
            string[] names = { "부국", "개발", "안정", "수비", "원정", "인재" };
            return names[(int)policy];
        }

        // 진영 방침의 핵심 효과 설명을 반환합니다.
        private string GetFactionPolicyDescription(WIFactionPolicy policy)
        {
            string[] descriptions =
            {
                "번영 사업 강화", "기술 사업 강화", "질서 사업 강화",
                "방어·회복 강화", "훈련과 원정 준비 강화", "탐색·영입 강화"
            };
            return descriptions[(int)policy];
        }

        // 영지관 운영 방침의 UI 표시명을 반환합니다.
        private string GetGovernorPolicyDisplayName(WIGovernorPolicy policy)
        {
            string[] names = { "균형", "번영", "연구", "전선", "인재" };
            return names[(int)policy];
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using ProjectWI.Systems;

namespace ProjectWI.Administration
{
    public partial class WIAdministrationUIController
    {
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
                Progress = $"{progress} / {objective.TargetValue}",
                RewardGold = $"G {objective.RewardGold}",
                RewardMana = $"M {objective.RewardMana}",
                RewardInfluence = $"I {objective.RewardInfluence}",
                ProgressNormalized = objective.TargetValue <= 0 ? 0f : UnityEngine.Mathf.Clamp01((float)progress / objective.TargetValue)
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

    }
}


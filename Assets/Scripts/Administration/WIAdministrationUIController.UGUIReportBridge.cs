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
                body.AppendLine($"규모 · {database.GetCastleSizeName(selectedCastle.CastleSize)}");
                body.AppendLine($"지형 · {castle.TerrainTrait.Get(database.UseEnglish)}");
                body.AppendLine($"특산 · {castle.Specialty.Get(database.UseEnglish)}");
                body.AppendLine($"전문 분야 효과 · {GetSpecialtyEffectDescription(castle)}");
                body.AppendLine();
                body.AppendLine($"번영 {selectedCastle.Prosperity} · 기술 {selectedCastle.Technology}");
                body.AppendLine($"질서 {selectedCastle.Stability} · 방어 {selectedCastle.Defense}");
                body.AppendLine($"주둔 인물 {WIAdministrationTurnSystem.GetCastleResidentHeroIds(state, selectedCastle).Count}/{selectedCastle.GetHeroSlotCount()}");
                body.AppendLine($"특화 시설 {selectedCastle.SpecialFacilityIds.Count}/{selectedCastle.GetSpecialFacilitySlotCount()}");
                if (selectedCastle.OccupationUnrestMonths > 0)
                {
                    body.AppendLine();
                    body.AppendLine(string.Format(database.GetText("UI_ADMIN_OCCUPATION_PROGRESS"),
                        selectedCastle.OccupationUnrestMonths, database.Automation.OccupationStabilityGain));
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
            System.Collections.Generic.HashSet<string> characterIds = new System.Collections.Generic.HashSet<string>();
            foreach (WICastleRuntimeState castle in state.Castles.FindAll(item => item.FactionId == state.PlayerFactionId))
            {
                characterIds.UnionWith(castle.HeroIds);
            }
            foreach (WIArmyState army in state.Armies.FindAll(item => item.FactionId == state.PlayerFactionId))
            {
                characterIds.UnionWith(army.Members.ConvertAll(member => member.HeroId));
            }
            System.Collections.Generic.List<WICastleRuntimeState> playerCastles = state.Castles.FindAll(
                item => item.FactionId == state.PlayerFactionId);
            WIFactionRuntimeState faction = state.GetPlayerFactionState();
            int year = ((System.Math.Max(1, state.Turn) - 1) / 12) + 1;
            int month = ((System.Math.Max(1, state.Turn) - 1) % 12) + 1;
            snapshot = new WIAdministrationMonthlyReportSnapshot
            {
                Title = $"{year}년 {month:00}월 월간 보고",
                TerritorySummary = $"영지 수  {playerCastles.Count}",
                CharacterSummary = $"인물  {characterIds.Count}",
                ArmySummary = $"전투단  {state.Armies.FindAll(item => item.FactionId == state.PlayerFactionId && item.IsOperational).Count}",
                StabilitySummary = $"평균 질서  {(playerCastles.Count == 0 ? 0 : (int)playerCastles.Average(item => item.Stability))}",
                ResearchSummary = string.IsNullOrEmpty(faction?.ActiveResearchId) ? "연구  대기" : "연구  진행 중",
                GoldSummary = $"금화\n{FormatSignedReportValue(report.GoldGained - report.GoldSpent)}",
                ManaSummary = $"마나\n{FormatSignedReportValue(report.ManaGained)}",
                InfluenceSummary = $"영향력\n{FormatSignedReportValue(report.InfluenceGained)}"
            };
            snapshot.Operations.AddRange(report.DelegationReports.Take(4));
            snapshot.News.AddRange(report.News.Take(2));
            if (snapshot.News.Count < 2 && report.AIReasonReports != null)
            {
                snapshot.News.AddRange(report.AIReasonReports.Take(2 - snapshot.News.Count));
            }
            for (int index = 0; index < state.PendingProjectEvents.Count; index += 1)
                snapshot.Actions.Add(CreateReportAction(WIAdministrationReportActionType.ProjectEvent,
                    index.ToString(), "영지", "선택 사건 · " + state.PendingProjectEvents[index].Title,
                    "사업의 성과 처리 방식을 결정합니다.", false));
            for (int index = 0; index < state.PendingRelationshipEvents.Count; index += 1)
            {
                WIRelationshipEventDefinition definition = database.GetRelationshipEvent(state.PendingRelationshipEvents[index].EventId);
                if (definition != null) snapshot.Actions.Add(CreateReportAction(WIAdministrationReportActionType.RelationshipEvent,
                    index.ToString(), "인물", "관계 사건 · " + definition.Title.Get(database.UseEnglish),
                    "두 인물의 관계와 공훈이 달라집니다.", false));
            }
            for (int index = 0; index < state.PendingRegionalEvents.Count; index += 1)
            {
                WIRegionalEventDefinition definition = database.GetRegionalEvent(state.PendingRegionalEvents[index].EventId);
                if (definition != null) snapshot.Actions.Add(CreateReportAction(WIAdministrationReportActionType.RegionalEvent,
                    index.ToString(), "영지", "지역 사건 · " + definition.Title.Get(database.UseEnglish),
                    "자원과 지역 상태에 영향을 줍니다.", false));
            }
            for (int index = 0; index < state.PendingOccupationEvents.Count; index += 1)
            {
                WICastleDefinition castle = database.GetCastle(state.PendingOccupationEvents[index].CastleId);
                snapshot.Actions.Add(CreateReportAction(WIAdministrationReportActionType.OccupationEvent,
                    index.ToString(), "군사", "점령 통치 · " + (castle?.DisplayName.Get(database.UseEnglish) ?? state.PendingOccupationEvents[index].CastleId),
                    "새 점령지의 질서와 통치 비용을 결정합니다.", true));
            }
            for (int index = 0; index < state.PendingRecruitmentEvents.Count; index += 1)
            {
                WIRecruitmentEventDefinition definition = database.GetRecruitmentEvent(state.PendingRecruitmentEvents[index].EventId);
                if (definition != null) snapshot.Actions.Add(CreateReportAction(WIAdministrationReportActionType.RecruitmentEvent,
                    index.ToString(), "인물", "영입 요구 · " + definition.Title.Get(database.UseEnglish),
                    "요구를 검토하고 합류 여부를 결정합니다.", false));
            }
            for (int index = 0; index < state.PendingLegacyChoices.Count; index += 1)
                snapshot.Actions.Add(CreateReportAction(WIAdministrationReportActionType.LegacyChoice,
                    index.ToString(), "인물", "영웅의 흔적 · " + state.PendingLegacyChoices[index].Legacy.DisplayName,
                    "새 흔적을 기록하거나 기존 흔적을 교체합니다.", false));
            foreach (WIBattleSessionState battle in state.BattleSessions.FindAll(item => item.PlayerInvolved && item.Status == WIBattleSessionStatus.Pending))
            {
                WICastleDefinition castle = database.GetCastle(battle.CastleId);
                snapshot.Actions.Add(CreateReportAction(WIAdministrationReportActionType.Battle,
                    battle.SessionId, "군사", $"전투 발생 · {castle?.DisplayName.Get(database.UseEnglish) ?? battle.CastleId}",
                    $"아군 {battle.AttackerPowerSnapshot} / 적군 {battle.DefenderPowerSnapshot}", true));
            }
            return true;
        }

        // 월간 자원 증감을 양수와 음수 모두 자연스러운 부호로 표시합니다.
        private static string FormatSignedReportValue(int value)
        {
            return value > 0 ? $"+{value}" : value.ToString();
        }
        // 월간 보고의 결정 카드를 공통 형식으로 생성합니다.
        private static WIAdministrationReportActionSnapshot CreateReportAction(
            WIAdministrationReportActionType type, string id, string category,
            string caption, string detail, bool danger)
        {
            return new WIAdministrationReportActionSnapshot
            {
                Type = type,
                Id = id,
                Category = category,
                Caption = caption,
                Detail = detail,
                Danger = danger
            };
        }

        // 월간 보고에서 선택한 사건은 기존 모달로, 전투는 기존 런타임 서비스로 전달합니다.
        public void ExecuteUGUIReportAction(WIAdministrationReportActionType type, string id)
        {
            if (type == WIAdministrationReportActionType.Battle)
            {
                WICampaignRuntimeService.Instance?.StartBattle(id);
                return;
            }
            RaiseUGUIScreenRequest<WIAdministrationEventChoiceUGUIController, WIAdministrationReportActionType, string>(() => UGUIEventChoiceRequested, type, id);
        }

        // QA에서 림가르드 영지와 UGUI 중점 사업 모달을 바로 표시합니다.
        public void OpenFocusProjectUGUIForQA()
        {
            OpenCastlePreviewForQA();
            RaiseUGUIScreenRequest<WIAdministrationFocusProjectUGUIController>(() => UGUIFocusProjectRequested);
        }

        // QA에서 림가르드 영지와 UGUI 영웅 배치 모달을 바로 표시합니다.
        public void OpenHeroAssignmentUGUIForQA()
        {
            OpenCastlePreviewForQA();
            RaiseUGUIScreenRequest<WIAdministrationHeroAssignmentUGUIController>(() => UGUIHeroAssignmentRequested);
        }

        // QA에서 림가르드 영지와 UGUI 인재 활동 모달을 바로 표시합니다.
        public void OpenCharacterActivityUGUIForQA()
        {
            OpenCastlePreviewForQA();
            OpenUGUICharacterActivity(false);
        }

        // QA에서 림가르드 영지의 특화 시설 선택 권한과 UGUI 모달을 바로 표시합니다.
        public void OpenSpecialFacilityUGUIForQA()
        {
            OpenCastlePreviewForQA();
            selectedCastle.PendingSpecialFacilityChoice = true;
            RaiseUGUIScreenRequest<WIAdministrationSpecialFacilityUGUIController>(() => UGUISpecialFacilityRequested);
        }

        // QA에서 림가르드 영지의 기본 시설 UGUI를 바로 표시합니다.
        public void OpenBasicFacilityUGUIForQA()
        {
            OpenCastlePreviewForQA();
            RaiseUGUIScreenRequest<WIAdministrationBasicFacilityUGUIController>(() => UGUIBasicFacilityRequested);
        }

        // QA에서 림가르드 영지의 영지관 위임 UGUI를 바로 표시합니다.
        public void OpenDelegationUGUIForQA()
        {
            OpenCastlePreviewForQA();
            RaiseUGUIScreenRequest<WIAdministrationDelegationUGUIController>(() => UGUIDelegationRequested);
        }

        // QA에서 림가르드 영지의 원정 UGUI를 바로 표시합니다.
        public void OpenMarchUGUIForQA()
        {
            OpenCastlePreviewForQA();
            RaiseUGUIScreenRequest<WIAdministrationMarchUGUIController>(() => UGUIMarchRequested);
        }

        // QA에서 림가르드 영지의 성 상세 기록 UGUI를 바로 표시합니다.
        public void OpenCastleRecordUGUIForQA()
        {
            OpenCastlePreviewForQA();
            RaiseUGUIScreenRequest<WIAdministrationCastleRecordUGUIController>(() => UGUICastleRecordRequested);
        }

        // QA에서 현재 캠페인 목표 UGUI를 바로 표시합니다.
        public void OpenObjectiveUGUIForQA()
        {
            OpenCastlePreviewForQA();
            RaiseUGUIScreenRequest<WIAdministrationObjectiveUGUIController>(() => UGUIObjectiveRequested);
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
                state.LastMonthlyReport.DelegationReports.Add("[위임 결과] 림가르드 · 번영 사업 · 예상 +5 / 실제 +6 · 비용 100G");
                state.LastMonthlyReport.AIReasonReports.Add("적대 진영은 접경 방어를 우선하고 전투단을 재편성했습니다.");
                state.LastMonthlyReport.News.Add("림가르드의 시장이 활기를 되찾아 주민들의 왕래가 늘었습니다.");
                state.LastMonthlyReport.News.Add("북부 국경에서 적 진영의 정찰 움직임이 보고되었습니다.");
            }
            RaiseUGUIScreenRequest<WIAdministrationMonthlyReportUGUIController>(() => UGUIMonthlyReportRequested);
        }

        // UGUI 하단 명령 버튼을 기존 게임 기능에 연결합니다.
        public void ExecuteUGUIShortcut(WIAdministrationShortcutAction action)
        {
            switch (action)
            {
                case WIAdministrationShortcutAction.Military:
                    RaiseUGUIScreenRequest<WIAdministrationMilitaryUGUIController>(() => UGUIMilitaryRequested);
                    return;
                case WIAdministrationShortcutAction.Heroes:
                    RaiseUGUIScreenRequest<WIAdministrationHeroesUGUIController>(() => UGUIHeroesRequested);
                    return;
                case WIAdministrationShortcutAction.Diplomacy:
                    RaiseUGUIScreenRequest<WIAdministrationDiplomacyUGUIController>(() => UGUIDiplomacyRequested);
                    return;
                case WIAdministrationShortcutAction.Scheme:
                    RaiseUGUIScreenRequest<WIAdministrationSchemeUGUIController>(() => UGUISchemeRequested);
                    return;
                case WIAdministrationShortcutAction.Research:
                    RaiseUGUIScreenRequest<WIAdministrationResearchUGUIController>(() => UGUIResearchRequested);
                    return;
                case WIAdministrationShortcutAction.Faction:
                    RaiseUGUIScreenRequest<WIAdministrationFactionUGUIController>(() => UGUIFactionRequested);
                    return;
                case WIAdministrationShortcutAction.Council:
                    RaiseUGUIScreenRequest<WIAdministrationCouncilUGUIController>(() => UGUICouncilRequested);
                    return;
                case WIAdministrationShortcutAction.MonthlyReport:
                    RaiseUGUIScreenRequest<WIAdministrationMonthlyReportUGUIController>(() => UGUIMonthlyReportRequested);
                    return;
                case WIAdministrationShortcutAction.EndTurn:
                    BeginTurn();
                    return;
            }
            SuspendUGUIForLegacyModal();
        }

    }
}

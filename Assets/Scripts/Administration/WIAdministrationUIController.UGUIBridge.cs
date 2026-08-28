using System;
using System.Collections.Generic;
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
        // 캠페인 시작 화면이 열려 있는 동안 월드와 영지 UGUI를 함께 숨깁니다.
        private bool uguiCampaignTitleVisible = true;

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
                WorldVisible = uguiWorldVisible
                    && uguiSuspendedForLegacyModal == false
                    && uguiCampaignTitleVisible == false,
                FactionName = playerFaction == null ? database.GetText("UI_GAME_TITLE") : playerFaction.DisplayName.Get(database.UseEnglish),
                Date = $"{state.Year}년 {state.Month:00}월",
                TurnDescription = $"진영 방침: {GetFactionPolicyDisplayName(state.FactionPolicy)} · {state.Month:00}월 명령을 검토하십시오.",
                Gold = $"금화  {FormatHudNumber(state.Gold, database.UseEnglish)}  <size=75%><color=#AEB4B8>(+{FormatHudNumber(forecast.GoldGained, database.UseEnglish)}/월)</color></size>",
                Mana = $"마나  {FormatHudNumber(state.ManaCrystal, database.UseEnglish)}  <size=75%><color=#AEB4B8>(+{FormatHudNumber(forecast.ManaGained, database.UseEnglish)}/월)</color></size>",
                Influence = $"영향력  {FormatHudNumber(state.Influence, database.UseEnglish)}  <size=75%><color=#AEB4B8>(+{FormatHudNumber(forecast.InfluenceGained, database.UseEnglish)}/월)</color></size>",
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
                EndTurnText = unresolvedBattleCount == 0 ? "다음 턴" : $"전투 해결 필요 · {unresolvedBattleCount}건"
            };
            if (castle != null)
            {
                WIHeroDefinition governor = database.GetHero(castle.GovernorHeroId);
                snapshot.CastleDetailTitles.AddRange(new[] { "영지관", "번영", "기술", "질서", "방어", "주둔 전투단" });
                snapshot.CastleDetailValues.Add(governor?.DisplayName.Get(database.UseEnglish) ?? "미배치");
                snapshot.CastleDetailValues.Add(castle.Prosperity.ToString());
                snapshot.CastleDetailValues.Add(castle.Technology.ToString());
                snapshot.CastleDetailValues.Add(castle.Stability.ToString());
                snapshot.CastleDetailValues.Add($"{castle.Defense}/100");
                snapshot.CastleDetailValues.Add($"{armyCount}개");
                foreach (string heroId in castle.HeroIds.Take(4))
                {
                    WIHeroDefinition hero = database.GetHero(heroId);
                    WICharacterRuntimeState character = state.GetCharacter(heroId);
                    snapshot.CastleHeroCards.Add(new WIAdministrationWorldHeroSnapshot
                    {
                        DisplayName = hero?.DisplayName.Get(database.UseEnglish) ?? heroId,
                        LevelText = $"Lv.{1 + (character?.Experience ?? 0) / 100}",
                        Portrait = hero?.Portrait
                    });
                }
            }
            foreach (WICastleDefinition definition in database.Castles)
            {
                WICastleRuntimeState castleState = state.GetCastle(definition.Id);
                WIFactionDefinition owner = castleState == null ? null : database.GetFaction(castleState.FactionId);
                snapshot.MapNodes.Add(new WIAdministrationMapNodeSnapshot
                {
                    CastleId = definition.Id,
                    DisplayName = definition.DisplayName.Get(database.UseEnglish),
                    FactionId = castleState?.FactionId ?? definition.FactionId,
                    CastleImage = definition.MapMarkerImage,
                    Position = castleState == null
                        ? new UnityEngine.Vector2(definition.NormalizedMapPosition.x, 1f - definition.NormalizedMapPosition.y)
                        : new UnityEngine.Vector2(castleState.NormalizedMapPosition.x, 1f - castleState.NormalizedMapPosition.y),
                    Selected = selectedCastle != null && selectedCastle.CastleId == definition.Id,
                    Tooltip = $"{owner?.DisplayName.Get(database.UseEnglish) ?? castleState?.FactionId}\n{definition.DisplayName.Get(database.UseEnglish)}"
                });
            }
            HashSet<string> visitedConnections = new HashSet<string>();
            foreach (WICastleDefinition definition in database.Castles)
            {
                WICastleRuntimeState originState = state.GetCastle(definition.Id);
                foreach (string adjacentId in originState.AdjacentCastleIds)
                {
                    string connectionId = string.CompareOrdinal(definition.Id, adjacentId) < 0
                        ? $"{definition.Id}|{adjacentId}"
                        : $"{adjacentId}|{definition.Id}";
                    if (visitedConnections.Add(connectionId) == false)
                    {
                        continue;
                    }

                    WICastleDefinition adjacent = database.GetCastle(adjacentId);
                    WICastleRuntimeState adjacentState = state.GetCastle(adjacentId);
                    if (originState == null || adjacent == null || adjacentState == null)
                    {
                        continue;
                    }

                    bool frontline = originState.FactionId != adjacentState.FactionId;
                    bool selectedRoute = selectedCastle != null &&
                        (selectedCastle.CastleId == definition.Id || selectedCastle.CastleId == adjacentId);
                    WIFactionDefinition owner = database.GetFaction(originState.FactionId);
                    UnityEngine.Color ownerColor = owner?.Color ?? UnityEngine.Color.gray;
                    UnityEngine.Color32 routeColor = selectedRoute
                        ? new UnityEngine.Color32(184, 226, 255, 245)
                        : frontline
                            ? new UnityEngine.Color32(190, 58, 52, 165)
                            : new UnityEngine.Color32(
                                (byte)(ownerColor.r * 170f), (byte)(ownerColor.g * 170f),
                                (byte)(ownerColor.b * 170f), 92);
                    snapshot.MapConnections.Add(new WIAdministrationMapConnectionSnapshot
                    {
                        Start = new UnityEngine.Vector2(originState.NormalizedMapPosition.x,
                            1f - originState.NormalizedMapPosition.y),
                        End = new UnityEngine.Vector2(adjacentState.NormalizedMapPosition.x,
                            1f - adjacentState.NormalizedMapPosition.y),
                        Color = routeColor,
                        Frontline = frontline,
                        Selected = selectedRoute
                    });
                }
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
            WIFactionDefinition playerFaction = database.Factions.FirstOrDefault(faction => faction.PlayerFaction == true);
            WITurnSummary forecast = WIAdministrationTurnSystem.GetFactionMonthlyIncome(database, state, state.PlayerFactionId);
            bool manageable = WIAdministrationTurnSystem.CanPlayerManageCastle(state, selectedCastle);
            bool detailed = WIInformationVisibility.CanViewCastleDetails(state, state.PlayerFactionId, selectedCastle);
            WIHeroDefinition governor = detailed ? database.GetHero(selectedCastle.GovernorHeroId) : null;
            string ownerName = owner == null ? selectedCastle.FactionId : owner.DisplayName.Get(database.UseEnglish);
            int unresolvedBattleCount = state.BattleSessions.Count(session =>
                session.PlayerInvolved && session.Status != WIBattleSessionStatus.Resolved);

            snapshot = new WIAdministrationTerritorySnapshot
            {
                Visible = uguiWorldVisible == false
                    && uguiSuspendedForLegacyModal == false
                    && uguiCampaignTitleVisible == false,
                Manageable = manageable,
                CanChooseSpecialFacility = manageable && selectedCastle.PendingSpecialFacilityChoice,
                CanEndTurn = unresolvedBattleCount == 0,
                EndTurnText = unresolvedBattleCount == 0
                    ? "다음 턴"
                    : $"전투 해결 필요 · {unresolvedBattleCount}건",
                FactionName = playerFaction == null ? database.GetText("UI_GAME_TITLE") : playerFaction.DisplayName.Get(database.UseEnglish),
                Date = $"{state.Year}년 {state.Month:00}월",
                Gold = $"금화  {FormatHudNumber(state.Gold, database.UseEnglish)}  <size=75%><color=#AEB4B8>(+{FormatHudNumber(forecast.GoldGained, database.UseEnglish)}/월)</color></size>",
                Mana = $"마나  {FormatHudNumber(state.ManaCrystal, database.UseEnglish)}  <size=75%><color=#AEB4B8>(+{FormatHudNumber(forecast.ManaGained, database.UseEnglish)}/월)</color></size>",
                Influence = $"영향력  {FormatHudNumber(state.Influence, database.UseEnglish)}  <size=75%><color=#AEB4B8>(+{FormatHudNumber(forecast.InfluenceGained, database.UseEnglish)}/월)</color></size>",
                CastleTitle = definition?.DisplayName.Get(database.UseEnglish) ?? selectedCastle.CastleId,
                CastleInfo = detailed
                    ? $"{ownerName} · {selectedCastle.CastleSize} · {definition?.TerrainTrait.Get(database.UseEnglish)} · 인물 {selectedCastle.HeroIds.Count}/{selectedCastle.GetHeroSlotCount()}"
                    : $"{ownerName} 소유 · 상세 정보 미확보",
                CastleImage = definition?.CastleImage,
                GovernorPortrait = governor?.Portrait,
                GovernorName = governor == null ? "영지관 미배치" : governor.DisplayName.Get(database.UseEnglish),
                Prosperity = detailed ? $"번영 {selectedCastle.Prosperity} · {GetCastleStatusName(selectedCastle.Prosperity)}" : "번영 ??",
                Technology = detailed ? $"기술 {selectedCastle.Technology} · {GetCastleStatusName(selectedCastle.Technology)}" : "기술 ??",
                Stability = detailed ? $"질서 {selectedCastle.Stability} · {GetCastleStatusName(selectedCastle.Stability)}" : "질서 ??",
                Defense = detailed ? $"방어 {selectedCastle.Defense} · {GetCastleStatusName(selectedCastle.Defense)}" : "방어 ??",
                Income = detailed
                    ? $"금화 {forecast.GoldGained}  ·  마나 {forecast.ManaGained}  ·  영향력 {forecast.InfluenceGained}"
                    : "수입 정보 미확보",
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

        // 캠페인 시작 화면 표시 상태를 변경하고 월드 및 영지 UGUI를 즉시 갱신합니다.
        public void SetUGUICampaignTitleVisibility(bool visible)
        {
            if (uguiCampaignTitleVisible == visible)
            {
                return;
            }

            uguiCampaignTitleVisible = visible;
            NotifyUGUIWorldChanged();
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

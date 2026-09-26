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
        // 성 화면의 선술집 바로가기는 시설 목록을 거치지 않습니다.
        public event Action UGUITavernRequested;
        // 선술집의 인재실과 월간 의뢰를 직접 엽니다.
        public void OpenUGUITavern()
        {
            RaiseUGUIScreenRequest<WIAdministrationBasicFacilityUGUIController>(() => UGUITavernRequested);
        }
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
        private bool uguiTalentOfficeOnly;
        // 캠페인 시작 화면이 열려 있는 동안 월드와 영지 UGUI를 함께 숨깁니다.
        private bool uguiCampaignTitleVisible = true;

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
                    ? $"{ownerName} · {database.GetCastleSizeName(selectedCastle.CastleSize)} · {definition?.TerrainTrait.Get(database.UseEnglish)} · 인물 {WIAdministrationTurnSystem.GetCastleResidentHeroIds(state, selectedCastle).Count}/{selectedCastle.GetHeroSlotCount()}"
                    : $"{ownerName} 소유 · 상세 정보 미확보",
                CastleImage = definition?.CastleImage,
                GovernorPortrait = governor?.Portrait,
                GovernorName = governor == null ? database.GetText("UI_ADMIN_GOVERNOR_OPTIONAL") : governor.DisplayName.Get(database.UseEnglish),
                Prosperity = detailed ? $"번영 {selectedCastle.Prosperity} · {GetCastleStatusName(selectedCastle.Prosperity)}" : "번영 ??",
                Technology = detailed ? $"기술 {selectedCastle.Technology} · {GetCastleStatusName(selectedCastle.Technology)}" : "기술 ??",
                Stability = detailed ? $"질서 {selectedCastle.Stability} · {GetCastleStatusName(selectedCastle.Stability)}" : "질서 ??",
                Defense = detailed ? $"방어 {selectedCastle.Defense} · {GetCastleStatusName(selectedCastle.Defense)}" : "방어 ??",
                Income = detailed
                    ? $"금화 {forecast.GoldGained}  ·  마나 {forecast.ManaGained}  ·  영향력 {forecast.InfluenceGained}"
                    : "수입 정보 미확보",
                ProjectStatus = detailed
                    ? selectedCastle.DelegatedToGovernor && selectedCastle.ActiveProject == null
                        ? string.Format(database.GetText("UI_ADMIN_OPERATION_STATUS"),
                            GetGovernorPolicyDisplayName(selectedCastle.GovernorPolicy), selectedCastle.GovernorMonthlyBudget)
                        : GetProjectStatusText(selectedCastle.ActiveProject)
                    : "첩보 조사를 성공하면 상세 정보가 공개됩니다."
            };

            var residentHeroIds = WIAdministrationTurnSystem.GetCastleResidentHeroIds(state, selectedCastle);
            for (int index = 0; index < 8; index += 1)
            {
                bool visible = detailed && index < selectedCastle.GetHeroSlotCount();
                bool occupied = visible && index < WIAdministrationTurnSystem.GetCastleResidentHeroIds(state, selectedCastle).Count;
                WIHeroDefinition hero = occupied ? database.GetHero(residentHeroIds[index]) : null;
                WICharacterRuntimeState character = occupied ? state.GetCharacter(residentHeroIds[index]) : null;
                string activity = character == null || character.Activity == WICharacterActivityType.None
                    ? "대기"
                    : character.Activity.ToString();
                snapshot.HeroSlots.Add(new WIAdministrationSlotSnapshot
                {
                    Visible = visible,
                    Occupied = occupied,
                    Caption = occupied
                        ? hero == null ? residentHeroIds[index] : $"{hero.DisplayName.Get(database.UseEnglish)}\n{activity}"
                        : database.GetText("UI_EMPTY_HERO"),
                    Image = hero?.Portrait
                });
            }

            for (int index = 0; index < 2; index += 1)
            {
                // 시안처럼 두 칸을 항상 보여 주고, 규모상 아직 없는 칸은 잠김 상태로 표시합니다.
                bool visible = detailed;
                bool unlocked = visible && index < selectedCastle.GetSpecialFacilitySlotCount();
                bool occupied = unlocked && index < selectedCastle.SpecialFacilityIds.Count;
                WISpecialFacilityDefinition facility = occupied
                    ? database.GetSpecialFacility(selectedCastle.SpecialFacilityIds[index])
                    : null;
                string emptyCaption = unlocked
                    ? database.GetText("UI_ADMIN_FACILITY_SLOT_EMPTY")
                    : database.GetText("UI_ADMIN_FACILITY_SLOT_LOCKED");
                snapshot.FacilitySlots.Add(new WIAdministrationSlotSnapshot
                {
                    Visible = visible,
                    Occupied = occupied,
                    Unlocked = unlocked,
                    ContentId = occupied ? selectedCastle.SpecialFacilityIds[index] : string.Empty,
                    Caption = occupied
                        ? facility == null ? selectedCastle.SpecialFacilityIds[index] : facility.DisplayName.Get(database.UseEnglish)
                        : emptyCaption,
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
            RaiseUGUIScreenRequest<WIAdministrationMonthlyReportUGUIController>(() => UGUIMonthlyReportRequested);
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
                    RaiseUGUIScreenRequest<WIAdministrationFocusProjectUGUIController>(() => UGUIFocusProjectRequested);
                    return;
                case WIAdministrationTerritoryCommand.AssignHero:
                    RaiseUGUIScreenRequest<WIAdministrationHeroAssignmentUGUIController>(() => UGUIHeroAssignmentRequested);
                    return;
                case WIAdministrationTerritoryCommand.CharacterActivity:
                case WIAdministrationTerritoryCommand.Military:
                    RaiseUGUIScreenRequest<WIAdministrationMilitaryUGUIController>(() => UGUIMilitaryRequested);
                    return;
                case WIAdministrationTerritoryCommand.ChooseSpecialFacility:
                    RaiseUGUIScreenRequest<WIAdministrationSpecialFacilityUGUIController>(() => UGUISpecialFacilityRequested);
                    return;
                case WIAdministrationTerritoryCommand.BasicFacility:
                    RaiseUGUIScreenRequest<WIAdministrationBasicFacilityUGUIController>(() => UGUIBasicFacilityRequested);
                    return;
                case WIAdministrationTerritoryCommand.Delegation:
                    RaiseUGUIScreenRequest<WIAdministrationDelegationUGUIController>(() => UGUIDelegationRequested);
                    return;
                case WIAdministrationTerritoryCommand.March:
                    RaiseUGUIScreenRequest<WIAdministrationMarchUGUIController>(() => UGUIMarchRequested);
                    return;
                case WIAdministrationTerritoryCommand.CastleRecord:
                    RaiseUGUIScreenRequest<WIAdministrationCastleRecordUGUIController>(() => UGUICastleRecordRequested);
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

        // 선택한 특화 시설 슬롯을 시설 종류에 대응하는 실제 게임 기능으로 연결합니다.
        public void ExecuteUGUIFacilitySlot(int index)
        {
            if (selectedCastle == null || index < 0)
            {
                return;
            }
            if (index >= selectedCastle.SpecialFacilityIds.Count)
            {
                RaiseUGUIScreenRequest<WIAdministrationSpecialFacilityUGUIController>(() => UGUISpecialFacilityRequested);
                return;
            }

            switch (selectedCastle.SpecialFacilityIds[index])
            {
                case "mage_tower":
                    RaiseUGUIScreenRequest<WIAdministrationResearchUGUIController>(() => UGUIResearchRequested);
                    break;
                case "knightly_order":
                    RaiseUGUIScreenRequest<WIAdministrationMilitaryUGUIController>(() => UGUIMilitaryRequested);
                    break;
                case "adventurers_guild":
                    RaiseUGUIScreenRequest<WIAdministrationBasicFacilityUGUIController>(() => UGUIBasicFacilityRequested);
                    break;
                case "sanctuary":
                    ShowUGUIMessage(database.GetText("UI_AUTO_RECOVERY_TITLE"), database.GetText("UI_AUTO_RECOVERY_HINT"));
                    break;
                case "spy_outpost":
                    RaiseUGUIScreenRequest<WIAdministrationSchemeUGUIController>(() => UGUISchemeRequested);
                    break;
                case "embassy":
                    RaiseUGUIScreenRequest<WIAdministrationDiplomacyUGUIController>(() => UGUIDiplomacyRequested);
                    break;
                case "grand_forge":
                case "grand_market":
                    RaiseUGUIScreenRequest<WIAdministrationCastleRecordUGUIController>(() => UGUICastleRecordRequested);
                    break;
                default:
                    RaiseUGUIScreenRequest<WIAdministrationCastleRecordUGUIController>(() => UGUICastleRecordRequested);
                    break;
            }
        }

        // 인재실만 열며 폐지된 일반 활동 요청은 군사 메뉴로 연결합니다.
        public void OpenUGUICharacterActivity(bool talentOfficeOnly)
        {
            if (talentOfficeOnly == false)
            {
                RaiseUGUIScreenRequest<WIAdministrationMilitaryUGUIController>(() => UGUIMilitaryRequested);
                return;
            }
            uguiTalentOfficeOnly = talentOfficeOnly;
            RaiseUGUIScreenRequest<WIAdministrationCharacterActivityUGUIController>(() => UGUICharacterActivityRequested);
        }

        // 현재 인재 활동 화면이 선술집 인재실 전용으로 요청되었는지 반환합니다.
        public bool IsUGUITalentOfficeMode()
        {
            return uguiTalentOfficeOnly;
        }
    }
}

using System.Linq;

namespace ProjectWI.Administration
{
    public partial class WIAdministrationUIController
    {
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

        // 선택 인물이 한 번의 명령으로 이동할 수 있는 모든 아군 성과 거리·목적지 슬롯을 UGUI에 제공합니다.
        public bool TryGetUGUICharacterTransferTargets(string actorHeroId,
            out WIAdministrationCharacterActivitySnapshot snapshot, out string error)
        {
            snapshot = new WIAdministrationCharacterActivitySnapshot();
            error = string.Empty;
            WICharacterRuntimeState actor = state.GetCharacter(actorHeroId);
            WICastleRuntimeState originState = state.Castles.FirstOrDefault(castle => castle.HeroIds.Contains(actorHeroId));
            if (actor == null || originState == null || state.IsCharacterBusy(actorHeroId))
            {
                error = "이동할 인물을 다시 선택하십시오.";
                return false;
            }
            foreach (WICastleRuntimeState target in state.Castles
                         .Where(castle => castle.FactionId == originState.FactionId &&
                                          castle.CastleId != originState.CastleId)
                         .OrderBy(castle => database.GetCastle(castle.CastleId)?.DisplayName.Get(database.UseEnglish)))
            {
                WICastleDefinition targetDefinition = database.GetCastle(target.CastleId);
                if (targetDefinition == null) continue;
                var route = WIAdministrationTurnSystem.GetCharacterTransferPath(
                    state, originState.CastleId, target.CastleId);
                if (route.Count == 0) continue;
                int reservedSlots = state.CharacterTransfers.Count(transfer => transfer.TargetCastleId == target.CastleId);
                int occupiedSlots = WIAdministrationTurnSystem.GetCastleResidentHeroIds(state, target).Count + reservedSlots;
                snapshot.Candidates.Add(new WIAdministrationCharacterActivityCandidateSnapshot
                {
                    HeroId = target.CastleId,
                    DisplayName = targetDefinition.DisplayName.Get(database.UseEnglish),
                    Summary = $"인물 슬롯 {occupiedSlots}/{target.GetHeroSlotCount()} · 이동 {route.Count}개월",
                    Interactable = occupiedSlots < target.GetHeroSlotCount()
                });
            }
            if (snapshot.Candidates.Count > 0) return true;
            error = "아군 영토 경로로 이동 가능한 성이 없습니다.";
            return false;
        }

        // 최종 목적지를 한 번 지정해 전체 경로 이동을 시작하고 영지 화면을 갱신합니다.
        public bool StartUGUICharacterTransfer(string actorHeroId, string targetCastleId, out string message)
        {
            message = string.Empty;
            if (WIAdministrationTurnSystem.StartCharacterTransfer(database, state, actorHeroId, targetCastleId) == false)
            {
                message = "이동할 수 없습니다. 임무, 영지관직, 아군 경로와 목적지 슬롯을 확인하십시오.";
                return false;
            }
            WICastleDefinition target = database.GetCastle(targetCastleId);
            SelectCastle(selectedCastle.CastleId);
            RefreshAll();
            WICharacterTransferState transfer = state.CharacterTransfers.FirstOrDefault(item => item.HeroId == actorHeroId);
            int months = transfer?.RemainingMonths ?? 1;
            message = $"{target?.DisplayName.Get(database.UseEnglish) ?? targetCastleId} 이동을 시작했습니다. {months}개월 후 도착합니다.";
            return true;
        }

    }
}

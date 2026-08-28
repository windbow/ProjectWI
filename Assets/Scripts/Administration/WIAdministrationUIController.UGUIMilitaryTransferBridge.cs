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

        // 선택 인물이 이동할 수 있는 같은 진영의 인접 성과 목적지 슬롯 상태를 UGUI에 제공합니다.
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
            foreach (string targetId in originState.AdjacentCastleIds)
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

    }
}


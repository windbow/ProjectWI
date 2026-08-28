using System.Linq;

namespace ProjectWI.Administration
{
    public partial class WIAdministrationUIController
    {
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
            WICastleRuntimeState origin = army == null ? null : state.GetCastle(army.CurrentCastleId);
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

    }
}


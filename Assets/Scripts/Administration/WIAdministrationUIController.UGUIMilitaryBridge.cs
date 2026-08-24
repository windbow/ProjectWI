using System;
using System.Linq;
using ProjectWI.Systems;

namespace ProjectWI.Administration
{
    public partial class WIAdministrationUIController
    {
        // 플레이 모드 QA에서 군사 UGUI를 즉시 엽니다.
        public void OpenMilitaryUGUIForQA()
        {
            OpenGlobalPreviewForQA();
            UGUIMilitaryRequested?.Invoke();
        }

        // 군사 UGUI의 현재 단계에 필요한 카드 스냅샷을 구성합니다.
        public bool TryGetUGUIMilitaryPanel(string mode, string context, int role,
            out WIAdministrationMilitarySnapshot snapshot, out string error)
        {
            snapshot = null;
            error = string.Empty;
            if (state == null || database == null)
            {
                error = "군사 정보를 불러올 수 없습니다.";
                return false;
            }
            snapshot = new WIAdministrationMilitarySnapshot();
            switch (mode)
            {
                case "overview": BuildMilitaryOverview(snapshot); break;
                case "battle": BuildBattleDetail(snapshot, context); break;
                case "army": BuildArmyDetail(snapshot, context); break;
                case "castles": BuildArmyCastles(snapshot); break;
                case "commanders": BuildArmyCommanders(snapshot, context); break;
                case "roles": BuildArmyRoles(snapshot, context); break;
                case "members": BuildArmyMembers(snapshot, context, (WIUnitRole)role); break;
                case "targets": BuildArmyTargets(snapshot, context); break;
                default: error = "알 수 없는 군사 화면입니다."; return false;
            }
            return true;
        }

        // 군사 UGUI 카드에서 요청한 실제 게임 상태 변경을 수행합니다.
        public bool ExecuteUGUIMilitaryAction(string action, string context, string id, int value,
            out string nextContext, out string error)
        {
            nextContext = string.Empty;
            error = string.Empty;
            WIArmyState army = state.Armies.FirstOrDefault(item => item.ArmyId == context);
            switch (action)
            {
                case "start-battle":
                    WICampaignRuntimeService service = WICampaignRuntimeService.Instance;
                    if (service == null || service.StartBattle(id) == false) error = "전투 씬을 시작할 수 없습니다.";
                    break;
                case "create":
                    WICastleRuntimeState castle = state.GetCastle(context);
                    WIArmyState created = WIAdministrationTurnSystem.CreateArmy(database, state, castle, id);
                    if (created == null) error = "전투단을 편성할 수 없습니다.";
                    else nextContext = created.ArmyId;
                    break;
                case "remove-member":
                    if (army == null || WIAdministrationTurnSystem.RemoveArmyMember(state, army, id) == false) error = "전투단원을 제외할 수 없습니다.";
                    break;
                case "add-member":
                    if (army == null || WIAdministrationTurnSystem.AddArmyMember(database, state, army, id, (WIUnitRole)value) == false) error = "전투단원을 추가할 수 없습니다.";
                    break;
                case "training":
                    if (WIAdministrationTurnSystem.ScheduleJointTraining(army) == false) error = "합동 훈련을 예약할 수 없습니다.";
                    break;
                case "disband":
                    if (WIAdministrationTurnSystem.DisbandArmy(state, army) == false) error = "전투단을 해산할 수 없습니다.";
                    break;
                case "march":
                    WICastleRuntimeState target = state.GetCastle(id);
                    if (army == null || WIAdministrationTurnSystem.BeginArmyMarch(database, state, army, id) == false) error = GetArmyMarchFailureMessage(army, target);
                    break;
                default: error = "지원하지 않는 군사 명령입니다."; break;
            }
            if (string.IsNullOrEmpty(error)) RefreshAll();
            return string.IsNullOrEmpty(error);
        }

        // 현재 전투와 플레이어 전투단 목록을 구성합니다.
        private void BuildMilitaryOverview(WIAdministrationMilitarySnapshot snapshot)
        {
            TryGetUGUIMilitarySnapshot(out WIAdministrationMilitarySnapshot source);
            snapshot.Title = "군사 · 전투단";
            snapshot.Summary = source.Summary;
            snapshot.Items.AddRange(source.Items);
        }

        // 선택한 전투 세션의 참가 진영과 전력을 표시합니다.
        private void BuildBattleDetail(WIAdministrationMilitarySnapshot snapshot, string sessionId)
        {
            WIBattleSessionState session = state.BattleSessions.FirstOrDefault(item => item.SessionId == sessionId);
            if (session == null) return;
            snapshot.Title = "전투 세션";
            snapshot.Summary = $"전장 · {database.GetCastle(session.CastleId).DisplayName.Get(database.UseEnglish)}\n" +
                $"공격 · {database.GetFaction(session.AttackerFactionId).DisplayName.Get(database.UseEnglish)} / 전력 {session.AttackerPowerSnapshot}\n" +
                $"수비 · {database.GetFaction(session.DefenderFactionId).DisplayName.Get(database.UseEnglish)} / 전력 {session.DefenderPowerSnapshot}";
            snapshot.Items.Add(new WIAdministrationMilitaryItemSnapshot
            {
                Kind = "start-battle", Id = session.SessionId, Title = "전투 시작",
                Description = $"상태 {session.Status} · 수비 전투단 {session.DefenderArmyIds.Count}개",
                Interactable = session.PlayerInvolved && session.Status == WIBattleSessionStatus.Pending
            });
        }

        // 선택한 전투단의 구성과 이용 가능한 명령을 표시합니다.
        private void BuildArmyDetail(WIAdministrationMilitarySnapshot snapshot, string armyId)
        {
            WIArmyState army = state.Armies.FirstOrDefault(item => item.ArmyId == armyId);
            if (army == null) return;
            snapshot.Title = army.DisplayName;
            string target = string.IsNullOrEmpty(army.StrategicTargetCastleId) ? "없음" : database.GetCastle(army.StrategicTargetCastleId).DisplayName.Get(database.UseEnglish);
            snapshot.Summary = $"위치 · {database.GetCastle(army.CurrentCastleId).DisplayName.Get(database.UseEnglish)} · 숙련 {army.Proficiency} · 보급 {army.Supply}\n임무 · {army.Mission} · 목표 {target}";
            foreach (WIArmyMemberState member in army.Members)
            {
                WIHeroDefinition hero = database.GetHero(member.HeroId);
                bool removable = member.Role != WIUnitRole.Commander && army.IsMoving == false && army.AwaitingBattle == false;
                snapshot.Items.Add(new WIAdministrationMilitaryItemSnapshot
                {
                    Kind = "remove-member", Id = member.HeroId,
                    Title = GetUnitRoleDisplayName(member.Role) + " · " + hero.DisplayName.Get(database.UseEnglish),
                    Description = removable ? "선택하면 전투단에서 제외" : "고정 구성원", Interactable = removable
                });
            }
            if (army.IsMoving || army.AwaitingBattle) return;
            snapshot.Items.Add(new WIAdministrationMilitaryItemSnapshot { Kind = "roles", Title = "전투단원 추가", Description = "역할과 대기 인물을 선택합니다." });
            snapshot.Items.Add(new WIAdministrationMilitaryItemSnapshot { Kind = "targets", Title = "이동 / 원정", Description = "인접 성을 목표로 지정합니다." });
            snapshot.Items.Add(new WIAdministrationMilitaryItemSnapshot { Kind = "training", Title = "합동 훈련", Description = army.JointTrainingScheduled ? "이미 다음 달 훈련이 예약되었습니다." : "다음 달 합동 훈련을 예약합니다.", Interactable = army.JointTrainingScheduled == false });
            snapshot.Items.Add(new WIAdministrationMilitaryItemSnapshot { Kind = "disband", Title = "전투단 해산", Description = "모든 구성원을 현재 성의 대기 상태로 돌립니다." });
        }

        // 새 전투단을 편성할 플레이어 성 목록을 구성합니다.
        private void BuildArmyCastles(WIAdministrationMilitarySnapshot snapshot)
        {
            snapshot.Title = "전투단 편성 · 성 선택";
            snapshot.Summary = "새 전투단을 주둔시킬 플레이어 성을 선택하십시오.";
            foreach (WICastleRuntimeState castle in state.Castles.Where(item => database.GetFaction(item.FactionId)?.PlayerFaction == true))
            {
                snapshot.Items.Add(new WIAdministrationMilitaryItemSnapshot { Kind = "castle", Id = castle.CastleId, Title = database.GetCastle(castle.CastleId).DisplayName.Get(database.UseEnglish), Description = $"대기 영웅 {castle.HeroIds.Count(id => state.IsCharacterBusy(id) == false)}명" });
            }
        }

        // 선택한 성에서 전투단 대장이 될 대기 영웅을 구성합니다.
        private void BuildArmyCommanders(WIAdministrationMilitarySnapshot snapshot, string castleId)
        {
            WICastleRuntimeState castle = state.GetCastle(castleId);
            snapshot.Title = "전투단 편성 · 대장 선택";
            snapshot.Summary = database.GetCastle(castleId).DisplayName.Get(database.UseEnglish);
            foreach (string heroId in castle.HeroIds.Where(id => state.IsCharacterBusy(id) == false))
            {
                WIHeroDefinition hero = database.GetHero(heroId);
                snapshot.Items.Add(new WIAdministrationMilitaryItemSnapshot { Kind = "create", Id = heroId, Title = hero.DisplayName.Get(database.UseEnglish), Description = $"통솔 {hero.Leadership} · 선택하면 전투단 편성" });
            }
        }

        // 추가할 전투단 역할 목록을 구성합니다.
        private void BuildArmyRoles(WIAdministrationMilitarySnapshot snapshot, string armyId)
        {
            snapshot.Title = "전투단원 추가 · 역할";
            snapshot.Summary = "새 구성원이 담당할 역할을 선택하십시오.";
            foreach (WIUnitRole role in Enum.GetValues(typeof(WIUnitRole)))
            {
                if (role == WIUnitRole.Commander) continue;
                snapshot.Items.Add(new WIAdministrationMilitaryItemSnapshot { Kind = "role", Value = (int)role, Title = GetUnitRoleDisplayName(role), Description = "대기 인물 선택으로 이동" });
            }
        }

        // 선택한 역할에 배치 가능한 같은 성의 대기 영웅을 구성합니다.
        private void BuildArmyMembers(WIAdministrationMilitarySnapshot snapshot, string armyId, WIUnitRole role)
        {
            WIArmyState army = state.Armies.FirstOrDefault(item => item.ArmyId == armyId);
            WICastleRuntimeState castle = state.GetCastle(army.CurrentCastleId);
            snapshot.Title = GetUnitRoleDisplayName(role) + " · 인물 선택";
            snapshot.Summary = "전투단과 같은 성의 대기 인물을 선택하십시오.";
            foreach (string heroId in castle.HeroIds.Where(id => state.IsCharacterBusy(id) == false))
            {
                WIHeroDefinition hero = database.GetHero(heroId);
                snapshot.Items.Add(new WIAdministrationMilitaryItemSnapshot { Kind = "add-member", Id = heroId, Value = (int)role, Title = hero.DisplayName.Get(database.UseEnglish), Description = $"통솔 {hero.Leadership} · 무력 {hero.Might}" });
            }
        }

        // 전투단이 이동하거나 원정할 인접 목표를 구성합니다.
        private void BuildArmyTargets(WIAdministrationMilitarySnapshot snapshot, string armyId)
        {
            WIArmyState army = state.Armies.FirstOrDefault(item => item.ArmyId == armyId);
            WICastleDefinition origin = database.GetCastle(army.CurrentCastleId);
            snapshot.Title = "이동 / 원정 목표";
            snapshot.Summary = origin.DisplayName.Get(database.UseEnglish) + "에서 출발합니다.";
            foreach (string targetId in state.GetCastle(army.CurrentCastleId).AdjacentCastleIds)
            {
                WICastleRuntimeState target = state.GetCastle(targetId);
                bool friendly = target.FactionId == army.FactionId;
                snapshot.Items.Add(new WIAdministrationMilitaryItemSnapshot { Kind = "march", Id = targetId, Title = (friendly ? "이동 · " : "원정 · ") + database.GetCastle(targetId).DisplayName.Get(database.UseEnglish), Description = friendly ? "같은 진영 성으로 이동" : "영향력 20 · 교전 진영만 가능" });
            }
        }
    }
}

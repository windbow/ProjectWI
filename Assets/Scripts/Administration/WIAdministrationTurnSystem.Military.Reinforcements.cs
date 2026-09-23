using System.Linq;

namespace ProjectWI.Administration
{
    public static partial class WIAdministrationTurnSystem
    {
        // 이미 도착한 같은 진영의 전투단을 미해결 전장에 중복 없이 합류시킵니다.
        public static bool JoinBattleReinforcement(WIAdministrationDatabaseSO database, WIAdministrationState state,
            WIBattleSessionState session, WIArmyState army)
        {
            if (session == null || army == null || session.Status == WIBattleSessionStatus.Resolved ||
                army.IsMoving == true || army.ReorganizationMonths > 0 || army.CurrentCastleId != session.CastleId ||
                (army.FactionId != session.AttackerFactionId && army.FactionId != session.DefenderFactionId) ||
                state.BattleSessions.Any(item => item.Status != WIBattleSessionStatus.Resolved &&
                    (item.AttackerArmyId == army.ArmyId || item.AttackerArmyIds.Contains(army.ArmyId) || item.DefenderArmyIds.Contains(army.ArmyId))))
            {
                return false;
            }
            bool attack = army.FactionId == session.AttackerFactionId;
            var participants = attack == true ? session.AttackerHeroIds : session.DefenderHeroIds;
            if (army.Members.Count == 0 || army.Members.Any(member =>
                session.AttackerHeroIds.Concat(session.DefenderHeroIds).Any(item => item.HeroId == member.HeroId)))
            {
                return false;
            }
            (attack == true ? session.AttackerArmyIds : session.DefenderArmyIds).Add(army.ArmyId);
            foreach (WIArmyMemberState member in army.Members)
            {
                participants.Add(new WIBattleParticipantState { HeroId = member.HeroId, ArmyId = army.ArmyId, Role = member.Role });
            }
            int power = GetArmyBattlePower(database, state, army);
            if (attack == true)
            {
                session.AttackerPowerSnapshot += power;
            }
            else
            {
                session.DefenderPowerSnapshot += power;
            }
            army.AwaitingBattle = true;
            return true;
        }

        // 월 도착이 끝난 뒤 기존 전투의 공격·수비 증원을 먼저 반영해 별도 전투 생성을 막습니다.
        private static void JoinArrivedBattleReinforcements(WIAdministrationDatabaseSO database, WIAdministrationState state)
        {
            foreach (var session in state.BattleSessions.Where(item => item.Status != WIBattleSessionStatus.Resolved).ToList())
            {
                foreach (var army in state.Armies.Where(item => item.CurrentCastleId == session.CastleId).ToList())
                {
                    JoinBattleReinforcement(database, state, session, army);
                }
            }
        }
    }
}

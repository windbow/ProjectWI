using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProjectWI.Administration
{
    public static partial class WIAdministrationTurnSystem
    {
        // 전투 대기 중인 전투단과 성 수비 전력을 비교해 임시 전략 전투 결과를 결정합니다.
        private static void ResolveStrategicBattles(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WITurnSummary summary)
        {
            JoinArrivedBattleReinforcements(database, state);
            // 이전 저장에서 이미 대기 중인 빈 성 전투도 같은 무혈 점령 규칙으로 정리합니다.
            foreach (WIBattleSessionState pending in state.BattleSessions.Where(item =>
                         item.Status == WIBattleSessionStatus.Pending &&
                         item.DefenderHeroIds.Any(member => database.GetHero(member.HeroId) != null) == false).ToList())
            {
                ResolveUnopposedOccupation(database, state, pending, summary);
            }
            List<WIArmyState> attackers = state.Armies.Where(army => army.AwaitingBattle).ToList();
            foreach (WIArmyState attacker in attackers)
            {
                bool alreadyAttacking = state.BattleSessions.Any(item =>
                    item.Status != WIBattleSessionStatus.Resolved &&
                    (item.AttackerArmyId == attacker.ArmyId ||
                     item.AttackerArmyIds != null && item.AttackerArmyIds.Contains(attacker.ArmyId)));
                if (alreadyAttacking)
                {
                    continue;
                }
                bool alreadyDefending = state.BattleSessions.Any(item =>
                    item.Status != WIBattleSessionStatus.Resolved && item.DefenderArmyIds.Contains(attacker.ArmyId));
                if (alreadyDefending)
                {
                    continue;
                }
                WICastleRuntimeState castle = state.GetCastle(attacker.CurrentCastleId);
                if (castle == null || castle.FactionId == attacker.FactionId)
                {
                    attacker.AwaitingBattle = false;
                    continue;
                }

                WIBattleSessionState session = state.BattleSessions.FirstOrDefault(item =>
                    item.AttackerArmyId == attacker.ArmyId && item.Status != WIBattleSessionStatus.Resolved);
                if (session == null)
                {
                    session = CreateBattleSession(database, state, attacker);
                }
                if (session != null && session.DefenderHeroIds.Any(item => database.GetHero(item.HeroId) != null) == false)
                {
                    ResolveUnopposedOccupation(database, state, session, summary);
                    continue;
                }
                bool useFallback = session != null && state.UseStrategicBattleFallback &&
                                   (session.PlayerInvolved == false || state.UsePlayerRealTimeBattles == false);
                if (useFallback)
                {
                    WIBattleOutcome outcome = session.AttackerPowerSnapshot > session.DefenderPowerSnapshot
                        ? WIBattleOutcome.Victory
                        : WIBattleOutcome.Defeat;
                    SubmitBattleResult(database, state, session.SessionId, outcome, WIBattleResolutionSource.StrategicFallback, summary);
                }
            }
        }

        // 빈 성은 기존 점령 처리를 재사용하되 전투 피해와 전투 경험 보상을 발생시키지 않습니다.
        private static void ResolveUnopposedOccupation(
            WIAdministrationDatabaseSO database, WIAdministrationState state,
            WIBattleSessionState session, WITurnSummary summary)
        {
            WIArmyState attacker = state.Armies.Find(item => item.ArmyId == session.AttackerArmyId);
            if (ResolveArmyVictoryAndOccupation(database, state, attacker, summary) == false)
            {
                return;
            }
            WICastleRuntimeState castle = state.GetCastle(session.CastleId);
            foreach (string armyId in session.AttackerArmyIds)
            {
                WIArmyState army = state.Armies.Find(item => item.ArmyId == armyId);
                if (army != null && army != attacker)
                {
                    CompleteSupportingArmyOccupation(database, state, army, castle);
                }
            }
            session.DefenderPowerSnapshot = 0;
            session.AttackerOutcome = WIBattleOutcome.Victory;
            session.ResolutionSource = WIBattleResolutionSource.UnopposedOccupation;
            session.Status = WIBattleSessionStatus.Resolved;
            string message = string.Format(database.GetText("REPORT_UNOPPOSED_OCCUPATION"),
                database.GetCastle(castle.CastleId).DisplayName.Get(database.UseEnglish),
                database.Factions.First(item => item.Id == session.AttackerFactionId).DisplayName.Get(database.UseEnglish));
            if (session.PlayerInvolved == true)
            {
                summary?.News.Insert(0, message);
            }
            else
            {
                summary?.News.Add(message);
            }
        }

        // 전투 진입 시점의 참가 전투단과 전력을 고정한 세션 데이터를 생성합니다.
        public static WIBattleSessionState CreateBattleSession(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WIArmyState attacker)
        {
            if (attacker == null || attacker.AwaitingBattle == false)
            {
                return null;
            }
            WICastleRuntimeState castle = state.GetCastle(attacker.CurrentCastleId);
            if (castle == null || castle.FactionId == attacker.FactionId)
            {
                return null;
            }
            List<WIArmyState> defenders = state.Armies.Where(army =>
                army != attacker && army.FactionId == castle.FactionId &&
                army.CurrentCastleId == castle.CastleId && army.IsOperational).ToList();
            WIArmyState counterAttacker = state.Armies.FirstOrDefault(army =>
                army != attacker && army.AwaitingBattle && army.FactionId == castle.FactionId &&
                army.CurrentCastleId == attacker.OriginCastleId && army.OriginCastleId == castle.CastleId);
            if (counterAttacker != null && defenders.Contains(counterAttacker) == false)
            {
                defenders.Add(counterAttacker);
            }
            List<WIArmyState> attackers = state.CampaignVariant == WICampaignVariant.AresMain
                ? state.Armies.Where(army =>
                    army.FactionId == attacker.FactionId && army.CurrentCastleId == castle.CastleId &&
                    army.AwaitingBattle && defenders.Contains(army) == false &&
                    army.OriginCastleId == attacker.OriginCastleId &&
                    string.IsNullOrEmpty(attacker.StrategicTargetCastleId) == false &&
                    army.StrategicTargetCastleId == attacker.StrategicTargetCastleId).ToList()
                : new List<WIArmyState> { attacker };
            if (attackers.Contains(attacker) == false)
            {
                attackers.Insert(0, attacker);
            }
            WIBattleSessionState session = new WIBattleSessionState
            {
                SessionId = $"battle_{state.NextBattleSessionNumber}",
                CastleId = castle.CastleId,
                AttackerArmyId = attacker.ArmyId,
                CounterAttackerArmyId = counterAttacker?.ArmyId ?? string.Empty,
                AttackerFactionId = attacker.FactionId,
                DefenderFactionId = castle.FactionId,
                AttackerPowerSnapshot = attackers.Sum(army => GetArmyBattlePower(database, state, army)),
                DefenderPowerSnapshot = GetCastleDefensePower(database, state, castle, defenders),
                PlayerInvolved = attacker.FactionId == state.PlayerFactionId || castle.FactionId == state.PlayerFactionId,
                Status = WIBattleSessionStatus.Pending
            };
            session.AttackerArmyIds.AddRange(attackers.Select(army => army.ArmyId));
            session.DefenderArmyIds.AddRange(defenders.Select(army => army.ArmyId));
            foreach (WIArmyState attackingArmy in attackers)
            {
                session.AttackerHeroIds.AddRange(attackingArmy.Members.Select(member => new WIBattleParticipantState
                {
                    HeroId = member.HeroId,
                    ArmyId = attackingArmy.ArmyId,
                    Role = member.Role
                }));
            }
            foreach (WIArmyState defender in defenders)
            {
                session.DefenderHeroIds.AddRange(defender.Members.Select(member => new WIBattleParticipantState
                {
                    HeroId = member.HeroId,
                    ArmyId = defender.ArmyId,
                    Role = member.Role
                }));
            }
            IEnumerable<string> assignedHeroIds = state.Armies.SelectMany(army => army.Members.Select(member => member.HeroId));
            session.DefenderHeroIds.AddRange(castle.HeroIds
                .Where(heroId => assignedHeroIds.Contains(heroId) == false)
                .Select(heroId => new WIBattleParticipantState
                {
                    HeroId = heroId,
                    ArmyId = string.Empty,
                    Role = WIUnitRole.Melee
                }));
            state.NextBattleSessionNumber += 1;
            state.BattleSessions.Add(session);
            foreach (WIArmyState defender in defenders)
            {
                defender.AwaitingBattle = true;
            }
            // 같은 월에 따로 출정해 도착한 전투단도 결과 판정 전에 같은 전장에 합류합니다.
            foreach (WIArmyState arrived in state.Armies.Where(army => army.CurrentCastleId == castle.CastleId).ToList())
            {
                JoinBattleReinforcement(database, state, session, arrived);
            }
            return session;
        }

        // 전투 씬이 사용할 세션을 진행 중 상태로 전환합니다.
        public static bool BeginRealTimeBattle(WIAdministrationState state, string sessionId)
        {
            WIBattleSessionState session = state.BattleSessions.Find(item => item.SessionId == sessionId);
            if (session == null || session.Status != WIBattleSessionStatus.Pending ||
                state.GetCastle(session.CastleId)?.FactionId == session.AttackerFactionId)
            {
                return false;
            }
            session.Status = WIBattleSessionStatus.InProgress;
            return true;
        }

        // 전략 판정 또는 실시간 전투 결과를 캠페인 상태에 동일한 규칙으로 반영합니다.
        public static bool SubmitBattleResult(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            string sessionId,
            WIBattleOutcome attackerOutcome,
            WIBattleResolutionSource source,
            WITurnSummary summary)
        {
            WIBattleSessionState session = state.BattleSessions.Find(item => item.SessionId == sessionId);
            if (session == null || session.Status == WIBattleSessionStatus.Resolved || attackerOutcome == WIBattleOutcome.None)
            {
                return false;
            }
            List<string> attackerIds = session.AttackerArmyIds == null || session.AttackerArmyIds.Count == 0
                ? new List<string> { session.AttackerArmyId }
                : session.AttackerArmyIds;
            List<WIArmyState> attackers = attackerIds.Select(id => state.Armies.Find(army => army.ArmyId == id))
                .Where(army => army != null).ToList();
            WIArmyState attacker = attackers.FirstOrDefault(army => army.ArmyId == session.AttackerArmyId) ??
                                   attackers.FirstOrDefault();
            WICastleRuntimeState castle = state.GetCastle(session.CastleId);
            if (attacker == null || castle == null)
            {
                return false;
            }
            List<WIArmyState> defenders = session.DefenderArmyIds
                .Select(id => state.Armies.Find(army => army.ArmyId == id))
                .Where(army => army != null).ToList();
            int margin = Mathf.Abs(session.AttackerPowerSnapshot - session.DefenderPowerSnapshot);
            attacker.LastBattlePower = session.AttackerPowerSnapshot;
            if (attackerOutcome == WIBattleOutcome.Victory)
            {
                if (defenders.Count == 0)
                {
                    ResolveDefeatedGarrisonCharacterFate(database, state, castle, margin,
                        session.DefenderRetreated, session.AttackerFactionId, summary);
                }
                foreach (WIArmyState defender in defenders)
                {
                    defender.LastBattlePower = GetArmyBattlePower(database, state, defender);
                    defender.LastBattleOutcome = WIBattleOutcome.Defeat;
                    RetreatAndReorganizeArmy(database, state, defender, castle.CastleId, margin,
                        session.DefenderRetreated, session.AttackerFactionId, summary);
                }
                foreach (WIArmyState attackingArmy in attackers)
                {
                    attackingArmy.LastBattlePower = GetArmyBattlePower(database, state, attackingArmy);
                    attackingArmy.LastBattleOutcome = WIBattleOutcome.Victory;
                    ApplyBattleConsequences(database, state, attackingArmy, true, margin, false, summary);
                    ApplyBattleRelationshipConsequences(database, state, attackingArmy, summary);
                }
                ResolveArmyVictoryAndOccupation(database, state, attacker, summary);
                foreach (WIArmyState support in attackers.Where(army => army != attacker))
                {
                    CompleteSupportingArmyOccupation(database, state, support, castle);
                }
                summary?.News.Add($"전투 결과 · {database.GetCastle(castle.CastleId).DisplayName.Get(database.UseEnglish)} 점령 · {attacker.DisplayName} 승리 ({session.AttackerPowerSnapshot}:{session.DefenderPowerSnapshot})");
            }
            else
            {
                foreach (WIArmyState attackingArmy in attackers)
                {
                    attackingArmy.LastBattlePower = GetArmyBattlePower(database, state, attackingArmy);
                    attackingArmy.LastBattleOutcome = WIBattleOutcome.Defeat;
                    RetreatAndReorganizeArmy(database, state, attackingArmy, castle.CastleId, margin,
                        session.AttackerRetreated, session.DefenderFactionId, summary);
                }
                foreach (WIArmyState defender in defenders)
                {
                    defender.LastBattleOutcome = WIBattleOutcome.Victory;
                    defender.AwaitingBattle = false;
                    if (defender.ArmyId == session.CounterAttackerArmyId)
                    {
                        defender.AwaitingBattle = false;
                    }
                    ApplyBattleConsequences(database, state, defender, true, margin, false, summary);
                    ApplyBattleRelationshipConsequences(database, state, defender, summary);
                    if (defender.ArmyId == session.CounterAttackerArmyId)
                    {
                        ResolveArmyVictoryAndOccupation(database, state, defender, summary);
                    }
                }
                summary?.News.Add($"전투 결과 · {database.GetCastle(castle.CastleId).DisplayName.Get(database.UseEnglish)} 방어 성공 · {attacker.DisplayName} 후퇴 ({session.AttackerPowerSnapshot}:{session.DefenderPowerSnapshot})");
            }
            session.AttackerOutcome = attackerOutcome;
            session.ResolutionSource = source;
            session.Status = WIBattleSessionStatus.Resolved;
            return true;
        }

        // 공동 공격에서 주 공격군이 점령한 뒤 지원 공격군도 같은 성에 정상 주둔시킵니다.
        private static void CompleteSupportingArmyOccupation(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WIArmyState army,
            WICastleRuntimeState castle)
        {
            army.AwaitingBattle = false;
            army.OriginCastleId = castle.CastleId;
            army.TargetCastleId = string.Empty;
            army.Mission = WIArmyMission.Reserve;
            army.StrategicTargetCastleId = string.Empty;
            army.ReorganizationMonths = Mathf.Max(army.ReorganizationMonths, database.Automation.VictoryReorganizationMonths);
            foreach (WIArmyMemberState member in army.Members)
            {
                if (castle.HeroIds.Contains(member.HeroId) == false)
                {
                    castle.HeroIds.Add(member.HeroId);
                }
            }
        }

        // 인물 능력, 역할, 숙련, 보급과 피로를 조합해 전투단 전투력을 계산합니다.
        public static int GetArmyBattlePower(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WIArmyState army)
        {
            int power = 0;
            foreach (WIArmyMemberState member in army.Members)
            {
                WIHeroDefinition hero = database.GetHero(member.HeroId);
                WICharacterRuntimeState character = state.GetCharacter(member.HeroId);
                if (hero == null || character == null)
                {
                    continue;
                }
                int roleBonus = member.Role == WIUnitRole.Commander ? hero.Leadership / 2 : 10;
                int injuryPenalty = character.InjuryMonths > 0 ? 30 : 0;
                int experienceBonus = Mathf.Min(20, character.Experience / 25);
                power += Mathf.Max(1, hero.Might + roleBonus + experienceBonus - character.Fatigue / 2 - injuryPenalty);
            }
            power += army.Proficiency == WIUnitProficiency.Elite ? 40 : (army.Proficiency == WIUnitProficiency.Trained ? 20 : 0);
            power += army.CohesionExperience / 2;
            power += GetFactionMilitaryInfrastructureBonus(state, army.FactionId);
            power += GetResearchEffectBonus(database, state.GetFactionState(army.FactionId), WIResearchEffectType.BattlePower);
            foreach (WIArmyMemberState member in army.Members)
            {
                WITitleDefinition title = database.GetTitle(state.GetCharacter(member.HeroId)?.TitleId);
                power += title == null ? 0 : title.BattlePowerBonus;
            }
            if (army.Supply == WISupplyState.Shortage)
            {
                power = power * 80 / 100;
            }
            if (army.Supply == WISupplyState.Depleted)
            {
                power = power * 60 / 100;
            }
            return Mathf.Max(1, power);
        }

        // 진영이 보유한 성의 번영과 기술 평균을 군수·장비 기반 전투력으로 변환합니다.
        public static int GetFactionMilitaryInfrastructureBonus(WIAdministrationState state, string factionId)
        {
            WICastleRuntimeState[] castles = state.Castles
                .Where(castle => castle.FactionId == factionId)
                .ToArray();
            if (castles.Length == 0)
            {
                return 0;
            }

            int averageDevelopment = Mathf.RoundToInt((float)castles
                .Average(castle => castle.Prosperity + castle.Technology));
            return Mathf.Clamp(averageDevelopment / 20, 0, 10);
        }

        // 성 방어도와 주둔 인물 및 수비 전투단 전투력을 합산합니다.
        public static int GetCastleDefensePower(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WICastleRuntimeState castle,
            List<WIArmyState> defenders)
        {
            WICastleDefinition castleDefinition = database.GetCastle(castle.CastleId);
            int specialtyDefense = castleDefinition != null && castleDefinition.SpecialtyEffectType == WICastleSpecialtyEffectType.DefensePower
                ? castleDefinition.SpecialtyEffectValue : 0;
            int power = castle.Defense * database.CastleDefensePowerPercent / 100 +
                        castle.Stability * database.CastleStabilityPowerPercent / 100 + specialtyDefense;
            foreach (string heroId in castle.HeroIds.Where(id => state.Armies.All(army => army.Members.All(member => member.HeroId != id))))
            {
                WIHeroDefinition hero = database.GetHero(heroId);
                WICharacterRuntimeState character = state.GetCharacter(heroId);
                if (hero != null && character != null && character.InjuryMonths <= 0)
                {
                    int heroPower = Mathf.Max(1, hero.Might - character.Fatigue / 2);
                    power += heroPower * database.GarrisonHeroPowerPercent / 100;
                }
            }
            power += defenders.Sum(army => GetArmyBattlePower(database, state, army));
            return Mathf.Max(1, power);
        }
    }
}

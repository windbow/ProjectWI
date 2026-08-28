using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProjectWI.Administration
{
    public enum WIAutoPlayerPolicy
    {
        Administration,
        Balanced,
        Aggressive
    }

    [Serializable]
    public class WIAutoCampaignMetrics
    {
        public WIAutoPlayerPolicy Policy;
        public int MonthsSimulated;
        public int DecisionsResolved;
        public int BattlesResolved;
        public int PlayerVictories;
        public int PlayerDefeats;
        public int ArmiesCreated;
        public int MarchesStarted;
        public int OwnershipChanges;
        public int FinalPlayerCastleCount;
        public WICampaignResult CampaignResult;
        public int RemainingPlayerBattles;
        public int RemainingDecisions;
        public int CommonCharactersReturned;
        public int CharactersDiscovered;
        public int CharactersRecruited;
        public int CharacterDeaths;
        public int PermanentHeroDeaths;
        public int CommonCharacterDeaths;
        public int FinalEmployedCharacters;
        public int FinalHeroCharacters;
        public int FinalCommonCharacters;
        public int FinalWanderingCharacters;
        public List<int> BattleIntervals = new List<int>();
        public List<int> PlayerBattlePowerMargins = new List<int>();
        public List<int> EarlyDecisionCountsByMonth = new List<int>();

        [NonSerialized] public Dictionary<string, int> ChoiceDistribution = new Dictionary<string, int>();

        // 정책별 선택 횟수를 누적합니다.
        public void RecordChoice(string key)
        {
            if (ChoiceDistribution.ContainsKey(key) == false)
            {
                ChoiceDistribution[key] = 0;
            }
            ChoiceDistribution[key] += 1;
        }
    }

    public static class WICampaignAutoPlayer
    {
        // 지정 정책으로 캠페인을 월 단위 실행하고 전황·선택 지표를 반환합니다.
        public static WIAutoCampaignMetrics Run(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WIAutoPlayerPolicy policy,
            int months,
            bool stopWhenCampaignEnds = false)
        {
            WIAutoCampaignMetrics metrics = new WIAutoCampaignMetrics { Policy = policy };
            if (database == null || state == null || months <= 0)
            {
                return metrics;
            }

            Dictionary<string, string> initialOwners = state.Castles.ToDictionary(
                castle => castle.CastleId, castle => castle.FactionId);
            int initialReturnCount = state.Characters.Sum(character => character.CommonReturnCount);
            int initialDiscoveredCount = state.Characters.Count(character => character.Discovered);
            int initialRecruitmentSuccessCount = state.PlayerRecruitmentSuccessCount;
            int initialDeathCount = state.Characters.Sum(character => character.DeathCount);
            int initialPermanentHeroDeaths = state.Characters.Count(character =>
                character.IsDead && character.BaseGrade == WICharacterGrade.Hero);
            int lastBattleMonth = 0;
            if (stopWhenCampaignEnds && state.CampaignResult != WICampaignResult.Ongoing)
            {
                metrics.FinalPlayerCastleCount = state.Castles.Count(castle =>
                    castle.FactionId == state.PlayerFactionId);
                metrics.CampaignResult = state.CampaignResult;
                return metrics;
            }
            for (int month = 1; month <= months; month += 1)
            {
                int decisionsBeforeMonth = metrics.DecisionsResolved;
                ResolvePendingDecisions(database, state, policy, metrics, state.LastMonthlyReport);
                ResolvePendingBattles(database, state, metrics, month, ref lastBattleMonth);
                ConfigureAdministration(state, policy);
                PrepareMilitaryAction(database, state, policy, metrics);
                PrepareRecruitmentAction(database, state, policy);
                WITurnSummary summary = WIAdministrationTurnSystem.ExecuteTurn(database, state);
                ResolvePendingDecisions(database, state, policy, metrics, summary);
                ResolvePendingBattles(database, state, metrics, month, ref lastBattleMonth);
                metrics.MonthsSimulated += 1;
                if (month <= 12)
                {
                    metrics.EarlyDecisionCountsByMonth.Add(metrics.DecisionsResolved - decisionsBeforeMonth);
                }
                if (stopWhenCampaignEnds && state.CampaignResult != WICampaignResult.Ongoing)
                {
                    break;
                }
            }

            metrics.OwnershipChanges = state.Castles.Count(castle =>
                initialOwners.TryGetValue(castle.CastleId, out string owner) && owner != castle.FactionId);
            metrics.FinalPlayerCastleCount = state.Castles.Count(castle =>
                castle.FactionId == state.PlayerFactionId);
            metrics.CampaignResult = state.CampaignResult;
            metrics.RemainingPlayerBattles = state.BattleSessions.Count(session =>
                session.PlayerInvolved && session.Status != WIBattleSessionStatus.Resolved);
            metrics.RemainingDecisions = CountPendingDecisions(state);
            metrics.CommonCharactersReturned = state.Characters.Sum(character => character.CommonReturnCount) - initialReturnCount;
            metrics.CharactersDiscovered = Mathf.Max(0,
                state.Characters.Count(character => character.Discovered) - initialDiscoveredCount);
            metrics.CharactersRecruited = Mathf.Max(0,
                state.PlayerRecruitmentSuccessCount - initialRecruitmentSuccessCount);
            metrics.CharacterDeaths = Mathf.Max(0,
                state.Characters.Sum(character => character.DeathCount) - initialDeathCount);
            metrics.PermanentHeroDeaths = Mathf.Max(0, state.Characters.Count(character =>
                character.IsDead && character.BaseGrade == WICharacterGrade.Hero) - initialPermanentHeroDeaths);
            metrics.CommonCharacterDeaths = Mathf.Max(0,
                metrics.CharacterDeaths - metrics.PermanentHeroDeaths);
            HashSet<string> playerCharacterIds = new HashSet<string>(state.Castles
                .Where(castle => castle.FactionId == state.PlayerFactionId)
                .SelectMany(castle => castle.HeroIds));
            foreach (string heroId in state.Armies.Where(army => army.FactionId == state.PlayerFactionId)
                         .SelectMany(army => army.Members).Select(member => member.HeroId))
            {
                playerCharacterIds.Add(heroId);
            }
            metrics.FinalEmployedCharacters = playerCharacterIds.Count(heroId =>
                state.GetCharacter(heroId)?.IsDead == false);
            metrics.FinalHeroCharacters = playerCharacterIds.Count(heroId =>
            {
                WICharacterRuntimeState character = state.GetCharacter(heroId);
                return character != null && character.IsDead == false &&
                       (character.BaseGrade == WICharacterGrade.Hero || character.PromotedToHero);
            });
            metrics.FinalCommonCharacters = Mathf.Max(0,
                metrics.FinalEmployedCharacters - metrics.FinalHeroCharacters);
            metrics.FinalWanderingCharacters = state.Characters.Count(character =>
                character.IsDead == false && character.Recruited == false && character.Captured == false &&
                string.IsNullOrEmpty(character.JoinedEnemyFactionId));
            return metrics;
        }

        // 실제 인물 활동 규칙을 사용해 재야 탐색 또는 발견한 인재 영입을 매월 한 건 준비합니다.
        private static void PrepareRecruitmentAction(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WIAutoPlayerPolicy policy)
        {
            WICharacterRuntimeState actor = state.Characters
                .Where(character => character.Recruited && character.IsDead == false && character.Captured == false &&
                                    character.Activity == WICharacterActivityType.None &&
                                    WIAdministrationTurnSystem.IsAdministrationCapable(state, character.HeroId) &&
                                    state.IsCharacterBusy(character.HeroId) == false)
                .Where(character => state.Castles.Any(castle =>
                    castle.FactionId == state.PlayerFactionId && castle.HeroIds.Contains(character.HeroId)))
                .OrderBy(character => character.Fatigue)
                .ThenByDescending(character => database.GetHero(character.HeroId)?.Charisma ?? 0)
                .FirstOrDefault();
            if (actor == null || actor.Fatigue > 70)
            {
                return;
            }

            WICharacterRuntimeState candidate = state.Characters
                .Where(character => character.Discovered && character.Recruited == false &&
                                    character.IsDead == false && character.Captured == false &&
                                    string.IsNullOrEmpty(character.JoinedEnemyFactionId))
                .OrderByDescending(character => character.RecruitmentProgress)
                .ThenBy(character => database.GetHero(character.HeroId)?.RequiredReputation ?? 0)
                .FirstOrDefault(character => actor.Reputation >=
                    (database.GetHero(character.HeroId)?.RequiredReputation ?? int.MaxValue));
            if (candidate != null)
            {
                actor.Activity = WICharacterActivityType.Recruit;
                actor.ActivityTargetHeroId = candidate.HeroId;
                return;
            }

            actor.Activity = WICharacterActivityType.Search;
            actor.ActivityTargetHeroId = string.Empty;
        }

        // 정책에 맞춰 플레이어 성의 위임 방침과 진영 방침을 지정합니다.
        private static void ConfigureAdministration(WIAdministrationState state, WIAutoPlayerPolicy policy)
        {
            switch (policy)
            {
                case WIAutoPlayerPolicy.Administration:
                    state.FactionPolicy = WIFactionPolicy.Prosperity;
                    break;
                case WIAutoPlayerPolicy.Aggressive:
                    state.FactionPolicy = WIFactionPolicy.Expedition;
                    break;
                default:
                    state.FactionPolicy = WIFactionPolicy.Stability;
                    break;
            }

            foreach (WICastleRuntimeState castle in state.Castles.Where(item =>
                         item.FactionId == state.PlayerFactionId &&
                         string.IsNullOrEmpty(item.GovernorHeroId) == false))
            {
                castle.DelegatedToGovernor = true;
                castle.GovernorMonthlyBudget = policy == WIAutoPlayerPolicy.Aggressive ? 100 : 200;
                castle.GovernorPolicy = policy == WIAutoPlayerPolicy.Administration
                    ? WIGovernorPolicy.Prosperity
                    : (policy == WIAutoPlayerPolicy.Aggressive
                        ? WIGovernorPolicy.Frontline
                        : WIGovernorPolicy.Balanced);
            }
        }

        // 정책에 따라 플레이어 전투단을 편성하고 합법적인 인접 적 성으로 원정시킵니다.
        private static void PrepareMilitaryAction(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WIAutoPlayerPolicy policy,
            WIAutoCampaignMetrics metrics)
        {
            if (policy == WIAutoPlayerPolicy.Administration)
            {
                return;
            }

            int armyLimit = policy == WIAutoPlayerPolicy.Aggressive ? 2 : 1;
            while (state.Armies.Count(army => army.FactionId == state.PlayerFactionId) < armyLimit)
            {
                WICastleRuntimeState baseCastle = state.Castles
                    .Where(castle => castle.FactionId == state.PlayerFactionId)
                    .OrderByDescending(castle => castle.HeroIds.Count)
                    .FirstOrDefault(castle => castle.HeroIds.Count(heroId =>
                        state.IsCharacterBusy(heroId) == false) >= 2);
                string commanderId = baseCastle?.HeroIds
                    .Where(heroId => state.IsCharacterBusy(heroId) == false)
                    .OrderByDescending(heroId => database.GetHero(heroId)?.Leadership ?? 0)
                    .FirstOrDefault();
                if (baseCastle == null || string.IsNullOrEmpty(commanderId))
                {
                    break;
                }

                WIArmyState army = WIAdministrationTurnSystem.CreateArmy(database, state, baseCastle, commanderId);
                if (army == null)
                {
                    break;
                }
                metrics.ArmiesCreated += 1;
                FillArmy(database, state, baseCastle, army, policy);
            }

            PositionAndConsolidateArmies(database, state);
            PrepareBlockedArmyTraining(database, state, policy);

            // 첫 관문은 1년 안에 공략하고 이후에는 정책별 준비 주기로 연속 점령 속도를 제한합니다.
            bool firstExpansion = state.Castles.Count(castle =>
                castle.FactionId == state.PlayerFactionId) <= 1;
            int marchInterval = firstExpansion
                ? 3
                : (policy == WIAutoPlayerPolicy.Aggressive ? 9 : 12);
            bool allowMarch = state.Turn >= 3 && state.Turn % marchInterval == 0;
            if (allowMarch == false)
            {
                return;
            }

            foreach (WIArmyState army in state.Armies.Where(item =>
                         item.FactionId == state.PlayerFactionId && item.IsOperational).ToList())
            {
                WICastleRuntimeState origin = state.GetCastle(army.CurrentCastleId);
                string targetId = origin?.AdjacentCastleIds
                    .Where(id =>
                    {
                        WICastleRuntimeState target = state.GetCastle(id);
                        return target != null && target.FactionId != state.PlayerFactionId &&
                               WIAdministrationTurnSystem.AreFactionsAtWar(
                                   state, state.PlayerFactionId, target.FactionId) &&
                               CanAutoAttack(database, state, army, target, policy);
                    })
                    .OrderBy(id => state.GetCastle(id).Defense)
                    .ThenBy(id => state.GetCastle(id).Stability)
                    .FirstOrDefault();
                if (string.IsNullOrEmpty(targetId) == false &&
                    WIAdministrationTurnSystem.BeginArmyMarch(database, state, army, targetId))
                {
                    army.Mission = WIArmyMission.Attack;
                    army.StrategicTargetCastleId = targetId;
                    metrics.MarchesStarted += 1;
                }
            }
        }

        // 전선의 예상 전력을 넘지 못한 전투단이 단순 대기하지 않고 합동훈련을 준비하게 합니다.
        private static void PrepareBlockedArmyTraining(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WIAutoPlayerPolicy policy)
        {
            foreach (WIArmyState army in state.Armies.Where(item =>
                         item.FactionId == state.PlayerFactionId && item.IsOperational))
            {
                WICastleRuntimeState origin = state.GetCastle(army.CurrentCastleId);
                if (origin == null)
                {
                    continue;
                }
                List<WICastleRuntimeState> enemies = origin.AdjacentCastleIds
                    .Select(state.GetCastle)
                    .Where(target => target != null && target.FactionId != state.PlayerFactionId &&
                                     WIAdministrationTurnSystem.AreFactionsAtWar(
                                         state, state.PlayerFactionId, target.FactionId))
                    .ToList();
                if (enemies.Count > 0 && enemies.Any(target =>
                        CanAutoAttack(database, state, army, target, policy)) == false)
                {
                    WIAdministrationTurnSystem.ScheduleJointTraining(army);
                }
            }
        }

        // 후방 전투단을 가장 가까운 전선으로 이동시키고 같은 성의 전투단을 권장 인원까지 통합합니다.
        private static void PositionAndConsolidateArmies(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state)
        {
            List<WIArmyState> playerArmies = state.Armies.Where(army =>
                army.FactionId == state.PlayerFactionId && army.IsOperational).ToList();
            foreach (IGrouping<string, WIArmyState> group in playerArmies.GroupBy(army => army.CurrentCastleId))
            {
                WIArmyState primary = group.OrderByDescending(army => army.Members.Count).FirstOrDefault();
                if (primary == null)
                {
                    continue;
                }

                int maximum = WIAdministrationTurnSystem.GetRecommendedArmySize(database, primary);
                foreach (WIArmyState support in group.Where(army => army != primary).ToList())
                {
                    foreach (WIArmyMemberState member in support.Members.ToList())
                    {
                        if (primary.Members.Count >= maximum)
                        {
                            break;
                        }
                        support.Members.Remove(member);
                        if (WIAdministrationTurnSystem.AddArmyMember(
                                database, state, primary, member.HeroId, member.Role) == false)
                        {
                            support.Members.Add(member);
                        }
                    }
                    if (support.Members.Count == 0)
                    {
                        state.Armies.Remove(support);
                    }
                }
            }

            List<WICastleRuntimeState> frontlines = state.Castles.Where(castle =>
                castle.FactionId == state.PlayerFactionId && castle.AdjacentCastleIds.Any(id =>
                {
                    WICastleRuntimeState adjacent = state.GetCastle(id);
                    return adjacent != null && adjacent.FactionId != state.PlayerFactionId &&
                           WIAdministrationTurnSystem.AreFactionsAtWar(
                               state, state.PlayerFactionId, adjacent.FactionId);
                })).ToList();
            foreach (WIArmyState army in state.Armies.Where(item =>
                         item.FactionId == state.PlayerFactionId && item.IsOperational).ToList())
            {
                WICastleRuntimeState origin = state.GetCastle(army.CurrentCastleId);
                if (origin == null || frontlines.Contains(origin))
                {
                    continue;
                }
                string step = FindFriendlyStep(state, origin.CastleId, frontlines.Select(item => item.CastleId));
                if (string.IsNullOrEmpty(step) == false &&
                    WIAdministrationTurnSystem.BeginArmyMarch(database, state, army, step))
                {
                    army.Mission = WIArmyMission.Reinforce;
                }
            }
        }

        // 아군 성만 통과해 가장 가까운 목적지 집합으로 가는 첫 이동 성을 반환합니다.
        private static string FindFriendlyStep(
            WIAdministrationState state,
            string originId,
            IEnumerable<string> destinationIds)
        {
            HashSet<string> destinations = new HashSet<string>(destinationIds);
            Queue<string> queue = new Queue<string>();
            Dictionary<string, string> previous = new Dictionary<string, string>();
            queue.Enqueue(originId);
            previous[originId] = string.Empty;
            string destination = string.Empty;
            while (queue.Count > 0)
            {
                string current = queue.Dequeue();
                if (current != originId && destinations.Contains(current))
                {
                    destination = current;
                    break;
                }
                WICastleRuntimeState castle = state.GetCastle(current);
                if (castle == null)
                {
                    continue;
                }
                foreach (string adjacentId in castle.AdjacentCastleIds)
                {
                    if (previous.ContainsKey(adjacentId) ||
                        state.GetCastle(adjacentId)?.FactionId != state.PlayerFactionId)
                    {
                        continue;
                    }
                    previous[adjacentId] = current;
                    queue.Enqueue(adjacentId);
                }
            }

            if (string.IsNullOrEmpty(destination))
            {
                return string.Empty;
            }
            string step = destination;
            while (previous[step] != originId && string.IsNullOrEmpty(previous[step]) == false)
            {
                step = previous[step];
            }
            return step;
        }

        // 자동 플레이가 피로 누적 또는 현격한 열세에서 반복 원정하지 않도록 공격 가능성을 판정합니다.
        private static bool CanAutoAttack(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WIArmyState army,
            WICastleRuntimeState target,
            WIAutoPlayerPolicy policy)
        {
            float averageFatigue = army.Members.Count == 0 ? 100f : (float)army.Members
                .Average(member => state.GetCharacter(member.HeroId)?.Fatigue ?? 100);
            if (averageFatigue > 60f)
            {
                return false;
            }

            List<WIArmyState> defenders = state.Armies.Where(defender =>
                defender.FactionId == target.FactionId && defender.CurrentCastleId == target.CastleId &&
                defender.IsOperational).ToList();
            int attackPower = WIAdministrationTurnSystem.GetArmyBattlePower(database, state, army);
            int defensePower = WIAdministrationTurnSystem.GetCastleDefensePower(database, state, target, defenders);
            // 서로의 성을 동시에 공격하면 회전 전투로 병합되므로 아군 출발 성으로 오는 적 전투단도 예상 수비 전력에 포함합니다.
            defensePower += state.Armies.Where(enemy =>
                    enemy.FactionId == target.FactionId && enemy.IsOperational && enemy.IsMoving &&
                    enemy.TargetCastleId == army.CurrentCastleId && defenders.Contains(enemy) == false)
                .Sum(enemy => WIAdministrationTurnSystem.GetArmyBattlePower(database, state, enemy));
            // 원정 명령 뒤 실제 전투가 열리기 전까지 수비 사업과 증원으로 전력이 변할 수 있으므로 안전 여유를 둡니다.
            int requiredPercent = policy == WIAutoPlayerPolicy.Aggressive ? 115 : 125;
            return attackPower * 100 >= defensePower * requiredPercent;
        }

        // 성의 대기 인물로 정책별 권장 규모까지 전투단원을 채웁니다.
        private static void FillArmy(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WICastleRuntimeState castle,
            WIArmyState army,
            WIAutoPlayerPolicy policy)
        {
            int maximum = WIAdministrationTurnSystem.GetRecommendedArmySize(database, army);
            int targetSize = policy == WIAutoPlayerPolicy.Aggressive ? maximum : Mathf.Min(3, maximum);
            targetSize = Mathf.Min(targetSize, Mathf.Max(1, castle.HeroIds.Count - 1));
            WIUnitRole[] roles =
            {
                WIUnitRole.Vanguard, WIUnitRole.Ranged, WIUnitRole.Magic,
                WIUnitRole.Support, WIUnitRole.Melee
            };
            int roleIndex = 0;
            foreach (string heroId in castle.HeroIds
                         .Where(id => state.IsCharacterBusy(id) == false)
                         .OrderBy(id => WIAdministrationTurnSystem.IsAdministrationCapable(state, id) ? 1 : 0)
                         .ThenByDescending(id => database.GetHero(id)?.Might ?? 0)
                         .ToList())
            {
                if (army.Members.Count >= targetSize)
                {
                    break;
                }
                WIAdministrationTurnSystem.AddArmyMember(
                    database, state, army, heroId, roles[roleIndex % roles.Length]);
                roleIndex += 1;
            }
        }

        // 플레이어 참가 대기 전투를 전력 스냅샷에 따른 전략 판정으로 모두 해소합니다.
        private static void ResolvePendingBattles(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WIAutoCampaignMetrics metrics,
            int month,
            ref int lastBattleMonth)
        {
            foreach (WIBattleSessionState session in state.BattleSessions.Where(item =>
                         item.PlayerInvolved && item.Status != WIBattleSessionStatus.Resolved).ToList())
            {
                WIBattleOutcome attackerOutcome = session.AttackerPowerSnapshot > session.DefenderPowerSnapshot
                    ? WIBattleOutcome.Victory
                    : WIBattleOutcome.Defeat;
                if (WIAdministrationTurnSystem.SubmitBattleResult(
                        database, state, session.SessionId, attackerOutcome,
                        WIBattleResolutionSource.StrategicFallback, state.LastMonthlyReport) == false)
                {
                    continue;
                }

                metrics.BattlesResolved += 1;
                metrics.BattleIntervals.Add(lastBattleMonth == 0 ? month : month - lastBattleMonth);
                lastBattleMonth = month;
                bool playerWon = session.AttackerFactionId == state.PlayerFactionId
                    ? attackerOutcome == WIBattleOutcome.Victory
                    : attackerOutcome == WIBattleOutcome.Defeat;
                int playerPower = session.AttackerFactionId == state.PlayerFactionId
                    ? session.AttackerPowerSnapshot : session.DefenderPowerSnapshot;
                int enemyPower = session.AttackerFactionId == state.PlayerFactionId
                    ? session.DefenderPowerSnapshot : session.AttackerPowerSnapshot;
                metrics.PlayerBattlePowerMargins.Add(playerPower - enemyPower);
                if (playerWon)
                {
                    metrics.PlayerVictories += 1;
                }
                else
                {
                    metrics.PlayerDefeats += 1;
                }
            }
        }

        // 정책별 점수로 사업·관계·지역·점령·영입·흔적 선택을 자동 해결합니다.
        private static void ResolvePendingDecisions(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WIAutoPlayerPolicy policy,
            WIAutoCampaignMetrics metrics,
            WITurnSummary summary)
        {
            foreach (WIPendingProjectEvent pending in state.PendingProjectEvents.ToList())
            {
                bool bold = policy == WIAutoPlayerPolicy.Aggressive;
                int gain = pending.HeroChoiceAvailable && policy != WIAutoPlayerPolicy.Administration ? 5 : (bold ? 4 : 2);
                if (WIAdministrationTurnSystem.ResolveProjectEvent(state, pending, gain, bold, summary))
                {
                    metrics.DecisionsResolved += 1;
                    metrics.RecordChoice($"project:{policy}");
                }
            }

            foreach (WIPendingRelationshipEvent pending in state.PendingRelationshipEvents.ToList())
            {
                WIRelationshipEventDefinition definition = database.GetRelationshipEvent(pending.EventId);
                int index = SelectBestIndex(definition?.Choices, choice =>
                    choice.RelationshipShift * (policy == WIAutoPlayerPolicy.Aggressive ? 2 : 5) +
                    choice.MeritDelta * 2 - Mathf.Max(0, choice.FatigueDelta));
                if (index >= 0 && WIAdministrationTurnSystem.ResolveRelationshipEvent(
                        database, state, pending, index, summary))
                {
                    metrics.DecisionsResolved += 1;
                    metrics.RecordChoice($"relationship:{index}");
                }
            }

            foreach (WIPendingRegionalEvent pending in state.PendingRegionalEvents.ToList())
            {
                WIRegionalEventDefinition definition = database.GetRegionalEvent(pending.EventId);
                int index = SelectBestIndex(definition?.Choices,
                    choice => ScoreRegionalChoice(choice, policy),
                    choice => WIRegionalEventSystem.CanChoose(state, choice));
                if (index >= 0 && WIRegionalEventSystem.Resolve(database, state, pending, index, summary))
                {
                    metrics.DecisionsResolved += 1;
                    metrics.RecordChoice($"regional:{index}");
                }
            }

            foreach (WIPendingOccupationEvent pending in state.PendingOccupationEvents.ToList())
            {
                int index = SelectBestIndex(database.OccupationChoices,
                    choice => ScoreOccupationChoice(choice, policy),
                    choice => WIOccupationEventSystem.CanChoose(state, choice));
                if (index >= 0 && WIOccupationEventSystem.Resolve(database, state, pending, index, summary))
                {
                    metrics.DecisionsResolved += 1;
                    metrics.RecordChoice($"occupation:{index}");
                }
            }

            foreach (WIPendingRecruitmentEvent pending in state.PendingRecruitmentEvents.ToList())
            {
                WIRecruitmentEventDefinition definition = database.GetRecruitmentEvent(pending.EventId);
                int index = SelectBestIndex(definition?.Choices,
                    choice => choice.RecruiterMeritGain * 2 - Mathf.Max(0, choice.RecruiterFatigueGain),
                    choice => WIAdministrationTurnSystem.CanChooseRecruitmentEventOption(
                        database, state, pending, choice));
                if (index >= 0 && WIAdministrationTurnSystem.ResolveRecruitmentEvent(
                        database, state, pending, index, summary))
                {
                    metrics.DecisionsResolved += 1;
                    metrics.RecordChoice($"recruitment:{index}");
                }
            }

            foreach (WIPendingLegacyChoice pending in state.PendingLegacyChoices.ToList())
            {
                WICastleRuntimeState castle = state.GetCastle(pending.CastleId);
                WIHeroLegacyState replaced = castle != null && castle.HeroLegacies.Count >= 2
                    ? castle.HeroLegacies.OrderBy(item => item.Bonus).FirstOrDefault()
                    : null;
                if (WIAdministrationTurnSystem.InstallHeroLegacy(state, castle, pending, replaced))
                {
                    metrics.DecisionsResolved += 1;
                    metrics.RecordChoice("legacy");
                }
            }
        }

        // 지역 사건 선택지를 정책별 자원·성장·군사 가치로 평가합니다.
        private static int ScoreRegionalChoice(
            WIRegionalEventChoiceDefinition choice,
            WIAutoPlayerPolicy policy)
        {
            int economy = choice.GoldDelta + choice.ManaDelta * 2 + choice.InfluenceDelta * 2;
            int growth = choice.ProsperityDelta * 4 + choice.TechnologyDelta * 4;
            int security = choice.StabilityDelta * 4 + choice.DefenseDelta * 4;
            if (policy == WIAutoPlayerPolicy.Administration)
            {
                return economy + growth * 2 + security;
            }
            if (policy == WIAutoPlayerPolicy.Aggressive)
            {
                return economy + growth + security * 2;
            }
            return economy + growth + security;
        }

        // 점령 통치 선택지를 정책별 안정·성장·방어 가치로 평가합니다.
        private static int ScoreOccupationChoice(
            WIOccupationChoiceDefinition choice,
            WIAutoPlayerPolicy policy)
        {
            int economy = choice.GoldDelta + choice.InfluenceDelta * 2;
            int administration = choice.StabilityDelta * 5 + choice.ProsperityDelta * 4 - choice.UnrestMonths * 3;
            int military = choice.DefenseDelta * 6 - choice.UnrestMonths * 2;
            return policy == WIAutoPlayerPolicy.Aggressive
                ? economy + administration + military * 2
                : (policy == WIAutoPlayerPolicy.Administration
                    ? economy + administration * 2 + military
                    : economy + administration + military);
        }

        // 선택지 목록에서 조건을 만족하는 최고 점수의 인덱스를 반환합니다.
        private static int SelectBestIndex<T>(
            IReadOnlyList<T> choices,
            Func<T, int> score,
            Func<T, bool> predicate = null)
        {
            if (choices == null || choices.Count == 0)
            {
                return -1;
            }

            int selectedIndex = -1;
            int selectedScore = int.MinValue;
            for (int index = 0; index < choices.Count; index += 1)
            {
                if (predicate != null && predicate(choices[index]) == false)
                {
                    continue;
                }
                int currentScore = score(choices[index]);
                if (currentScore > selectedScore)
                {
                    selectedIndex = index;
                    selectedScore = currentScore;
                }
            }
            return selectedIndex;
        }

        // 현재 해결을 기다리는 플레이어 선택 수를 반환합니다.
        private static int CountPendingDecisions(WIAdministrationState state)
        {
            return state.PendingProjectEvents.Count + state.PendingRelationshipEvents.Count +
                   state.PendingRegionalEvents.Count + state.PendingOccupationEvents.Count +
                   state.PendingRecruitmentEvents.Count + state.PendingLegacyChoices.Count;
        }
    }
}

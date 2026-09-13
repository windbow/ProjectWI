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

    public enum WIAutoStrategicPhase
    {
        Observe,
        Prepare,
        Recover,
        Assemble,
        Attack
    }

    [Serializable]
    public class WIAutoDecisionTrace
    {
        public int Month;
        public WIAutoStrategicPhase Phase;
        public string TargetCastleId;
        public string ReasonCode;
        public string Description;
    }

    [Serializable]
    public class WIAutoStrategicPlan
    {
        public string TargetCastleId;
        public string AssemblyCastleId;
        public WIAutoStrategicPhase Phase = WIAutoStrategicPhase.Observe;
        public int RecoveryUntilTurn;
        public int LastDefeatTurn;
        public int ConsecutiveDefeats;
        public string LastAttackTargetCastleId;
        public int SameTargetDefeats;
        public string AvoidedTargetCastleId;
        public int AvoidedTargetUntilTurn;
        public int ConsecutiveNoAttackChecks;
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
        public int FinalValdorCastleCount;
        public WICampaignResult CampaignResult;
        public int RemainingPlayerBattles;
        public int RemainingDecisions;
        public int CommonCharactersReturned;
        public int CharactersDiscovered;
        public int CharactersRecruited;
        public int CharacterDeaths;
        public int PermanentHeroDeaths;
        public int CommonCharacterDeaths;
        public int CharacterCaptures;
        public int CharacterDefections;
        public int PrisonerExchanges;
        public int PrisonerRansoms;
        public int PrisonersRecovered;
        public int PrisonerRansomGoldSpent;
        public int PrisonerRansomManaSpent;
        public int FinalEmployedCharacters;
        public int FinalHeroCharacters;
        public int FinalCommonCharacters;
        public int FinalWanderingCharacters;
        public int RestActions;
        public int RecoveryMonths;
        public int GoalChanges;
        public int IdleMilitaryMonths;
        public int LongestNoMarchMonths;
        public int MaximumHeroFatigue;
        public int ArmyReinforcements;
        public int GoalsAbandoned;
        public int JointAttackBattles;
        public int ThreatResponseMonths;
        public int DefensiveReinforcementMarches;
        public int WarsDeclared;
        public int FinalPlayerArmyCount;
        public int FinalOperationalArmyCount;
        public int FinalMovingArmyCount;
        public int FinalAwaitingBattleArmyCount;
        public int FinalReorganizingArmyCount;
        public int MovingArmyMonths;
        public int AwaitingBattleArmyMonths;
        public int ReorganizingArmyMonths;
        public int LongestMovingArmyMonths;
        public int LongestAwaitingBattleArmyMonths;
        public int LongestReorganizingArmyMonths;
        public int FinalArmyMemberCount;
        public int FinalPlayerArmyPower;
        public int FinalAverageArmyFatigue;
        public int FinalIdleCommonCharacters;
        public int FinalValdorBorderCastleCount;
        public int FinalAttackableValdorBorderCount;
        public int FinalCurrentNoMarchMonths;
        public string FinalStrategicTargetCastleId;
        public int FinalStrategicAssemblyPower;
        public int FinalStrategicRequiredPower;
        public int FinalInfluence;
        public bool FinalAtWarWithValdor;
        public List<int> BattleIntervals = new List<int>();
        public List<int> PlayerBattlePowerMargins = new List<int>();
        public List<int> EarlyDecisionCountsByMonth = new List<int>();
        public List<WIAutoDecisionTrace> DecisionTraces = new List<WIAutoDecisionTrace>();

        [NonSerialized] public Dictionary<string, int> ChoiceDistribution = new Dictionary<string, int>();
        [NonSerialized] public Dictionary<string, int> DecisionReasonCounts = new Dictionary<string, int>();

        // 정책별 선택 횟수를 누적합니다.
        public void RecordChoice(string key)
        {
            if (ChoiceDistribution.ContainsKey(key) == false)
            {
                ChoiceDistribution[key] = 0;
            }
            ChoiceDistribution[key] += 1;
        }

        // 월별 자동 행동 또는 무행동의 핵심 이유를 누적합니다.
        public void RecordDecision(int month, WIAutoStrategicPlan plan, string reasonCode, string description)
        {
            DecisionTraces.Add(new WIAutoDecisionTrace
            {
                Month = month,
                Phase = plan?.Phase ?? WIAutoStrategicPhase.Observe,
                TargetCastleId = plan?.TargetCastleId ?? string.Empty,
                ReasonCode = reasonCode,
                Description = description
            });
            if (DecisionReasonCounts.ContainsKey(reasonCode) == false)
            {
                DecisionReasonCounts[reasonCode] = 0;
            }
            DecisionReasonCounts[reasonCode] += 1;
        }
    }

    public static partial class WICampaignAutoPlayer
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
            int noMarchMonths = 0;
            Dictionary<string, int> movingStreaks = new Dictionary<string, int>();
            Dictionary<string, int> awaitingBattleStreaks = new Dictionary<string, int>();
            Dictionary<string, int> reorganizingStreaks = new Dictionary<string, int>();
            WIAutoStrategicPlan plan = new WIAutoStrategicPlan();
            if (stopWhenCampaignEnds && state.CampaignResult != WICampaignResult.Ongoing)
            {
                metrics.FinalPlayerCastleCount = state.Castles.Count(castle =>
                    castle.FactionId == state.PlayerFactionId);
                metrics.FinalValdorCastleCount = state.Castles.Count(castle => castle.FactionId == "valdor");
                metrics.CampaignResult = state.CampaignResult;
                return metrics;
            }
            for (int month = 1; month <= months; month += 1)
            {
                int decisionsBeforeMonth = metrics.DecisionsResolved;
                int marchesBeforeMonth = metrics.MarchesStarted;
                ResolvePendingDecisions(database, state, policy, metrics, state.LastMonthlyReport);
                ResolvePendingBattles(database, state, metrics, plan, month, ref lastBattleMonth);
                ResolvePrisonerPolicy(database, state, policy, metrics, plan, month);
                ConfigureAdministration(state, policy);
                PrepareHeroRecovery(database, state, metrics, plan, month);
                PrepareMilitaryAction(database, state, policy, metrics, plan, month);
                PrepareRecruitmentAction(database, state, policy, metrics, plan, month);
                WITurnSummary summary = WIAdministrationTurnSystem.ExecuteTurn(database, state);
                ResolvePendingDecisions(database, state, policy, metrics, summary);
                ResolvePendingBattles(database, state, metrics, plan, month, ref lastBattleMonth);
                ResolvePendingDecisions(database, state, policy, metrics, summary);
                RecordArmyStateDurations(
                    state,
                    metrics,
                    movingStreaks,
                    awaitingBattleStreaks,
                    reorganizingStreaks);
                metrics.MonthsSimulated += 1;
                if (metrics.MarchesStarted == marchesBeforeMonth)
                {
                    noMarchMonths += 1;
                    metrics.IdleMilitaryMonths += 1;
                    metrics.LongestNoMarchMonths = Mathf.Max(metrics.LongestNoMarchMonths, noMarchMonths);
                }
                else
                {
                    noMarchMonths = 0;
                }
                metrics.MaximumHeroFatigue = Mathf.Max(metrics.MaximumHeroFatigue,
                    GetMaximumPlayerHeroFatigue(state));
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
            metrics.FinalValdorCastleCount = state.Castles.Count(castle => castle.FactionId == "valdor");
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
            CaptureFinalMilitarySnapshot(database, state, policy, metrics, plan, noMarchMonths);
            return metrics;
        }

        // 플레이어 전투단이 이동·전투 대기·재편성에 머문 기간과 최장 연속 기간을 월 단위로 기록합니다.
        private static void RecordArmyStateDurations(
            WIAdministrationState state,
            WIAutoCampaignMetrics metrics,
            Dictionary<string, int> movingStreaks,
            Dictionary<string, int> awaitingBattleStreaks,
            Dictionary<string, int> reorganizingStreaks)
        {
            foreach (WIArmyState army in state.Armies.Where(item => item.FactionId == state.PlayerFactionId))
            {
                bool isAwaitingBattle = army.AwaitingBattle;
                bool isMoving = isAwaitingBattle == false && army.IsMoving;
                bool isReorganizing = isAwaitingBattle == false && isMoving == false && army.ReorganizationMonths > 0;

                UpdateArmyStateDuration(
                    army.ArmyId,
                    isMoving,
                    movingStreaks,
                    ref metrics.MovingArmyMonths,
                    ref metrics.LongestMovingArmyMonths);
                UpdateArmyStateDuration(
                    army.ArmyId,
                    isAwaitingBattle,
                    awaitingBattleStreaks,
                    ref metrics.AwaitingBattleArmyMonths,
                    ref metrics.LongestAwaitingBattleArmyMonths);
                UpdateArmyStateDuration(
                    army.ArmyId,
                    isReorganizing,
                    reorganizingStreaks,
                    ref metrics.ReorganizingArmyMonths,
                    ref metrics.LongestReorganizingArmyMonths);
            }
        }

        // 한 전투단의 지정 상태 누적 월과 최장 연속 체류 기간을 갱신합니다.
        private static void UpdateArmyStateDuration(
            string armyId,
            bool active,
            Dictionary<string, int> streaks,
            ref int totalArmyMonths,
            ref int longestArmyMonths)
        {
            if (active == false)
            {
                streaks[armyId] = 0;
                return;
            }

            int streak = streaks.TryGetValue(armyId, out int previousStreak) ? previousStreak + 1 : 1;
            streaks[armyId] = streak;
            totalArmyMonths += 1;
            longestArmyMonths = Mathf.Max(longestArmyMonths, streak);
        }

        // 장기 시뮬레이션 종료 시 전투단·피로·보충·발도르 접경 상태를 진단용 지표로 저장합니다.
        private static void CaptureFinalMilitarySnapshot(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WIAutoPlayerPolicy policy,
            WIAutoCampaignMetrics metrics,
            WIAutoStrategicPlan plan,
            int currentNoMarchMonths)
        {
            List<WIArmyState> armies = state.Armies.Where(army =>
                army.FactionId == state.PlayerFactionId).ToList();
            List<WIArmyState> operationalArmies = armies.Where(army => army.IsOperational).ToList();
            List<string> memberIds = armies.SelectMany(army => army.Members)
                .Select(member => member.HeroId).Distinct().ToList();
            metrics.FinalPlayerArmyCount = armies.Count;
            metrics.FinalOperationalArmyCount = operationalArmies.Count;
            metrics.FinalMovingArmyCount = armies.Count(army => army.IsMoving);
            metrics.FinalAwaitingBattleArmyCount = armies.Count(army => army.AwaitingBattle);
            metrics.FinalReorganizingArmyCount = armies.Count(army => army.ReorganizationMonths > 0);
            metrics.FinalArmyMemberCount = memberIds.Count;
            metrics.FinalPlayerArmyPower = operationalArmies.Sum(army =>
                WIAdministrationTurnSystem.GetArmyBattlePower(database, state, army));
            metrics.FinalAverageArmyFatigue = memberIds.Count == 0 ? 0 : Mathf.RoundToInt((float)memberIds
                .Average(heroId => state.GetCharacter(heroId)?.Fatigue ?? 0));
            metrics.FinalIdleCommonCharacters = state.Castles
                .Where(castle => castle.FactionId == state.PlayerFactionId)
                .SelectMany(castle => castle.HeroIds)
                .Distinct()
                .Count(heroId => state.GetCharacter(heroId)?.BaseGrade == WICharacterGrade.Common &&
                                 state.IsCharacterBusy(heroId) == false);
            List<WICastleRuntimeState> valdorBorders = state.Castles.Where(castle =>
                castle.FactionId == "valdor" && castle.AdjacentCastleIds.Any(id =>
                    state.GetCastle(id)?.FactionId == state.PlayerFactionId)).ToList();
            metrics.FinalValdorBorderCastleCount = valdorBorders.Count;
            metrics.FinalAttackableValdorBorderCount = valdorBorders.Count(target => operationalArmies.Any(army =>
                state.GetCastle(army.CurrentCastleId)?.AdjacentCastleIds.Contains(target.CastleId) == true &&
                CanAutoAttack(database, state, army, target, policy)));
            metrics.FinalCurrentNoMarchMonths = currentNoMarchMonths;
            metrics.FinalStrategicTargetCastleId = plan.TargetCastleId ?? string.Empty;
            metrics.FinalStrategicAssemblyPower = operationalArmies.Where(army =>
                    army.CurrentCastleId == plan.AssemblyCastleId)
                .Sum(army => WIAdministrationTurnSystem.GetArmyBattlePower(database, state, army));
            metrics.FinalStrategicRequiredPower = GetRequiredAttackPower(
                database, state, state.GetCastle(plan.TargetCastleId), policy, plan.AssemblyCastleId);
            metrics.FinalInfluence = state.Influence;
            metrics.FinalAtWarWithValdor = WIAdministrationTurnSystem.AreFactionsAtWar(
                state, state.PlayerFactionId, "valdor");
        }

        // 실제 인물 활동 규칙을 사용해 재야 탐색 또는 발견한 인재 영입을 매월 한 건 준비합니다.
        private static void PrepareRecruitmentAction(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WIAutoPlayerPolicy policy,
            WIAutoCampaignMetrics metrics,
            WIAutoStrategicPlan plan,
            int month)
        {
            int playerCastleCount = state.Castles.Count(castle => castle.FactionId == state.PlayerFactionId);
            int employedCount = state.Characters.Count(character => character.Recruited &&
                character.IsDead == false && character.Captured == false &&
                (state.Castles.Any(castle => castle.FactionId == state.PlayerFactionId &&
                                             castle.HeroIds.Contains(character.HeroId)) ||
                 state.Armies.Any(army => army.FactionId == state.PlayerFactionId &&
                                          army.Members.Any(member => member.HeroId == character.HeroId))));
            int targetRosterSize = 14 + Mathf.Max(0, playerCastleCount - 1) * 8;
            if (employedCount >= targetRosterSize)
            {
                metrics.RecordDecision(month, plan, "ROSTER_TARGET_MET",
                    $"현재 인원 {employedCount}명이 {playerCastleCount}성 권장 인원 {targetRosterSize}명을 충족합니다.");
                return;
            }

            WICharacterRuntimeState actor = state.Characters
                .Where(character => character.Recruited && character.IsDead == false && character.Captured == false &&
                                    character.Activity == WICharacterActivityType.None &&
                                    (state.CampaignVariant == WICampaignVariant.AresMain ? WIAdministrationTurnSystem.CanRecruitTalent(state, character.HeroId) : WIAdministrationTurnSystem.IsAdministrationCapable(state, character.HeroId)) &&
                                    state.IsCharacterBusy(character.HeroId) == false)
                .Where(character => state.Castles.Any(castle =>
                    castle.FactionId == state.PlayerFactionId && castle.HeroIds.Contains(character.HeroId)))
                .Where(character => state.Castles.Any(castle => castle.DelegatedToGovernor && castle.GovernorHeroId == character.HeroId) == false)
                .OrderBy(character => character.BaseGrade == WICharacterGrade.Common ? 0 : 1)
                .ThenBy(character => character.Fatigue)
                .ThenByDescending(character => database.GetHero(character.HeroId)?.Charisma ?? 0)
                .FirstOrDefault();
            if (actor == null || actor.Fatigue > 70)
            {
                metrics.RecordDecision(month, plan, "RECRUITMENT_NO_ACTOR", "인재 활동이 가능한 대기 영웅이 없습니다.");
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
                metrics.RecordDecision(month, plan, "RECRUITMENT_PROGRESS", $"{candidate.HeroId} 영입 설득을 진행합니다.");
                return;
            }

            actor.Activity = WICharacterActivityType.Search;
            actor.ActivityTargetHeroId = string.Empty;
            metrics.RecordDecision(month, plan, "RECRUITMENT_SEARCH", "영입 후보를 찾기 위해 재야 인재를 탐색합니다.");
        }

        // 피로가 높은 대기 영웅에게 개인 휴식을 지정하고 인재 활동 담당자를 교대합니다.
        private static void PrepareHeroRecovery(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WIAutoCampaignMetrics metrics,
            WIAutoStrategicPlan plan,
            int month)
        {
            foreach (WICharacterRuntimeState character in state.Characters.Where(character =>
                         character.Recruited && character.IsDead == false && character.Captured == false &&
                         character.Activity == WICharacterActivityType.None && character.Fatigue >= 70 &&
                         (state.CampaignVariant == WICampaignVariant.AresMain ? WIAdministrationTurnSystem.CanRecruitTalent(state, character.HeroId) : WIAdministrationTurnSystem.IsAdministrationCapable(state, character.HeroId)) &&
                         state.IsCharacterBusy(character.HeroId) == false &&
                         state.Castles.Any(castle => castle.FactionId == state.PlayerFactionId &&
                                                     castle.HeroIds.Contains(character.HeroId))))
            {
                character.Activity = WICharacterActivityType.Rest;
                character.ActivityTargetHeroId = string.Empty;
                metrics.RestActions += 1;
                metrics.RecordDecision(month, plan, "HERO_REST", $"{character.HeroId}의 피로 {character.Fatigue} 회복을 우선합니다.");
            }
        }

        // 플레이어 소속 내정 가능 영웅 중 가장 높은 피로를 반환합니다.
        private static int GetMaximumPlayerHeroFatigue(WIAdministrationState state)
        {
            return state.Characters.Where(character =>
                    character.Recruited && character.IsDead == false &&
                    (state.CampaignVariant == WICampaignVariant.AresMain ? WIAdministrationTurnSystem.CanRecruitTalent(state, character.HeroId) : WIAdministrationTurnSystem.IsAdministrationCapable(state, character.HeroId)) &&
                    (state.Castles.Any(castle => castle.FactionId == state.PlayerFactionId &&
                                                castle.HeroIds.Contains(character.HeroId)) ||
                     state.Armies.Any(army => army.FactionId == state.PlayerFactionId &&
                                             army.Members.Any(member => member.HeroId == character.HeroId))))
                .Select(character => character.Fatigue)
                .DefaultIfEmpty(0)
                .Max();
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


    }
}

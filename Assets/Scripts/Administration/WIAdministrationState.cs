using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProjectWI.Administration
{
    public enum WICampaignResult
    {
        Ongoing,
        Victory,
        Defeat
    }

    public enum WICampaignEndingType
    {
        None,
        Concord,
        Dominion
    }

    [Serializable]
    public class WICastleProjectState
    {
        public WICastleProjectType ProjectType;
        public WIProjectInvestment Investment;
        public string ManagerHeroId;
        public string AssistantHeroId;
        public int RemainingMonths = 1;
        public bool Delegated;
        public int ExpectedGain;
        public int GoldCost;
        public string PlanReason;
    }

    [Serializable]
    public class WICastleRuntimeState
    {
        // 해당 성의 다음 달 모병 예약과 제한된 자동 충원 지시입니다.
        public WIMusterOrder MusterOrder;
        public WIMusterPolicy MusterPolicy = new WIMusterPolicy();
        public string CastleId;
        public string FactionId;
        public string GovernorHeroId;
        public List<string> HeroIds = new List<string>();
        public List<string> SpecialFacilityIds = new List<string>();
        public bool PendingSpecialFacilityChoice;
        public bool InvasionWarning;
        public int Prosperity;
        public int Technology;
        public int Stability;
        public int Defense;
        public WICastleSize CastleSize;
        public Vector2 NormalizedMapPosition;
        public List<string> AdjacentCastleIds = new List<string>();
        public WICastleProjectState ActiveProject;
        // 다음 달부터 재사용할 지시이며 실제 임무를 점유하지 않습니다.
        public WICastleProjectState StandingProject;
        public bool RepeatProject;
        // 점령 후 대기 내정 인물이 도착하면 영지관을 한 번 자동 임명합니다.
        public bool PendingGovernorAppointment;
        public bool DelegatedToGovernor;
        public WIGovernorPolicy GovernorPolicy = WIGovernorPolicy.Balanced;
        public int GovernorMonthlyBudget = 100;
        public int OccupationUnrestMonths;
        public List<WIHeroLegacyState> HeroLegacies = new List<WIHeroLegacyState>();
        public List<WIHeroLegacyState> CommemoratedHeroLegacies = new List<WIHeroLegacyState>();
        public List<WITavernQuestState> TavernQuests = new List<WITavernQuestState>();
        public int CounterintelligenceMonths;

        // 현재 성 규모에서 사용할 수 있는 특화 시설 슬롯 수를 반환합니다.
        public int GetSpecialFacilitySlotCount()
        {
            return CastleSize == WICastleSize.Small ? 0 : (CastleSize == WICastleSize.Medium ? 1 : 2);
        }

        // 현재 성 규모에 따른 주둔 인물 슬롯 수를 반환합니다.
        public int GetHeroSlotCount()
        {
            return CastleSize == WICastleSize.Small ? 4 : (CastleSize == WICastleSize.Medium ? 6 : 8);
        }
    }

    [Serializable]
    public class WITurnSummary
    {
        public int GoldGained;
        public int GoldSpent;
        public int ManaGained;
        public int InfluenceGained;
        public int AIProjectsCompleted;
        public List<string> AIProjectFactionIds = new List<string>();
        public List<string> News = new List<string>();
        public List<string> DelegationReports = new List<string>();
        public List<string> AIReasonReports = new List<string>();
    }

    [Serializable]
    public class WISchemeIntelState
    {
        public string ObserverFactionId;
        public string TargetCastleId;
        public int RemainingMonths;
    }

    [Serializable]
    public class WISchemeMissionState
    {
        public string SchemeId;
        public string InitiatorFactionId;
        public string AgentHeroId;
        public string TargetCastleId;
        public string TargetHeroId;
        public int RemainingMonths = 1;
        public int ResolutionRoll;
    }

    [Serializable]
    public class WICharacterTransferState
    {
        public string HeroId;
        public string OriginCastleId;
        public string TargetCastleId;
        public string CurrentCastleId;
        public List<string> RouteCastleIds = new List<string>();
        public int RouteIndex;
        public int RemainingMonths = 1;
    }

    [Serializable]
    public class WIHeroLegacyState
    {
        public string DefinitionId;
        public string HeroId;
        public WICastleProjectType ProjectType;
        public string DisplayName;
        public string Description;
        public int Bonus = 2;
    }

    [Serializable]
    public class WITavernQuestState
    {
        public string QuestId;
        public WITavernQuestType QuestType;
        public WIQuestStatus Status;
        public string AssignedHeroId;
        public int RemainingMonths;
    }

    [Serializable]
    public class WIPendingProjectEvent
    {
        public string CastleId;
        public WICastleProjectType ProjectType;
        public string HeroId;
        public string Title;
        public bool HeroChoiceAvailable;
    }

    [Serializable]
    public class WIPendingLegacyChoice
    {
        public string CastleId;
        public WIHeroLegacyState Legacy;
    }

    [Serializable]
    public class WIPendingRelationshipEvent
    {
        public string EventId;
        public string FirstHeroId;
        public string SecondHeroId;
    }

    [Serializable]
    public class WIPendingRecruitmentEvent
    {
        public string EventId;
        public string RecruiterHeroId;
        public string CandidateHeroId;
    }

    [Serializable]
    public class WIArmyMemberState
    {
        public string HeroId;
        public WIUnitRole Role;
    }

    public enum WIArmyMission
    {
        Reserve,
        Defend,
        Reinforce,
        Attack
    }

    public enum WIBattleOutcome
    {
        None,
        Victory,
        Defeat
    }

    public enum WIBattleSessionStatus
    {
        Pending,
        InProgress,
        Resolved
    }

    public enum WIBattleResolutionSource
    {
        StrategicFallback,
        RealTimeBattle,
        // 방어 인물이 없어 실시간 전투와 전투 피해 없이 점령한 결과입니다.
        UnopposedOccupation
    }

    [Serializable]
    public class WIBattleParticipantState
    {
        public string HeroId;
        public string ArmyId;
        public WIUnitRole Role;
    }

    [Serializable]
    public class WIBattleSessionState
    {
        public string SessionId;
        public string CastleId;
        public string AttackerArmyId;
        public List<string> AttackerArmyIds = new List<string>();
        public string CounterAttackerArmyId;
        public List<string> DefenderArmyIds = new List<string>();
        public string AttackerFactionId;
        public string DefenderFactionId;
        public int AttackerPowerSnapshot;
        public int DefenderPowerSnapshot;
        public List<WIBattleParticipantState> AttackerHeroIds = new List<WIBattleParticipantState>();
        public List<WIBattleParticipantState> DefenderHeroIds = new List<WIBattleParticipantState>();
        public bool PlayerInvolved;
        public WIBattleSessionStatus Status;
        public WIBattleOutcome AttackerOutcome;
        public WIBattleResolutionSource ResolutionSource;
        public bool AttackerRetreated;
        public bool DefenderRetreated;
    }

    [Serializable]
    public class WIArmyState
    {
        public string ArmyId;
        public string DisplayName;
        public string FactionId;
        public string CurrentCastleId;
        public string OriginCastleId;
        public string TargetCastleId;
        public int RemainingTravelMonths;
        public bool AwaitingBattle;
        public WIUnitProficiency Proficiency = WIUnitProficiency.Rookie;
        public WISupplyState Supply = WISupplyState.Sufficient;
        public int CohesionExperience;
        public bool JointTrainingScheduled;
        public WIArmyMission Mission = WIArmyMission.Reserve;
        public string StrategicTargetCastleId;
        public int ReorganizationMonths;
        public WIBattleOutcome LastBattleOutcome;
        public int LastBattlePower;
        public List<WIArmyMemberState> Members = new List<WIArmyMemberState>();

        public bool IsMoving => RemainingTravelMonths > 0;
        public bool IsOperational => IsMoving == false && AwaitingBattle == false && ReorganizationMonths <= 0;
    }

    [Serializable]
    public class WIFactionRuntimeState
    {
        public string FactionId;
        public int Gold;
        public int ManaCrystal;
        public int Influence;
        public WIFactionPolicy Policy = WIFactionPolicy.Prosperity;
        public List<string> CompletedResearchIds = new List<string>();
        public string ActiveResearchId;
        public string ResearcherHeroId;
        public int ResearchRemainingMonths;
        public bool Eliminated;
        public int EliminatedTurn = -1;
    }

    public enum WIDiplomaticStatus
    {
        War,
        Neutral,
        Friendly,
        NonAggression,
        Alliance
    }

    [Serializable]
    public class WIDiplomaticRelationState
    {
        public string FirstFactionId;
        public string SecondFactionId;
        public WIDiplomaticStatus Status = WIDiplomaticStatus.Neutral;
        public int AidCooldownMonths;
        public string JointAttackTargetCastleId;
        public int JointAttackMonthsRemaining;
    }

    [Serializable]
    public class WICharacterRuntimeState
    {
        public string HeroId;
        public bool Discovered;
        public bool Recruited;
        public int Merit;
        public int Reputation;
        public int Experience;
        public int Fatigue;
        public int InjuryMonths;
        public bool Captured;
        public string CaptorFactionId;
        public string CapturedFromFactionId;
        public int CapturedMonthsRemaining;
        public bool RansomRequested;
        public WIResourceType RansomResourceType = WIResourceType.Gold;
        public int RansomAmount;
        public bool IsDead;
        public int DeathCount;
        public int CommonReturnMonthsRemaining;
        public int CommonReturnCount;
        public string JoinedEnemyFactionId;
        public string RecruitmentCastleId;
        public int RecruitmentProgress;
        public WILoyaltyState LoyaltyState = WILoyaltyState.Stable;
        public string TitleId;
        public WICharacterGrade BaseGrade;
        public bool PromotedToHero;
        public bool PromotionAchievement;
        public WICharacterActivityType Activity;
        public string ActivityTargetHeroId;
        // 마스터 데이터에서 복사한 특성으로 저장 복원 직후에도 자격을 판정합니다.
        public List<WITraitType> Traits = new List<WITraitType>();
        // 개인 활동의 유지 지시와 피로에 따른 자동 휴식 상태입니다.
        public bool RepeatActivity;
        public WICharacterActivityType StandingActivity;
        public string StandingActivityTargetHeroId;
        public bool AutomaticRecovery;
    }

    [Serializable]
    public class WIRelationshipState
    {
        public string FirstHeroId;
        public string SecondHeroId;
        public WIRelationshipLevel Level = WIRelationshipLevel.Normal;
        public int SharedBattleVictories;
    }

    [Serializable]
    public class WIPendingRegionalEvent
    {
        public string EventId;
    }

    [Serializable]
    public class WIPendingOccupationEvent
    {
        public string CastleId;
        public string DefeatedFactionId;
    }

    [Serializable]
    public partial class WIAdministrationState
    {
        public WICampaignDifficulty Difficulty = WICampaignDifficulty.Standard;
        public WICampaignVariant CampaignVariant = WICampaignVariant.Free;
        // 자동 시뮬레이션의 재현 가능한 전투·인물 운명 표본을 구분합니다. 실제 캠페인은 기본값 0을 사용합니다.
        public int SimulationSeed;
        public int Year;
        public int Month;
        public int Turn;
        public List<WIFactionRuntimeState> Factions = new List<WIFactionRuntimeState>();
        public string PlayerFactionId;
        public List<WIDiplomaticRelationState> DiplomaticRelations = new List<WIDiplomaticRelationState>();
        public List<WICharacterRuntimeState> Characters = new List<WICharacterRuntimeState>();
        public List<WIRelationshipState> Relationships = new List<WIRelationshipState>();
        public List<WICastleRuntimeState> Castles = new List<WICastleRuntimeState>();
        public WITurnSummary LastMonthlyReport;
        public List<WIPendingProjectEvent> PendingProjectEvents = new List<WIPendingProjectEvent>();
        public List<WIPendingLegacyChoice> PendingLegacyChoices = new List<WIPendingLegacyChoice>();
        public List<WIPendingRelationshipEvent> PendingRelationshipEvents = new List<WIPendingRelationshipEvent>();
        public List<string> CompletedRelationshipEventKeys = new List<string>();
        public List<WIPendingRegionalEvent> PendingRegionalEvents = new List<WIPendingRegionalEvent>();
        public List<string> CompletedRegionalEventIds = new List<string>();
        public List<WIPendingOccupationEvent> PendingOccupationEvents = new List<WIPendingOccupationEvent>();
        public List<WIPendingRecruitmentEvent> PendingRecruitmentEvents = new List<WIPendingRecruitmentEvent>();
        public int PlayerRecruitmentSuccessCount;
        public List<WIArmyState> Armies = new List<WIArmyState>();
        public int NextArmyNumber = 1;
        public List<WIBattleSessionState> BattleSessions = new List<WIBattleSessionState>();
        public List<string> PendingHeroPromotionIds = new List<string>();
        public List<WICharacterTransferState> CharacterTransfers = new List<WICharacterTransferState>();
        public List<WISchemeIntelState> SchemeIntel = new List<WISchemeIntelState>();
        public List<WISchemeMissionState> SchemeMissions = new List<WISchemeMissionState>();
        public int NextBattleSessionNumber = 1;
        public bool UseStrategicBattleFallback = true;
        public bool UsePlayerRealTimeBattles = true;
        public bool TutorialSkipped;
        public List<string> CompletedTutorialIds = new List<string>();
        public List<string> CompletedCampaignObjectiveIds = new List<string>();
        public WICampaignResult CampaignResult;
        public int CampaignResultTurn;
        public bool CampaignResultAcknowledged;
        public WICampaignEndingType CampaignEnding;
        public List<string> OccupationPolicyHistory = new List<string>();
        public int PendingPlayerGoldSpent;

        public int Gold
        {
            get => GetPlayerFactionState()?.Gold ?? 0;
            set { if (GetPlayerFactionState() != null) GetPlayerFactionState().Gold = value; }
        }

        public int ManaCrystal
        {
            get => GetPlayerFactionState()?.ManaCrystal ?? 0;
            set { if (GetPlayerFactionState() != null) GetPlayerFactionState().ManaCrystal = value; }
        }

        public int Influence
        {
            get => GetPlayerFactionState()?.Influence ?? 0;
            set { if (GetPlayerFactionState() != null) GetPlayerFactionState().Influence = value; }
        }

        public WIFactionPolicy FactionPolicy
        {
            get => GetPlayerFactionState()?.Policy ?? WIFactionPolicy.Prosperity;
            set { if (GetPlayerFactionState() != null) GetPlayerFactionState().Policy = value; }
        }

        // 마스터 데이터에서 새 캠페인의 런타임 상태를 생성합니다.
        public static WIAdministrationState Create(WIAdministrationDatabaseSO database,
            WICampaignDifficulty difficulty = WICampaignDifficulty.Standard,
            WICampaignVariant variant = WICampaignVariant.Free)
        {
            WIAdministrationState state = new WIAdministrationState
            {
                Year = database.StartingYear,
                Month = database.StartingMonth,
                Turn = 1,
                Difficulty = difficulty,
                CampaignVariant = variant
            };

            foreach (WIFactionDefinition faction in database.Factions)
            {
                if (faction.PlayerFaction)
                {
                    state.PlayerFactionId = faction.Id;
                }
                state.Factions.Add(new WIFactionRuntimeState
                {
                    FactionId = faction.Id,
                    Gold = faction.PlayerFaction ? database.StartingGold : faction.StartingGold,
                    ManaCrystal = faction.PlayerFaction ? database.StartingManaCrystal : faction.StartingManaCrystal,
                    Influence = faction.PlayerFaction ? database.StartingInfluence : faction.StartingInfluence,
                    Policy = GetPolicyForAIStrategy(faction.AIStrategy)
                });
            }

            state.EnsureDefaultDiplomacy();

            foreach (WICastleDefinition castle in database.Castles)
            {
                state.Castles.Add(new WICastleRuntimeState
                {
                    CastleId = castle.Id,
                    FactionId = castle.FactionId,
                    Prosperity = castle.InitialProsperity,
                    Technology = castle.InitialTechnology,
                    Stability = castle.InitialStability,
                    Defense = castle.InitialDefense,
                    CastleSize = castle.CastleSize,
                    NormalizedMapPosition = castle.NormalizedMapPosition,
                    AdjacentCastleIds = castle.AdjacentCastleIds.ToList()
                });
            }

            foreach (WIHeroDefinition hero in database.Heroes)
            {
                state.Characters.Add(new WICharacterRuntimeState
                {
                    HeroId = hero.Id,
                    LoyaltyState = hero.LoyaltyState,
                    Traits = hero.Traits.ToList(),
                    BaseGrade = hero.Grade
                });
            }

            foreach (WIStartingHeroPlacement placement in database.StartingHeroes)
            {
                WICastleRuntimeState castle = state.GetCastle(placement.CastleId);
                if (castle == null)
                {
                    continue;
                }

                WICharacterRuntimeState character = state.GetCharacter(placement.HeroId);
                if (character == null)
                {
                    continue;
                }

                castle.HeroIds.Add(placement.HeroId);
                character.Discovered = true;
                character.Recruited = true;
                if (placement.Governor == true &&
                    WIAdministrationTurnSystem.IsAdministrationCapable(state, character.HeroId))
                {
                    castle.GovernorHeroId = placement.HeroId;
                }
            }

            foreach (WIStartingRelationshipDefinition definition in database.StartingRelationships)
            {
                WIHeroDefinition first = database.GetHero(definition.FirstHeroId);
                WIHeroDefinition second = database.GetHero(definition.SecondHeroId);
                if (first == null || second == null || first.Id == second.Id) continue;
                state.GetOrCreateRelationship(first.Id, second.Id).Level = definition.Level;
            }

            WICampaignVariantDefinition variantDefinition = database.GetCampaignVariant(variant);
            if (variantDefinition != null)
            {
                ApplyCampaignCastlePlacements(state, variantDefinition);
                ApplyCampaignCharacterPlacements(state, variantDefinition);
                WICastleRuntimeState additionalCastle = state.GetCastle(variantDefinition.AdditionalPlayerCastleId);
                if (additionalCastle != null)
                {
                    additionalCastle.FactionId = state.PlayerFactionId;
                }
                if (string.IsNullOrEmpty(variantDefinition.RelationshipFirstHeroId) == false &&
                    string.IsNullOrEmpty(variantDefinition.RelationshipSecondHeroId) == false)
                {
                    state.GetOrCreateRelationship(variantDefinition.RelationshipFirstHeroId,
                        variantDefinition.RelationshipSecondHeroId).Level = variantDefinition.RelationshipLevel;
                }
            }

            return state;
        }

        // 시나리오에 저장된 시작 인물 배치를 성과 인물 고용 상태에 적용합니다.
        private static void ApplyCampaignCharacterPlacements(
            WIAdministrationState state,
            WICampaignVariantDefinition variantDefinition)
        {
            if (variantDefinition.CharacterPlacements == null ||
                variantDefinition.CharacterPlacements.Count == 0)
            {
                return;
            }

            foreach (WICastleRuntimeState castle in state.Castles)
            {
                castle.HeroIds.Clear();
                castle.GovernorHeroId = string.Empty;
            }
            foreach (WICharacterRuntimeState character in state.Characters)
            {
                character.Recruited = false;
                character.Discovered = false;
            }

            foreach (WICampaignCharacterPlacement placement in variantDefinition.CharacterPlacements)
            {
                WICastleRuntimeState castle = state.GetCastle(placement.CastleId);
                WICharacterRuntimeState character = state.GetCharacter(placement.HeroId);
                if (castle == null || character == null || castle.HeroIds.Contains(placement.HeroId))
                {
                    continue;
                }
                castle.HeroIds.Add(placement.HeroId);
                character.Recruited = true;
                character.Discovered = true;
                if (placement.Governor && string.IsNullOrEmpty(castle.GovernorHeroId) &&
                    WIAdministrationTurnSystem.IsAdministrationCapable(state, character.HeroId))
                {
                    castle.GovernorHeroId = placement.HeroId;
                }
            }
        }

        // 이전 저장 파일에 없는 시나리오별 지도 좌표와 연결 정보를 마스터 데이터에서 복구합니다.
        public void EnsureRuntimeCastleMap(WIAdministrationDatabaseSO database)
        {
            WICampaignVariantDefinition variantDefinition = database.GetCampaignVariant(CampaignVariant);
            foreach (WICastleRuntimeState castle in Castles)
            {
                WICastleDefinition definition = database.GetCastle(castle.CastleId);
                if (definition == null)
                {
                    continue;
                }

                if (castle.AdjacentCastleIds == null || castle.AdjacentCastleIds.Count == 0)
                {
                    castle.AdjacentCastleIds = definition.AdjacentCastleIds.ToList();
                }
                if (castle.NormalizedMapPosition == Vector2.zero)
                {
                    castle.NormalizedMapPosition = definition.NormalizedMapPosition;
                }

                WICampaignCastlePlacement placement = variantDefinition?.CastlePlacements
                    .FirstOrDefault(item => item.CastleId == castle.CastleId);
                if (placement == null)
                {
                    continue;
                }
                if (placement.OverrideMapPosition)
                {
                    castle.NormalizedMapPosition = placement.NormalizedMapPosition;
                }
                if (placement.OverrideConnections)
                {
                    castle.AdjacentCastleIds = placement.AdjacentCastleIds.Distinct().ToList();
                }
                foreach (string heroId in placement.RecruitableHeroIds)
                {
                    WICharacterRuntimeState candidate = GetCharacter(heroId);
                    if (candidate != null && candidate.Recruited == false &&
                        string.IsNullOrEmpty(candidate.RecruitmentCastleId))
                    {
                        candidate.RecruitmentCastleId = castle.CastleId;
                    }
                }
            }
        }

        // 시나리오 전용 성 소유권·좌표·연결을 런타임 상태에 적용하고 시작 인물을 재배치합니다.
        private static void ApplyCampaignCastlePlacements(
            WIAdministrationState state,
            WICampaignVariantDefinition variantDefinition)
        {
            Dictionary<string, string> originalOwners = state.Castles.ToDictionary(
                castle => castle.CastleId, castle => castle.FactionId);
            foreach (WICampaignCastlePlacement placement in variantDefinition.CastlePlacements)
            {
                WICastleRuntimeState castle = state.GetCastle(placement.CastleId);
                if (castle == null)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(placement.FactionId) == false)
                {
                    castle.FactionId = placement.FactionId;
                }
                if (placement.OverrideMapPosition)
                {
                    castle.NormalizedMapPosition = placement.NormalizedMapPosition;
                }
                if (placement.OverrideConnections)
                {
                    castle.AdjacentCastleIds = placement.AdjacentCastleIds.Distinct().ToList();
                }
                if (placement.OverrideInitialStats)
                {
                    castle.Prosperity = placement.InitialProsperity;
                    castle.Technology = placement.InitialTechnology;
                    castle.Stability = placement.InitialStability;
                    castle.Defense = placement.InitialDefense;
                }
                foreach (string heroId in placement.RecruitableHeroIds)
                {
                    WICharacterRuntimeState candidate = state.GetCharacter(heroId);
                    if (candidate != null && candidate.Recruited == false)
                    {
                        candidate.RecruitmentCastleId = castle.CastleId;
                    }
                }
            }

            WICastleRuntimeState playerStart = state.GetCastle(variantDefinition.PlayerStartingCastleId);
            if (playerStart == null)
            {
                return;
            }

            List<string> playerHeroIds = state.Castles
                .Where(castle => originalOwners.TryGetValue(castle.CastleId, out string owner) &&
                                 owner == state.PlayerFactionId)
                .SelectMany(castle => castle.HeroIds)
                .Distinct()
                .ToList();
            foreach (WICastleRuntimeState castle in state.Castles)
            {
                string originalOwner = originalOwners[castle.CastleId];
                if (castle.FactionId != originalOwner && castle.HeroIds.Count > 0)
                {
                    WICastleRuntimeState fallback = state.Castles.FirstOrDefault(item =>
                        item.CastleId != castle.CastleId && item.FactionId == originalOwner);
                    if (fallback != null)
                    {
                        foreach (string heroId in castle.HeroIds.Where(id => playerHeroIds.Contains(id) == false).ToList())
                        {
                            if (fallback.HeroIds.Contains(heroId) == false)
                            {
                                fallback.HeroIds.Add(heroId);
                            }
                            castle.HeroIds.Remove(heroId);
                        }
                    }
                }
                foreach (string heroId in playerHeroIds)
                {
                    castle.HeroIds.Remove(heroId);
                }
            }

            foreach (string heroId in playerHeroIds)
            {
                if (playerStart.HeroIds.Contains(heroId) == false)
                {
                    playerStart.HeroIds.Add(heroId);
                }
            }
            playerStart.GovernorHeroId = playerHeroIds.FirstOrDefault();
        }

        // AI 성향에 대응하는 기본 진영 방침을 반환합니다.
        private static WIFactionPolicy GetPolicyForAIStrategy(WIAIStrategy strategy)
        {
            switch (strategy)
            {
                case WIAIStrategy.Development: return WIFactionPolicy.Development;
                case WIAIStrategy.Defense: return WIFactionPolicy.Defense;
                case WIAIStrategy.Aggressive: return WIFactionPolicy.Expedition;
                case WIAIStrategy.Scheme: return WIFactionPolicy.Talent;
                default: return WIFactionPolicy.Prosperity;
            }
        }

        // ID로 진영의 독립 경제 상태를 찾습니다.
        public WIFactionRuntimeState GetFactionState(string factionId)
        {
            return Factions.Find(item => item.FactionId == factionId);
        }

        // 플레이어 진영의 경제 상태를 반환합니다.
        public WIFactionRuntimeState GetPlayerFactionState()
        {
            return Factions.Find(item => item.FactionId == PlayerFactionId);
        }

        // 두 진영 사이의 외교 관계를 순서와 무관하게 찾거나 생성합니다.
        public WIDiplomaticRelationState GetOrCreateDiplomaticRelation(string firstFactionId, string secondFactionId)
        {
            if (firstFactionId == secondFactionId || string.IsNullOrEmpty(firstFactionId) || string.IsNullOrEmpty(secondFactionId))
            {
                return null;
            }

            WIDiplomaticRelationState relation = DiplomaticRelations.Find(item =>
                (item.FirstFactionId == firstFactionId && item.SecondFactionId == secondFactionId) ||
                (item.FirstFactionId == secondFactionId && item.SecondFactionId == firstFactionId));
            if (relation != null)
            {
                return relation;
            }

            relation = new WIDiplomaticRelationState
            {
                FirstFactionId = firstFactionId,
                SecondFactionId = secondFactionId,
                Status = WIDiplomaticStatus.Neutral
            };
            DiplomaticRelations.Add(relation);
            return relation;
        }

        // 신규 및 구버전 캠페인에 기본 진영 관계를 채웁니다.
        public void EnsureDefaultDiplomacy()
        {
            DiplomaticRelations = DiplomaticRelations ?? new List<WIDiplomaticRelationState>();
            for (int firstIndex = 0; firstIndex < Factions.Count; firstIndex++)
            {
                for (int secondIndex = firstIndex + 1; secondIndex < Factions.Count; secondIndex++)
                {
                    string firstId = Factions[firstIndex].FactionId;
                    string secondId = Factions[secondIndex].FactionId;
                    WIDiplomaticRelationState existing = DiplomaticRelations.Find(item =>
                        (item.FirstFactionId == firstId && item.SecondFactionId == secondId) ||
                        (item.FirstFactionId == secondId && item.SecondFactionId == firstId));
                    if (existing != null)
                    {
                        continue;
                    }

                    DiplomaticRelations.Add(new WIDiplomaticRelationState
                    {
                        FirstFactionId = firstId,
                        SecondFactionId = secondId,
                        Status = (firstId == "avalon" && secondId == "valdor") ||
                                 (firstId == "valdor" && secondId == "avalon")
                            ? WIDiplomaticStatus.War
                            : WIDiplomaticStatus.Neutral
                    });
                }
            }
        }

        // ID로 런타임 성 상태를 찾습니다.
        public WICastleRuntimeState GetCastle(string castleId)
        {
            return Castles.Find(item => item.CastleId == castleId);
        }

        // ID로 인물의 캠페인 상태를 찾습니다.
        public WICharacterRuntimeState GetCharacter(string heroId)
        {
            return Characters.Find(item => item.HeroId == heroId);
        }

        // 두 인물 사이의 관계 상태를 순서와 무관하게 찾거나 생성합니다.
        public WIRelationshipState GetOrCreateRelationship(string firstHeroId, string secondHeroId)
        {
            WIRelationshipState relationship = Relationships.Find(item =>
                (item.FirstHeroId == firstHeroId && item.SecondHeroId == secondHeroId) ||
                (item.FirstHeroId == secondHeroId && item.SecondHeroId == firstHeroId));
            if (relationship != null)
            {
                return relationship;
            }

            relationship = new WIRelationshipState
            {
                FirstHeroId = firstHeroId,
                SecondHeroId = secondHeroId
            };
            Relationships.Add(relationship);
            return relationship;
        }

        // 다른 성의 중점 사업을 포함해 인물이 이미 이번 달 임무를 맡았는지 확인합니다.
        public bool IsHeroAssignedToProject(string heroId)
        {
            return Castles.Exists(item =>
                item.ActiveProject != null &&
                (item.ActiveProject.ManagerHeroId == heroId || item.ActiveProject.AssistantHeroId == heroId));
        }

        // 기존 단일 인자 호출과 조건 함수 전달에는 모든 업무 점유를 확인합니다.
        public bool IsCharacterBusy(string heroId)
        {
            return IsCharacterBusy(heroId, false, false);
        }

        // 업무별 점유를 확인하며 내정과 전투단의 병행 판단에서는 해당 점유만 제외합니다.
        public bool IsCharacterBusy(string heroId, bool ignoreArmy = false, bool ignoreAdministration = false)
        {
            WICharacterRuntimeState character = GetCharacter(heroId);
            bool assignedToQuest = Castles.Exists(castle => castle.TavernQuests.Exists(quest =>
                quest.Status == WIQuestStatus.Accepted && quest.AssignedHeroId == heroId));
            bool assignedToArmy = Armies.Exists(army => army.Members.Exists(member => member.HeroId == heroId));
            bool assignedToResearch = Factions.Exists(faction => faction.ResearcherHeroId == heroId && string.IsNullOrEmpty(faction.ActiveResearchId) == false);
            bool transferring = CharacterTransfers.Exists(transfer => transfer.HeroId == heroId);
            bool assignedToScheme = SchemeMissions != null && SchemeMissions.Exists(mission => mission.AgentHeroId == heroId);
            return (ignoreAdministration == false && IsHeroAssignedToProject(heroId)) ||
                   (character != null && (character.Captured || character.IsDead)) ||
                   (character != null && character.Activity != WICharacterActivityType.None &&
                       (ignoreAdministration == false || character.Activity != WICharacterActivityType.Training)) ||
                   assignedToQuest ||
                   (ignoreArmy == false && assignedToArmy) ||
                   (ignoreAdministration == false && assignedToResearch) ||
                   transferring ||
                   assignedToScheme;
        }
    }
}

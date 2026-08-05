using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectWI.Administration
{
    public enum WIResourceType
    {
        Gold,
        ManaCrystal,
        Influence
    }

    public enum WICastleSize
    {
        Small,
        Medium,
        Large
    }

    public enum WICastleProjectType
    {
        Prosperity,
        Technology,
        Stability,
        Fortification,
        Recruitment,
        Training,
        Recovery,
        Expansion
    }

    public enum WIProjectInvestment
    {
        Basic,
        Intensive
    }

    public enum WICastleSpecialtyEffectType
    {
        ProjectGain,
        GoldIncome,
        ManaIncome,
        InfluenceIncome,
        DefensePower
    }

    public enum WIFactionPolicy
    {
        Prosperity,
        Development,
        Stability,
        Defense,
        Expedition,
        Talent
    }

    public enum WIGovernorPolicy
    {
        Balanced,
        Prosperity,
        Research,
        Frontline,
        Talent
    }

    public enum WIHeroRace
    {
        Human,
        Elf,
        Dwarf,
        Beast,
        Orc,
        Undead,
        DarkElf
    }

    public enum WIHeroClass
    {
        MagicSwordsman,
        Guardian,
        Crusader,
        SwordMaster,
        Archer,
        Assassin,
        Archmage,
        Priest,
        Druid,
        Strategist,
        Alchemist,
        Warlock
    }

    public enum WILoyaltyState
    {
        Stable,
        Unsettled,
        Danger
    }

    public enum WICharacterGrade
    {
        Hero,
        Common
    }

    public enum WITraitType
    {
        Agronomist,
        Merchant,
        Architect,
        Constable,
        Scholar,
        Negotiator,
        Doctor,
        Instructor
    }

    public enum WICharacterActivityType
    {
        None,
        Search,
        Socialize,
        Recruit,
        Training,
        Rest
    }

    public enum WIRelationshipLevel
    {
        Conflict,
        Normal,
        Fondness
    }

    [Serializable]
    public class WIRelationshipEventChoiceDefinition
    {
        [SerializeField] private WILocalizedString label;
        [SerializeField] private WILocalizedString resultDescription;
        [SerializeField] private int relationshipShift;
        [SerializeField] private int meritDelta;
        [SerializeField] private int fatigueDelta;

        public WILocalizedString Label => label;
        public WILocalizedString ResultDescription => resultDescription;
        public int RelationshipShift => relationshipShift;
        public int MeritDelta => meritDelta;
        public int FatigueDelta => fatigueDelta;
    }

    [Serializable]
    public class WIRelationshipEventDefinition
    {
        [SerializeField] private string id;
        [SerializeField] private WILocalizedString title;
        [SerializeField] private WILocalizedString description;
        [SerializeField] private WIRelationshipLevel requiredLevel;
        [SerializeField] private List<WIRelationshipEventChoiceDefinition> choices = new List<WIRelationshipEventChoiceDefinition>();

        public string Id => id;
        public WILocalizedString Title => title;
        public WILocalizedString Description => description;
        public WIRelationshipLevel RequiredLevel => requiredLevel;
        public IReadOnlyList<WIRelationshipEventChoiceDefinition> Choices => choices;
    }

    [Serializable]
    public class WIStartingRelationshipDefinition
    {
        [SerializeField] private string factionId;
        [SerializeField] private string firstHeroId;
        [SerializeField] private string secondHeroId;
        [SerializeField] private WIRelationshipLevel level = WIRelationshipLevel.Normal;
        [SerializeField] private WILocalizedString context;

        public string FactionId => factionId;
        public string FirstHeroId => firstHeroId;
        public string SecondHeroId => secondHeroId;
        public WIRelationshipLevel Level => level;
        public WILocalizedString Context => context;
    }

    [Serializable]
    public class WIRegionalEventChoiceDefinition
    {
        [SerializeField] private WILocalizedString label;
        [SerializeField] private WILocalizedString resultDescription;
        [SerializeField] private int goldDelta;
        [SerializeField] private int manaDelta;
        [SerializeField] private int influenceDelta;
        [SerializeField] private int prosperityDelta;
        [SerializeField] private int technologyDelta;
        [SerializeField] private int stabilityDelta;
        [SerializeField] private int defenseDelta;

        public WILocalizedString Label => label;
        public WILocalizedString ResultDescription => resultDescription;
        public int GoldDelta => goldDelta;
        public int ManaDelta => manaDelta;
        public int InfluenceDelta => influenceDelta;
        public int ProsperityDelta => prosperityDelta;
        public int TechnologyDelta => technologyDelta;
        public int StabilityDelta => stabilityDelta;
        public int DefenseDelta => defenseDelta;
    }

    [Serializable]
    public class WIRegionalEventDefinition
    {
        [SerializeField] private string id;
        [SerializeField] private string originFactionId;
        [SerializeField] private string targetCastleId;
        [SerializeField] private int minimumTurn = 1;
        [SerializeField] private WILocalizedString title;
        [SerializeField] private WILocalizedString description;
        [SerializeField] private List<WIRegionalEventChoiceDefinition> choices = new List<WIRegionalEventChoiceDefinition>();

        public string Id => id;
        public string OriginFactionId => originFactionId;
        public string TargetCastleId => targetCastleId;
        public int MinimumTurn => minimumTurn;
        public WILocalizedString Title => title;
        public WILocalizedString Description => description;
        public IReadOnlyList<WIRegionalEventChoiceDefinition> Choices => choices;
    }

    [Serializable]
    public class WIOccupationChoiceDefinition
    {
        [SerializeField] private string id;
        [SerializeField] private WILocalizedString label;
        [SerializeField] private WILocalizedString resultDescription;
        [SerializeField] private int goldDelta;
        [SerializeField] private int influenceDelta;
        [SerializeField] private int stabilityDelta;
        [SerializeField] private int defenseDelta;
        [SerializeField] private int prosperityDelta;
        [SerializeField] private int unrestMonths = 2;

        public string Id => id;
        public WILocalizedString Label => label;
        public WILocalizedString ResultDescription => resultDescription;
        public int GoldDelta => goldDelta;
        public int InfluenceDelta => influenceDelta;
        public int StabilityDelta => stabilityDelta;
        public int DefenseDelta => defenseDelta;
        public int ProsperityDelta => prosperityDelta;
        public int UnrestMonths => unrestMonths;
    }

    [Serializable]
    public class WIFactionEliminationNarrativeDefinition
    {
        [SerializeField] private string factionId;
        [SerializeField] private WILocalizedString title;
        [SerializeField] private WILocalizedString description;

        public string FactionId => factionId;
        public WILocalizedString Title => title;
        public WILocalizedString Description => description;
    }

    [Serializable]
    public class WICampaignEndingDefinition
    {
        [SerializeField] private WICampaignEndingType endingType;
        [SerializeField] private WILocalizedString title;
        [SerializeField] private WILocalizedString description;

        public WICampaignEndingType EndingType => endingType;
        public WILocalizedString Title => title;
        public WILocalizedString Description => description;
    }

    public enum WICampaignVariant
    {
        Classic,
        BorderGarrison,
        DividedCourt
    }

    [Serializable]
    public class WICampaignVariantDefinition
    {
        [SerializeField] private WICampaignVariant variant;
        [SerializeField] private WILocalizedString displayName;
        [SerializeField] private WILocalizedString description;
        [SerializeField] private string additionalPlayerCastleId;
        [SerializeField] private string relationshipFirstHeroId;
        [SerializeField] private string relationshipSecondHeroId;
        [SerializeField] private WIRelationshipLevel relationshipLevel = WIRelationshipLevel.Normal;

        public WICampaignVariant Variant => variant;
        public WILocalizedString DisplayName => displayName;
        public WILocalizedString Description => description;
        public string AdditionalPlayerCastleId => additionalPlayerCastleId;
        public string RelationshipFirstHeroId => relationshipFirstHeroId;
        public string RelationshipSecondHeroId => relationshipSecondHeroId;
        public WIRelationshipLevel RelationshipLevel => relationshipLevel;
    }

    [Serializable]
    public class WIHeroLegacyDefinition
    {
        [SerializeField] private string id;
        [SerializeField] private WICastleProjectType projectType;
        [SerializeField] private WILocalizedString displayName;
        [SerializeField] private WILocalizedString description;
        [SerializeField] private int projectGainBonus = 2;

        public string Id => id;
        public WICastleProjectType ProjectType => projectType;
        public WILocalizedString DisplayName => displayName;
        public WILocalizedString Description => description;
        public int ProjectGainBonus => projectGainBonus;
    }

    [Serializable]
    public class WIRecruitmentEventChoiceDefinition
    {
        [SerializeField] private WILocalizedString label;
        [SerializeField] private WILocalizedString resultDescription;
        [SerializeField] private int requiredReputation;
        [SerializeField] private int requiredMerit;
        [SerializeField] private int requiredFactionCastleCount;
        [SerializeField] private bool requiresNegotiator;
        [SerializeField] private int recruiterMeritGain;
        [SerializeField] private int recruiterFatigueGain;

        public WILocalizedString Label => label;
        public WILocalizedString ResultDescription => resultDescription;
        public int RequiredReputation => requiredReputation;
        public int RequiredMerit => requiredMerit;
        public int RequiredFactionCastleCount => requiredFactionCastleCount;
        public bool RequiresNegotiator => requiresNegotiator;
        public int RecruiterMeritGain => recruiterMeritGain;
        public int RecruiterFatigueGain => recruiterFatigueGain;
    }

    [Serializable]
    public class WIRecruitmentEventDefinition
    {
        [SerializeField] private string id;
        [SerializeField] private WILocalizedString title;
        [SerializeField] private WILocalizedString description;
        [SerializeField] private List<WIRecruitmentEventChoiceDefinition> choices = new List<WIRecruitmentEventChoiceDefinition>();

        public string Id => id;
        public WILocalizedString Title => title;
        public WILocalizedString Description => description;
        public IReadOnlyList<WIRecruitmentEventChoiceDefinition> Choices => choices;
    }

    public enum WITavernQuestType
    {
        Escort,
        Hunt,
        Search,
        Mediation,
        Counterintelligence
    }

    public enum WIQuestAptitude
    {
        Leadership,
        Might,
        Intelligence,
        Charisma,
        Politics
    }

    [Serializable]
    public class WITavernQuestDefinition
    {
        [SerializeField] private WITavernQuestType questType;
        [SerializeField] private WILocalizedString displayName;
        [SerializeField] private WILocalizedString description;
        [SerializeField] private int durationMonths = 1;
        [SerializeField] private WIQuestAptitude aptitude;
        [SerializeField] private int aptitudeThreshold = 60;
        [SerializeField] private int goldReward;
        [SerializeField] private int meritReward;
        [SerializeField] private int reputationReward;
        [SerializeField] private int fatigueCost;
        [SerializeField] private int aptitudeBonusGold;
        [SerializeField] private int castleEffectValue;

        public WITavernQuestType QuestType => questType;
        public WILocalizedString DisplayName => displayName;
        public WILocalizedString Description => description;
        public int DurationMonths => durationMonths;
        public WIQuestAptitude Aptitude => aptitude;
        public int AptitudeThreshold => aptitudeThreshold;
        public int GoldReward => goldReward;
        public int MeritReward => meritReward;
        public int ReputationReward => reputationReward;
        public int FatigueCost => fatigueCost;
        public int AptitudeBonusGold => aptitudeBonusGold;
        public int CastleEffectValue => castleEffectValue;
    }

    public enum WIQuestStatus
    {
        Available,
        Accepted
    }

    public enum WIUnitRole
    {
        Commander,
        Vanguard,
        Melee,
        Ranged,
        Magic,
        Support
    }

    public enum WIUnitProficiency
    {
        Rookie,
        Trained,
        Elite
    }

    public enum WISupplyState
    {
        Sufficient,
        Shortage,
        Depleted
    }

    public enum WIAIStrategy
    {
        Prosperity,
        Development,
        Defense,
        Aggressive,
        Scheme
    }

    public enum WIResearchEffectType
    {
        GoldIncomePercent,
        ManaIncomePerCastle,
        InfluenceIncomePerCastle,
        ProjectGain,
        BattlePower
    }

    public enum WISchemeType
    {
        Investigation,
        Counterintelligence,
        Rumor,
        Alienation
    }

    public enum WICampaignDifficulty
    {
        Standard,
        Relaxed,
        Hard
    }

    [Serializable]
    public class WICampaignDifficultyDefinition
    {
        [SerializeField] private WICampaignDifficulty difficulty;
        [SerializeField] private WILocalizedString displayName;
        [SerializeField] private WILocalizedString description;
        [SerializeField, Range(1, 3)] private int aiCandidateWindow = 2;

        public WICampaignDifficulty Difficulty => difficulty;
        public WILocalizedString DisplayName => displayName;
        public WILocalizedString Description => description;
        public int AICandidateWindow => aiCandidateWindow;
    }

    [Serializable]
    public class WISchemeDefinition
    {
        [SerializeField] private string id;
        [SerializeField] private WILocalizedString displayName;
        [SerializeField] private WILocalizedString description;
        [SerializeField] private WISchemeType schemeType;
        [SerializeField] private int influenceCost = 10;
        [SerializeField] private int baseSuccessChance = 40;
        [SerializeField] private int effectValue = 10;
        [SerializeField] private int durationMonths = 3;
        [SerializeField] private int baseDetectionChance = 25;
        [SerializeField] private int failureDetectionBonus = 20;

        public string Id => id;
        public WILocalizedString DisplayName => displayName;
        public WILocalizedString Description => description;
        public WISchemeType SchemeType => schemeType;
        public int InfluenceCost => influenceCost;
        public int BaseSuccessChance => baseSuccessChance;
        public int EffectValue => effectValue;
        public int DurationMonths => durationMonths;
        public int BaseDetectionChance => baseDetectionChance;
        public int FailureDetectionBonus => failureDetectionBonus;
    }

    [Serializable]
    public class WIResearchDefinition
    {
        [SerializeField] private string id;
        [SerializeField] private WILocalizedString displayName;
        [SerializeField] private WILocalizedString description;
        [SerializeField] private int manaCost = 100;
        [SerializeField] private int requiredTechnology = 30;
        [SerializeField] private int durationMonths = 1;
        [SerializeField] private WIResearchEffectType effectType;
        [SerializeField] private int effectValue = 5;
        [SerializeField] private string prerequisiteResearchId;

        public string Id => id;
        public WILocalizedString DisplayName => displayName;
        public WILocalizedString Description => description;
        public int ManaCost => manaCost;
        public int RequiredTechnology => requiredTechnology;
        public int DurationMonths => durationMonths;
        public WIResearchEffectType EffectType => effectType;
        public int EffectValue => effectValue;
        public string PrerequisiteResearchId => prerequisiteResearchId;
    }

    [Serializable]
    public class WITitleDefinition
    {
        [SerializeField] private string id;
        [SerializeField] private WILocalizedString displayName;
        [SerializeField] private int requiredMerit = 20;
        [SerializeField] private int influenceCost = 20;
        [SerializeField] private int projectBonus;
        [SerializeField] private int battlePowerBonus;

        public string Id => id;
        public WILocalizedString DisplayName => displayName;
        public int RequiredMerit => requiredMerit;
        public int InfluenceCost => influenceCost;
        public int ProjectBonus => projectBonus;
        public int BattlePowerBonus => battlePowerBonus;
    }

    [Serializable]
    public class WILocalizedString
    {
        [SerializeField] private string uid;
        [SerializeField] private string korean;
        [SerializeField] private string english;

        public string Uid => uid;
        public string Korean => korean;
        public string English => english;

        // 현재 언어에 맞는 표시 문자열을 반환합니다.
        public string Get(bool useEnglish)
        {
            return useEnglish == true ? english : korean;
        }
    }

    [Serializable]
    public class WIFactionDefinition
    {
        [SerializeField] private string id;
        [SerializeField] private WILocalizedString displayName;
        [SerializeField] private Color color = Color.white;
        [SerializeField] private Sprite emblem;
        [SerializeField] private bool playerFaction;
        [SerializeField] private WIAIStrategy aiStrategy = WIAIStrategy.Prosperity;
        [SerializeField] private int startingGold = 1200;
        [SerializeField] private int startingManaCrystal = 300;
        [SerializeField] private int startingInfluence = 100;

        public string Id => id;
        public WILocalizedString DisplayName => displayName;
        public Color Color => color;
        public Sprite Emblem => emblem;
        public bool PlayerFaction => playerFaction;
        public WIAIStrategy AIStrategy => aiStrategy;
        public int StartingGold => startingGold;
        public int StartingManaCrystal => startingManaCrystal;
        public int StartingInfluence => startingInfluence;
    }

    [Serializable]
    public class WIHeroDefinition
    {
        [SerializeField] private string id;
        [SerializeField] private WILocalizedString displayName;
        [SerializeField] private WIHeroRace race;
        [SerializeField] private WIHeroClass heroClass;
        [SerializeField] private WICharacterGrade grade = WICharacterGrade.Hero;
        [SerializeField] private List<WITraitType> traits = new List<WITraitType>();
        [SerializeField] private Sprite portrait;
        [SerializeField] private int leadership = 50;
        [SerializeField] private int might = 50;
        [SerializeField] private int intelligence = 50;
        [SerializeField] private int charisma = 50;
        [SerializeField] private int politics = 50;
        [SerializeField] private WILoyaltyState loyaltyState = WILoyaltyState.Stable;
        [SerializeField] private int requiredReputation;
        [SerializeField] private WILocalizedString recruitmentRequest;
        [SerializeField] private bool activeSkillAvailable;
        [SerializeField] private string recruitmentEventId;

        public string Id => id;
        public WILocalizedString DisplayName => displayName;
        public WIHeroRace Race => race;
        public WIHeroClass HeroClass => heroClass;
        public WICharacterGrade Grade => grade;
        public IReadOnlyList<WITraitType> Traits => traits;
        public Sprite Portrait => portrait;
        public int Leadership => leadership;
        public int Might => might;
        public int Intelligence => intelligence;
        public int Charisma => charisma;
        public int Politics => politics;
        public WILoyaltyState LoyaltyState => loyaltyState;
        public int RequiredReputation => requiredReputation;
        public WILocalizedString RecruitmentRequest => recruitmentRequest;
        public bool ActiveSkillAvailable => activeSkillAvailable;
        public string RecruitmentEventId => recruitmentEventId;
    }

    [Serializable]
    public class WISpecialFacilityDefinition
    {
        [SerializeField] private string id;
        [SerializeField] private WILocalizedString displayName;
        [SerializeField] private WILocalizedString description;
        [SerializeField] private Sprite icon;

        public string Id => id;
        public WILocalizedString DisplayName => displayName;
        public WILocalizedString Description => description;
        public Sprite Icon => icon;
    }

    [Serializable]
    public class WICastleDefinition
    {
        [SerializeField] private string id;
        [SerializeField] private WILocalizedString displayName;
        [SerializeField] private WICastleSize castleSize = WICastleSize.Small;
        [SerializeField] private string factionId;
        [SerializeField] private WILocalizedString terrainTrait;
        [SerializeField] private WILocalizedString specialty;
        [SerializeField] private WICastleSpecialtyEffectType specialtyEffectType;
        [SerializeField] private WICastleProjectType specialtyProjectType;
        [SerializeField] private int specialtyEffectValue = 2;
        [SerializeField] private Vector2 normalizedMapPosition;
        [SerializeField] private List<string> adjacentCastleIds = new List<string>();
        [SerializeField, Range(0, 100)] private int initialProsperity = 30;
        [SerializeField, Range(0, 100)] private int initialTechnology = 20;
        [SerializeField, Range(0, 100)] private int initialStability = 40;
        [SerializeField, Range(0, 100)] private int initialDefense = 25;
        [SerializeField] private Sprite castleImage;

        public string Id => id;
        public WILocalizedString DisplayName => displayName;
        public WICastleSize CastleSize => castleSize;
        public string FactionId => factionId;
        public WILocalizedString TerrainTrait => terrainTrait;
        public WILocalizedString Specialty => specialty;
        public WICastleSpecialtyEffectType SpecialtyEffectType => specialtyEffectType;
        public WICastleProjectType SpecialtyProjectType => specialtyProjectType;
        public int SpecialtyEffectValue => specialtyEffectValue;
        public Vector2 NormalizedMapPosition => normalizedMapPosition;
        public IReadOnlyList<string> AdjacentCastleIds => adjacentCastleIds;
        public int InitialProsperity => initialProsperity;
        public int InitialTechnology => initialTechnology;
        public int InitialStability => initialStability;
        public int InitialDefense => initialDefense;
        public Sprite CastleImage => castleImage;
    }

    [Serializable]
    public class WIStartingHeroPlacement
    {
        [SerializeField] private string castleId;
        [SerializeField] private string heroId;
        [SerializeField] private bool governor;

        public string CastleId => castleId;
        public string HeroId => heroId;
        public bool Governor => governor;
    }

    public enum WIHeroStatType
    {
        Leadership,
        Might,
        Intelligence,
        Charisma,
        Politics
    }

    [Serializable]
    public class WIHeroClassDefinition
    {
        [SerializeField] private WIHeroClass heroClass;
        [SerializeField] private WILocalizedString displayName;
        [SerializeField] private WILocalizedString description;
        [SerializeField] private WIHeroStatType primaryStat;
        [SerializeField] private WIHeroStatType secondaryStat;
        [SerializeField] private WIUnitRole recommendedRole = WIUnitRole.Melee;

        public WIHeroClass HeroClass => heroClass;
        public WILocalizedString DisplayName => displayName;
        public WILocalizedString Description => description;
        public WIHeroStatType PrimaryStat => primaryStat;
        public WIHeroStatType SecondaryStat => secondaryStat;
        public WIUnitRole RecommendedRole => recommendedRole;
    }

    [Serializable]
    public class WITutorialDefinition
    {
        [SerializeField] private string id;
        [SerializeField, Range(1, 12)] private int month = 1;
        [SerializeField] private WILocalizedString title;
        [SerializeField] private WILocalizedString description;

        public string Id => id;
        public int Month => month;
        public WILocalizedString Title => title;
        public WILocalizedString Description => description;
    }

    [Serializable]
    public class WICampaignRuleDefinition
    {
        [SerializeField] private bool victoryRequiresAllCastles = true;
        [SerializeField] private bool defeatWhenNoCastles = true;
        [SerializeField] private WILocalizedString victoryTitle;
        [SerializeField] private WILocalizedString victoryDescription;
        [SerializeField] private WILocalizedString defeatTitle;
        [SerializeField] private WILocalizedString defeatDescription;

        public bool VictoryRequiresAllCastles => victoryRequiresAllCastles;
        public bool DefeatWhenNoCastles => defeatWhenNoCastles;
        public WILocalizedString VictoryTitle => victoryTitle;
        public WILocalizedString VictoryDescription => victoryDescription;
        public WILocalizedString DefeatTitle => defeatTitle;
        public WILocalizedString DefeatDescription => defeatDescription;
    }

    public enum WICampaignObjectiveType
    {
        CastleProsperity,
        PlayerCastleCount,
        FactionEliminated,
        ContinentalUnification
    }

    [Serializable]
    public class WICampaignObjectiveDefinition
    {
        [SerializeField] private string id;
        [SerializeField] private WILocalizedString title;
        [SerializeField] private WILocalizedString situation;
        [SerializeField] private WILocalizedString description;
        [SerializeField] private WICampaignObjectiveType objectiveType;
        [SerializeField] private string targetCastleId;
        [SerializeField] private string targetFactionId;
        [SerializeField] private int targetValue;
        [SerializeField] private int rewardGold;
        [SerializeField] private int rewardMana;
        [SerializeField] private int rewardInfluence;

        public string Id => id;
        public WILocalizedString Title => title;
        public WILocalizedString Situation => situation;
        public WILocalizedString Description => description;
        public WICampaignObjectiveType ObjectiveType => objectiveType;
        public string TargetCastleId => targetCastleId;
        public string TargetFactionId => targetFactionId;
        public int TargetValue => targetValue;
        public int RewardGold => rewardGold;
        public int RewardMana => rewardMana;
        public int RewardInfluence => rewardInfluence;
    }

    [Serializable]
    public class WIProjectBalanceDefinition
    {
        [SerializeField] private int basicCost = 100;
        [SerializeField] private int intensiveCost = 180;
        [SerializeField] private int expansionBasicCost = 600;
        [SerializeField] private int baseGain = 4;
        [SerializeField] private int statDivisor = 20;
        [SerializeField] private int intensiveGainBonus = 4;
        [SerializeField] private int traitGainBonus = 2;
        [SerializeField] private int policyGainBonus = 2;
        [SerializeField] private int minimumGain = 4;
        [SerializeField] private int maximumGain = 16;
        [SerializeField] private int expansionDurationMonths = 3;

        public int BasicCost => basicCost;
        public int IntensiveCost => intensiveCost;
        public int ExpansionBasicCost => expansionBasicCost;
        public int BaseGain => baseGain;
        public int StatDivisor => statDivisor;
        public int IntensiveGainBonus => intensiveGainBonus;
        public int TraitGainBonus => traitGainBonus;
        public int PolicyGainBonus => policyGainBonus;
        public int MinimumGain => minimumGain;
        public int MaximumGain => maximumGain;
        public int ExpansionDurationMonths => expansionDurationMonths;
    }

    [Serializable]
    public class WITraitDefinition
    {
        [SerializeField] private WITraitType traitType;
        [SerializeField] private WILocalizedString displayName;
        [SerializeField] private WILocalizedString description;
        [SerializeField] private List<WICastleProjectType> projectTypes = new List<WICastleProjectType>();
        [SerializeField] private int uniqueEffectValue = 10;

        public WITraitType TraitType => traitType;
        public WILocalizedString DisplayName => displayName;
        public WILocalizedString Description => description;
        public IReadOnlyList<WICastleProjectType> ProjectTypes => projectTypes;
        public int UniqueEffectValue => uniqueEffectValue;
    }

    [CreateAssetMenu(fileName = "WI_AdministrationDatabase", menuName = "WI/Administration/Database")]
    public class WIAdministrationDatabaseSO : ScriptableObject
    {
        [Header("표시 설정")]
        [SerializeField] private bool useEnglish;
        [SerializeField] private WILocalizedString gameTitle;
        [SerializeField] private Sprite globalMapImage;
        [SerializeField] private List<WILocalizedString> uiStrings = new List<WILocalizedString>();

        [Header("마스터 데이터")]
        [SerializeField] private List<WIFactionDefinition> factions = new List<WIFactionDefinition>();
        [SerializeField] private List<WICastleDefinition> castles = new List<WICastleDefinition>();
        [SerializeField] private List<WIHeroDefinition> heroes = new List<WIHeroDefinition>();
        [SerializeField] private List<WIHeroClassDefinition> heroClassDefinitions = new List<WIHeroClassDefinition>();
        [SerializeField] private List<WISpecialFacilityDefinition> specialFacilities = new List<WISpecialFacilityDefinition>();
        [SerializeField] private List<WIResearchDefinition> researchDefinitions = new List<WIResearchDefinition>();
        [SerializeField] private List<WITitleDefinition> titleDefinitions = new List<WITitleDefinition>();
        [SerializeField] private List<WISchemeDefinition> schemeDefinitions = new List<WISchemeDefinition>();
        [SerializeField] private List<WICampaignDifficultyDefinition> difficultyDefinitions = new List<WICampaignDifficultyDefinition>();
        [SerializeField] private List<WITutorialDefinition> tutorialDefinitions = new List<WITutorialDefinition>();
        [SerializeField] private WICampaignRuleDefinition campaignRules = new WICampaignRuleDefinition();
        [SerializeField] private List<WICampaignObjectiveDefinition> campaignObjectives = new List<WICampaignObjectiveDefinition>();
        [SerializeField] private WIProjectBalanceDefinition projectBalance = new WIProjectBalanceDefinition();
        [SerializeField] private List<WITraitDefinition> traitDefinitions = new List<WITraitDefinition>();
        [SerializeField] private List<WIRelationshipEventDefinition> relationshipEventDefinitions = new List<WIRelationshipEventDefinition>();
        [SerializeField] private List<WIStartingRelationshipDefinition> startingRelationships = new List<WIStartingRelationshipDefinition>();
        [SerializeField] private List<WIRegionalEventDefinition> regionalEventDefinitions = new List<WIRegionalEventDefinition>();
        [SerializeField] private List<WIOccupationChoiceDefinition> occupationChoices = new List<WIOccupationChoiceDefinition>();
        [SerializeField] private List<WIFactionEliminationNarrativeDefinition> factionEliminationNarratives = new List<WIFactionEliminationNarrativeDefinition>();
        [SerializeField] private List<WICampaignEndingDefinition> campaignEndings = new List<WICampaignEndingDefinition>();
        [SerializeField] private List<WICampaignVariantDefinition> campaignVariants = new List<WICampaignVariantDefinition>();
        [SerializeField] private List<WIHeroLegacyDefinition> heroLegacyDefinitions = new List<WIHeroLegacyDefinition>();
        [SerializeField] private List<WIRecruitmentEventDefinition> recruitmentEventDefinitions = new List<WIRecruitmentEventDefinition>();
        [SerializeField] private List<WITavernQuestDefinition> tavernQuestDefinitions = new List<WITavernQuestDefinition>();

        [Header("시작 상태")]
        [SerializeField] private int startingYear = 1;
        [SerializeField] private int startingMonth = 1;
        [SerializeField] private int startingGold = 1200;
        [SerializeField] private int startingManaCrystal = 300;
        [SerializeField] private int startingInfluence = 100;
        [SerializeField] private List<WIStartingHeroPlacement> startingHeroes = new List<WIStartingHeroPlacement>();
        [SerializeField] private int capturePowerMargin = 50;
        [SerializeField] private int captureDurationMonths = 3;
        [SerializeField] private int prisonerRansomGold = 150;
        [SerializeField] private int jointAttackInfluenceCost = 20;
        [SerializeField] private int jointAttackDurationMonths = 3;
        [SerializeField] private bool permanentDeathEnabled;
        [SerializeField] private int battleVictoryMerit = 10;
        [SerializeField] private int battleVictoryExperience = 15;
        [SerializeField] private int battleDefeatExperience = 8;
        [SerializeField] private int battleVictoryFatigue = 15;
        [SerializeField] private int battleDefeatFatigue = 25;
        [SerializeField] private int orderlyRetreatFatigue = 12;
        [SerializeField] private int battleInjuryPowerMargin = 30;
        [SerializeField] private int battleInjuryMonths = 1;
        [SerializeField] private int battleBondVictoryThreshold = 2;
        [SerializeField] private int promotionRequiredMerit = 80;
        [SerializeField] private int promotionRequiredReputation = 30;
        [SerializeField] private int promotionInfluenceCost = 30;

        public bool UseEnglish => useEnglish;
        public Sprite GlobalMapImage => globalMapImage;

        // 시스템 언어 설정에 따라 런타임 표시 언어를 변경합니다.
        public void SetUseEnglish(bool value)
        {
            useEnglish = value;
        }
        public WILocalizedString GameTitle => gameTitle;
        public IReadOnlyList<WIFactionDefinition> Factions => factions;
        public IReadOnlyList<WICastleDefinition> Castles => castles;
        public IReadOnlyList<WIHeroDefinition> Heroes => heroes;
        public IReadOnlyList<WIHeroClassDefinition> HeroClassDefinitions => heroClassDefinitions;
        public IReadOnlyList<WISpecialFacilityDefinition> SpecialFacilities => specialFacilities;
        public IReadOnlyList<WIResearchDefinition> ResearchDefinitions => researchDefinitions;
        public IReadOnlyList<WITitleDefinition> TitleDefinitions => titleDefinitions;
        public IReadOnlyList<WISchemeDefinition> SchemeDefinitions => schemeDefinitions;
        public IReadOnlyList<WICampaignDifficultyDefinition> DifficultyDefinitions => difficultyDefinitions;
        public IReadOnlyList<WITutorialDefinition> TutorialDefinitions => tutorialDefinitions;
        public WICampaignRuleDefinition CampaignRules => campaignRules;
        public IReadOnlyList<WICampaignObjectiveDefinition> CampaignObjectives => campaignObjectives;

        // 식별자로 캠페인 목표 정의를 찾습니다.
        public WICampaignObjectiveDefinition GetCampaignObjective(string id)
        {
            return campaignObjectives.Find(item => item.Id == id);
        }
        public WIProjectBalanceDefinition ProjectBalance => projectBalance;
        public IReadOnlyList<WITraitDefinition> TraitDefinitions => traitDefinitions;
        public IReadOnlyList<WIRelationshipEventDefinition> RelationshipEventDefinitions => relationshipEventDefinitions;
        public IReadOnlyList<WIStartingRelationshipDefinition> StartingRelationships => startingRelationships;
        public IReadOnlyList<WIRegionalEventDefinition> RegionalEventDefinitions => regionalEventDefinitions;
        public IReadOnlyList<WIOccupationChoiceDefinition> OccupationChoices => occupationChoices;
        public IReadOnlyList<WIFactionEliminationNarrativeDefinition> FactionEliminationNarratives => factionEliminationNarratives;
        public IReadOnlyList<WICampaignEndingDefinition> CampaignEndings => campaignEndings;
        public IReadOnlyList<WICampaignVariantDefinition> CampaignVariants => campaignVariants;

        // 시작 변형 유형에 대응하는 캠페인 설정을 찾습니다.
        public WICampaignVariantDefinition GetCampaignVariant(WICampaignVariant variant)
        {
            return campaignVariants.Find(item => item.Variant == variant);
        }

        // 결말 유형에 대응하는 제목과 설명을 찾습니다.
        public WICampaignEndingDefinition GetCampaignEnding(WICampaignEndingType endingType)
        {
            return campaignEndings.Find(item => item.EndingType == endingType);
        }

        // 세력 ID에 대응하는 멸망 사건 서사를 찾습니다.
        public WIFactionEliminationNarrativeDefinition GetFactionEliminationNarrative(string factionId)
        {
            return factionEliminationNarratives.Find(item => item.FactionId == factionId);
        }

        // 식별자로 지역·세력 사건 정의를 찾습니다.
        public WIRegionalEventDefinition GetRegionalEvent(string id)
        {
            return regionalEventDefinitions.Find(item => item.Id == id);
        }
        public IReadOnlyList<WIHeroLegacyDefinition> HeroLegacyDefinitions => heroLegacyDefinitions;
        public IReadOnlyList<WIRecruitmentEventDefinition> RecruitmentEventDefinitions => recruitmentEventDefinitions;
        public IReadOnlyList<WITavernQuestDefinition> TavernQuestDefinitions => tavernQuestDefinitions;
        public int StartingYear => startingYear;
        public int StartingMonth => startingMonth;
        public int StartingGold => startingGold;
        public int StartingManaCrystal => startingManaCrystal;
        public int StartingInfluence => startingInfluence;
        public IReadOnlyList<WIStartingHeroPlacement> StartingHeroes => startingHeroes;
        public int CapturePowerMargin => capturePowerMargin;
        public int CaptureDurationMonths => captureDurationMonths;
        public int PrisonerRansomGold => prisonerRansomGold;
        public int JointAttackInfluenceCost => jointAttackInfluenceCost;
        public int JointAttackDurationMonths => jointAttackDurationMonths;
        public bool PermanentDeathEnabled => permanentDeathEnabled;
        public int BattleVictoryMerit => battleVictoryMerit;
        public int BattleVictoryExperience => battleVictoryExperience;
        public int BattleDefeatExperience => battleDefeatExperience;
        public int BattleVictoryFatigue => battleVictoryFatigue;
        public int BattleDefeatFatigue => battleDefeatFatigue;
        public int OrderlyRetreatFatigue => orderlyRetreatFatigue;
        public int BattleInjuryPowerMargin => battleInjuryPowerMargin;
        public int BattleInjuryMonths => battleInjuryMonths;
        public int BattleBondVictoryThreshold => battleBondVictoryThreshold;
        public int PromotionRequiredMerit => promotionRequiredMerit;
        public int PromotionRequiredReputation => promotionRequiredReputation;
        public int PromotionInfluenceCost => promotionInfluenceCost;

        // 문자열 UID로 현지화된 표시 문구를 찾습니다.
        public string GetText(string uid)
        {
            WILocalizedString value = uiStrings.Find(item => item.Uid == uid);
            return value == null ? uid : value.Get(useEnglish);
        }

        // ID로 세력 데이터를 찾습니다.
        public WIFactionDefinition GetFaction(string id)
        {
            return factions.Find(item => item.Id == id);
        }

        // 열거형에 대응하는 특기 표시·적용 사업 데이터를 찾습니다.
        public WITraitDefinition GetTrait(WITraitType traitType)
        {
            return traitDefinitions.Find(item => item.TraitType == traitType);
        }

        // 식별자로 관계 사건 정의를 반환합니다.
        public WIRelationshipEventDefinition GetRelationshipEvent(string id)
        {
            return relationshipEventDefinitions.Find(item => item.Id == id);
        }

        // 식별자로 영웅의 흔적 정의를 반환합니다.
        public WIHeroLegacyDefinition GetHeroLegacy(string id)
        {
            return heroLegacyDefinitions.Find(item => item.Id == id);
        }

        // 사업 종류에 맞는 영웅의 흔적 정의를 반환합니다.
        public WIHeroLegacyDefinition GetHeroLegacy(WICastleProjectType projectType)
        {
            return heroLegacyDefinitions.Find(item => item.ProjectType == projectType);
        }

        // 식별자로 등용 요구 사건 정의를 반환합니다.
        public WIRecruitmentEventDefinition GetRecruitmentEvent(string id)
        {
            return recruitmentEventDefinitions.Find(item => item.Id == id);
        }

        // 의뢰 종류에 맞는 선술집 의뢰 정의를 반환합니다.
        public WITavernQuestDefinition GetTavernQuest(WITavernQuestType questType)
        {
            return tavernQuestDefinitions.Find(item => item.QuestType == questType);
        }

        // ID로 연구 정의를 찾습니다.
        public WIResearchDefinition GetResearch(string id)
        {
            return researchDefinitions.Find(item => item.Id == id);
        }

        // ID로 작위 정의를 찾습니다.
        public WITitleDefinition GetTitle(string id)
        {
            return titleDefinitions.Find(item => item.Id == id);
        }

        // ID로 계략 정의를 찾습니다.
        public WISchemeDefinition GetScheme(string id)
        {
            return schemeDefinitions.Find(item => item.Id == id);
        }

        // 난이도 값으로 캠페인 난이도 정의를 찾습니다.
        public WICampaignDifficultyDefinition GetDifficulty(WICampaignDifficulty difficulty)
        {
            return difficultyDefinitions.Find(item => item.Difficulty == difficulty);
        }

        // ID로 성 데이터를 찾습니다.
        public WICastleDefinition GetCastle(string id)
        {
            return castles.Find(item => item.Id == id);
        }

        // ID로 영웅 데이터를 찾습니다.
        public WIHeroDefinition GetHero(string id)
        {
            return heroes.Find(item => item.Id == id);
        }

        // 직업 열거형에 대응하는 능력치 성향과 권장 역할을 찾습니다.
        public WIHeroClassDefinition GetHeroClass(WIHeroClass heroClass)
        {
            return heroClassDefinitions.Find(item => item.HeroClass == heroClass);
        }

        // ID로 특화 시설 데이터를 찾습니다.
        public WISpecialFacilityDefinition GetSpecialFacility(string id)
        {
            return specialFacilities.Find(item => item.Id == id);
        }
    }
}

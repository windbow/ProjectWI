using UnityEngine;
using System;
using System.Collections.Generic;
using ProjectWI.Administration;

namespace ProjectWI.Battle
{
    public enum WIBattleSkillType
    {
        AreaDamage,
        HealAllies,
        CommandBuff
    }

    public enum WIBattleObjectiveType
    {
        Elimination,
        TimedDefense,
        ControlPoint
    }

    public enum WIBattleZoomLevel
    {
        A,
        B,
        C
    }

    [Serializable]
    public class WIBattleObjectiveDefinition
    {
        [SerializeField] private string id;
        [SerializeField] private WILocalizedString displayName;
        [SerializeField] private WILocalizedString description;
        [SerializeField] private WIBattleObjectiveType objectiveType;
        [SerializeField, Min(1f)] private float durationSeconds = 90f;
        [SerializeField, Min(1f)] private float controlDurationSeconds = 20f;
        [SerializeField, Min(0.5f)] private float controlRadius = 2f;

        public string Id => id;
        public WILocalizedString DisplayName => displayName;
        public WILocalizedString Description => description;
        public WIBattleObjectiveType ObjectiveType => objectiveType;
        public float DurationSeconds => Mathf.Max(1f, durationSeconds);
        public float ControlDurationSeconds => Mathf.Max(1f, controlDurationSeconds);
        public float ControlRadius => Mathf.Max(0.5f, controlRadius);
    }

    [Serializable]
    public class WIBattleSkillDefinition
    {
        [SerializeField] private string heroId;
        [SerializeField] private string displayName;
        [SerializeField] private string description;
        [SerializeField] private WIBattleSkillType skillType;
        [SerializeField] private int manaCost = 40;
        [SerializeField] private int power = 35;
        [SerializeField] private float range = 4f;
        [SerializeField] private float cooldown = 10f;
        // 범위 피해 스킬을 지정 위치에 떨어뜨릴 때의 효과 반경입니다. Range는 시전 가능 거리로 사용합니다.
        [SerializeField, Min(0.5f)] private float areaRadius = 2f;

        public string HeroId => heroId;
        public string DisplayName => displayName;
        public string Description => description;
        public WIBattleSkillType SkillType => skillType;
        public int ManaCost => manaCost;
        public int Power => power;
        public float Range => range;
        public float Cooldown => cooldown;
        public float AreaRadius => Mathf.Max(0.5f, areaRadius);
        // 위치를 지정해 시전하는 스킬인지 여부입니다.
        public bool RequiresTarget => skillType == WIBattleSkillType.AreaDamage;
    }

    // 전장 지형 구역 종류입니다.
    public enum WIBattleTerrainType
    {
        // 원거리 사거리·피해 증가
        HighGround,
        // 투사체 피해 감소, 이동 속도 감소
        Forest,
        // 근접 교전 슬롯 수 감소
        Narrow
    }

    // 전장 위의 원형 지형 구역 정의입니다.
    [Serializable]
    public class WIBattleTerrainZoneDefinition
    {
        [SerializeField] private WIBattleTerrainType terrainType;
        [SerializeField] private Vector2 center;
        [SerializeField, Min(0.5f)] private float radius = 1.8f;

        public WIBattleTerrainZoneDefinition()
        {
        }

        public WIBattleTerrainZoneDefinition(WIBattleTerrainType terrainType, Vector2 center, float radius)
        {
            this.terrainType = terrainType;
            this.center = center;
            this.radius = radius;
        }

        public WIBattleTerrainType TerrainType => terrainType;
        public Vector2 Center => center;
        public float Radius => Mathf.Max(0.5f, radius);
    }

    [CreateAssetMenu(fileName = "WI_BattleConfig", menuName = "WI/Battle/Config")]
    public class WIBattleConfigSO : ScriptableObject
    {
        // 공격 종류와 실제 피격을 표시하는 사전 제작 파티클 프리팹입니다.
        [Header("전투 이펙트")]
        [SerializeField] private GameObject slashEffectPrefab;
        [SerializeField] private GameObject arrowEffectPrefab;
        [SerializeField] private GameObject magicEffectPrefab;
        [SerializeField] private GameObject hitEffectPrefab;
        // 전장 좌표 기준 이펙트 크기와 캐릭터 몸통 표시 높이입니다.
        [SerializeField, Min(0.01f)] private float slashEffectScale = 0.9f;
        [SerializeField, Min(0.01f)] private float projectileEffectScale = 0.8f;
        [SerializeField, Min(0.01f)] private float hitEffectScale = 0.7f;
        [SerializeField] private float effectHeight = 0.45f;
        // 단발 프리팹의 시각 재생 시간이며 피해 판정에는 영향을 주지 않습니다.
        [SerializeField, Min(0.01f)] private float slashEffectDuration = 0.285f;
        [SerializeField, Min(0.01f)] private float hitEffectDuration = 0.32f;
        public GameObject SlashEffectPrefab => slashEffectPrefab;
        public GameObject ArrowEffectPrefab => arrowEffectPrefab;
        public GameObject MagicEffectPrefab => magicEffectPrefab;
        public GameObject HitEffectPrefab => hitEffectPrefab;
        public float SlashEffectScale => slashEffectScale;
        public float ProjectileEffectScale => projectileEffectScale;
        public float HitEffectScale => hitEffectScale;
        public float EffectHeight => effectHeight;
        public float SlashEffectDuration => Mathf.Max(0.01f, slashEffectDuration);
        public float HitEffectDuration => Mathf.Max(0.01f, hitEffectDuration);
        [SerializeField] private Vector2 arenaSize = new Vector2(18f, 10f);
        [SerializeField] private Vector2 arenaBackgroundSize = new Vector2(57.024f, 32.076f);
        [SerializeField] private float formationColumnSpacing = 1.4f;
        [SerializeField] private float formationRowSpacing = 1.2f;
        // 연속 이동 모드의 고정 시뮬레이션 틱 간격(초)입니다.
        [SerializeField, Min(0.01f)] private float fixedTickSeconds = 0.05f;
        // 한 인물을 동시에 근접 공격할 수 있는 교전 슬롯 수입니다.
        [SerializeField, Range(1, 8)] private int meleeSlotCount = 4;
        // 근접 사거리 대비 교전 슬롯이 놓이는 거리 비율입니다.
        [SerializeField, Range(0.5f, 1f)] private float meleeSlotDistanceRatio = 0.8f;
        // 교전 슬롯 사이 각도 간격(도)입니다.
        [SerializeField, Range(20f, 90f)] private float meleeSlotAngleStep = 45f;
        // 표적을 다시 고르는 주기(초)입니다.
        [SerializeField, Min(0.05f)] private float retargetInterval = 0.5f;
        // 슬롯이 가득 찬 적을 표적으로 고를 때 거리에 더하는 불이익입니다.
        [SerializeField, Min(0f)] private float fullSlotTargetPenalty = 3f;
        // 원거리 인물이 멈춰 사격하는 사거리 비율입니다.
        [SerializeField, Range(0.3f, 1f)] private float rangedPreferredRangeRatio = 0.85f;
        // 원거리 인물이 재장전 중 뒤로 물러나기 시작하는 적과의 거리입니다.
        [SerializeField, Min(0f)] private float rangedRetreatDistance = 1.6f;
        // 교전 중인 인물이 겹침 해소에서 밀리지 않도록 주는 질량 배수입니다.
        [SerializeField, Min(1f)] private float engagedCollisionMass = 3f;
        // 후열 보호 명령에서 원거리·지원 인물 주변 위협으로 판단하는 적 거리입니다.
        [SerializeField, Min(0.5f)] private float protectThreatRadius = 3f;
        // 후열 보호 명령에서 전위가 보호 대상 앞에 서는 거리입니다.
        [SerializeField, Min(0f)] private float protectGuardDistance = 1.2f;
        // 분산 명령에서 같은 진영 인물 최소 간격에 곱하는 배율입니다.
        [SerializeField, Min(1f)] private float spreadSpacingMultiplier = 2f;
        // 집결 명령에서 원래 진형 간격을 대장 주변으로 축소하는 비율입니다.
        [SerializeField, Range(0.1f, 1f)] private float rallyFormationScale = 0.5f;
        // 후퇴 인물이 전장 가장자리에서 이 거리 안에 들어오면 이탈 처리합니다.
        [SerializeField, Min(0.05f)] private float retreatEscapeMargin = 0.3f;
        // 대상의 옆에서 근접 공격할 때 피해 배율입니다.
        [SerializeField, Min(1f)] private float flankDamageMultiplier = 1.25f;
        // 대상의 등 뒤에서 근접 공격할 때 피해 배율입니다.
        [SerializeField, Min(1f)] private float rearDamageMultiplier = 1.5f;
        // 대상이 바라보는 방향과 공격자 방향의 내적이 이 값 이상이면 정면, 음수 이 값 이하면 후방으로 판정합니다.
        [SerializeField, Range(0f, 1f)] private float flankFrontDot = 0.5f;
        // 이동 명령 분대원이 목적지에 도착했다고 보는 거리입니다.
        [SerializeField, Min(0.05f)] private float moveOrderArrivalDistance = 0.3f;
        // 분대원이 쓰러질 때 분대 사기 감소량입니다.
        [SerializeField, Min(0f)] private float moraleLossPerAllyDown = 10f;
        // 분대장이 쓰러질 때 그 분대의 추가 사기 감소량입니다.
        [SerializeField, Min(0f)] private float moraleLossLeaderDown = 35f;
        // 분대장이 쓰러질 때 같은 진영 다른 분대의 사기 감소량입니다.
        [SerializeField, Min(0f)] private float moraleLossSideOnLeaderDown = 8f;
        // 측면 근접 피격 1회당 분대 사기 감소량입니다.
        [SerializeField, Min(0f)] private float moraleLossFlankHit = 1f;
        // 후방 근접 피격 1회당 분대 사기 감소량입니다.
        [SerializeField, Min(0f)] private float moraleLossRearHit = 2.5f;
        // 무너지지 않은 분대의 초당 사기 회복량입니다.
        [SerializeField, Min(0f)] private float moraleRegenPerSecond = 1f;
        // 무너진 분대의 초당 사기 회복량입니다.
        [SerializeField, Min(0f)] private float routRecoveryPerSecond = 4f;
        // 무너진 분대가 분대장 생존 시 전선에 복귀하는 사기입니다.
        [SerializeField, Range(1f, 100f)] private float routRecoverThreshold = 40f;
        // 전투 시작 전에 분대를 배치하는 단계를 사용할지 여부입니다.
        [SerializeField] private bool useDeploymentPhase = true;
        // 자기 진영 가장자리에서 배치 가능한 전장 가로 비율입니다.
        [SerializeField, Range(0.1f, 0.5f)] private float deploymentZoneDepthRatio = 0.4f;
        // 직접 지휘 중 분대원이 영웅 곁 자기 자리에서 이 거리보다 멀어지면 교전을 끊고 따라갑니다.
        [SerializeField, Min(0.2f)] private float followSlackDistance = 1.2f;
        // 전장 지형 구역 목록입니다.
        [SerializeField] private List<WIBattleTerrainZoneDefinition> terrainZones = new List<WIBattleTerrainZoneDefinition>
        {
            new WIBattleTerrainZoneDefinition(WIBattleTerrainType.HighGround, new Vector2(0f, 3.2f), 1.8f),
            new WIBattleTerrainZoneDefinition(WIBattleTerrainType.Forest, new Vector2(-2.5f, -3f), 1.8f),
            new WIBattleTerrainZoneDefinition(WIBattleTerrainType.Narrow, new Vector2(3f, -3f), 1.5f)
        };
        // 고지에 선 원거리 인물의 사거리 배율입니다.
        [SerializeField, Min(1f)] private float highGroundRangeMultiplier = 1.25f;
        // 고지에 선 원거리 인물의 피해 배율입니다.
        [SerializeField, Min(1f)] private float highGroundDamageMultiplier = 1.1f;
        // 숲 안의 인물이 받는 투사체 피해 배율입니다.
        [SerializeField, Range(0.1f, 1f)] private float forestProjectileDamageMultiplier = 0.6f;
        // 숲 안의 인물 이동 속도 배율입니다.
        [SerializeField, Range(0.1f, 1f)] private float forestMoveSpeedMultiplier = 0.8f;
        // 좁은 길 안의 인물을 동시에 근접 공격할 수 있는 수입니다.
        [SerializeField, Range(1, 8)] private int narrowMeleeSlotCount = 2;
        // 지형 구역 표시 색입니다.
        [SerializeField] private Color highGroundZoneColor = new Color(0.95f, 0.8f, 0.35f, 0.35f);
        [SerializeField] private Color forestZoneColor = new Color(0.25f, 0.75f, 0.3f, 0.35f);
        [SerializeField] private Color narrowZoneColor = new Color(0.6f, 0.6f, 0.65f, 0.35f);
        // 배치 단계에서 아군 배치 가능 구역 표시 색입니다.
        [SerializeField] private Color deploymentZoneColor = new Color(0.4f, 0.7f, 1f, 0.18f);
        [SerializeField] private int baseHealth = 100;
        // 이동 잔여 한 달을 전투 중 증원 대기 시간으로 환산하는 초입니다.
        [SerializeField, Min(1f)] private float reinforcementSecondsPerMonth = 15f;
        public float ReinforcementSecondsPerMonth => Mathf.Max(1f, reinforcementSecondsPerMonth);
        [SerializeField] private int healthPerMight = 3;
        [SerializeField] private int baseMana = 30;
        [SerializeField] private int manaPerIntelligence = 2;
        [SerializeField] private float moveSpeed = 2.5f;
        [SerializeField] private float meleeRange = 1.2f;
        [SerializeField] private float rangedRange = 5f;
        [SerializeField] private float attackCooldown = 1f;
        [SerializeField] private int baseDamage = 8;
        [SerializeField] private float advanceSpeedMultiplier = 1.25f;
        [SerializeField] private float holdDamageReduction = 0.25f;
        [SerializeField] private float focusDamageMultiplier = 1.2f;
        [SerializeField] private float minimumUnitSpacing = 0.75f;
        [SerializeField] private float collisionResolveStrength = 0.8f;
        [SerializeField] private float meleeKnockbackDistance = 0.35f;
        [SerializeField] private float holdKnockbackResistance = 0.7f;
        [SerializeField] private float formationReturnSpeed = 2.5f;
        [SerializeField] private float placeholderCharacterSize = 0.65f;
        [SerializeField] private float battleSpriteScale = 1f;
        [SerializeField] private bool showCharacterHealthBars;
        // 전투 캐릭터 원본 Sprite가 오른쪽을 바라보도록 그려졌는지 여부입니다. 현재 아트는 왼쪽을 바라봅니다.
        [SerializeField] private bool battleSpriteFacesRight;
        // 진영 구분용 발밑 그림자 색의 불투명도입니다. 색은 진영 임시 색상을 사용합니다.
        [SerializeField, Range(0f, 1f)] private float sideMarkerAlpha = 0.75f;
        // 무너져 퇴각 중인 인물 이미지에 곱하는 색입니다.
        [SerializeField] private Color routingTint = new Color(0.6f, 0.6f, 0.6f, 0.85f);
        // 스킬 시전 가능 거리 미리보기 색입니다.
        [SerializeField] private Color skillRangePreviewColor = new Color(1f, 1f, 1f, 0.12f);
        // 스킬 효과 범위 미리보기 색입니다.
        [SerializeField] private Color skillAreaPreviewColor = new Color(0.95f, 0.3f, 0.15f, 0.35f);
        [SerializeField] private bool showCharacterLabels;
        [SerializeField] private float placeholderProjectileSize = 0.18f;
        [SerializeField] private float placeholderAttackEffectDuration = 0.22f;
        [SerializeField] private Color attackerPlaceholderColor = new Color(0.82f, 0.2f, 0.16f);
        [SerializeField] private Color defenderPlaceholderColor = new Color(0.16f, 0.42f, 0.85f);
        [SerializeField] private float projectileSpeed = 8f;
        [SerializeField] private float projectileLifetime = 2.5f;
        [SerializeField] private float projectileCollisionRadius = 0.28f;
        [SerializeField] private bool projectileFriendlyFireEnabled;
        [SerializeField] private float projectileFriendlyFireSafeDistance = 0.8f;
        [SerializeField] private float skillVisualDuration = 0.65f;
        [SerializeField] private float cameraPanSpeed = 7f;
        [SerializeField] private float cameraMinimumZoom = 3.5f;
        [SerializeField] private float cameraMiddleZoom = 8f;
        [SerializeField] private float cameraMaximumZoom = 12f;
        [SerializeField] private float characterSelectionRadius = 0.8f;
        [SerializeField] private List<WIBattleSkillDefinition> heroSkills = new List<WIBattleSkillDefinition>();
        [SerializeField] private List<WIBattleObjectiveDefinition> battleObjectives = new List<WIBattleObjectiveDefinition>();
        [SerializeField] private Sprite arenaBackground;
        [SerializeField] private GameObject arenaPrefab;
        [SerializeField] private Sprite placeholderSprite;
        [SerializeField] private Material characterDefaultMaterial;
        [SerializeField] private Material characterFarOutlineMaterial;

        public Vector2 ArenaSize => arenaSize;
        public Vector2 ArenaBackgroundSize => arenaBackgroundSize;
        public float FormationColumnSpacing => formationColumnSpacing;
        public float FormationRowSpacing => formationRowSpacing;
        public float FixedTickSeconds => Mathf.Max(0.01f, fixedTickSeconds);
        public int MeleeSlotCount => Mathf.Clamp(meleeSlotCount, 1, 8);
        public float MeleeSlotDistanceRatio => meleeSlotDistanceRatio;
        public float MeleeSlotAngleStep => meleeSlotAngleStep;
        public float RetargetInterval => Mathf.Max(0.05f, retargetInterval);
        public float FullSlotTargetPenalty => fullSlotTargetPenalty;
        public float RangedPreferredRangeRatio => rangedPreferredRangeRatio;
        public float RangedRetreatDistance => rangedRetreatDistance;
        public float EngagedCollisionMass => Mathf.Max(1f, engagedCollisionMass);
        public float ProtectThreatRadius => Mathf.Max(0.5f, protectThreatRadius);
        public float ProtectGuardDistance => Mathf.Max(0f, protectGuardDistance);
        public float SpreadSpacingMultiplier => Mathf.Max(1f, spreadSpacingMultiplier);
        public float RallyFormationScale => rallyFormationScale;
        public float RetreatEscapeMargin => Mathf.Max(0.05f, retreatEscapeMargin);
        public float FlankDamageMultiplier => Mathf.Max(1f, flankDamageMultiplier);
        public float RearDamageMultiplier => Mathf.Max(1f, rearDamageMultiplier);
        public float FlankFrontDot => flankFrontDot;
        public float MoveOrderArrivalDistance => Mathf.Max(0.05f, moveOrderArrivalDistance);
        public float MoraleLossPerAllyDown => moraleLossPerAllyDown;
        public float MoraleLossLeaderDown => moraleLossLeaderDown;
        public float MoraleLossSideOnLeaderDown => moraleLossSideOnLeaderDown;
        public float MoraleLossFlankHit => moraleLossFlankHit;
        public float MoraleLossRearHit => moraleLossRearHit;
        public float MoraleRegenPerSecond => moraleRegenPerSecond;
        public float RoutRecoveryPerSecond => routRecoveryPerSecond;
        public float RoutRecoverThreshold => routRecoverThreshold;
        public bool UseDeploymentPhase => useDeploymentPhase;
        public float DeploymentZoneDepthRatio => deploymentZoneDepthRatio;
        public float FollowSlackDistance => Mathf.Max(0.2f, followSlackDistance);
        public IReadOnlyList<WIBattleTerrainZoneDefinition> TerrainZones => terrainZones;
        public float HighGroundRangeMultiplier => Mathf.Max(1f, highGroundRangeMultiplier);
        public float HighGroundDamageMultiplier => Mathf.Max(1f, highGroundDamageMultiplier);
        public float ForestProjectileDamageMultiplier => forestProjectileDamageMultiplier;
        public float ForestMoveSpeedMultiplier => forestMoveSpeedMultiplier;
        public int NarrowMeleeSlotCount => Mathf.Clamp(narrowMeleeSlotCount, 1, 8);
        public Color DeploymentZoneColor => deploymentZoneColor;

        // 지형 종류에 맞는 구역 표시 색을 반환합니다.
        public Color GetTerrainZoneColor(WIBattleTerrainType terrainType)
        {
            if (terrainType == WIBattleTerrainType.HighGround) return highGroundZoneColor;
            if (terrainType == WIBattleTerrainType.Forest) return forestZoneColor;
            return narrowZoneColor;
        }
        public int BaseHealth => baseHealth;
        public int HealthPerMight => healthPerMight;
        public int BaseMana => baseMana;
        public int ManaPerIntelligence => manaPerIntelligence;
        public float MoveSpeed => moveSpeed;
        public float MeleeRange => meleeRange;
        public float RangedRange => rangedRange;
        public float AttackCooldown => attackCooldown;
        public int BaseDamage => baseDamage;
        public float AdvanceSpeedMultiplier => advanceSpeedMultiplier;
        public float HoldDamageReduction => holdDamageReduction;
        public float FocusDamageMultiplier => focusDamageMultiplier;
        public float MinimumUnitSpacing => minimumUnitSpacing;
        public float CollisionResolveStrength => collisionResolveStrength;
        public float MeleeKnockbackDistance => meleeKnockbackDistance;
        public float HoldKnockbackResistance => holdKnockbackResistance;
        public float FormationReturnSpeed => formationReturnSpeed;
        public float PlaceholderCharacterSize => placeholderCharacterSize;
        public float BattleSpriteScale => battleSpriteScale;
        public bool ShowCharacterHealthBars => showCharacterHealthBars;
        public bool BattleSpriteFacesRight => battleSpriteFacesRight;
        public float SideMarkerAlpha => sideMarkerAlpha;
        public Color RoutingTint => routingTint;
        public Color SkillRangePreviewColor => skillRangePreviewColor;
        public Color SkillAreaPreviewColor => skillAreaPreviewColor;
        public bool ShowCharacterLabels => showCharacterLabels;
        public float PlaceholderProjectileSize => placeholderProjectileSize;
        public float PlaceholderAttackEffectDuration => placeholderAttackEffectDuration;
        public Color AttackerPlaceholderColor => attackerPlaceholderColor;
        public Color DefenderPlaceholderColor => defenderPlaceholderColor;
        public float ProjectileSpeed => projectileSpeed;
        public float ProjectileLifetime => projectileLifetime;
        public float ProjectileCollisionRadius => projectileCollisionRadius;
        public bool ProjectileFriendlyFireEnabled => projectileFriendlyFireEnabled;
        public float ProjectileFriendlyFireSafeDistance => projectileFriendlyFireSafeDistance;
        public float SkillVisualDuration => skillVisualDuration;
        public float CameraPanSpeed => cameraPanSpeed;
        public float CameraMinimumZoom => cameraMinimumZoom;
        public float CameraMiddleZoom => cameraMiddleZoom;
        public float CameraMaximumZoom => cameraMaximumZoom;
        public float CharacterSelectionRadius => characterSelectionRadius;
        public IReadOnlyList<WIBattleSkillDefinition> HeroSkills => heroSkills;
        public IReadOnlyList<WIBattleObjectiveDefinition> BattleObjectives => battleObjectives;
        public Sprite ArenaBackground => arenaBackground;
        public GameObject ArenaPrefab => arenaPrefab;
        public Sprite PlaceholderSprite => placeholderSprite;
        public Material CharacterDefaultMaterial => characterDefaultMaterial;
        public Material CharacterFarOutlineMaterial => characterFarOutlineMaterial;

        // 영웅 ID에 대응하는 액티브 스킬 설정을 반환합니다.
        public WIBattleSkillDefinition GetHeroSkill(string heroId)
        {
            return heroSkills.Find(skill => skill.HeroId == heroId);
        }


        // 식별자에 대응하는 전투 목표를 반환합니다.
        public WIBattleObjectiveDefinition GetBattleObjective(string id)
        {
            return battleObjectives.Find(item => item.Id == id);
        }

        // 실제 캠페인 전투 번호를 기준으로 전투 목표를 순환 선택합니다.
        public WIBattleObjectiveDefinition SelectBattleObjective(string sessionId)
        {
            if (battleObjectives.Count == 0)
            {
                return null;
            }

            int separator = sessionId == null ? -1 : sessionId.LastIndexOf('_');
            if (separator < 0 || int.TryParse(sessionId.Substring(separator + 1), out int number) == false)
            {
                return battleObjectives.Find(item => item.ObjectiveType == WIBattleObjectiveType.Elimination) ?? battleObjectives[0];
            }

            return battleObjectives[Mathf.Abs(number - 1) % battleObjectives.Count];
        }
    }
}

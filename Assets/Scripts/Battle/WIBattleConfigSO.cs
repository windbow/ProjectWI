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

    // 직업을 묶은 전투 병과입니다. 병과마다 고유 전투 규칙이 있습니다.
    public enum WIBattleArchetype
    {
        // 방진: 멈춰 있으면 정면 피해 감소·돌격 무력화, 측면에 약함
        Shield,
        // 돌격: 달려와 치면 돌격 충격
        Charger,
        // 유격: 빠르고 후방 공격에 강하며 원거리·지원을 노림
        Skirmisher,
        // 궁병: 분대 일제 사격, 포물선 화살의 착탄 범위 피해
        Archer,
        // 술사: 느리고 강한 마법탄, 명중 지점 주변 피해
        Caster,
        // 지원: 다친 아군 자동 치료
        Support,
        // 지휘: 주변 아군 분대 사기 보호
        Commander
    }

    // 직업별 전투 병과와 능력 배율입니다.
    [Serializable]
    public class WIBattleClassProfile
    {
        [SerializeField] private WIHeroClass heroClass;
        [SerializeField] private WIBattleArchetype archetype;
        [SerializeField, Min(0.1f)] private float healthMultiplier = 1f;
        [SerializeField, Min(0.1f)] private float damageMultiplier = 1f;
        [SerializeField, Min(0.1f)] private float moveSpeedMultiplier = 1f;
        [SerializeField, Min(0.1f)] private float attackCooldownMultiplier = 1f;
        [SerializeField, Min(0.1f)] private float attackRangeMultiplier = 1f;

        public WIBattleClassProfile()
        {
        }

        public WIBattleClassProfile(WIHeroClass heroClass, WIBattleArchetype archetype, float health, float damage, float speed, float cooldown, float range)
        {
            this.heroClass = heroClass;
            this.archetype = archetype;
            healthMultiplier = health;
            damageMultiplier = damage;
            moveSpeedMultiplier = speed;
            attackCooldownMultiplier = cooldown;
            attackRangeMultiplier = range;
        }

        public WIHeroClass HeroClass => heroClass;
        public WIBattleArchetype Archetype => archetype;
        public float HealthMultiplier => Mathf.Max(0.1f, healthMultiplier);
        public float DamageMultiplier => Mathf.Max(0.1f, damageMultiplier);
        public float MoveSpeedMultiplier => Mathf.Max(0.1f, moveSpeedMultiplier);
        public float AttackCooldownMultiplier => Mathf.Max(0.1f, attackCooldownMultiplier);
        public float AttackRangeMultiplier => Mathf.Max(0.1f, attackRangeMultiplier);
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
        // 전장 프리팹 인스턴스에 적용하는 균일 배율입니다. 캐릭터 크기는 그대로 두고 맵을 넓힐 때 사용합니다.
        [SerializeField, Min(0.1f)] private float arenaPrefabScale = 1f;
        // 전장 중앙에서 각 진영 전위 열까지의 가로 거리이며 두 진영 시작 간격의 절반입니다.
        [SerializeField, Min(0.5f)] private float formationFrontLineDistance = 6f;
        // 한 분대의 최대 인원이며 전투단이 이보다 크면 여러 분대로 나눕니다.
        [SerializeField, Range(2, 12)] private int squadMaxSize = 8;
        // 분대 블록 한 줄(가로 열)에 서는 인원 수입니다.
        [SerializeField, Range(1, 6)] private int squadBlockFiles = 3;
        // 분대 블록 안 인물 간격입니다.
        [SerializeField, Min(0.3f)] private float squadMemberSpacing = 0.85f;
        // 같은 전열에 선 분대 블록 사이 간격입니다.
        [SerializeField, Min(0f)] private float squadGap = 1.2f;
        // 근접 전열과 원거리 후열 사이 간격입니다.
        [SerializeField, Min(0f)] private float formationLineGap = 1.5f;
        // 분대 결속: 교전 중 분대원이 블록 자리에서 벗어날 수 있는 최대 거리입니다.
        [SerializeField, Min(0f)] private float formationEngageLeash = 1.2f;
        // 분대 결속: 분대원 평균이 자리에서 이만큼 이상 벌어지면 기준점이 전진을 멈추고 기다립니다.
        [SerializeField, Min(0.1f)] private float cohesionWaitDistance = 1.2f;
        // 분대 결속: 뒤처진 분대원을 기다리는 최대 시간(초)입니다.
        [SerializeField, Min(0f)] private float cohesionMaxWaitSeconds = 1.5f;
        // 돌격 분대가 적과 이 거리 안에 들면 기준점 속도를 올려 돌진합니다.
        [SerializeField, Min(0f)] private float chargeSprintDistance = 4f;
        [SerializeField, Min(1f)] private float chargeSprintMultiplier = 1.35f;
        // 줄이 칼같이 맞지 않도록 자리마다 주는 위치 흔들림 최대값입니다.
        [SerializeField, Min(0f)] private float formationJitter = 0.12f;
        // 인물마다 주는 이동 속도 편차 비율입니다.
        [SerializeField, Range(0f, 0.3f)] private float moveSpeedVariance = 0.06f;
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
        [Header("Class Archetypes")]
        // 직업별 병과와 능력 배율 목록입니다.
        [SerializeField] private List<WIBattleClassProfile> classProfiles = new List<WIBattleClassProfile>
        {
            new WIBattleClassProfile(WIHeroClass.MagicSwordsman, WIBattleArchetype.Charger, 1.1f, 1.1f, 1.05f, 1f, 1f),
            new WIBattleClassProfile(WIHeroClass.Guardian, WIBattleArchetype.Shield, 1.4f, 0.8f, 0.85f, 1f, 1f),
            new WIBattleClassProfile(WIHeroClass.Crusader, WIBattleArchetype.Commander, 1.3f, 1f, 0.95f, 1f, 1f),
            new WIBattleClassProfile(WIHeroClass.SwordMaster, WIBattleArchetype.Charger, 1f, 1.2f, 1.15f, 1f, 1f),
            new WIBattleClassProfile(WIHeroClass.Archer, WIBattleArchetype.Archer, 0.85f, 1f, 1f, 1f, 1.15f),
            new WIBattleClassProfile(WIHeroClass.Assassin, WIBattleArchetype.Skirmisher, 0.8f, 1.15f, 1.3f, 0.9f, 1f),
            new WIBattleClassProfile(WIHeroClass.Archmage, WIBattleArchetype.Caster, 0.75f, 1.5f, 0.9f, 1.5f, 1f),
            new WIBattleClassProfile(WIHeroClass.Priest, WIBattleArchetype.Support, 0.9f, 0.6f, 1f, 1f, 1f),
            new WIBattleClassProfile(WIHeroClass.Druid, WIBattleArchetype.Support, 1f, 0.7f, 1f, 1f, 1f),
            new WIBattleClassProfile(WIHeroClass.Strategist, WIBattleArchetype.Commander, 0.9f, 0.8f, 1f, 1f, 1f),
            new WIBattleClassProfile(WIHeroClass.Alchemist, WIBattleArchetype.Caster, 0.9f, 1.2f, 1f, 1.3f, 1f),
            new WIBattleClassProfile(WIHeroClass.Warlock, WIBattleArchetype.Caster, 0.8f, 1.4f, 0.95f, 1.4f, 1f)
        };
        // 돌격: 이만큼 달려온 뒤 첫 근접 타격이 돌격 충격이 됩니다.
        [SerializeField, Min(0.5f)] private float chargeMinDistance = 2.5f;
        [SerializeField, Min(1f)] private float chargeDamageMultiplier = 2f;
        [SerializeField, Min(1f)] private float chargeKnockbackMultiplier = 3f;
        [SerializeField, Min(0f)] private float chargeMoraleDamage = 4f;
        // 방진: 멈춰 선 방진이 정면에서 받는 근접 피해 배율과 측면·후방 추가 피해 배율입니다.
        [SerializeField, Range(0.1f, 1f)] private float braceFrontDamageMultiplier = 0.6f;
        [SerializeField, Min(1f)] private float braceFlankExtraMultiplier = 1.25f;
        // 유격: 후방 공격 피해 배율과 원거리·지원 표적 선호 정도(거리 할인)입니다.
        [SerializeField, Min(1f)] private float skirmisherRearMultiplier = 2f;
        [SerializeField, Min(0f)] private float skirmisherBacklinePreference = 3f;
        // 궁병: 포물선 화살의 착탄 반경과 표시 높이입니다.
        [SerializeField, Min(0f)] private float volleySplashRadius = 0.8f;
        [SerializeField, Min(0f)] private float arrowArcHeight = 1.4f;
        // 술사: 마법탄 명중 지점 주변 피해 반경과 주변 피해 배율입니다.
        [SerializeField, Min(0f)] private float casterSplashRadius = 1f;
        [SerializeField, Range(0f, 1f)] private float casterSplashDamageRatio = 0.5f;
        // 지원: 치료량과 치료를 시작하는 아군 체력 비율입니다.
        [SerializeField, Min(0)] private int supportHealPower = 12;
        [SerializeField, Range(0.1f, 1f)] private float supportHealThreshold = 0.75f;
        // 지휘: 사기 보호 반경, 추가 사기 회복, 사기 손실 배율입니다.
        [SerializeField, Min(0f)] private float commandAuraRadius = 4f;
        [SerializeField, Min(0f)] private float commandAuraMoraleRegen = 2f;
        [SerializeField, Range(0.1f, 1f)] private float commandAuraLossMultiplier = 0.7f;
        // 돌격 충격의 화면 흔들림 세기입니다.
        [SerializeField, Range(0f, 1f)] private float shakeChargeImpact = 0.25f;

        [Header("Game Feel")]
        // 걸을 때 몸이 위아래로 튀는 높이와 초당 걸음 수, 좌우 기울기(도)입니다.
        [SerializeField, Min(0f)] private float walkBobHeight = 0.06f;
        [SerializeField, Min(0f)] private float walkBobFrequency = 3.2f;
        [SerializeField, Min(0f)] private float walkTiltDegrees = 4f;
        // 근접 공격 때 앞으로 내딛는 거리와 시간입니다.
        [SerializeField, Min(0f)] private float attackLungeDistance = 0.14f;
        [SerializeField, Min(0.01f)] private float attackLungeDuration = 0.16f;
        // 피격 시 번쩍이는 색과 시간, 눌림 정도입니다.
        [SerializeField] private Color hitFlashColor = new Color(1f, 0.35f, 0.3f, 1f);
        [SerializeField, Min(0.01f)] private float hitFlashDuration = 0.12f;
        [SerializeField, Range(0f, 0.5f)] private float hitSquashAmount = 0.14f;
        // 전투 불능 시 넘어지며 사라지는 시간과 회전 각도입니다.
        [SerializeField, Min(0.05f)] private float deathFallDuration = 0.55f;
        [SerializeField] private float deathFallDegrees = 80f;
        // 증원 등장 시 튀어나오는 시간입니다.
        [SerializeField, Min(0.05f)] private float spawnPopDuration = 0.3f;
        // 선택 분대 표시 맥동 속도와 크기입니다.
        [SerializeField, Min(0f)] private float selectionPulseSpeed = 6f;
        [SerializeField, Range(0f, 0.5f)] private float selectionPulseAmount = 0.1f;
        // 화면 흔들림 최대 이동량과 초당 감쇠량입니다.
        [SerializeField, Min(0f)] private float cameraShakeMaxOffset = 0.35f;
        [SerializeField, Min(0.1f)] private float cameraShakeDecay = 1.8f;
        // 사건별 화면 흔들림 세기(0~1)입니다.
        [SerializeField, Range(0f, 1f)] private float shakeFirstClash = 0.5f;
        [SerializeField, Range(0f, 1f)] private float shakeLeaderKill = 0.55f;
        [SerializeField, Range(0f, 1f)] private float shakeSkillImpact = 0.4f;
        [SerializeField, Range(0f, 1f)] private float shakeSquadRouted = 0.3f;
        [SerializeField, Range(0f, 1f)] private float shakeKill = 0.06f;
        [SerializeField, Range(0f, 1f)] private float shakeFlankHit = 0.04f;
        // 사건별 히트 스톱(짧은 정지) 시간입니다.
        [SerializeField, Min(0f)] private float hitStopFirstClash = 0.08f;
        [SerializeField, Min(0f)] private float hitStopLeaderKill = 0.14f;
        [SerializeField, Min(0f)] private float hitStopSkillImpact = 0.06f;
        // 명령 지점에 퍼지는 원의 시간과 색입니다.
        [SerializeField, Min(0.05f)] private float orderPingDuration = 0.45f;
        [SerializeField] private Color orderPingMoveColor = new Color(0.4f, 0.95f, 1f, 0.8f);
        [SerializeField] private Color orderPingAttackColor = new Color(1f, 0.35f, 0.25f, 0.85f);
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
        public float ArenaPrefabScale => Mathf.Max(0.1f, arenaPrefabScale);
        public float FormationFrontLineDistance => Mathf.Max(0.5f, formationFrontLineDistance);
        public int SquadMaxSize => Mathf.Clamp(squadMaxSize, 2, 12);
        public int SquadBlockFiles => Mathf.Clamp(squadBlockFiles, 1, 6);
        public float SquadMemberSpacing => Mathf.Max(0.3f, squadMemberSpacing);
        public float SquadGap => Mathf.Max(0f, squadGap);
        public float FormationLineGap => Mathf.Max(0f, formationLineGap);
        public float FormationJitter => Mathf.Max(0f, formationJitter);
        public float FormationEngageLeash => Mathf.Max(0f, formationEngageLeash);
        public float CohesionWaitDistance => Mathf.Max(0.1f, cohesionWaitDistance);
        public float CohesionMaxWaitSeconds => Mathf.Max(0f, cohesionMaxWaitSeconds);
        public float ChargeSprintDistance => Mathf.Max(0f, chargeSprintDistance);
        public float ChargeSprintMultiplier => Mathf.Max(1f, chargeSprintMultiplier);
        public float MoveSpeedVariance => moveSpeedVariance;
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
        public float ChargeMinDistance => Mathf.Max(0.5f, chargeMinDistance);
        public float ChargeDamageMultiplier => Mathf.Max(1f, chargeDamageMultiplier);
        public float ChargeKnockbackMultiplier => Mathf.Max(1f, chargeKnockbackMultiplier);
        public float ChargeMoraleDamage => chargeMoraleDamage;
        public float BraceFrontDamageMultiplier => braceFrontDamageMultiplier;
        public float BraceFlankExtraMultiplier => Mathf.Max(1f, braceFlankExtraMultiplier);
        public float SkirmisherRearMultiplier => Mathf.Max(1f, skirmisherRearMultiplier);
        public float SkirmisherBacklinePreference => skirmisherBacklinePreference;
        public float VolleySplashRadius => volleySplashRadius;
        public float ArrowArcHeight => arrowArcHeight;
        public float CasterSplashRadius => casterSplashRadius;
        public float CasterSplashDamageRatio => casterSplashDamageRatio;
        public int SupportHealPower => supportHealPower;
        public float SupportHealThreshold => supportHealThreshold;
        public float CommandAuraRadius => commandAuraRadius;
        public float CommandAuraMoraleRegen => commandAuraMoraleRegen;
        public float CommandAuraLossMultiplier => commandAuraLossMultiplier;

        // 직업에 대응하는 병과 설정을 반환하며 없으면 null입니다.
        public WIBattleClassProfile GetClassProfile(WIHeroClass heroClass)
        {
            return classProfiles.Find(item => item.HeroClass == heroClass);
        }
        public float WalkBobHeight => walkBobHeight;
        public float WalkBobFrequency => walkBobFrequency;
        public float WalkTiltDegrees => walkTiltDegrees;
        public float AttackLungeDistance => attackLungeDistance;
        public float AttackLungeDuration => Mathf.Max(0.01f, attackLungeDuration);
        public Color HitFlashColor => hitFlashColor;
        public float HitFlashDuration => Mathf.Max(0.01f, hitFlashDuration);
        public float HitSquashAmount => hitSquashAmount;
        public float DeathFallDuration => Mathf.Max(0.05f, deathFallDuration);
        public float DeathFallDegrees => deathFallDegrees;
        public float SpawnPopDuration => Mathf.Max(0.05f, spawnPopDuration);
        public float SelectionPulseSpeed => selectionPulseSpeed;
        public float SelectionPulseAmount => selectionPulseAmount;
        public float CameraShakeMaxOffset => cameraShakeMaxOffset;
        public float CameraShakeDecay => Mathf.Max(0.1f, cameraShakeDecay);
        public float OrderPingDuration => Mathf.Max(0.05f, orderPingDuration);
        public Color OrderPingMoveColor => orderPingMoveColor;
        public Color OrderPingAttackColor => orderPingAttackColor;

        // 연출 사건의 화면 흔들림 세기를 반환합니다.
        public float GetShakeTrauma(WIBattleFeedbackType type)
        {
            switch (type)
            {
                case WIBattleFeedbackType.FirstClash: return shakeFirstClash;
                case WIBattleFeedbackType.LeaderKill: return shakeLeaderKill;
                case WIBattleFeedbackType.SkillImpact: return shakeSkillImpact;
                case WIBattleFeedbackType.SquadRouted: return shakeSquadRouted;
                case WIBattleFeedbackType.Kill: return shakeKill;
                case WIBattleFeedbackType.ChargeImpact: return shakeChargeImpact;
                default: return shakeFlankHit;
            }
        }

        // 연출 사건의 히트 스톱 시간을 반환합니다.
        public float GetHitStop(WIBattleFeedbackType type)
        {
            switch (type)
            {
                case WIBattleFeedbackType.FirstClash: return hitStopFirstClash;
                case WIBattleFeedbackType.LeaderKill: return hitStopLeaderKill;
                case WIBattleFeedbackType.SkillImpact: return hitStopSkillImpact;
                default: return 0f;
            }
        }
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

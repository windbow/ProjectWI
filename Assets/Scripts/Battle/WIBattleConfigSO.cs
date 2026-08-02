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

    [Serializable]
    public class WIBattleSkillDefinition
    {
        [SerializeField] private string heroId;
        [SerializeField] private string displayName;
        [SerializeField] private WIBattleSkillType skillType;
        [SerializeField] private int manaCost = 40;
        [SerializeField] private int power = 35;
        [SerializeField] private float range = 4f;
        [SerializeField] private float cooldown = 10f;

        public string HeroId => heroId;
        public string DisplayName => displayName;
        public WIBattleSkillType SkillType => skillType;
        public int ManaCost => manaCost;
        public int Power => power;
        public float Range => range;
        public float Cooldown => cooldown;
    }

    [CreateAssetMenu(fileName = "WI_BattleConfig", menuName = "WI/Battle/Config")]
    public class WIBattleConfigSO : ScriptableObject
    {
        [SerializeField] private Vector2 arenaSize = new Vector2(18f, 10f);
        [SerializeField] private float formationColumnSpacing = 1.4f;
        [SerializeField] private float formationRowSpacing = 1.2f;
        [SerializeField] private int baseHealth = 100;
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
        [SerializeField] private List<WIBattleSkillDefinition> heroSkills = new List<WIBattleSkillDefinition>();
        [SerializeField] private Sprite placeholderSprite;

        public Vector2 ArenaSize => arenaSize;
        public float FormationColumnSpacing => formationColumnSpacing;
        public float FormationRowSpacing => formationRowSpacing;
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
        public IReadOnlyList<WIBattleSkillDefinition> HeroSkills => heroSkills;
        public Sprite PlaceholderSprite => placeholderSprite;

        // 영웅 ID에 대응하는 액티브 스킬 설정을 반환합니다.
        public WIBattleSkillDefinition GetHeroSkill(string heroId)
        {
            return heroSkills.Find(skill => skill.HeroId == heroId);
        }
    }
}

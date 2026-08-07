using System;
using System.Collections.Generic;
using ProjectWI.Administration;
using UnityEngine;

namespace ProjectWI.Battle
{
    public enum WIBattleSide
    {
        Attacker,
        Defender
    }

    public enum WIBattleCommand
    {
        Advance,
        Hold,
        Focus,
        Retreat
    }

    public enum WIBattleVisualEffectType
    {
        MeleeHit,
        Projectile,
        SkillDamage,
        SkillHeal,
        SkillCommand
    }

    [Serializable]
    public class WIBattleVisualEffectState
    {
        public int EffectId;
        public WIBattleVisualEffectType EffectType;
        public WIBattleSide Side;
        public Vector2 StartPosition;
        public Vector2 EndPosition;
        public float StartedAt;
        public float Duration;
        public float Radius;
    }

    [Serializable]
    public class WIBattleProjectileState
    {
        public int ProjectileId;
        public string ShooterHeroId;
        public string TargetHeroId;
        public WIBattleSide Side;
        public Vector2 Position;
        public Vector2 TargetPosition;
        public int Damage;
        public float RemainingLifetime;
        public float TravelledDistance;
    }

    [Serializable]
    public class WIBattleCharacterState
    {
        public string HeroId;
        public string ArmyId;
        public WIBattleSide Side;
        public WIUnitRole Role;
        public WICharacterGrade Grade;
        public WIHeroClass HeroClass;
        public string DisplayName;
        public Vector2 Position;
        public Vector2 FormationPosition;
        public int MaxHealth;
        public int Health;
        public int MaxMana;
        public int Mana;
        public int AttackDamage;
        public float AttackRange;
        public float MoveSpeed;
        public float CooldownRemaining;
        public float SkillCooldownRemaining;

        public bool IsAlive => Health > 0;
    }

    [Serializable]
    public class WIBattleRuntimeState
    {
        public string SessionId;
        public float ElapsedSeconds;
        public bool Finished;
        public WIBattleOutcome AttackerOutcome;
        public string ObjectiveId;
        public string ObjectiveName;
        public WIBattleObjectiveType ObjectiveType;
        public float ObjectiveDurationSeconds;
        public float ControlDurationSeconds;
        public float ControlRadius;
        public float AttackerControlSeconds;
        public float DefenderControlSeconds;
        public WIBattleCommand AttackerCommand = WIBattleCommand.Advance;
        public WIBattleCommand DefenderCommand = WIBattleCommand.Hold;
        public string AttackerFocusHeroId;
        public string DefenderFocusHeroId;
        public List<WIBattleCharacterState> Characters = new List<WIBattleCharacterState>();
        public List<WIBattleVisualEffectState> VisualEffects = new List<WIBattleVisualEffectState>();
        public int NextVisualEffectId = 1;
        public List<WIBattleProjectileState> Projectiles = new List<WIBattleProjectileState>();
        public int NextProjectileId = 1;
    }
}

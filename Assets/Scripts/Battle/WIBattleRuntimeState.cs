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

    [Serializable]
    public class WIBattleCharacterState
    {
        public string HeroId;
        public string ArmyId;
        public WIBattleSide Side;
        public WIUnitRole Role;
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
        public WIBattleCommand AttackerCommand = WIBattleCommand.Advance;
        public WIBattleCommand DefenderCommand = WIBattleCommand.Hold;
        public string AttackerFocusHeroId;
        public string DefenderFocusHeroId;
        public List<WIBattleCharacterState> Characters = new List<WIBattleCharacterState>();
    }
}

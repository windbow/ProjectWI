using System.Collections.Generic;
using System.Linq;
using ProjectWI.Administration;
using UnityEngine;

namespace ProjectWI.Battle
{
    public static class WIBattleRuntimeBuilder
    {
        // 전략 전투 세션의 참가자 스냅샷을 실제 전투 캐릭터와 초기 진형으로 변환합니다.
        public static WIBattleRuntimeState Build(
            WIBattleConfigSO config,
            WIAdministrationDatabaseSO database,
            WIBattleSessionState session)
        {
            WIBattleRuntimeState runtime = new WIBattleRuntimeState { SessionId = session.SessionId };
            AddSide(runtime, config, database, session.AttackerHeroIds, WIBattleSide.Attacker);
            AddSide(runtime, config, database, session.DefenderHeroIds, WIBattleSide.Defender);
            return runtime;
        }

        // 한 진영의 인물을 역할 순서에 맞춰 전열, 중앙과 후열에 배치합니다.
        private static void AddSide(
            WIBattleRuntimeState runtime,
            WIBattleConfigSO config,
            WIAdministrationDatabaseSO database,
            List<WIBattleParticipantState> participants,
            WIBattleSide side)
        {
            List<WIBattleParticipantState> ordered = participants
                .OrderBy(item => GetRoleColumn(item.Role))
                .ThenBy(item => item.HeroId).ToList();
            Dictionary<int, int> rowsByColumn = new Dictionary<int, int>();
            foreach (WIBattleParticipantState participant in ordered)
            {
                WIHeroDefinition hero = database.GetHero(participant.HeroId);
                if (hero == null) continue;
                int column = GetRoleColumn(participant.Role);
                int row = rowsByColumn.ContainsKey(column) ? rowsByColumn[column] : 0;
                rowsByColumn[column] = row + 1;
                float direction = side == WIBattleSide.Attacker ? 1f : -1f;
                float x = direction * (-config.ArenaSize.x * 0.35f + column * config.FormationColumnSpacing);
                float y = GetCenteredRow(row, ordered.Count(item => GetRoleColumn(item.Role) == column), config.FormationRowSpacing);
                bool ranged = participant.Role == WIUnitRole.Ranged || participant.Role == WIUnitRole.Magic || participant.Role == WIUnitRole.Support;
                int maxHealth = config.BaseHealth + hero.Might * config.HealthPerMight;
                Vector2 formationPosition = new Vector2(x, y);
                runtime.Characters.Add(new WIBattleCharacterState
                {
                    HeroId = hero.Id,
                    ArmyId = participant.ArmyId,
                    Side = side,
                    Role = participant.Role,
                    Grade = hero.Grade,
                    HeroClass = hero.HeroClass,
                    DisplayName = hero.DisplayName.Korean,
                    Position = formationPosition,
                    FormationPosition = formationPosition,
                    MaxHealth = maxHealth,
                    Health = maxHealth,
                    MaxMana = config.BaseMana + hero.Intelligence * config.ManaPerIntelligence,
                    Mana = config.BaseMana + hero.Intelligence * config.ManaPerIntelligence,
                    AttackDamage = config.BaseDamage + hero.Might / 5,
                    AttackRange = ranged ? config.RangedRange : config.MeleeRange,
                    MoveSpeed = config.MoveSpeed
                });
            }
        }

        // 역할에 대응하는 진형 열 번호를 반환합니다.
        private static int GetRoleColumn(WIUnitRole role)
        {
            if (role == WIUnitRole.Vanguard) return 0;
            if (role == WIUnitRole.Commander || role == WIUnitRole.Melee) return 1;
            return 2;
        }

        // 같은 열의 인원을 중앙 기준으로 균등하게 배치할 Y 좌표를 반환합니다.
        private static float GetCenteredRow(int row, int count, float spacing)
        {
            return (row - (count - 1) * 0.5f) * spacing;
        }
    }
}

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
        Retreat,
        // 전위가 원거리·지원 인물 주변으로 돌아와 접근하는 적을 막습니다.
        Protect,
        // 인물 간격을 넓혀 광역 피해에 대비합니다.
        Spread,
        // 대장 주변으로 모여 진형을 회복합니다.
        Rally,
        // 선택 분대가 지정 위치로 이동한 뒤 그 자리를 지킵니다.
        MoveTo,
        // 직접 지휘하는 영웅을 분대원이 따라가며 싸웁니다.
        Follow
    }

    // 화면 흔들림·히트 스톱 같은 연출을 일으키는 전투 사건 종류입니다.
    public enum WIBattleFeedbackType
    {
        // 측면·후방 근접 타격
        FlankHit,
        // 인물 전투 불능
        Kill,
        // 분대장 전투 불능
        LeaderKill,
        // 분대 사기 붕괴
        SquadRouted,
        // 범위 스킬 명중
        SkillImpact,
        // 전투 첫 근접 충돌
        FirstClash,
        // 돌격 병과의 돌격 충격
        ChargeImpact
    }

    // 표시 계층이 한 프레임에 한 번 읽고 비우는 연출 사건입니다. 전투 결과에는 영향을 주지 않습니다.
    [Serializable]
    public struct WIBattleFeedbackEvent
    {
        public WIBattleFeedbackType Type;
        public Vector2 Position;
        public WIBattleSide Side;
    }

    // 영웅 한 명과 휘하 인물로 구성된 전투 중 조작 단위입니다.
    [Serializable]
    public class WIBattleSquadState
    {
        // 진영 안에서 고유한 분대 번호이며 1부터 시작합니다.
        public int SquadId;
        public WIBattleSide Side;
        // 분대장(영웅 또는 대장) 인물 ID입니다.
        public string LeaderHeroId;
        // 진영 공통 명령 대신 분대 전용 명령을 사용하는지 여부입니다.
        public bool HasCommandOverride;
        // 분대 전용 명령입니다.
        public WIBattleCommand Command;
        // 분대 전용 집중 공격 표적 ID입니다.
        public string FocusHeroId;
        // 분대 사기(0~100)이며 0이 되면 무너져 퇴각합니다.
        public float Morale = 100f;
        // 사기가 무너져 퇴각 중인지 여부입니다.
        public bool IsRouting;
        // 아군 지휘 병과의 사기 보호 범위 안에 있는지 여부입니다.
        public bool InCommandAura;
        // 블록 앞줄 가운데 기준점입니다. 결속 이동 중 분대원은 이 점 기준 자기 자리를 유지합니다.
        public Vector2 Anchor;
        // 직전 틱에 결속 이동을 쓰고 있었는지 여부이며, 새로 시작하면 기준점을 현재 블록 위치로 맞춥니다.
        public bool CohesionActive;
        // 분대원 중 누군가 적과 사거리 안에서 맞닿아 있는지 여부입니다.
        public bool Engaged;
        // 선두 인물이 향할 기준점 목표와 속도, 전진 중 여부입니다.
        public Vector2 AnchorGoal;
        public float AnchorSpeed;
        public bool AnchorMoving;
        // 뒤처진 분대원을 기다린 시간이며 상한을 넘으면 기다리지 않고 전진합니다.
        public float WaitTime;
    }

    public enum WIBattleVisualEffectType
    {
        MeleeHit,
        Projectile,
        SkillDamage,
        SkillHeal,
        SkillCommand,
        // 실제 피해가 적용된 위치에만 표시하는 피격 효과입니다.
        Hit
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
        // 발사 당시 역할을 보존하여 궁수와 마법 계열의 외형을 구분합니다.
        public WIUnitRole ShooterRole;
        public int ProjectileId;
        public string ShooterHeroId;
        public string TargetHeroId;
        public WIBattleSide Side;
        public Vector2 Position;
        public Vector2 TargetPosition;
        public int Damage;
        public float RemainingLifetime;
        public float TravelledDistance;
        // 포물선으로 날아가 목표 지점에 떨어지는 화살인지 여부입니다. 비행 중에는 충돌하지 않습니다.
        public bool IsArc;
        // 착탄·명중 지점 주변 피해 반경입니다.
        public float SplashRadius;
        // 발사 지점에서 목표 지점까지 거리이며 포물선 표시에 씁니다.
        public float TotalDistance;
        // 표시 보간용으로 직전 틱 시작 시점의 위치를 저장합니다.
        public Vector2 PreviousPosition;
        // 직전 틱 위치가 기록되었는지 여부입니다.
        public bool HasPreviousPosition;
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
        // 연속 이동 모드에서 현재 추적 중인 적 인물 ID입니다.
        public string TargetHeroId;
        // 다음 표적 재평가까지 남은 시간입니다.
        public float RetargetRemaining;
        // 목표 주변 근접 교전 슬롯 번호이며 슬롯이 없으면 -1입니다.
        public int EngagementSlot = -1;
        // 인물이 현재 화면 오른쪽을 바라보는지 여부입니다.
        public bool FacingRight;
        // 소속 분대 번호이며 0이면 아직 배정되지 않았습니다.
        public int SquadId;
        // 소속 분대가 무너져 퇴각 중인지 여부이며 표시 색상에 사용합니다.
        public bool IsRouting;
        // 직접 지휘 중 분대장 기준으로 유지할 상대 위치입니다.
        public Vector2 FollowOffset;
        // 직업에서 정한 전투 병과입니다.
        public WIBattleArchetype Archetype;
        // 블록 자리 흔들림 값입니다(인물마다 고정).
        public Vector2 BlockJitter;
        // 살아 있는 분대원 중 블록 자리 순번이며 앞줄이 쓰러지면 앞으로 당겨집니다.
        public int BlockIndex;
        // 직업 배율을 반영한 공격 간격(초)이며 0이면 전투 설정 기본값을 씁니다.
        public float AttackInterval;
        // 마지막 공격 뒤 멈추지 않고 달려온 거리이며 돌격 충격과 방진 태세 판정에 씁니다.
        public float ChargeDistance;
        // 표시 보간용으로 직전 틱 시작 시점의 위치를 저장합니다.
        public Vector2 PreviousPosition;
        // 직전 틱 위치가 기록되었는지 여부입니다.
        public bool HasPreviousPosition;

        // 직전 틱과 현재 틱 사이를 보간한 표시 위치를 반환합니다.
        public Vector2 GetDisplayPosition(float alpha)
        {
            return HasPreviousPosition == true ? Vector2.Lerp(PreviousPosition, Position, alpha) : Position;
        }

        // 후퇴로 전장 가장자리를 벗어나 전투에서 이탈했는지 여부입니다.
        public bool Escaped;

        // 쓰러지지 않고 전장에 남아 있는지 여부입니다.
        public bool IsAlive => Health > 0 && Escaped == false;
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
        // 전투 시작 전 배치 단계 중이면 시뮬레이션을 진행하지 않습니다.
        public bool IsDeploying;
        // 플레이어가 직접 지휘 중인 영웅 ID입니다.
        public string ControlledHeroId;
        // 표시 계층이 소비할 연출 사건 목록입니다.
        public List<WIBattleFeedbackEvent> FeedbackEvents = new List<WIBattleFeedbackEvent>();
        // 첫 근접 충돌 연출을 이미 냈는지 여부입니다.
        public bool FirstClashDone;
        // 양 진영의 분대 목록입니다.
        public List<WIBattleSquadState> Squads = new List<WIBattleSquadState>();
        public List<WIBattleVisualEffectState> VisualEffects = new List<WIBattleVisualEffectState>();
        public int NextVisualEffectId = 1;
        public List<WIBattleProjectileState> Projectiles = new List<WIBattleProjectileState>();
        public int NextProjectileId = 1;
        // 고정 틱으로 아직 처리하지 않은 누적 프레임 시간입니다.
        public float TickAccumulator;
        // 화면 표시에서 직전 틱과 현재 틱 사이를 보간하는 비율(0~1)입니다.
        public float InterpolationAlpha = 1f;
    }
}

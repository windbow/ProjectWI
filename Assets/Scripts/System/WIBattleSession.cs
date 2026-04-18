using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

namespace ProjectWI.SubSystem
{
    public class WIBattleSession
    {
        /// <summary>전투가 벌어지고 있는 현재 던전의 이름 (로그 출력용)</summary>
        public string DungeonName { get; private set; }
        
        /// <summary>현재 전투에 참여 중인 아군(모험가) 캐릭터 목록</summary>
        public List<WICharacterBase> Adventurers { get; private set; } = new List<WICharacterBase>();
        
        /// <summary>현재 전투에 참여 중인 적군(몬스터) 캐릭터 목록</summary>
        public List<WICharacterBase> Monsters { get; private set; } = new List<WICharacterBase>();
        
        /// <summary>전투에 참여하는 모든 캐릭터의 현재 행동력 누적 수치를 관리하는 매핑 테이블</summary>
        public Dictionary<WICharacterBase, float> ActionValues { get; private set; } = new Dictionary<WICharacterBase, float>();
        
        /// <summary>다음 행동이 발생하기까지 차오르고 있는 공용 턴 대기 타이머</summary>
        public float TurnWaitTimer = 0f;
        
        /// <summary>게이지가 100%가 되기 위해 필요한 고정 목표 시간 (기본값 1초)</summary>
        public float TurnWaitTimeMax = 1.0f; 
        
        /// <summary>캐릭터가 행동하기 위해 필요한 최소 행동력 수치 (기본값 100)</summary>
        public const float ActionValueThreshold = 100f;
        
        /// <summary>전투 로그가 추가될 때 UI 구독자들에게 알리는 델리게이트 이벤트</summary>
        public Action<string> OnLogAdded;
        
        /// <summary>매 틱마다 행동력 갱신 후 발생하는 이벤트 (행동력 바 갱신용)</summary>
        public Action OnBattleTick;
        
        /// <summary>UI를 나갔다가 들어와도 볼 수 있도록 보관해두는 전투 로그 히스토리 리스트</summary>
        private List<string> battleLogs = new List<string>();
        

        /// <summary>모험가 전멸 또는 몬스터 전멸로 인해 현재 몬스터 웨이브와의 전투가 끝났음을 나타내는 플래그</summary>
        public bool IsBattleEnded { get; private set; } = false;

        public void ResetBattleState()
        {
            IsBattleEnded = false;
            TurnWaitTimer = 0f;
            foreach (var ch in Adventurers) 
            { 
                ActionValues[ch] = 0f; 
            }
            foreach (var ch in Monsters) 
            { 
                if (ch.CurrentHp <= 0) 
                {
                    ch.CurrentHp = ch.MaxHp; 
                }
                ActionValues[ch] = 0f; 
            } 
            AddLog("------------------------------------");
        }

        public WIBattleSession(string dungeonName)
        {
            DungeonName = dungeonName;
            AddLog($"[시스템] {DungeonName} 전투 시작!");
        }
        
        /// <summary>
        /// 세션에 참여할 모험가 파티와 몬스터 파티의 리스트를 주입받고, 딕셔너리를 활용해 전체 캐릭터의 행동력을 초기화합니다.
        /// </summary>
        public void SetCombatants(List<WICharacterBase> advs, List<WICharacterBase> mons)
        {
            Adventurers = new List<WICharacterBase>(advs);
            Monsters = new List<WICharacterBase>(mons);
            
            ActionValues.Clear();
            System.Random rand = new System.Random();
            
            // 전투 시작 시 초기 행동력을 다르게 주어(Initiative Roll) 턴이 동시에 오지 않도록 분산시킵니다.
            foreach (var adv in Adventurers) 
            {
                ActionValues[adv] = (float)rand.NextDouble() * 20f;
            }
            foreach (var mon in Monsters) 
            {
                ActionValues[mon] = (float)rand.NextDouble() * 20f;
            }
            
            TurnWaitTimer = 0f;
        }
        
        /// <summary>
        /// 백그라운드 전투에서 발생하는 공격, 스킬 사용, 사망 등의 메시지를 리스트에 보관하고 UI 이벤트를 트리거합니다.
        /// </summary>
        public void AddLog(string log)
        {
            battleLogs.Add(log);
            if(battleLogs.Count > 50) 
            {
                battleLogs.RemoveAt(0); 
            }
            
            if (OnLogAdded != null)
            {
                OnLogAdded.Invoke(log);
            }
        }
        
        /// <summary>
        /// 최근까지 누적된 모든 전투 로그를 반환하여, 유저가 UI 페이지에 늦게 진입하더라도 밀린 텍스트를 그릴 수 있게 해줍니다.
        /// </summary>
        public List<string> GetLogs()
        {
            return battleLogs;
        }

        /// <summary>
        /// (삭제된 로직) 고정 대기시간 사용으로 인해 동적 WaitTime 계산 함수는 사용하지 않습니다.
        /// </summary>
        private void RecalculateWaitTime()
        {
            // 사용 안 함
        }

        /// <summary>
        /// 현재 쌓여가고 있는 공용 대기 타이머(TurnWaitTimer)가 목표치 대비 얼마나 찼는지(0~1 비율) 반환하여 UI 슬라이더를 업데이트합니다.
        /// </summary>
        public float GetTurnProgressRatio()
        {
            return Mathf.Clamp01(TurnWaitTimer / TurnWaitTimeMax);
        }

        /// <summary>
        /// 특정 캐릭터의 현재 누적 행동력을 0~1 비율로 반환합니다.
        /// 기준값은 캐릭터 스피드 × 5초로, 속도에 비례해 게이지가 차오르는 체감을 유지합니다.
        /// 행동 후 행동력이 초기화(0)되므로 게이지도 함께 리셋됩니다.
        /// </summary>
        public float GetActionValueRatio(WICharacterBase character)
        {
            if (ActionValues.ContainsKey(character) == false)
            {
                return 0f;
            }

            float value = ActionValues[character];

            if (value < 0f)
            {
                return 0f; // 사망 처리된 캐릭터
            }

            // 모든 캐릭터가 동일한 Threshold(100)를 기준으로 게이지가 표시되도록 수정
            return Mathf.Clamp01(value / ActionValueThreshold);
        }
        
        /// <summary>
        /// 프레임마다 공용 대기열 게이지(TurnWaitTimer)를 채웁니다. 속도에 따른 행동력 누적은 백그라운드에서 진행됩니다.
        /// 게이지가 꽉 차면, 누적 행동력이 가장 높은 '단 한 명'만 턴을 가져갑니다. 이후 게이지는 0으로 초기화됩니다.
        /// </summary>
        public void Tick(float deltaTime)
        {
            // 1. 공용 턴 게이지 상승 (최대치까지만 차오름)
            if (TurnWaitTimer < TurnWaitTimeMax)
            {
                TurnWaitTimer += deltaTime;
                if (TurnWaitTimer > TurnWaitTimeMax)
                {
                    TurnWaitTimer = TurnWaitTimeMax;
                }
            }
            
            // 2. 캐릭터들은 상태와 상관없이 스피드에 맞춰 행동력(ActionValue) 누적
            foreach (var key in ActionValues.Keys.ToList())
            {
                if (key.CurrentHp > 0)
                {
                    ActionValues[key] += key.GetSpeed() * deltaTime;
                }
            }

            // 행동력 갱신 이벤트 발생 (UI 행동력 바 갱신용)
            if (OnBattleTick != null)
            {
                OnBattleTick.Invoke();
            }

            // 3. 게이지가 가득 찼을(TurnWaitTimeMax) 때만 행동 여부 판단
            if (TurnWaitTimer >= TurnWaitTimeMax)
            {
                WICharacterBase actingCharacter = null;
                float highestAv = -1f;

                // 살아있는 대상 중 행동력이 Threshold 이상이면서 가장 높은 1인 색출
                foreach (var kvp in ActionValues)
                {
                    if (kvp.Key.CurrentHp > 0 && kvp.Value >= ActionValueThreshold)
                    {
                        if (kvp.Value > highestAv)
                        {
                            highestAv = kvp.Value;
                            actingCharacter = kvp.Key;
                        }
                    }
                }

                // 4. 행동 가능한 캐릭터가 있을 경우에만 행동을 수행하고 게이지를 리셋
                if (actingCharacter != null)
                {
                    // 게이지 리셋 (다시 0부터 차오르기 시작함)
                    TurnWaitTimer = 0f;
                    
                    // 해당 캐릭터의 행동력 소모
                    ActionValues[actingCharacter] = 0f; 
                    
                    WICharacterBase target = GetAutoTarget(actingCharacter);
                    if (target != null)
                    {
                        ExecuteTurn(actingCharacter, target);
                        CheckDead();
                    }
                }
                // 행동 가능한 캐릭터가 없다면 TurnWaitTimer는 Max 상태를 유지하며 행동력이 찰 때까지 대기
            }
        }

        /// <summary>
        /// 단일 타겟 기술 사용을 가정하여, 상대편 진영의 리스트를 순회하며 죽지 않은 첫 번째 캐릭터를 오토 타겟으로 잡아 반환합니다.
        /// </summary>
        private WICharacterBase GetAutoTarget(WICharacterBase attacker)
        {
            if (Adventurers.Contains(attacker))
            {
                return Monsters.FirstOrDefault(m => m.CurrentHp > 0);
            }
            else if (Monsters.Contains(attacker))
            {
                return Adventurers.FirstOrDefault(a => a.CurrentHp > 0);
            }
            return null;
        }
        
        /// <summary>
        /// 100 게이지에 도달한 공격자가 직업의 스킬 데미지 배율 등을 이용해 최종 데미지를 연산하고 방어자의 체력을 깎은 뒤 로그를 배출합니다.
        /// </summary>
        private void ExecuteTurn(WICharacterBase attacker, WICharacterBase defender)
        {
            attacker.OnTurnStart();
            
            WISkill usedSkill = null;
            if (attacker.Job != null && attacker.Job.Skills.Length > 0 && attacker.Job.Skills[0] != null)
            {
                usedSkill = attacker.Job.Skills[0];
            }
            
            int finalDamage = attacker.AttackPower;
            string logText = $"[{attacker.Name}] -> [{defender.Name}] 일반 공격! ({finalDamage} 피해)";
            
            if (usedSkill != null)
            {
                finalDamage = Mathf.RoundToInt(attacker.AttackPower * usedSkill.DamageMultiplier);
                logText = $"[{attacker.Name}] -> [{defender.Name}] {usedSkill.SkillName} 사용! ({finalDamage} 피해)";
            }
            
            defender.TakeDamage(finalDamage);
            AddLog(logText);
        }
        
        /// <summary>
        /// 턴 종료 직후 체력이 0 이하인 대상의 행동 수치를 비활성화(-1) 처리하며, 
        /// 모험가 진영 또는 몬스터 진영 전멸 여부를 파악해 다음 웨이브로 진행시키거나 파티를 부활 처리시킵니다.
        /// </summary>
        private bool CheckDead()
        {
            bool anyDead = false;

            foreach (var adv in Adventurers)
            {
                if (adv.CurrentHp <= 0 && ActionValues.ContainsKey(adv) && ActionValues[adv] >= 0) 
                {
                    AddLog($"[시스템] {adv.Name} 쓰러짐!");
                    ActionValues[adv] = -1f;
                    anyDead = true;
                }
            }

            foreach (var mon in Monsters)
            {
                if (mon.CurrentHp <= 0 && ActionValues.ContainsKey(mon) && ActionValues[mon] >= 0)
                {
                    AddLog($"[시스템] {mon.Name} 처치!");
                    ActionValues[mon] = -1f;
                    anyDead = true;
                }
            }

            bool isAdvWiped = Adventurers.All(a => a.CurrentHp <= 0);
            bool isMonWiped = Monsters.All(m => m.CurrentHp <= 0);

            if (isAdvWiped)
            {
                AddLog("[시스템] 모험가 파티 전멸! (부활 중...)");
                foreach (var adv in Adventurers) 
                { 
                    adv.CurrentHp = adv.MaxHp; 
                    ActionValues[adv] = 0f; 
                }
                foreach (var mon in Monsters) 
                { 
                    ActionValues[mon] = 0f; 
                }
                anyDead = true;
                IsBattleEnded = true;
            }
            else if (isMonWiped)
            {
                AddLog("[시스템] 파티 승리!");
                foreach (var mon in Monsters) 
                { 
                    mon.CurrentHp = mon.MaxHp; 
                    ActionValues[mon] = 0f; 
                }
                foreach (var adv in Adventurers) 
                { 
                    ActionValues[adv] = 0f; 
                }
                anyDead = true;
                IsBattleEnded = true;
            }

            if (anyDead)
            {
                // 전투 상황 처리를 위해 유지
            }

            return anyDead;
        }
    }
}

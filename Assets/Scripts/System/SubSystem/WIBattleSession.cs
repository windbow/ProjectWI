using System.Collections.Generic;
using UnityEngine;
using System;

namespace ProjectWI.SubSystem
{
    // MonoBehaviour가 아닌 순수 C# 클래스. 백그라운드 전투 시뮬레이션 담당.
    public class WIBattleSession
    {
        public string DungeonName { get; private set; }
        
        // 시뮬레이션용 임시 스테이터스 (차후 WICharacterBase 데이터 연동으로 교체 예정)
        public int AdvMaxHp = 100;
        public int AdvCurrentHp = 100;
        public float AdvSpeed = 10f;
        public float AdvTurnProgress = 0f;
        
        public int MonMaxHp = 100;
        public int MonCurrentHp = 100;
        public float MonSpeed = 8f;
        public float MonTurnProgress = 0f;
        
        public Action<string> OnLogAdded; // UI 연동용 옵저버 콜백
        
        private List<string> battleLogs = new List<string>();
        
        // 공격 후 대기 시간 (기획서: "A 행동 후 B 행동 하기 전에 1초 대기")
        private float waitTimeRemaining = 0f;

        public WIBattleSession(string dungeonName)
        {
            DungeonName = dungeonName;
            AddLog($"[시스템] {DungeonName} 전투 시작!");
        }
        
        public void AddLog(string log)
        {
            battleLogs.Add(log);
            if(battleLogs.Count > 50) battleLogs.RemoveAt(0); // 최대 50개 유지
            
            if (OnLogAdded != null)
            {
                OnLogAdded.Invoke(log);
            }
        }
        
        public List<string> GetLogs()
        {
            return battleLogs;
        }
        
        public void Tick(float deltaTime)
        {
            if (waitTimeRemaining > 0f)
            {
                waitTimeRemaining -= deltaTime;
                return;
            }

            // 스피드 기반 턴 게이지 증가 (100 도달 시 행동)
            AdvTurnProgress += AdvSpeed * 2f * deltaTime; 
            MonTurnProgress += MonSpeed * 2f * deltaTime;
            
            if (AdvTurnProgress >= 100f)
            {
                AdvTurnProgress = 0f;
                ExecuteAction("모험가", "몬스터", ref MonCurrentHp, 15);
                CheckDead();
                waitTimeRemaining = 1.0f; // 기획: 행동 후 1초 대기
            }
            else if (MonTurnProgress >= 100f)
            {
                MonTurnProgress = 0f;
                ExecuteAction("몬스터", "모험가", ref AdvCurrentHp, 5);
                CheckDead();
                waitTimeRemaining = 1.0f;
            }
        }
        
        private void ExecuteAction(string attacker, string defender, ref int defHp, int damage)
        {
            defHp -= damage;
            if (defHp < 0) defHp = 0;
            AddLog($"[{attacker}] {defender} 공격! ({damage} 피해)");
        }
        
        private void CheckDead()
        {
            if (AdvCurrentHp <= 0)
            {
                AddLog("[시스템] 모험가 쓰러짐! (부활 중...)");
                AdvCurrentHp = AdvMaxHp;
            }
            if (MonCurrentHp <= 0)
            {
                AddLog("[시스템] 몬스터 처치! 다음 몬스터 조우");
                MonCurrentHp = MonMaxHp;
                AdvTurnProgress = 0f;
                MonTurnProgress = 0f;
            }
        }
    }
}

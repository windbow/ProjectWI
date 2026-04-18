using System.Collections.Generic;
using UnityEngine;
using System;
using ProjectWI.Data;

namespace ProjectWI.SubSystem
{
    public enum WIDungeonState
    {
        Exploring,
        Battling
    }

    public class WIDungeonSession
    {
        /// <summary>현재 탐험 중인 던전의 기본 정보와 발생할 수 있는 이벤트 풀(Pool)을 담고 있는 에셋 데이터</summary>
        public WIDungeonDataSO DungeonData { get; private set; }
        
        /// <summary>현재 던전 세션의 상태 (탐험 중인지, 전투 중인지)</summary>
        public WIDungeonState CurrentState { get; private set; }
        
        /// <summary>전투 돌입 시 턴제 전투 로직을 전담하여 처리하는 하위 배틀 세션</summary>
        public WIBattleSession BattlePhase { get; private set; }
        
        /// <summary>탐험 중 새 이벤트 로그가 발생했음을 UI 등에 알리는 델리게이트</summary>
        public Action<string> OnDungeonLogAdded;
        
        /// <summary>탐험을 통해 모인 텍스트 로그들을 보관하여 늦게 접속한 UI도 렌더링할 수 있도록 돕는 리스트</summary>
        private List<string> dungeonLogs = new List<string>();

        /// <summary>다음 탐험 이벤트 룰렛을 돌리기 위해 차오르고 있는 타이머 값 (0 ~ exploreInterval)</summary>
        private float exploreTimer = 0f;
        
        /// <summary>탐험 이벤트를 한 번 굴리기 위해 필요한 쿨다운 시간 (기본 2초)</summary>
        private float exploreInterval = 2.0f; 
        
        public WIDungeonSession(WIDungeonDataSO data)
        {
            DungeonData = data;
            CurrentState = WIDungeonState.Exploring;
            
            BattlePhase = new WIBattleSession(data.dungeonName);
            
            // 전투 로그도 던전 로그와 통합할 수 있도록 이벤트 라우팅
            BattlePhase.OnLogAdded += AddLog;
            
            AddLog($"[던전 진입] '{data.dungeonName}' 탐험을 시작합니다.");
        }

        public void BindCombatants(List<WICharacterBase> advs, List<WICharacterBase> mons)
        {
            // 배틀 세션에 미리 캐릭터들을 잡아둔다. 
            // 추후 실제 게임에선 몬스터 조우 이벤트 시점마다 mons 리스트를 팩토리에서 새로 찍어내야 합니다.
            BattlePhase.SetCombatants(advs, mons);
        }

        public void AddLog(string msg)
        {
            dungeonLogs.Add(msg);
            
            if (dungeonLogs.Count > 100)
            {
                dungeonLogs.RemoveAt(0);
            }

            if (OnDungeonLogAdded != null)
            {
                OnDungeonLogAdded.Invoke(msg);
            }
        }
        
        public List<string> GetLogs()
        {
            return dungeonLogs;
        }

        // UI에 보여질 프로그레스 바(게이지) 비율
        public float GetExploreProgressRatio()
        {
            // 탐험 상태일 때는 탐험 타이머(2초) 비율
            if (CurrentState == WIDungeonState.Exploring)
            {
                return Mathf.Clamp01(exploreTimer / exploreInterval);
            }

            // 전투 중일 때는 턴 대기시간(1초) 비율
            return BattlePhase.GetTurnProgressRatio();
        }

        public void Tick(float deltaTime)
        {
            if (CurrentState == WIDungeonState.Exploring)
            {
                exploreTimer += deltaTime;
                if (exploreTimer >= exploreInterval)
                {
                    exploreTimer = 0f;
                    ProcessRandomEvent();
                }
            }
            else if (CurrentState == WIDungeonState.Battling)
            {
                BattlePhase.Tick(deltaTime);
                
                // 전투 상황 내부에서 전멸/승리를 통해 전투 종료 트리거가 켜짐
                if (BattlePhase.IsBattleEnded)
                {
                    CurrentState = WIDungeonState.Exploring;
                    exploreTimer = 0f; // 다시 탐험 대기
                    AddLog("[던전] 전투 처리가 종료되어 다시 탐험을 시작합니다.");
                }
            }
        }

        private void ProcessRandomEvent()
        {
            if (DungeonData == null)
            {
                AddLog("던전 데이터 없음");
                return;
            }

            WIDungeonEventSO evt = DungeonData.RollRandomEvent();
            if (evt == null)
            {
                AddLog("던전 이벤트 없음");
                return;
            }

            AddLog($"{evt.logMessage}");
            
            switch (evt.eventType)
            {
                // @todo : 골드 획득, 경험치 획득 등의 텍스트는 직접 입력하면 안됨
                case WIDungeonEventType.Treasure:
                    if (evt.rewardGold > 0)
                    {
                        AddLog($"+ {evt.rewardGold} 골드 획득!");
                    }

                    if (evt.rewardExp > 0)
                    {
                        AddLog($"+ {evt.rewardExp} 경험치 획득!");
                    }
                    break;
                    
                case WIDungeonEventType.Story:
                    // 연출 등 추가
                    break;
                    
                case WIDungeonEventType.MonsterEncounter:
                    AddLog("[던전] 몬스터 무리와 조우하여 전투에 돌입합니다!");
                    
                    CurrentState = WIDungeonState.Battling;
                    
                    // 새로 전투 진입시 타이머 등 초기화
                    BattlePhase.ResetBattleState(); 
                    break;
            }
        }
    }
}

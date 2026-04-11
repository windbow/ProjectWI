using UnityEngine;
using System.Collections.Generic;

namespace ProjectWI.SubSystem
{
    public class WIBattleManager : MonoBehaviour
    {
        public static WIBattleManager Instance { get; private set; }
        
        private Dictionary<int, WIDungeonSession> activeSessions = new Dictionary<int, WIDungeonSession>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 컴포넌트 활성화 시 테스트 편의를 위해 던전 세션을 생성하고 
        /// 배수진 전투(더미 모험가 2명 vs 더미 몬스터 2마리)를 하드코딩하여 세션에 주입하고 전투를 개시합니다.
        /// </summary>
        private void Start()
        {
            ProjectWI.Data.WIDungeonDataSO dummyDungeon = ScriptableObject.CreateInstance<ProjectWI.Data.WIDungeonDataSO>();
            dummyDungeon.dungeonId = 1;
            dummyDungeon.dungeonName = "초심자의 숲 (Dungeon 1)";
            dummyDungeon.eventPool = new List<ProjectWI.Data.WIDungeonEventSO>();

            var evtNone = ScriptableObject.CreateInstance<ProjectWI.Data.WIDungeonEventSO>();
            evtNone.eventType = ProjectWI.Data.WIDungeonEventType.None;
            evtNone.logMessage = "어두운 통로를 걷고 있습니다...";
            evtNone.weight = 30;
            dummyDungeon.eventPool.Add(evtNone);

            var evtTreasure = ScriptableObject.CreateInstance<ProjectWI.Data.WIDungeonEventSO>();
            evtTreasure.eventType = ProjectWI.Data.WIDungeonEventType.Treasure;
            evtTreasure.logMessage = "오래된 나무 상자를 발견했습니다!";
            evtTreasure.rewardGold = 100;
            evtTreasure.weight = 10;
            dummyDungeon.eventPool.Add(evtTreasure);

            var evtMonster = ScriptableObject.CreateInstance<ProjectWI.Data.WIDungeonEventSO>();
            evtMonster.eventType = ProjectWI.Data.WIDungeonEventType.MonsterEncounter;
            evtMonster.logMessage = "어둠 속에서 적재적의 눈빛이 번뜩입니다!";
            evtMonster.weight = 20;
            dummyDungeon.eventPool.Add(evtMonster);

            WIDungeonSession session = CreateSession(1, dummyDungeon);
            
            // 더미 모험가 1 (전사)
            GameObject advObj1 = new GameObject("DummyAdv_Warrior");
            advObj1.transform.SetParent(this.transform);
            WIAdventurer adv1 = advObj1.AddComponent<WIAdventurer>();
            adv1.Name = "김철수(전사)";
            adv1.MaxHp = 100;
            adv1.CurrentHp = 100;
            adv1.AttackPower = 10;
            adv1.Speed = 10f;
            
            WIJob warriorJob = new WIJob();
            warriorJob.JobID = 1;
            warriorJob.JobName = "전사";
            warriorJob.Skills[0] = new WISkill() { SkillID = 101, SkillName = "파워 스트라이크", Cooldown = 3f, DamageMultiplier = 2 };
            adv1.Job = warriorJob;

            // 더미 모험가 2 (마법사)
            GameObject advObj2 = new GameObject("DummyAdv_Mage");
            advObj2.transform.SetParent(this.transform);
            WIAdventurer adv2 = advObj2.AddComponent<WIAdventurer>();
            adv2.Name = "이영희(마법사)";
            adv2.MaxHp = 60;
            adv2.CurrentHp = 60;
            adv2.AttackPower = 15;
            adv2.Speed = 8f;
            
            WIJob mageJob = new WIJob();
            mageJob.JobID = 2;
            mageJob.JobName = "마법사";
            mageJob.Skills[0] = new WISkill() { SkillID = 201, SkillName = "파이어볼", Cooldown = 4f, DamageMultiplier = 3 }; 
            adv2.Job = mageJob;
            
            // 더미 몬스터 1 (슬라임)
            GameObject monObj1 = new GameObject("DummyMon_Slime");
            monObj1.transform.SetParent(this.transform);
            WIMonster mon1 = monObj1.AddComponent<WIMonster>();
            mon1.Name = "슬라임A";
            mon1.MaxHp = 80;
            mon1.CurrentHp = 80;
            mon1.AttackPower = 5;
            mon1.Speed = 7f;
            
            WIJob slimeJob = new WIJob();
            slimeJob.JobID = 99;
            slimeJob.JobName = "슬라임 기본";
            slimeJob.Skills[0] = new WISkill() { SkillID = 901, SkillName = "몸통박치기", Cooldown = 1.5f, DamageMultiplier = 1 };
            mon1.Job = slimeJob;

            // 더미 몬스터 2 (고블린)
            GameObject monObj2 = new GameObject("DummyMon_Goblin");
            monObj2.transform.SetParent(this.transform);
            WIMonster mon2 = monObj2.AddComponent<WIMonster>();
            mon2.Name = "고블린A";
            mon2.MaxHp = 50;
            mon2.CurrentHp = 50;
            mon2.AttackPower = 8;
            mon2.Speed = 11.5f;
            
            WIJob goblinJob = new WIJob();
            goblinJob.JobID = 100;
            goblinJob.JobName = "고블린 기본";
            goblinJob.Skills[0] = new WISkill() { SkillID = 902, SkillName = "빠른 찌르기", Cooldown = 1.0f, DamageMultiplier = 1 };
            mon2.Job = goblinJob;
            
            List<WICharacterBase> advs = new List<WICharacterBase>() { adv1, adv2 };
            List<WICharacterBase> mons = new List<WICharacterBase>() { mon1, mon2 };

            session.BindCombatants(advs, mons);
        }

        /// <summary>
        /// 새로운 무한사냥 던전의 배틀 세션을 생성합니다. 이미 해당 던전 ID의 세션이 구동 중이라면 기존 세션을 반환합니다.
        /// </summary>
        public WIDungeonSession CreateSession(int dungeonId, ProjectWI.Data.WIDungeonDataSO data)
        {
            if (activeSessions.ContainsKey(dungeonId)) return activeSessions[dungeonId];
            
            WIDungeonSession newSession = new WIDungeonSession(data);
            activeSessions.Add(dungeonId, newSession);
            return newSession;
        }

        /// <summary>
        /// 던전 ID를 통해 딕셔너리에 보관된 기존 실행 세션을 식별해 가져옵니다. (UI 바인딩 등의 용도)
        /// </summary>
        public WIDungeonSession GetSession(int dungeonId)
        {
            if (activeSessions.ContainsKey(dungeonId))
            {
                return activeSessions[dungeonId];
            }
            return null;
        }

        /// <summary>
        /// Unity 엔진의 Update 사이클마다 현재 진행 중인 모든 백그라운드 전투 세션의 틱(Tick)을 흘려보냅니다.
        /// </summary>
        private void Update()
        {
            float dt = Time.deltaTime;
            foreach(var kvp in activeSessions)
            {
                kvp.Value.Tick(dt);
            }
        }
    }
}

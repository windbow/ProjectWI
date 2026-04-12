using UnityEngine;
using System.Collections.Generic;

namespace ProjectWI.SubSystem
{
    public class WIBattleManager : MonoBehaviour
    {
        /// <summary>전역에서 쉽게 접근하기 위한 싱글톤 인스턴스 (게임 초기화 시 등록됨)</summary>
        public static WIBattleManager Instance { get; private set; }
        
        /// <summary>던전 ID를 키값으로 하여 현재 백그라운드에서 돌아가고 있는 모든 던전 세션들을 관리하는 풀(Pool)</summary>
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
#if UNITY_EDITOR
            // 1. 저장된 던전 데이터를 에셋에서 불러옵니다.
            ProjectWI.Data.WIDungeonDataSO dunSO = UnityEditor.AssetDatabase.LoadAssetAtPath<ProjectWI.Data.WIDungeonDataSO>("Assets/Data/ScriptableObject/DungeonData/DUN_Beginner.asset");
            if (dunSO == null)
            {
                Debug.LogError("던전 더미 데이터가 없습니다! 관리자 창에서 에셋 생성 또는 경로를 확인해주세요.");
                return;
            }

            WIDungeonSession session = CreateSession(dunSO.dungeonId, dunSO);
            
            // 2. 사전에 스크립트로 생성해둔 직업/몬스터/스킬 데이터를 불러옵니다.
            var warriorSO = UnityEditor.AssetDatabase.LoadAssetAtPath<ProjectWI.Data.WIJobDataSO>("Assets/Data/ScriptableObject/JobData/JOB_Warrior.asset");
            var mageSO = UnityEditor.AssetDatabase.LoadAssetAtPath<ProjectWI.Data.WIJobDataSO>("Assets/Data/ScriptableObject/JobData/JOB_Mage.asset");
            var slimeSO = UnityEditor.AssetDatabase.LoadAssetAtPath<ProjectWI.Data.WIMonsterDataSO>("Assets/Data/ScriptableObject/MonsterData/MON_Slime.asset");
            var goblinSO = UnityEditor.AssetDatabase.LoadAssetAtPath<ProjectWI.Data.WIMonsterDataSO>("Assets/Data/ScriptableObject/MonsterData/MON_Goblin.asset");
            
            var pStrikeSO = UnityEditor.AssetDatabase.LoadAssetAtPath<ProjectWI.Data.WISkillDataSO>("Assets/Data/ScriptableObject/SkillData/SKL_PowerStrike.asset");
            var fBallSO = UnityEditor.AssetDatabase.LoadAssetAtPath<ProjectWI.Data.WISkillDataSO>("Assets/Data/ScriptableObject/SkillData/SKL_Fireball.asset");
            var tackleSO = UnityEditor.AssetDatabase.LoadAssetAtPath<ProjectWI.Data.WISkillDataSO>("Assets/Data/ScriptableObject/SkillData/SKL_Tackle.asset");
            var stabSO = UnityEditor.AssetDatabase.LoadAssetAtPath<ProjectWI.Data.WISkillDataSO>("Assets/Data/ScriptableObject/SkillData/SKL_Stab.asset");

            // 더미 모험가 1 (전사)
            GameObject advObj1 = new GameObject("DummyAdv_Warrior");
            advObj1.transform.SetParent(this.transform);
            WIAdventurer adv1 = advObj1.AddComponent<WIAdventurer>();
            adv1.Name = "김철수(" + warriorSO.jobName + ")";
            adv1.MaxHp = (int)warriorSO.baseHp;
            adv1.CurrentHp = adv1.MaxHp;
            adv1.AttackPower = (int)warriorSO.baseAttack;
            adv1.Speed = warriorSO.baseSpeed;
            
            WIJob warriorJob = new WIJob() { JobID = 1, JobName = warriorSO.jobName };
            warriorJob.Skills[0] = new WISkill() { SkillID = 101, SkillName = pStrikeSO.skillName, Cooldown = pStrikeSO.cooldown, DamageMultiplier = pStrikeSO.damageMultiplier };
            adv1.Job = warriorJob;

            // 더미 모험가 2 (마법사)
            GameObject advObj2 = new GameObject("DummyAdv_Mage");
            advObj2.transform.SetParent(this.transform);
            WIAdventurer adv2 = advObj2.AddComponent<WIAdventurer>();
            adv2.Name = "이영희(" + mageSO.jobName + ")";
            adv2.MaxHp = (int)mageSO.baseHp;
            adv2.CurrentHp = adv2.MaxHp;
            adv2.AttackPower = (int)mageSO.baseAttack;
            adv2.Speed = mageSO.baseSpeed;
            
            WIJob mageJob = new WIJob() { JobID = 2, JobName = mageSO.jobName };
            mageJob.Skills[0] = new WISkill() { SkillID = 201, SkillName = fBallSO.skillName, Cooldown = fBallSO.cooldown, DamageMultiplier = fBallSO.damageMultiplier }; 
            adv2.Job = mageJob;
            
            // 더미 몬스터 1 (슬라임)
            GameObject monObj1 = new GameObject("DummyMon_Slime");
            monObj1.transform.SetParent(this.transform);
            WIMonster mon1 = monObj1.AddComponent<WIMonster>();
            mon1.Name = slimeSO.monsterName;
            mon1.MaxHp = (int)slimeSO.maxHp;
            mon1.CurrentHp = mon1.MaxHp;
            mon1.AttackPower = (int)slimeSO.attackPower;
            mon1.Speed = slimeSO.speed;
            
            WIJob slimeJob = new WIJob() { JobID = 99, JobName = "슬라임" };
            slimeJob.Skills[0] = new WISkill() { SkillID = 901, SkillName = tackleSO.skillName, Cooldown = tackleSO.cooldown, DamageMultiplier = tackleSO.damageMultiplier };
            mon1.Job = slimeJob;

            // 더미 몬스터 2 (고블린)
            GameObject monObj2 = new GameObject("DummyMon_Goblin");
            monObj2.transform.SetParent(this.transform);
            WIMonster mon2 = monObj2.AddComponent<WIMonster>();
            mon2.Name = goblinSO.monsterName;
            mon2.MaxHp = (int)goblinSO.maxHp;
            mon2.CurrentHp = mon2.MaxHp;
            mon2.AttackPower = (int)goblinSO.attackPower;
            mon2.Speed = goblinSO.speed;
            
            WIJob goblinJob = new WIJob() { JobID = 100, JobName = "고블린" };
            goblinJob.Skills[0] = new WISkill() { SkillID = 902, SkillName = stabSO.skillName, Cooldown = stabSO.cooldown, DamageMultiplier = stabSO.damageMultiplier };
            mon2.Job = goblinJob;
            
            List<WICharacterBase> advs = new List<WICharacterBase>() { adv1, adv2 };
            List<WICharacterBase> mons = new List<WICharacterBase>() { mon1, mon2 };

            session.BindCombatants(advs, mons);
#endif
        }

        /// <summary>
        /// 새로운 무한사냥 던전의 배틀 세션을 생성합니다. 이미 해당 던전 ID의 세션이 구동 중이라면 기존 세션을 반환합니다.
        /// </summary>
        public WIDungeonSession CreateSession(int dungeonId, ProjectWI.Data.WIDungeonDataSO data)
        {
            if (activeSessions.ContainsKey(dungeonId)) 
            {
                return activeSessions[dungeonId];
            }
            
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

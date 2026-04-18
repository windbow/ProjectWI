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
            // 이제 하드코딩된 초기화 대신 UI와 연동하여 동적으로 전투를 시작합니다.
        }

        /// <summary>
        /// 선택된 파티 멤버들을 기반으로 특정 던전의 전투를 시작합니다.
        /// </summary>
        public void StartBattle(int dungeonId, List<WIAdventurerInstance> partyMembers)
        {
            // 1. 던전 데이터 로드
            ProjectWI.Data.WIDungeonDataSO dunSO = UnityEditor.AssetDatabase.LoadAssetAtPath<ProjectWI.Data.WIDungeonDataSO>("Assets/Data/ScriptableObject/DungeonData/DUN_Beginner.asset");
            if (dunSO == null)
            {
                Debug.LogError("던전 데이터를 찾을 수 없습니다.");
                return;
            }

            // 2. 세션 생성 또는 가져오기
            WIDungeonSession session = CreateSession(dungeonId, dunSO);
            
            // 3. 기존에 생성된 캐릭터 오브젝트가 있다면 제거 (새로운 전투 시작을 위해)
            foreach (Transform child in this.transform)
            {
                Destroy(child.gameObject);
            }

            // 4. 파티 멤버(모험가) 생성 및 데이터 설정
            List<WICharacterBase> adventurerCombatants = new List<WICharacterBase>();
            foreach (var member in partyMembers)
            {
                GameObject advObj = new GameObject($"Adv_{member.Name}");
                advObj.transform.SetParent(this.transform);
                WIAdventurer adv = advObj.AddComponent<WIAdventurer>();
                
                // 데이터 주입
                adv.Name = member.Name;
                adv.MaxHp = (int)member.JobData.baseHp;
                adv.CurrentHp = adv.MaxHp;
                adv.AttackPower = (int)member.JobData.baseAttack;
                adv.Speed = member.JobData.baseSpeed;
                
                int jid = 0;
                int.TryParse(member.JobData.id, out jid);
                WIJob job = new WIJob() { JobID = jid, JobName = member.JobData.jobName };
                // 스킬이 있다면 첫 번째 스킬 연동 (더미 데이터 구조 유지)
                if (member.JobData.skills != null && member.JobData.skills.Length > 0)
                {
                    var skillSO = member.JobData.skills[0];
                    job.Skills[0] = new WISkill() 
                    { 
                        SkillID = skillSO.skillId, 
                        SkillName = skillSO.skillName, 
                        Cooldown = skillSO.cooldown, 
                        DamageMultiplier = skillSO.damageMultiplier 
                    };
                }
                adv.Job = job;
                
                adventurerCombatants.Add(adv);
            }

            // 5. 몬스터 생성 (현재는 고블린/슬라임 고정 테스트 유지, 추후 던전 데이터 연동)
            var slimeSO = UnityEditor.AssetDatabase.LoadAssetAtPath<ProjectWI.Data.WIMonsterDataSO>("Assets/Data/ScriptableObject/MonsterData/MON_Slime.asset");
            var goblinSO = UnityEditor.AssetDatabase.LoadAssetAtPath<ProjectWI.Data.WIMonsterDataSO>("Assets/Data/ScriptableObject/MonsterData/MON_Goblin.asset");
            
            List<WICharacterBase> monsterCombatants = new List<WICharacterBase>();
            
            // 슬라임 추가
            GameObject mon1Obj = new GameObject("Mon_Slime");
            mon1Obj.transform.SetParent(this.transform);
            WIMonster mon1 = mon1Obj.AddComponent<WIMonster>();
            mon1.Name = slimeSO.monsterName;
            mon1.MaxHp = (int)slimeSO.maxHp;
            mon1.CurrentHp = mon1.MaxHp;
            mon1.AttackPower = (int)slimeSO.attackPower;
            mon1.Speed = slimeSO.speed;
            monsterCombatants.Add(mon1);

            // 6. 세션에 전투원 바인딩 및 초기화
            session.BindCombatants(adventurerCombatants, monsterCombatants);
            session.BattlePhase.ResetBattleState();
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

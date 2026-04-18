using System.Collections.Generic;
using UnityEngine;
using ProjectWI.Data;

namespace ProjectWI.SubSystem
{
    /// <summary>
    /// 플레이어의 소유 자산(영입한 캐릭터, 파티 구성 등)을 관리하는 데이터 클래스입니다.
    /// </summary>
    public class WIPlayerData : MonoBehaviour
    {
        /// <summary>전역 접근을 위한 싱글톤 인스턴스</summary>
        public static WIPlayerData Instance { get; private set; }

        /// <summary>현재 플레이어가 영입하여 보유 중인 모든 모험가 데이터 목록</summary>
        [SerializeField] private List<WIAdventurerInstance> recruitedAdventurers = new List<WIAdventurerInstance>();
        
        /// <summary>전투에 참여하기 위해 편성된 파티 목록 (최대 2명)</summary>
        [SerializeField] private List<WIAdventurerInstance> currentParty = new List<WIAdventurerInstance>();

        public List<WIAdventurerInstance> RecruitedAdventurers => recruitedAdventurers;
        public List<WIAdventurerInstance> CurrentParty => currentParty;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 새로운 모험가를 영입 목록에 추가합니다.
        /// </summary>
        public void RecruitAdventurer(WIJobDataSO jobData)
        {
            WIAdventurerInstance newAdv = new WIAdventurerInstance(jobData);
            recruitedAdventurers.Add(newAdv);
            Debug.Log($"[선술집] {newAdv.Name}({jobData.jobName})를 영입했습니다!");
        }

        /// <summary>
        /// 파티에 캐릭터를 추가하거나 교체합니다.
        /// </summary>
        public void SetParty(List<WIAdventurerInstance> selectedAdvs)
        {
            currentParty = new List<WIAdventurerInstance>(selectedAdvs);
        }
    }

    /// <summary>
    /// 개별 모험가의 인스턴스 데이터 (레벨, 스탯 등 저장용)
    /// </summary>
    [System.Serializable]
    public class WIAdventurerInstance
    {
        public string Name;
        public WIJobDataSO JobData;
        public int Level = 1;
        public float CurrentExp = 0;

        public WIAdventurerInstance(WIJobDataSO jobData)
        {
            this.JobData = jobData;
            this.Name = jobData.jobName; // 기본 이름은 직업명으로 설정
        }
    }
}

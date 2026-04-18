using UnityEngine;

namespace ProjectWI.Data
{
    [CreateAssetMenu(fileName = "NewJobData", menuName = "WIData/JobData", order = 1)]
    public class WIJobDataSO : ScriptableObject
    {
        public string id;
        public string jobName;
        public string description;
        
        [Header("Base Stats")]
        public float baseHp;
        public float baseSpeed;
        public float baseAttack;

        public WISkillDataSO[] skills;
    }
}

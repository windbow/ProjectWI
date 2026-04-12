using UnityEngine;

namespace ProjectWI.Data
{
    [CreateAssetMenu(fileName = "NewJobData", menuName = "WI Data/Job Data")]
    public class WIJobDataSO : ScriptableObject
    {
        public string id;
        public string jobName;
        public string description;
        
        [Header("Base Stats")]
        public float baseHp;
        public float baseSpeed;
        public float baseAttack;
    }
}

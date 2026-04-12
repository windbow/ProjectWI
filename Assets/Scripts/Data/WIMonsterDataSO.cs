using UnityEngine;

namespace ProjectWI.Data
{
    [CreateAssetMenu(fileName = "NewMonsterData", menuName = "WI Data/Monster Data")]
    public class WIMonsterDataSO : ScriptableObject
    {
        public string id;
        public string monsterName;
        
        [Header("Base Stats")]
        public float maxHp;
        public float speed;
        public float attackPower;
        
        [Header("Rewards")]
        public int killRewardGold;
        public int killRewardExp;
    }
}

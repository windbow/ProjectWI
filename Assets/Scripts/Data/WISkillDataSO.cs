using UnityEngine;

namespace ProjectWI.Data
{
    [CreateAssetMenu(fileName = "NewSkillData", menuName = "WI/Data/Skill Data", order = 0)]
    public class WISkillDataSO : ScriptableObject
    {
        [Tooltip("고유 식별자 (임포터 맵핑용)")]
        public string id;
        
        [Tooltip("스킬 이름")]
        public string skillName;
        
        [Tooltip("스킬 효과 설명")]
        [TextArea(2, 5)]
        public string description;
        
        [Tooltip("스킬 쿨타임 (초)")]
        public float cooldown;
        
        [Tooltip("피해량 계수 (1.0 = 100%)")]
        public float damageMultiplier;
        
        [Tooltip("소모 마나가 있다면 설정")]
        public int manaCost;
    }
}

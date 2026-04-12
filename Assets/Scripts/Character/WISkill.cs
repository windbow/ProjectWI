using UnityEngine;

[System.Serializable]
public class WISkill
{
    public int SkillID;
    public string SkillName;
    
    [Header("Skill Properties")]
    public float Cooldown;
    public float DamageMultiplier; // 배율 등 임시 데이터
    
    // 향후 스킬의 이펙트, 타겟팅 타입, 버프 로직 등이 추가될 수 있습니다.
}

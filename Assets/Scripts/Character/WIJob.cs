using UnityEngine;

[System.Serializable]
public class WIJob
{
    public int JobID;
    public string JobName;

    [Header("Job Skills")]
    // 모든 직업은 3개의 스킬을 가짐
    public WISkill[] Skills = new WISkill[3];
    
    // 직업 고유의 보너스 스탯 등 추가 가능
}

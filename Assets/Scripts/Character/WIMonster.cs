using UnityEngine;

public class WIMonster : WICharacterBase
{
    [Header("Monster Specific")]
    [SerializeField] private int dropGold;
    [SerializeField] private int dropExp;
    
    private void Awake()
    {
        characterName = "몬스터";
    }

    public override void OnTurnStart()
    {
        base.OnTurnStart();
        // 몬스터 턴 행동 로직
    }
}

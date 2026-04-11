using UnityEngine;

public class WIAdventurer : WICharacterBase
{
    // 추가적인 모험가 고유 프로퍼티가 들어갑니다.
    
    private void Awake()
    {
        characterName = "모험가";
    }

    public override void OnTurnStart()
    {
        base.OnTurnStart();
        // 모험가 턴 행동 로직
    }
}

using UnityEngine;

public class WICharacterBase : MonoBehaviour
{
    [Header("Basic Info")]
    [SerializeField] protected string characterName;
    [SerializeField] protected WIJob currentJob;
    
    // 메모리 조작 방지를 위한 Obscured 타입 변수들 적용
    protected WIObscuredInt level = new WIObscuredInt(1);
    protected WIObscuredInt str = new WIObscuredInt(5);
    protected WIObscuredInt dex = new WIObscuredInt(5);
    protected WIObscuredInt intl = new WIObscuredInt(5);
    protected WIObscuredInt vit = new WIObscuredInt(5);
    protected WIObscuredInt luk = new WIObscuredInt(5);
    
    protected WIObscuredInt attackPower = new WIObscuredInt(10);
    protected WIObscuredInt defense = new WIObscuredInt(5);
    protected WIObscuredInt magicAttack = new WIObscuredInt(0);
    protected WIObscuredInt magicDefense = new WIObscuredInt(5);
    
    protected WIObscuredFloat evasionRate = new WIObscuredFloat(0.05f);
    protected WIObscuredFloat accuracy = new WIObscuredFloat(0.95f);
    protected WIObscuredFloat criticalRate = new WIObscuredFloat(0.1f);
    protected WIObscuredFloat criticalDamage = new WIObscuredFloat(1.5f);
    protected WIObscuredFloat speed = new WIObscuredFloat(10f); 
    
    protected WIObscuredFloat cooldownReduction = new WIObscuredFloat(0f);
    protected WIObscuredFloat expGainBonus = new WIObscuredFloat(0f);
    
    public int GetSpeed() 
    {
        return (int)speed.GetValue();
    }
    
    public virtual void TakeDamage(int damage)
    {
        // 공통 데미지 처리
    }
    
    public virtual void OnTurnStart()
    {
        // 캐릭터 턴 시작 시 처리
    }
}

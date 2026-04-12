using UnityEngine;

public class WICharacterBase : MonoBehaviour
{
    [Header("Basic Info")]
    [SerializeField] protected string characterName;
    [SerializeField] protected WIJob currentJob;
    
    protected WIObscuredInt maxHp = new WIObscuredInt(100);
    protected WIObscuredInt currentHp = new WIObscuredInt(100);

    public string Name { get { return characterName; } set { characterName = value; } }
    public WIJob Job { get { return currentJob; } set { currentJob = value; } }
    
    public int MaxHp { get { return maxHp.GetValue(); } set { maxHp.SetValue(value); } }
    public int CurrentHp { get { return currentHp.GetValue(); } set { currentHp.SetValue(value); } }
    public int AttackPower { get { return attackPower.GetValue(); } set { attackPower.SetValue(value); } }
    public float Speed { get { return speed.GetValue(); } set { speed.SetValue(value); } }
    
    // 캐릭터의 베이스 레벨이나 공통 레벨로 사용할 수 있습니다.
    protected WIObscuredInt level = new WIObscuredInt(1);
    
    // 직업별 레벨과 경험치를 저장하는 리스트 (인스펙터 노출용)
    [SerializeField] protected System.Collections.Generic.List<WIJobLevelData> jobLevels = new System.Collections.Generic.List<WIJobLevelData>();
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
    
    /// <summary>HP가 변경될 때 UI 등 구독자에게 알리는 이벤트</summary>
    public System.Action OnHpChanged;

    public virtual void TakeDamage(int damage)
    {
        int nextHp = currentHp.GetValue() - damage;
        if (nextHp < 0)
        {
            nextHp = 0;
        }
        currentHp.SetValue(nextHp);

        if (OnHpChanged != null)
        {
            OnHpChanged.Invoke();
        }
    }
    
    public virtual void OnTurnStart()
    {
        // 캐릭터 턴 시작 시 처리
    }
    
    /// <summary>
    /// 대상 Job의 레벨을 반환합니다. 데이터가 없으면 기본값인 1을 반환합니다.
    /// </summary>
    public int GetJobLevel(int jobId)
    {
        var jobLevel = jobLevels.Find(j => j.JobID == jobId);
        if (jobLevel != null)
        {
            return jobLevel.Level.GetValue();
        }
        return 1;
    }

    /// <summary>
    /// 대상 Job에 경험치를 추가합니다. 처음 경험치를 얻는 직업이면 새 데이터를 생성합니다.
    /// </summary>
    public void AddJobExp(int jobId, float exp)
    {
        var jobLevel = jobLevels.Find(j => j.JobID == jobId);
        if (jobLevel == null)
        {
            jobLevel = new WIJobLevelData(jobId, 1, 0f);
            jobLevels.Add(jobLevel);
        }
        
        float currentExp = jobLevel.Exp.GetValue();
        jobLevel.Exp.SetValue(currentExp + exp);
        
        // 향후 추가: 레벨업 시 필요 경험치 테이블을 참조하여 레벨업 처리 로직 구현
    }
}

using UnityEngine;

[System.Serializable]
public class WIJobLevelData
{
    public int JobID;
    public WIObscuredInt Level;
    public WIObscuredFloat Exp;

    public WIJobLevelData(int jobId, int initialLevel = 1, float initialExp = 0f)
    {
        JobID = jobId;
        Level = new WIObscuredInt(initialLevel);
        Exp = new WIObscuredFloat(initialExp);
    }
}

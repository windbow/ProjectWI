using UnityEngine;

[System.Serializable]
public struct WIObscuredInt
{
    [SerializeField] private int cryptoKey;
    [SerializeField] private int hiddenValue;

    public WIObscuredInt(int value)
    {
        cryptoKey = Random.Range(1000, 9999);
        hiddenValue = value ^ cryptoKey;
    }

    public int GetValue()
    {
        return hiddenValue ^ cryptoKey;
    }

    public void SetValue(int value)
    {
        cryptoKey = UnityEngine.Random.Range(1000, 9999);
        hiddenValue = value ^ cryptoKey;
    }
}

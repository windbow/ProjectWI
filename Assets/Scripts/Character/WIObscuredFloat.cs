using UnityEngine;

[System.Serializable]
public struct WIObscuredFloat
{
    [SerializeField] private float cryptoKey;
    [SerializeField] private float hiddenValue;

    public WIObscuredFloat(float value)
    {
        cryptoKey = UnityEngine.Random.Range(-1000f, 1000f);
        hiddenValue = value + cryptoKey;
    }

    public float GetValue()
    {
        return hiddenValue - cryptoKey;
    }

    public void SetValue(float value)
    {
        cryptoKey = UnityEngine.Random.Range(-1000f, 1000f);
        hiddenValue = value + cryptoKey;
    }
}

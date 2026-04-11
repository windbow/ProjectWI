using UnityEngine;

using System;

[System.Serializable]
public struct WIObscuredFloat
{
    private static System.Random rng = new System.Random();

    [SerializeField] private float cryptoKey;
    [SerializeField] private float hiddenValue;

    public WIObscuredFloat(float value)
    {
        cryptoKey = (float)(rng.NextDouble() * 2000.0 - 1000.0);
        hiddenValue = value + cryptoKey;
    }

    public float GetValue()
    {
        return hiddenValue - cryptoKey;
    }

    public void SetValue(float value)
    {
        cryptoKey = (float)(rng.NextDouble() * 2000.0 - 1000.0);
        hiddenValue = value + cryptoKey;
    }
}

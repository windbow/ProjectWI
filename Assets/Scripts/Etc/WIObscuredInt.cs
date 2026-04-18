using UnityEngine;

using System;

[System.Serializable]
public struct WIObscuredInt
{
    private static System.Random rng = new System.Random();

    [SerializeField] private int cryptoKey;
    [SerializeField] private int hiddenValue;

    public WIObscuredInt(int value)
    {
        cryptoKey = rng.Next(1000, 9999);
        hiddenValue = value ^ cryptoKey;
    }

    public int GetValue()
    {
        return hiddenValue ^ cryptoKey;
    }

    public void SetValue(int value)
    {
        cryptoKey = rng.Next(1000, 9999);
        hiddenValue = value ^ cryptoKey;
    }
}

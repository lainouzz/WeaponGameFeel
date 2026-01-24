using System;
using UnityEngine;

[SerializeField]
public class DamageStat
{
    public float baseMultiplier = 1f;
    public float upgradeBonus = 0f;

    public event Action<float, float> OnValueChanged;

    public float FinalMultiplier => baseMultiplier + upgradeBonus;

    public DamageStat(float initialMultiplier)
    {
        baseMultiplier = initialMultiplier;
    }

    internal void SetMultiplier(float newMultiplier)
    {
        if (Mathf.Approximately(baseMultiplier, newMultiplier)) return;

        float old = FinalMultiplier;
        baseMultiplier = newMultiplier;
        OnValueChanged?.Invoke(FinalMultiplier, old);
    }

    // Optional: nicer upgrade method
    public void AddUpgradeBonus(float amount)
    {
        upgradeBonus += amount;
        OnValueChanged?.Invoke(FinalMultiplier, FinalMultiplier - amount);
    }
}

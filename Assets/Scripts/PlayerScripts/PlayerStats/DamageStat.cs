using System;
using UnityEngine;

[SerializeField]
public class DamageStat
{

    public float baseDamageMultiplaier = 1f;
    public float upgradeBonus = 0f;
    public event Action<float, float> OnValueChanged;

    public float FinalMultiplier => baseDamageMultiplaier + upgradeBonus;

    public DamageStat(float baseDamageMultiplaier)
    {
        baseDamageMultiplaier = baseDamageMultiplaier;
    }

    internal void SetMultiplier(float damageMultiplier)
    {
        damageMultiplier = FinalMultiplier;
    }
}

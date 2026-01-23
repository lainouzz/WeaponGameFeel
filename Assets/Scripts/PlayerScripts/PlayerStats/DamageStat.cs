using System;
using UnityEngine;

[SerializeField]
public class DamageStat
{

    public float baseDamageMultiplaier = 1f;
    public float upgradeBonus = 0f;

    public float FinalMultiplier => baseDamageMultiplaier + upgradeBonus;
}

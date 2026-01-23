using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayerStatsSaveData
{
    public List<StatUpgradeSave> upgrades = new();
    public float currentHealth = 100f;
    public float currentStamina = 100f;
}

[Serializable]
public class StatUpgradeSave
{
    public StatsManager.StatType StatType;
    public int upgradeLevel;
}

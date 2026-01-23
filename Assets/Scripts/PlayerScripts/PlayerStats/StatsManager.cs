using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

public class StatsManager : MonoBehaviour
{
    public static StatsManager Instance { get; private set; }

    [Header("Player Health")]
    public float maxHealth = 100f;
    public float healthRegenRate = 5f;
    public float healthRegenDelay = 3f;

    [Header("Player Stamina")]
    public float maxStamina = 100f;
    public float staminaRegenRate = 10f;
    public float staminaRegenDelay = 2f;

    [Header("Player Damage")]
    public float baseDamage = 10f;
    public float damageMultiplier = 1.5f;

    [Header("Player Upgrade Settings")]
    [SerializeField] private float healthUpgradeAmount = 20f;
    [SerializeField] private float staminaUpgradeAmount = 15f;
    [SerializeField] private float damageUpgradeAmount = 10f;
    [SerializeField] private int upgradeCost = 100;
    [SerializeField] private float upgradeMultiplier = 1.2f;

    [Header("Save Settings")]
    [SerializeField] private string saveFileName = "playerstats.json";
    private string SavePath => Path.Combine(Application.persistentDataPath, saveFileName);

    public HealthStat Health { get; private set; }
    public StaminaStat Stamina { get; private set; }

    public DamageStat Damage { get; private set; }

    private int HealthUpgradeLevel = 0;
    private int StaminaUpgradeLevel = 0;
    private int DamageUpgradeLevel = 0;

    public event Action OnStatsChanged;
    public event Action<string> OnUpgradeFailed;

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        InitializeStats();
        LoadUpgrades();
    }

    private void InitializeStats()
    {
        // Init health
        Health = new HealthStat(maxHealth, maxHealth);
        Health.SetMax(maxHealth, preserveCurrent: false);  // Ensures init at full

        // Init stamina
        Stamina = new StaminaStat(maxStamina, maxStamina);
        Stamina.SetMax(maxStamina, preserveCurrent: false);

        Health.OnDeath += HandlePlayerDeath;
        Health.OnValueChanged += (c, m) => OnStatsChanged?.Invoke();
        Stamina.OnValueChanged += (c, m) => OnStatsChanged?.Invoke();
    }

    // Update is called once per frame
    void Update()
    {
        Health.Regenerate(Time.deltaTime);
        Stamina.Regenerate(Time.deltaTime);
    }

    public void TakeDamage(float damage)
    {
        Health.TakeDamage(damage);
        Debug.Log($"[Stats] Took {damage} damage. Health: {Health.CurrentValue}/{Health.MaxValue}");
    }
    public void Heal(float amount)
    {
        Health.Heal(amount);
        Debug.Log($"[Stats] Healed {amount}. Health: {Health.CurrentValue}/{Health.MaxValue}");
    }
    public void UseStamina(float amount)
    {
        Stamina.Remove(amount);
        Debug.Log($"[Stats] Used {amount} stamina. Stamina: {Stamina.CurrentValue}/{Stamina.MaxValue}");
    }
    public float GetFinalDamage(float baseDamage)
    {
        return baseDamage * Damage.FinalMultiplier;
    }

    // Upgrade

    public int GetUpgradeCost(StatType statType)
    {
       int Level = statType switch
       {
           StatType.Health => HealthUpgradeLevel,
           StatType.Stamina => StaminaUpgradeLevel,
           StatType.Damage => DamageUpgradeLevel,
           _ => 0
       };

       return Mathf.FloorToInt(upgradeCost * Mathf.Pow(upgradeMultiplier, Level));
    }

    public int GetUpgradeLevel(StatType statType)
    {
        return statType switch
        {
            StatType.Health => HealthUpgradeLevel,
            StatType.Stamina => StaminaUpgradeLevel,
            StatType.Damage => DamageUpgradeLevel,
            _ => 0
        };
    }

    public bool TryUpgradeStat(StatType statType)
    {
        int cost = GetUpgradeCost(statType);

        if (PlayerLoadSaveManager.instance == null)
        {
            OnUpgradeFailed?.Invoke("PlayerLoadSaveManager instance is null.");
            return false;
        }
        if (PlayerLoadSaveManager.instance.Credits < cost)
        {
            OnUpgradeFailed?.Invoke($"Not enough credits! Need {cost}c");
            Debug.Log($"[Stats] Upgrade failed - need {cost}c, have {PlayerLoadSaveManager.instance.Credits}c");
            return false;
        }

        PlayerLoadSaveManager.instance.SpendCredits(cost);

        switch (statType)
        {
            case StatType.Health:
                Health.UpgradeMax(healthUpgradeAmount);  // Keeps healing on upgrade
                HealthUpgradeLevel++;
                Debug.Log($"[Stats] Upgraded Health to level {HealthUpgradeLevel}. New Max Health: {Health.MaxValue}");
                break;
            case StatType.Stamina:
                Stamina.UpgradeMax(staminaUpgradeAmount);
                StaminaUpgradeLevel++;
                Debug.Log($"[Stats] Upgraded Stamina to level {StaminaUpgradeLevel}. New Max Stamina: {Stamina.MaxValue}");
                break;
            case StatType.Damage:
                damageMultiplier += damageUpgradeAmount;
                DamageUpgradeLevel++;
                Debug.Log($"[Stats] Upgraded Damage to level {DamageUpgradeLevel}. New Damage Multiplier: {damageMultiplier}");
                break;
        }

        OnStatsChanged?.Invoke();
        SaveUpgrades();  // Auto-save
        return true;
    }

    private void HandlePlayerDeath()
    {
        // Could trigger death screen, respawn, etc.
        Debug.Log("[Stats] Player died!");
        PlayerBehavior.Instance.OnDeath();
    }


    public IEnumerable<StatType> GetAllStatTypes()
    {
        return Enum.GetValues(typeof(StatType)) as StatType[];
    }

    public void ApplyUpgrades()
    {
        // Health: Set total max, preserve current
        float totalHealthAdded = healthUpgradeAmount * HealthUpgradeLevel;
        Health.SetMax(maxHealth + totalHealthAdded, preserveCurrent: true);

        // Stamina: Same
        float totalStaminaAdded = staminaUpgradeAmount * StaminaUpgradeLevel;
        Stamina.SetMax(maxStamina + totalStaminaAdded, preserveCurrent: true);

        // Damage: Direct field update (no current to preserve)
        damageMultiplier = 1.5f + (damageUpgradeAmount * DamageUpgradeLevel);
    }

    public void SetUpgradeLevel(StatType statType, int level)
    {
        switch (statType)
        {
            case StatType.Health: HealthUpgradeLevel = level; break;
            case StatType.Stamina: StaminaUpgradeLevel = level; break;
            case StatType.Damage: DamageUpgradeLevel = level; break;
        }

        ApplyUpgrades();
    }

    public void SaveUpgrades()
    {
        try
        {
            var data = new PlayerStatsSaveData();

            foreach (StatType type in GetAllStatTypes())
            {
                data.upgrades.Add(new StatUpgradeSave
                {
                    StatType = type,
                    upgradeLevel = GetUpgradeLevel(type)
                });
            }

            // Save currents
            data.currentHealth = Health.CurrentValue;
            data.currentStamina = Stamina.CurrentValue;

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SavePath, json);

            Debug.Log($"[Stats] Saved upgrades to {SavePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Stats] Failed to save: {e.Message}");
        }
    }

    public void LoadUpgrades()
    {
        if (!File.Exists(SavePath))
        {
            Debug.Log("[Stats] No save file found, using defaults.");
            return;
        }

        try
        {
            string json = File.ReadAllText(SavePath);
            var data = JsonUtility.FromJson<PlayerStatsSaveData>(json);

            foreach (var upgrade in data.upgrades)
            {
                SetUpgradeLevel(upgrade.StatType, upgrade.upgradeLevel);
            }

            // Restore currents (clamped after max applied)
            Health.SetCurrent(data.currentHealth);
            Stamina.SetCurrent(data.currentStamina);

            Debug.Log($"[Stats] Loaded upgrades from {SavePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Stats] Failed to load: {e.Message}");
        }
    }

    private void OnApplicationQuit()
    {
        SaveUpgrades();
    }

    void OnDestroy()
    {
        if (Health != null)
        {
            Health.OnDeath -= HandlePlayerDeath;
        }
    }

    public enum StatType
    {
        Health,
        Stamina,
        Damage
    }
}

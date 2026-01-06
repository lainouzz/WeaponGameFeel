using System;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;

public class StatsManager : MonoBehaviour
{
    public static StatsManager Instance { get; private set; }

    [Header("Player Health")]
    public float maxHealth = 100f;
    public float healthRegenRate = 5f;
    public float healthRegenDelay = 3f;
    public float currentHealth;

    [Header("Player Stamina")]
    public float maxStamina = 100f;
    public float staminaRegenRate = 10f;
    public float staminaRegenDelay = 2f;
    public float currentStamina;

    [Header("Player Upgrade Settings")]
    [SerializeField] private float healthUpgradeAmount = 20f;
    [SerializeField] private float staminaUpgradeAmount = 15f;
    [SerializeField] private int upgradeCost = 100;
    [SerializeField] private float upgradeMultiplier = 1.2f;

    public HealthStat Health { get; private set; }

    private int HealthUpgradeLevel = 0;
    private int StaminaUpgradeLevel = 0;

    public event Action OnStatsChanged;
    public event Action<String> OnUpgradeFailed;

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        InitializeStats();
    }

    private void InitializeStats()
    {
        Health = new HealthStat(maxHealth, maxHealth);
        currentHealth = maxHealth;
        Health.OnDeath += HandlePlayerDeath;
        Health.OnValueChanged += (c, m) => OnStatsChanged?.Invoke();
    }

    // Update is called once per frame
    void Update()
    {
        Health.Regenerate(Time.deltaTime);
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
    // Upgrade

    public int GetUpgradeCost(StatType statType)
    {
       int Level = statType switch
       {
           StatType.Health => HealthUpgradeLevel,
           _ => 0
       };

       return Mathf.FloorToInt(upgradeCost * Mathf.Pow(upgradeMultiplier, Level));
    }

    public int GetUpgradeLevel(StatType statType)
    {
        return statType switch
        {
            StatType.Health => HealthUpgradeLevel,
            _ => 0
        };
    }

    public bool TryUpgradeStat(StatType statType)
    {
        int cost = GetUpgradeCost(statType);
        
        if(PlayerInventoryManager.instance == null)
        {
            OnUpgradeFailed?.Invoke("PlayerInventoryManager instance is null.");
            return false;
        }

        if(PlayerInventoryManager.instance.Credits < cost)
        {
            OnUpgradeFailed?.Invoke($"Not enough credits! Need {cost}c");
            Debug.Log($"[Stats] Upgrade failed - need {cost}c, have {PlayerInventoryManager.instance.Credits}c");
            return false;
        }

        PlayerInventoryManager.instance.SpendCredits(cost);

        switch (statType)
        {
            case StatType.Health:
                Health.UpgradeMax(healthUpgradeAmount);
                HealthUpgradeLevel++;
                Debug.Log($"[Stats] Upgraded Health to level {HealthUpgradeLevel}. New Max Health: {maxHealth}");
                break;
        }
        OnStatsChanged?.Invoke();
        return true;
    }

    private void HandlePlayerDeath()
    {
        Debug.Log("[Stats] Player died!");
        // Could trigger death screen, respawn, etc.
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
        Stamina
    }
}

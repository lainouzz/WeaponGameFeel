using TMPro;
using UnityEngine;
using static StatsManager;

/// <summary>
/// Connects player inventory and vendor - handles item transfers and selling
/// </summary>
public class ReforgeStationMenuController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInventoryManager playerInventory;
    [SerializeField] private VendorSellManager vendorSellManager;

    [Header("Stat Upgrade UI")]
    [SerializeField] private TMP_Text healthUpgradeCostText;
    [SerializeField] private TMP_Text healthLevelText;
    [SerializeField] private TMP_Text staminaUpgradeCostText;
    [SerializeField] private TMP_Text staminaLevelText;
    [SerializeField] private TMP_Text damageUpgradeCostText;
    [SerializeField] private TMP_Text damageLevelText;

    void OnEnable()
    {
        if (playerInventory != null)
        {
            playerInventory.OnItemRightClicked += HandleTransferToVendor;
        }

        if (vendorSellManager != null)
        {
            vendorSellManager.OnSellConfirmed += HandleSellConfirmed;
        }

        if (StatsManager.Instance != null)
        {
            StatsManager.Instance.OnStatsChanged += UpdateStatUpgradeUI;
        }
        
        UpdateStatUpgradeUI();
    }


    void Start()
    {
        UpdateStatUpgradeUI();
    }

    private void HandleTransferToVendor(InventoryItem item)
    {
        if (item == null || vendorSellManager == null || playerInventory == null)
            return;

        // Remove from player inventory
        playerInventory.RemoveItem(item);

        // Add to vendor sell queue
        vendorSellManager.AddItem(item);

        Debug.Log($"[ReforgeStation] Transferred {item.data.itemName} to vendor");
    }

    private void HandleSellConfirmed(int totalValue)
    {
        if (playerInventory == null) return;

        // Add credits to player
        playerInventory.AddCredits(totalValue);

        Debug.Log($"[ReforgeStation] Sold items for {totalValue} credits");
    }

    /// <summary>
    /// Called by Health Upgrade button in UI
    /// </summary>
    public void OnUpgradeHealthClicked()
    {
        if (StatsManager.Instance != null)
        {
            int costBefore = StatsManager.Instance.GetUpgradeCost(StatType.Health);
            int creditsBefore = PlayerInventoryManager.instance?.Credits ?? 0;
            
            bool success = StatsManager.Instance.TryUpgradeStat(StatType.Health);
            
            if (success)
            {
                int creditsAfter = PlayerInventoryManager.instance?.Credits ?? 0;
                Debug.Log($"[ReforgeStation] Health upgraded! Cost: {costBefore}c | Credits: {creditsBefore} -> {creditsAfter}");
            }
            
            UpdateStatUpgradeUI();
        }
    }

    /// <summary>
    /// Called by Stamina Upgrade button in UI
    /// </summary>
    public void OnUpgradeStaminaClicked()
    {
        if (StatsManager.Instance != null)
        {
            int costBefore = StatsManager.Instance.GetUpgradeCost(StatType.Stamina);
            int creditsBefore = PlayerInventoryManager.instance?.Credits ?? 0;
            
            bool success = StatsManager.Instance.TryUpgradeStat(StatType.Stamina);
            
            if (success)
            {
                int creditsAfter = PlayerInventoryManager.instance?.Credits ?? 0;
                Debug.Log($"[ReforgeStation] Stamina upgraded! Cost: {costBefore}c | Credits: {creditsBefore} -> {creditsAfter}");
            }
            
            UpdateStatUpgradeUI();
        }
    }

    public void OnUpgradeDamageClicked()
    {
        if (StatsManager.Instance != null)
        {
            int costBefore = StatsManager.Instance.GetUpgradeCost(StatType.Damage);
            int creditsBefore = PlayerInventoryManager.instance?.Credits ?? 0;
            
            bool success = StatsManager.Instance.TryUpgradeStat(StatType.Damage);
            
            if (success)
            {
                int creditsAfter = PlayerInventoryManager.instance?.Credits ?? 0;
                Debug.Log($"[ReforgeStation] Damage upgraded! Cost: {costBefore}c | Credits: {creditsBefore} -> {creditsAfter}");
            }
            
            UpdateStatUpgradeUI();
        }
    }

    private void UpdateStatUpgradeUI()
    {
        if (StatsManager.Instance == null) return;

        // Health upgrade UI
        if (healthUpgradeCostText != null)
        {
            int cost = StatsManager.Instance.GetUpgradeCost(StatType.Health);
            healthUpgradeCostText.text = $"Cost: {cost}c";
        }

        if (healthLevelText != null)
        {
            int level = StatsManager.Instance.GetUpgradeLevel(StatType.Health);
            float maxHP = StatsManager.Instance.Health?.MaxValue ?? 0f;
            healthLevelText.text = $"Health Lv.{level} ({maxHP:F0} HP)";
        }

        // Stamina upgrade UI
        if (staminaUpgradeCostText != null)
        {
            int cost = StatsManager.Instance.GetUpgradeCost(StatType.Stamina);
            staminaUpgradeCostText.text = $"Cost: {cost}c";
        }

        if (staminaLevelText != null)
        {
            int level = StatsManager.Instance.GetUpgradeLevel(StatType.Stamina);
            float maxStamina = StatsManager.Instance.Stamina?.MaxValue ?? 0f;
            staminaLevelText.text = $"Stamina Lv.{level} ({maxStamina:F0} SP)";
        }
        
        // Damage upgrade UI
        if (damageUpgradeCostText != null)
        {
            int cost = StatsManager.Instance.GetUpgradeCost(StatType.Damage);
            damageUpgradeCostText.text = $"Cost: {cost}c";
        }

        if (damageLevelText != null)
        {
            int level = StatsManager.Instance.GetUpgradeLevel(StatType.Damage);
            damageLevelText.text = $"Damage Lv.{level}";
        }
    }

    void OnDisable()
    {
        if (playerInventory != null)
        {
            playerInventory.OnItemRightClicked -= HandleTransferToVendor;
        }

        if (vendorSellManager != null)
        {
            vendorSellManager.OnSellConfirmed -= HandleSellConfirmed;
        }

        if (StatsManager.Instance != null)
        {
            StatsManager.Instance.OnStatsChanged -= UpdateStatUpgradeUI;
        }
    }
}

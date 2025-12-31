using UnityEngine;
using TMPro;

/// <summary>
/// Connects player inventory and vendor - handles item transfers and selling
/// </summary>
public class ReforgeStationMenuController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInventoryManager playerInventory;
    [SerializeField] private VendorSellManager vendorSellManager;

    [Header("Upgrade System (Debug)")]
    [SerializeField] private TMP_Text upgradeDebugText;
    [SerializeField] private int upgradeBaseCost = 100;
    
    private int currentUpgradeLevel = 0;
    private int maxUpgradeLevel = 10;

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
    }

    void Start()
    {
        UpdateUpgradeDebugText();
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
    /// Called by Upgrade button in UI
    /// </summary>
    public void OnUpgradeButtonClicked()
    {
        if (playerInventory == null) return;

        if (currentUpgradeLevel >= maxUpgradeLevel)
        {
            Debug.Log("[ReforgeStation] Already at max upgrade level!");
            return;
        }

        int cost = GetUpgradeCost();

        if (playerInventory.SpendCredits(cost))
        {
            currentUpgradeLevel++;
            Debug.Log($"[ReforgeStation] Upgraded to level {currentUpgradeLevel}! Cost: {cost}");
            UpdateUpgradeDebugText();
        }
        else
        {
            Debug.Log($"[ReforgeStation] Not enough credits! Need {cost}, have {playerInventory.Credits}");
        }
    }

    private int GetUpgradeCost()
    {
        // Cost increases with each level
        return upgradeBaseCost * (currentUpgradeLevel + 1);
    }

    private void UpdateUpgradeDebugText()
    {
        if (upgradeDebugText != null)
        {
            int nextCost = currentUpgradeLevel < maxUpgradeLevel ? GetUpgradeCost() : 0;
            string costText = currentUpgradeLevel < maxUpgradeLevel ? $"Next: {nextCost}c" : "MAX LEVEL";
            upgradeDebugText.text = $"Level: {currentUpgradeLevel}/{maxUpgradeLevel}\n{costText}";
        }
    }
}

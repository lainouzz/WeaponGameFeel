using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the vendor's sell inventory (items queued for sale)
/// </summary>
public class VendorSellManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject itemUIPrefab;
    [SerializeField] private Button sellButton;
    [SerializeField] private TMP_Text sellButtonText;
    [SerializeField] private TMP_Text totalValueText;

    /// <summary>
    /// Event fired when sell button is clicked, returns total value sold
    /// </summary>
    public event Action<int> OnSellConfirmed;

    private List<InventoryItem> itemsForSale = new List<InventoryItem>();
    private List<InventoryItemUI> uiInstances = new List<InventoryItemUI>();

    public IReadOnlyList<InventoryItem> ItemsForSale => itemsForSale;

    void Start()
    {
        if (sellButton != null)
        {
            sellButton.onClick.AddListener(OnSellButtonClicked);
        }
        UpdateSellUI();
    }

    public void AddItem(InventoryItem item)
    {
        if (item == null || item.data == null) return;

        itemsForSale.Add(item.Clone());
        RefreshUI();
    }

    public void RemoveItem(InventoryItem item)
    {
        if (item == null) return;

        itemsForSale.Remove(item);
        RefreshUI();
    }

    public void ClearAll()
    {
        itemsForSale.Clear();
        RefreshUI();
    }

    public int GetTotalValue()
    {
        int total = 0;
        foreach (var item in itemsForSale)
        {
            total += item.TotalSellValue;
        }
        return total;
    }

    private void OnSellButtonClicked()
    {
        if (itemsForSale.Count == 0) return;

        int totalValue = GetTotalValue();
        
        Debug.Log($"[Vendor] Selling {itemsForSale.Count} items for {totalValue} credits");
        
        // Fire event for controller to handle
        OnSellConfirmed?.Invoke(totalValue);
        
        // Clear the sell queue
        itemsForSale.Clear();
        RefreshUI();
    }

    private void UpdateSellUI()
    {
        int totalValue = GetTotalValue();
        int itemCount = itemsForSale.Count;

        if (sellButtonText != null)
        {
            sellButtonText.text = itemCount > 0 ? $"Sell ({itemCount})" : "Sell";
        }

        if (totalValueText != null)
        {
            totalValueText.text = $"{totalValue}c";
        }

        if (sellButton != null)
        {
            sellButton.interactable = itemCount > 0;
        }
    }

    public void RefreshUI()
    {
        // Clear existing UI
        foreach (var ui in uiInstances)
        {
            if (ui != null)
            {
                Destroy(ui.gameObject);
            }
        }
        uiInstances.Clear();

        // Create UI for each item
        if (itemUIPrefab == null || contentParent == null) return;

        foreach (var item in itemsForSale)
        {
            GameObject obj = Instantiate(itemUIPrefab, contentParent);
            
            // Reset local position to avoid Z offset issues
            RectTransform rt = obj.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.localPosition = new Vector3(rt.localPosition.x, rt.localPosition.y, 0f);
            }
            
            InventoryItemUI ui = obj.GetComponent<InventoryItemUI>();

            if (ui != null)
            {
                ui.Setup(item);
                uiInstances.Add(ui);
            }
        }

        UpdateSellUI();
    }

    void OnDestroy()
    {
        if (sellButton != null)
        {
            sellButton.onClick.RemoveListener(OnSellButtonClicked);
        }
    }
}

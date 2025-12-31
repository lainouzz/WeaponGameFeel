using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;

/// <summary>
/// UI component for displaying an inventory item. Right-click to transfer.
/// </summary>
public class InventoryItemUI : MonoBehaviour, IPointerClickHandler
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text priceText;

    /// <summary>
    /// Event fired when item is right-clicked
    /// </summary>
    public event Action<InventoryItemUI> OnRightClick;

    private InventoryItem inventoryItem;

    public InventoryItem Item => inventoryItem;

    public void Setup(InventoryItem item)
    {
        inventoryItem = item;
        Refresh();
    }

    public void Refresh()
    {
        if (inventoryItem == null || inventoryItem.data == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        if (iconImage != null)
        {
            iconImage.sprite = inventoryItem.data.icon;
            iconImage.enabled = inventoryItem.data.icon != null;
        }

        if (nameText != null)
        {
            nameText.text = inventoryItem.data.itemName;
        }

        if (priceText != null)
        {
            priceText.text = $"{inventoryItem.TotalSellValue}c";
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            OnRightClick?.Invoke(this);
        }
    }
}

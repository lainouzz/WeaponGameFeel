using System;

/// <summary>
/// Runtime representation of an item in inventory
/// </summary>
[Serializable]
public class InventoryItem
{
    public ItemData data;
    public int quantity;

    public InventoryItem(ItemData itemData, int amount = 1)
    {
        data = itemData;
        quantity = amount;
    }

    public int TotalSellValue => data != null ? data.sellPrice * quantity : 0;

    public InventoryItem Clone()
    {
        return new InventoryItem(data, quantity);
    }
}

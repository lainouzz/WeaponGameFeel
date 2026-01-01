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
    
    /// <summary>
    /// Maximum stack size for this item
    /// </summary>
    public int MaxQuantity => data != null ? data.quantityLimit : 1;
    
    /// <summary>
    /// Check if this stack is full
    /// </summary>
    public bool IsFull => quantity >= MaxQuantity;
    
    /// <summary>
    /// How many more can be added to this stack
    /// </summary>
    public int SpaceRemaining => MaxQuantity - quantity;

    /// <summary>
    /// Try to add quantity to this stack, respecting the limit
    /// </summary>
    /// <param name="amount">Amount to add</param>
    /// <returns>Amount that couldn't be added (overflow)</returns>
    public int AddQuantity(int amount)
    {
        int canAdd = Math.Min(amount, SpaceRemaining);
        quantity += canAdd;
        return amount - canAdd; // Return overflow
    }

    /// <summary>
    /// Remove quantity from this stack
    /// </summary>
    /// <param name="amount">Amount to remove</param>
    /// <returns>Amount actually removed</returns>
    public int RemoveQuantity(int amount)
    {
        int canRemove = Math.Min(amount, quantity);
        quantity -= canRemove;
        return canRemove;
    }

    public InventoryItem Clone()
    {
        return new InventoryItem(data, quantity);
    }
}

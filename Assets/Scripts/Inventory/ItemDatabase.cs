using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Database of all items in the game. Used for save/load to look up ItemData by ID.
/// </summary>
[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Inventory/Item Database")]
public class ItemDatabase : ScriptableObject
{
    [SerializeField] private List<ItemData> allItems = new List<ItemData>();

    private Dictionary<string, ItemData> itemLookup;

    /// <summary>
    /// Initialize the lookup dictionary
    /// </summary>
    public void Initialize()
    {
        itemLookup = new Dictionary<string, ItemData>();
        foreach (var item in allItems)
        {
            if (item != null && !string.IsNullOrEmpty(item.itemId))
            {
                if (!itemLookup.ContainsKey(item.itemId))
                {
                    itemLookup[item.itemId] = item;
                }
                else
                {
                    Debug.LogWarning($"[ItemDatabase] Duplicate item ID: {item.itemId}");
                }
            }
        }
        Debug.Log($"[ItemDatabase] Initialized with {itemLookup.Count} items.");
    }

    /// <summary>
    /// Get ItemData by its unique ID
    /// </summary>
    public ItemData GetItemById(string itemId)
    {
        if (itemLookup == null)
        {
            Initialize();
        }

        if (string.IsNullOrEmpty(itemId)) return null;

        itemLookup.TryGetValue(itemId, out ItemData item);
        return item;
    }

    /// <summary>
    /// Get all items in the database
    /// </summary>
    public IReadOnlyList<ItemData> AllItems => allItems;
}

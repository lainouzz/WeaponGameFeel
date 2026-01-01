using UnityEngine;

/// <summary>
/// ScriptableObject that defines the data for an inventory item
/// </summary>
[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item Data")]
public class ItemData : ScriptableObject
{
    [Tooltip("Unique ID for save/load. Must be unique per item!")]
    public string itemId;
    
    public string itemName = "New Item";
    public Sprite icon;
    public int sellPrice = 10;
    public int quantity;
    public int quantityLimit = 10;

    public ItemType itemType;
    public ItemRarity rarity;


    private void OnValidate()
    {
        // Auto-generate ID from asset name if empty
        if (string.IsNullOrEmpty(itemId))
        {
            itemId = name;
            //itemId = Random.Range(000001, 999999).ToString();
        }
    }
}


[System.Serializable]
public enum ItemType
{
    Weapon,
    Armor,
    Consumable,
    Material,
    Misc
}
[System.Serializable]
public enum ItemRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}

using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Manages the player's inventory and credits with save/load support
/// </summary>
public class PlayerInventoryManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject itemUIPrefab;
    [SerializeField] private TMP_Text creditsText;

    [Header("In-Game Inventory Toggle")]
    [SerializeField] private bool isInGameUI;
    [SerializeField] private bool startClosed = true;

    [Header("Starting Configuration")]
    [SerializeField] private int startingCredits = 1000;
    [SerializeField] private List<ItemData> startingItems = new List<ItemData>();

    [Header("Save/Load")]
    [SerializeField] private ItemDatabase itemDatabase;
    [SerializeField] private bool autoSave = true;
    [SerializeField] private string saveFileName = "inventory.json";

    /// <summary>
    /// Event fired when an item is right-clicked for transfer
    /// </summary>
    public event Action<InventoryItem> OnItemRightClicked;

    /// <summary>
    /// Event fired when credits change
    /// </summary>
    public event Action<int> OnCreditsChanged;

    /// <summary>
    /// Event fired when inventory is opened/closed
    /// </summary>
    public event Action<bool> OnInventoryToggled;

    // Stored items dictionary (persistent)
    private Dictionary<string, int> storedItems = new Dictionary<string, int>();

    // Active session items (for UI display)
    private List<InventoryItem> items = new List<InventoryItem>();
    private List<InventoryItemUI> uiInstances = new List<InventoryItemUI>();
    private int credits;
    private bool isOpen;

    public int Credits => credits;
    public bool IsOpen => isOpen;

    private string SavePath => Path.Combine(Application.persistentDataPath, saveFileName);

    void Awake()
    {
        if (itemDatabase != null)
        {
            itemDatabase.Initialize();
        }
    }

    void Start()
    {
        // Try to load saved data first
        if (!LoadInventory())
        {
            // No save found, use starting configuration
            credits = startingCredits;
            
            foreach (var itemData in startingItems)
            {
                if (itemData != null)
                {
                    AddItem(itemData);
                }
            }
        }

        UpdateCreditsUI();
        
        // Rebuild items list from stored dictionary
        RebuildItemsFromStorage();

        // Set initial state for in-game UI
        if (isInGameUI && startClosed)
        {
            CloseInventory();
        }
    }

    void OnApplicationQuit()
    {
        if (autoSave)
        {
            SaveInventory();
        }
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && autoSave)
        {
            SaveInventory();
        }
    }

    void Update()
    {
        if (isInGameUI && Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
        {
            ToggleInventory();
        }
    }

    #region Inventory Toggle

    public void ToggleInventory()
    {
        if (isOpen)
            CloseInventory();
        else
            OpenInventory();
    }

    public void OpenInventory()
    {
        isOpen = true;

        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(true);
        }

        RefreshUI();

        if (isInGameUI)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        OnInventoryToggled?.Invoke(true);
        Debug.Log("[PlayerInventory] Opened");
    }

    public void CloseInventory()
    {
        isOpen = false;

        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
        }

        if (isInGameUI)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        OnInventoryToggled?.Invoke(false);
        Debug.Log("[PlayerInventory] Closed");
    }

    #endregion

    #region Item Management

    public void AddItem(ItemData itemData, int quantity = 1)
    {
        if (itemData == null || string.IsNullOrEmpty(itemData.itemId)) return;

        // Add to stored dictionary
        if (storedItems.ContainsKey(itemData.itemId))
        {
            storedItems[itemData.itemId] += quantity;
        }
        else
        {
            storedItems[itemData.itemId] = quantity;
        }

        // Rebuild UI items list
        RebuildItemsFromStorage();

        if (isOpen || !isInGameUI)
        {
            RefreshUI();
        }

        if (autoSave)
        {
            SaveInventory();
        }

        Debug.Log($"[PlayerInventory] Added {quantity}x {itemData.itemName}");
    }

    public void RemoveItem(InventoryItem item)
    {
        if (item == null || item.data == null) return;
        RemoveItem(item.data, item.quantity);
    }

    public bool RemoveItem(ItemData itemData, int quantity = 1)
    {
        if (itemData == null || string.IsNullOrEmpty(itemData.itemId)) return false;

        if (!storedItems.ContainsKey(itemData.itemId)) return false;
        if (storedItems[itemData.itemId] < quantity) return false;

        storedItems[itemData.itemId] -= quantity;

        if (storedItems[itemData.itemId] <= 0)
        {
            storedItems.Remove(itemData.itemId);
        }

        RebuildItemsFromStorage();
        RefreshUI();

        if (autoSave)
        {
            SaveInventory();
        }

        Debug.Log($"[PlayerInventory] Removed {quantity}x {itemData.itemName}");
        return true;
    }

    private void RebuildItemsFromStorage()
    {
        items.Clear();

        if (itemDatabase == null) return;

        foreach (var kvp in storedItems)
        {
            ItemData itemData = itemDatabase.GetItemById(kvp.Key);
            if (itemData != null)
            {
                items.Add(new InventoryItem(itemData, kvp.Value));
            }
        }
    }

    #endregion

    #region Credits

    public void AddCredits(int amount)
    {
        credits += amount;
        UpdateCreditsUI();
        OnCreditsChanged?.Invoke(credits);

        if (autoSave)
        {
            SaveInventory();
        }

        Debug.Log($"[PlayerInventory] Added {amount} credits. Total: {credits}");
    }

    public bool SpendCredits(int amount)
    {
        if (credits < amount) return false;

        credits -= amount;
        UpdateCreditsUI();
        OnCreditsChanged?.Invoke(credits);

        if (autoSave)
        {
            SaveInventory();
        }

        Debug.Log($"[PlayerInventory] Spent {amount} credits. Remaining: {credits}");
        return true;
    }

    private void UpdateCreditsUI()
    {
        if (creditsText != null)
        {
            creditsText.text = $"{credits}";
        }
    }

    #endregion

    #region Save/Load

    [Serializable]
    private class InventorySaveData
    {
        public int credits;
        public List<ItemSaveData> items = new List<ItemSaveData>();
    }

    [Serializable]
    private class ItemSaveData
    {
        public string itemId;
        public int quantity;
    }

    public void SaveInventory()
    {
        try
        {
            InventorySaveData saveData = new InventorySaveData
            {
                credits = this.credits
            };

            foreach (var kvp in storedItems)
            {
                saveData.items.Add(new ItemSaveData
                {
                    itemId = kvp.Key,
                    quantity = kvp.Value
                });
            }

            string json = JsonUtility.ToJson(saveData, true);
            File.WriteAllText(SavePath, json);

            Debug.Log($"[PlayerInventory] Saved to {SavePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[PlayerInventory] Failed to save: {e.Message}");
        }
    }

    public bool LoadInventory()
    {
        try
        {
            if (!File.Exists(SavePath))
            {
                Debug.Log("[PlayerInventory] No save file found.");
                return false;
            }

            string json = File.ReadAllText(SavePath);
            InventorySaveData saveData = JsonUtility.FromJson<InventorySaveData>(json);

            credits = saveData.credits;
            storedItems.Clear();

            foreach (var itemSave in saveData.items)
            {
                if (!string.IsNullOrEmpty(itemSave.itemId))
                {
                    storedItems[itemSave.itemId] = itemSave.quantity;
                }
            }

            Debug.Log($"[PlayerInventory] Loaded {storedItems.Count} items, {credits} credits");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[PlayerInventory] Failed to load: {e.Message}");
            return false;
        }
    }

    [ContextMenu("Delete Save File")]
    public void DeleteSaveFile()
    {
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
            Debug.Log("[PlayerInventory] Save file deleted.");
        }
    }

    [ContextMenu("Force Save")]
    public void ForceSave()
    {
        SaveInventory();
    }

    #endregion

    #region UI

    public void RefreshUI()
    {
        foreach (var ui in uiInstances)
        {
            if (ui != null)
            {
                ui.OnRightClick -= HandleItemRightClick;
                Destroy(ui.gameObject);
            }
        }
        uiInstances.Clear();

        if (itemUIPrefab == null || contentParent == null) return;

        foreach (var item in items)
        {
            GameObject obj = Instantiate(itemUIPrefab, contentParent);

            RectTransform rt = obj.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.localPosition = new Vector3(rt.localPosition.x, rt.localPosition.y, 0f);
            }

            InventoryItemUI ui = obj.GetComponent<InventoryItemUI>();

            if (ui != null)
            {
                ui.Setup(item);
                ui.OnRightClick += HandleItemRightClick;
                uiInstances.Add(ui);
            }
        }
    }

    private void HandleItemRightClick(InventoryItemUI ui)
    {
        if (ui != null && ui.Item != null)
        {
            OnItemRightClicked?.Invoke(ui.Item);
        }
    }

    #endregion
}

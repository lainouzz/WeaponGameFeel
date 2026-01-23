using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Manages the player's inventory and credits with save/load support
/// </summary>
public class PlayerLoadSaveManager : MonoBehaviour
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
    [SerializeField] private bool loadOnStart = true;
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

    public static PlayerLoadSaveManager instance { get; private set; }

    // Active session items (for UI display)
    private List<InventoryItem> items = new List<InventoryItem>();
    private List<InventoryItemUI> uiinstances = new List<InventoryItemUI>();
    private int credits;
    private bool isOpen;
    private bool isInitialized;

    public int Credits => credits;
    public bool IsOpen => isOpen;

    private string SavePath => Path.Combine(Application.persistentDataPath, saveFileName);

    void Awake()
    {
        InitializeDatabase();

        // Singleton pattern (optional)
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }

    void Start()
    {
        if (loadOnStart)
        {
            InitializeInventory();
        }

        // Set initial state for in-game UI
        if (isInGameUI && startClosed)
        {
            CloseInventory();
        }
    }

    private void InitializeDatabase()
    {
        if (itemDatabase == null)
        {
            Debug.LogError("[PlayerInventory] ItemDatabase is not assigned! Save/Load will not work.");
            return;
        }
        
        itemDatabase.Initialize();
    }

    /// <summary>
    /// Initialize inventory - call this to load saved data
    /// </summary>
    public void InitializeInventory()
    {
        if (isInitialized) return;

        // Make sure database is initialized
        if (itemDatabase == null)
        {
            Debug.LogError("[PlayerInventory] Cannot initialize - ItemDatabase is null!");
            return;
        }

        // Try to load saved data first
        bool loaded = LoadInventory();
        
        if (!loaded)
        {
            // No save found, use starting configuration
            Debug.Log("[PlayerInventory] No save file, using starting configuration.");
            credits = startingCredits;
            storedItems.Clear();
            
            foreach (var itemData in startingItems)
            {
                if (itemData != null && !string.IsNullOrEmpty(itemData.itemId))
                {
                    if (storedItems.ContainsKey(itemData.itemId))
                    {
                        storedItems[itemData.itemId]++;
                    }
                    else
                    {
                        storedItems[itemData.itemId] = 1;
                    }
                }
            }
        }

        UpdateCreditsUI();
        RebuildItemsFromStorage();
        RefreshUI();
        
        isInitialized = true;
        Debug.Log($"[PlayerInventory] Initialized with {items.Count} items, {credits} credits");
    }



    /// <summary>
    /// Force reload from save file
    /// </summary>
    [ContextMenu("Force Reload")]
    public void ForceReload()
    {
        isInitialized = false;
        InitializeDatabase();
        InitializeInventory();
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
        if (!isInGameUI)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
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

        // Rebuild and refresh when opening
        RebuildItemsFromStorage();
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

    /// <summary>
    /// Add items to inventory, respecting quantity limits
    /// </summary>
    /// <param name="itemData">The item to add</param>
    /// <param name="quantity">Amount to add</param>
    /// <returns>Amount that couldn't be added (overflow)</returns>
    public int AddItem(ItemData itemData, int quantity = 1)
    {
        if (itemData == null)
        {
            Debug.LogError("[PlayerInventory] AddItem: itemData is null!");
            return quantity;
        }
        
        if (string.IsNullOrEmpty(itemData.itemId))
        {
            Debug.LogError($"[PlayerInventory] AddItem: itemId is empty for {itemData.itemName}!");
            return quantity;
        }

        int limit = itemData.quantityLimit > 0 ? itemData.quantityLimit : int.MaxValue;
        int currentAmount = storedItems.ContainsKey(itemData.itemId) ? storedItems[itemData.itemId] : 0;
        int spaceAvailable = limit - currentAmount;
        int toAdd = Math.Min(quantity, spaceAvailable);
        int overflow = quantity - toAdd;

        if (toAdd > 0)
        {
            if (storedItems.ContainsKey(itemData.itemId))
            {
                storedItems[itemData.itemId] += toAdd;
            }
            else
            {
                storedItems[itemData.itemId] = toAdd;
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

            Debug.Log($"[PlayerInventory] Added {toAdd}x {itemData.itemName} (Total: {storedItems[itemData.itemId]}/{limit})");
        }

        if (overflow > 0)
        {
            Debug.LogWarning($"[PlayerInventory] Couldn't add {overflow}x {itemData.itemName} - at limit ({limit})");
        }

        return overflow;
    }

    /// <summary>
    /// Check if we can add more of this item
    /// </summary>
    public bool CanAddItem(ItemData itemData, int quantity = 1)
    {
        if (itemData == null || string.IsNullOrEmpty(itemData.itemId)) return false;
        
        int limit = itemData.quantityLimit > 0 ? itemData.quantityLimit : int.MaxValue;
        int currentAmount = storedItems.ContainsKey(itemData.itemId) ? storedItems[itemData.itemId] : 0;
        
        return currentAmount + quantity <= limit;
    }

    /// <summary>
    /// Get current quantity of an item
    /// </summary>
    public int GetItemQuantity(ItemData itemData)
    {
        if (itemData == null || string.IsNullOrEmpty(itemData.itemId)) return 0;
        return storedItems.ContainsKey(itemData.itemId) ? storedItems[itemData.itemId] : 0;
    }

    /// <summary>
    /// Check if item is at max quantity
    /// </summary>
    public bool IsItemAtLimit(ItemData itemData)
    {
        if (itemData == null || string.IsNullOrEmpty(itemData.itemId)) return true;
        
        int limit = itemData.quantityLimit > 0 ? itemData.quantityLimit : int.MaxValue;
        int currentAmount = storedItems.ContainsKey(itemData.itemId) ? storedItems[itemData.itemId] : 0;
        
        return currentAmount >= limit;
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

        if (itemDatabase == null)
        {
            Debug.LogError("[PlayerInventory] RebuildItemsFromStorage: ItemDatabase is null!");
            return;
        }

        foreach (var kvp in storedItems)
        {
            ItemData itemData = itemDatabase.GetItemById(kvp.Key);
            if (itemData != null)
            {
                items.Add(new InventoryItem(itemData, kvp.Value));
            }
            else
            {
                Debug.LogWarning($"[PlayerInventory] Item ID '{kvp.Key}' not found in database!");
            }
        }
        
        Debug.Log($"[PlayerInventory] Rebuilt {items.Count} items from {storedItems.Count} stored entries");
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

            Debug.Log($"[PlayerInventory] Saved {storedItems.Count} items to {SavePath}");
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
                Debug.Log($"[PlayerInventory] No save file found at {SavePath}");
                return false;
            }

            string json = File.ReadAllText(SavePath);
            Debug.Log($"[PlayerInventory] Loading from file: {json}");
            
            InventorySaveData saveData = JsonUtility.FromJson<InventorySaveData>(json);

            if (saveData == null)
            {
                Debug.LogError("[PlayerInventory] Failed to parse save data!");
                return false;
            }

            credits = saveData.credits;
            storedItems.Clear();

            if (saveData.items != null)
            {
                foreach (var itemSave in saveData.items)
                {
                    if (!string.IsNullOrEmpty(itemSave.itemId))
                    {
                        storedItems[itemSave.itemId] = itemSave.quantity;
                        Debug.Log($"[PlayerInventory] Loaded item: {itemSave.itemId} x{itemSave.quantity}");
                    }
                }
            }

            Debug.Log($"[PlayerInventory] Loaded {storedItems.Count} items, {credits} credits from {SavePath}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[PlayerInventory] Failed to load: {e.Message}\n{e.StackTrace}");
            return false;
        }
    }

    public void ClearInventory()
    {
        storedItems.Clear();
        //credits = 0;
        RebuildItemsFromStorage();
        RefreshUI();
        UpdateCreditsUI();
        if (autoSave)
        {
            SaveInventory();
        }
        Debug.Log("[PlayerInventory] Inventory cleared.");
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
        //SaveStats();
    }

    [ContextMenu("Print Save Path")]
    public void PrintSavePath()
    {
        Debug.Log($"[PlayerInventory] Save path: {SavePath}");
        Debug.Log($"[PlayerInventory] File exists: {File.Exists(SavePath)}");
        Debug.Log($"[PlayerInventory] Save file content: {(File.Exists(SavePath) ? File.ReadAllText(SavePath) : "N/A")}");
    }

    [ContextMenu("Print Current State")]
    public void PrintCurrentState()
    {
        Debug.Log($"[PlayerInventory] Credits: {credits}");
        Debug.Log($"[PlayerInventory] Stored items count: {storedItems.Count}");
        foreach (var kvp in storedItems)
        {
            Debug.Log($"  - {kvp.Key}: {kvp.Value}");
        }
        Debug.Log($"[PlayerInventory] Display items count: {items.Count}");
    }

    #endregion

    #region UI

    public void RefreshUI()
    {
        foreach (var ui in uiinstances)
        {
            if (ui != null)
            {
                ui.OnRightClick -= HandleItemRightClick;
                Destroy(ui.gameObject);
            }
        }
        uiinstances.Clear();

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
                uiinstances.Add(ui);
            }
        }
        
        Debug.Log($"[PlayerInventory] RefreshUI: Created {uiinstances.Count} UI elements");
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

using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class ItemPickupBehavior : MonoBehaviour
{
    public LayerMask itemLayerMask;
    public TMP_Text pickupText;
    public float raycastInterval = 0.1f;
    public float pickupRange = 20f;

    private GameInput gameInput;
    private PlayerInventoryManager playerInventoryManager;
    private float raycastTimer;
    private ItemPickup currentItem;

    void Awake()
    {
        gameInput = new GameInput();
        playerInventoryManager = FindAnyObjectByType<PlayerInventoryManager>();
    }

    void OnEnable()
    {
        gameInput.Enable();
    }

    void Start()
    {
        if (pickupText != null)
        {
            pickupText.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        // Timer-based raycast to reduce performance cost
        raycastTimer -= Time.deltaTime;
        if (raycastTimer <= 0f)
        {
            raycastTimer = raycastInterval;
            ItemDetect();
        }

        // Check for pickup input (must be in Update for wasPressedThisFrame)
        if (currentItem != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            PickupItem();
        }
    }

    private void ItemDetect()
    {
        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, pickupRange, itemLayerMask))
        {
            ItemPickup pickup = hit.collider.GetComponent<ItemPickup>();
            if (pickup != null && pickup.itemData != null)
            {
                currentItem = pickup;
                if (pickupText != null)
                {
                    pickupText.gameObject.SetActive(true);
                    
                    // Show quantity info if at limit
                    if (playerInventoryManager != null && playerInventoryManager.IsItemAtLimit(pickup.itemData))
                    {
                        pickupText.text = $"{pickup.itemData.itemName} (FULL)";
                    }
                    else
                    {
                        int current = playerInventoryManager != null ? playerInventoryManager.GetItemQuantity(pickup.itemData) : 0;
                        int limit = pickup.itemData.quantityLimit;
                        pickupText.text = $"Press E to pick up {pickup.itemData.itemName} ({current}/{limit})";
                    }
                }
                return;
            }
        }

        // Nothing found - clear current item
        currentItem = null;
        if (pickupText != null)
        {
            pickupText.gameObject.SetActive(false);
        }
    }

    private void PickupItem()
    {
        if (currentItem == null || currentItem.itemData == null) return;
        if (playerInventoryManager == null) return;

        // Check if we can pick up
        if (playerInventoryManager.IsItemAtLimit(currentItem.itemData))
        {
            Debug.Log($"Cannot pick up {currentItem.itemData.itemName} - inventory full for this item!");
            return;
        }

        int overflow = playerInventoryManager.AddItem(currentItem.itemData);
        
        if (overflow == 0)
        {
            // Fully picked up
            Debug.Log($"Picked up {currentItem.itemData.itemName}");
            Destroy(currentItem.gameObject);
            currentItem = null;
        }
        else
        {
            // Partial pickup (shouldn't happen with quantity=1, but handles future cases)
            Debug.Log($"Partially picked up {currentItem.itemData.itemName}, {overflow} remaining");
        }

        if (pickupText != null)
        {
            pickupText.gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        gameInput.Disable();
    }
}

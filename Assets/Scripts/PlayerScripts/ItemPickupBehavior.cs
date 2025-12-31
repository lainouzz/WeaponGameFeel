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
                    pickupText.text = $"Press E to pick up {pickup.itemData.itemName}";
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

        playerInventoryManager.AddItem(currentItem.itemData);
        playerInventoryManager.RefreshUI();
        Debug.Log($"Picked up {currentItem.itemData.itemName}");

        Destroy(currentItem.gameObject);
        currentItem = null;

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

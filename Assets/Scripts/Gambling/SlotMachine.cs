using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Simple slot machine that players can gamble credits on.
/// Attach to a cube or any object with a collider (set as trigger).
/// </summary>
public class SlotMachine : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int minBet = 10;
    [SerializeField] private int maxBet = 100;
    [SerializeField] private int betIncrement = 10;
    [SerializeField] private float spinDuration = 2f;
    [SerializeField] private float interactRange = 3f;
    [SerializeField] private bool useDistanceCheck = true; // Fallback if trigger doesn't work

    [Header("Odds (0-100)")]
    [Tooltip("Chance to win 2x bet")]
    [SerializeField] private float chanceWin2x = 30f;
    [Tooltip("Chance to win 3x bet")]
    [SerializeField] private float chanceWin3x = 15f;
    [Tooltip("Chance to win 5x bet (jackpot)")]
    [SerializeField] private float chanceWin5x = 5f;

    [Header("Symbols (for display)")]
    [SerializeField] private string[] symbols = { "A", "B", "C", "D", "7" };

    [Header("UI References")]
    [SerializeField] private GameObject slotUI;
    [SerializeField] private TMP_Text slot1Text;
    [SerializeField] private TMP_Text slot2Text;
    [SerializeField] private TMP_Text slot3Text;
    [SerializeField] private TMP_Text betText;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private TMP_Text playerCreditsText;
    [SerializeField] private TMP_Text interactPromptText;

    [Header("Audio (Optional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip spinSound;
    [SerializeField] private AudioClip winSound;
    [SerializeField] private AudioClip loseSound;
    [SerializeField] private AudioClip jackpotSound;

    [Header("Debug")]
    [SerializeField] private bool debugMode = true;

    public event Action<int> OnWin;
    public event Action<int> OnLose;

    private int currentBet;
    private bool isSpinning;
    private bool isPlayerNear;
    private bool isUIOpen;
    private PlayerInventoryManager playerInventory;
    private PlayerMovement playerMovement;
    private GameInput gameInput;
    private Transform playerTransform;

    void Awake()
    {
        gameInput = new GameInput();
        currentBet = minBet;
    }

    void OnEnable()
    {
        gameInput.Enable();
    }

    void Start()
    {
        playerInventory = PlayerInventoryManager.instance;
        
        // Find player transform and movement
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            playerMovement = player.GetComponent<PlayerMovement>();
            if (playerMovement == null)
            {
                playerMovement = player.GetComponentInChildren<PlayerMovement>();
            }
            if (debugMode) Debug.Log($"[SlotMachine] Found player with 'Player' tag. Movement: {(playerMovement != null ? "Found" : "NULL")}");
        }
        else
        {
            // Try to find by name or other means
            var cam = Camera.main;
            if (cam != null)
            {
                playerTransform = cam.transform;
                playerMovement = FindObjectOfType<PlayerMovement>();
                if (debugMode) Debug.Log("[SlotMachine] Using main camera as player reference (no 'Player' tag found)");
            }
        }

        if (slotUI != null)
        {
            slotUI.SetActive(false);
        }

        if (interactPromptText != null)
        {
            interactPromptText.gameObject.SetActive(false);
        }

        UpdateUI();
        
        if (debugMode) Debug.Log($"[SlotMachine] Initialized. PlayerInventory: {(playerInventory != null ? "Found" : "NULL")}");
    }

    void Update()
    {
        // Distance-based player detection (fallback)
        if (useDistanceCheck && playerTransform != null)
        {
            float distance = Vector3.Distance(transform.position, playerTransform.position);
            bool wasNear = isPlayerNear;
            isPlayerNear = distance <= interactRange;

            // Show/hide prompt based on distance
            if (isPlayerNear && !wasNear)
            {
                if (interactPromptText != null)
                {
                    interactPromptText.gameObject.SetActive(true);
                    interactPromptText.text = "Press E to use Slot Machine";
                }
                if (debugMode) Debug.Log($"[SlotMachine] Player entered range (distance: {distance:F2})");
            }
            else if (!isPlayerNear && wasNear)
            {
                if (interactPromptText != null)
                {
                    interactPromptText.gameObject.SetActive(false);
                }
                if (isUIOpen) CloseSlotMachine();
                if (debugMode) Debug.Log($"[SlotMachine] Player left range (distance: {distance:F2})");
            }
        }

        // Check for interaction
        if (isPlayerNear && !isUIOpen && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (debugMode) Debug.Log("[SlotMachine] E pressed, opening...");
            OpenSlotMachine();
        }

        // Close UI
        if (isUIOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (debugMode) Debug.Log("[SlotMachine] Escape pressed, closing...");
            CloseSlotMachine();
        }

        // Spin
        if (isUIOpen && !isSpinning && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (debugMode) Debug.Log("[SlotMachine] Space pressed, trying to spin...");
            TrySpin();
        }

        // Adjust bet
        if (isUIOpen && !isSpinning)
        {
            if (Keyboard.current != null && Keyboard.current.upArrowKey.wasPressedThisFrame)
            {
                IncreaseBet();
            }
            if (Keyboard.current != null && Keyboard.current.downArrowKey.wasPressedThisFrame)
            {
                DecreaseBet();
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (debugMode) Debug.Log($"[SlotMachine] OnTriggerEnter: {other.name} (Tag: {other.tag})");

        if (other.CompareTag("Player"))
        {
            isPlayerNear = true;
            slotUI.SetActive(true);
            playerTransform = other.transform;
            if (interactPromptText != null)
            {
                interactPromptText.gameObject.SetActive(true);
                interactPromptText.text = "Press E to use Slot Machine";
            }
            if (debugMode) Debug.Log("[SlotMachine] Player entered trigger zone");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (debugMode) Debug.Log($"[SlotMachine] OnTriggerExit: {other.name} (Tag: {other.tag})");

        if (other.CompareTag("Player"))
        {
            isPlayerNear = false;
            if (interactPromptText != null)
            {
                interactPromptText.gameObject.SetActive(false);
            }
            CloseSlotMachine();
            if (debugMode) Debug.Log("[SlotMachine] Player left trigger zone");
        }
    }

    public void OpenSlotMachine()
    {
        if (playerInventory == null)
        {
            playerInventory = PlayerInventoryManager.instance;
            if (debugMode) Debug.Log($"[SlotMachine] Re-fetching PlayerInventory: {(playerInventory != null ? "Found" : "Still NULL")}");
        }

        isUIOpen = true;

        if (slotUI != null)
        {
            slotUI.SetActive(true);
        }

        // Disable player movement
        if (playerMovement != null)
        {
            playerMovement.canMove = false;
        }

        // Show cursor
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        UpdateUI();
        Debug.Log("[SlotMachine] Opened - Movement disabled");
    }

    public void CloseSlotMachine()
    {
        if (!isUIOpen) return;
        
        isUIOpen = false;

        if (slotUI != null)
        {
            slotUI.SetActive(false);
        }

        // Re-enable player movement
        if (playerMovement != null)
        {
            playerMovement.canMove = true;
        }

        // Hide cursor
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        Debug.Log("[SlotMachine] Closed - Movement enabled");
    }

    public void IncreaseBet()
    {
        currentBet = Mathf.Min(currentBet + betIncrement, maxBet);
        UpdateUI();
        Debug.Log($"[SlotMachine] Bet increased to {currentBet}c");
    }

    public void DecreaseBet()
    {
        currentBet = Mathf.Max(currentBet - betIncrement, minBet);
        UpdateUI();
        Debug.Log($"[SlotMachine] Bet decreased to {currentBet}c");
    }

    public void TrySpin()
    {
        if (isSpinning)
        {
            Debug.Log("[SlotMachine] Already spinning, please wait...");
            return;
        }

        if (playerInventory == null)
        {
            Debug.LogError("[SlotMachine] No PlayerInventoryManager found!");
            if (resultText != null) resultText.text = "No inventory found!";
            return;
        }

        if (playerInventory.Credits < currentBet)
        {
            Debug.Log($"[SlotMachine] Not enough credits! Have: {playerInventory.Credits}c, Need: {currentBet}c");
            if (resultText != null) resultText.text = "Not enough credits!";
            return;
        }

        Debug.Log($"[SlotMachine] Spinning with bet: {currentBet}c (Credits before: {playerInventory.Credits}c)");

        // Take bet
        playerInventory.SpendCredits(currentBet);
        StartCoroutine(SpinRoutine());
    }

    private IEnumerator SpinRoutine()
    {
        isSpinning = true;
        if (resultText != null) resultText.text = "Spinning...";
        Debug.Log("[SlotMachine] Spin started...");

        // Play spin sound
        if (audioSource != null && spinSound != null)
        {
            audioSource.PlayOneShot(spinSound);
        }

        // Animate slots
        float elapsed = 0f;
        while (elapsed < spinDuration)
        {
            if (slot1Text != null) slot1Text.text = symbols[UnityEngine.Random.Range(0, symbols.Length)];
            if (slot2Text != null) slot2Text.text = symbols[UnityEngine.Random.Range(0, symbols.Length)];
            if (slot3Text != null) slot3Text.text = symbols[UnityEngine.Random.Range(0, symbols.Length)];

            elapsed += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }

        // Determine result
        float roll = UnityEngine.Random.Range(0f, 100f);
        int winnings = 0;
        string resultSymbol;

        Debug.Log($"[SlotMachine] Roll: {roll:F2}% (5x: <{chanceWin5x}%, 3x: <{chanceWin5x + chanceWin3x}%, 2x: <{chanceWin5x + chanceWin3x + chanceWin2x}%)");

        if (roll < chanceWin5x)
        {
            // Jackpot - 5x
            winnings = currentBet * 5;
            resultSymbol = symbols[symbols.Length - 1];
            if (slot1Text != null) slot1Text.text = resultSymbol;
            if (slot2Text != null) slot2Text.text = resultSymbol;
            if (slot3Text != null) slot3Text.text = resultSymbol;
            if (resultText != null) resultText.text = $"JACKPOT! +{winnings}c";

            Debug.Log($"[SlotMachine] JACKPOT! Won {winnings}c (5x multiplier)");

            if (audioSource != null && jackpotSound != null)
            {
                audioSource.PlayOneShot(jackpotSound);
            }
        }
        else if (roll < chanceWin5x + chanceWin3x)
        {
            // Big win - 3x
            winnings = currentBet * 3;
            resultSymbol = symbols[UnityEngine.Random.Range(2, symbols.Length)];
            if (slot1Text != null) slot1Text.text = resultSymbol;
            if (slot2Text != null) slot2Text.text = resultSymbol;
            if (slot3Text != null) slot3Text.text = resultSymbol;
            if (resultText != null) resultText.text = $"BIG WIN! +{winnings}c";

            Debug.Log($"[SlotMachine] BIG WIN! Won {winnings}c (3x multiplier)");

            if (audioSource != null && winSound != null)
            {
                audioSource.PlayOneShot(winSound);
            }
        }
        else if (roll < chanceWin5x + chanceWin3x + chanceWin2x)
        {
            // Small win - 2x
            winnings = currentBet * 2;
            resultSymbol = symbols[UnityEngine.Random.Range(0, 3)];
            if (slot1Text != null) slot1Text.text = resultSymbol;
            if (slot2Text != null) slot2Text.text = resultSymbol;
            if (slot3Text != null) slot3Text.text = symbols[UnityEngine.Random.Range(0, symbols.Length)];
            if (resultText != null) resultText.text = $"WIN! +{winnings}c";

            Debug.Log($"[SlotMachine] WIN! Won {winnings}c (2x multiplier)");

            if (audioSource != null && winSound != null)
            {
                audioSource.PlayOneShot(winSound);
            }
        }
        else
        {
            // Lose
            int idx1 = UnityEngine.Random.Range(0, symbols.Length);
            int idx2 = (idx1 + 1) % symbols.Length;
            int idx3 = (idx1 + 2) % symbols.Length;
            if (slot1Text != null) slot1Text.text = symbols[idx1];
            if (slot2Text != null) slot2Text.text = symbols[idx2];
            if (slot3Text != null) slot3Text.text = symbols[idx3];
            if (resultText != null) resultText.text = $"No luck! -{currentBet}c";

            Debug.Log($"[SlotMachine] LOST! Lost {currentBet}c");

            if (audioSource != null && loseSound != null)
            {
                audioSource.PlayOneShot(loseSound);
            }

            OnLose?.Invoke(currentBet);
        }

        // Award winnings
        if (winnings > 0)
        {
            playerInventory.AddCredits(winnings);
            OnWin?.Invoke(winnings);
        }

        Debug.Log($"[SlotMachine] Spin complete. Credits now: {playerInventory.Credits}c");

        UpdateUI();
        isSpinning = false;
    }

    private void UpdateUI()
    {
        if (betText != null)
        {
            betText.text = $"Bet: {currentBet}c";
        }

        if (playerCreditsText != null && playerInventory != null)
        {
            playerCreditsText.text = $"Credits: {playerInventory.Credits}c";
        }

        if (debugMode && playerInventory == null)
        {
            Debug.LogWarning("[SlotMachine] UpdateUI: PlayerInventory is null!");
        }
    }

    void OnDisable()
    {
        gameInput.Disable();
    }

    void OnDrawGizmosSelected()
    {
        // Draw interact range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}

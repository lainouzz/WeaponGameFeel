using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

public class Debugging : MonoBehaviour
{
    public bool debugMode;
    public bool isPlayerDead;

    [Header("Debug Credit Settings")]
    [SerializeField] private int debugCreditAmount = 1000;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // Toggle debug mode with X
        if (Keyboard.current != null && Keyboard.current.xKey.wasPressedThisFrame)
        {
            debugMode = !debugMode;
            Debug.Log($"[Debug] Debug mode: {(debugMode ? "ON" : "OFF")}");
        }

        // Only allow debug commands when debug mode is on
        if (!debugMode) return;

        // K = Kill player (clear inventory)
        if (Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame)
        {
            isPlayerDead = true;
            DebugClearInvOnDead();
        }

        // G = Give credits
        if (Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame)
        {
            DebugGiveCredits();
        }
        if(Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            StatsManager.Instance.ResetUpgrades();
        }

        // H = Heal player to full
        if (Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame)
        {
            DebugHealPlayer();
        }

        // J = Damage player (10 damage)
        if (Keyboard.current != null && Keyboard.current.jKey.wasPressedThisFrame)
        {
            DebugDamagePlayer(10f);
        }
    }

    public void DebugClearInvOnDead()
    {
        if (debugMode && isPlayerDead)
        {
            Debug.Log("[Debug] Player is dead, clearing inventory.");
            if (PlayerLoadSaveManager.instance != null)
            {
                PlayerLoadSaveManager.instance.ClearInventory();
            }
        }
    }

    public void DebugGiveCredits()
    {
        if (PlayerLoadSaveManager.instance != null)
        {
            PlayerLoadSaveManager.instance.AddCredits(debugCreditAmount);
            Debug.Log($"[Debug] Added {debugCreditAmount} credits. Total: {PlayerLoadSaveManager.instance.Credits}");
        }
        else
        {
            Debug.LogWarning("[Debug] PlayerLoadSaveManager not found!");
        }
    }

    public void DebugHealPlayer()
    {
        if (StatsManager.Instance != null && StatsManager.Instance.Health != null)
        {
            StatsManager.Instance.Health.SetToMax();
            Debug.Log($"[Debug] Healed player to full. Health: {StatsManager.Instance.Health.CurrentValue}/{StatsManager.Instance.Health.MaxValue}");
        }
        else
        {
            Debug.LogWarning("[Debug] StatsManager not found!");
        }
    }

    public void DebugDamagePlayer(float damage)
    {
        if (StatsManager.Instance != null)
        {
            StatsManager.Instance.TakeDamage(damage);
            Debug.Log($"[Debug] Damaged player for {damage}. Health: {StatsManager.Instance.Health.CurrentValue}/{StatsManager.Instance.Health.MaxValue}");
        }
        else
        {
            Debug.LogWarning("[Debug] StatsManager not found!");
        }
    }
}

using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

public class Debugging : MonoBehaviour
{
    public bool debugMode;
    public bool isPlayerDead;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Keyboard.current != null && Keyboard.current.xKey.wasPressedThisFrame)
        {
            debugMode = true;
        }

        if(Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame)
        {
            isPlayerDead = true;
            DebugClearInvOnDead();
        }
        
    }

    public void DebugClearInvOnDead()
    {
       if (debugMode && isPlayerDead) {
            Debug.Log("Debug: Player is dead, clearing inventory.");
            PlayerInventoryManager.instance.ClearInventory();

       }
    }
}

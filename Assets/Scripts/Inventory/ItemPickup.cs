using UnityEngine;

/// <summary>
/// Attach to a world object to make it a pickupable item
/// </summary>
public class ItemPickup : MonoBehaviour
{
    public ItemData itemData;

    private EnemyBehavior enemyBehavior;

    void Awake()
    {
        enemyBehavior = FindAnyObjectByType<EnemyBehavior>(); // change later on, this might cause performance issues later on
        InitializeOnStart();
    }

    public void InitializeOnStart()
    {
        itemData = enemyBehavior.lootTable[Random.Range(0, enemyBehavior.lootTable.Count)];
    }
}

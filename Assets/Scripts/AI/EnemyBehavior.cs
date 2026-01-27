using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBehavior : MonoBehaviour, IDamagable
{
    [SerializeField]private float currentHealth;
    [SerializeField]private float maxHealth;

    [SerializeField] private float attackRange;
    [SerializeField] private float attackDamage;

    [SerializeField] public List<ItemData> lootTable;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    private void OnEnable()
    {

    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        
    }

    // Update is called once per frame
    private void Update()
    {
        
    }

    private void HandleMovement()
    {

    }

    private void HandleAttack()
    {

    }
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            HandleDeath();
        }
    }

    private void HandleDeath()
    {
        Spawnbehavior.Instance.OnEnemyDeath();
        SpawnDropLoot();
        Destroy(gameObject);
    }

    private void SpawnDropLoot()
    {
        // Implement loot spawning logic here
        for (int i = 0; i < lootTable.Count; i++)
        {
            float dropChance = Random.Range(0f, 1f);
            if (dropChance <= lootTable[i].dropProbability)
            {
                Instantiate(lootTable[i].itemPrefab, transform.position, Quaternion.identity);
            }
        }

    }

    private void OnDestroy() { }

}

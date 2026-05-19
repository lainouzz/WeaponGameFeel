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

    private bool isDead = false;

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
        if (isDead) return;

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            isDead = true;
            HandleDeath();
        }
    }

    private void HandleDeath()
    {
        if (Spawnbehavior.Instance != null)
            Spawnbehavior.Instance.OnEnemyDeath();

        if (WaveManager.Instance != null)
            WaveManager.Instance.OnEnemyKilled();

        Destroy(gameObject);
        WaveUI.Instance?.UpdateEnemiesLeft(WaveManager.Instance.EnemiesRemaining);
    }

    private void OnDestroy() { }

}
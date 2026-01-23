using UnityEngine;

public class EnemyBehavior : MonoBehaviour, IDamagable
{
    [SerializeField]private float currentHealth;
    [SerializeField]private float maxHealth;


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
        Destroy(gameObject);
    }

    private void OnDestroy() { }

}

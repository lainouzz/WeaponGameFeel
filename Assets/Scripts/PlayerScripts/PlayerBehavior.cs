using System;
using UnityEngine;

public class PlayerBehavior : MonoBehaviour
{
    public static PlayerBehavior Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    private void OnEnable()
    {

    }

    public void TakeDamage(float damage)
    {
        if (StatsManager.Instance != null && StatsManager.Instance.Health != null)
        {
            StatsManager.Instance.Health.TakeDamage(damage);
            Debug.Log($"[PlayerBehavior] Player took {damage} damage. Current Health: {StatsManager.Instance.Health.CurrentValue}/{StatsManager.Instance.Health.MaxValue}");
        }
    }

    public void OnDeath()
    {
        // Disable input
        // Play animation
        // Trigger extraction failure
        // Destroy or deactivate player
    }

    private void OnDisable()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}

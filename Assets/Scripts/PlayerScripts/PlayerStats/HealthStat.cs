using System;
using UnityEngine;

[SerializeField]
public class HealthStat : BaseStat
{
    [SerializeField] private float regenRate;
    [SerializeField] private float regenDelay;

    private float timeSinceLastDamage;

    public event Action OnDeath;

    public HealthStat(float baseValue, float maxValue) : base(baseValue, maxValue)
    {
        OnDepleted += () => OnDeath?.Invoke();
    }

    public void TakeDamage(float damage)
    {
        timeSinceLastDamage += Time.time;
        Remove(damage);
    }

    public void Heal(float amount)
    {
        Add(amount);
    }

    public override void Regenerate(float deltaTime)
    {
        if (regenRate <= 0) return;
        if (Time.time < timeSinceLastDamage + regenDelay) return;
        if (currentValue >= maxValue) return;

        Add(regenRate * deltaTime);
    }
}

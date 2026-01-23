using System;
using UnityEngine;

[SerializeField]
public class StaminaStat : BaseStat
{
    [SerializeField] private float regenRate;
    [SerializeField] private float regenDelay;

    private float timeSinceLastDamage;

    public event Action OnDeath;

    public StaminaStat(float baseValue, float maxValue) : base(baseValue, maxValue)
    {
        OnDepleted += () => OnDeath?.Invoke();
    }

    public void Consume(float amount)
    {
        timeSinceLastDamage = Time.time;
        Remove(amount);
    }

    public override void Regenerate(float deltaTime)
    {
        if (regenRate <= 0) return;
        if (Time.time < timeSinceLastDamage + regenDelay) return;
        if (currentValue >= maxValue) return;

        Add(regenRate * deltaTime);
    }
}

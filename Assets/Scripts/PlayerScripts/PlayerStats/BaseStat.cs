using UnityEngine;
using System;

[Serializable]
public abstract class BaseStat
{
    [SerializeField] protected float baseValue;
    [SerializeField] protected float maxValue;

    protected float currentValue;
    public float BaseValue => baseValue;
    public float MaxValue => maxValue;
    public float CurrentValue => currentValue;
    public float Percentage => maxValue > 0 ? currentValue / maxValue : 0f;

    public event Action<float, float> OnValueChanged;
    public event Action OnDepleted;
    public event Action OnFilled;

    public BaseStat(float baseValue, float maxValue)
    {
        this.baseValue = baseValue;
        this.maxValue = maxValue;
        this.currentValue = maxValue;
    }

    public virtual void Add(float amount)
    {
        float oldValue = currentValue;
        currentValue = Mathf.Clamp(Mathf.RoundToInt(currentValue + amount), 0, Mathf.RoundToInt(maxValue));
        if (currentValue != oldValue)
        {
            OnValueChanged?.Invoke(currentValue, maxValue);
        }

        if (currentValue >= maxValue && oldValue < maxValue)
        {
            OnFilled?.Invoke();
        }
    }

    public virtual void Remove(float amount)
    {
        float oldValue = currentValue;
        currentValue = Mathf.Clamp(Mathf.RoundToInt(currentValue - amount), 0, Mathf.RoundToInt(maxValue));
        if (currentValue != oldValue)
        {
            OnValueChanged?.Invoke(currentValue, maxValue);
        }
        if(currentValue <= 0 && oldValue > 0)
        {
            OnDepleted?.Invoke();
        }
    }

    public virtual void UpgradeMax(float amount)
    {
        maxValue += amount;
        currentValue += amount;
        OnValueChanged?.Invoke(currentValue, maxValue);
    }

    public virtual void SetToMax()
    {
        currentValue = maxValue;
        OnValueChanged?.Invoke(currentValue, maxValue);
    }

    public virtual void SetMax(float newMax, bool preserveCurrent = true)
    {
        float oldMax = maxValue;
        maxValue = Mathf.Max(0f, newMax);  // Avoid negative max

        float oldCurrent = currentValue;
        if (preserveCurrent)
        {
            currentValue = Mathf.Clamp(currentValue, 0f, maxValue);
        }
        else
        {
            currentValue = maxValue; 
        }

        if (currentValue != oldCurrent || maxValue != oldMax)
        {
            OnValueChanged?.Invoke(currentValue, maxValue);
        }

        // Trigger filled/depleted if crossed thresholds
        if (currentValue >= maxValue && oldCurrent < oldMax)
        {
            OnFilled?.Invoke();
        }
        if (currentValue <= 0 && oldCurrent > 0)
        {
            OnDepleted?.Invoke();
        }
    }

    public virtual void SetCurrent(float value)
    {
        float oldValue = currentValue;
        currentValue = Mathf.Clamp(Mathf.RoundToInt(value), 0, Mathf.RoundToInt(maxValue));
        if (currentValue != oldValue)
        {
            OnValueChanged?.Invoke(currentValue, maxValue);
        }
        if (currentValue >= maxValue && oldValue < maxValue)
        {
            OnFilled?.Invoke();
        }
        if (currentValue <= 0 && oldValue > 0)
        {
            OnDepleted?.Invoke();
        }
    }

    public virtual void SetToZero()
    {
        currentValue = 0;
        OnValueChanged?.Invoke(currentValue, maxValue);
        OnDepleted?.Invoke();
    }

    public abstract void Regenerate(float deltaTime);
}

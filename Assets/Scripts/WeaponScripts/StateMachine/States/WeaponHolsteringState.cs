using UnityEngine;

/// <summary>
/// Holstering state - weapon is being put away
/// </summary>
public class WeaponHolsteringState : WeaponStateBase
{
    public override WeaponStateType StateType => WeaponStateType.Holstering;

    private float holsterEndTime;

    /// <summary>
    /// Event fired when holster is complete (for weapon manager to handle)
    /// </summary>
    public event System.Action OnHolsterComplete;

    public WeaponHolsteringState(WeaponStateMachine stateMachine, Weapon weapon) 
        : base(stateMachine, weapon) { }

    public override void Enter()
    {
        holsterEndTime = Time.time + weapon.HolsterTime;
        weapon.OnHolsterStart();
        Debug.Log($"[WeaponState] Holstering weapon ({weapon.HolsterTime}s)");
    }

    public override void Update()
    {
        if (Time.time >= holsterEndTime)
        {
            weapon.OnHolsterFinish();
            OnHolsterComplete?.Invoke();
            // Note: The weapon will likely be disabled after this
            // so we don't transition to another state
        }
    }

    public override void Exit()
    {
        // Cleanup if needed
    }

    public override bool CanTransitionTo(WeaponStateType newState)
    {
        // Once holstering, generally can't cancel
        // But allow drawing if player changes mind quickly
        switch (newState)
        {
            case WeaponStateType.Drawing:
                return true; // Cancel holster to draw again
            default:
                return false;
        }
    }

    // Get holster progress (0 to 1)
    public float GetHolsterProgress()
    {
        float elapsed = Time.time - (holsterEndTime - weapon.HolsterTime);
        return Mathf.Clamp01(elapsed / weapon.HolsterTime);
    }

    // Check if holster is complete
    public bool IsComplete => Time.time >= holsterEndTime;
}

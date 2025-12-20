using UnityEngine;

/// <summary>
/// Switching state - weapon is being switched to/from
/// Handles equip/holster animations and timing
/// </summary>
public class WeaponSwitchingState : WeaponStateBase
{
    public override WeaponStateType StateType => WeaponStateType.Switching;

    private float switchEndTime;
    private bool isSwitchingIn; // true = drawing weapon, false = holstering

    public WeaponSwitchingState(WeaponStateMachine stateMachine, Weapon weapon) 
        : base(stateMachine, weapon) { }

    /// <summary>
    /// Call this before transitioning to set up the switch direction
    /// </summary>
    public void SetSwitchDirection(bool switchingIn)
    {
        isSwitchingIn = switchingIn;
    }

    public override void Enter()
    {
        float duration = isSwitchingIn ? weapon.DrawTime : weapon.HolsterTime;
        switchEndTime = Time.time + duration;

        if (isSwitchingIn)
        {
            weapon.OnDrawStart();
            Debug.Log($"[WeaponState] Drawing weapon ({duration}s)");
        }
        else
        {
            weapon.OnHolsterStart();
            Debug.Log($"[WeaponState] Holstering weapon ({duration}s)");
        }
    }

    public override void Update()
    {
        if (Time.time >= switchEndTime)
        {
            if (isSwitchingIn)
            {
                weapon.OnDrawFinish();
                RequestStateChange(WeaponStateType.Idle);
            }
            else
            {
                weapon.OnHolsterFinish();
                // Weapon will be disabled by WeaponManager, no state change needed
            }
        }
    }

    public override void Exit()
    {
        // Cleanup if needed
    }

    public override bool CanTransitionTo(WeaponStateType newState)
    {
        // During switching, only allow transition to idle (when complete)
        // or forced transitions
        switch (newState)
        {
            case WeaponStateType.Idle:
                return isSwitchingIn && Time.time >= switchEndTime;
            default:
                return false;
        }
    }

    // Get switch progress (0 to 1)
    public float GetSwitchProgress()
    {
        float duration = isSwitchingIn ? weapon.DrawTime : weapon.HolsterTime;
        float elapsed = Time.time - (switchEndTime - duration);
        return Mathf.Clamp01(elapsed / duration);
    }
}

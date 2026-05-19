using UnityEngine;

/// <summary>
/// Switching state - weapon is being equipped/drawn
/// </summary>
public class WeaponSwitchingState : WeaponStateBase
{
    public override WeaponStateType StateType => WeaponStateType.Switching;

    private float switchEndTime;

    public WeaponSwitchingState(WeaponStateMachine stateMachine, Weapon weapon) 
        : base(stateMachine, weapon) { }

    public override void Enter()
    {
        float duration = weapon.DrawTime;
        switchEndTime = Time.time + duration;
        weapon.OnDrawStart();
        Debug.Log($"[WeaponState] Drawing weapon ({duration}s)");
    }

    public override void Update()
    {
        if (Time.time >= switchEndTime)
        {
            weapon.OnDrawFinish();
            RequestStateChange(WeaponStateType.Idle);
        }
    }

    public override void Exit()
    {
        // Cleanup if needed
    }

    public override bool CanTransitionTo(WeaponStateType newState)
    {
        // During switching, only allow transition to idle (when complete)
        return newState == WeaponStateType.Idle && Time.time >= switchEndTime;
    }

    // Get switch progress (0 to 1)
    public float GetSwitchProgress()
    {
        float elapsed = Time.time - (switchEndTime - weapon.DrawTime);
        return Mathf.Clamp01(elapsed / weapon.DrawTime);
    }
}

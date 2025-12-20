using UnityEngine;

/// <summary>
/// Drawing state - weapon is being equipped/drawn
/// </summary>
public class WeaponDrawingState : WeaponStateBase
{
    public override WeaponStateType StateType => WeaponStateType.Drawing;

    private float drawEndTime;

    public WeaponDrawingState(WeaponStateMachine stateMachine, Weapon weapon) 
        : base(stateMachine, weapon) { }

    public override void Enter()
    {
        drawEndTime = Time.time + weapon.DrawTime;
        weapon.OnDrawStart();
        Debug.Log($"[WeaponState] Drawing weapon ({weapon.DrawTime}s)");
    }

    public override void Update()
    {
        if (Time.time >= drawEndTime)
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
        switch (newState)
        {
            case WeaponStateType.Idle:
                return Time.time >= drawEndTime;
            case WeaponStateType.Holstering:
                return true; // Can cancel draw to holster
            default:
                return false;
        }
    }

    // Get draw progress (0 to 1)
    public float GetDrawProgress()
    {
        float elapsed = Time.time - (drawEndTime - weapon.DrawTime);
        return Mathf.Clamp01(elapsed / weapon.DrawTime);
    }
}

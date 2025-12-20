using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Firing state - weapon is actively shooting
/// Handles continuous fire for automatic weapons
/// </summary>
public class WeaponFiringState : WeaponStateBase
{
    public override WeaponStateType StateType => WeaponStateType.Firing;

    private float nextFireTime;

    public WeaponFiringState(WeaponStateMachine stateMachine, Weapon weapon) 
        : base(stateMachine, weapon) { }

    public override void Enter()
    {
        // Fire immediately on entering state
        if (weapon.CanFire())
        {
            weapon.Fire();
            nextFireTime = Time.time + weapon.FireInterval;
        }
        else
        {
            // Can't fire, go back to idle
            RequestStateChange(WeaponStateType.Idle);
        }
    }

    public override void Update()
    {
        if (Mouse.current == null)
        {
            RequestStateChange(WeaponStateType.Idle);
            return;
        }

        bool wantsToFire = weapon.IsAutomatic 
            ? Mouse.current.leftButton.isPressed 
            : Mouse.current.leftButton.wasPressedThisFrame;

        // Check if we should stop firing
        if (!wantsToFire)
        {
            RequestStateChange(WeaponStateType.Idle);
            return;
        }

        // Check if out of ammo
        if (weapon.CurrentAmmo <= 0)
        {
            if (weapon.ReserveAmmo > 0)
            {
                // Auto-reload
                RequestStateChange(WeaponStateType.Reloading);
            }
            else
            {
                // Out of all ammo
                RequestStateChange(WeaponStateType.Idle);
            }
            return;
        }

        // Handle automatic fire
        if (weapon.IsAutomatic && Time.time >= nextFireTime)
        {
            weapon.Fire();
            nextFireTime = Time.time + weapon.FireInterval;
        }
        // For semi-auto, we already fired on Enter, so go back to idle
        else if (!weapon.IsAutomatic)
        {
            RequestStateChange(WeaponStateType.Idle);
        }
    }

    public override void Exit()
    {
        // Stop muzzle flash for auto weapons
        weapon.StopMuzzleFlash();
    }

    public override bool CanTransitionTo(WeaponStateType newState)
    {
        switch (newState)
        {
            case WeaponStateType.Idle:
            case WeaponStateType.Reloading:
                return true;
            case WeaponStateType.Switching:
            case WeaponStateType.Holstering:
                return true; // Allow weapon switch even while firing
            default:
                return false;
        }
    }
}

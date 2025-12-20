using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Idle state - weapon is ready but not actively doing anything
/// Handles transitions to firing, reloading, and inspecting
/// </summary>
public class WeaponIdleState : WeaponStateBase
{
    public override WeaponStateType StateType => WeaponStateType.Idle;

    public WeaponIdleState(WeaponStateMachine stateMachine, Weapon weapon) 
        : base(stateMachine, weapon) { }

    public override void Enter()
    {
        // Weapon is now ready
    }

    public override void Update()
    {
        // Check for state transitions based on input
        CheckForFireInput();
        CheckForReloadInput();
        CheckForInspectInput();
    }

    private void CheckForFireInput()
    {
        if (Mouse.current == null) return;

        bool wantsToFire = weapon.IsAutomatic 
            ? Mouse.current.leftButton.isPressed 
            : Mouse.current.leftButton.wasPressedThisFrame;

        if (wantsToFire && weapon.CanFire())
        {
            RequestStateChange(WeaponStateType.Firing);
        }
        else if (wantsToFire && weapon.CurrentAmmo <= 0 && weapon.ReserveAmmo > 0)
        {
            // Auto-reload when trying to fire with empty mag
            RequestStateChange(WeaponStateType.Reloading);
        }
    }

    private void CheckForReloadInput()
    {
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            if (weapon.CanReload())
            {
                RequestStateChange(WeaponStateType.Reloading);
            }
        }
    }

    private void CheckForInspectInput()
    {
        // Inspect is handled by the Inspect component directly
        // This is here for potential future integration
    }

    public override bool CanTransitionTo(WeaponStateType newState)
    {
        // Idle can transition to most states
        switch (newState)
        {
            case WeaponStateType.Firing:
            case WeaponStateType.Reloading:
            case WeaponStateType.Inspecting:
            case WeaponStateType.Switching:
            case WeaponStateType.Holstering:
                return true;
            default:
                return false;
        }
    }
}

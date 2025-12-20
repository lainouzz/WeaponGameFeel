using UnityEngine;

/// <summary>
/// Inspecting state - weapon is being inspected by the player
/// Integrates with the existing Inspect component
/// </summary>
public class WeaponInspectingState : WeaponStateBase
{
    public override WeaponStateType StateType => WeaponStateType.Inspecting;

    private Inspect inspectComponent;

    public WeaponInspectingState(WeaponStateMachine stateMachine, Weapon weapon, Inspect inspectComponent) 
        : base(stateMachine, weapon) 
    {
        this.inspectComponent = inspectComponent;
    }

    public override void Enter()
    {
        Debug.Log("[WeaponState] Inspecting weapon");
        // Inspection is handled by Inspect component
        // We just track the state here
    }

    public override void Update()
    {
        // Check if inspection ended (Inspect component controls this)
        if (inspectComponent != null && !inspectComponent.isInspecting)
        {
            RequestStateChange(WeaponStateType.Idle);
        }
    }

    public override void Exit()
    {
        Debug.Log("[WeaponState] Inspection ended");
    }

    public override bool CanTransitionTo(WeaponStateType newState)
    {
        // From inspecting, can only go back to idle
        // The Inspect component handles when inspection ends
        switch (newState)
        {
            case WeaponStateType.Idle:
                return true;
            case WeaponStateType.Switching:
            case WeaponStateType.Holstering:
                return true; // Can always switch weapons
            default:
                return false;
        }
    }
}

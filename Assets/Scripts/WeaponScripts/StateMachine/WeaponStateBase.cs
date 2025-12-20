using UnityEngine;

/// <summary>
/// Base class for all weapon states providing common functionality
/// </summary>
public abstract class WeaponStateBase : IWeaponState
{
    protected WeaponStateMachine stateMachine;
    protected Weapon weapon;

    public abstract WeaponStateType StateType { get; }

    public WeaponStateBase(WeaponStateMachine stateMachine, Weapon weapon)
    {
        this.stateMachine = stateMachine;
        this.weapon = weapon;
    }

    public virtual void Enter()
    {
        // Override in derived classes
    }

    public virtual void Update()
    {
        // Override in derived classes
    }

    public virtual void FixedUpdate()
    {
        // Override in derived classes
    }

    public virtual void Exit()
    {
        // Override in derived classes
    }

    public virtual bool CanTransitionTo(WeaponStateType newState)
    {
        // By default, allow all transitions
        // Override in derived classes to restrict transitions
        return true;
    }

    protected void RequestStateChange(WeaponStateType newState)
    {
        stateMachine.ChangeState(newState);
    }
}

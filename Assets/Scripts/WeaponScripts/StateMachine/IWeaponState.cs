/// <summary>
/// Interface that all weapon states must implement
/// </summary>
public interface IWeaponState
{
    /// <summary>
    /// The type of this state
    /// </summary>
    WeaponStateType StateType { get; }

    /// <summary>
    /// Called when entering this state
    /// </summary>
    void Enter();

    /// <summary>
    /// Called every frame while in this state
    /// </summary>
    void Update();

    /// <summary>
    /// Called every fixed update while in this state (for physics)
    /// </summary>
    void FixedUpdate();

    /// <summary>
    /// Called when exiting this state
    /// </summary>
    void Exit();

    /// <summary>
    /// Check if this state can transition to another state
    /// </summary>
    /// <param name="newState">The state to transition to</param>
    /// <returns>True if transition is allowed</returns>
    bool CanTransitionTo(WeaponStateType newState);
}

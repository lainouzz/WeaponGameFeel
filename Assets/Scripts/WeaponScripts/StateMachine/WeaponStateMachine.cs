using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// State machine that manages weapon states and transitions
/// </summary>
public class WeaponStateMachine
{
    private Dictionary<WeaponStateType, IWeaponState> states;
    private IWeaponState currentState;
    private Weapon weapon;

    /// <summary>
    /// The current active state type
    /// </summary>
    public WeaponStateType CurrentStateType => currentState?.StateType ?? WeaponStateType.Idle;

    /// <summary>
    /// Event fired when state changes
    /// </summary>
    public event Action<WeaponStateType, WeaponStateType> OnStateChanged;

    public WeaponStateMachine(Weapon weapon)
    {
        this.weapon = weapon;
        states = new Dictionary<WeaponStateType, IWeaponState>();
    }

    /// <summary>
    /// Register a state with the state machine
    /// </summary>
    public void RegisterState(IWeaponState state)
    {
        if (states.ContainsKey(state.StateType))
        {
            Debug.LogWarning($"State {state.StateType} already registered. Overwriting.");
        }
        states[state.StateType] = state;
    }

    /// <summary>
    /// Initialize the state machine and enter the initial state
    /// </summary>
    public void Initialize(WeaponStateType initialState = WeaponStateType.Idle)
    {
        if (states.TryGetValue(initialState, out var state))
        {
            currentState = state;
            currentState.Enter();
            OnStateChanged?.Invoke(WeaponStateType.Idle, initialState);
        }
        else
        {
            Debug.LogError($"Initial state {initialState} not registered!");
        }
    }

    /// <summary>
    /// Update the current state (call from MonoBehaviour.Update)
    /// </summary>
    public void Update()
    {
        currentState?.Update();
    }

    /// <summary>
    /// Fixed update the current state (call from MonoBehaviour.FixedUpdate)
    /// </summary>
    public void FixedUpdate()
    {
        currentState?.FixedUpdate();
    }

    /// <summary>
    /// Request a state change
    /// </summary>
    /// <param name="newStateType">The state to transition to</param>
    /// <returns>True if the transition was successful</returns>
    public bool ChangeState(WeaponStateType newStateType)
    {
        // Can't change to the same state
        if (currentState != null && currentState.StateType == newStateType)
        {
            return false;
        }

        // Check if the target state exists
        if (!states.TryGetValue(newStateType, out var newState))
        {
            Debug.LogWarning($"State {newStateType} not registered!");
            return false;
        }

        // Check if current state allows transition
        if (currentState != null && !currentState.CanTransitionTo(newStateType))
        {
            return false;
        }

        // Perform the transition
        WeaponStateType previousStateType = currentState?.StateType ?? WeaponStateType.Idle;
        
        currentState?.Exit();
        currentState = newState;
        currentState.Enter();

        OnStateChanged?.Invoke(previousStateType, newStateType);
        
        return true;
    }

    public void ForceState(WeaponStateType newStateType)
    {
        if (!states.TryGetValue(newStateType, out var newState))
        {
            Debug.LogError($"State {newStateType} not registered!");
            return;
        }

        WeaponStateType previousStateType = currentState?.StateType ?? WeaponStateType.Idle;
        
        currentState?.Exit();
        currentState = newState;
        currentState.Enter();

        OnStateChanged?.Invoke(previousStateType, newStateType);
    }

    public bool IsInState(WeaponStateType stateType)
    {
        return currentState?.StateType == stateType;
    }


    public bool CanTransitionTo(WeaponStateType stateType)
    {
        if (currentState == null) return true;
        if (!states.ContainsKey(stateType)) return false;
        return currentState.CanTransitionTo(stateType);
    }

    public T GetState<T>(WeaponStateType stateType) where T : class, IWeaponState
    {
        if (states.TryGetValue(stateType, out var state))
        {
            return state as T;
        }
        return null;
    }
}

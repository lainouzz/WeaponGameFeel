using UnityEngine;

/// <summary>
/// Reloading state - weapon is being reloaded
/// Handles reload timing and magazine drop effects
/// </summary>
public class WeaponReloadingState : WeaponStateBase
{
    public override WeaponStateType StateType => WeaponStateType.Reloading;

    private float reloadEndTime;
    private bool magazineDropped;
    private bool magazineInserted;
    private float magazineDropTime;
    private float magazineInsertTime;

    public WeaponReloadingState(WeaponStateMachine stateMachine, Weapon weapon) 
        : base(stateMachine, weapon) { }

    public override void Enter()
    {
        if (!weapon.CanReload())
        {
            RequestStateChange(WeaponStateType.Idle);
            return;
        }

        reloadEndTime = Time.time + weapon.ReloadTime;
        magazineDropped = false;
        magazineInserted = false;
        
        // Magazine drops at configured percent into the reload
        magazineDropTime = Time.time + (weapon.ReloadTime * weapon.MagazineDropTimePercent);
        
        // New magazine appears at configured percent into the reload
        magazineInsertTime = Time.time + (weapon.ReloadTime * weapon.MagazineInsertTimePercent);

        weapon.OnReloadStart();
        
        Debug.Log($"[WeaponState] Reloading started ({weapon.ReloadTime}s)");
    }

    public override void Update()
    {
        // Handle magazine drop effect (hide current mag + spawn dropped mag)
        if (!magazineDropped && Time.time >= magazineDropTime)
        {
            weapon.DropMagazine();
            magazineDropped = true;
        }

        // Handle new magazine insert (show current mag again)
        if (!magazineInserted && Time.time >= magazineInsertTime)
        {
            weapon.ShowCurrentMagazine();
            magazineInserted = true;
        }

        // Check if reload is complete
        if (Time.time >= reloadEndTime)
        {
            FinishReload();
            return;
        }

        // Check for reload cancel (if allowed and player is trying to fire)
        if (weapon.CanCancelReload && weapon.CurrentAmmo > 0)
        {
            if (UnityEngine.InputSystem.Mouse.current != null && 
                UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
            {
                CancelReload();
                return;
            }
        }
    }

    private void FinishReload()
    {
        // Ensure magazine is visible when reload completes
        weapon.ShowCurrentMagazine();
        weapon.OnReloadFinish();
        Debug.Log($"[WeaponState] Reload complete! Ammo: {weapon.CurrentAmmo}/{weapon.ReserveAmmo}");
        RequestStateChange(WeaponStateType.Idle);
    }

    private void CancelReload()
    {
        // If cancelled after mag was dropped but before new one inserted, show it anyway
        // (gameplay > realism here - player shouldn't be stuck without a mag)
        weapon.ShowCurrentMagazine();
        weapon.OnReloadCancel();
        Debug.Log("[WeaponState] Reload cancelled");
        RequestStateChange(WeaponStateType.Firing);
    }

    public override void Exit()
    {
        // Safety: ensure magazine is visible when leaving reload state
        weapon.ShowCurrentMagazine();
    }

    public override bool CanTransitionTo(WeaponStateType newState)
    {
        switch (newState)
        {
            case WeaponStateType.Idle:
                return true; // Reload finished
            case WeaponStateType.Firing:
                return weapon.CanCancelReload && weapon.CurrentAmmo > 0;
            case WeaponStateType.Switching:
            case WeaponStateType.Holstering:
                return true; // Can always switch weapons
            default:
                return false;
        }
    }

    // Get reload progress (0 to 1)
    public float GetReloadProgress()
    {
        float elapsed = Time.time - (reloadEndTime - weapon.ReloadTime);
        return Mathf.Clamp01(elapsed / weapon.ReloadTime);
    }
}

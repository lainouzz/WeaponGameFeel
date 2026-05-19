using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Weapon
// Keep this as the orchestration layer after the split.
// Responsibilities:
// - state machine setup and transitions
// - ammo / reserve ammo bookkeeping
// - input wiring and high-level weapon control
// - delegating to helper components for recoil, ballistics, visuals, and magazine visuals

public class Weapon : MonoBehaviour
{
    [Header("Weapon Config")]
    public bool isAutomatic;
    public float fireRate;

    [Header("Ammo & Reload")]
    [Tooltip("Maximum ammo in magazine")]
    public int magazineSize = 30;
    [Tooltip("Total reserve ammo")]
    public int maxReserveAmmo = 120;
    [Tooltip("Time to reload in seconds")]
    public float reloadTime = 2f;
    [Tooltip("Can reload be cancelled by firing")]
    public bool canCancelReload = false;

    [Header("Weapon Switching")]
    [Tooltip("Time to draw/equip this weapon")]
    public float drawTime = 0.5f;

    private Inspect inspectComponent;
    private GameInput gameInput;
    private WeaponRecoilController recoilController;
    private WeaponBallistics ballisticsController;
    private WeaponMagazineVisuals magazineVisualsController;
    private WeaponVisuals visualsController;
    private WeaponSoundHandler soundHandler;
    private WeaponStateMachine stateMachine;

    public Animator animator;
    [Tooltip("Fire rate the fire animation was authored at (shots/sec). Adjusts animation speed to match fireRate.")]
    public float baseAnimationFireRate = 5f;

    private int currentAmmo;
    private int reserveAmmo;
    private float nextFireTime;
    private bool isInitialized;

    public WeaponStateMachine StateMachine => stateMachine;
    public WeaponStateType CurrentState => stateMachine?.CurrentStateType ?? WeaponStateType.Idle;
    public int CurrentAmmo => currentAmmo;
    public int ReserveAmmo => reserveAmmo;
    public int MaxReserveAmmo => maxReserveAmmo;
    public bool IsAutomatic => isAutomatic;
    public float ReloadTime => reloadTime;
    public float DrawTime => drawTime;
    public float FireInterval => fireRate > 0 ? 1f / fireRate : 0f;
    public float MagazineDropTimePercent => magazineVisualsController != null ? magazineVisualsController.magazineDropTimePercent : 0.3f;
    public float MagazineInsertTimePercent => magazineVisualsController != null ? magazineVisualsController.magazineInsertTimePercent : 0.7f;
    public bool CanCancelReload => canCancelReload;
    public bool IsAiming => recoilController != null && recoilController.isAiming;
    public Inspect InspectComponent => inspectComponent;
    public bool IsReloading => stateMachine?.IsInState(WeaponStateType.Reloading) ?? false;

    public event Action<int, int> OnAmmoChanged;
    public event Action<bool> OnReloadStateChanged;
    public event Action<WeaponStateType, WeaponStateType> OnWeaponStateChanged;
    public event Action<bool> OnFireModeChanged;

    private void Awake()
    {
        inspectComponent = FindAnyObjectByType<Inspect>();
        gameInput = new GameInput();
        soundHandler = GetComponent<WeaponSoundHandler>();
        recoilController = GetComponent<WeaponRecoilController>();
        ballisticsController = GetComponent<WeaponBallistics>();
        magazineVisualsController = GetComponent<WeaponMagazineVisuals>();
        visualsController = GetComponent<WeaponVisuals>();

        if (recoilController != null)
        {
            recoilController.gameInput = gameInput;
            recoilController.cameraMovement = FindAnyObjectByType<CameraMovement>();
            recoilController.playerMovement = GetComponentInParent<PlayerMovement>();
            recoilController.mainCamera = Camera.main;
        }

        if (ballisticsController != null)
        {
            ballisticsController.recoilController = recoilController;
            ballisticsController.playerMovement = GetComponentInParent<PlayerMovement>();
            ballisticsController.mainCamera = Camera.main;
        }

        InitializeStateMachine();
    }

    private void InitializeStateMachine()
    {
        stateMachine = new WeaponStateMachine(this);
        stateMachine.RegisterState(new WeaponIdleState(stateMachine, this));
        stateMachine.RegisterState(new WeaponFiringState(stateMachine, this));
        stateMachine.RegisterState(new WeaponReloadingState(stateMachine, this));
        stateMachine.RegisterState(new WeaponInspectingState(stateMachine, this, inspectComponent));
        stateMachine.RegisterState(new WeaponDrawingState(stateMachine, this));
        stateMachine.RegisterState(new WeaponSwitchingState(stateMachine, this));
        stateMachine.OnStateChanged += HandleStateChanged;
    }

    private void HandleStateChanged(WeaponStateType from, WeaponStateType to)
    {
        OnWeaponStateChanged?.Invoke(from, to);
        if (from == WeaponStateType.Reloading || to == WeaponStateType.Reloading)
        {
            OnReloadStateChanged?.Invoke(to == WeaponStateType.Reloading);
        }
    }

    private void Start()
    {
        currentAmmo = magazineSize;
        reserveAmmo = maxReserveAmmo;
        OnAmmoChanged += HandleAmmoChanged;
        stateMachine.Initialize(WeaponStateType.Idle);
        isInitialized = true;
        OnAmmoChanged?.Invoke(currentAmmo, reserveAmmo);
    }

    private void HandleAmmoChanged(int current, int reserve)
    {
        visualsController?.UpdateAmmoText(current, reserve);
    }

    private void OnEnable()
    {
        if (gameInput == null) return;
        gameInput.Enable();
        gameInput.Player.Aim.started += OnAimStarted;
        gameInput.Player.Aim.canceled += OnAimCanceled;
    }

    private void OnDisable()
    {
        if (gameInput != null)
        {
            gameInput.Player.Aim.started -= OnAimStarted;
            gameInput.Player.Aim.canceled -= OnAimCanceled;
            gameInput.Disable();
        }

        OnAmmoChanged -= HandleAmmoChanged;
        visualsController?.StopAutoMuzzle();
    }

    private void OnAimStarted(InputAction.CallbackContext ctx)
    {
        if (inspectComponent != null && inspectComponent.isInspecting) return;
        if (stateMachine.IsInState(WeaponStateType.Reloading)) return;
        if (stateMachine.IsInState(WeaponStateType.Switching)) return;
        if (stateMachine.IsInState(WeaponStateType.Drawing)) return;
        if (recoilController != null && recoilController.isAiming) return;

        if (recoilController != null)
        {
            recoilController.isAiming = true;
        }
    }

    private void OnAimCanceled(InputAction.CallbackContext ctx)
    {
        if (recoilController != null)
        {
            recoilController.isAiming = false;
        }
    }

    private void Update()
    {
        if (!isInitialized) return;

        stateMachine.Update();

        if (inspectComponent != null && inspectComponent.isInspecting && !stateMachine.IsInState(WeaponStateType.Inspecting))
        {
            stateMachine.ChangeState(WeaponStateType.Inspecting);
        }

        if ((inspectComponent != null && inspectComponent.isInspecting) || stateMachine.IsInState(WeaponStateType.Reloading))
        {
            if (recoilController != null)
            {
                recoilController.isAiming = false;
            }
        }

        if (inspectComponent == null || !inspectComponent.isInspecting)
        {
            recoilController?.RecoilAndSway();
            recoilController?.UpdateADS();
        }
        else if (recoilController != null)
        {
            recoilController.UpdateRecoil(Time.deltaTime);
            recoilController.UpdateRecoilKick(Time.deltaTime);
            recoilController.ApplyRecoilKickPos();
        }

        ballisticsController?.UpdateSpreadRecovery();

        if (Keyboard.current != null && Keyboard.current.bKey.wasPressedThisFrame)
        {
            ToggleFireMode();
        }
    }

    public bool CanFire()
    {
        if (currentAmmo <= 0) return false;
        if (Time.time < nextFireTime) return false;
        if (inspectComponent != null && inspectComponent.isInspecting) return false;
        return true;
    }

    public bool CanReload()
    {
        if (currentAmmo >= magazineSize) return false;
        if (reserveAmmo <= 0) return false;
        if (inspectComponent != null && inspectComponent.isInspecting) return false;
        return true;
    }

    public void Fire()
    {
        if (!CanFire()) return;

        currentAmmo--;
        OnAmmoChanged?.Invoke(currentAmmo, reserveAmmo);

        
        if (recoilController != null)
        {
            recoilController.recoilPitchVel -= recoilController.recoilImpulePitch;
            recoilController.recoilYawVel += UnityEngine.Random.Range(-recoilController.recoilImpuleYaw, recoilController.recoilImpuleYaw);
            recoilController.recoilRollVel += UnityEngine.Random.Range(-recoilController.recoilImpuleRoll, recoilController.recoilImpuleRoll);
            recoilController.recoilKickVel += recoilController.recoilKickImpule;
            recoilController.lastFireTime = Time.time;
            recoilController.ApplyCameraRecoil();
        }

        ballisticsController?.PerformRaycast();

        nextFireTime = Time.time + FireInterval;
        soundHandler?.PlayFireSound();

        if (visualsController != null)
        {
            if (isAutomatic)
            {
                if (!visualsController.isAutoMuzzlePlaying)
                {
                    if (animator != null && baseAnimationFireRate > 0f)
                    {
                        animator.SetFloat("FireAnimSpeed", fireRate / baseAnimationFireRate);
                    }
                    animator.SetTrigger("FireFull");
                    animator.SetBool("IsFiring", true);
                    visualsController.StartAutoMuzzle();
                }
            }
            else
            {
                // Scale animation speed so the bolt cycle fits within one fire interval
                if (animator != null && baseAnimationFireRate > 0f)
                {
                    animator.SetFloat("FireAnimSpeed", fireRate / baseAnimationFireRate);
                }
                animator.SetTrigger("FireSingle");       
                visualsController.PlaySingleMuzzle();
            }
        }
    }

    public void StopMuzzleFlash()
    {
        visualsController?.StopMuzzleFlash();
    }

    public void OnReloadStart()
    {
        if (recoilController != null)
        {
            recoilController.isAiming = false;
        }
        visualsController?.OnReloadStart();
    }

    public void OnReloadFinish()
    {
        int ammoNeeded = magazineSize - currentAmmo;
        int ammoToAdd = Mathf.Min(ammoNeeded, reserveAmmo);
        currentAmmo += ammoToAdd;
        reserveAmmo -= ammoToAdd;
        OnAmmoChanged?.Invoke(currentAmmo, reserveAmmo);
        visualsController?.OnReloadFinish();
    }

    public void OnReloadCancel()
    {
        visualsController?.OnReloadCancel();
    }

    public void DropMagazine()
    {
        magazineVisualsController?.DropMagazine();
    }

    public void HideCurrentMagazine()
    {
        magazineVisualsController?.HideCurrentMagazine();
    }

    public void ShowCurrentMagazine()
    {
        magazineVisualsController?.ShowCurrentMagazine();
    }

    public void UpdateUI()
    {
        visualsController?.UpdateAmmoText(currentAmmo, reserveAmmo);
    }

    public void OnDrawStart()
    {
        visualsController?.OnDrawStart();
    }

    public void OnDrawFinish()
    {
        visualsController?.OnDrawFinish();
    }

    public void PlayDryFireSound()
    {
        soundHandler?.PlayDryFireSound();
    }

    private void TryReload()
    {
        if (CanReload())
        {
            stateMachine.ChangeState(WeaponStateType.Reloading);
        }
    }

    public void ToggleFireMode()
    {
        if (stateMachine.IsInState(WeaponStateType.Reloading)) return;
        if (stateMachine.IsInState(WeaponStateType.Drawing)) return;
        if (stateMachine.IsInState(WeaponStateType.Switching)) return;

        isAutomatic = !isAutomatic;
        OnFireModeChanged?.Invoke(isAutomatic);
    }

    public void CancelReload()
    {
        if (stateMachine.IsInState(WeaponStateType.Reloading) && canCancelReload)
        {
            stateMachine.ChangeState(WeaponStateType.Idle);
        }
    }
}

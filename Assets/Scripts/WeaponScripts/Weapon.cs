using System;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using Random = UnityEngine.Random;

public class Weapon : MonoBehaviour
{
    public Transform firePoint;
    public ParticleSystem particleSystem;
    public TMP_Text ammoText;
    public StatsManager statsManager;

    public float swapMultiplier;
    public float smoothSwayAmount;
    public float rollAmount;

    //fire config
    public bool isAutomatic;
    public float fireRate;

    //raycast shooting config
    [Header("Raycast Shooting")]
    [Tooltip("Maximum distance the raycast can travel")]
    public float maxRange = 100f;
    [Tooltip("Damage dealt per shot")]
    public float damage = 25f;
    [Tooltip("Layers the raycast can hit")]
    public LayerMask hitLayers = ~0; // Default to everything
    [Tooltip("Impact effect prefab (optional)")]
    public GameObject impactEffectPrefab;
    [Tooltip("Force applied to hit objects with Rigidbody")]
    public float impactForce = 500f;
    [Tooltip("How the force is applied (Impulse = instant, Force = continuous)")]
    public ForceMode impactForceMode = ForceMode.Impulse;

    //bullet spread config
    [Header("Bullet Spread")]
    [Tooltip("Base spread when hipfiring (degrees)")]
    public float hipfireSpread = 3f;
    [Tooltip("Spread when fully aimed (degrees)")]
    public float adsSpread = 0.1f;
    [Tooltip("Additional spread when walking")]
    public float walkSpreadBonus = 1f;
    [Tooltip("Additional spread when sprinting")]
    public float sprintSpreadBonus = 3f;
    [Tooltip("Additional spread when in air")]
    public float airSpreadBonus = 2f;
    [Tooltip("Additional spread when crouching (can be negative for bonus accuracy)")]
    public float crouchSpreadBonus = -0.5f;
    [Tooltip("Spread increase per shot (for sustained fire)")]
    public float spreadPerShot = 0.2f;
    [Tooltip("How fast spread recovers (degrees per second)")]
    public float spreadRecoveryRate = 5f;
    [Tooltip("Max additional spread from sustained fire")]
    public float maxSustainedSpread = 2f;

    //ammo config
    [Header("Ammo & Reload")]
    [Tooltip("Maximum ammo in magazine")]
    public int magazineSize = 30;
    [Tooltip("Total reserve ammo")]
    public int maxReserveAmmo = 120;
    [Tooltip("Time to reload in seconds")]
    public float reloadTime = 2f;
    [Tooltip("Can reload be cancelled by firing")]
    public bool canCancelReload = false;
    [Tooltip("When magazine drops during reload (0-1, percentage of reload time)")]
    [Range(0f, 1f)]
    public float magazineDropTimePercent = 0.3f;

    //magazine drop config
    [Header("Magazine Drop")]
    [Tooltip("Prefab for dropped magazine (optional)")]
    public GameObject droppedMagazinePrefab;
    [Tooltip("The current magazine GameObject on the weapon model")]
    public GameObject currentMagazineObject;
    [Tooltip("Where the magazine spawns from")]
    public Transform magazineDropPoint;
    [Tooltip("Force applied to dropped magazine")]
    public float magazineDropForce = 2f;
    [Tooltip("How long dropped magazine stays in scene")]
    public float magazineLifetime = 10f;
    [Tooltip("When new magazine appears during reload (0-1, percentage of reload time)")]
    [Range(0f, 1f)]
    public float magazineInsertTimePercent = 0.7f;

    //weapon switching config
    [Header("Weapon Switching")]
    [Tooltip("Time to draw/equip this weapon")]
    public float drawTime = 0.5f;
    [Tooltip("Time to holster/put away this weapon")]
    public float holsterTime = 0.3f;

    [Header("Holster Position (Visual)")]
    [Tooltip("Position when holstered")]
    public Vector3 holsterPosition = new Vector3(9.99999142f, 300.000061f, 38.7737503f);
    [Tooltip("Rotation when holstered (Euler angles)")]
    public Vector3 holsterRotation = Vector3.zero;
    [Tooltip("Speed to move to/from holster position")]
    public float holsterMoveSpeed = 5f;

    //recoil/inertia config (visual weapon kick)
    [Header("Visual Weapon Recoil")]
    public float recoilImpulePitch;
    public float recoilImpuleYaw;
    public float recoilImpuleRoll;
    public float recoilSpring;
    public float recoilDamping;
    public float recoilKickImpule;
    public float recoilKickSpring;
    public float recoilKickDamping;

    //procedural camera recoil config (Battlefield-style)
    [Header("Procedural Camera Recoil - Hipfire")]
    [Tooltip("Vertical camera recoil per shot when hipfiring (degrees)")]
    public float cameraRecoilPitchHipfire = 0.3f;
    [Tooltip("Max horizontal camera recoil per shot when hipfiring (degrees, randomized +/-)")]
    public float cameraRecoilYawHipfire = 0.15f;
    [Tooltip("Multiplier for first shot recoil when hipfiring")]
    public float firstShotMultiplierHipfire = 1.5f;

    [Header("Procedural Camera Recoil - ADS")]
    [Tooltip("Vertical camera recoil per shot when aiming (degrees)")]
    public float cameraRecoilPitchADS = 0.059f;
    [Tooltip("Max horizontal camera recoil per shot when aiming (degrees, randomized +/-)")]
    public float cameraRecoilYawADS = 0.15f;
    [Tooltip("Multiplier for first shot recoil when aiming (ADS)")]
    public float firstShotMultiplierADS = 0.6f;

    //aim down sights config
    [Header("Aim Down Sights (ADS)")]
    [Tooltip("Position offset when aiming (adjust to align sights)")]
    public Vector3 aimPosition = new Vector3(0f, -0.1f, 0.1f);
    [Tooltip("Rotation offset when aiming (Euler angles)")]
    public Vector3 aimRotation = Vector3.zero;
    [Tooltip("How fast the weapon moves to/from aim position")]
    public float aimSpeed = 10f;
    [Tooltip("Sway multiplier when aiming (lower = more stable)")]
    [Range(0f, 1f)]
    public float aimSwayMultiplier = 0.3f;
    [Tooltip("How fast position smoothing is (higher = snappier)")]
    public float positionSmoothSpeed = 15f;

    // Component references
    private Inspect inspectComponent;
    private GameInput gameInput;
    private PlayerMovement playerMovement;
    private CameraMovement cameraMovement;
    private Camera mainCamera;
    private WeaponSoundHandler soundHandler;

    // State machine
    private WeaponStateMachine stateMachine;
    
    // Public property for state machine access
    public WeaponStateMachine StateMachine => stateMachine;
    public WeaponStateType CurrentState => stateMachine?.CurrentStateType ?? WeaponStateType.Idle;

    // recoil states
    private float recoilPitch; 
    private float recoilYaw;
    private float recoilRoll;
    private float recoilPitchVel;
    private float recoilYawVel;
    private float recoilRollVel;

    //recoil kick states
    private float recoilKick;
    private float recoilKickVel;
    private Vector3 originalPosition;
    private Quaternion originalRotation;

    private float nextFireTime;
    private float lastFireTime; // Track when we last fired for first shot bonus

    // muzzle flash state
    private bool isAutoMuzzlePlaying;

    // ADS state
    private bool isAiming;
    private float aimLerpT; // 0 = hip, 1 = aimed
    private Vector3 currentPositionOffset; // Current smoothed position offset
    private Quaternion hipRotation;

    // Holster state
    private bool isHolstered;
    private float holsterLerpT; // 0 = ready, 1 = holstered

    // Ammo state
    private int currentAmmo;
    private int reserveAmmo;

    // Spread state
    private float currentSustainedSpread;

    // Public properties for UI and state machine
    public int CurrentAmmo => currentAmmo;
    public int ReserveAmmo => reserveAmmo;
    public int MaxReserveAmmo => maxReserveAmmo;
    public bool IsAutomatic => isAutomatic;
    public float ReloadTime => reloadTime;
    public float DrawTime => drawTime;
    public float HolsterTime => holsterTime;
    public float FireInterval => fireRate > 0 ? 1f / fireRate : 0f;
    public float MagazineDropTimePercent => magazineDropTimePercent;
    public float MagazineInsertTimePercent => magazineInsertTimePercent;
    public bool CanCancelReload => canCancelReload;
    public bool IsAiming => isAiming;
    public bool IsHolstered => isHolstered;
    public Inspect InspectComponent => inspectComponent;

    // Legacy property for backwards compatibility
    public bool IsReloading => stateMachine?.IsInState(WeaponStateType.Reloading) ?? false;

    // Initialization flag
    private bool isInitialized;

    // Events for UI updates
    public event Action<int, int> OnAmmoChanged; // currentAmmo, reserveAmmo
    public event Action<bool> OnReloadStateChanged; // isReloading
    public event Action<WeaponStateType, WeaponStateType> OnWeaponStateChanged; // from, to

    void Awake()
    {
        inspectComponent = FindAnyObjectByType<Inspect>();
        playerMovement = GetComponentInParent<PlayerMovement>();
        cameraMovement = FindAnyObjectByType<CameraMovement>();
        mainCamera = Camera.main;
        gameInput = new GameInput();
        soundHandler = GetComponent<WeaponSoundHandler>();

        // Initialize state machine
        InitializeStateMachine();

        // Ensure particle system is configured safely
        if (particleSystem != null)
        {
            var main = particleSystem.main;
            main.loop = false; // default to non-looping; we'll enable looping when auto-firing
            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void InitializeStateMachine()
    {
        stateMachine = new WeaponStateMachine(this);
        
        // Register all states
        stateMachine.RegisterState(new WeaponIdleState(stateMachine, this));
        stateMachine.RegisterState(new WeaponFiringState(stateMachine, this));
        stateMachine.RegisterState(new WeaponReloadingState(stateMachine, this));
        stateMachine.RegisterState(new WeaponInspectingState(stateMachine, this, inspectComponent));
        stateMachine.RegisterState(new WeaponDrawingState(stateMachine, this));
        stateMachine.RegisterState(new WeaponHolsteringState(stateMachine, this));
        stateMachine.RegisterState(new WeaponSwitchingState(stateMachine, this));

        // Subscribe to state changes
        stateMachine.OnStateChanged += HandleStateChanged;
    }

    private void HandleStateChanged(WeaponStateType from, WeaponStateType to)
    {
        //Debug.Log($"[Weapon] State changed: {from} -> {to}");
        
        // Fire event for external listeners
        OnWeaponStateChanged?.Invoke(from, to);
        
        // Handle reload state changed event for backwards compatibility
        if (from == WeaponStateType.Reloading || to == WeaponStateType.Reloading)
        {
            OnReloadStateChanged?.Invoke(to == WeaponStateType.Reloading);
        }
    }

    void Start()
    {
        // Capture original position/rotation in Start after all Awake calls complete
        // This ensures the hierarchy is fully set up
        originalPosition = transform.localPosition;
        originalRotation = transform.localRotation;
        hipRotation = transform.localRotation;
        
        // Initialize current offset to zero (no offset from original)
        currentPositionOffset = Vector3.zero;

        // Initialize ammo
        currentAmmo = magazineSize;
        reserveAmmo = maxReserveAmmo;
        
        // Subscribe to ammo changes for UI updates
        OnAmmoChanged += HandleAmmoChanged;
        
        // Initialize state machine
        stateMachine.Initialize(WeaponStateType.Idle);
        
        isInitialized = true;

        // Notify UI of initial ammo state
        OnAmmoChanged?.Invoke(currentAmmo, reserveAmmo);
    }

    /// <summary>
    /// Handles UI update when ammo changes
    /// </summary>
    private void HandleAmmoChanged(int current, int reserve)
    {
        if (ammoText != null)
        {
            ammoText.text = $"{current} / {reserve}";
        }
    }

    void OnEnable()
    {
        gameInput.Enable();
        gameInput.Player.Aim.started += OnAimStarted;
        gameInput.Player.Aim.canceled += OnAimCanceled;
    }

    private void OnAimStarted(InputAction.CallbackContext ctx)
    {
        // Can't aim while in certain states
        if (inspectComponent != null && inspectComponent.isInspecting) return;
        if (stateMachine.IsInState(WeaponStateType.Reloading)) return;
        if (stateMachine.IsInState(WeaponStateType.Switching)) return;
        if (stateMachine.IsInState(WeaponStateType.Drawing)) return;
        if (stateMachine.IsInState(WeaponStateType.Holstering)) return;
        if (isHolstered) return; // Can't aim while holstered
        
        isAiming = true;
    }

    private void OnAimCanceled(InputAction.CallbackContext ctx)
    {
        isAiming = false;
    }

    // Update is called once per frame
    void Update()
    {
        // Don't run until properly initialized
        if (!isInitialized) return;

        // Check for holster toggle input (H key)
        if (Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame)
        {
            ToggleHolster();
        }

        // If holstered, skip most weapon logic
        if (isHolstered || holsterLerpT > 0.1f)
        {
            UpdateHolsterPosition();
            return;
        }

        // Update state machine (handles input and state transitions)
        stateMachine.Update();

        // Check for inspect state transition
        if (inspectComponent != null && inspectComponent.isInspecting && 
            !stateMachine.IsInState(WeaponStateType.Inspecting))
        {
            stateMachine.ChangeState(WeaponStateType.Inspecting);
        }

        // Cancel aiming if in certain states
        if ((inspectComponent != null && inspectComponent.isInspecting) || 
            stateMachine.IsInState(WeaponStateType.Reloading))
        {
            isAiming = false;
        }

        // Always update visuals
        if (inspectComponent == null || !inspectComponent.isInspecting)
        {
            RecoilAndSway();
            UpdateADS();
        }
        else
        {
            UpdateRecoil(Time.deltaTime);
            UpdateRecoilKick(Time.deltaTime);
            ApplyRecoilKickPos();
        }

        // Update spread recovery
        UpdateSpreadRecovery();

        // Update holster transition (for unholstering)
        UpdateHolsterPosition();
    }

    #region State Machine Interface Methods
    
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

        // Consume ammo
        currentAmmo--;
        OnAmmoChanged?.Invoke(currentAmmo, reserveAmmo);

        // Visual weapon recoil (the gun model kicks)
        recoilPitchVel += recoilImpulePitch;
        recoilYawVel += UnityEngine.Random.Range(-recoilImpuleYaw, recoilImpuleYaw);
        recoilRollVel += UnityEngine.Random.Range(-recoilImpuleRoll, recoilImpuleRoll);
        recoilKickVel += recoilKickImpule;

        // Procedural camera recoil
        ApplyCameraRecoil();

        // Perform raycast shooting
        PerformRaycast();

        // Update fire time
        nextFireTime = Time.time + FireInterval;
        lastFireTime = Time.time;

        // Play fire sound
        if (soundHandler != null)
        {
            soundHandler.PlayFireSound();
        }

        // Muzzle flash
        if (particleSystem != null)
        {
            if (isAutomatic)
            {
                if (!isAutoMuzzlePlaying)
                {
                    StartAutoMuzzle();
                }
            }
            else
            {
                PlaySingleMuzzle();
            }
        }
    }

    public void StopMuzzleFlash()
    {
        if (particleSystem != null && isAutoMuzzlePlaying)
        {
            StopAutoMuzzle();
        }
    }

    public void OnReloadStart()
    {
        isAiming = false;
    }

    public void OnReloadFinish()
    {
        int ammoNeeded = magazineSize - currentAmmo;
        int ammoToAdd = Mathf.Min(ammoNeeded, reserveAmmo);

        currentAmmo += ammoToAdd;
        reserveAmmo -= ammoToAdd;

        OnAmmoChanged?.Invoke(currentAmmo, reserveAmmo);
    }

    public void OnReloadCancel()
    {
        // Nothing special needed
    }

    public void DropMagazine()
    {
        // Hide the current magazine on the weapon
        HideCurrentMagazine();

        if (droppedMagazinePrefab == null) return;

        Transform dropPoint = magazineDropPoint != null ? magazineDropPoint : transform;
        
        GameObject droppedMag = Instantiate(droppedMagazinePrefab, dropPoint.position, dropPoint.rotation);
        
        // Add physics
        Rigidbody rb = droppedMag.GetComponent<Rigidbody>();
       

        // Apply ejection force
        Vector3 ejectDirection = (-transform.right + -transform.up * 0.5f).normalized;
        rb.AddForce(ejectDirection * magazineDropForce, ForceMode.Impulse);
        rb.AddTorque(UnityEngine.Random.insideUnitSphere * magazineDropForce * 0.5f, ForceMode.Impulse);

        if (rb == null)
        {
            rb = droppedMag.AddComponent<Rigidbody>();
        }
        MeshCollider mc = droppedMag.GetComponent<MeshCollider>();
        if (mc == null)
        {
            mc = droppedMag.AddComponent<MeshCollider>();
            mc.convex = true;
        }

        // Destroy after lifetime
        Destroy(droppedMag, magazineLifetime);
        
        //Debug.Log("[Weapon] Magazine dropped");
    }

    public void HideCurrentMagazine()
    {
        if (currentMagazineObject != null)
        {
            currentMagazineObject.SetActive(false);
           // Debug.Log("[Weapon] Current magazine hidden");
        }
    }

    public void ShowCurrentMagazine()
    {
        if (currentMagazineObject != null)
        {
            currentMagazineObject.SetActive(true);
           // Debug.Log("[Weapon] Current magazine shown");
        }
    }

    public void UpdateUI()
    {
        ammoText.text = $"{currentAmmo} / {reserveAmmo}";
    }

    public void OnDrawStart()
    {
        // Could trigger animation here
    }

    public void OnDrawFinish()
    {
        // Weapon is ready
    }

    public void OnHolsterStart()
    {
        isAiming = false;
    }
    public void OnHolsterFinish()
    {
        // Weapon can be disabled now
    }

    /// <summary>
    /// Play dry fire sound (called when firing with no ammo)
    /// </summary>
    public void PlayDryFireSound()
    {
        if (soundHandler != null)
        {
            soundHandler.PlayDryFireSound();
        }
    }

    #endregion

    #region Legacy Methods (for backwards compatibility)

    private void HandleReloadInput()
    {
        // Now handled by state machine
    }

    private void TryReload()
    {
        if (CanReload())
        {
            stateMachine.ChangeState(WeaponStateType.Reloading);
        }
    }

    private void HandleReloadProgress()
    {
        // Now handled by state machine
    }

    private void HandleFireInput()
    {
        // Now handled by state machine
    }

    public void CancelReload()
    {
        if (stateMachine.IsInState(WeaponStateType.Reloading) && canCancelReload)
        {
            stateMachine.ChangeState(WeaponStateType.Idle);
        }
    }

    #endregion

    private void UpdateADS()
    {
        float dt = Time.deltaTime;

        // Smoothly interpolate aim lerp value
        float targetLerp = isAiming ? 1f : 0f;
        aimLerpT = Mathf.MoveTowards(aimLerpT, targetLerp, aimSpeed * dt);

        // Calculate target offset from original position
        Vector3 aimOffset = Vector3.Lerp(Vector3.zero, aimPosition, aimLerpT);
        Vector3 recoilKickOffset = new Vector3(0, 0, -recoilKick);
        Vector3 targetOffset = aimOffset + recoilKickOffset;

        // Smoothly interpolate the offset (not the absolute position)
        currentPositionOffset = Vector3.Lerp(currentPositionOffset, targetOffset, positionSmoothSpeed * dt);

        // Apply offset to original position
        transform.localPosition = originalPosition + currentPositionOffset;
    }

    private void StartAutoMuzzle()
    {
        var main = particleSystem.main;
        main.loop = true;
        particleSystem.Play();
        isAutoMuzzlePlaying = true;
    }

    private void StopAutoMuzzle()
    {
        particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        isAutoMuzzlePlaying = false;
    }

    private void PlaySingleMuzzle()
    {
        var main = particleSystem.main;
        main.loop = false;
        particleSystem.Play();
    }

    private void PerformRaycast()
    {
        // Get ray origin and direction from camera center (screen center)
        Ray ray;
        if (mainCamera != null)
        {
            ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        }
        else
        {
            // Fallback to fire point forward
            ray = new Ray(firePoint.position, firePoint.forward);
        }

        // Apply bullet spread
        float totalSpread = CalculateTotalSpread();
        Vector3 spreadDirection = ApplySpread(ray.direction, totalSpread);
        ray = new Ray(ray.origin, spreadDirection);

        // Add sustained spread for next shot
        currentSustainedSpread = Mathf.Min(currentSustainedSpread + spreadPerShot, maxSustainedSpread);

        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, maxRange, hitLayers))
        {
            // Debug visualization
            Debug.DrawLine(ray.origin, hit.point, Color.red, 1f);

            // Spawn impact effect if assigned
            if (impactEffectPrefab != null)
            {
                GameObject impact = Instantiate(impactEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(impact, 2f); // Clean up after 2 seconds
            }

            // Apply physics force to hit object
            ApplyImpactForce(hit, ray.direction);

            if (hit.collider.TryGetComponent<IDamagable>(out var damagable))
            {
                Debug.Log($"Found IDamagable on {hit.collider.name}");

                if (statsManager == null)
                {
                    Debug.LogError("statsManager reference is NULL in Weapon!");
                    return;
                }

                float finalDmg = statsManager.GetFinalDamage(damage);
                Debug.Log($"Calling TakeDamage({finalDmg}) on {hit.collider.name}");

                damagable.TakeDamage(finalDmg);
            }
        }
        else
        {
            // Missed - draw ray to max range
            Debug.DrawRay(ray.origin, ray.direction * maxRange, Color.yellow, 1f);
        }
    }

    private void ApplyImpactForce(RaycastHit hit, Vector3 direction)
    {
        if (impactForce <= 0f) return;

        // Try to get Rigidbody from hit object
        Rigidbody rb = hit.collider.attachedRigidbody;
        
        if (rb != null && !rb.isKinematic)
        {
            // Apply force at the hit point in the direction of the shot
            rb.AddForceAtPosition(direction.normalized * impactForce, hit.point, impactForceMode);
        }
    }

    private float CalculateTotalSpread()
    {
        // Base spread lerped between hipfire and ADS
        float baseSpread = Mathf.Lerp(hipfireSpread, adsSpread, aimLerpT);

        // Movement spread bonuses
        float movementSpread = 0f;

        if (playerMovement != null)
        {
            // Check if in air
            if (!playerMovement.isGrounded)
            {
                movementSpread += airSpreadBonus;
            }
            else
            {
                // Ground movement bonuses
                if (playerMovement.IsCrouching)
                {
                    movementSpread += crouchSpreadBonus; // Can be negative for accuracy bonus
                }
                
                if (playerMovement.IsMoving)
                {
                    if (playerMovement.IsSprinting)
                    {
                        movementSpread += sprintSpreadBonus;
                    }
                    else
                    {
                        movementSpread += walkSpreadBonus;
                    }
                }
            }
        }

        // Sustained fire spread (from continuous shooting)
        float sustainedSpread = currentSustainedSpread;

        // Total spread (can't go below 0)
        float totalSpread = Mathf.Max(0f, baseSpread + movementSpread + sustainedSpread);

        return totalSpread;
    }

    private Vector3 ApplySpread(Vector3 direction, float spreadAngle)
    {
        if (spreadAngle <= 0f)
            return direction;

        // Random point in a cone
        float spreadRad = spreadAngle * Mathf.Deg2Rad;
        
        // Random angle around the direction
        float randomAngle = UnityEngine.Random.Range(0f, 2f * Mathf.PI);
        
        // Random distance from center (use sqrt for uniform distribution in circle)
        float randomRadius = Mathf.Sqrt(UnityEngine.Random.Range(0f, 1f)) * Mathf.Tan(spreadRad);

        // Create perpendicular vectors
        Vector3 right = Vector3.Cross(direction, Vector3.up).normalized;
        if (right.magnitude < 0.001f)
        {
            right = Vector3.Cross(direction, Vector3.forward).normalized;
        }
        Vector3 up = Vector3.Cross(right, direction).normalized;

        // Apply offset
        Vector3 spreadOffset = right * (Mathf.Cos(randomAngle) * randomRadius) + 
                               up * (Mathf.Sin(randomAngle) * randomRadius);

        return (direction + spreadOffset).normalized;
    }

    private void UpdateSpreadRecovery()
    {
        // Recover sustained spread over time when not firing
        if (currentSustainedSpread > 0f)
        {
            currentSustainedSpread = Mathf.Max(0f, currentSustainedSpread - spreadRecoveryRate * Time.deltaTime);
        }
    }

    private void ApplyCameraRecoil()
    {
        if (cameraMovement == null) return;

        // Check if this is a "first shot" (hasn't fired recently)
        float timeSinceLastFire = Time.time - lastFireTime;
        bool isFirstShot = timeSinceLastFire > 0.3f; // 300ms gap = first shot

        // Lerp between hipfire and ADS values based on aim state
        float cameraRecoilPitch = Mathf.Lerp(cameraRecoilPitchHipfire, cameraRecoilPitchADS, aimLerpT);
        float cameraRecoilYaw = Mathf.Lerp(cameraRecoilYawHipfire, cameraRecoilYawADS, aimLerpT);
        float firstShotMultiplier = Mathf.Lerp(firstShotMultiplierHipfire, firstShotMultiplierADS, aimLerpT);

        float pitchMultiplier = isFirstShot ? firstShotMultiplier : 1f;

        // Calculate recoil amounts
        float pitch = cameraRecoilPitch * pitchMultiplier;
        float yaw = UnityEngine.Random.Range(-cameraRecoilYaw, cameraRecoilYaw);

        // Apply to camera
        cameraMovement.AddRecoil(pitch, yaw);
    }

    public void RecoilAndSway()
    {
        // Apply sway multiplier when aiming (less sway = more stable)
        float currentSwayMultiplier = Mathf.Lerp(1f, aimSwayMultiplier, aimLerpT);

        Vector2 mouse = gameInput.Player.Look.ReadValue<Vector2>() * swapMultiplier * currentSwayMultiplier;
        mouse.y = Mathf.Clamp(mouse.y, -10f, 10f);
        mouse.x = Mathf.Clamp(mouse.x, -10f, 10f);

        Quaternion rotX = Quaternion.AngleAxis(-mouse.y, Vector3.right);
        Quaternion rotY = Quaternion.AngleAxis(mouse.x, Vector3.up);
        Quaternion swayRot = rotX * rotY;

        Vector2 moveInput = gameInput.Player.Move.ReadValue<Vector2>();
        // Reduce roll when aiming
        float currentRollAmount = Mathf.Lerp(rollAmount, rollAmount * 0.3f, aimLerpT);
        Quaternion rollX = Quaternion.AngleAxis(moveInput.x * currentRollAmount, Vector3.forward);
        rollX = Mathf.Abs(moveInput.x) > 0.1f ? rollX : Quaternion.Slerp(rollX, Quaternion.identity, Time.deltaTime * 5f);

        UpdateRecoil(Time.deltaTime);
        Quaternion recoilRot = Quaternion.Euler(recoilPitch, recoilYaw, recoilRoll);

        // Blend aim rotation
        Quaternion aimRot = Quaternion.Euler(aimRotation);
        Quaternion baseRot = Quaternion.Slerp(Quaternion.identity, aimRot, aimLerpT);

        Quaternion finalRot = baseRot * swayRot * recoilRot * rollX;
        // Slerp toward the combined rotation - use a fixed interpolation factor
        float rotSmoothFactor = Mathf.Clamp01(smoothSwayAmount * Time.deltaTime);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, finalRot, rotSmoothFactor);

        UpdateRecoilKick(Time.deltaTime);
    }

    private void ApplyRecoilKickPos()
    {
        // When inspecting, just apply kick offset directly
        transform.localPosition = originalPosition + new Vector3(0, 0, -recoilKick);
    }

    private void UpdateRecoilKick(float deltaTime)
    {
        float k = recoilKickSpring; //spring constant
        float c = recoilKickDamping; //damping constant

        float a = -k * recoilKick - c * recoilKickVel; // spring damping formula
        recoilKickVel += a * deltaTime;
        recoilKick += recoilKickVel * deltaTime;

        if(Mathf.Abs(recoilKick) < 0.0005f && Mathf.Abs(recoilKickVel) < 0.0005f)
        {
            recoilKick = 0;
            recoilKickVel = 0;
        }
        if(recoilKick < 0f && recoilKickVel < 0f)
        {
            recoilKick = 0;
            recoilKickVel = 0;
        }
    }

    private void UpdateRecoil(float deltaTime)
    {
        //Simple recoil spring formula: a = -k * x - c * v

        float k = recoilSpring; //spring constant
        float c = recoilDamping; //damping constant

        float pitchA = -k * recoilPitch - c * recoilPitchVel;
        float yawA = -k * recoilYaw - c * recoilYawVel;
        float rollA = -k * recoilRoll - c * recoilRollVel;

        recoilPitchVel += pitchA * deltaTime;
        recoilYawVel += yawA * deltaTime;
        recoilRollVel += rollA * deltaTime;

        recoilPitch += recoilPitchVel * deltaTime;
        recoilYaw += recoilYawVel * deltaTime;
        recoilRoll += recoilRollVel * deltaTime;

        //threashholds
        if (Mathf.Abs(recoilPitch) < 0.001 && Mathf.Abs(recoilPitchVel) < 0.001)
        {
            recoilPitch = 0;
            recoilPitchVel = 0;

        }
        if (Mathf.Abs(recoilYaw) < 0.001 && Mathf.Abs(recoilYawVel) < 0.001)
        {
            recoilYaw = 0;
            recoilYawVel = 0;
        }
        if (Mathf.Abs(recoilRoll) < 0.001 && Mathf.Abs(recoilRollVel) < 0.001)
        {
            recoilRoll = 0;
            recoilRollVel = 0;
        }
    }

    public void AddAmmo(int amount)
    {
        reserveAmmo = Mathf.Min(reserveAmmo + amount, maxReserveAmmo);
        OnAmmoChanged?.Invoke(currentAmmo, reserveAmmo);
    }

    private void OnDisable()
    {
        gameInput.Player.Aim.started -= OnAimStarted;
        gameInput.Player.Aim.canceled -= OnAimCanceled;
        gameInput.Disable();
        
        // Unsubscribe from events
        OnAmmoChanged -= HandleAmmoChanged;
        
        // Ensure muzzle flash is stopped
        if (particleSystem != null)
        {
            StopAutoMuzzle();
        }
    }

    #region Holster System

    public void ToggleHolster()
    {
        if (isHolstered)
        {
            Unholster();
        }
        else
        {
            Holster();
        }
    }

    public void Holster()
    {
        if (isHolstered) return;
        
        isHolstered = true;
        isAiming = false;
        
        // Stop any muzzle flash
        StopMuzzleFlash();
        
        Debug.Log("[Weapon] Holstering weapon");
    }

    public void Unholster()
    {
        if (!isHolstered) return;
        
        isHolstered = false;
        Debug.Log("[Weapon] Unholstering weapon");
    }

    private void UpdateHolsterPosition()
    {
        float dt = Time.deltaTime;
        
        // Smoothly interpolate holster lerp value
        float targetLerp = isHolstered ? 1f : 0f;
        holsterLerpT = Mathf.MoveTowards(holsterLerpT, targetLerp, holsterMoveSpeed * dt);
        
        // If fully unholstered, let normal weapon logic handle position
        if (holsterLerpT < 0.01f)
        {
            holsterLerpT = 0f;
            return;
        }
        
        // Calculate holster position/rotation
        Vector3 targetPos = Vector3.Lerp(originalPosition, holsterPosition, holsterLerpT);
        Quaternion holsterRot = Quaternion.Euler(holsterRotation);
        Quaternion targetRot = Quaternion.Slerp(originalRotation, holsterRot, holsterLerpT);
        
        // Apply
        transform.localPosition = targetPos;
        transform.localRotation = targetRot;
    }

    #endregion
}

using UnityEngine;
using UnityEngine.InputSystem;

// WeaponRecoilController
// Move here from `Weapon.cs`:
// - `RecoilAndSway()` - DONE
// - `UpdateADS()` - DONE
// - `ApplyRecoilKickPos()` - DONE
// - `UpdateRecoilKick(float deltaTime)` - DONE
// - `UpdateRecoil(float deltaTime)` - DONE
// - `ApplyCameraRecoil()` - DONE   
// - recoil fields: `recoilPitch`, `recoilYaw`, `recoilRoll`, `recoilPitchVel`, `recoilYawVel`, `recoilRollVel`, `recoilKick`, `recoilKickVel`
// - ADS/visual fields: `swapMultiplier`, `smoothSwayAmount`, `rollAmount`, `aimPosition`, `aimRotation`, `aimSpeed`, `aimSwayMultiplier`, `positionSmoothSpeed`, `aimLerpT`, `isAiming`, `currentPositionOffset`, `originalPosition`, `cameraMovement`, `playerMovement`, `mainCamera`, `lastFireTime`
// - recoil tuning fields: `recoilImpulePitch`, `recoilImpuleYaw`, `recoilImpuleRoll`, `recoilSpring`, `recoilDamping`, `recoilKickImpule`, `recoilKickSpring`, `recoilKickDamping`
// - camera recoil fields: `cameraRecoilPitchHipfire`, `cameraRecoilYawHipfire`, `firstShotMultiplierHipfire`, `cameraRecoilPitchADS`, `cameraRecoilYawADS`, `firstShotMultiplierADS`

public class WeaponRecoilController : MonoBehaviour
{
    [Header("Recoil State")]
    public float recoilPitch;
    public float recoilYaw;
    public float recoilRoll;
    public float recoilPitchVel;
    public float recoilYawVel;
    public float recoilRollVel;
    public float recoilKick;
    public float recoilKickVel;

    [Header("ADS / Visual State")]
    public float swapMultiplier = 1f;
    public float smoothSwayAmount = 1f;
    public float rollAmount = 1f;
    public Vector3 aimPosition;
    public Vector3 aimRotation;
    public float aimSpeed = 10f;
    public float aimSwayMultiplier = 1f;
    public float positionSmoothSpeed = 10f;
    public float aimLerpT;
    public bool isAiming;
    public Vector3 currentPositionOffset = Vector3.zero;
    public Vector3 originalPosition;

    [Header("Weapon / Input References")]
    public GameInput gameInput;
    public CameraMovement cameraMovement;
    public PlayerMovement playerMovement;
    public Camera mainCamera;
    public float lastFireTime;

    [Header("Recoil Tuning")]
    public float recoilImpulePitch = 10f;
    public float recoilImpuleYaw = 5f;
    public float recoilImpuleRoll = 2f;
    public float recoilSpring = 20f;
    public float recoilDamping = 5f;
    public float recoilKickImpule = 0.5f;
    public float recoilKickSpring = 15f;
    public float recoilKickDamping = 4f;

    [Header("Camera Recoil")]
    public float cameraRecoilPitchHipfire = 2f;
    public float cameraRecoilYawHipfire = 1f;
    public float firstShotMultiplierHipfire = 1.5f;
    public float cameraRecoilPitchADS = 1.5f;
    public float cameraRecoilYawADS = 0.75f;
    public float firstShotMultiplierADS = 1.25f;

    public void Start()
    {
        originalPosition = transform.localPosition;
        if (cameraMovement == null) cameraMovement = FindAnyObjectByType<CameraMovement>();
        if (playerMovement == null) playerMovement = GetComponentInParent<PlayerMovement>();
        if (mainCamera == null && Camera.main != null) mainCamera = Camera.main;
    }

    public void RecoilAndSway()
    {
        if (gameInput == null) return;

        // Apply sway multiplier when aiming (less sway = more stable)
        float currentSwayMultiplier = Mathf.Lerp(1f, aimSwayMultiplier, aimLerpT);

        // Scale mouse input consistently (0.01f matches CameraMovement look scaling)
        Vector2 mouse = gameInput.Player.Look.ReadValue<Vector2>() * 0.01f * swapMultiplier * currentSwayMultiplier;
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

    public void UpdateADS()
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

    public void ApplyRecoilKickPos()
    {
        // When inspecting, just apply kick offset directly
        transform.localPosition = originalPosition + new Vector3(0, 0, -recoilKick);
    }

    public void UpdateRecoilKick(float deltaTime)
    {
        float k = recoilKickSpring; //spring constant
        float c = recoilKickDamping; //damping constant

        float a = -k * recoilKick - c * recoilKickVel; // spring damping formula
        recoilKickVel += a * deltaTime;
        recoilKick += recoilKickVel * deltaTime;

        if (Mathf.Abs(recoilKick) < 0.0005f && Mathf.Abs(recoilKickVel) < 0.0005f)
        {
            recoilKick = 0;
            recoilKickVel = 0;
        }
        if (recoilKick < 0f && recoilKickVel < 0f)
        {
            recoilKick = 0;
            recoilKickVel = 0;
        }
    }

    public void UpdateRecoil(float deltaTime)
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

    public void ApplyCameraRecoil()
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
}
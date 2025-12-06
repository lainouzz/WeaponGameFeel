using UnityEngine;
using UnityEngine.InputSystem;

public class Inspect : MonoBehaviour
{
    public float maxPitch = 30f;
    [Range(-30f, 0f)]
    public float minPitch = -30f;
    [Range(0f, 90f)]
    public float maxRoll = 30f;
    [Range(-90f, 0f)]
    public float minRoll = -30f;
    [Range(0f, 90f)]
    public float maxYaw = 30f;
    [Range(-90f, 0f)]
    public float minYaw = -30f;

    // Roll extension config
    [Header("Extended Roll (Inspect Other Side)")]
    [Tooltip("How close to max/min roll before extension is allowed")]
    public float rollThreshold = 5f;
    [Tooltip("Additional roll beyond max/min when extended")]
    public float rollExtension = 60f;
    [Tooltip("How far back from max you need to go to exit extended mode")]
    public float rollExitThreshold = 15f;

    // weight
    public float weight = 1f; // in KG
    public float baseWeight = 2f; // in KG

    // weighted rotation settings (legacy PD params, not used with SmoothDamp approach)
    public float KpBase = 12f;
    public float KdBase = 4f;

    public float rotationSpeed = 120f;
    public float returnSpeed = 8f; // kept for backward compat, not used directly
    public Transform weaponPivot;

    // Smoothing config
    [Tooltip("Base smoothing time (s). Scaled by weight.")]
    public float baseSmoothTime = 0.12f;
    [Tooltip("Maximum angular speed (deg/s).")]
    public float maxAngularSpeed = 720f;

    private Quaternion originalRotation;

    // Current angles (deg)
    private float pitch; // around X
    private float roll;  // around Z
    private float yaw;   // around Y

    // Target angles (deg)
    private float targetPitch;
    private float targetRoll;
    private float targetYaw;

    // Velocities for SmoothDamp (deg/s)
    private float pitchVel;
    private float rollVel;
    private float yawVel;

    private GameInput gameInput;
    public bool isInspecting;
    public bool isAtMaxRoll;

    // Extended roll state tracking
    private bool isExtendedRollActive;
    private int extendedRollDirection; // 1 = positive (max), -1 = negative (min), 0 = none

    void Awake()
    {
        gameInput = new GameInput();
    }

    void OnEnable()
    {
        gameInput.Enable();
        gameInput.Player.Inspect.started += OnInspectStarted;
        gameInput.Player.Inspect.canceled += OnInspectCanceled;
    }

    void Start()
    {
        if (weaponPivot != null)
            originalRotation = weaponPivot.localRotation;
        else
            originalRotation = transform.localRotation;

        // Initialize targets to current to avoid a jump on first update
        targetPitch = pitch;
        targetRoll = roll;
        targetYaw = yaw;
    }

    void Update()
    {
        if (isInspecting)
        {
            OnInspect();
        }
        else
        {
            InspectDone();
        }

        // Apply rotation each frame from current angles
        Quaternion offset = Quaternion.Euler(pitch, yaw, roll);
        if (weaponPivot != null)
            weaponPivot.localRotation = originalRotation * offset;
        else
            transform.localRotation = originalRotation * offset;
    }

    private void OnInspectStarted(InputAction.CallbackContext ctx)
    {
        isInspecting = true;
        // Align targets to current so input feels continuous
        targetPitch = pitch;
        targetRoll = roll;
        targetYaw = yaw;
    }

    private void OnInspectCanceled(InputAction.CallbackContext ctx)
    {
        isInspecting = false;
        // Reset extended roll state when inspection ends
        isExtendedRollActive = false;
        extendedRollDirection = 0;
        isAtMaxRoll = false;
    }

    public void OnInspect()
    {
        float dt = Time.deltaTime;

        // Weight scaling: heavier -> slower input and slower response
        float weightScale = 1f + (weight - baseWeight) / baseWeight;
        weightScale = Mathf.Max(0.1f, weightScale);

        float sensitivity = rotationSpeed / weightScale;
        float smoothTime = Mathf.Max(0.01f, baseSmoothTime * weightScale);

        Vector2 lookInput = gameInput.Player.Look.ReadValue<Vector2>();

        // Calculate dynamic roll limits
        float dynamicMaxRoll = maxRoll;
        float dynamicMinRoll = minRoll;

        // Check for entering extended roll mode (hysteresis: enter condition)
        if (!isExtendedRollActive)
        {
            // Near max roll and pushing further right
            if (targetRoll >= maxRoll - rollThreshold && lookInput.x < 0)
            {
                isExtendedRollActive = true;
                extendedRollDirection = 1;
                isAtMaxRoll = true;
            }
            // Near min roll and pushing further left
            else if (targetRoll <= minRoll + rollThreshold && lookInput.x > 0)
            {
                isExtendedRollActive = true;
                extendedRollDirection = -1;
                isAtMaxRoll = true;
            }
        }
        else
        {
            // Check for exiting extended roll mode (hysteresis: exit condition - must back off significantly)
            if (extendedRollDirection == 1 && targetRoll < maxRoll - rollExitThreshold)
            {
                isExtendedRollActive = false;
                extendedRollDirection = 0;
                isAtMaxRoll = false;
            }
            else if (extendedRollDirection == -1 && targetRoll > minRoll + rollExitThreshold)
            {
                isExtendedRollActive = false;
                extendedRollDirection = 0;
                isAtMaxRoll = false;
            }
        }

        // Apply extended limits when active
        if (isExtendedRollActive)
        {
            if (extendedRollDirection == 1)
                dynamicMaxRoll = maxRoll + rollExtension;
            else if (extendedRollDirection == -1)
                dynamicMinRoll = minRoll - rollExtension;
        }

        // Integrate input into target angles and clamp using dynamic limits
        targetRoll = Mathf.Clamp(targetRoll - lookInput.x * sensitivity * dt, dynamicMinRoll, dynamicMaxRoll);
        targetPitch = Mathf.Clamp(targetPitch - lookInput.y * sensitivity * dt, minPitch, maxPitch);
        targetYaw = Mathf.Clamp(targetYaw + lookInput.x * sensitivity * dt, minYaw, maxYaw);

        // Smoothly move current angles toward targets (heavy feel without fighting input)
        pitch = Mathf.SmoothDamp(pitch, targetPitch, ref pitchVel, smoothTime, maxAngularSpeed, dt);
        roll = Mathf.SmoothDamp(roll, targetRoll, ref rollVel, smoothTime, maxAngularSpeed, dt);
        yaw = Mathf.SmoothDamp(yaw, targetYaw, ref yawVel, smoothTime, maxAngularSpeed, dt);

        // Hard clamp as safety
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        roll = Mathf.Clamp(roll, minRoll - rollExtension, maxRoll + rollExtension);
        yaw = Mathf.Clamp(yaw, minYaw, maxYaw);
    }

    public void InspectDone()
    {
        // When not inspecting, return to original (zero offset) using the same smoothing
        float dt = Time.deltaTime;

        float weightScale = 1f + (weight - baseWeight) / baseWeight;
        weightScale = Mathf.Max(0.1f, weightScale);
        float smoothTime = Mathf.Max(0.01f, baseSmoothTime * weightScale);

        // Targets are zero offset in local weapon space
        targetPitch = 0f;
        targetRoll = 0f;
        targetYaw = 0f;

        pitch = Mathf.SmoothDamp(pitch, targetPitch, ref pitchVel, smoothTime, maxAngularSpeed, dt);
        roll = Mathf.SmoothDamp(roll, targetRoll, ref rollVel, smoothTime, maxAngularSpeed, dt);
        yaw = Mathf.SmoothDamp(yaw, targetYaw, ref yawVel, smoothTime, maxAngularSpeed, dt);

        // Safety clamp
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        roll = Mathf.Clamp(roll, minRoll, maxRoll);
        yaw = Mathf.Clamp(yaw, minYaw, maxYaw);
    }

    void OnDisable()
    {
        gameInput.Player.Inspect.started -= OnInspectStarted;
        gameInput.Player.Inspect.canceled -= OnInspectCanceled;
        gameInput.Disable();
    }
}

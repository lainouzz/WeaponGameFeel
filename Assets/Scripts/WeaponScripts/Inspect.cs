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

        // Integrate input into target angles and clamp
        targetRoll = Mathf.Clamp(targetRoll - lookInput.x * sensitivity * dt, minRoll, maxRoll);
        targetPitch = Mathf.Clamp(targetPitch - lookInput.y * sensitivity * dt, minPitch, maxPitch);
        targetYaw = Mathf.Clamp(targetYaw + lookInput.x * sensitivity * dt, minYaw, maxYaw);

        // Smoothly move current angles toward targets (heavy feel without fighting input)
        pitch = Mathf.SmoothDamp(pitch, targetPitch, ref pitchVel, smoothTime, maxAngularSpeed, dt);
        roll = Mathf.SmoothDamp(roll, targetRoll, ref rollVel, smoothTime, maxAngularSpeed, dt);
        yaw = Mathf.SmoothDamp(yaw, targetYaw, ref yawVel, smoothTime, maxAngularSpeed, dt);

        // Hard clamp as safety (should be rarely hit since targets are clamped)
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        roll = Mathf.Clamp(roll, minRoll, maxRoll);
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

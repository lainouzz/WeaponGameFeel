using UnityEngine;
using UnityEngine.InputSystem;

public class CameraMovement : MonoBehaviour
{
    public Transform camera;

    public GameInput gameInput;

    public Transform playerTransform;

    [Range(1f, 100f)]
    public float mouseSensX;
    [Range(1f, 100f)]
    public float mouseSensY;

    [Header("Procedural Recoil (Battlefield-style)")]
    [Tooltip("How fast recoil recovers when not firing (lower = slower recovery)")]
    public float recoilRecoverySpeed = 5f;
    [Tooltip("How much mouse input helps counter recoil (multiplier)")]
    public float recoilCounterMultiplier = 1.2f;
    [Tooltip("Max vertical recoil accumulation (degrees)")]
    public float maxVerticalRecoil = 8f;
    [Tooltip("Max horizontal recoil accumulation (degrees)")]
    public float maxHorizontalRecoil = 3f;

    [Header("Crouch Camera")]
    [Tooltip("How much the camera lowers when crouching")]
    public float crouchCameraOffset = -0.5f;
    [Tooltip("How fast the camera moves when crouching/standing")]
    public float crouchCameraSpeed = 10f;

    [Header("Head Bob - Walking")]
    [Tooltip("Vertical bob amount when walking")]
    public float walkBobAmountY = 0.02f;
    [Tooltip("Horizontal bob amount when walking")]
    public float walkBobAmountX = 0.01f;
    [Tooltip("Bob speed when walking")]
    public float walkBobSpeed = 10f;

    [Header("Head Bob - Sprinting")]
    [Tooltip("Vertical bob amount when sprinting")]
    public float sprintBobAmountY = 0.04f;
    [Tooltip("Horizontal bob amount when sprinting")]
    public float sprintBobAmountX = 0.02f;
    [Tooltip("Bob speed when sprinting")]
    public float sprintBobSpeed = 14f;

    [Header("Head Bob - Crouching")]
    [Tooltip("Vertical bob amount when crouching")]
    public float crouchBobAmountY = 0.01f;
    [Tooltip("Horizontal bob amount when crouching")]
    public float crouchBobAmountX = 0.005f;
    [Tooltip("Bob speed when crouching")]
    public float crouchBobSpeed = 6f;

    [Header("Head Bob Settings")]
    [Tooltip("How fast bob smooths in/out")]
    public float bobSmoothSpeed = 8f;

    private float verticalRot;
    private float mouseX;
    private float mouseY;

    // Procedural recoil state
    private float recoilPitch;      // Current vertical recoil offset
    private float recoilYaw;        // Current horizontal recoil offset
    private float targetRecoilPitch; // Target recoil to reach
    private float targetRecoilYaw;
    private float recoilPitchVel;   // SmoothDamp velocity
    private float recoilYawVel;

    // Head bob state
    private float bobTimer;
    private Vector3 bobOffset;
    private Vector3 targetBobOffset;
    private Vector3 originalCameraLocalPos;

    // Crouch camera state
    private float currentCrouchOffset;
    private float targetCrouchOffset;

    private Inspect inspectScript;
    private PlayerMovement playerMovement;

    private Vector3 originalPosition;
    private Quaternion hipRotation;
    private bool isInitialized;

    void Awake()
    {
        inspectScript = FindAnyObjectByType<Inspect>();
        playerMovement = FindAnyObjectByType<PlayerMovement>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Initialize the field instead of creating a new local variable to avoid null reference in Update
        gameInput = new GameInput();
        gameInput.Enable();

        originalPosition = transform.localPosition;
        hipRotation = transform.localRotation;
        
        if (camera != null)
        {
            originalCameraLocalPos = camera.localPosition;
        }
        
        isInitialized = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (!isInitialized) return;

        Vector2 lookInput = gameInput.Player.Look.ReadValue<Vector2>();

        if (!inspectScript.isInspecting)
        {
            HandleLookX(lookInput.x);
            HandleLookY(lookInput.y);
            UpdateProceduralRecoil(lookInput);
            UpdateCrouchCamera();
            UpdateHeadBob();
        }
        else
        {

        }
    }

    private void HandleLookX(float lookX)
    {
        mouseX = lookX * mouseSensX * Time.deltaTime;
        
        // Apply horizontal rotation including recoil
        float totalYaw = mouseX + recoilYaw;
        transform.Rotate(Vector3.up * totalYaw);
        playerTransform.Rotate(Vector3.up * totalYaw);
    }

    private void HandleLookY(float lookY)
    {
        mouseY = lookY * mouseSensY * Time.deltaTime;
        
        // Player input counters recoil - pulling down reduces recoil faster
        if (mouseY > 0 && recoilPitch > 0) // Player pulling down while recoil is up
        {
            float counterAmount = mouseY * recoilCounterMultiplier;
            targetRecoilPitch = Mathf.Max(0, targetRecoilPitch - counterAmount);
        }

        // Apply vertical rotation including current recoil offset
        verticalRot -= mouseY;
        verticalRot = Mathf.Clamp(verticalRot - recoilPitch, -85.0f, 85.0f);
        camera.localRotation = Quaternion.Euler(verticalRot, 0f, 0f);
    }

    private void UpdateCrouchCamera()
    {
        if (playerMovement == null) return;

        // Set target crouch offset based on crouch state
        targetCrouchOffset = playerMovement.IsCrouching ? crouchCameraOffset : 0f;

        // Smoothly interpolate to target
        currentCrouchOffset = Mathf.Lerp(currentCrouchOffset, targetCrouchOffset, crouchCameraSpeed * Time.deltaTime);
    }

    private void UpdateHeadBob()
    {
        if (playerMovement == null || camera == null) return;

        bool isMoving = playerMovement.IsMoving && playerMovement.isGrounded;

        if (isMoving)
        {
            // Determine bob parameters based on movement state
            float bobAmountY, bobAmountX, bobSpeed;

            if (playerMovement.IsCrouching)
            {
                bobAmountY = crouchBobAmountY;
                bobAmountX = crouchBobAmountX;
                bobSpeed = crouchBobSpeed;
            }
            else if (playerMovement.IsSprinting)
            {
                bobAmountY = sprintBobAmountY;
                bobAmountX = sprintBobAmountX;
                bobSpeed = sprintBobSpeed;
            }
            else
            {
                bobAmountY = walkBobAmountY;
                bobAmountX = walkBobAmountX;
                bobSpeed = walkBobSpeed;
            }

            // Advance bob timer
            bobTimer += Time.deltaTime * bobSpeed;

            // Calculate bob offset using sine waves
            // Vertical bob uses full sine wave, horizontal uses half frequency for natural sway
            float bobY = Mathf.Sin(bobTimer) * bobAmountY;
            float bobX = Mathf.Sin(bobTimer * 0.5f) * bobAmountX;

            targetBobOffset = new Vector3(bobX, bobY, 0f);
        }
        else
        {
            // Smoothly return to center when not moving
            targetBobOffset = Vector3.zero;
            
            // Gradually reset timer to avoid jarring restart
            bobTimer = Mathf.Lerp(bobTimer, 0f, Time.deltaTime * 2f);
        }

        // Smooth the bob offset
        bobOffset = Vector3.Lerp(bobOffset, targetBobOffset, bobSmoothSpeed * Time.deltaTime);

        // Apply bob + crouch offset to camera position
        Vector3 crouchOffset = new Vector3(0f, currentCrouchOffset, 0f);
        camera.localPosition = originalCameraLocalPos + bobOffset + crouchOffset;
    }

    private void UpdateProceduralRecoil(Vector2 lookInput)
    {
        float dt = Time.deltaTime;

        // Smoothly recover recoil targets toward zero
        targetRecoilPitch = Mathf.MoveTowards(targetRecoilPitch, 0f, recoilRecoverySpeed * dt);
        targetRecoilYaw = Mathf.MoveTowards(targetRecoilYaw, 0f, recoilRecoverySpeed * 2f * dt); // Horizontal recovers faster

        // Smoothly interpolate current recoil toward target
        recoilPitch = Mathf.SmoothDamp(recoilPitch, targetRecoilPitch, ref recoilPitchVel, 0.05f);
        recoilYaw = Mathf.SmoothDamp(recoilYaw, targetRecoilYaw, ref recoilYawVel, 0.03f);

        // Clamp to max values
        recoilPitch = Mathf.Clamp(recoilPitch, 0f, maxVerticalRecoil);
        recoilYaw = Mathf.Clamp(recoilYaw, -maxHorizontalRecoil, maxHorizontalRecoil);
    }
    public void AddRecoil(float pitchAmount, float yawAmount)
    {
        targetRecoilPitch = Mathf.Min(targetRecoilPitch + pitchAmount, maxVerticalRecoil);
        targetRecoilYaw = Mathf.Clamp(targetRecoilYaw + yawAmount, -maxHorizontalRecoil, maxHorizontalRecoil);
    }

    public void ResetRecoil()
    {
        recoilPitch = 0f;
        recoilYaw = 0f;
        targetRecoilPitch = 0f;
        targetRecoilYaw = 0f;
        recoilPitchVel = 0f;
        recoilYawVel = 0f;
    }

    void OnDisable()
    {
        gameInput.Disable();
    }
}

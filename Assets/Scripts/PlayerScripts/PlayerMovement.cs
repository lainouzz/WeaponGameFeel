using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour, ITarget
{
    [Header("Movement")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 8f;
    public float crouchSpeed = 2.5f;
    public float jumpForce = 2f;
    public float gravityScale = -15f;

    [Header("Crouch")]
    public float standingHeight = 2f;
    public float crouchHeight = 1f;
    public float crouchTransitionSpeed = 10f;

    [Header("Footstep Audio")]
    public AudioSource footstepAudioSource;
    [Tooltip("Array of left footstep sounds - will pick randomly")]
    public AudioClip[] leftFootstepClips;
    [Tooltip("Array of right footstep sounds - will pick randomly")]
    public AudioClip[] rightFootstepClips;
    [Tooltip("Time between footsteps when walking")]
    public float walkStepInterval = 0.5f;
    [Tooltip("Multiplier for sprint interval (lower = faster steps)")]
    [Range(0.3f, 1f)]
    public float sprintIntervalMultiplier = 0.6f;
    [Tooltip("Multiplier for crouch interval (higher = slower steps)")]
    [Range(1f, 2f)]
    public float crouchIntervalMultiplier = 1.5f;
    [Tooltip("Volume of footstep sounds")]
    [Range(0f, 1f)]
    public float footstepVolume = 0.7f;
    [Tooltip("Random pitch variation for footsteps (+/-)")]
    [Range(0f, 0.2f)]
    public float footstepPitchVariation = 0.1f;

    [Header("State")]
    public bool isGrounded;
    public bool canJump;
    public bool isSprinting;
    public bool isCrouching;
    public bool canMove = true;

    private Vector3 velocity;
    private Vector3 moveDirection;
    private float currentSpeed;
    private float targetHeight;

    // Footstep state
    private float nextStepTime;
    private bool isLeftFoot = true;
    private int lastLeftIndex = -1;
    private int lastRightIndex = -1;

    public Vector3 MoveDirection => moveDirection;
    public bool IsSprinting => isSprinting;
    public bool IsCrouching => isCrouching;
    public bool IsMoving => moveDirection.magnitude > 0.1f;
    public bool CanMove => canMove;

    public Transform targetTransform => transform;
    public Vector3 position => transform.position;
    public Quaternion rotation => transform.rotation;
    public bool IsAlive => true;

    private GameInput gameInput;
    private CharacterController controller;

    private void Awake()
    {
        gameInput = new GameInput();
        controller = GetComponent<CharacterController>();
    }

    private void OnEnable()
    {
        gameInput.Enable();
        gameInput.Player.Crouch.started += OnCrouchStarted;
        gameInput.Player.Crouch.canceled += OnCrouchCanceled;
    }

    private void OnCrouchStarted(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        if (!canMove) return;
        isCrouching = true;
        isSprinting = false; // Can't sprint while crouching
    }

    private void OnCrouchCanceled(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        isCrouching = false;
    }

    void Update()
    {
        HandleMovement();
        HandleCrouch();
        UpdateFootsteps();

        if (canJump && canMove)
        {
            HandleJump();
        }
    }

    private void HandleMovement()
    {
        // If movement is disabled, only apply gravity
        if (!canMove)
        {
            moveDirection = Vector3.zero;
            isSprinting = false;
            
            // Still apply gravity
            velocity.y += gravityScale * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);

            if (controller.isGrounded && velocity.y < 0)
            {
                isGrounded = true;
                velocity.y = -2f;
            }
            else
            {
                isGrounded = false;
            }
            return;
        }

        // Check sprint input
        bool sprintPressed = gameInput.Player.Sprint.IsPressed();
        isSprinting = sprintPressed && !isCrouching && isGrounded;

        // Determine current speed
        if (isCrouching)
        {
            currentSpeed = crouchSpeed;
        }
        else if (isSprinting)
        {
            currentSpeed = sprintSpeed;
        }
        else
        {
            currentSpeed = walkSpeed;
        }

        Vector2 inputVector = gameInput.Player.Move.ReadValue<Vector2>();
        moveDirection = (transform.right * inputVector.x + transform.forward * inputVector.y).normalized;
        
        controller.Move(moveDirection * currentSpeed * Time.deltaTime);

        velocity.y += gravityScale * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        if (controller.isGrounded && velocity.y < 0)
        {
            isGrounded = true;
            canJump = true;
            velocity.y = -2f; 
        }
        else
        {
            isGrounded = false;
        }
    }

    private void UpdateFootsteps()
    {
        // Need audio setup
        if (footstepAudioSource == null)
            return;

        bool hasLeftClips = leftFootstepClips != null && leftFootstepClips.Length > 0;
        bool hasRightClips = rightFootstepClips != null && rightFootstepClips.Length > 0;

        if (!hasLeftClips && !hasRightClips)
            return;

        // Only play when moving and grounded
        if (!IsMoving || !isGrounded)
        {
            // Reset timer so we get a step immediately when starting to move
            nextStepTime = 0f;
            return;
        }

        // Calculate interval based on movement state
        float interval = walkStepInterval;

        if (isSprinting)
            interval *= sprintIntervalMultiplier;
        else if (isCrouching)
            interval *= crouchIntervalMultiplier;

        // Time to play a footstep?
        if (Time.time >= nextStepTime)
        {
            AudioClip clip = null;

            if (isLeftFoot && hasLeftClips)
            {
                clip = GetRandomClip(leftFootstepClips, ref lastLeftIndex);
            }
            else if (!isLeftFoot && hasRightClips)
            {
                clip = GetRandomClip(rightFootstepClips, ref lastRightIndex);
            }
            else if (hasLeftClips)
            {
                // Fallback: use left clips for both feet if right is empty
                clip = GetRandomClip(leftFootstepClips, ref lastLeftIndex);
            }
            else if (hasRightClips)
            {
                // Fallback: use right clips for both feet if left is empty
                clip = GetRandomClip(rightFootstepClips, ref lastRightIndex);
            }

            if (clip != null)
            {
                // Apply random pitch variation
                float pitch = 1f + Random.Range(-footstepPitchVariation, footstepPitchVariation);
                footstepAudioSource.pitch = pitch;
                footstepAudioSource.PlayOneShot(clip, footstepVolume);
            }

            isLeftFoot = !isLeftFoot;
            nextStepTime = Time.time + interval;
        }
    }

    /// <summary>
    /// Get a random clip from an array, avoiding repeating the same clip twice in a row
    /// </summary>
    private AudioClip GetRandomClip(AudioClip[] clips, ref int lastIndex)
    {
        if (clips == null || clips.Length == 0)
            return null;

        if (clips.Length == 1)
            return clips[0];

        // Pick a random index, but avoid the last one we played
        int index;
        int attempts = 0;
        do
        {
            index = Random.Range(0, clips.Length);
            attempts++;
        }
        while (index == lastIndex && attempts < 10);

        lastIndex = index;
        return clips[index];
    }

    private void HandleCrouch()
    {
        targetHeight = isCrouching ? crouchHeight : standingHeight;
        
        float currentHeight = controller.height;
        if (Mathf.Abs(currentHeight - targetHeight) > 0.01f)
        {
            float newHeight = Mathf.Lerp(currentHeight, targetHeight, crouchTransitionSpeed * Time.deltaTime);
            float heightDifference = newHeight - currentHeight;
            
            controller.height = newHeight;
            
            // Adjust controller center to keep feet on ground
            Vector3 center = controller.center;
            center.y += heightDifference / 2f;
            controller.center = center;
        }
    }

    private void HandleJump()
    {
        if (gameInput.Player.Jump.triggered && isGrounded && canJump && !isCrouching)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravityScale);
            isCrouching = false; // Stand up when jumping
        }
    }

    private void OnDisable()
    {
        gameInput.Player.Crouch.started -= OnCrouchStarted;
        gameInput.Player.Crouch.canceled -= OnCrouchCanceled;
        gameInput.Disable();
    }
}

using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
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

    [Header("State")]
    public bool isGrounded;
    public bool canJump;
    public bool isSprinting;
    public bool isCrouching;

    private Vector3 velocity;
    private Vector3 moveDirection;
    private float currentSpeed;
    private float targetHeight;

    public Vector3 MoveDirection => moveDirection;
    public bool IsSprinting => isSprinting;
    public bool IsCrouching => isCrouching;
    public bool IsMoving => moveDirection.magnitude > 0.1f;

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

        if (canJump)
        {
            HandleJump();
        }
    }

    private void HandleMovement()
    {
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

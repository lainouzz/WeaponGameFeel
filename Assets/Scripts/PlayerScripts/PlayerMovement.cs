using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed;
    public float jumpForce;

    public float gravityScale;

    public bool isGrounded;
    public bool canJump;

    private Vector3 velocity;
    private Vector3 moveDirection;

    public Vector3 MoveDirection => moveDirection;

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
    }


    // Update is called once per frame
    void Update()
    {
        HandleMovement();

        if (canJump)
        {
            HandleJump();
        }
    }

    private void HandleMovement()
    {
        Vector2 inputVector = gameInput.Player.Move.ReadValue<Vector2>();
        moveDirection = (transform.right * inputVector.x + transform.forward * inputVector.y).normalized;
        
        controller.Move(moveDirection * moveSpeed * Time.deltaTime);

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

    private void HandleJump()
    {
        if (gameInput.Player.Jump.triggered && isGrounded && canJump)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravityScale);
            Debug.Log("Jumped");
        }
    }

    private void OnDisable()
    {
        gameInput.Disable();
    }

}

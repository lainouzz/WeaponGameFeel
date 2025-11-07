using UnityEngine;
using UnityEngine.InputSystem;

public class CameraMovement : MonoBehaviour
{
    public Transform camera;

    public GameInput gameInput;

    [Range(1f, 100f)]
    public float mouseSensX;
    [Range(1f, 100f)]
    public float mouseSensY;

    private float verticalRot;
    private float mouseX;
    private float mouseY;

    private Inspect inspectScript;

    void Awake()
    {
        inspectScript = FindAnyObjectByType<Inspect>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Initialize the field instead of creating a new local variable to avoid null reference in Update
        gameInput = new GameInput();
        gameInput.Enable();
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 lookInput = gameInput.Player.Look.ReadValue<Vector2>();

        if (!inspectScript.isInspecting)
        {
            HandleLookX(lookInput.x);
            HandleLookY(lookInput.y);
        }
        else
        {
            // When inspecting, we might want to limit or change the camera movement behavior
            // For now, we will just not update the camera rotation
        }
    }

    private void HandleLookX(float lookX)
    {
        mouseX = lookX * mouseSensX * Time.deltaTime;
        transform.Rotate(Vector3.up * mouseX);
    }

    private void HandleLookY(float lookY)
    {
        mouseY = lookY * mouseSensY * Time.deltaTime;
        verticalRot -= mouseY;
        // Clamp should have min first then max
        verticalRot = Mathf.Clamp(verticalRot, -85.0f, 85.0f);
        camera.localRotation = Quaternion.Euler(verticalRot, 0f, 0f);
    }
}

using UnityEngine;
using UnityEngine.InputSystem;

public class Inspect : MonoBehaviour
{
    public float rotationSpeed = 120f;
    public float maxPitch = 30f;
    public float maxRoll = 30f;
    public float returnSpeed = 8f; // speed to return to original rotation
    public Transform weaponPivot;

    private Quaternion originalRotation;
    private float pitch; // around X
    private float roll;  // around Z

    private GameInput gameInput;
    public bool isInspecting;

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

    void OnDisable()
    {
        gameInput.Player.Inspect.started -= OnInspectStarted;
        gameInput.Player.Inspect.canceled -= OnInspectCanceled;
        gameInput.Disable();
    }

    void Start()
    {
        if (weaponPivot != null)
            originalRotation = weaponPivot.localRotation;
        else
            originalRotation = transform.localRotation;
    }

    void Update()
    {
        if (isInspecting)
        {
            OnInspect();
        }
        else
        {
            pitch = Mathf.MoveTowards(pitch, 0f, returnSpeed * Time.deltaTime);
            roll = Mathf.MoveTowards(roll, 0f, returnSpeed * Time.deltaTime);

            Quaternion offset = Quaternion.Euler(pitch, 0f, roll);
            Quaternion target = originalRotation * offset;

            if (weaponPivot != null)
                weaponPivot.localRotation = Quaternion.Slerp(weaponPivot.localRotation, target, returnSpeed * Time.deltaTime);
            else
                transform.localRotation = Quaternion.Slerp(transform.localRotation, target, returnSpeed * Time.deltaTime);
        }
    }

    private void OnInspectStarted(InputAction.CallbackContext ctx)
    {
        isInspecting = true;
    }

    private void OnInspectCanceled(InputAction.CallbackContext ctx)
    {
        isInspecting = false;
    }

    public void OnInspect()
    {
        Vector2 lookInput = gameInput.Player.Look.ReadValue<Vector2>();

        roll = Mathf.Clamp(roll - lookInput.x * rotationSpeed * Time.deltaTime, -maxRoll, maxRoll);
        pitch = Mathf.Clamp(pitch - lookInput.y * rotationSpeed * Time.deltaTime, -maxPitch, maxPitch);

        Quaternion offset = Quaternion.Euler(pitch, 0f, roll);
        if (weaponPivot != null)
            weaponPivot.localRotation = originalRotation * offset;
        else
            transform.localRotation = originalRotation * offset;
    }
}

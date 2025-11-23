using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class Weapon : MonoBehaviour
{
    public Transform firePoint;

    public float swapMultiplier;
    public float smoothSwayAmount;
    public float rollAmount;

    //fire config
    public bool isAutomatic;
    public float fireRate;

    //recoil/inertia config
    public float recoilImpulePitch;
    public float recoilImpuleYaw;
    public float recoilImpuleRoll;
    public float recoilSpring;
    public float recoilDamping;
    public float recoilKickImpule;
    public float recoilKickSpring;
    public float recoilKickDamping;

    private Inspect Inspect;
    private GameInput gameInput;
    private PlayerMovement playerMovement;

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

    private float nextFireTime;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        Inspect = FindAnyObjectByType<Inspect>();
        playerMovement = GetComponentInParent<PlayerMovement>();
        gameInput = new GameInput();

        originalPosition = transform.localPosition;
    }

    void OnEnable()
    {
        gameInput.Enable();
    }

    // Update is called once per frame
    void Update()
    {
        HandleFireInput();

        if (!Inspect.isInspecting)
        {
            RecoilAndSway();
        }
        else
        {
            UpdateRecoil(Time.deltaTime);
            UpdateRecoilKick(Time.deltaTime);
            ApplyRecoilKickPos();
        }
    }

    private void HandleFireInput()
    {
        bool pressed = Mouse.current != null && Mouse.current.leftButton.isPressed;
        bool pressOnce = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;

        bool shouldWeFire = isAutomatic ? pressed : pressOnce;

        // enforce fire rate using nextFireTime
        if (shouldWeFire && Time.time >= nextFireTime)
        {
            FireOnce();
            nextFireTime = Time.time + (fireRate > 0 ? 1f / fireRate : 0f);
        }
    }

    private void FireOnce()
    {
        recoilPitchVel += recoilImpulePitch;
        recoilYawVel += UnityEngine.Random.Range(-recoilImpuleYaw, recoilImpuleYaw);
        recoilRollVel += UnityEngine.Random.Range(-recoilImpuleRoll, recoilImpuleRoll);

        recoilKickVel += recoilKickImpule;

        Debug.DrawRay(firePoint.position, transform.forward * 5, Color.red, 1f);
    }

    public void RecoilAndSway()
    {
        Vector2 mouse = gameInput.Player.Look.ReadValue<Vector2>() * swapMultiplier;
        mouse.y = Mathf.Clamp(mouse.y, -10f, 10f);
        mouse.x = Mathf.Clamp(mouse.x, -10f, 10f);

        Quaternion rotX = Quaternion.AngleAxis(-mouse.y, Vector3.right);
        Quaternion rotY = Quaternion.AngleAxis(mouse.x, Vector3.up);
        Quaternion swayRot = rotX * rotY;

        Vector2 moveInput = gameInput.Player.Move.ReadValue<Vector2>();
        Quaternion rollX = Quaternion.AngleAxis(moveInput.x * rollAmount, Vector3.forward);
        rollX = Mathf.Abs(moveInput.x) > 0.1f ? rollX : Quaternion.Slerp(rollX, Quaternion.identity, Time.deltaTime * 5f);

        UpdateRecoil(Time.deltaTime);
        Quaternion recoilRot = Quaternion.Euler(recoilPitch, recoilYaw, recoilRoll);

        Quaternion finalRot = swayRot * recoilRot * rollX;
        // slerp toward the combined sway + recoil rotation, not just sway
        transform.localRotation = Quaternion.Slerp(transform.localRotation, finalRot, smoothSwayAmount * Time.deltaTime);

        UpdateRecoilKick(Time.deltaTime);
        ApplyRecoilKickPos();
    }

    private void ApplyRecoilKickPos()
    {
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

    private void OnDisable()
    {
        gameInput.Disable();
    }
}

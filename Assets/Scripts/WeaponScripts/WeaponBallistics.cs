using UnityEngine;

// WeaponBallistics
// Move here from `Weapon.cs`:
// - `PerformRaycast()`
// - `ApplyImpactForce(RaycastHit hit, Vector3 direction)`
// - `CalculateTotalSpread()`
// - `ApplySpread(Vector3 direction, float spreadAngle)`
// - `UpdateSpreadRecovery()`
// - raycast/damage fields: `firePoint`, `statsManager`, `maxRange`, `damage`, `hitLayers`, `impactEffectPrefab`, `impactForce`, `impactForceMode`
// - spread fields: `hipfireSpread`, `adsSpread`, `walkSpreadBonus`, `sprintSpreadBonus`, `airSpreadBonus`, `crouchSpreadBonus`, `spreadPerShot`, `spreadRecoveryRate`, `maxSustainedSpread`
// - spread state: `currentSustainedSpread`

public class WeaponBallistics : MonoBehaviour
{
    [Header("Ballistics References")]
    public Transform firePoint;
    public StatsManager statsManager;
    public Camera mainCamera;
    public PlayerMovement playerMovement;
    public WeaponRecoilController recoilController;

    [Header("Raycast Shooting")]
    [Tooltip("Maximum distance the raycast can travel")]
    public float maxRange = 100f;
    [Tooltip("Damage dealt per shot")]
    public float damage = 25f;
    [Tooltip("Layers the raycast can hit")]
    public LayerMask hitLayers = ~0;
    [Tooltip("Impact effect prefab (optional)")]
    public GameObject impactEffectPrefab;
    [Tooltip("Force applied to hit objects with Rigidbody")]
    public float impactForce = 500f;
    [Tooltip("How the force is applied (Impulse = instant, Force = continuous)")]
    public ForceMode impactForceMode = ForceMode.Impulse;

    [Header("Bullet Spread")]
    [Tooltip("Base spread when hipfiring (degrees)")]
    public float hipfireSpread = 3f;
    [Tooltip("Spread when fully aimed (degrees)")]
    public float adsSpread = 0.1f;
    [Tooltip("Additional spread when walking")]
    public float walkSpreadBonus = 1f;
    [Tooltip("Additional spread when sprinting")]
    public float sprintSpreadBonus = 3f;
    [Tooltip("Additional spread when in air")]
    public float airSpreadBonus = 2f;
    [Tooltip("Additional spread when crouching (can be negative for bonus accuracy)")]
    public float crouchSpreadBonus = -0.5f;
    [Tooltip("Spread increase per shot (for sustained fire)")]
    public float spreadPerShot = 0.2f;
    [Tooltip("How fast spread recovers (degrees per second)")]
    public float spreadRecoveryRate = 5f;
    [Tooltip("Max additional spread from sustained fire")]
    public float maxSustainedSpread = 2f;

    [Header("Spread State")]
    public float currentSustainedSpread;

    public void PerformRaycast()
    {
        Ray ray;
        if (mainCamera != null)
        {
            ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        }
        else
        {
            ray = new Ray(firePoint.position, firePoint.forward);
        }

        float totalSpread = CalculateTotalSpread();
        Vector3 spreadDirection = ApplySpread(ray.direction, totalSpread);
        ray = new Ray(ray.origin, spreadDirection);

        currentSustainedSpread = Mathf.Min(currentSustainedSpread + spreadPerShot, maxSustainedSpread);

        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, maxRange, hitLayers))
        {
            Debug.DrawLine(ray.origin, hit.point, Color.red, 1f);

            if (impactEffectPrefab != null)
            {
                GameObject impact = Instantiate(impactEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(impact, 2f);
            }

            ApplyImpactForce(hit, ray.direction);

            if (hit.collider.TryGetComponent<IDamagable>(out var damagable))
            {
                if (statsManager == null)
                {
                    Debug.LogError("statsManager reference is NULL in WeaponBallistics!");
                    return;
                }

                float finalDmg = statsManager.GetFinalDamage(damage);
                damagable.TakeDamage(finalDmg);
            }
        }
        else
        {
            Debug.DrawRay(ray.origin, ray.direction * maxRange, Color.yellow, 1f);
        }
    }

    public void ApplyImpactForce(RaycastHit hit, Vector3 direction)
    {
        if (impactForce <= 0f) return;

        Rigidbody rb = hit.collider.attachedRigidbody;
        if (rb != null && !rb.isKinematic)
        {
            rb.AddForceAtPosition(direction.normalized * impactForce, hit.point, impactForceMode);
        }
    }

    public float CalculateTotalSpread()
    {
        float aimLerpT = recoilController != null ? recoilController.aimLerpT : 0f;
        float baseSpread = Mathf.Lerp(hipfireSpread, adsSpread, aimLerpT);
        float movementSpread = 0f;

        if (playerMovement != null)
        {
            if (!playerMovement.isGrounded)
            {
                movementSpread += airSpreadBonus;
            }
            else
            {
                if (playerMovement.IsCrouching)
                {
                    movementSpread += crouchSpreadBonus;
                }

                if (playerMovement.IsMoving)
                {
                    movementSpread += playerMovement.IsSprinting ? sprintSpreadBonus : walkSpreadBonus;
                }
            }
        }

        return Mathf.Max(0f, baseSpread + movementSpread + currentSustainedSpread);
    }

    public Vector3 ApplySpread(Vector3 direction, float spreadAngle)
    {
        if (spreadAngle <= 0f) return direction;

        float spreadRad = spreadAngle * Mathf.Deg2Rad;
        float randomAngle = Random.Range(0f, 2f * Mathf.PI);
        float randomRadius = Mathf.Sqrt(Random.Range(0f, 1f)) * Mathf.Tan(spreadRad);

        Vector3 right = Vector3.Cross(direction, Vector3.up).normalized;
        if (right.magnitude < 0.001f)
        {
            right = Vector3.Cross(direction, Vector3.forward).normalized;
        }
        Vector3 up = Vector3.Cross(right, direction).normalized;

        Vector3 spreadOffset = right * (Mathf.Cos(randomAngle) * randomRadius) + up * (Mathf.Sin(randomAngle) * randomRadius);
        return (direction + spreadOffset).normalized;
    }

    public void UpdateSpreadRecovery()
    {
        if (currentSustainedSpread > 0f)
        {
            currentSustainedSpread = Mathf.Max(0f, currentSustainedSpread - spreadRecoveryRate * Time.deltaTime);
        }
    }
}

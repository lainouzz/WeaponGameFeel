using UnityEngine;

public class HitZone : MonoBehaviour
{
    public enum ZoneType { Head, Torso, Limbs }

    public ZoneType zoneType;
    public float damageMultiplier = 1f;

    // Called directly by WeaponBallistics when a raycast hits this collider
    public void ProcessHit(float baseDamage, StatsManager statsManager)
    {
        float finalDamage = baseDamage * damageMultiplier;
        if (statsManager != null)
        {
            finalDamage = statsManager.GetFinalDamage(finalDamage);
        }

        IDamagable damagable = GetComponentInParent<IDamagable>();
        if (damagable != null)
        {
            damagable.TakeDamage(finalDamage);
        }
        else
        {
            Debug.LogWarning($"HitZone '{name}' ({zoneType}) found no IDamagable in parent hierarchy.");
        }
    }
}

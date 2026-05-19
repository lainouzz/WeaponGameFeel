using UnityEngine;

// WeaponMagazineVisuals
// Move here from `Weapon.cs`:
// - `DropMagazine()`
// - `HideCurrentMagazine()`
// - `ShowCurrentMagazine()`
// - magazine visual fields: `droppedMagazinePrefab`, `currentMagazineObject`, `magazineDropPoint`, `magazineDropForce`, `magazineLifetime`, `magazineDropTimePercent`, `magazineInsertTimePercent`
// - any future reload-animation visual helpers

public class WeaponMagazineVisuals : MonoBehaviour
{
    [Header("Magazine Visual References")]
    public GameObject droppedMagazinePrefab;
    public GameObject currentMagazineObject;
    public Transform magazineDropPoint;

    [Header("Magazine Visual Settings")]
    public float magazineDropForce = 2f;
    public float magazineLifetime = 10f;
    [Range(0f, 1f)] public float magazineDropTimePercent = 0.3f;
    [Range(0f, 1f)] public float magazineInsertTimePercent = 0.7f;

    public void DropMagazine()
    {
        HideCurrentMagazine();

        if (droppedMagazinePrefab == null) return;

        Transform dropPoint = magazineDropPoint != null ? magazineDropPoint : transform;
        GameObject droppedMag = Instantiate(droppedMagazinePrefab, dropPoint.position, dropPoint.rotation);

        Rigidbody rb = droppedMag.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = droppedMag.AddComponent<Rigidbody>();
        }

        Vector3 ejectDirection = (-transform.right + -transform.up * 0.5f).normalized;
        rb.AddForce(ejectDirection * magazineDropForce, ForceMode.Impulse);
        rb.AddTorque(Random.insideUnitSphere * magazineDropForce * 0.5f, ForceMode.Impulse);

        MeshCollider mc = droppedMag.GetComponent<MeshCollider>();
        if (mc == null)
        {
            mc = droppedMag.AddComponent<MeshCollider>();
            mc.convex = true;
        }

        Destroy(droppedMag, magazineLifetime);
    }

    public void HideCurrentMagazine()
    {
        if (currentMagazineObject != null)
        {
            currentMagazineObject.SetActive(false);
        }
    }

    public void ShowCurrentMagazine()
    {
        if (currentMagazineObject != null)
        {
            currentMagazineObject.SetActive(true);
        }
    }
}

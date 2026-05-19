using UnityEngine;
using TMPro;

// WeaponVisuals
// Optional umbrella presentation class.
// If you prefer one visual layer, move here from `Weapon.cs`:
// - muzzle flash helpers: `StartAutoMuzzle()`, `StopAutoMuzzle()`, `PlaySingleMuzzle()`, `StopMuzzleFlash()`
// - visual state fields: `particleSystem`, `isAutoMuzzlePlaying`
// - presentation callbacks: `OnDrawStart()`, `OnDrawFinish()`, `OnReloadStart()`, `OnReloadFinish()`, `OnReloadCancel()`
// - any future UI/FX-only weapon behavior

public class WeaponVisuals : MonoBehaviour
{
    [Header("Visual References")]
    public ParticleSystem particleSystem;
    public TMP_Text ammoText;

    [Header("Visual State")]
    public bool isAutoMuzzlePlaying;

    public void UpdateAmmoText(int currentAmmo, int reserveAmmo)
    {
        if (ammoText != null)
        {
            ammoText.text = $"{currentAmmo} / {reserveAmmo}";
        }
    }

    public void StartAutoMuzzle()
    {
        if (particleSystem == null) return;

        var main = particleSystem.main;
        main.loop = true;
        particleSystem.Play();
        isAutoMuzzlePlaying = true;
    }

    public void StopAutoMuzzle()
    {
        if (particleSystem == null) return;

        particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        isAutoMuzzlePlaying = false;
    }

    public void PlaySingleMuzzle()
    {
        if (particleSystem == null) return;

        var main = particleSystem.main;
        main.loop = false;
        particleSystem.Play();
    }

    public void StopMuzzleFlash()
    {
        if (isAutoMuzzlePlaying)
        {
            StopAutoMuzzle();
        }
    }

    public void OnDrawStart() { }
    public void OnDrawFinish() { }
    public void OnReloadStart() { }
    public void OnReloadFinish() { }
    public void OnReloadCancel() { }
}

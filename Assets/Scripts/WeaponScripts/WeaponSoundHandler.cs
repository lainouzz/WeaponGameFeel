using UnityEngine;

/// <summary>
/// Handles all weapon sound effects, integrating with the weapon state machine.
/// Attach this component to the same GameObject as the Weapon component.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class WeaponSoundHandler : MonoBehaviour
{
    [Header("Audio Sources")]
    [Tooltip("Main audio source for one-shot sounds (fire, reload, etc.)")]
    [SerializeField] private AudioSource mainAudioSource;
    [Tooltip("Looping audio source for automatic fire (optional, will create if needed)")]
    [SerializeField] private AudioSource loopAudioSource;

    [Header("Fire Sounds")]
    [Tooltip("Sound played for each shot (semi-auto) or start of burst")]
    public AudioClip fireSoundSingle;
    [Tooltip("Looping sound for automatic fire (optional - uses fireSoundSingle if not set)")]
    public AudioClip fireSoundLoop;
    [Tooltip("Sound for when firing with empty magazine (click)")]
    public AudioClip dryFireSound;
    [Tooltip("Tail/echo sound played after firing stops")]
    public AudioClip fireTailSound;

    [Header("Fire Sound Settings")]
    [Range(0f, 1f)]
    public float fireVolume = 1f;
    [Tooltip("Random pitch variation (+/-)")]
    [Range(0f, 0.3f)]
    public float firePitchVariation = 0.05f;

    [Header("Reload Sounds - Empty Reload (0 bullets)")]
    [Tooltip("Full reload sound when magazine is empty (includes charging handle)")]
    public AudioClip reloadEmptySound;
    [Tooltip("Or use separate sounds for each phase:")]
    public AudioClip magazineOutSound;
    public AudioClip magazineInSound;
    public AudioClip chargingHandleSound;

    [Header("Reload Sounds - Tactical Reload (1+ bullets)")]
    [Tooltip("Reload sound when magazine still has bullets (no charging handle)")]
    public AudioClip reloadTacticalSound;

    [Header("Reload Sound Timing")]
    [Tooltip("When to play magazine out sound (0-1, percentage of reload)")]
    [Range(0f, 1f)]
    public float magazineOutTimePercent = 0.1f;
    [Tooltip("When to play magazine in sound (0-1, percentage of reload)")]
    [Range(0f, 1f)]
    public float magazineInTimePercent = 0.5f;
    [Tooltip("When to play charging handle sound (0-1, percentage of reload)")]
    [Range(0f, 1f)]
    public float chargingHandleTimePercent = 0.8f;

    [Header("Reload Sound Settings")]
    [Range(0f, 1f)]
    public float reloadVolume = 1f;

    [Header("Other Sounds")]
    public AudioClip drawSound;
    public AudioClip holsterSound;
    public AudioClip aimInSound;
    public AudioClip aimOutSound;

    [Header("Other Sound Settings")]
    [Range(0f, 1f)]
    public float otherVolume = 0.8f;

    // Component references
    private Weapon weapon;
    private bool wasAiming;
    
    // Reload state tracking
    private bool isReloading;
    private bool wasEmptyReload;
    private float reloadStartTime;
    private float reloadDuration;
    private bool playedMagOut;
    private bool playedMagIn;
    private bool playedChargingHandle;

    // Fire state tracking
    private bool isFiring;

    void Awake()
    {
        weapon = GetComponent<Weapon>();
        
        // Get or create main audio source
        if (mainAudioSource == null)
        {
            mainAudioSource = GetComponent<AudioSource>();
            if (mainAudioSource == null)
            {
                mainAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        
        // Configure main audio source
        mainAudioSource.playOnAwake = false;
        mainAudioSource.spatialBlend = 0f; // 3D sound

        // Create loop audio source if needed for automatic weapons
        if (loopAudioSource == null && weapon != null && weapon.IsAutomatic)
        {
            loopAudioSource = gameObject.AddComponent<AudioSource>();
            loopAudioSource.playOnAwake = false;
            loopAudioSource.loop = true;
            loopAudioSource.spatialBlend = 0f;
        }
    }

    void Start()
    {
        if (weapon != null)
        {
            // Subscribe to weapon events
            weapon.OnWeaponStateChanged += HandleWeaponStateChanged;
        }
    }

    void Update()
    {
        // Handle reload sound timing
        if (isReloading)
        {
            UpdateReloadSounds();
        }

        // Handle aim sounds
        UpdateAimSounds();
    }

    private void HandleWeaponStateChanged(WeaponStateType from, WeaponStateType to)
    {
        // Handle firing sounds
        if (to == WeaponStateType.Firing)
        {
            OnStartFiring();
        }
        else if (from == WeaponStateType.Firing)
        {
            OnStopFiring();
        }

        // Handle reload sounds
        if (to == WeaponStateType.Reloading)
        {
            OnStartReload();
        }
        else if (from == WeaponStateType.Reloading)
        {
            OnEndReload();
        }

        // Handle draw/holster sounds
        if (to == WeaponStateType.Drawing)
        {
            PlaySound(drawSound, otherVolume);
        }
        else if (to == WeaponStateType.Holstering)
        {
            PlaySound(holsterSound, otherVolume);
        }
    }

    #region Fire Sounds

    private void OnStartFiring()
    {
        isFiring = true;

        if (weapon.IsAutomatic && fireSoundLoop != null)
        {
            // Start looping fire sound
            if (loopAudioSource != null)
            {
                loopAudioSource.clip = fireSoundLoop;
                loopAudioSource.volume = fireVolume;
                loopAudioSource.pitch = 1f + Random.Range(-firePitchVariation, firePitchVariation);
                loopAudioSource.Play();
            }
        }
    }

    private void OnStopFiring()
    {
        isFiring = false;

        // Stop loop sound
        if (loopAudioSource != null && loopAudioSource.isPlaying)
        {
            loopAudioSource.Stop();
        }

        // Play tail sound
        if (fireTailSound != null)
        {
            PlaySound(fireTailSound, fireVolume * 0.7f);
        }
    }

    /// <summary>
    /// Called by Weapon when a shot is fired
    /// </summary>
    public void PlayFireSound()
    {
        // For automatic weapons with loop sound, the loop handles it
        if (weapon.IsAutomatic && fireSoundLoop != null && loopAudioSource != null && loopAudioSource.isPlaying)
        {
            return;
        }

        // Play single shot sound
        if (fireSoundSingle != null)
        {
            float pitch = 1f + Random.Range(-firePitchVariation, firePitchVariation);
            PlaySoundWithPitch(fireSoundSingle, fireVolume, pitch);
        }
    }

    /// <summary>
    /// Play dry fire sound (clicking on empty magazine)
    /// </summary>
    public void PlayDryFireSound()
    {
        if (dryFireSound != null)
        {
            PlaySound(dryFireSound, fireVolume * 0.8f);
        }
    }

    #endregion

    #region Reload Sounds

    private void OnStartReload()
    {
        isReloading = true;
        reloadStartTime = Time.time;
        reloadDuration = weapon.ReloadTime;
        wasEmptyReload = weapon.CurrentAmmo == 0;
        
        // Reset timing flags
        playedMagOut = false;
        playedMagIn = false;
        playedChargingHandle = false;

        // If using single reload sounds, play immediately
        if (wasEmptyReload && reloadEmptySound != null && magazineOutSound == null)
        {
            PlaySound(reloadEmptySound, reloadVolume);
        }
        else if (!wasEmptyReload && reloadTacticalSound != null && magazineOutSound == null)
        {
            PlaySound(reloadTacticalSound, reloadVolume);
        }
    }

    private void UpdateReloadSounds()
    {
        float elapsed = Time.time - reloadStartTime;
        float progress = elapsed / reloadDuration;

        // Play magazine out sound
        if (!playedMagOut && progress >= magazineOutTimePercent && magazineOutSound != null)
        {
            PlaySound(magazineOutSound, reloadVolume);
            playedMagOut = true;
        }

        // Play magazine in sound
        if (!playedMagIn && progress >= magazineInTimePercent && magazineInSound != null)
        {
            PlaySound(magazineInSound, reloadVolume);
            playedMagIn = true;
        }

        // Play charging handle sound (only for empty reloads)
        if (!playedChargingHandle && wasEmptyReload && progress >= chargingHandleTimePercent && chargingHandleSound != null)
        {
            PlaySound(chargingHandleSound, reloadVolume);
            playedChargingHandle = true;
        }
    }

    private void OnEndReload()
    {
        isReloading = false;
    }

    #endregion

    #region Aim Sounds

    private void UpdateAimSounds()
    {
        if (weapon == null) return;

        bool currentlyAiming = weapon.IsAiming;

        if (currentlyAiming && !wasAiming)
        {
            // Started aiming
            if (aimInSound != null)
            {
                PlaySound(aimInSound, otherVolume);
            }
        }
        else if (!currentlyAiming && wasAiming)
        {
            // Stopped aiming
            if (aimOutSound != null)
            {
                PlaySound(aimOutSound, otherVolume);
            }
        }

        wasAiming = currentlyAiming;
    }

    #endregion

    #region Utility Methods

    private void PlaySound(AudioClip clip, float volume)
    {
        if (clip == null || mainAudioSource == null) return;
        mainAudioSource.PlayOneShot(clip, volume);
    }

    private void PlaySoundWithPitch(AudioClip clip, float volume, float pitch)
    {
        if (clip == null || mainAudioSource == null) return;
        
        // PlayOneShot doesn't support pitch, so we need to use Play()
        // Create a temporary approach or just use PlayOneShot with slight workaround
        mainAudioSource.pitch = pitch;
        mainAudioSource.PlayOneShot(clip, volume);
        mainAudioSource.pitch = 1f; // Reset immediately (won't affect PlayOneShot)
    }

    /// <summary>
    /// Stop all currently playing sounds
    /// </summary>
    public void StopAllSounds()
    {
        if (mainAudioSource != null)
        {
            mainAudioSource.Stop();
        }
        if (loopAudioSource != null)
        {
            loopAudioSource.Stop();
        }
    }

    #endregion

    void OnDisable()
    {
        StopAllSounds();
    }

    void OnDestroy()
    {
        if (weapon != null)
        {
            weapon.OnWeaponStateChanged -= HandleWeaponStateChanged;
        }
    }
}

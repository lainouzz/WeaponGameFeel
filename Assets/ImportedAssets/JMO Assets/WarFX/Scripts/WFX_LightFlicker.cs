using UnityEngine;
using System.Collections;

/**
 *	Muzzle flash light flicker.
 *	Supports single-fire (one burst) and full-auto (continuous) modes.
 *	
 *	(c) 2015, Jean Moreno
**/

[RequireComponent(typeof(Light))]
public class WFX_LightFlicker : MonoBehaviour
{
	[Tooltip("Interval between each on/off toggle")]
	public float flickerInterval = 0.02f;
	[Tooltip("How long the light stays on for a single shot (seconds)")]
	public float singleShotDuration = 0.07f;

	private Light muzzleLight;
	private Coroutine activeCoroutine;

	private void Awake()
	{
		muzzleLight = GetComponent<Light>();
		muzzleLight.enabled = false;
	}

	// Call once per single shot
	public void PlaySingle()
	{
		StopActive();
		activeCoroutine = StartCoroutine(SingleFlicker());
	}

	// Call when full-auto fire starts
	public void StartAuto()
	{
		if (activeCoroutine != null) return;
		activeCoroutine = StartCoroutine(AutoFlicker());
	}

	// Call when full-auto fire stops
	public void StopAuto()
	{
		StopActive();
		muzzleLight.enabled = false;
	}

	private void StopActive()
	{
		if (activeCoroutine != null)
		{
			StopCoroutine(activeCoroutine);
			activeCoroutine = null;
		}
	}

	private IEnumerator SingleFlicker()
	{
		float elapsed = 0f;
		while (elapsed < singleShotDuration)
		{
			muzzleLight.enabled = !muzzleLight.enabled;
			elapsed += flickerInterval;
			yield return new WaitForSeconds(flickerInterval);
		}
		muzzleLight.enabled = false;
		activeCoroutine = null;
	}

	private IEnumerator AutoFlicker()
	{
		while (true)
		{
			muzzleLight.enabled = !muzzleLight.enabled;
			yield return new WaitForSeconds(flickerInterval);
		}
	}
}

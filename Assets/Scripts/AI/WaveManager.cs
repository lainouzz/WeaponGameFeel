using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Manages escalating enemy waves. After a configurable number of waves,
/// extraction is unlocked. Drives Spawnbehavior to spawn enemies each wave.
/// </summary>
public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    public WaveUI waveUI;

    [Header("Wave Settings")]
    [SerializeField] private int baseEnemiesPerWave = 5;
    [SerializeField] private int enemiesScalingPerWave = 2;
    [SerializeField] private int wavesBeforeExtraction = 3;
    [SerializeField] private float timeBetweenWaves = 5f;

    public int CurrentWave { get; private set; } = 0;
    public int EnemiesRemaining { get; private set; } = 0;
    public bool IsExtractionUnlocked { get; private set; } = false;
    public bool IsWaveActive { get; private set; } = false;

    // UI events
    public event Action<int> OnWaveStarted;
    public event Action<int> OnWaveCompleted;
    public event Action OnExtractionUnlocked;
    public event Action<int> OnEnemiesRemainingChanged;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        StartCoroutine(StartWaveAfterDelay(1f));
    }

    private IEnumerator StartWaveAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartNextWave();
    }

    public void StartNextWave()
    {
        if (IsExtractionUnlocked) return;

        CurrentWave++;
        int enemyCount = baseEnemiesPerWave + (CurrentWave - 1) * enemiesScalingPerWave;
        EnemiesRemaining = enemyCount;
        IsWaveActive = true;

        OnWaveStarted?.Invoke(CurrentWave);
        OnEnemiesRemainingChanged?.Invoke(EnemiesRemaining);

        waveUI.WaveText.text = $"Wave {CurrentWave}";
        waveUI.EnemiesLeftText.text = $"Enemies Left: {EnemiesRemaining}";

        StartCoroutine(SpawnWaveEnemies(enemyCount));
    }

    private IEnumerator SpawnWaveEnemies(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (Spawnbehavior.Instance != null)
                Spawnbehavior.Instance.SpawnEnemy();

            yield return new WaitForSeconds(0.5f);
        }
    }

    /// <summary>Called by EnemyBehavior when an enemy dies.</summary>
    public void OnEnemyKilled()
    {
        if (!IsWaveActive) return;

        EnemiesRemaining = Mathf.Max(0, EnemiesRemaining - 1);
        OnEnemiesRemainingChanged?.Invoke(EnemiesRemaining);

        if (EnemiesRemaining <= 0)
            StartCoroutine(HandleWaveComplete());
    }

    private IEnumerator HandleWaveComplete()
    {
        IsWaveActive = false;
        OnWaveCompleted?.Invoke(CurrentWave);
        waveUI.WaveText.text = $"Wave {CurrentWave} Complete!";
        waveUI.EnemiesLeftText.text = $"Enemies Left: {EnemiesRemaining}";

        if (CurrentWave >= wavesBeforeExtraction)
        {
            IsExtractionUnlocked = true;
            OnExtractionUnlocked?.Invoke();
        }
        else
        {
            yield return new WaitForSeconds(timeBetweenWaves);
            StartNextWave();
        }
    }
}

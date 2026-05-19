using UnityEngine;
using TMPro;

public class WaveUI : MonoBehaviour
{
    public static WaveUI Instance { get; private set; }

    public TMP_Text WaveText;
    public TMP_Text EnemiesLeftText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }

    public void UpdateEnemiesLeft(int enemiesLeft)
    {
        EnemiesLeftText.text = $"Enemies Left: {enemiesLeft}";
    }
}

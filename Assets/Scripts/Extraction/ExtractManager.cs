using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class ExtractManager : MonoBehaviour
{
    public bool isExtracting = false;

    [SerializeField] private float extractTimer = 10f; // default length
    [SerializeField] private float defaultExtractTime = 10f;

    public GameObject extractUI;
    public TMP_Text extractTimerText;
    public ExtractionZone currentExtractionZone;
    public static ExtractManager instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        // Start extraction when entering zone
        if (currentExtractionZone != null && currentExtractionZone.isInExtractZone && !isExtracting)
        {
            StartExtractTimer();
        }

        // Count down while extracting
        if (isExtracting)
        {
            extractTimer -= Time.deltaTime;

            if (extractTimerText != null)
            {
                extractTimerText.text = extractTimer.ToString("F2");
            }

            if (extractTimer <= 0f)
            {
                ExtractPlayer();
                StopExtractTimer();
            }

            // If player leaves zone, stop/cancel extraction
            if (currentExtractionZone != null && !currentExtractionZone.isInExtractZone)
            {
                StopExtractTimer();
            }
        }
    }

    public void StartExtractTimer()
    {
        isExtracting = true;
        extractTimer = defaultExtractTime;

        if (extractUI != null)
        {
            extractUI.SetActive(true);
        }
    }

    public void StopExtractTimer()
    {
        isExtracting = false;
        extractTimer = defaultExtractTime;

        if (extractUI != null)
        {
            extractUI.SetActive(false);
        }

        if (extractTimerText != null)
        {
            extractTimerText.text = string.Empty;
        }
    }

    private void ExtractPlayer()
    {
        if (currentExtractionZone != null && currentExtractionZone.isInExtractZone)
        {
            SceneManager.LoadScene("MainMenu");
            PlayerLoadSaveManager.instance.SaveInventory();
        }
    }
}

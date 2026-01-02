using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class ExtractManager : MonoBehaviour
{
    public bool isExtracting = false;

    [SerializeField] private float extractTimer = 10f; // default length

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
        if (currentExtractionZone.isInExtractZone && !isExtracting)
        {
            StartExtractTimer();
        }

        // Count down while extracting
        if (isExtracting)
        {
            extractTimer -= Time.deltaTime;
            extractTimerText.text = extractTimer.ToString("F2");
            Debug.Log("Extracting in: " + extractTimer);

            if (extractTimer <= 0f)
            {
                isExtracting = false;
                ExtractPlayer();
            }

            // Optional: if player leaves zone, cancel extraction
            if (!currentExtractionZone.isInExtractZone)
            {
                isExtracting = false;
                // Optionally reset timer
                //extractTimer = 10f;
            }
        }
    }

    public void StartExtractTimer()
    {
        isExtracting = true;
        // Optionally set initial time here
        extractTimer = 10f;
    }

    private void ExtractPlayer()
    {
        if (currentExtractionZone.isInExtractZone)
        {
            Debug.Log("EXTRACTED");
            SceneManager.LoadScene("MainMenu");
        }
    }
}

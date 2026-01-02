using UnityEngine;

public class ExtractionZone : MonoBehaviour
{
    public bool isInExtractZone = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isInExtractZone = true;
            //ExtractManager.instance.StartExtractTimer(extractZoneTimer);
            Debug.Log("Player has entered the extraction zone.");
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isInExtractZone = false;
            Debug.Log("Player has exited the extraction zone.");
        }
    }
}

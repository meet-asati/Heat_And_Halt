using UnityEngine;
using System.Collections;

public class FlickeringLight : MonoBehaviour
{
    private Light myLight;

    [Header("Flicker Settings")]
    public float minIntensity = 2f;  // Don't go totally dark to keep some visibility
    public float maxIntensity = 10f;  // The normal "bright" state
    public float flickerSpeedMin = 0.01f; 
    public float flickerSpeedMax = 0.1f;

    void Start()
    {
        myLight = GetComponent<Light>();
        
        // Ensure the light is set to Realtime
        if(myLight.lightmapBakeType != LightmapBakeType.Realtime)
        {
            Debug.LogWarning("Flickering script works best with Realtime Light Mode!");
        }

        StartCoroutine(FlickerRoutine());
    }

    IEnumerator FlickerRoutine()
    {
        while (true)
        {
            // Pick a random brightness
            myLight.intensity = Random.Range(minIntensity, maxIntensity);

            // Wait for a tiny, random amount of time before the next jump
            float randomWait = Random.Range(flickerSpeedMin, flickerSpeedMax);
            yield return new WaitForSeconds(randomWait);
        }
    }
}
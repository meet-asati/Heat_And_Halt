using UnityEngine;

public class SirenLight : MonoBehaviour
{
    private Light myLight;

    [Header("Siren Settings")]
    public float minIntensity = 0f;   // The dimmest the light gets
    public float maxIntensity = 100f; // The brightest (matches your current 100)
    public float blinkSpeed = 2f;     // How fast it pulses

    void Start()
    {
        // Automatically grab the light component from this object
        myLight = GetComponent<Light>();
    }

    void Update()
    {
        // Use PingPong to bounce the value back and forth between 0 and 1
        float t = Mathf.PingPong(Time.time * blinkSpeed, 1f);
        
        // Smoothly blend the intensity between min and max based on t
        myLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, t);
    }
}
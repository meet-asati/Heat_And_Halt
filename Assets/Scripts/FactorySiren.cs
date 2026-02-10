using UnityEngine;

public class FactorySiren : MonoBehaviour
{
    private Light myLight;

    [Header("Siren Colors")]
    public Color colorA = Color.white;  // First color
    public Color colorB = Color.red;    // Second color

    [Header("Settings")]
    public float speed = 3f;            // How fast it switches

    void Start()
    {
        myLight = GetComponent<Light>();
        
        // Safety check: Make sure we found the light
        if (myLight == null)
        {
            Debug.LogError("No Light component found on this object!");
        }
    }

    void Update()
    {
        if (myLight != null)
        {
            // Create a value that goes back and forth between 0 and 1
            float t = Mathf.PingPong(Time.time * speed, 1f);

            // Blend the two colors based on that value
            myLight.color = Color.Lerp(colorA, colorB, t);
        }
    }
}
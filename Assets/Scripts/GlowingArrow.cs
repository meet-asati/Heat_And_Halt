using UnityEngine;

public class GlowingArrow : MonoBehaviour
{
    public float pulseSpeed = 2f;
    public float minAlpha = 0.3f;
    public float maxAlpha = 1.0f;
    
    private Material mat;
    private Color baseColor;

    void Start()
    {
        Renderer r = GetComponent<Renderer>();
        if (r != null)
        {
            mat = r.material;
            baseColor = mat.color;
        }
    }

    void Update()
    {
        if (mat == null) return;

        // Calculate alpha using Sin wave for smooth pulsing
        float alpha = Mathf.Lerp(minAlpha, maxAlpha, (Mathf.Sin(Time.time * pulseSpeed) + 1.0f) / 2.0f);
        
        Color newColor = baseColor;
        newColor.a = alpha;
        mat.color = newColor;
    }
}
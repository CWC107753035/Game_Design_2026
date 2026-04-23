using UnityEngine;

public class CameraObstacleFader : MonoBehaviour
{
    public float targetAlpha = 0.3f;
    public float fadeSpeed = 5f;

    private Renderer rendererComponent;
    private Material[] originalMaterials;
    private Material[] transparentMaterials;
    private float currentAlpha = 1f;
    private bool shouldBeTransparent = false;

    void Awake()
    {
        rendererComponent = GetComponent<Renderer>();
        originalMaterials = rendererComponent.materials;
        transparentMaterials = new Material[originalMaterials.Length];
        
        for (int i = 0; i < originalMaterials.Length; i++)
        {
            Material mat = new Material(originalMaterials[i]);
            
            // Set URP Material to Transparent Mode
            mat.SetFloat("_Surface", 1);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            
            transparentMaterials[i] = mat;
        }
    }

    public void MarkForFadeOut(float alpha = 0.3f)
    {
        shouldBeTransparent = true;
        targetAlpha = alpha;
    }

    void Update()
    {
        float target = shouldBeTransparent ? targetAlpha : 1f;
        currentAlpha = Mathf.MoveTowards(currentAlpha, target, Time.deltaTime * fadeSpeed);
        
        if (currentAlpha >= 0.99f && !shouldBeTransparent)
        {
            // Restore original opaque materials and destroy this script
            rendererComponent.materials = originalMaterials;
            Destroy(this);
            return;
        }

        // Apply transparent materials and alpha
        rendererComponent.materials = transparentMaterials;
        foreach (var mat in transparentMaterials)
        {
            if (mat.HasProperty("_BaseColor"))
            {
                Color c = mat.GetColor("_BaseColor");
                c.a = currentAlpha;
                mat.SetColor("_BaseColor", c);
            }
            else if (mat.HasProperty("_Color"))
            {
                Color c = mat.GetColor("_Color");
                c.a = currentAlpha;
                mat.SetColor("_Color", c);
            }
        }

        // Reset so if player camera stops pinging, we fade back in
        shouldBeTransparent = false;
    }

    void OnDestroy()
    {
        if (transparentMaterials != null)
        {
            foreach (var mat in transparentMaterials)
            {
                if (mat != null) Destroy(mat);
            }
        }
    }
}

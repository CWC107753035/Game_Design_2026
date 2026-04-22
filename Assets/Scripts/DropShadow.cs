using UnityEngine;

namespace Slime
{
    public class DropShadow : MonoBehaviour
    {
        [Header("Drop Shadow Settings")]
        [Tooltip("The maximum distance the shadow will be cast.")]
        public float maxShadowDistance = 5f;
        [Tooltip("The size of the shadow when the player is on the ground.")]
        public float baseScale = 1.25f;
        [Tooltip("Offset above the ground to prevent z-fighting.")]
        public Vector3 offset = new Vector3(0, 0.05f, 0);
        [Tooltip("The opacity opacity of the shadow.")]
        [Range(0f, 1f)] public float shadowOpacity = 0.85f;
        [Tooltip("Layers the shadow can be cast upon.")]
        public LayerMask groundMask = ~0;

        private GameObject _shadowQuad;
        private Material _shadowMat;
        private Texture2D _shadowTexture;

        void Start()
        {
            // Set up the quad GameObject
            _shadowQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _shadowQuad.name = "DynamicDropShadow";
            
            // Remove the collider so it doesn't block physics or raycasts
            Destroy(_shadowQuad.GetComponent<Collider>());

            // Generate a soft circle texture for the drop shadow
            CreateShadowTexture();

            // Set up the material
            // Unlit/Transparent is safe and doesn't require lighting, Sprites/Default also works.
            _shadowMat = new Material(Shader.Find("Sprites/Default"));
            _shadowMat.mainTexture = _shadowTexture;
            _shadowQuad.GetComponent<MeshRenderer>().sharedMaterial = _shadowMat;

            // Make the quad a child so it is organized, but we will unparent or just modify position manually in LateUpdate
            _shadowQuad.transform.SetParent(null); // Keep it unparented so its transform isn't messed up by player rotation
        }

        private void CreateShadowTexture()
        {
            int resolution = 64;
            _shadowTexture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
            _shadowTexture.wrapMode = TextureWrapMode.Clamp;
            
            float centerXY = resolution / 2f;
            float maxRadius = resolution / 2f;

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(centerXY, centerXY));
                    float alpha = 1f - Mathf.Clamp01(dist / maxRadius);
                    
                    // Simple easing function for softer edges (less aggressive falloff so it's larger and darker)
                    alpha = Mathf.Pow(alpha, 1.2f);

                    // Black color with calculated alpha combined with the base shadow opacity
                    _shadowTexture.SetPixel(x, y, new Color(0, 0, 0, alpha * shadowOpacity)); 
                }
            }
            _shadowTexture.Apply();
        }

        void LateUpdate()
        {
            // Raycast down from the center of the player
            if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out RaycastHit hit, maxShadowDistance + 0.1f, groundMask, QueryTriggerInteraction.Ignore))
            {
                _shadowQuad.SetActive(true);
                
                // Position slightly above the surface to avoid z-fighting
                _shadowQuad.transform.position = hit.point + offset;
                
                // Align with the ground normal
                _shadowQuad.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(Vector3.forward, hit.normal), hit.normal);
                _shadowQuad.transform.Rotate(90, 0, 0); // Rotate quad to lie flat

                // Calculate scale based on distance
                // We subtract 0.1f because we originated the ray slightly higher
                float distance = Mathf.Max(0, hit.distance - 0.1f);
                float distanceRatio = distance / maxShadowDistance;
                
                // As distance hits maxShadowDistance, scale approaches 0
                float currentScale = Mathf.Lerp(baseScale, 0f, distanceRatio);
                _shadowQuad.transform.localScale = new Vector3(currentScale, currentScale, 1f);
                
                // Also adjust alpha for a fading out effect as it gets higher
                Color col = _shadowMat.color;
                col.a = Mathf.Lerp(1f, 0f, distanceRatio);
                _shadowMat.color = col;
            }
            else
            {
                _shadowQuad.SetActive(false);
            }
        }
        
        void OnDestroy()
        {
            if (_shadowQuad != null) Destroy(_shadowQuad);
            if (_shadowMat != null) Destroy(_shadowMat);
            if (_shadowTexture != null) Destroy(_shadowTexture);
        }
    }
}

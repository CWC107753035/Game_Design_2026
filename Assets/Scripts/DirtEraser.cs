using UnityEngine;
using UnityEngine.Events;

public class DirtEraser : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The dirt material using the Custom/URPDirtMasked shader")]
    public Material dirtMaterial;
    [Tooltip("The player or object that erases dirt")]
    public Transform character;

    [Header("Portal Settings")]
    [Tooltip("If true, finishing this dirt will act as a teleport portal. If false, it's just a cleanable surface.")]
    public bool isPortal = true;
    [Tooltip("Check this if you want the portal to load a completely different scene instead of teleporting locally!")]
    public bool loadNewScene = false;
    public string targetSceneName = "";
    
    [Space(10)]
    [Tooltip("If not loading a new scene, this is the local transform to teleport to.")]
    public Transform teleportTarget;
    [Tooltip("Check this setting if THIS is the destination circle and you want it to act as a portal instantly without dirt!")]
    public bool isAlreadyCleanPortal = false;
    public static float globalTeleportCooldown = 0f;
    [Tooltip("Radius around the center of the magic circle where teleporting will trigger. 1.0f is slightly smaller than a standard scaled plane.")]
    public float triggerRadius = 1.0f;
    [Tooltip("Maximum height above the portal where teleporting will trigger. Prevents mid-air teleports.")]
    public float triggerHeight = 0.5f;

    [Header("Dirt Settings")]
    [Tooltip("Direction to cast the cleaning ray. By default it casts globally DOWN. Change this if the dirt is on a wall! (e.g., (1,0,0) or (0,0,1))")]
    public Vector3 raycastDirection = Vector3.down;
    public float heightOffset = 0.05f;
    
    [Header("Events")]
    [Tooltip("Triggered when the dirt is fully erased! Great for puzzles.")]
    public UnityEvent onDirtErased;
    public float brushSize = 0.05f;
    public int maskResolution = 512;
    [Range(0f, 1f)]
    [Tooltip("Percentage of the total area needed to erase (0.25 equals roughly 35% of a circle)")]
    public float eraseThreshold = 0.25f;

    private RenderTexture dirtMask;
    private Texture2D drawTexture;
    private bool isDirty = false;
    private GameObject dirtOverlayPlane;
    private MeshCollider overlayCollider;
    private bool isFinished = false;
    private bool[] clearedPixels;
    private int clearedPixelsCount = 0;
    private int totalPixelsCount;
    private bool wasInCircle = false;

    void Start()
    {
        if (isAlreadyCleanPortal)
        {
            isFinished = true;
            return; // Skip setting up dirt entirely!
        }

        // 1. Generate the RenderTexture memory
        dirtMask = new RenderTexture(maskResolution, maskResolution, 0, RenderTextureFormat.ARGB32);
        dirtMask.Create();

        // 2. Create a CPU-side texture matching the RenderTexture
        drawTexture = new Texture2D(dirtMask.width, dirtMask.height, TextureFormat.RGBA32, false);
        totalPixelsCount = dirtMask.width * dirtMask.height;
        clearedPixels = new bool[totalPixelsCount];
        
        // Fill with white (fully dirty initially)
        Color[] pixels = new Color[totalPixelsCount];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.white;
        
        drawTexture.SetPixels(pixels);
        drawTexture.Apply();

        Graphics.Blit(drawTexture, dirtMask);

        // 3. Material Instantiation
        if (dirtMaterial != null)
        {
            dirtMaterial = new Material(dirtMaterial);
            dirtMaterial.SetTexture("_MaskTex", dirtMask);
        }

        // 4. Mesh Cloning Pipeline
        CreateAutoDirtOverlay();
    }

    void CreateAutoDirtOverlay()
    {
        dirtOverlayPlane = new GameObject("Auto_Dirt_Overlay");
        // We do NOT parent it so we can easily offset its world position upwards
        dirtOverlayPlane.transform.position = this.transform.position + Vector3.up * heightOffset;
        dirtOverlayPlane.transform.rotation = this.transform.rotation;
        dirtOverlayPlane.transform.localScale = this.transform.lossyScale * 1.002f;

        // Perfectly clone whatever mesh the magic circle currently uses!
        MeshFilter parentMF = GetComponent<MeshFilter>();
        if (parentMF != null)
        {
            MeshFilter childMF = dirtOverlayPlane.AddComponent<MeshFilter>();
            childMF.sharedMesh = parentMF.sharedMesh;
        }

        if (dirtMaterial != null)
        {
            MeshRenderer mr = dirtOverlayPlane.AddComponent<MeshRenderer>();
            mr.material = dirtMaterial;
        }

        // Add a physics collider specifically so we can do accurate UV raycasting!
        // We set the layer to Ignore Raycast so it doesn't block player clicks
        dirtOverlayPlane.layer = 2; // Layer 2 is 'Ignore Raycast' built-in Unity
        overlayCollider = dirtOverlayPlane.AddComponent<MeshCollider>();
    }

    void Update()
    {
        if (character == null) return;
        
        // Check if player is standing over the portal logic-wise
        bool currentlyInCircle = false;
        float horizontalDist = Vector3.Distance(new Vector3(character.position.x, 0, character.position.z), 
                                                new Vector3(transform.position.x, 0, transform.position.z));
        float heightDiff = character.position.y - transform.position.y;
        if (horizontalDist <= triggerRadius && heightDiff >= -0.5f && heightDiff <= triggerHeight)
        {
            currentlyInCircle = true;
        }

        if (isFinished)
        {
            if (isPortal)
            {
                // Now fully operates as a two-way portal for any form!
                // We ONLY trigger when they ENTER the circle (!wasInCircle). This cleanly prevents teleport loops.
                if (currentlyInCircle && !wasInCircle && Time.time > globalTeleportCooldown)
                {
                    if (loadNewScene || teleportTarget != null)
                    {
                        PerformTeleport();
                    }
                }
            }

            wasInCircle = currentlyInCircle;
            return;
        }

        wasInCircle = currentlyInCircle;
        EraseAtCharacterPosition();

        if (isDirty)
        {
            Graphics.Blit(drawTexture, dirtMask);
            isDirty = false;

            float erasePercentage = (float)clearedPixelsCount / totalPixelsCount;
            // Provide a helpful log for the user to tune their threshold!
            Debug.Log($"Wiped: {erasePercentage * 100f:F1}% / {eraseThreshold * 100f}% required");

            if (erasePercentage >= eraseThreshold)
            {
                TriggerCompletion();
            }
        }
    }

    void TriggerCompletion()
    {
        isFinished = true;

        if (dirtOverlayPlane != null)
        {
            Destroy(dirtOverlayPlane);
        }

        onDirtErased?.Invoke();

        if (isPortal)
        {
            PerformTeleport();
        }
    }

    void PerformTeleport()
    {
        // Apply a global cooldown so we don't instantly teleport back as soon as we arrive!
        globalTeleportCooldown = Time.time + 1.5f;

        if (loadNewScene && !string.IsNullOrEmpty(targetSceneName))
        {
            Debug.Log("Teleporting to a completely new scene: " + targetSceneName);
            UnityEngine.SceneManagement.SceneManager.LoadScene(targetSceneName);
            return;
        }

        if (teleportTarget == null) return;

        Vector3 oldPosition = character.position;
        Vector3 newPosition = teleportTarget.position + Vector3.up * 1f;

        // Reset rigidbody velocities if they exist so it doesn't bounce violently
        Rigidbody rb = character.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Teleport the core player!
        character.position = newPosition;
        Physics.SyncTransforms();
        
        // INSTANTLY TRANSLATE THE PBF FLUID! No swooshing or physics explosions.
        Slime.Slime_PBF slimePbf = character.GetComponent<Slime.Slime_PBF>();
        if (slimePbf != null)
        {
            Vector3 diff = newPosition - oldPosition;
            slimePbf.TeleportSystem(diff);
        }
        
        // INSTANTLY SNAP THE CAMERA! This prevents the "lerping swoop" visual effect completely.
        PlayerCamera cam = Object.FindAnyObjectByType<PlayerCamera>();
        if (cam != null)
        {
            cam.SnapToTarget();
        }

        Debug.Log("Teleporting to: " + teleportTarget.name);
    }

    void EraseAtCharacterPosition()
    {
        // Feature: Only the base SLIME form can erase dirt! (Not Ice or Steam)
        Slime.Slime_PBF slime = character.GetComponent<Slime.Slime_PBF>();
        if (slime != null && (slime.isFog || slime.isFrozen))
        {
            return; // Exit out, do not erase!
        }

        // Drop a tiny laser from the character towards the dirt surface to find EXACTLY where their feet hit
        Vector3 rayStart = character.position - raycastDirection.normalized * 1f;
        Ray ray = new Ray(rayStart, raycastDirection.normalized);

        if (overlayCollider.Raycast(ray, out RaycastHit hit, 3f))
        {
            // By using hit.textureCoord, we get perfectly accurate UV positions regardless of the shape/scale/rotation!
            float u = hit.textureCoord.x;
            float v = hit.textureCoord.y;

            // Paint clear (transparent) onto the CPU mask
            int pixelX = Mathf.RoundToInt(u * drawTexture.width);
            int pixelY = Mathf.RoundToInt(v * drawTexture.height);
            int brushPixels = Mathf.RoundToInt(brushSize * drawTexture.width);

            for (int x = -brushPixels; x <= brushPixels; x++)
            {
                for (int y = -brushPixels; y <= brushPixels; y++)
                {
                    // Circular brush shape
                    if (x * x + y * y > brushPixels * brushPixels) continue;

                    int px = Mathf.Clamp(pixelX + x, 0, drawTexture.width - 1);
                    int py = Mathf.Clamp(pixelY + y, 0, drawTexture.height - 1);
                    
                    int pixelIndex = py * drawTexture.width + px;
                    if (!clearedPixels[pixelIndex])
                    {
                        clearedPixels[pixelIndex] = true;
                        clearedPixelsCount++;
                        drawTexture.SetPixel(px, py, Color.clear);
                    }
                }
            }

            drawTexture.Apply();
            isDirty = true;
        }
    }
}
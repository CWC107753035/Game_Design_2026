using UnityEngine;

public class CameraAngleTrigger : MonoBehaviour
{
    [Header("Camera Settings")]
    [Tooltip("The pitch angle (up/down) to apply when in this area.")]
    public float overridePitch = 45f;
    
    [Tooltip("The yaw angle (left/right) to apply when in this area.")]
    public float overrideYaw = 0f;
    
    [Tooltip("Set to a value > 0 to override distance from player. -1 means no override (use default).")]
    public float overrideDistance = -1f;

    private PlayerCamera playerCam;
    private Collider triggerCollider;
    private Transform playerTransform;
    private bool isPlayerInside = false;

    void Start()
    {
        triggerCollider = GetComponent<Collider>();

        // Try to find the camera at start if possible
        if (Camera.main != null)
        {
            playerCam = Camera.main.GetComponent<PlayerCamera>();
        }
    }

    void Update()
    {
        // Fallback robust check: if player moves too fast, OnTriggerExit might not fire.
        // We manually verify if the player is still inside the collider.
        if (isPlayerInside && playerTransform != null && triggerCollider != null)
        {
            Vector3 closestPoint = triggerCollider.ClosestPoint(playerTransform.position);
            // Use a generous threshold (5 units) because ClosestPoint returns the nearest
            // point on the SURFACE of the box. When the player is inside the box, the distance
            // to the nearest wall can easily be 1-2 units. The old 0.1f value was way too strict.
            if (Vector3.Distance(playerTransform.position, closestPoint) > 5.0f)
            {
                PlayerExited();
            }
        }
    }

    // We detect the player by searching for the Slime_PBF component in the parent hierarchy,
    // just like ButtonTrigger does. This is much more reliable than CompareTag("Player")
    // because the slime has multiple child colliders, and child GameObjects do NOT inherit
    // the parent's "Player" tag. With CompareTag, a trigger would only fire if the specific
    // root collider entered — which depends entirely on the trigger's rotation and position.
    void OnTriggerEnter(Collider other)
    {
        Slime.Slime_PBF slime = other.GetComponentInParent<Slime.Slime_PBF>();
        if (slime != null)
        {
            Debug.Log($"[CameraAngleTrigger] Player entered trigger: {gameObject.name}");
            playerTransform = slime.transform; // Always use the ROOT slime transform
            isPlayerInside = true;

            if (playerCam == null)
            {
                if (Camera.main != null)
                    playerCam = Camera.main.GetComponent<PlayerCamera>();
                
                if (playerCam == null)
                    playerCam = FindObjectOfType<PlayerCamera>();
            }

            if (playerCam != null)
            {
                Debug.Log($"[CameraAngleTrigger] Setting Override: Pitch={overridePitch}, Yaw={overrideYaw}, Distance={overrideDistance}");
                playerCam.SetCameraOverride(overridePitch, overrideYaw, overrideDistance);
            }
            else
            {
                Debug.LogWarning("[CameraAngleTrigger] Could not find PlayerCamera in the scene!");
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        Slime.Slime_PBF slime = other.GetComponentInParent<Slime.Slime_PBF>();
        if (slime != null)
        {
            PlayerExited();
        }
    }

    void PlayerExited()
    {
        if (isPlayerInside)
        {
            Debug.Log($"[CameraAngleTrigger] Player exited trigger: {gameObject.name}");
            isPlayerInside = false;
            playerTransform = null;

            if (playerCam != null)
            {
                playerCam.ResetCameraOverride();
            }
        }
    }
}

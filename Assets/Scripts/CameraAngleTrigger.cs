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
            // If the closest point to the player on the collider is not the player's position,
            // it means the player is outside the collider.
            if (Vector3.Distance(playerTransform.position, closestPoint) > 0.1f)
            {
                PlayerExited();
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("[CameraAngleTrigger] Player entered trigger.");
            playerTransform = other.transform;
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
        if (other.CompareTag("Player"))
        {
            PlayerExited();
        }
    }

    void PlayerExited()
    {
        if (isPlayerInside)
        {
            Debug.Log("[CameraAngleTrigger] Player exited trigger.");
            isPlayerInside = false;
            playerTransform = null;

            if (playerCam != null)
            {
                playerCam.ResetCameraOverride();
            }
        }
    }
}

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

    void Start()
    {
        // Try to find the camera at start if possible
        if (Camera.main != null)
        {
            playerCam = Camera.main.GetComponent<PlayerCamera>();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("[CameraAngleTrigger] Player entered trigger.");
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
            if (playerCam != null)
            {
                playerCam.ResetCameraOverride();
            }
        }
    }
}

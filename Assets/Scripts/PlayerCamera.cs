using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCamera : MonoBehaviour
{
    [Header("References")]
    public Transform target;
    [Tooltip("Layers that forcefully push the camera inward (solid walls).")]
    public LayerMask wallLayers;
    [Tooltip("Layers that turn transparent instead of blocking the camera (props, trees, etc).")]
    public LayerMask fadeLayers;

    [Header("Position")]
    public float defaultDistance = 3f;
    public float minDistance = 0f;
    public float heightOffset = 1f;

    [Header("Rotation")]
    public float mouseSensitivityX = 3f;
    public float mouseSensitivityY = 2f;
    public float minVerticalAngle = -10f;
    public float maxVerticalAngle = 45f;

    [Header("Smoothing")]
    public float positionDampingXZ = 0.1f; // how fast camera follows horizontally
    public float positionDampingY = 0.2f;  // how fast camera follows vertically
    public float rotationSpeed = 8f;       // how fast camera rotates
    public float wallPushSpeed = 20f;
    public float wallPullSpeed = 2f;

    [Header("Collision")]
    public float castRadius = 0.2f;
    public float fixedYaw = 0f;
    public float fixedPitch = 20f;

    private float yaw = 0f;
    private float pitch = 20f;
    private float currentDistance;
    private float distVelocity = 0f;
    private Vector3 posVelocityXZ;
    private float posVelocityY;
    private Vector3 currentFollowPos;

    void Start()
    {
        if (fadeLayers.value == 0)
        {
            Debug.LogWarning("[PlayerCamera] The 'Fade Layers' property is currently set to 'Nothing' in your Inspector! Please click on the camera and select the layers you want to fade out.");
        }

        currentDistance = defaultDistance;
        currentFollowPos = target.position;
        yaw = transform.eulerAngles.y;
        pitch = transform.eulerAngles.x;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        yaw = fixedYaw;
        pitch = fixedPitch;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // HandleRotationInput();
        SmoothFollowTarget();
        HandleWallCollision();
        ApplyCameraTransform();
    }

    // void HandleRotationInput()
    // {
    //     Vector2 mouseDelta = Mouse.current.delta.ReadValue();
    //     yaw += mouseDelta.x * mouseSensitivityX * Time.deltaTime;
    //     pitch -= mouseDelta.y * mouseSensitivityY * Time.deltaTime;
    //     pitch = Mathf.Clamp(pitch, minVerticalAngle, maxVerticalAngle);
    // }

    void SmoothFollowTarget()
    {
        // Smooth follow target position (damp XZ and Y separately)
        Vector3 targetPos = target.position;

        float smoothX = Mathf.SmoothDamp(currentFollowPos.x, targetPos.x, 
            ref posVelocityXZ.x, positionDampingXZ);
        float smoothZ = Mathf.SmoothDamp(currentFollowPos.z, targetPos.z, 
            ref posVelocityXZ.z, positionDampingXZ);
        float smoothY = Mathf.SmoothDamp(currentFollowPos.y, targetPos.y, 
            ref posVelocityY, positionDampingY);

        currentFollowPos = new Vector3(smoothX, smoothY, smoothZ);
    }

    void HandleWallCollision()
    {
        Vector3 origin = currentFollowPos + Vector3.up * heightOffset;
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        Vector3 camDir = rotation * Vector3.back;

        // 1. Check for Objects that we want to turn transparent
        RaycastHit[] fadeHits = Physics.SphereCastAll(origin, castRadius, camDir, defaultDistance, fadeLayers);
        foreach (var hit in fadeHits)
        {
            Renderer renderer = hit.collider.GetComponentInParent<Renderer>();
            if (renderer == null) renderer = hit.collider.GetComponentInChildren<Renderer>();
            
            if (renderer != null)
            {
                CameraObstacleFader fader = renderer.gameObject.GetComponent<CameraObstacleFader>();
                if (fader == null) fader = renderer.gameObject.AddComponent<CameraObstacleFader>();
                fader.MarkForFadeOut();
            }
        }

        // 2. Check for solid Walls that physically block the camera
        float targetDistance = defaultDistance;
        if (Physics.SphereCast(origin, castRadius, camDir, out RaycastHit wallHit, defaultDistance, wallLayers))
        {
            // Try to place the camera just in front of the wall
            targetDistance = Mathf.Clamp(wallHit.distance, minDistance, defaultDistance);
        }

        // 3. Smoothly animate camera pushing inward (fast) or recovering outward (slow)
        float speed = currentDistance > targetDistance ? wallPushSpeed : wallPullSpeed;
        currentDistance = Mathf.SmoothDamp(currentDistance, targetDistance, ref distVelocity, 1f / speed);
    }

    void ApplyCameraTransform()
    {
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        Vector3 origin = currentFollowPos + Vector3.up * heightOffset;

        transform.position = origin + rotation * Vector3.back * currentDistance;
        transform.rotation = rotation;
    }

    public void SnapToTarget()
    {
        if (target != null)
        {
            currentFollowPos = target.position;
            posVelocityXZ = Vector3.zero;
            posVelocityY = 0f;
            ApplyCameraTransform();
        }
    }
}
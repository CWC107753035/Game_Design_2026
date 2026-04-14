using UnityEngine;
using Unity.Cinemachine;

public class CameraWallPush : CinemachineExtension
{
    [Header("References")]
    public Transform followTarget;

    [Header("Settings")]
    public LayerMask wallLayers;
    public float defaultDistance = 3f;
    public float minDistance = 0f;
    public float pullSpeed = 2f;

    private float currentDistance;
    private float velocity = 0f;

    protected override void OnEnable()
    {
        base.OnEnable();
        currentDistance = defaultDistance;
    }

    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage,
        ref CameraState state,
        float deltaTime)
    {
        // Run after the Body stage (position has been calculated)
        if (stage != CinemachineCore.Stage.Body) return;
        if (followTarget == null) return;

        Vector3 origin = followTarget.position + Vector3.up * 1f;
        Vector3 camPos = state.GetCorrectedPosition();
        Vector3 camDir = (camPos - origin).normalized;
        float desiredDistance = defaultDistance;

        // Centre ray
        if (Physics.Linecast(origin, camPos, out RaycastHit hit, wallLayers))
        {
            float dist = Vector3.Distance(origin, hit.point) - 0.05f;
            desiredDistance = Mathf.Min(desiredDistance, Mathf.Max(dist, minDistance));
            Debug.DrawLine(origin, hit.point, Color.red);
            Debug.Log($"Hit: {hit.collider.name} | desired: {desiredDistance:F2}");
        }
        else
        {
            Debug.DrawLine(origin, camPos, Color.green);
        }

        // Side rays
        Vector3 right = state.GetCorrectedOrientation() * Vector3.right * 0.3f;
        Vector3 up = state.GetCorrectedOrientation() * Vector3.up * 0.3f;
        foreach (var offset in new[] { right, -right, up, -up })
        {
            if (Physics.Linecast(origin + offset, camPos + offset,
                out RaycastHit sideHit, wallLayers))
            {
                float dist = Vector3.Distance(origin, sideHit.point) - 0.05f;
                desiredDistance = Mathf.Min(desiredDistance, Mathf.Max(dist, minDistance));
                Debug.DrawLine(origin + offset, sideHit.point, Color.red);
            }
        }

        // Smooth the distance
        float smoothTime = desiredDistance < currentDistance ? 0.02f : 1f / pullSpeed;
        currentDistance = Mathf.SmoothDamp(
            currentDistance, desiredDistance, ref velocity, smoothTime);

        // Directly reposition the camera along the same direction
        Vector3 newPos = origin + camDir * currentDistance;
        state.PositionCorrection += newPos - camPos;

        Debug.Log($"desired: {desiredDistance:F2} | current: {currentDistance:F2}");
    }
}
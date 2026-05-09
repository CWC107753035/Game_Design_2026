using UnityEngine;
using Slime;

[RequireComponent(typeof(Collider))]
public class WaterStream : MonoBehaviour
{
    [Header("Push Settings (Water Form)")]
    [Tooltip("The point or next stream mesh to push the water slime towards. If empty, pushes along the stream's forward direction.")]
    public Transform nextStreamPoint;
    
    [Tooltip("How fast the water stream pushes the water slime.")]
    public float pushSpeed = 15f;

    [Tooltip("How high above the water surface the slime should float. Increase this if the slime's bottom sinks into the mesh.")]
    public float floatOffset = 0.25f;

    [Header("Ice Form Settings")]
    [Tooltip("The maximum fall speed for the ice slime going through the stream. Makes it fall 'slowly'.")]
    public float iceMaxFallSpeed = 2f;

    private void Start()
    {
        // Automatically make sure this water plane allows objects to pass through it
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerStay(Collider other)
    {
        // Detect the slime by finding its core PBF component
        Slime_PBF slime = other.transform.root.GetComponentInChildren<Slime_PBF>();
        if (slime == null) return;

        ControllerTest controller = slime.GetComponent<ControllerTest>();
        Rigidbody rb = slime.GetComponent<Rigidbody>();
        
        if (controller == null || rb == null) return;

        if (slime.isFog)
        {
            controller.disableForwardBack = false; // Reset movement
            controller.isGroundedOverride = false; // Reset grounded
            // Air Form: Slime dies instantly
            if (CheckpointManager.Instance != null)
            {
                CheckpointManager.Instance.RespawnPlayer();
            }
        }
        else if (slime.isFrozen)
        {
            controller.disableForwardBack = false; // Reset movement
            controller.isGroundedOverride = false; // Reset grounded
            // Ice Form: Slowly falls or goes through it
            Vector3 vel = rb.linearVelocity;
            if (vel.y < -iceMaxFallSpeed)
            {
                rb.linearVelocity = new Vector3(vel.x, -iceMaxFallSpeed, vel.z);
            }
        }
        else
        {
            // Water Form: Automatically push to the next mesh and stay on top
            
            // 1. Restrict forward/backward (W/S) input so player can only steer left/right
            controller.disableForwardBack = true;

            // 2. Calculate push direction
            Vector3 pushDirection;
            if (nextStreamPoint != null)
            {
                pushDirection = (nextStreamPoint.position - slime.transform.position).normalized;
            }
            else
            {
                pushDirection = transform.forward;
            }

            // 3. Horizontal Push: Feed into the controller's external momentum system
            controller.externalVelocity = new Vector3(pushDirection.x * pushSpeed, 0f, pushDirection.z * pushSpeed);

            // 4. Vertical Push (Floating logic)
            // We only interfere with Y velocity if the player is NOT jumping up.
            Vector3 currentVel = rb.linearVelocity;
            if (currentVel.y <= 0.1f)
            {
                Collider myCol = GetComponent<Collider>();
                float surfaceY = myCol.bounds.max.y;
                float targetY = surfaceY + floatOffset;
                float slimeY = slime.transform.position.y;
                
                // Allow jumping if they are floating on or slightly below the surface
                if (slimeY <= targetY + 0.15f)
                {
                    controller.isGroundedOverride = true;
                }
                else
                {
                    controller.isGroundedOverride = false;
                }

                if (slimeY < targetY)
                {
                    // Use a strong spring force to push the slime up to the surface
                    currentVel.y = (targetY - slimeY) * 15f; 
                }
                else
                {
                    // Cancel manual gravity so they float perfectly on top without sinking
                    currentVel.y = 0f;
                }
                rb.linearVelocity = currentVel;
            }
            else
            {
                // If they are moving upwards (jumping), disable grounded so they can't double jump
                controller.isGroundedOverride = false;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // When leaving the stream (or jumping out of it), re-enable full movement controls!
        Slime_PBF slime = other.transform.root.GetComponentInChildren<Slime_PBF>();
        if (slime != null)
        {
            ControllerTest controller = slime.GetComponent<ControllerTest>();
            if (controller != null)
            {
                controller.disableForwardBack = false;
                controller.isGroundedOverride = false;
            }
        }
    }
}

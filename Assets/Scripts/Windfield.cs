using UnityEngine;

public class WindField : MonoBehaviour
{
    public float windStrength = 3f;
    public Vector3 windDirection = Vector3.up;

    [Header("Obstacle Blocking")]
    [Tooltip("If true, the wind will be blocked by solid objects like closed doors.")]
    public bool checkObstacles = true;
    
    [Tooltip("Layers that can block the wind (usually Default).")]
    public LayerMask obstacleLayers = Physics.DefaultRaycastLayers;

    // OnTriggerStay runs every physics frame that an object is inside the trigger box
    private void OnTriggerStay(Collider other)
    {
        // Only apply force if the object is tagged "Player"
        if (other.CompareTag("Player"))
        {
            Rigidbody playerRb = other.GetComponent<Rigidbody>();
            
            if (playerRb != null)
            {
                if (checkObstacles)
                {
                    // Cast a ray from the player directly against the wind direction
                    if (Physics.Raycast(other.transform.position, -windDirection.normalized, out RaycastHit hit, 100f, obstacleLayers))
                    {
                        // Calculate how far "up" the wind the obstruction is.
                        // If it hits the door, the hit point is high up. If it hits the fan itself, the hit point is low.
                        float obstructionHeight = Vector3.Dot(hit.point - transform.position, windDirection.normalized);
                        
                        // If the object blocking the ray is significantly higher than the base of the windfield, it's a door/floor blocking the wind!
                        if (obstructionHeight > 0.5f && hit.collider.gameObject != this.gameObject)
                        {
                            return; // The wind is blocked! Do not apply force.
                        }
                    }
                }

                // Apply a continuous pushing force. 
                // ForceMode.Acceleration ignores the player's mass, making it easier to tune.
                playerRb.AddForce(windDirection.normalized * windStrength, ForceMode.Acceleration);
            }
        }
    }
}
using UnityEngine;

public class WindField : MonoBehaviour
{
    public float windStrength = 3f;
    
    public Vector3 windDirection = Vector3.up;

    // OnTriggerStay runs every physics frame that an object is inside the trigger box
    private void OnTriggerStay(Collider other)
    {
        // Only apply force if the object is tagged "Player"
        if (other.CompareTag("Player"))
        {
            Rigidbody playerRb = other.GetComponent<Rigidbody>();
            
            if (playerRb != null)
            {
                // Apply a continuous pushing force. 
                // ForceMode.Acceleration ignores the player's mass, making it easier to tune.
                playerRb.AddForce(windDirection.normalized * windStrength, ForceMode.Acceleration);
            }
        }
    }
}
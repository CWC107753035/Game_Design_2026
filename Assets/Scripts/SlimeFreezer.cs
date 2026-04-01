using UnityEngine;
using Slime; // We need this to access your Slime_PBF script

public class SlimeFreezer : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Check if the object entering the trigger has the Slime_PBF script
        // We use GetComponentInParent just in case the slime's colliders are on child objects
        Slime_PBF slime = other.GetComponentInParent<Slime_PBF>();
        
        if (slime != null)
        {
            slime.isFrozen = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Slime stays frozen forever!
    }
}
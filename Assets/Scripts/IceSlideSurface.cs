using UnityEngine;
using Slime;

[RequireComponent(typeof(Collider))]
public class IceSlideSurface : MonoBehaviour
{
    [Header("Ice Form Settings")]
    public bool enableForIce = true;
    [Tooltip("How strongly the slope pulls the Ice form downhill.")]
    public float slideAcceleration = 40f;
    
    [Tooltip("The maximum sliding velocity the Ice form can reach.")]
    public float maxSlideSpeed = 30f;

    [Header("Normal Slime Settings")]
    public bool enableForSlime = false;
    [Tooltip("How strongly the slope pulls the normal Slime form downhill.")]
    public float slimeSlideAcceleration = 10f;
    
    [Tooltip("The maximum sliding velocity the normal Slime form can reach.")]
    public float slimeMaxSlideSpeed = 10f;

    private void OnCollisionStay(Collision collision)
    {
        // Try to get the slime controller components from ANYWHERE in the colliding object's hierarchy
        ControllerTest controller = collision.collider.transform.root.GetComponentInChildren<ControllerTest>();
        Slime_PBF slime = collision.collider.transform.root.GetComponentInChildren<Slime_PBF>();

        // We specifically check that the script exists and matches an enabled form
        if (controller != null && slime != null)
        {
            bool isNormalSlime = !slime.isFrozen && !slime.isFog;
            
            bool canSlide = false;
            float currentAccel = 0f;
            float currentMaxSpeed = 0f;

            if (slime.isFrozen && enableForIce)
            {
                canSlide = true;
                currentAccel = slideAcceleration;
                currentMaxSpeed = maxSlideSpeed;
            }
            else if (isNormalSlime && enableForSlime)
            {
                canSlide = true;
                currentAccel = slimeSlideAcceleration;
                currentMaxSpeed = slimeMaxSlideSpeed;
            }

            if (canSlide)
            {
                // Evaluate the surface normal of the slope using its exact rotation, avoiding buggy collision normals
                Vector3 normal = transform.up;
                if (normal.y < 0) normal = -normal; // Ensure normal always points toward the upward hemisphere
                
                // Only slide if the surface is actually an incline (not a flat floor or vertical wall)
                if (normal.y < 0.99f && normal.y > 0.05f)
                {
                    // Project 'Straight Down' onto the slope's surface. 
                    // This gives the exact perfect 3D vector pointing straight downhill, sticking them to the plane.
                    Vector3 downHillDir = Vector3.ProjectOnPlane(Vector3.down, normal).normalized;

                    // Inject 3D sliding physics natively into the Slime controller
                    controller.externalVelocity += downHillDir * currentAccel * Time.fixedDeltaTime;

                    // Debug print so we know the force is applying!
                    Debug.Log($"[IceSlideSurface] Sliding! Push: {downHillDir} | Current Vel: {controller.externalVelocity.magnitude}");

                    // Cap the speed so they don't break through walls or glitch out
                    if (controller.externalVelocity.magnitude > currentMaxSpeed)
                    {
                        controller.externalVelocity = controller.externalVelocity.normalized * currentMaxSpeed;
                    }
                }
                else 
                {
                    Debug.Log($"[IceSlideSurface] Slope is too flat to slide! Normal.y = {normal.y}");
                }
            }
        }
    }
}

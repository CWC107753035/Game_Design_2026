using UnityEngine;
using UnityEngine.Events;
using Slime;

[RequireComponent(typeof(Rigidbody))]
public class CogSwitch : MonoBehaviour
{
    [Header("Rotation Settings")]
    [Tooltip("The local axis to spin around. (0,1,0) spins like a merry-go-round, (1,0,0) or (0,0,1) spins like a ferris wheel.")]
    public Vector3 spinAxis = Vector3.up;
    
    [Tooltip("Maximum rotation speed in degrees per second.")]
    public float maxSpinSpeed = 180f;
    
    [Tooltip("How fast it accelerates (degrees per second per second).")]
    public float acceleration = 60f;

    [Header("Behavior")]
    [Tooltip("If true, just touching it ONCE will make it automatically accelerate to max speed forever. If false, the slime must STAY on it to build up speed.")]
    public bool autoAccelerateOnceTriggered = true;

    [Tooltip("If true (and Auto Accelerate is false), the cog will slow down when the slime steps off.")]
    public bool slowDownWhenNotStoodOn = false;
    
    [Tooltip("How fast it decelerates if the slime steps off.")]
    public float deceleration = 30f;

    [Header("Door Mechanism Mode")]
    [Tooltip("If true, the cog acts like a valve to open doors. It stops when fully open, and unwinds when the slime gets off.")]
    public bool isDoorMechanism = false;

    [Tooltip("If true, pushing the cog either way will open the door. If false, it only opens when pushed in the required direction.")]
    public bool allowAnyDirection = true;
    
    [Tooltip("If Allow Any Direction is false, use 1 for Counter-Clockwise, or -1 for Clockwise.")]
    public float requiredSpinDirection = 1f;
    
    [Tooltip("How many total degrees the cog must spin to fully open the door.")]
    public float degreesToFullyOpen = 720f;
    
    [Tooltip("How fast it spins backwards to close the door when the slime gets off.")]
    public float unwindAcceleration = 60f;

    [Header("Jump Mode Settings")]
    [Tooltip("If true, standing on it does nothing. The slime must JUMP onto it (or hit it hard). Each impact adds a burst of spin speed.")]
    public bool requireJumpImpact = false;
    
    [Tooltip("How much spin speed is added per jump/impact.")]
    public float jumpImpactForce = 120f;
    
    [Tooltip("Minimum impact velocity to count as a jump (prevents slowly walking onto it from counting).")]
    public float minImpactVelocity = 2f;

    [Header("Events")]
    [Tooltip("Fired exactly once when the cog reaches maximum speed in normal mode.")]
    public UnityEvent onMaxSpeedReached;

    [Tooltip("Fires continuously with a value from 0.0 (closed) to 1.0 (open) when in Door Mechanism Mode. Hook this to your doors!")]
    public UnityEvent<float> onMechanismProgress;

    private float currentSpinVelocity = 0f;
    private float spinDirection = 1f;
    private bool isSlimeStanding = false;
    private bool isTriggered = false;
    private bool hasReachedMaxSpeed = false;
    
    // Door mechanism variables
    private float accumulatedAngle = 0f;
    private float openingDirection = 1f;
    
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        // Ensure it's kinematic so we can rotate it via script, but physics objects still interact with it naturally
        rb.isKinematic = true; 
        
        // We use Interpolate so the visual rotation looks perfectly smooth to the camera
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (!allowAnyDirection)
        {
            openingDirection = Mathf.Sign(requiredSpinDirection);
        }
    }

    void OnCollisionStay(Collision collision)
    {
        if (requireJumpImpact) return; // Ignore standing if Jump Mode is enabled

        // Try to find the slime component on the colliding object
        Slime_PBF slime = collision.collider.transform.root.GetComponentInChildren<Slime_PBF>();
        
        // Ensure it's not the fog/air form
        if (slime != null && !slime.isFog)
        {
            // Convert the local spin axis to a world-space direction
            Vector3 worldSpinAxis = transform.TransformDirection(spinAxis.normalized);
            bool hitSide = false;

            // Check all contact points between the slime and the cog
            for (int i = 0; i < collision.contactCount; i++)
            {
                ContactPoint contact = collision.GetContact(i);
                
                // We check the direction of the surface normal.
                // If it hits the flat top/bottom, the normal points in the same (or opposite) direction as the spin axis (dot product ~ 1).
                // If it hits the curved side, the normal points perpendicular to the spin axis (dot product ~ 0).
                float dotProduct = Mathf.Abs(Vector3.Dot(contact.normal, worldSpinAxis));

                // If dot product is less than 0.7, it's mostly hitting the side
                if (dotProduct < 0.7f)
                {
                    hitSide = true;
                    break;
                }
            }

            if (hitSide)
            {
                isSlimeStanding = true;

                // Calculate the physics torque to determine WHICH WAY the cog should spin!
                ContactPoint primaryContact = collision.GetContact(0);
                
                // Vector from cog center to the contact point
                Vector3 r = primaryContact.point - transform.position;
                
                // The force applied by the slime. We use gravity (down) as the base,
                // but add its actual movement velocity so pushing sideways works too.
                Rigidbody slimeRb = collision.collider.attachedRigidbody;
                Vector3 force = Vector3.down * 5f; // Baseline gravity push
                if (slimeRb != null)
                {
                    force += slimeRb.linearVelocity;
                }

                // Torque = Cross Product of Lever Arm (r) and Force (F)
                Vector3 torque = Vector3.Cross(r, force);

                // Compare torque direction to the cog's spin axis
                float alignment = Vector3.Dot(torque, worldSpinAxis);

                // Update the spin direction if there's a clear push
                if (Mathf.Abs(alignment) > 0.1f)
                {
                    float detectedDirection = Mathf.Sign(alignment);
                    
                    if (!isTriggered)
                    {
                        spinDirection = detectedDirection;
                        isTriggered = true;
                    }
                    else if (!autoAccelerateOnceTriggered)
                    {
                        // If they must keep standing on it, they can push it the other way to reverse it!
                        spinDirection = detectedDirection;
                    }
                }
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!requireJumpImpact) return; // Only process jumps if Jump Mode is enabled

        Slime_PBF slime = collision.collider.transform.root.GetComponentInChildren<Slime_PBF>();
        
        // Ensure it's not the fog/air form
        if (slime != null && !slime.isFog)
        {
            // Check if they hit it hard enough
            if (collision.relativeVelocity.magnitude >= minImpactVelocity)
            {
                Vector3 worldSpinAxis = transform.TransformDirection(spinAxis.normalized);
                bool hitSide = false;

                for (int i = 0; i < collision.contactCount; i++)
                {
                    ContactPoint contact = collision.GetContact(i);
                    float dotProduct = Mathf.Abs(Vector3.Dot(contact.normal, worldSpinAxis));
                    if (dotProduct < 0.7f)
                    {
                        hitSide = true;
                        break;
                    }
                }

                if (hitSide)
                {
                    ContactPoint primaryContact = collision.GetContact(0);
                    Vector3 r = primaryContact.point - transform.position;
                    
                    // The impact velocity is the perfect force vector!
                    Vector3 force = collision.relativeVelocity; 
                    
                    Vector3 torque = Vector3.Cross(r, force);
                    float alignment = Vector3.Dot(torque, worldSpinAxis);

                    if (Mathf.Abs(alignment) > 0.1f)
                    {
                        float detectedDirection = Mathf.Sign(alignment);
                        spinDirection = detectedDirection;

                        if (isDoorMechanism && accumulatedAngle <= 0.01f && Mathf.Abs(currentSpinVelocity) < 0.1f)
                        {
                            if (allowAnyDirection) openingDirection = spinDirection; 
                        }

                        // Add the impulse velocity!
                        currentSpinVelocity += jumpImpactForce * spinDirection;
                        currentSpinVelocity = Mathf.Clamp(currentSpinVelocity, -maxSpinSpeed, maxSpinSpeed);
                        
                        isTriggered = true;
                    }
                }
            }
        }
    }

    void FixedUpdate()
    {
        if (isDoorMechanism)
        {
            if (isSlimeStanding && !requireJumpImpact)
            {
                // If it's fully closed and stopped, the direction they push becomes the "opening" direction!
                if (accumulatedAngle <= 0.01f && Mathf.Abs(currentSpinVelocity) < 0.1f)
                {
                    if (allowAnyDirection) openingDirection = spinDirection; 
                }
                
                float targetVelocity = maxSpinSpeed * spinDirection;
                currentSpinVelocity = Mathf.MoveTowards(currentSpinVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);
            }
            else
            {
                // Unwind! Spin back to zero by going opposite to the opening direction
                float targetVelocity = maxSpinSpeed * -openingDirection;
                currentSpinVelocity = Mathf.MoveTowards(currentSpinVelocity, targetVelocity, unwindAcceleration * Time.fixedDeltaTime);
            }

            float deltaAngle = currentSpinVelocity * Time.fixedDeltaTime;

            // Calculate how much the "progress" should change
            float progressDelta = (Mathf.Sign(currentSpinVelocity) == Mathf.Sign(openingDirection)) ? Mathf.Abs(deltaAngle) : -Mathf.Abs(deltaAngle);
            float nextAngle = accumulatedAngle + progressDelta;

            if (nextAngle >= degreesToFullyOpen)
            {
                // Hit the open limit!
                progressDelta = degreesToFullyOpen - accumulatedAngle;
                deltaAngle = progressDelta * openingDirection;
                accumulatedAngle = degreesToFullyOpen;
                currentSpinVelocity = 0f; // hard stop
            }
            else if (nextAngle <= 0f)
            {
                // Hit the closed limit!
                progressDelta = -accumulatedAngle;
                deltaAngle = progressDelta * -openingDirection;
                accumulatedAngle = 0f;
                currentSpinVelocity = 0f; // hard stop
            }
            else
            {
                accumulatedAngle = nextAngle;
            }

            if (Mathf.Abs(deltaAngle) > 0.001f)
            {
                Quaternion localDeltaRotation = Quaternion.Euler(spinAxis.normalized * deltaAngle);
                rb.MoveRotation(rb.rotation * localDeltaRotation);
            }

            // Always broadcast progress so doors stay updated
            onMechanismProgress?.Invoke(accumulatedAngle / degreesToFullyOpen);
        }
        else
        {
            // Existing logic for infinite spin ...
            if ((isSlimeStanding && !requireJumpImpact) || (isTriggered && autoAccelerateOnceTriggered))
            {
                float targetVelocity = maxSpinSpeed * spinDirection;
                currentSpinVelocity = Mathf.MoveTowards(currentSpinVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);
            }
            else if (slowDownWhenNotStoodOn)
            {
                currentSpinVelocity = Mathf.MoveTowards(currentSpinVelocity, 0f, deceleration * Time.fixedDeltaTime);
            }

            float absSpeed = Mathf.Abs(currentSpinVelocity);
            if (absSpeed >= maxSpinSpeed - 0.1f && !hasReachedMaxSpeed)
            {
                hasReachedMaxSpeed = true;
                onMaxSpeedReached?.Invoke();
            }
            else if (absSpeed < maxSpinSpeed - 0.1f && hasReachedMaxSpeed)
            {
                hasReachedMaxSpeed = false; 
            }

            if (Mathf.Abs(currentSpinVelocity) > 0f)
            {
                Quaternion localDeltaRotation = Quaternion.Euler(spinAxis.normalized * (currentSpinVelocity * Time.fixedDeltaTime));
                rb.MoveRotation(rb.rotation * localDeltaRotation);
            }
        }

        // Reset standing flag for the next physics frame
        isSlimeStanding = false;
    }
}

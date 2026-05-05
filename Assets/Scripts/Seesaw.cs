using UnityEngine;

public class Seesaw : MonoBehaviour
{
    [Header("Seesaw Settings")]
    [Tooltip("The maximum angle the seesaw can tilt in either direction.")]
    public float maxTiltAngle = 30f;
    
    [Tooltip("How much influence the slime's mass has. Higher = faster leaning.")]
    public float massSensitivity = 15f;
    
    [Tooltip("How fast it returns to the flat position when nothing is on it.")]
    public float returnSpeed = 2f;

    [Tooltip("Set this to 1 for the axis you want to pivot around. (Usually Z for a 2.5D game).")]
    public Vector3 rotationAxis = new Vector3(0, 0, 1);
    
    [Tooltip("Set this to 1 for the axis representing the length of the plank. (Usually X).")]
    public Vector3 lengthAxis = new Vector3(1, 0, 0);

    [Tooltip("Check this if the seesaw tilts UP towards you instead of DOWN.")]
    public bool invertTiltDirection = false;

    private float _currentAngle = 0f;
    private float _targetAngle = 0f;
    private Rigidbody _rb;
    private Quaternion _startRotation;
    
    // Track what is currently standing on the seesaw to fix Unity's flaky OnCollisionStay
    private System.Collections.Generic.List<Rigidbody> _objectsOnSeesaw = new System.Collections.Generic.List<Rigidbody>();

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        if (_rb == null) _rb = gameObject.AddComponent<Rigidbody>();
        
        _rb.isKinematic = true; 
        _startRotation = transform.localRotation;
    }

    void FixedUpdate()
    {
        // 1. Calculate tilt from all objects currently standing on the seesaw
        float totalTiltForce = 0f;

        // Clean up any objects that were destroyed
        _objectsOnSeesaw.RemoveAll(r => r == null || !r.gameObject.activeInHierarchy);

        foreach (Rigidbody objRb in _objectsOnSeesaw)
        {
            // Find exactly where they are relative to the center
            Vector3 localPos = transform.InverseTransformPoint(objRb.position);
            
            // Extract the distance along the length axis
            float distanceFromCenter = Vector3.Dot(localPos, lengthAxis);
            
            float force = distanceFromCenter * objRb.mass * massSensitivity;
            if (invertTiltDirection) force = -force;

            totalTiltForce += force;
        }

        // 2. Apply the force to the target angle
        if (_objectsOnSeesaw.Count > 0)
        {
            _targetAngle += totalTiltForce * Time.fixedDeltaTime;
        }

        // Clamp the target angle
        _targetAngle = Mathf.Clamp(_targetAngle, -maxTiltAngle, maxTiltAngle);

        // 3. Smoothly lerp the current angle towards the target
        _currentAngle = Mathf.Lerp(_currentAngle, _targetAngle, Time.fixedDeltaTime * returnSpeed * 3f);
        
        // Apply rotation
        Quaternion deltaRot = Quaternion.AngleAxis(_currentAngle, rotationAxis);
        _rb.MoveRotation(_startRotation * deltaRot);

        // 4. Gradually reset target angle to 0. If objects are on it, the force above will fight this decay.
        _targetAngle = Mathf.Lerp(_targetAngle, 0f, Time.fixedDeltaTime * returnSpeed);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.rigidbody != null && !_objectsOnSeesaw.Contains(collision.rigidbody))
        {
            _objectsOnSeesaw.Add(collision.rigidbody);
            
            // Parent the slime to the seesaw. Because the seesaw is a Uniform (1,1,1) Empty parent (Seesaw_Pivot),
            // this will seamlessly lock the slime's physics to the rotation without warping the slime's visual scale.
            collision.rigidbody.transform.SetParent(this.transform, true);
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.rigidbody != null)
        {
            _objectsOnSeesaw.Remove(collision.rigidbody);
            
            // When leaving the seesaw, unparent the slime back to the world root.
            collision.rigidbody.transform.SetParent(null, true);
        }
    }

    void OnDrawGizmos()
    {
        // Draw a red sphere exactly where the script thinks the "Center" (Pivot) is
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.position, 0.3f);

        // Draw a green line showing which direction it thinks the "Length" of the seesaw is
        Gizmos.color = Color.green;
        Vector3 endPoint = transform.position + transform.rotation * lengthAxis * 3f;
        Gizmos.DrawLine(transform.position, endPoint);
    }
}

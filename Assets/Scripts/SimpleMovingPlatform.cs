using UnityEngine;

public class SimpleMovingPlatform : MonoBehaviour
{
    private Vector3 _lastPosition;
    private Vector3 _currentDelta;

    private void Start()
    {
        _lastPosition = transform.position;
    }

    private void FixedUpdate()
    {
        // Calculate exactly how much the platform moved this physics frame
        _currentDelta = transform.position - _lastPosition;
        _lastPosition = transform.position;
    }

    // This is called constantly, but ONLY while the player is physically touching the platform
    private void OnCollisionStay(Collision collision)
    {
        // Check if the object touching us is the player
        if (collision.gameObject.CompareTag("Player") || (collision.transform.root != null && collision.transform.root.CompareTag("Player")))
        {
            // If the platform actually moved...
            if (_currentDelta.magnitude > 0.00001f)
            {
                // We physically push the player along with us without ever parenting them!
                // This prevents all the weird momentum and teleportation bugs.
                collision.transform.root.position += _currentDelta;
            }
        }
    }
}

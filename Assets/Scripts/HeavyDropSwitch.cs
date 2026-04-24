using UnityEngine;
using UnityEngine.Events;

public class HeavyDropSwitch : MonoBehaviour
{
    [Header("Drop Settings")]
    [Tooltip("How fast the player must be falling downwards to trigger the switch.")]
    public float requiredFallSpeed = 5f;
    [Tooltip("If true, the switch can only be pressed once.")]
    public bool triggerOnlyOnce = true;

    [Header("Button Animation")]
    [Tooltip("The Animator attached to this button.")]
    public Animator buttonAnimator;
    [Tooltip("The trigger parameter name in the Animator to play the push down animation.")]
    public string pushDownTriggerName = "PushDown";

    [Header("Events")]
    [Tooltip("Events triggered when the button is successfully pressed by a heavy drop (e.g., open a door, play a sound).")]
    public UnityEvent onHeavyDropTriggered;

    private bool _isPressed = false;

    private void OnCollisionEnter(Collision collision)
    {
        if (_isPressed && triggerOnlyOnce) return;

        // Check if it's the player
        if (collision.gameObject.CompareTag("Player"))
        {
            // The relative velocity tells us how hard the impact was.
            // Since the player is falling down, we check the relative velocity along the Y axis.
            // Alternatively, we can check the player's Rigidbody velocity before the collision.
            Rigidbody playerRb = collision.gameObject.GetComponent<Rigidbody>();
            
            // Note: collision.relativeVelocity could also be used, but sometimes it's less reliable 
            // depending on physics materials. A strong downward impact usually has a high magnitude.
            // We'll check both the relative velocity magnitude and the player's downward velocity.
            
            float impactSpeed = collision.relativeVelocity.magnitude;

            // If the impact is strong enough
            if (impactSpeed >= requiredFallSpeed)
            {
                TriggerSwitch();
            }
        }
    }

    // Alternatively, if the switch uses a Trigger Collider instead of a solid collider
    private void OnTriggerEnter(Collider other)
    {
        if (_isPressed && triggerOnlyOnce) return;

        if (other.CompareTag("Player"))
        {
            Rigidbody playerRb = other.attachedRigidbody;
            if (playerRb != null)
            {
                // If they are falling fast enough (velocity.y is negative when falling)
                if (playerRb.linearVelocity.y <= -requiredFallSpeed)
                {
                    TriggerSwitch();
                }
            }
        }
    }

    private void TriggerSwitch()
    {
        _isPressed = true;
        Debug.Log("Heavy Drop Switch Triggered!");

        // 1. Play the button's own animation
        if (buttonAnimator != null && !string.IsNullOrEmpty(pushDownTriggerName))
        {
            buttonAnimator.SetTrigger(pushDownTriggerName);
        }

        // 2. Trigger any other events (like another object's animation)
        onHeavyDropTriggered?.Invoke();
    }
}

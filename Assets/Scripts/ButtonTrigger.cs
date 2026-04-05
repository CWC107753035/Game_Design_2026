using UnityEngine;

public class ButtonTrigger : MonoBehaviour
{
    [Tooltip("Drag the DoorHinge object here")]
    public Animator doorAnimator;

    private bool _hasBeenPushed = false;

    // This runs when the player steps into the button's trigger area
    private void OnTriggerEnter(Collider other)
    {
        // Only open if the player touches it, and only open it once
        if (other.CompareTag("Player") && !_hasBeenPushed)
        {
            // "Open" must match the exact spelling of the Trigger in Step 3
            doorAnimator.SetTrigger("Open"); 
            _hasBeenPushed = true;
            
            Debug.Log("Button Pushed! Door is opening.");
        }
    }
}
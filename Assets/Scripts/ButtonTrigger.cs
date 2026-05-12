using UnityEngine;
using UnityEngine.Events;
using Slime;

public class ButtonTrigger : MonoBehaviour
{
    [Tooltip("Leave empty if you only want to use the On Triggered event.")]
    public Animator doorAnimator;
    public Animator buttonAnimator;

    [Header("Additional Actions")]
    [Tooltip("Hook up your ProgressiveAnimation or any other custom scripts here!")]
    public UnityEvent onTriggered;

    [Header("Audio")]
    [Tooltip("The sound to play when the button is pushed.")]
    public AudioClip buttonClickSound;
    private AudioSource _audioSource;

    private bool _hasBeenPushed = false;

    private void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
        }
    }

    // This runs when the player steps into the button's trigger area
    private void OnTriggerEnter(Collider other)
    {
        if (_hasBeenPushed)
            return;

        Slime_PBF slime = other.GetComponentInParent<Slime_PBF>();
        if (slime == null)
            return;

        if (slime.isFog)
            return;

        // Safely trigger the door if assigned
        if (doorAnimator != null)
        {
            doorAnimator.SetTrigger("open");
        }
        
        // Safely trigger the button if assigned
        if (buttonAnimator != null)
        {
            buttonAnimator.SetTrigger("push");
        }

        // Trigger any custom events (like ProgressiveAnimation.PlayNextSegment)
        onTriggered?.Invoke();

        if (_audioSource != null && buttonClickSound != null)
        {
            _audioSource.PlayOneShot(buttonClickSound);
        }

        _hasBeenPushed = true;
    }
}
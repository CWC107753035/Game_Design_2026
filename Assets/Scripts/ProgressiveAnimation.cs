using UnityEngine;

public class ProgressiveAnimation : MonoBehaviour
{
    [Tooltip("The Animator that plays the animation.")]
    public Animator targetAnimator;

    [Tooltip("Optional: If your Animator starts in an Idle state and needs a Trigger to begin, type the Trigger parameter name here (e.g., 'open').")]
    public string startTriggerName = "";

    [Tooltip("The exact name of the Animation State inside the Animator window. (Only needed if you DON'T use a Start Trigger)")]
    public string animationStateName = "LavaRise";

    [Tooltip("How many seconds of the animation to play each time the button is pressed.")]
    public float secondsPerTrigger = 5f;

    [Tooltip("The total length of the animation in seconds (e.g., 20).")]
    public float totalAnimationLength = 20f;

    private float _targetTime = 0f;
    private float _timeElapsed = 0f;
    private bool _isPlaying = false;
    private bool _hasStarted = false;

    private void Start()
    {
        if (targetAnimator != null)
        {
            // Pause the animator immediately at the start
            targetAnimator.speed = 0f;
            
            // If there's no trigger to start it, we manually force it to start at the beginning of the Lava state
            if (string.IsNullOrEmpty(startTriggerName))
            {
                targetAnimator.Play(animationStateName, 0, 0f);
            }
        }
    }

    // Call this method from your HeavyDropSwitch or CogSwitch UnityEvent!
    public void PlayNextSegment()
    {
        if (_targetTime >= totalAnimationLength)
        {
            Debug.Log("Animation has already fully completed!");
            return;
        }

        // Increase our target time
        _targetTime += secondsPerTrigger;
        
        // Cap the target time to the maximum length of the animation
        if (_targetTime > totalAnimationLength) 
        {
            _targetTime = totalAnimationLength;
        }

        _isPlaying = true;
        
        if (targetAnimator != null)
        {
            targetAnimator.speed = 1f; // Resume playing normal speed
            
            // If this is the very first time we are playing, fire the trigger to exit Idle!
            if (!_hasStarted && !string.IsNullOrEmpty(startTriggerName))
            {
                targetAnimator.SetTrigger(startTriggerName);
            }
        }
        
        _hasStarted = true;
    }

    private void Update()
    {
        if (_isPlaying && targetAnimator != null)
        {
            // We use standard game time to track how long it has been playing.
            // This is foolproof and doesn't rely on spelling the Animation State Name correctly!
            _timeElapsed += Time.deltaTime;
            
            if (_timeElapsed >= _targetTime)
            {
                // We've reached our target time segment! Pause the animator.
                targetAnimator.speed = 0f;
                _isPlaying = false;
                
                // Snap our tracker perfectly to the target in case of tiny frame rate overshoots
                _timeElapsed = _targetTime; 
            }
        }
    }
}

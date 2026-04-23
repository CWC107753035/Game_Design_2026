using UnityEngine;
using Slime;

public class IntermittentFire : MonoBehaviour
{
    [Header("Fire Timers")]
    [Tooltip("How long the fire stays active and emitting.")]
    public float timeOn = 2f;
    
    [Tooltip("How long the fire turns off and stops emitting.")]
    public float timeOff = 3f;

    [Header("Particle System")]
    [Tooltip("Drag your Fire Particle System here. Make sure 'Collision' and 'Send Collision Messages' are checked on the Particle System!")]
    public ParticleSystem fireParticles;

    private float _timer;
    private bool _isOn;

    private void Start()
    {
        // Start the trap in the ON state
        _isOn = true;
        _timer = timeOn;

        if (fireParticles != null)
        {
            if (!fireParticles.isPlaying) fireParticles.Play();

            // If the particle system is on a child object, it won't trigger OnParticleCollision here.
            // We dynamically attach a tiny script to the particles to bounce the collision message back!
            if (fireParticles.gameObject != this.gameObject)
            {
                var forwarder = fireParticles.gameObject.GetComponent<ParticleCollisionForwarder>();
                if (forwarder == null) forwarder = fireParticles.gameObject.AddComponent<ParticleCollisionForwarder>();
                forwarder.parentFire = this;
            }
        }
    }

    private void Update()
    {
        _timer -= Time.deltaTime;
        
        // When the timer runs out, flip the state!
        if (_timer <= 0f)
        {
            _isOn = !_isOn;
            _timer = _isOn ? timeOn : timeOff;

            if (fireParticles != null)
            {
                if (_isOn)
                {
                    fireParticles.Play();
                }
                else
                {
                    // Stop emitting new particles, but let existing ones naturally fade away
                    fireParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }
            }
        }
    }

    // Called either by Unity natively (if script is on particle object) OR by the Forwarder script.
    public void HandleParticleHit(GameObject other)
    {
        Slime_PBF slime = other.transform.root.GetComponentInChildren<Slime_PBF>();
        
        if (slime != null)
        {
            slime.HeatUp();
        }
    }

    private void OnParticleCollision(GameObject other)
    {
        HandleParticleHit(other);
    }
}

// A tiny helper script that gets dynamically slapped onto child particle systems
// to bounce the Unity message back up to the main script!
public class ParticleCollisionForwarder : MonoBehaviour
{
    public IntermittentFire parentFire;

    private void OnParticleCollision(GameObject other)
    {
        if (parentFire != null)
        {
            parentFire.HandleParticleHit(other);
        }
    }
}

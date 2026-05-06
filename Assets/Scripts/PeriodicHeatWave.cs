using UnityEngine;
using Slime;

public class PeriodicHeatWave : MonoBehaviour
{
    [Header("Heat Wave Settings")]
    [Tooltip("Time in seconds between each heat wave.")]
    public float heatInterval = 10f;
    
    [Tooltip("Optional: Initial delay before the first heat wave hits.")]
    public float initialDelay = 0f;

    [Header("VFX Settings")]
    [Tooltip("The fire particle systems to act as warning indicators.")]
    public ParticleSystem[] warningParticles;

    [Tooltip("How many seconds before the heat wave should the particles start?")]
    public float warningDuration = 3f;

    [Header("Lighting Settings")]
    [Tooltip("Optional: A Light (like a Directional Light) to change color during the warning.")]
    public Light directionalLight;

    [Tooltip("The color the light will smoothly transition to right before the heat hits.")]
    public Color heatColor = new Color(1f, 0.5f, 0.5f, 1f); // Reddish by default

    [Tooltip("How long it takes to fade from Red back to Normal after a heat wave.")]
    public float fadeToNormalDuration = 5f;

    [Tooltip("How long it takes to fade from Normal to Red before the next heat wave.")]
    public float fadeToRedDuration = 5f;

    private float timer;
    private bool isWarningActive = false;
    private Slime_PBF slime;
    private Color originalLightColor;

    private void Start()
    {
        timer = heatInterval + initialDelay;
        if (warningParticles != null)
        {
            foreach (var ps in warningParticles)
            {
                if (ps != null) ps.Stop();
            }
        }
        
        if (directionalLight != null)
        {
            originalLightColor = directionalLight.color;
        }

        // Attempt to find the slime at the start
        FindSlime();
    }

    private void Update()
    {
        timer -= Time.deltaTime;

        // 1. Smooth light transition with custom fade durations
        if (directionalLight != null && heatInterval > 0f)
        {
            float elapsed = heatInterval - timer;
            float fraction = 0f; // Default is Normal

            if (elapsed <= fadeToNormalDuration && fadeToNormalDuration > 0f)
            {
                // Fading from Red (1) to Normal (0)
                fraction = 1f - (elapsed / fadeToNormalDuration);
            }
            else if (timer <= fadeToRedDuration && fadeToRedDuration > 0f)
            {
                // Fading from Normal (0) to Red (1)
                fraction = 1f - (timer / fadeToRedDuration);
            }

            // Ensure fraction stays safely between 0 and 1
            fraction = Mathf.Clamp01(fraction);

            directionalLight.color = Color.Lerp(originalLightColor, heatColor, fraction);
        }

        // 2. Particle Warning Effect
        if (timer <= warningDuration && !isWarningActive)
        {
            StartWarning();
        }

        if (timer <= 0f)
        {
            ReleaseHeat();
            timer = heatInterval; // Reset timer for the next wave
            isWarningActive = false; // Reset warning state
        }
    }

    private void StartWarning()
    {
        isWarningActive = true;
        if (warningParticles != null)
        {
            foreach (var ps in warningParticles)
            {
                if (ps != null) ps.Play();
            }
        }
    }

    private void ReleaseHeat()
    {
        // Stop the particles when heat is released
        if (warningParticles != null)
        {
            foreach (var ps in warningParticles)
            {
                if (ps != null) ps.Stop();
            }
        }

        // If slime was destroyed or not found yet, try finding it again
        if (slime == null)
        {
            FindSlime();
        }

        if (slime != null)
        {
            // The HeatUp method already handles the logic of Ice -> Water -> Steam!
            slime.HeatUp();
            Debug.Log("Periodic Heat Wave Triggered! Slime heated up.");
        }
    }

    private void FindSlime()
    {
        slime = FindObjectOfType<Slime_PBF>();
    }
}

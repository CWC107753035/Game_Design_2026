using UnityEngine;

public class BGMManager : MonoBehaviour
{
    private static BGMManager instance;
    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        // If a BGMManager already exists from a previous scene...
        if (instance != null && instance != this)
        {
            // Check if the music track in the NEW scene is different from the OLD scene
            if (instance.audioSource.clip != this.audioSource.clip)
            {
                // The new scene has DIFFERENT music! 
                // Destroy the old manager, and let this new one take over.
                Destroy(instance.gameObject);
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                // The new scene has the SAME music!
                // Destroy this new duplicate so the original music keeps playing without restarting.
                Destroy(gameObject);
            }
            return;
        }

        // If this is the very first BGMManager
        instance = this;
        DontDestroyOnLoad(gameObject);
    }
}

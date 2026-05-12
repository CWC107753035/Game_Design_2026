using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class PreludeScreen : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("How long the fade-out transition takes.")]
    public float fadeDuration = 1f;

    [Tooltip("If true, the prelude only shows ONCE ever (uses PlayerPrefs to remember). If false, it shows every time.")]
    public bool showOnlyOnce = true;

    private CanvasGroup canvasGroup;
    private bool isDismissing = false;

    void Start()
    {
        // Check if the prelude has already been shown before
        if (showOnlyOnce && PlayerPrefs.GetInt("PreludeShown", 0) == 1)
        {
            // Already shown before — skip it entirely
            gameObject.SetActive(false);
            return;
        }

        // Set up the CanvasGroup for fading
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        // Pause the game while the prelude is showing
        Time.timeScale = 0f;
    }

    void Update()
    {
        if (isDismissing)
        {
            // Fade out
            canvasGroup.alpha -= Time.unscaledDeltaTime / fadeDuration;
            if (canvasGroup.alpha <= 0f)
            {
                Time.timeScale = 1f; // Unpause the game
                if (showOnlyOnce)
                {
                    PlayerPrefs.SetInt("PreludeShown", 1);
                    PlayerPrefs.Save();
                }
                gameObject.SetActive(false);
            }
            return;
        }

        // Dismiss on space bar press
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            isDismissing = true;
            canvasGroup.blocksRaycasts = false;
        }
    }
}


using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class OutroScreen : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("The name of the main menu scene to load after the outro.")]
    public string mainMenuSceneName = "1.Menu";

    [Tooltip("Drag the OutroCanvas (or OutroImage) GameObject here. It should start DISABLED in the hierarchy.")]
    public GameObject outroCanvasObject;

    [Tooltip("How long the fade-in takes when the outro appears.")]
    public float fadeInDuration = 1f;

    [Tooltip("How long the fade-out takes before loading the menu.")]
    public float fadeOutDuration = 1f;

    private CanvasGroup canvasGroup;
    private bool isActive = false;
    private bool isFadingIn = false;
    private bool isFadingOut = false;

    /// <summary>
    /// Call this from the magic circle's event to trigger the ending!
    /// This script should be on a SEPARATE always-active GameObject (e.g. an empty called "OutroManager").
    /// </summary>
    public void TriggerOutro()
    {
        if (outroCanvasObject == null)
        {
            Debug.LogError("[OutroScreen] outroCanvasObject is not assigned! Drag your OutroCanvas into the slot.");
            return;
        }

        Debug.Log("[OutroScreen] TriggerOutro called!");

        // Enable the canvas now
        outroCanvasObject.SetActive(true);

        // Get or add CanvasGroup
        canvasGroup = outroCanvasObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = outroCanvasObject.AddComponent<CanvasGroup>();

        isActive = true;
        isFadingIn = true;
        isFadingOut = false;
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;
        Time.timeScale = 0f; // Pause the game

        // Unlock the cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        if (!isActive) return;

        // Phase 1: Fade in the outro image
        if (isFadingIn)
        {
            canvasGroup.alpha += Time.unscaledDeltaTime / fadeInDuration;
            if (canvasGroup.alpha >= 1f)
            {
                canvasGroup.alpha = 1f;
                isFadingIn = false;
            }
            return;
        }

        // Phase 2: Wait for space bar to dismiss
        if (!isFadingOut)
        {
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                isFadingOut = true;
            }
            return;
        }

        // Phase 3: Fade out then load main menu
        canvasGroup.alpha -= Time.unscaledDeltaTime / fadeOutDuration;
        if (canvasGroup.alpha <= 0f)
        {
            Time.timeScale = 1f; // Unpause before loading
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}


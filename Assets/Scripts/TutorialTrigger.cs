using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using Slime;

/// <summary>
/// Trigger-based tutorial popup that displays a Canvas panel (containing a PNG
/// image) when the player enters the zone. The game pauses and the player
/// presses Space to dismiss. Each trigger only fires ONCE per scene load —
/// re-entering after dismissal does nothing. Reloading the scene
/// resets all tutorials.
///
/// Setup:
///   1. Create a UI Canvas → add a Panel (the popup).
///   2. Inside the Panel, add a UI Image and assign your tutorial PNG sprite.
///   3. Add an empty GameObject in the world with a Collider (Is Trigger = true).
///   4. Attach this script to that trigger GameObject.
///   5. Drag the Panel into the "popupPanel" field.
///   6. Set a unique TutorialID (e.g. "move_tutorial", "jump_tutorial").
///   7. The Panel should start DISABLED in the scene.
/// </summary>
public class TutorialTrigger : MonoBehaviour
{
    // ── Static tracking ────────────────────────────────────────────────
    // Tracks which tutorial IDs have already been shown this scene load.
    // Cleared automatically whenever a scene is loaded.
    private static HashSet<string> _shownThisLoad = new HashSet<string>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        // Domain-reload safety (enter play mode settings)
        _shownThisLoad = new HashSet<string>();
    }

    // ── Inspector fields ───────────────────────────────────────────────
    [Header("Tutorial Identity")]
    [Tooltip("A unique ID for this tutorial. Tutorials with the same ID share the 'shown once' state.")]
    public string tutorialID = "tutorial_default";

    [Header("UI Reference")]
    [Tooltip("The popup panel GameObject on your Canvas. Put your tutorial PNG Image as a child of this panel.")]
    public GameObject popupPanel;

    [Header("Fade Settings")]
    [Tooltip("Seconds to fade the popup in/out. Set to 0 for instant show/hide.")]
    public float fadeDuration = 0.3f;

    // ── Private state ──────────────────────────────────────────────────
    private CanvasGroup _canvasGroup;
    private Coroutine _activeRoutine;
    private bool _isShowing = false;
    private bool _isFadingIn = false;

    // ── Lifecycle ──────────────────────────────────────────────────────
    void Awake()
    {
        // Register the scene-loaded callback so the static set resets on reload.
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Clear the "already shown" set so tutorials replay after a scene reload.
        _shownThisLoad.Clear();
    }

    void Start()
    {
        // Ensure we have a CanvasGroup for smooth fading.
        if (popupPanel != null)
        {
            _canvasGroup = popupPanel.GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = popupPanel.AddComponent<CanvasGroup>();

            // Start hidden
            _canvasGroup.alpha = 0f;
            popupPanel.SetActive(false);
        }
    }

    void Update()
    {
        // While the tutorial is showing and fully faded in, wait for Space to dismiss.
        if (_isShowing && !_isFadingIn)
        {
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                HideTutorial();
            }
        }
    }

    // ── Trigger detection ──────────────────────────────────────────────
    // Uses GetComponentInParent<Slime_PBF>() for reliable multi-collider
    // detection — the same pattern used by ButtonTrigger and CameraAngleTrigger.
    void OnTriggerEnter(Collider other)
    {
        Slime_PBF slime = other.GetComponentInParent<Slime_PBF>();
        if (slime == null) return;

        // Already shown this scene load? Do nothing.
        if (_shownThisLoad.Contains(tutorialID)) return;

        ShowTutorial();
    }

    // ── Show / Hide logic ──────────────────────────────────────────────
    private void ShowTutorial()
    {
        if (popupPanel == null)
        {
            Debug.LogError($"[TutorialTrigger] '{tutorialID}' on {gameObject.name} is missing the popupPanel reference!");
            return;
        }

        // Mark as shown for this scene load
        _shownThisLoad.Add(tutorialID);
        _isShowing = true;

        // Activate and begin fade
        popupPanel.SetActive(true);

        // Pause the game while tutorial is showing
        Time.timeScale = 0f;

        // Stop any running fade/hide routine
        if (_activeRoutine != null) StopCoroutine(_activeRoutine);

        _activeRoutine = StartCoroutine(FadeIn());
    }

    private void HideTutorial()
    {
        if (!_isShowing) return;
        _isShowing = false;

        if (_activeRoutine != null) StopCoroutine(_activeRoutine);

        // Unpause before fading out
        Time.timeScale = 1f;

        _activeRoutine = StartCoroutine(FadeOut());
    }

    // ── Coroutines ─────────────────────────────────────────────────────
    private IEnumerator FadeIn()
    {
        _isFadingIn = true;

        // Fade in using unscaledDeltaTime since the game is paused
        if (fadeDuration > 0f)
        {
            _canvasGroup.alpha = 0f;
            while (_canvasGroup.alpha < 1f)
            {
                _canvasGroup.alpha += Time.unscaledDeltaTime / fadeDuration;
                yield return null;
            }
        }
        _canvasGroup.alpha = 1f;
        _isFadingIn = false;

        // Now waiting for player to press Space (handled in Update)
        _activeRoutine = null;
    }

    private IEnumerator FadeOut()
    {
        if (fadeDuration > 0f && _canvasGroup != null)
        {
            while (_canvasGroup.alpha > 0f)
            {
                _canvasGroup.alpha -= Time.unscaledDeltaTime / fadeDuration;
                yield return null;
            }
        }

        if (_canvasGroup != null) _canvasGroup.alpha = 0f;
        if (popupPanel != null) popupPanel.SetActive(false);
        _activeRoutine = null;
    }
}

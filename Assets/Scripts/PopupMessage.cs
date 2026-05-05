using UnityEngine;
using TMPro;
using System.Collections;

public class PopupMessage : MonoBehaviour
{
    [SerializeField] private GameObject popupPanel;
    [SerializeField] private TextMeshProUGUI messageText;

    private Coroutine _hideCoroutine;

    // Static wrapper to find all instances and show them
    public static void ShowAll(string message, float duration = 0f)
    {
        PopupMessage[] allPopups = Resources.FindObjectsOfTypeAll<PopupMessage>();
        foreach (var p in allPopups)
        {
            if (p.gameObject.scene.isLoaded) // Only affect objects in the active scene, not prefabs
            {
                p.InternalShow(message, duration);
            }
        }
    }

    // Static wrapper to find all instances and hide them
    public static void HideAll()
    {
        PopupMessage[] allPopups = Resources.FindObjectsOfTypeAll<PopupMessage>();
        foreach (var p in allPopups)
        {
            if (p.gameObject.scene.isLoaded)
            {
                p.InternalHide();
            }
        }
    }

    private void InternalShow(string message, float duration)
    {
        if (popupPanel == null) return; // Skip broken duplicates
        if (messageText == null) return;

        if (_hideCoroutine != null) StopCoroutine(_hideCoroutine);
        
        messageText.text = message;
        popupPanel.SetActive(true);

        if (duration > 0f && gameObject.activeInHierarchy)
        {
            _hideCoroutine = StartCoroutine(HideAfter(duration));
        }
    }

    private void InternalHide()
    {
        if (_hideCoroutine != null) { StopCoroutine(_hideCoroutine); _hideCoroutine = null; }
        if (popupPanel != null) popupPanel.SetActive(false);
    }

    private IEnumerator HideAfter(float duration)
    {
        yield return new WaitForSeconds(duration);
        if (popupPanel != null) popupPanel.SetActive(false);
        _hideCoroutine = null;
    }
}

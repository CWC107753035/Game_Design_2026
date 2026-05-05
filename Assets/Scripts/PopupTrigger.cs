using UnityEngine;
using TMPro;
using System.Collections;

public class PopupTrigger : MonoBehaviour
{
    [Header("UI References (Drag from Hierarchy)")]
    [Tooltip("Drag the PopupPanel GameObject here")]
    public GameObject directPopupPanel;
    
    [Tooltip("Drag the MessageText GameObject here")]
    public TextMeshProUGUI directMessageText;

    [Header("Message Settings")]
    [TextArea] public string message;
    [Tooltip("Auto-hide after this many seconds. Set to 0 to stay until player leaves the area.")]
    public float autoDuration = 0f;

    private bool _hasShown = false;
    private Coroutine _hideCoroutine;

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("PopupTrigger entered by: " + other.gameObject.name);

        bool isPlayer = other.CompareTag("Player") || 
                       (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag("Player"));
        
        if (!isPlayer) return;

        if (directPopupPanel == null || directMessageText == null)
        {
            Debug.LogError("PopupTrigger on " + gameObject.name + " is missing UI references! Please assign them in the Inspector.");
            return;
        }

        if (_hasShown) return;
        _hasShown = true;

        if (_hideCoroutine != null) StopCoroutine(_hideCoroutine);
        
        directMessageText.text = message;
        directPopupPanel.SetActive(true);

        if (autoDuration > 0f)
        {
            _hideCoroutine = StartCoroutine(HideAfter(autoDuration));
        }
    }

    void OnTriggerExit(Collider other)
    {
        bool isPlayer = other.CompareTag("Player") || 
                       (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag("Player"));
                       
        if (!isPlayer) return;

        _hasShown = false;

        if (autoDuration <= 0f && directPopupPanel != null)
        {
            if (_hideCoroutine != null) { StopCoroutine(_hideCoroutine); _hideCoroutine = null; }
            directPopupPanel.SetActive(false);
        }
    }

    private IEnumerator HideAfter(float duration)
    {
        yield return new WaitForSeconds(duration);
        if (directPopupPanel != null) directPopupPanel.SetActive(false);
        _hideCoroutine = null;
    }
}

using UnityEngine;
using TMPro;
using System.Collections;

public class PopupMessage : MonoBehaviour
{
    public static PopupMessage Instance { get; private set; }

    [SerializeField] private GameObject popupPanel;
    [SerializeField] private TextMeshProUGUI messageText;

    private Coroutine _hideCoroutine;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // Show message and stay until Hide() is called
    public void Show(string message)
    {
        if (_hideCoroutine != null) StopCoroutine(_hideCoroutine);
        messageText.text = message;
        popupPanel.SetActive(true);
    }

    // Show message then auto-hide after duration (seconds)
    public void Show(string message, float duration)
    {
        Show(message);
        _hideCoroutine = StartCoroutine(HideAfter(duration));
    }

    public void Hide()
    {
        if (_hideCoroutine != null) { StopCoroutine(_hideCoroutine); _hideCoroutine = null; }
        popupPanel.SetActive(false);
    }

    private IEnumerator HideAfter(float duration)
    {
        yield return new WaitForSeconds(duration);
        popupPanel.SetActive(false);
        _hideCoroutine = null;
    }
}

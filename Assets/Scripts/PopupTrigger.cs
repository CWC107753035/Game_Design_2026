using UnityEngine;

public class PopupTrigger : MonoBehaviour
{
    [TextArea] public string message;

    [Tooltip("Auto-hide after this many seconds. Set to 0 to stay until player leaves the area.")]
    public float autoDuration = 0f;

    private bool _hasShown = false;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || _hasShown) return;

        _hasShown = true;

        if (autoDuration > 0f)
            PopupMessage.Instance.Show(message, autoDuration);
        else
            PopupMessage.Instance.Show(message);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (autoDuration <= 0f)
            PopupMessage.Instance.Hide();
    }
}

using UnityEngine;
using Slime;

public class ButtonTrigger : MonoBehaviour
{
    public Animator doorAnimator;
    public Animator buttonAnimator;

    private bool _hasBeenPushed = false;

    // This runs when the player steps into the button's trigger area
    private void OnTriggerEnter(Collider other)
    {
        if (_hasBeenPushed)
            return;

        if (!other.CompareTag("Player"))
            return;

        Slime_PBF slime = other.GetComponentInParent<Slime_PBF>();
        if (slime != null && slime.isFog)
            return;

        doorAnimator.SetTrigger("open");
        buttonAnimator.SetTrigger("push");
        _hasBeenPushed = true;
    }
}
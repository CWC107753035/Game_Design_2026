using UnityEngine;

public class SlideDoor : MonoBehaviour
{
    [Header("Door Panels")]
    public Transform leftDoorPanel;
    public Transform rightDoorPanel;

    [Header("Open Offsets")]
    [Tooltip("How far the left door moves when fully open. Usually a local offset like (-2, 0, 0).")]
    public Vector3 leftDoorOpenOffset = new Vector3(-2f, 0f, 0f);
    
    [Tooltip("How far the right door moves when fully open. Usually a local offset like (2, 0, 0).")]
    public Vector3 rightDoorOpenOffset = new Vector3(2f, 0f, 0f);

    private Vector3 leftClosedPos;
    private Vector3 rightClosedPos;

    void Start()
    {
        // Remember the original "closed" positions when the game starts
        if (leftDoorPanel != null) leftClosedPos = leftDoorPanel.localPosition;
        if (rightDoorPanel != null) rightClosedPos = rightDoorPanel.localPosition;
    }

    /// <summary>
    /// Connect this method to the 'On Mechanism Progress' event of your CogSwitch!
    /// Progress goes from 0.0 (fully closed) to 1.0 (fully open).
    /// </summary>
    public void SetProgress(float progress)
    {
        // Clamp to be perfectly safe
        progress = Mathf.Clamp01(progress);

        if (leftDoorPanel != null)
        {
            Vector3 targetLocalPos = Vector3.Lerp(leftClosedPos, leftClosedPos + leftDoorOpenOffset, progress);
            
            // Check if it has a kinematic Rigidbody for safe physics movement
            Rigidbody leftRb = leftDoorPanel.GetComponent<Rigidbody>();
            if (leftRb != null && leftRb.isKinematic)
            {
                Vector3 worldPos = leftDoorPanel.parent != null ? leftDoorPanel.parent.TransformPoint(targetLocalPos) : targetLocalPos;
                leftRb.MovePosition(worldPos);
            }
            else
            {
                leftDoorPanel.localPosition = targetLocalPos;
            }
        }

        if (rightDoorPanel != null)
        {
            Vector3 targetLocalPos = Vector3.Lerp(rightClosedPos, rightClosedPos + rightDoorOpenOffset, progress);
            
            Rigidbody rightRb = rightDoorPanel.GetComponent<Rigidbody>();
            if (rightRb != null && rightRb.isKinematic)
            {
                Vector3 worldPos = rightDoorPanel.parent != null ? rightDoorPanel.parent.TransformPoint(targetLocalPos) : targetLocalPos;
                rightRb.MovePosition(worldPos);
            }
            else
            {
                rightDoorPanel.localPosition = targetLocalPos;
            }
        }
    }
}

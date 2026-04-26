using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (CheckpointManager.Instance != null)
            {
                CheckpointManager.Instance.SetCheckpoint(transform.position);
            }
            else
            {
                Debug.LogWarning("You touched a checkpoint, but there is no 'CheckpointManager' in this scene! Please add one.");
            }
        }
    }
}

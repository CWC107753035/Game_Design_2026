using UnityEngine;

public class PlayerRespawnTrigger : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("The tag of the object that should trigger the respawn. Usually 'Player'.")]
    public string targetTag = "Player";

    // Called when the player walks into a Trigger collider
    private void OnTriggerEnter(Collider other)
    {
        // Check if the object that entered is the player (either directly or its parent)
        if (other.CompareTag(targetTag) || (other.transform.root != null && other.transform.root.CompareTag(targetTag)))
        {
            RespawnPlayer();
        }
    }

    // Called when the player physically hits a solid Collision collider
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(targetTag) || (collision.transform.root != null && collision.transform.root.CompareTag(targetTag)))
        {
            RespawnPlayer();
        }
    }

    private void RespawnPlayer()
    {
        // Instead of reloading the entire scene (which resets all puzzle progress),
        // we just use your CheckpointManager to instantly teleport the player back!
        if (CheckpointManager.Instance != null)
        {
            CheckpointManager.Instance.RespawnPlayer();
            Debug.Log("Player hit respawn trigger. Respawning at last checkpoint.");
        }
        else
        {
            Debug.LogWarning("Player hit a respawn trigger, but no CheckpointManager was found in the scene!");
        }
    }
}

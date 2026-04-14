using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }

    private Vector3 _respawnPoint;
    private Transform _player;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        _player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (_player != null)
            _respawnPoint = _player.position;
    }

    public void SetCheckpoint(Vector3 position)
    {
        _respawnPoint = position;
        Debug.Log("Checkpoint saved at: " + position);
    }

    public void RespawnPlayer()
    {
        if (_player == null)
            _player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (_player == null) return;

        var rb = _player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        _player.position = _respawnPoint;
    }
}

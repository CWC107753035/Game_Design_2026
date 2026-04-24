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

        var controller = _player.GetComponent<Slime.ControllerTest>();
        if (controller != null)
        {
            controller.externalVelocity = Vector3.zero;
        }

        // 1. Force player back into base Slime form
        var slimePbf = _player.GetComponent<Slime.Slime_PBF>();
        if (slimePbf != null)
        {
            slimePbf.isFog = false;
            slimePbf.isFrozen = false;
        }

        Vector3 oldPosition = _player.position;

        // 2. Perform the position reset
        _player.position = _respawnPoint;
        Physics.SyncTransforms();

        // 3. Immediately translate the PBF particles just like the portal to prevent physics drag
        if (slimePbf != null)
        {
            Vector3 diff = _respawnPoint - oldPosition;
            slimePbf.TeleportSystem(diff);
        }

        // 4. Snap the camera instantly
        var cam = FindObjectOfType<PlayerCamera>();
        if (cam != null)
        {
            cam.SnapToTarget();
        }
    }
}

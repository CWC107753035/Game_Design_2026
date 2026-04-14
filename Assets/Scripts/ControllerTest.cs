using UnityEngine;
using UnityEngine.InputSystem;

namespace Slime
{
    public class ControllerTest : MonoBehaviour
    {
        [Range(1, 20)] public float speed = 10;
        [SerializeField] private float jumpHeight = 1.25f;
        [SerializeField] private float groundRayLength = 1.2f;
        [SerializeField] private float jumpBufferTime = 0.15f;
        [SerializeField] private float fogJumpCooldown = 2f; // Tune in Inspector

        private Rigidbody _rb;
        private Slime_PBF _slimePbf;
        private float _jumpCooldown = 0f;
        private float _jumpBufferTimer = 0f;

        void Start()
        {
            _rb = GetComponent<Rigidbody>();
            _slimePbf = GetComponent<Slime_PBF>();

            if (_rb != null)
                _rb.useGravity = false;
        }

        // Read jump in Update so FixedUpdate never misses a press
        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
                _jumpBufferTimer = jumpBufferTime;
        }

        // Two conditions must both be true to allow a jump:
        // 1. Ground ray detects ground
        // 2. Jump cooldown has elapsed (time buffer since last jump)
        private bool IsGrounded()
        {
            if (_jumpCooldown > 0f) return false;
            Vector3 origin = transform.position + Vector3.up * 0.1f;
            return Physics.Raycast(origin, Vector3.down, groundRayLength, ~0, QueryTriggerInteraction.Ignore);
        }

        void FixedUpdate()
        {
            if (_jumpCooldown > 0f)
                _jumpCooldown -= Time.fixedDeltaTime;
            if (_jumpBufferTimer > 0f)
                _jumpBufferTimer -= Time.fixedDeltaTime;

            ApplyManualGravity();
            HandleMovement();
        }

        private void ApplyManualGravity()
        {
            if (_slimePbf == null || _rb == null) return;

            float gravityVal = _slimePbf.isFog ? _slimePbf.fogGravity : _slimePbf.gravity;
            _rb.AddForce(Vector3.up * gravityVal, ForceMode.Acceleration);
        }

        private void HandleMovement()
        {
            if (_rb == null) return;

            Vector2 input = Vector2.zero;
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) input.y += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) input.y -= 1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) input.x -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) input.x += 1f;
            }

            Vector3 moveDirection = new Vector3(input.x, 0, input.y).normalized;
            Vector3 targetVelocity = moveDirection * speed;
            targetVelocity.y = _rb.linearVelocity.y;

            if (_jumpBufferTimer > 0f && IsGrounded())
            {
                float activeGravity = _slimePbf.isFog ? _slimePbf.fogGravity : _slimePbf.gravity;
                float requiredJumpVelocity = Mathf.Sqrt(2f * Mathf.Abs(activeGravity) * jumpHeight);
                targetVelocity.y = requiredJumpVelocity;
                _jumpCooldown = _slimePbf.isFog ? fogJumpCooldown : 0.12f;
                _jumpBufferTimer = 0f;
            }

            _rb.linearVelocity = targetVelocity;
        }
    }
}

using UnityEngine;
using UnityEngine.InputSystem;

namespace Slime
{
    public class ControllerTest : MonoBehaviour
    {
        [Range(1, 20)] public float speed = 10;
        [SerializeField] private float jumpHeight = 1.25f; // Target jump height in meters
        [SerializeField] private float groundRayLength = 1.2f; // Tune to match slime size

        private Rigidbody _rb;
        private Slime_PBF _slimePbf;
        private float _jumpCooldown = 0f; // Time until we can check ground again after a jump

        void Start()
        {
            _rb = GetComponent<Rigidbody>();
            _slimePbf = GetComponent<Slime_PBF>();

            // Disable Unity's default physics gravity so we can apply our custom PBF gravity consistently
            if (_rb != null)
                _rb.useGravity = false;
        }

        // Raycast straight down from slightly above center.
        private bool IsGrounded()
        {
            if (_jumpCooldown > 0f) return false; // Still in post-jump grace period
            Vector3 origin = transform.position + Vector3.up * 0.1f;
            return Physics.Raycast(origin, Vector3.down, groundRayLength, ~0, QueryTriggerInteraction.Ignore);
        }

        void FixedUpdate()
        {
            if (_jumpCooldown > 0f)
                _jumpCooldown -= Time.fixedDeltaTime;

            ApplyManualGravity();
            HandleMovement();
        }

        private void ApplyManualGravity()
        {
            if (_slimePbf == null || _rb == null) return;

            // Get gravity magnitude (expecting negative values from Slime_PBF)
            float gravityVal = _slimePbf.isFog ? _slimePbf.fogGravity : _slimePbf.gravity;
        
            // Apply gravity force manually to the Rigidbody
            _rb.AddForce(Vector3.up * gravityVal, ForceMode.Acceleration);
        }

        private void HandleMovement()
        {
            if (_rb == null) return;

            Vector2 input = Vector2.zero;
            bool jumpPressed = false;

            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) input.y += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) input.y -= 1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) input.x -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) input.x += 1f;
                jumpPressed = keyboard.spaceKey.wasPressedThisFrame;
            }

            Vector3 moveDirection = new Vector3(input.x, 0, input.y).normalized;
            Vector3 targetVelocity = moveDirection * speed;
            
            // Maintain current vertical velocity for gravity/jumping
            targetVelocity.y = _rb.linearVelocity.y;

            if (jumpPressed && IsGrounded())
            {
                // To maintain identical jump height H across different gravity values G:
                // Velocity = sqrt(2 * |G| * H)
                float currentGravityMag = Mathf.Abs(_slimePbf.isFog ? _slimePbf.fogGravity : _slimePbf.gravity);
                float requiredJumpVelocity = Mathf.Sqrt(2f * currentGravityMag * jumpHeight);

                targetVelocity.y = requiredJumpVelocity;
                _jumpCooldown = 0.5f; 
            }

            _rb.linearVelocity = targetVelocity;
        }
    }
}

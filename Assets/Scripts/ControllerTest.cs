using UnityEngine;
using UnityEngine.InputSystem;

namespace Slime
{
    public class ControllerTest : MonoBehaviour
    {
        [Range(1, 20)] public float speed = 10;
        [SerializeField] private float jumpForce = 5f;
        [SerializeField] private float groundRayLength = 1.2f; // tune to match slime size

        private Rigidbody _rb;
        private float _jumpCooldown = 0f; // time until we can check ground again after a jump

        void Start()
        {
            _rb = GetComponent<Rigidbody>();
        }

        // Raycast straight down from slightly above center.
        // Works regardless of which collider is active (sphere vs frozen mesh).
        private bool IsGrounded()
        {
            if (_jumpCooldown > 0f) return false; // still in post-jump grace period
            Vector3 origin = transform.position + Vector3.up * 0.1f;
            return Physics.Raycast(origin, Vector3.down, groundRayLength, ~0, QueryTriggerInteraction.Ignore);
        }

        void FixedUpdate()
        {
            if (_jumpCooldown > 0f)
                _jumpCooldown -= Time.fixedDeltaTime;

            HandleMovement();
        }

        private void HandleMovement()
        {
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
            targetVelocity.y = _rb.linearVelocity.y;

            if (jumpPressed && IsGrounded())
            {
                targetVelocity.y = jumpForce;
                _jumpCooldown = 1f; // block ground detection for 0.4s after jumping
            }

            _rb.linearVelocity = targetVelocity;
        }
    }
}

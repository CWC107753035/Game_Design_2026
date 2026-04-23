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

            // 1. Ice form creates wildly different mesh shapes.
            // We must find the absolute lowest physical point of the slime's body.
            float lowestY = transform.position.y;
            Collider[] cols = GetComponentsInChildren<Collider>();
            foreach (var c in cols)
            {
                if (c.enabled && !c.isTrigger)
                {
                    lowestY = Mathf.Min(lowestY, c.bounds.min.y);
                }
            }

            // 2. Cast a thick sphere (radius 0.25) from slightly above the lowest point.
            // This ensures we catch the floor regardless of if we are stretched tall or wide,
            // while ignoring our own weirdly-shaped ice colliders.
            Vector3 origin = new Vector3(transform.position.x, lowestY + 0.25f, transform.position.z);
            float radius = 0.25f;
            float checkDist = 0.3f;

            RaycastHit[] hits = Physics.SphereCastAll(origin, radius, Vector3.down, checkDist, ~0, QueryTriggerInteraction.Ignore);
            foreach (var hit in hits)
            {
                if (hit.collider.transform.root != this.transform.root)
                {
                    return true;
                }
            }
            return false;
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

        // Expose sliding momentum so external scripts can push the slime
        public Vector3 externalVelocity = Vector3.zero;
        public float slideDecay = 2f; // How fast sliding momentum wears off

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

            // Add sliding physics momentum purely
            targetVelocity.x += externalVelocity.x;
            targetVelocity.z += externalVelocity.z;

            // Preserve gravity and jump natively, unless the slope is aggressively forcing us DOWN
            if (externalVelocity.y < -0.1f)
            {
                // We are sliding downhill. We enforce an absolute downward constraint
                // so we "stick" perfectly to the ramp rather than flying off like a ski jump.
                targetVelocity.y = Mathf.Min(_rb.linearVelocity.y, externalVelocity.y);
            }
            else
            {
                targetVelocity.y = _rb.linearVelocity.y;
            }

            if (_jumpBufferTimer > 0f && IsGrounded())
            {
                float activeGravity = _slimePbf.isFog ? _slimePbf.fogGravity : _slimePbf.gravity;
                float requiredJumpVelocity = Mathf.Sqrt(2f * Mathf.Abs(activeGravity) * jumpHeight);
                targetVelocity.y = requiredJumpVelocity;
                _jumpCooldown = _slimePbf.isFog ? fogJumpCooldown : 0.12f;
                _jumpBufferTimer = 0f;
                
                // CRITICAL: We must stop forcing the player downwards so the jump can actually launch them into the air!
                externalVelocity.y = 0f; 
            }

            _rb.linearVelocity = targetVelocity;

            // Gradually slow down the sliding push so it eventually stops when they reach flat ground
            // Or if they hit a wall.
            externalVelocity = Vector3.MoveTowards(externalVelocity, Vector3.zero, slideDecay * Time.fixedDeltaTime);
        }
    }
}

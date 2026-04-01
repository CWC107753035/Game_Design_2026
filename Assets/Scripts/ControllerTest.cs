using UnityEngine;
using UnityEngine.InputSystem;

namespace Slime
{
    public class ControllerTest : MonoBehaviour
    {
        [Range(1, 20)] public float speed = 10;
        private Rigidbody _rb;

        void Start()
        {
            _rb = GetComponent<Rigidbody>();
        }

        void FixedUpdate()
        {
            HandleMovement();
        }
    
        private void HandleMovement()
        {
            Vector2 input = Vector2.zero;
            bool jump = false;

            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) input.y += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) input.y -= 1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) input.x -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) input.x += 1f;
                
                jump = keyboard.spaceKey.wasPressedThisFrame;

                if (input.sqrMagnitude > 0)
                {
                    // Debug.Log($"Input Detected: {input}");
                }
            }

            Vector3 moveDirection = new Vector3(input.x, 0, input.y).normalized;
            Vector3 targetVelocity = moveDirection * speed;
            
            // Maintain current vertical velocity for gravity/jumping
            targetVelocity.y = _rb.linearVelocity.y;

            if (jump)
            {
                targetVelocity.y = 5f; // Simple jump force
                Debug.Log("Slime Jumped!");
            }

            _rb.linearVelocity = targetVelocity;
        }
    }
}

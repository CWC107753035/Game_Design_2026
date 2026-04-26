using UnityEngine;
using UnityEngine.InputSystem;

public class LUController : MonoBehaviour
{
    public Animator animator;
    public float moveSpeed = 2f;
    public float rotateSpeed = 90f;
    public float runSpeed = 5f;

    void Update()
    {
        Vector2 moveInput = InputSystem.actions.FindAction("Move").ReadValue<Vector2>();

        float forwardInput = moveInput.y;
        float rightInput = moveInput.x;

        // 检测 Shift
        bool isRunningInput = Keyboard.current.leftShiftKey.isPressed;

        // 使用 currentSpeed 确保实际移动速度会变
        float currentSpeed = (isRunningInput && forwardInput > 0) ? runSpeed : moveSpeed;

        transform.Translate(0, 0, forwardInput * Time.deltaTime * currentSpeed);
        transform.Rotate(0, rightInput * Time.deltaTime * rotateSpeed, 0);

        if (animator != null)
        {
            if (animator != null)
            {
                // 核心修改：使用 Mathf.Abs 取绝对值
                // 只要 forwardInput 不为 0 (前后走) 或者 rightInput 不为 0 (左右转)
                // 都会判定为正在移动
                bool moving = Mathf.Abs(forwardInput) > 0.1f || Mathf.Abs(rightInput) > 0.1f;

                animator.SetBool("IsMoving", moving);
                animator.SetFloat("moveMult", forwardInput);
                animator.SetBool("IsRunning", isRunningInput && forwardInput > 0);
            }
           
        }
    }
}
using UnityEngine;
using UnityEngine.SceneManagement; // 必须引用场景管理命名空间

public class LevelResetTrigger : MonoBehaviour
{
    [Header("设置")]
    [Tooltip("指定触发重置的物体标签，通常设为 Player")]
    public string targetTag = "Player";

    // 当物体（带 Rigidbody）进入触发区域时执行
    private void OnTriggerEnter(Collider other)
    {
        // 检查碰撞到的物体是否是玩家
        if (other.CompareTag(targetTag))
        {
            ResetLevel();
        }
    }

    // 或者使用普通碰撞（如果物体不是 Trigger）
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(targetTag))
        {
            ResetLevel();
        }
    }

    private void ResetLevel()
    {
        // 获取当前活跃场景的名称并重新加载
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);

        Debug.Log("检测到玩家碰撞，正在重置关卡: " + currentSceneName);
    }
}
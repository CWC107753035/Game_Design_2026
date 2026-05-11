using UnityEngine;
using Slime;
using System.Collections;

public class PushableManager : MonoBehaviour
{
    [Header("位移参数")]
    public float pushDistance = 2.0f;    // 每次撞击移动的距离
    public float moveDuration = 0.3f;    // 移动平滑时间
    public float cooldown = 0.4f;        // 触发冷却

    [Header("碰撞检测")]
    public float minVelocity = 2.5f;
    public string targetLayerName = "Walls"; // 目标 Layer 名改为 Walls
    public float detectionRadiusRatio = 0.85f; // 检测球体的半径比例

    private bool isMoving = false;
    private int wallLayerMask;
    private float myRadius;

    void Start()
    {
        // 1. 获取 Walls 层的位掩码
        wallLayerMask = LayerMask.GetMask(targetLayerName);

        // 2. 自动计算物体半径（基于第一个找到的 Collider）
        Collider col = GetComponentInChildren<Collider>();
        if (col != null)
        {
            myRadius = col.bounds.extents.x;
        }
        else
        {
            myRadius = 0.5f;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isMoving) return;

        // 获取玩家脚本判定冰形态
        Slime_PBF player = collision.gameObject.GetComponentInParent<Slime_PBF>();

        if (player != null && player.isFrozen)
        {
            // 只有撞击速度足够快才触发
            if (collision.relativeVelocity.magnitude > minVelocity)
            {
                // 计算位移方向：从碰撞点指向物体中心
                Vector3 impactPoint = collision.contacts[0].point;
                Vector3 rawDir = (transform.position - impactPoint);
                rawDir.y = 0; // 锁定水平移动
                Vector3 moveDir = rawDir.normalized;

                // 3. 执行前方的墙体检测
                if (!IsPathBlocked(moveDir))
                {
                    StartCoroutine(PerformMove(moveDir));
                }
                else
                {
                    Debug.Log("<color=cyan>检测到 Walls 层物体，停止移动。</color>");
                }
            }
        }
    }

    private bool IsPathBlocked(Vector3 direction)
    {
        // 射线起点稍微前移，确保不射到自己
        Vector3 rayStart = transform.position + (direction * 0.1f);
        float checkDistance = pushDistance;

        RaycastHit hit;
        // 使用 SphereCast 探测前方是否有实体墙，忽略触发器
        if (Physics.SphereCast(rayStart, myRadius * detectionRadiusRatio, direction, out hit, checkDistance, wallLayerMask, QueryTriggerInteraction.Ignore))
        {
            return true;
        }
        return false;
    }

    private IEnumerator PerformMove(Vector3 direction)
    {
        isMoving = true;
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + direction * pushDistance;
        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / moveDuration;
            // 平滑移动曲线
            transform.position = Vector3.Lerp(startPos, endPos, Mathf.SmoothStep(0, 1, t));
            yield return null;
        }

        transform.position = endPos;
        yield return new WaitForSeconds(cooldown);
        isMoving = false;
    }

    // 在场景中可视化检测范围
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, transform.forward * pushDistance);
    }
}
using UnityEngine;
using Slime;

public class FracturedWall : MonoBehaviour
{
    [Header("碰撞设置")]
    [SerializeField] private float breakVelocity = 5f; // 触发碎裂的速度阈值
    [SerializeField] private float explosionForce = 500f; // 撞击时的冲力
    [SerializeField] private float explosionRadius = 2f; // 冲力影响范围

    private bool isBroken = false;
    private Rigidbody[] fragments;

    void Start()
    {
        // 获取所有子碎块的 Rigidbody
        fragments = GetComponentsInChildren<Rigidbody>();

        // 初始化：让碎块静止不动，不受重力影响
        foreach (var rb in fragments)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isBroken) return;

        // 获取玩家脚本
        Slime_PBF slime = collision.gameObject.GetComponentInParent<Slime_PBF>();

        if (slime != null && slime.isFrozen)
        {
            // 检查撞击速度
            if (collision.relativeVelocity.magnitude > breakVelocity)
            {
                Explode(collision.contacts[0].point);
            }
        }
    }

    private void Explode(Vector3 hitPoint)
    {
        isBroken = true;

        foreach (var rb in fragments)
        {
            // 激活物理引擎
            rb.isKinematic = false;
            rb.useGravity = true;

            // 给每个碎片施加一个向外的爆炸力，让碎裂更自然
            rb.AddExplosionForce(explosionForce, hitPoint, explosionRadius);

            // (可选) 几秒后销毁小碎片以节省性能
            Destroy(rb.gameObject, 5f);
        }

        // 碎裂后禁用父物体的碰撞器或自身脚本
        Debug.Log("冰墙已碎裂！");
    }
}
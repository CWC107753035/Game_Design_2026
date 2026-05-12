using UnityEngine;
using Slime;

public class FracturedWall : MonoBehaviour
{
    [Header("破壊設定")]
    [SerializeField] private float breakVelocity = 5f; // 壁を壊すのに必要な速度
    [SerializeField] private float explosionForce = 500f; // 爆発の力
    [SerializeField] private float explosionRadius = 2f; // 爆発の半径

    private bool isBroken = false;
    private Rigidbody[] fragments;
    private AudioClip breakSound;

    void Start()
    {
        // すべての子フラグメントの Rigidbody
        fragments = GetComponentsInChildren<Rigidbody>();

        // 最初はすべてのフラグメントを固定する
        foreach (var rb in fragments)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // Auto-load the ice crack sound from Resources folder — no need to drag it in manually!
        breakSound = Resources.Load<AudioClip>("ice_crack");
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isBroken) return;

        // スライムかチェック
        Slime_PBF slime = collision.gameObject.GetComponentInParent<Slime_PBF>();

        if (slime != null && slime.isFrozen)
        {
            // 速度が十分か確認
            if (collision.relativeVelocity.magnitude > breakVelocity)
            {
                Explode(collision.contacts[0].point);
            }
        }
    }

    private void Explode(Vector3 hitPoint)
    {
        isBroken = true;

        // Play the break sound as a 2D sound so the player always hears it clearly
        if (breakSound != null)
        {
            GameObject tempAudio = new GameObject("TempBreakAudio");
            AudioSource tempSource = tempAudio.AddComponent<AudioSource>();
            tempSource.spatialBlend = 0f; // 2D sound
            tempSource.PlayOneShot(breakSound);
            Destroy(tempAudio, breakSound.length + 0.1f);
        }

        foreach (var rb in fragments)
        {
            // フラグメントを解放
            rb.isKinematic = false;
            rb.useGravity = true;

            // 爆発力を加える
            rb.AddExplosionForce(explosionForce, hitPoint, explosionRadius);

            // フラグメントを時間後に削除
            Destroy(rb.gameObject, 5f);
        }

        Debug.Log("壁が破壊された！");
    }
}
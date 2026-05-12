using UnityEngine;
using Slime;
using System.Collections;

public class PushableWithSound : MonoBehaviour
{
    [Header("位移设置")]
    public float pushDistance = 2.0f;
    public float moveDuration = 0.35f;
    public float cooldown = 0.5f;

    [Header("音效设置")]
    public AudioSource audioSource;    // 拖入物体自带的 AudioSource
    public AudioClip pushClip;         // 拖入撞击移动的音效文件
    [Range(0, 1)] public float volume = 0.8f;

    [Header("检测设置")]
    public float minVelocity = 3.0f;
    public string wallLayerName = "Walls";

    [Header("侧面判定")]
    [Range(0, 1)] public float sideThreshold = 0.7f;

    private bool isMoving = false;
    private int wallLayerMask;

    void Start()
    {
        wallLayerMask = LayerMask.GetMask(wallLayerName);

        // 自动获取 AudioSource，如果没有则报错提醒
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isMoving) return;

        Slime_PBF player = collision.gameObject.GetComponentInParent<Slime_PBF>();

        if (player != null && player.isFrozen)
        {
            if (collision.relativeVelocity.magnitude > minVelocity)
            {
                Vector3 contactNormal = collision.contacts[0].normal;

                // 侧面撞击判定
                if (Mathf.Abs(contactNormal.y) < (1f - sideThreshold))
                {
                    Vector3 impactPoint = collision.contacts[0].point;
                    Vector3 pushDir = (transform.position - impactPoint);
                    pushDir.y = 0;
                    Vector3 moveDirection = pushDir.normalized;

                    if (!IsPathBlocked(moveDirection))
                    {
                        // 播放音效
                        PlayPushSound();
                        StartCoroutine(SmoothMove(moveDirection));
                    }
                }
            }
        }
    }

    void PlayPushSound()
    {
        if (audioSource != null && pushClip != null)
        {
            // 使用 PlayOneShot 可以重叠播放，且不会因为脚本逻辑切断声音
            audioSource.PlayOneShot(pushClip, volume);
        }
    }

    // ... 保持之前的 IsPathBlocked 和 SmoothMove 逻辑不变 ...
    private bool IsPathBlocked(Vector3 direction)
    {
        float rayLength = pushDistance + 0.1f;
        return Physics.Raycast(transform.position, direction, rayLength, wallLayerMask);
    }

    private IEnumerator SmoothMove(Vector3 direction)
    {
        isMoving = true;
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + direction * pushDistance;
        float elapsed = 0f;
        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, endPos, Mathf.SmoothStep(0, 1, elapsed / moveDuration));
            yield return null;
        }
        transform.position = endPos;
        yield return new WaitForSeconds(cooldown);
        isMoving = false;
    }
}
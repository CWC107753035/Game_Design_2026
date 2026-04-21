using UnityEngine;
using TMPro; // 必须引用 TMP 命名空间
using System.Collections; // 必须引用协程命名空间

public class PlayerDetection : MonoBehaviour
{
    [Header("设置要显示的 TMP 文字")]
    public TextMeshPro textMesh; // 直接拖入 TextMeshPro 组件

    [Header("淡入淡出速度")]
    public float fadeSpeed = 2f;

    private Coroutine fadeCoroutine;

    void Start()
    {
        // 初始状态：将文字透明度设为 0（不可见）
        if (textMesh != null)
        {
            Color c = textMesh.color;
            c.a = 0;
            textMesh.color = c;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            StartFade(1f); // 开始淡入到 1 (不透明)
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            StartFade(0f); // 开始淡出到 0 (透明)
        }
    }

    // 辅助方法：确保同一时间只有一个淡入淡出在运行
    void StartFade(float targetAlpha)
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeRoutine(targetAlpha));
    }

    IEnumerator FadeRoutine(float targetAlpha)
    {
        Color c = textMesh.color;
        while (!Mathf.Approximately(c.a, targetAlpha))
        {
            // 使用 MoveTowards 平滑改变 Alpha 值
            c.a = Mathf.MoveTowards(c.a, targetAlpha, fadeSpeed * Time.deltaTime);
            textMesh.color = c;
            yield return null; // 等待下一帧
        }
    }
}
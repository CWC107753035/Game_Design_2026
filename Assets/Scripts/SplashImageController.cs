using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SplashImageController : MonoBehaviour
{
    public RawImage splashImage;    // 拖入你的图片
    public float displayTime = 3.0f; // 显示时长
    public float fadeSpeed = 1.5f;   // 淡出速度

    void Start()
    {
        // 确保游戏开始时图片是可见的
        if (splashImage != null)
        {
            StartCoroutine(FadeOutProcess());
        }
    }

    IEnumerator FadeOutProcess()
    {
        // 1. 等待预设的停留时间
        yield return new WaitForSeconds(displayTime);

        // 2. 逐渐降低透明度实现淡出
        Color tempColor = splashImage.color;
        while (tempColor.a > 0)
        {
            tempColor.a -= Time.deltaTime * fadeSpeed;
            splashImage.color = tempColor;
            yield return null;
        }

        // 3. 彻底隐藏图片物体，释放交互
        splashImage.gameObject.SetActive(false);
    }
}
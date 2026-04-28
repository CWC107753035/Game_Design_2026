using UnityEngine;

public class IntermittentFire : MonoBehaviour
{
    [Header("时间控制 (秒)")]
    [Tooltip("游戏开始后，等待多久才开始第一次喷射")]
    public float initialDelay = 0f;

    [Tooltip("火焰单次喷射的持续时间")]
    public float timeOn = 2f;

    [Tooltip("两次喷射之间的熄灭时间")]
    public float timeOff = 3f;

    [Header("粒子系统")]
    public ParticleSystem fireParticles;

    private float _timer;
    private bool _isOn;
    private bool _inInitialDelay;

    private void Start()
    {
        // 初始化计时逻辑
        if (initialDelay > 0)
        {
            _inInitialDelay = true;
            _isOn = false;
            _timer = initialDelay;
        }
        else
        {
            _inInitialDelay = false;
            _isOn = true;
            _timer = timeOn;
        }

        // 初始视觉状态
        UpdateFireVisuals();
    }

    private void Update()
    {
        _timer -= Time.deltaTime;

        if (_timer <= 0f)
        {
            if (_inInitialDelay)
            {
                _inInitialDelay = false;
                _isOn = true;
                _timer = timeOn;
            }
            else
            {
                _isOn = !_isOn;
                _timer = _isOn ? timeOn : timeOff;
            }

            UpdateFireVisuals();
        }
    }

    private void UpdateFireVisuals()
    {
        if (fireParticles != null)
        {
            if (_isOn)
                fireParticles.Play();
            else
                // 停止发射新粒子，但允许已有的粒子飞完
                fireParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }
}
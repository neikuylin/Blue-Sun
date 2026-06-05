using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("特效/爆炸控制器")]
public sealed class 爆炸控制器 : MonoBehaviour
{
    [Header("生命周期")]
    [SerializeField] private float 爆炸持续时间 = 1f;
    [SerializeField] private float 光源衰减时间 = 1f;

    [Header("光源衰减")]
    [SerializeField] private Light[] 衰减光源列表;
    [SerializeField] private bool 结束时关闭光源 = true;

    private float 已播放时间;
    private float[] 初始光源强度列表;
    private float[] 初始光源范围列表;
    private ParticleSystem[] 粒子系统列表;

    private void Awake()
    {
        读取粒子系统();
        记录初始光源参数();
    }

    private void OnEnable()
    {
        已播放时间 = 0f;
        读取粒子系统();
        应用爆炸持续时间();
        记录初始光源参数();
        应用光源衰减(1f);
    }

    private void Update()
    {
        float lightFadeDuration = Mathf.Max(0.0001f, 光源衰减时间);
        已播放时间 += Time.deltaTime;
        float remain01 = Mathf.Clamp01(1f - 已播放时间 / lightFadeDuration);
        应用光源衰减(remain01);

        if (已播放时间 >= lightFadeDuration && 结束时关闭光源)
        {
            关闭光源();
        }
    }

    private void 记录初始光源参数()
    {
        int count = 衰减光源列表 != null ? 衰减光源列表.Length : 0;
        if (初始光源强度列表 == null || 初始光源强度列表.Length != count)
        {
            初始光源强度列表 = new float[count];
            初始光源范围列表 = new float[count];
        }

        for (int i = 0; i < count; i++)
        {
            Light targetLight = 衰减光源列表[i];
            if (targetLight == null)
            {
                初始光源强度列表[i] = 0f;
                初始光源范围列表[i] = 0f;
                continue;
            }

            初始光源强度列表[i] = targetLight.intensity;
            初始光源范围列表[i] = targetLight.range;
            targetLight.enabled = true;
        }
    }

    private void 读取粒子系统()
    {
        粒子系统列表 = GetComponentsInChildren<ParticleSystem>(true);
    }

    private void 应用爆炸持续时间()
    {
        if (粒子系统列表 == null)
        {
            return;
        }

        float duration = Mathf.Max(0.0001f, 爆炸持续时间);
        for (int i = 0; i < 粒子系统列表.Length; i++)
        {
            ParticleSystem particleSystem = 粒子系统列表[i];
            if (particleSystem == null)
            {
                continue;
            }

            ParticleSystem.MainModule main = particleSystem.main;
            bool shouldPlay = particleSystem.isPlaying || main.playOnAwake;
            particleSystem.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            main.duration = duration;
            if (shouldPlay)
            {
                particleSystem.Play(false);
            }
        }
    }

    private void 应用光源衰减(float remain01)
    {
        if (衰减光源列表 == null || 初始光源强度列表 == null || 初始光源范围列表 == null)
        {
            return;
        }

        int count = Mathf.Min(衰减光源列表.Length, Mathf.Min(初始光源强度列表.Length, 初始光源范围列表.Length));
        for (int i = 0; i < count; i++)
        {
            Light targetLight = 衰减光源列表[i];
            if (targetLight == null)
            {
                continue;
            }

            targetLight.intensity = Mathf.Max(0f, 初始光源强度列表[i] * remain01);
            targetLight.range = Mathf.Max(0f, 初始光源范围列表[i] * remain01);
        }
    }

    private void 关闭光源()
    {
        if (衰减光源列表 == null)
        {
            return;
        }

        for (int i = 0; i < 衰减光源列表.Length; i++)
        {
            Light targetLight = 衰减光源列表[i];
            if (targetLight != null)
            {
                targetLight.enabled = false;
            }
        }
    }
}

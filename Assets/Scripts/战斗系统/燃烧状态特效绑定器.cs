using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("特效/燃烧状态特效绑定器")]
public sealed class 燃烧状态特效绑定器 : MonoBehaviour
{
    private const string ParticleObjectName = "燃烧状态粒子";

    private readonly List<ParticleSystem> 粒子系统列表 = new List<ParticleSystem>();
    private readonly List<ParticleSystemRenderer> 粒子渲染器列表 = new List<ParticleSystemRenderer>();
    private readonly List<SkinnedMeshRenderer> 蒙皮网格列表 = new List<SkinnedMeshRenderer>();
    private 燃烧状态特效全局配置 当前配置;

    private void OnEnable()
    {
        当前配置 = 燃烧状态特效全局配置.LoadDefault();
        if (当前配置 == null)
        {
            Debug.LogWarning($"[燃烧状态特效] {name} 找不到 Resources/燃烧状态特效全局配置。", this);
            return;
        }

        绑定角色蒙皮网格();
        确保粒子系统();
        应用粒子参数();
        播放粒子();
    }

    private void OnDisable()
    {
        for (int i = 0; i < 粒子系统列表.Count; i++)
        {
            ParticleSystem particleSystem = 粒子系统列表[i];
            if (particleSystem != null)
            {
                particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }

    private void OnValidate()
    {
        if (Application.isPlaying && 粒子系统列表.Count > 0)
        {
            当前配置 = 燃烧状态特效全局配置.LoadDefault();
            应用粒子参数();
        }
    }

    private void 确保粒子系统()
    {
        粒子系统列表.Clear();
        粒子渲染器列表.Clear();
        for (int i = 0; i < 蒙皮网格列表.Count; i++)
        {
            Transform existing = transform.Find($"{ParticleObjectName}_{i + 1}");
            if (existing == null)
            {
                GameObject particleObject = new GameObject($"{ParticleObjectName}_{i + 1}");
                particleObject.transform.SetParent(transform, false);
                existing = particleObject.transform;
            }

            ParticleSystem particleSystem = existing.GetComponent<ParticleSystem>();
            if (particleSystem == null)
            {
                particleSystem = existing.gameObject.AddComponent<ParticleSystem>();
            }

            ParticleSystemRenderer particleRenderer = existing.GetComponent<ParticleSystemRenderer>();
            if (particleRenderer == null)
            {
                particleRenderer = existing.gameObject.AddComponent<ParticleSystemRenderer>();
            }

            粒子系统列表.Add(particleSystem);
            粒子渲染器列表.Add(particleRenderer);
        }
    }

    private void 绑定角色蒙皮网格()
    {
        蒙皮网格列表.Clear();
        SkinnedMeshRenderer[] renderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        收集蒙皮网格(renderers, 蒙皮网格列表);
        if (蒙皮网格列表.Count == 0)
        {
            Debug.LogWarning($"[燃烧状态特效] {name} 的当前物体和子物体没有可用 SkinnedMeshRenderer，无法从角色表面发射燃烧粒子。", this);
        }
    }

    private static void 收集蒙皮网格(SkinnedMeshRenderer[] renderers, List<SkinnedMeshRenderer> target)
    {
        if (renderers == null || target == null)
        {
            return;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            SkinnedMeshRenderer renderer = renderers[i];
            if (renderer != null && renderer.sharedMesh != null && renderer.enabled)
            {
                target.Add(renderer);
            }
        }
    }

    private void 应用粒子参数()
    {
        if (当前配置 == null)
        {
            return;
        }

        int count = Mathf.Min(粒子系统列表.Count, 蒙皮网格列表.Count);
        if (count == 0)
        {
            return;
        }

        int ratePerRenderer = Mathf.Max(1, Mathf.CeilToInt(当前配置.火焰数量 / (float)count));
        int maxParticlesPerRenderer = Mathf.Max(1, Mathf.CeilToInt(当前配置.最大粒子数 / (float)count));
        for (int i = 0; i < count; i++)
        {
            应用单个粒子参数(粒子系统列表[i], i < 粒子渲染器列表.Count ? 粒子渲染器列表[i] : null, 蒙皮网格列表[i], ratePerRenderer, maxParticlesPerRenderer);
        }
    }

    private void 应用单个粒子参数(
        ParticleSystem particleSystem,
        ParticleSystemRenderer particleRenderer,
        SkinnedMeshRenderer skinnedMeshRenderer,
        int rateOverTime,
        int maxParticles)
    {
        if (particleSystem == null || skinnedMeshRenderer == null)
        {
            return;
        }

        if (particleSystem.isPlaying)
        {
            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        ParticleSystem.MainModule main = particleSystem.main;
        main.loop = true;
        main.playOnAwake = true;
        main.duration = 1f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = maxParticles;
        main.startLifetime = 当前配置.火焰生命周期;
        main.startSpeed = 当前配置.上飘速度;
        main.startSize = new ParticleSystem.MinMaxCurve(
            Mathf.Max(0.0001f, 当前配置.火焰大小 - 当前配置.火焰大小浮动),
            当前配置.火焰大小 + 当前配置.火焰大小浮动);
        main.startColor = new Color(1f, 1f, 1f, 当前配置.火焰颜色.a);
        main.gravityModifier = -0.08f;

        ParticleSystem.EmissionModule emission = particleSystem.emission;
        emission.enabled = true;
        emission.rateOverTime = rateOverTime;

        ParticleSystem.ShapeModule shape = particleSystem.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.SkinnedMeshRenderer;
        shape.skinnedMeshRenderer = skinnedMeshRenderer;
        shape.radiusThickness = 1f;
        shape.randomPositionAmount = 当前配置.表面散布厚度;

        ParticleSystem.NoiseModule noise = particleSystem.noise;
        noise.enabled = true;
        noise.strength = 0.08f;
        noise.frequency = 1.2f;
        noise.scrollSpeed = 0.4f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(当前配置.中心颜色, 0f),
                new GradientColorKey(当前配置.外围颜色, 1f)
            },
            new[]
            {
                new GradientAlphaKey(当前配置.火焰颜色.a, 0f),
                new GradientAlphaKey(当前配置.结束透明度, 1f)
            });
        colorOverLifetime.color = gradient;

        ParticleSystem.TextureSheetAnimationModule textureSheetAnimation = particleSystem.textureSheetAnimation;
        textureSheetAnimation.enabled = true;
        textureSheetAnimation.mode = ParticleSystemAnimationMode.Grid;
        textureSheetAnimation.numTilesX = Mathf.Max(1, 当前配置.贴图横向格数);
        textureSheetAnimation.numTilesY = Mathf.Max(1, 当前配置.贴图纵向格数);
        textureSheetAnimation.animation = ParticleSystemAnimationType.WholeSheet;
        textureSheetAnimation.useRandomRow = false;
        textureSheetAnimation.frameOverTime = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0f, 1f, 0.9999f));
        textureSheetAnimation.startFrame = new ParticleSystem.MinMaxCurve(0f);
        textureSheetAnimation.cycleCount = Mathf.Max(1, 当前配置.贴图动画循环次数);
        textureSheetAnimation.fps = Mathf.Max(1, 当前配置.贴图动画帧率);

        if (particleRenderer != null)
        {
            if (当前配置.粒子网格 == null)
            {
                Debug.LogWarning($"[燃烧状态特效] {name} 的全局配置没有绑定粒子网格，Mesh 粒子无法显示。", this);
                return;
            }

            particleRenderer.renderMode = ParticleSystemRenderMode.Mesh;
            particleRenderer.mesh = 当前配置.粒子网格;
            particleRenderer.sortMode = ParticleSystemSortMode.Distance;
            particleRenderer.sortingOrder = 1;
            particleRenderer.maxParticleSize = 0.35f;
            particleRenderer.lengthScale = 2f;
            if (当前配置.火焰材质 != null)
            {
                particleRenderer.sharedMaterial = 当前配置.火焰材质;
            }
        }
    }

    private void 播放粒子()
    {
        for (int i = 0; i < 粒子系统列表.Count; i++)
        {
            ParticleSystem particleSystem = 粒子系统列表[i];
            if (particleSystem == null)
            {
                continue;
            }

            particleSystem.Play(true);
        }
    }
}

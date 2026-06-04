using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DisallowMultipleComponent]
[AddComponentMenu("特效/武器火焰附魔控制器")]
public sealed class 武器火焰附魔控制器 : MonoBehaviour, 武器特效开关接口
{
    private const string 全局配置路径 = "武器火焰附魔全局配置";
    private const string 火星粒子物体名 = "火焰附魔火星粒子";
    private const string Trail物体名 = "火焰附魔Trail拖尾";

    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int DarkFireColorId = Shader.PropertyToID("_DarkFireColor");
    private static readonly int MainFireColorId = Shader.PropertyToID("_MainFireColor");
    private static readonly int CoreFireColorId = Shader.PropertyToID("_CoreFireColor");
    private static readonly int FireIntensityId = Shader.PropertyToID("_FireIntensity");
    private static readonly int FireSpeedId = Shader.PropertyToID("_FireSpeed");
    private static readonly int FireScaleId = Shader.PropertyToID("_FireScale");
    private static readonly int FlowDirectionId = Shader.PropertyToID("_FlowDirection");
    private static readonly int OriginalKeepId = Shader.PropertyToID("_OriginalKeep");
    private static readonly int OuterFireRangeId = Shader.PropertyToID("_OuterFireRange");
    private static readonly int OuterFireIntensityId = Shader.PropertyToID("_OuterFireIntensity");
    private static readonly int FlickerStrengthId = Shader.PropertyToID("_FlickerStrength");
    private static readonly int FlickerSpeedId = Shader.PropertyToID("_FlickerSpeed");
    private static readonly int MetallicId = Shader.PropertyToID("_Metallic");
    private static readonly int GlossinessId = Shader.PropertyToID("_Glossiness");
    private static readonly int SparkCoreColorId = Shader.PropertyToID("_CoreColor");
    private static readonly int SparkGlowColorId = Shader.PropertyToID("_GlowColor");
    private static readonly int SparkZTestId = Shader.PropertyToID("_ZTest");
    private static readonly int TrailColorAId = Shader.PropertyToID("_ColorA");
    private static readonly int TrailColorBId = Shader.PropertyToID("_ColorB");
    private static readonly int TrailSoftnessId = Shader.PropertyToID("_Softness");
    private static readonly int TrailNoiseScaleId = Shader.PropertyToID("_NoiseScale");
    private static readonly int TrailNoiseStrengthId = Shader.PropertyToID("_NoiseStrength");
    private static readonly int TrailIntensityId = Shader.PropertyToID("_Intensity");
    private static readonly int TrailZTestId = Shader.PropertyToID("_ZTest");

    [SerializeField] private bool 启用附魔;
    [SerializeField, HideInInspector] private Material[] 原始材质列表;

    private 武器火焰附魔全局配置 全局配置缓存;
    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;
    private MaterialPropertyBlock propertyBlock;
    private MaterialPropertyBlock 火星粒子参数块;
    private ParticleSystem 火星粒子系统;
    private ParticleSystemRenderer 火星粒子渲染器;
    private Material 火星粒子运行时材质;
    private Material 火星粒子运行时来源材质;
    private TrailRenderer trailRenderer;
    private Transform trailTransform;
    private Material trail运行时材质;
    private Material trail运行时来源材质;
    private MaterialPropertyBlock trail参数块;
    private Vector3 trail上次位置;
    private bool trail已采样;
    private bool trail缺少材质已警告;

#if UNITY_EDITOR
    private double 上次编辑器预览时间;
    private bool 已请求编辑器延迟应用;
#endif

    public bool 当前启用附魔 => 启用附魔;
    public Material 当前火焰材质 => 取全局配置() != null ? 取全局配置().火焰材质 : null;
    public Material[] 当前原始材质列表 => 原始材质列表;
    public 武器火焰附魔全局配置 当前全局配置 => 取全局配置();

#if UNITY_EDITOR
    private void 请求编辑器延迟应用()
    {
        if (已请求编辑器延迟应用)
        {
            return;
        }

        已请求编辑器延迟应用 = true;
        EditorApplication.delayCall += 编辑器延迟应用;
    }

    private void 编辑器延迟应用()
    {
        已请求编辑器延迟应用 = false;
        if (this == null || Application.isPlaying)
        {
            return;
        }

        确保编辑状态特效子物体();
        应用附魔设置();
    }

    private void 确保编辑状态特效子物体()
    {
        if (Application.isPlaying)
        {
            return;
        }

        if (PrefabUtility.IsPartOfPrefabAsset(gameObject))
        {
            return;
        }

        Transform spark = 取或创建编辑状态子物体(火星粒子物体名);
        if (spark != null)
        {
            确保编辑状态组件<ParticleSystem>(spark);
        }

        Transform trail = 取或创建编辑状态子物体(Trail物体名);
        if (trail != null)
        {
            确保编辑状态组件<TrailRenderer>(trail);
        }
    }

    private Transform 取或创建编辑状态子物体(string childName)
    {
        Transform child = transform.Find(childName);
        if (child != null)
        {
            return child;
        }

        GameObject childObject = new GameObject(childName);
        Undo.RegisterCreatedObjectUndo(childObject, $"创建{childName}");
        childObject.transform.SetParent(transform, false);
        childObject.transform.localPosition = Vector3.zero;
        childObject.transform.localRotation = Quaternion.identity;
        childObject.transform.localScale = Vector3.one;
        EditorUtility.SetDirty(gameObject);
        EditorUtility.SetDirty(childObject);
        return childObject.transform;
    }

    private static T 确保编辑状态组件<T>(Transform child) where T : Component
    {
        T component = child.GetComponent<T>();
        if (component != null)
        {
            return component;
        }

        component = Undo.AddComponent<T>(child.gameObject);
        EditorUtility.SetDirty(child.gameObject);
        return component;
    }
#endif

    private void Reset()
    {
        取组件和参数块();
#if UNITY_EDITOR
        确保编辑状态特效子物体();
#endif
        记录原始材质列表();
        应用附魔设置();
    }

    private void OnEnable()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            上次编辑器预览时间 = EditorApplication.timeSinceStartup;
            EditorApplication.update -= 编辑器预览更新;
            EditorApplication.update += 编辑器预览更新;
            请求编辑器延迟应用();
            return;
        }
#endif

        应用附魔设置();
    }

    private void OnDisable()
    {
#if UNITY_EDITOR
        EditorApplication.update -= 编辑器预览更新;
#endif

        还原原始材质();
        清空Trail拖尾();
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            请求编辑器延迟应用();
            return;
        }
#endif

        应用附魔设置();
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        更新Trail火焰拖尾();
    }

    [ContextMenu("应用武器火焰附魔")]
    public void 应用附魔设置()
    {
        取组件和参数块();
#if UNITY_EDITOR
        确保编辑状态特效子物体();
#endif

        if (meshRenderer == null)
        {
            Debug.LogWarning($"[武器火焰附魔] {name} 缺少 MeshRenderer，无法应用3D武器火焰附魔。", this);
            return;
        }

        武器火焰附魔全局配置 config = 取全局配置();
        if (config == null)
        {
            Debug.LogWarning($"[武器火焰附魔] {name} 找不到 Resources/{全局配置路径}，无法应用火焰附魔。", this);
            return;
        }

        if (!启用附魔)
        {
            还原原始材质();
            设置火星粒子子物体启用(false);
            设置Trail子物体启用(false);
            return;
        }

        记录原始材质列表();
        if (config.火焰材质 == null)
        {
            Debug.LogWarning($"[武器火焰附魔] 全局配置没有指定火焰材质，无法应用火焰附魔。", this);
            return;
        }

        应用火焰材质到全部材质槽(config.火焰材质);
        meshRenderer.GetPropertyBlock(propertyBlock);
        写入原始材质参数(propertyBlock);
        propertyBlock.SetColor(ColorId, config.颜色);
        propertyBlock.SetColor(DarkFireColorId, config.暗部火焰颜色);
        propertyBlock.SetColor(MainFireColorId, config.主火焰颜色);
        propertyBlock.SetColor(CoreFireColorId, config.核心火焰颜色);
        propertyBlock.SetFloat(FireIntensityId, config.火焰强度);
        propertyBlock.SetFloat(FireSpeedId, config.火焰速度);
        propertyBlock.SetFloat(FireScaleId, config.火焰密度);
        propertyBlock.SetVector(FlowDirectionId, 取标准化流动方向(config.流动方向));
        propertyBlock.SetFloat(OriginalKeepId, config.原图保留强度);
        propertyBlock.SetFloat(OuterFireRangeId, config.外扩火焰范围);
        propertyBlock.SetFloat(OuterFireIntensityId, config.外扩火焰强度);
        propertyBlock.SetFloat(FlickerStrengthId, config.闪烁强度);
        propertyBlock.SetFloat(FlickerSpeedId, config.闪烁速度);
        meshRenderer.SetPropertyBlock(propertyBlock);
        应用火星粒子设置(config);
        应用Trail拖尾设置(config);
    }

    public void 设置武器特效启用(bool 启用)
    {
        if (启用附魔 == 启用)
        {
            return;
        }

        启用附魔 = 启用;
        应用附魔设置();
    }

    [ContextMenu("还原武器原始材质")]
    public void 还原原始材质()
    {
        取组件和参数块();
        if (meshRenderer == null)
        {
            return;
        }

        propertyBlock.Clear();
        meshRenderer.SetPropertyBlock(propertyBlock);

        if (原始材质列表 != null && 原始材质列表.Length > 0)
        {
            meshRenderer.sharedMaterials = 原始材质列表;
        }

        停止火星粒子();
        停止Trail拖尾();
    }

    private void 记录原始材质列表()
    {
        if (meshRenderer == null || (原始材质列表 != null && 原始材质列表.Length > 0))
        {
            return;
        }

        Material[] currentMaterials = meshRenderer.sharedMaterials;
        if (currentMaterials == null || currentMaterials.Length == 0)
        {
            return;
        }

        武器火焰附魔全局配置 config = 取全局配置();
        Material fireMaterial = config != null ? config.火焰材质 : null;
        if (fireMaterial != null && 全部材质都是火焰材质(currentMaterials, fireMaterial))
        {
            return;
        }

        原始材质列表 = currentMaterials;
    }

    private bool 全部材质都是火焰材质(Material[] materials, Material fireMaterial)
    {
        if (materials == null || materials.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < materials.Length; i++)
        {
            if (materials[i] != fireMaterial)
            {
                return false;
            }
        }

        return true;
    }

    private void 应用火焰材质到全部材质槽(Material fireMaterial)
    {
        Material[] currentMaterials = meshRenderer.sharedMaterials;
        if (currentMaterials == null || currentMaterials.Length == 0)
        {
            meshRenderer.sharedMaterial = fireMaterial;
            return;
        }

        Material[] fireMaterials = new Material[currentMaterials.Length];
        for (int i = 0; i < fireMaterials.Length; i++)
        {
            fireMaterials[i] = fireMaterial;
        }

        meshRenderer.sharedMaterials = fireMaterials;
    }

    private void 写入原始材质参数(MaterialPropertyBlock block)
    {
        Material sourceMaterial = 取第一个原始材质();
        if (sourceMaterial == null)
        {
            return;
        }

        if (sourceMaterial.HasProperty(MainTexId))
        {
            Texture mainTexture = sourceMaterial.GetTexture(MainTexId);
            if (mainTexture != null)
            {
                block.SetTexture(MainTexId, mainTexture);
            }
        }

        if (sourceMaterial.HasProperty(MetallicId))
        {
            block.SetFloat(MetallicId, sourceMaterial.GetFloat(MetallicId));
        }

        if (sourceMaterial.HasProperty(GlossinessId))
        {
            block.SetFloat(GlossinessId, sourceMaterial.GetFloat(GlossinessId));
        }
    }

    private Material 取第一个原始材质()
    {
        if (原始材质列表 == null)
        {
            return null;
        }

        for (int i = 0; i < 原始材质列表.Length; i++)
        {
            if (原始材质列表[i] != null)
            {
                return 原始材质列表[i];
            }
        }

        return null;
    }

    private void 应用火星粒子设置(武器火焰附魔全局配置 config)
    {
        if (!config.启用火星粒子)
        {
            停止火星粒子();
            设置火星粒子子物体启用(false);
            return;
        }

        设置火星粒子子物体启用(true);
        取火星粒子系统();
        if (火星粒子系统 == null)
        {
            return;
        }

        Bounds localBounds = 取模型本地包围盒();
        Transform particleTransform = 火星粒子系统.transform;
        particleTransform.localPosition = localBounds.center;
        particleTransform.localRotation = Quaternion.identity;
        particleTransform.localScale = Vector3.one;

        ParticleSystem.MainModule main = 火星粒子系统.main;
        main.loop = true;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = config.火星生命周期;
        main.startSize = config.火星大小;
        main.startSpeed = 0f;
        main.startColor = new ParticleSystem.MinMaxGradient(config.火星起始颜色, config.火星结束颜色);
        main.maxParticles = Mathf.Max(8, Mathf.CeilToInt(config.火星数量 * config.火星生命周期 * 3f));
        main.gravityModifier = 0f;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        ParticleSystem.EmissionModule emission = 火星粒子系统.emission;
        emission.enabled = true;
        emission.rateOverTime = config.火星数量;

        ParticleSystem.ShapeModule shape = 火星粒子系统.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.position = Vector3.zero;
        Vector3 shapeScale = localBounds.size * config.火星发射范围倍率;
        shapeScale.x = Mathf.Max(shapeScale.x, 0.01f);
        shapeScale.y = Mathf.Max(shapeScale.y, 0.01f);
        shapeScale.z = Mathf.Max(shapeScale.z, 0.01f);
        shape.scale = shapeScale;

        ParticleSystem.VelocityOverLifetimeModule velocity = 火星粒子系统.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(-config.火星左右扰动, config.火星左右扰动);
        velocity.y = new ParticleSystem.MinMaxCurve(config.火星上升速度 * 0.35f, config.火星上升速度 * 0.9f);
        velocity.z = new ParticleSystem.MinMaxCurve(-config.火星左右扰动, config.火星左右扰动);

        ParticleSystem.NoiseModule noise = 火星粒子系统.noise;
        noise.enabled = true;
        noise.strength = config.火星左右扰动;
        noise.frequency = 1.6f;
        noise.scrollSpeed = 0.45f;
        noise.damping = true;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = 火星粒子系统.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(config.火星起始颜色, 0f),
                new GradientColorKey(config.火星结束颜色, 1f)
            },
            new[]
            {
                new GradientAlphaKey(config.火星起始颜色.a, 0f),
                new GradientAlphaKey(config.火星结束颜色.a, 1f)
            });
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = 火星粒子系统.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.4f),
            new Keyframe(0.18f, 1f),
            new Keyframe(1f, 0f)));

        if (火星粒子渲染器 != null)
        {
            火星粒子渲染器.renderMode = ParticleSystemRenderMode.Billboard;
            火星粒子渲染器.sortingOrder = 0;
            火星粒子渲染器.sharedMaterial = 取火星粒子运行时材质(config);
            火星粒子渲染器.minParticleSize = 0f;
            火星粒子渲染器.maxParticleSize = 0.5f;
            写入火星粒子材质参数();
        }

        if (!火星粒子系统.isPlaying)
        {
            火星粒子系统.Play();
        }
    }

    private void 停止火星粒子()
    {
        if (火星粒子系统 == null)
        {
            Transform child = transform.Find(火星粒子物体名);
            if (child != null)
            {
                火星粒子系统 = child.GetComponent<ParticleSystem>();
            }
        }

        if (火星粒子系统 != null)
        {
            火星粒子系统.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    private void 设置火星粒子子物体启用(bool enabled)
    {
        Transform child = transform.Find(火星粒子物体名);
        if (child != null && child.gameObject.activeSelf != enabled)
        {
            child.gameObject.SetActive(enabled);
        }
    }

    private void 更新Trail火焰拖尾()
    {
        武器火焰附魔全局配置 config = 取全局配置();
        if (config == null || !启用附魔 || !config.启用Trail火焰拖尾)
        {
            停止Trail拖尾();
            return;
        }

        取组件和参数块();
        if (meshRenderer == null)
        {
            停止Trail拖尾();
            return;
        }

        取TrailRenderer();
        if (trailRenderer == null || trailTransform == null)
        {
            return;
        }

        Bounds bounds = meshRenderer.bounds;
        Vector3 position = trailTransform.position;
        float speed = 计算Trail速度(position);
        更新Trail宽度(config, 取模型最长轴长度(bounds) * config.Trail整体长轴倍率, speed);
    }

    private void 应用Trail拖尾设置(武器火焰附魔全局配置 config)
    {
        if (!config.启用Trail火焰拖尾)
        {
            停止Trail拖尾();
            设置Trail子物体启用(false);
            return;
        }

        设置Trail子物体启用(true);
        Material trailMaterial = 取Trail运行时材质(config);
        if (trailMaterial == null)
        {
            停止Trail拖尾();
            return;
        }

        取TrailRenderer();
        if (trailRenderer == null)
        {
            return;
        }

        配置TrailRenderer(trailMaterial);
        if (!Application.isPlaying)
        {
            trailRenderer.emitting = false;
            trailRenderer.Clear();
        }
    }

    private void 更新Trail宽度(武器火焰附魔全局配置 config, float baseWidth, float speed)
    {
        float speed01 = config.Trail触发速度阈值 <= 0f ? 1f : Mathf.Clamp01(speed / Mathf.Max(config.Trail触发速度阈值 * 4f, 0.0001f));
        float widthScale = Mathf.Lerp(config.Trail低速宽度收缩, 1f, speed01);
        trailRenderer.widthMultiplier = Mathf.Max(0.001f, baseWidth * widthScale);
        设置Trail宽度曲线(config);
        写入Trail材质参数(config);
        trailRenderer.emitting = speed >= config.Trail触发速度阈值;
    }

    private float 计算Trail速度(Vector3 position)
    {
        if (!trail已采样)
        {
            trail上次位置 = position;
            trail已采样 = true;
            return 0f;
        }

        float speed = Vector3.Distance(trail上次位置, position) / Mathf.Max(Time.deltaTime, 0.0001f);
        trail上次位置 = position;
        return speed;
    }

    private void 取TrailRenderer()
    {
        if (trailRenderer == null || trailTransform == null)
        {
            Transform existing = transform.Find(Trail物体名);
            if (existing != null)
            {
                trailTransform = existing;
                trailRenderer = existing.GetComponent<TrailRenderer>();
            }
        }

        if (trailTransform == null)
        {
            Debug.LogWarning($"[武器火焰附魔] {name} 缺少子物体：{Trail物体名}。请在编辑状态重新应用武器火焰附魔控制器。", this);
            return;
        }

        if (trailRenderer == null)
        {
            Debug.LogWarning($"[武器火焰附魔] {name} 的 {Trail物体名} 缺少 TrailRenderer。请在编辑状态重新应用武器火焰附魔控制器。", trailTransform);
        }
    }

    private void 配置TrailRenderer(Material material)
    {
        if (trailRenderer == null)
        {
            return;
        }

        武器火焰附魔全局配置 config = 取全局配置();
        if (config == null)
        {
            return;
        }

        trailRenderer.sharedMaterial = material;
        trailRenderer.time = config.Trail持续时间;
        trailRenderer.minVertexDistance = config.Trail最小顶点距离;
        trailRenderer.numCornerVertices = 3;
        trailRenderer.numCapVertices = 2;
        trailRenderer.autodestruct = false;
        trailRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trailRenderer.receiveShadows = false;
        trailRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        trailRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        trailRenderer.textureMode = LineTextureMode.Stretch;
        trailRenderer.alignment = LineAlignment.TransformZ;
        trailRenderer.colorGradient = 创建Trail颜色渐变();
    }

    private void 设置Trail宽度曲线(武器火焰附魔全局配置 config)
    {
        if (trailRenderer == null)
        {
            return;
        }

        trailRenderer.widthCurve = new AnimationCurve(
            new Keyframe(0f, config.Trail末端宽度倍率),
            new Keyframe(0.2f, 1f),
            new Keyframe(1f, 0f));
    }

    private void 停止Trail拖尾()
    {
        if (trailRenderer != null)
        {
            trailRenderer.emitting = false;
        }

        trail已采样 = false;
    }

    private void 设置Trail子物体启用(bool enabled)
    {
        Transform child = transform.Find(Trail物体名);
        if (child != null && child.gameObject.activeSelf != enabled)
        {
            child.gameObject.SetActive(enabled);
        }
    }

    private void 清空Trail拖尾()
    {
        if (trailRenderer != null)
        {
            trailRenderer.Clear();
        }

        if (trail运行时材质 != null)
        {
            销毁对象(trail运行时材质);
            trail运行时材质 = null;
            trail运行时来源材质 = null;
        }

        trail已采样 = false;
    }

    private Material 取Trail运行时材质(武器火焰附魔全局配置 config)
    {
        if (config.Trail拖尾材质 == null)
        {
            if (!trail缺少材质已警告)
            {
                Debug.LogWarning($"[武器火焰附魔] 全局配置没有指定Trail拖尾材质，Trail火焰拖尾不可见。", this);
                trail缺少材质已警告 = true;
            }

            return null;
        }

        trail缺少材质已警告 = false;
        if (trail运行时材质 == null || trail运行时来源材质 != config.Trail拖尾材质)
        {
            if (trail运行时材质 != null)
            {
                销毁对象(trail运行时材质);
            }

            trail运行时材质 = new Material(config.Trail拖尾材质)
            {
                name = $"{config.Trail拖尾材质.name}_Trail运行时",
                hideFlags = HideFlags.HideAndDontSave
            };
            trail运行时来源材质 = config.Trail拖尾材质;
        }

        trail运行时材质.renderQueue = config.Trail无视深度 ? 3120 : 3000;
        if (trail运行时材质.HasProperty(TrailZTestId))
        {
            trail运行时材质.SetFloat(TrailZTestId, config.Trail无视深度 ? 8f : 4f);
        }

        return trail运行时材质;
    }

    private void 写入Trail材质参数(武器火焰附魔全局配置 config)
    {
        if (trailRenderer == null)
        {
            return;
        }

        if (trail参数块 == null)
        {
            trail参数块 = new MaterialPropertyBlock();
        }

        trailRenderer.GetPropertyBlock(trail参数块);
        trail参数块.SetColor(TrailColorAId, config.Trail外侧颜色);
        trail参数块.SetColor(TrailColorBId, config.Trail内侧颜色);
        trail参数块.SetFloat(TrailSoftnessId, config.Trail边缘柔和);
        trail参数块.SetFloat(TrailNoiseScaleId, config.Trail火焰噪声密度);
        trail参数块.SetFloat(TrailNoiseStrengthId, config.Trail火焰破碎强度);
        trail参数块.SetFloat(TrailIntensityId, config.Trail亮度);
        trailRenderer.SetPropertyBlock(trail参数块);
    }

    private static Gradient 创建Trail颜色渐变()
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.7f, 0.55f),
                new GradientAlphaKey(0f, 1f)
            });
        return gradient;
    }

    private float 取模型最长轴长度(Bounds worldBounds)
    {
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            return Mathf.Max(worldBounds.size.x, worldBounds.size.y, worldBounds.size.z);
        }

        Vector3 localSize = meshFilter.sharedMesh.bounds.size;
        Vector3 lossyScale = transform.lossyScale;
        float x = Mathf.Abs(localSize.x * lossyScale.x);
        float y = Mathf.Abs(localSize.y * lossyScale.y);
        float z = Mathf.Abs(localSize.z * lossyScale.z);
        return Mathf.Max(x, y, z);
    }

    private void 取火星粒子系统()
    {
        Transform child = transform.Find(火星粒子物体名);
        if (child == null)
        {
            Debug.LogWarning($"[武器火焰附魔] {name} 缺少子物体：{火星粒子物体名}。请在编辑状态重新应用武器火焰附魔控制器。", this);
            火星粒子系统 = null;
            火星粒子渲染器 = null;
            return;
        }

        火星粒子系统 = child.GetComponent<ParticleSystem>();
        if (火星粒子系统 == null)
        {
            Debug.LogWarning($"[武器火焰附魔] {name} 的 {火星粒子物体名} 缺少 ParticleSystem。请在编辑状态重新应用武器火焰附魔控制器。", child);
            火星粒子渲染器 = null;
            return;
        }

        火星粒子渲染器 = 火星粒子系统.GetComponent<ParticleSystemRenderer>();
    }

    private Bounds 取模型本地包围盒()
    {
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            return meshFilter.sharedMesh.bounds;
        }

        Bounds worldBounds = meshRenderer.bounds;
        Vector3 localCenter = transform.InverseTransformPoint(worldBounds.center);
        Vector3 localSize = transform.InverseTransformVector(worldBounds.size);
        localSize = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));
        return new Bounds(localCenter, localSize);
    }

    private Material 取火星粒子材质(武器火焰附魔全局配置 config)
    {
        if (config.火星粒子材质 != null)
        {
            return config.火星粒子材质;
        }

        Debug.LogWarning($"[武器火焰附魔] 全局配置没有指定火星粒子材质，火星粒子不可见。", this);
        return null;
    }

    private Material 取火星粒子运行时材质(武器火焰附魔全局配置 config)
    {
        Material sourceMaterial = 取火星粒子材质(config);
        if (sourceMaterial == null)
        {
            return null;
        }

        if (火星粒子运行时材质 == null || 火星粒子运行时来源材质 != sourceMaterial)
        {
            火星粒子运行时材质 = new Material(sourceMaterial)
            {
                name = $"{sourceMaterial.name}_运行时",
                hideFlags = HideFlags.HideAndDontSave
            };
            火星粒子运行时来源材质 = sourceMaterial;
        }

        火星粒子运行时材质.renderQueue = config.火星无视深度 ? 3100 : 3000;
        if (火星粒子运行时材质.HasProperty(SparkZTestId))
        {
            火星粒子运行时材质.SetFloat(SparkZTestId, config.火星无视深度 ? 8f : 4f);
        }

        return 火星粒子运行时材质;
    }

    private void 写入火星粒子材质参数()
    {
        if (火星粒子渲染器 == null)
        {
            return;
        }

        if (火星粒子参数块 == null)
        {
            火星粒子参数块 = new MaterialPropertyBlock();
        }

        火星粒子渲染器.GetPropertyBlock(火星粒子参数块);
        武器火焰附魔全局配置 config = 取全局配置();
        if (config == null)
        {
            return;
        }

        火星粒子参数块.SetColor(SparkCoreColorId, config.火星圆点颜色);
        火星粒子参数块.SetColor(SparkGlowColorId, config.火星外发光颜色);
        火星粒子渲染器.SetPropertyBlock(火星粒子参数块);
    }

#if UNITY_EDITOR
    private void 编辑器预览更新()
    {
        if (Application.isPlaying || this == null || !isActiveAndEnabled)
        {
            return;
        }

        武器火焰附魔全局配置 config = 取全局配置();
        if (config == null || !启用附魔 || !config.启用火星粒子)
        {
            return;
        }

        取组件和参数块();
        if (meshRenderer == null)
        {
            return;
        }

        if (火星粒子系统 == null)
        {
            应用火星粒子设置(config);
        }

        if (火星粒子系统 == null)
        {
            return;
        }

        double currentTime = EditorApplication.timeSinceStartup;
        float deltaTime = Mathf.Clamp((float)(currentTime - 上次编辑器预览时间), 0f, 0.05f);
        上次编辑器预览时间 = currentTime;

        if (!火星粒子系统.isPlaying)
        {
            火星粒子系统.Play();
        }

        火星粒子系统.Simulate(deltaTime, true, false, false);
        SceneView.RepaintAll();
    }
#endif

    private Vector4 取标准化流动方向(Vector2 sourceDirection)
    {
        Vector2 direction = sourceDirection;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = Vector2.up;
        }

        direction.Normalize();
        return new Vector4(direction.x, direction.y, 0f, 0f);
    }

    private 武器火焰附魔全局配置 取全局配置()
    {
        if (全局配置缓存 == null)
        {
            全局配置缓存 = Resources.Load<武器火焰附魔全局配置>(全局配置路径);
        }

        return 全局配置缓存;
    }

    private void 取组件和参数块()
    {
        if (meshRenderer == null)
        {
            meshRenderer = GetComponent<MeshRenderer>();
        }

        if (meshFilter == null)
        {
            meshFilter = GetComponent<MeshFilter>();
        }

        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }
    }

    private static void 销毁对象(Object target)
    {
        if (target == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(target);
        }
        else
        {
            DestroyImmediate(target);
        }
    }
}

using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("特效/武器火焰实体拖尾控制器")]
public sealed class 武器火焰实体拖尾控制器 : MonoBehaviour
{
    private const string 默认材质路径 = "武器火焰实体拖尾材质";
    private const string Trail物体名 = "武器火焰Trail拖尾";

    private static readonly int ColorAId = Shader.PropertyToID("_ColorA");
    private static readonly int ColorBId = Shader.PropertyToID("_ColorB");
    private static readonly int SoftnessId = Shader.PropertyToID("_Softness");
    private static readonly int NoiseScaleId = Shader.PropertyToID("_NoiseScale");
    private static readonly int NoiseStrengthId = Shader.PropertyToID("_NoiseStrength");
    private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
    private static readonly int ZTestId = Shader.PropertyToID("_ZTest");

    public enum 拖尾生成方式
    {
        按模型整体生成 = 0,
        按绑定点生成 = 1
    }

    [Header("生成方式")]
    [SerializeField] private 拖尾生成方式 生成方式 = 拖尾生成方式.按模型整体生成;

    [Header("绑定点")]
    [SerializeField] private Transform 刀柄点;
    [SerializeField] private Transform 刀尖点;
    [SerializeField] private Transform 拖尾容器;

    [Header("模型整体")]
    [SerializeField] private MeshRenderer 目标模型渲染器;
    [SerializeField, Range(0.1f, 4f)] private float 整体长轴倍率 = 1.15f;

    [Header("拖尾开关")]
    [SerializeField] private bool 启用拖尾 = true;
    [SerializeField] private bool 无视深度 = true;
    [SerializeField] private Material 拖尾材质;

    [Header("Trail生成")]
    [SerializeField, Range(0.03f, 1f)] private float 拖尾持续时间 = 0.22f;
    [SerializeField, Range(0f, 20f)] private float 触发速度阈值 = 0.25f;
    [SerializeField, Range(0.01f, 1f)] private float 最小顶点距离 = 0.03f;
    [SerializeField, Range(0f, 1f)] private float 末端宽度倍率 = 0.08f;
    [SerializeField, Range(0f, 1f)] private float 低速宽度收缩 = 0.35f;

    [Header("视觉")]
    [SerializeField] private Color 外侧颜色 = new Color(1f, 0.16f, 0.02f, 0.75f);
    [SerializeField] private Color 内侧颜色 = new Color(1f, 0.82f, 0.18f, 0.95f);
    [SerializeField, Range(0.01f, 1f)] private float 边缘柔和 = 0.35f;
    [SerializeField, Range(0.1f, 40f)] private float 火焰噪声密度 = 8f;
    [SerializeField, Range(0f, 1f)] private float 火焰破碎强度 = 0.28f;
    [SerializeField, Range(0f, 6f)] private float 亮度 = 1.8f;

    private TrailRenderer trailRenderer;
    private Transform trailTransform;
    private Material 运行时材质;
    private Material 运行时材质来源;
    private MaterialPropertyBlock 参数块;
    private Vector3 上次位置;
    private bool 已采样;
    private bool 缺少绑定已警告;
    private bool 缺少模型渲染器已警告;
    private bool 缺少材质已警告;

    private void OnEnable()
    {
        重置采样();
    }

    private void OnDisable()
    {
        清空片段();

        if (运行时材质 != null)
        {
            销毁对象(运行时材质);
            运行时材质 = null;
            运行时材质来源 = null;
        }
    }

    private void OnValidate()
    {
        整体长轴倍率 = Mathf.Clamp(整体长轴倍率, 0.1f, 4f);
        拖尾持续时间 = Mathf.Clamp(拖尾持续时间, 0.03f, 1f);
        触发速度阈值 = Mathf.Max(0f, 触发速度阈值);
        最小顶点距离 = Mathf.Clamp(最小顶点距离, 0.01f, 1f);
        末端宽度倍率 = Mathf.Clamp01(末端宽度倍率);
        低速宽度收缩 = Mathf.Clamp01(低速宽度收缩);
        边缘柔和 = Mathf.Clamp01(边缘柔和);
        火焰噪声密度 = Mathf.Clamp(火焰噪声密度, 0.1f, 40f);
        火焰破碎强度 = Mathf.Clamp01(火焰破碎强度);
        亮度 = Mathf.Clamp(亮度, 0f, 6f);
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (!启用拖尾)
        {
            设置Trail发射(false);
            重置采样();
            return;
        }

        Material material = 取拖尾材质();
        if (material == null)
        {
            设置Trail发射(false);
            return;
        }

        if (!取TrailRenderer(material))
        {
            return;
        }

        if (生成方式 == 拖尾生成方式.按绑定点生成)
        {
            更新绑定点Trail();
            return;
        }

        更新模型整体Trail();
    }

    [ContextMenu("清空火焰实体拖尾")]
    public void 清空片段()
    {
        if (trailRenderer != null)
        {
            trailRenderer.Clear();
        }

        if (trailTransform != null)
        {
            销毁对象(trailTransform.gameObject);
            trailTransform = null;
            trailRenderer = null;
        }
    }

    private void 更新模型整体Trail()
    {
        MeshRenderer renderer = 取目标模型渲染器();
        if (renderer == null)
        {
            设置Trail发射(false);
            重置采样();
            return;
        }

        Bounds bounds = renderer.bounds;
        Vector3 position = bounds.center;
        float speed = 计算速度(position);
        更新Trail位置和宽度(position, 取模型最长轴长度(renderer, bounds) * 整体长轴倍率, speed);
    }

    private void 更新绑定点Trail()
    {
        if (!检查绑定点())
        {
            设置Trail发射(false);
            重置采样();
            return;
        }

        Vector3 position = (刀柄点.position + 刀尖点.position) * 0.5f;
        float width = Vector3.Distance(刀柄点.position, 刀尖点.position);
        float speed = 计算速度(position);
        更新Trail位置和宽度(position, width, speed);
    }

    private void 更新Trail位置和宽度(Vector3 position, float baseWidth, float speed)
    {
        if (trailTransform == null || trailRenderer == null)
        {
            return;
        }

        trailTransform.position = position;
        float speed01 = 触发速度阈值 <= 0f ? 1f : Mathf.Clamp01(speed / Mathf.Max(触发速度阈值 * 4f, 0.0001f));
        float widthScale = Mathf.Lerp(低速宽度收缩, 1f, speed01);
        trailRenderer.widthMultiplier = Mathf.Max(0.001f, baseWidth * widthScale);
        设置Trail宽度曲线();
        写入Trail材质参数();
        设置Trail发射(speed >= 触发速度阈值);
    }

    private float 计算速度(Vector3 position)
    {
        if (!已采样)
        {
            上次位置 = position;
            已采样 = true;
            return 0f;
        }

        float speed = Vector3.Distance(上次位置, position) / Mathf.Max(Time.deltaTime, 0.0001f);
        上次位置 = position;
        return speed;
    }

    private bool 取TrailRenderer(Material material)
    {
        if (trailRenderer != null && trailTransform != null)
        {
            配置TrailRenderer(material);
            return true;
        }

        Transform parent = 拖尾容器 != null ? 拖尾容器 : transform;
        Transform existing = parent.Find(Trail物体名);
        if (existing != null)
        {
            trailTransform = existing;
            trailRenderer = existing.GetComponent<TrailRenderer>();
        }

        if (trailRenderer == null)
        {
            GameObject trailObject = new GameObject(Trail物体名);
            trailObject.transform.SetParent(parent, false);
            trailTransform = trailObject.transform;
            trailRenderer = trailObject.AddComponent<TrailRenderer>();
        }

        配置TrailRenderer(material);
        return trailRenderer != null;
    }

    private void 配置TrailRenderer(Material material)
    {
        if (trailRenderer == null)
        {
            return;
        }

        trailRenderer.sharedMaterial = material;
        trailRenderer.time = 拖尾持续时间;
        trailRenderer.minVertexDistance = 最小顶点距离;
        trailRenderer.numCornerVertices = 3;
        trailRenderer.numCapVertices = 2;
        trailRenderer.autodestruct = false;
        trailRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trailRenderer.receiveShadows = false;
        trailRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        trailRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        trailRenderer.textureMode = LineTextureMode.Stretch;
        trailRenderer.alignment = LineAlignment.View;
        trailRenderer.colorGradient = 创建Trail颜色渐变();
    }

    private void 设置Trail宽度曲线()
    {
        if (trailRenderer == null)
        {
            return;
        }

        AnimationCurve widthCurve = new AnimationCurve(
            new Keyframe(0f, 末端宽度倍率),
            new Keyframe(0.2f, 1f),
            new Keyframe(1f, 0f));
        trailRenderer.widthCurve = widthCurve;
    }

    private void 设置Trail发射(bool emitting)
    {
        if (trailRenderer == null)
        {
            return;
        }

        trailRenderer.emitting = emitting;
    }

    private void 写入Trail材质参数()
    {
        if (trailRenderer == null)
        {
            return;
        }

        if (参数块 == null)
        {
            参数块 = new MaterialPropertyBlock();
        }

        trailRenderer.GetPropertyBlock(参数块);
        参数块.SetColor(ColorAId, 外侧颜色);
        参数块.SetColor(ColorBId, 内侧颜色);
        参数块.SetFloat(SoftnessId, 边缘柔和);
        参数块.SetFloat(NoiseScaleId, 火焰噪声密度);
        参数块.SetFloat(NoiseStrengthId, 火焰破碎强度);
        参数块.SetFloat(IntensityId, 亮度);
        trailRenderer.SetPropertyBlock(参数块);
    }

    private bool 检查绑定点()
    {
        if (刀柄点 != null && 刀尖点 != null)
        {
            缺少绑定已警告 = false;
            return true;
        }

        if (!缺少绑定已警告)
        {
            Debug.LogWarning($"[武器火焰Trail拖尾] {name} 缺少刀柄点或刀尖点，无法按绑定点生成拖尾。", this);
            缺少绑定已警告 = true;
        }

        return false;
    }

    private MeshRenderer 取目标模型渲染器()
    {
        if (目标模型渲染器 == null)
        {
            目标模型渲染器 = GetComponent<MeshRenderer>();
        }

        if (目标模型渲染器 != null)
        {
            缺少模型渲染器已警告 = false;
            return 目标模型渲染器;
        }

        if (!缺少模型渲染器已警告)
        {
            Debug.LogWarning($"[武器火焰Trail拖尾] {name} 使用模型整体生成，但缺少 MeshRenderer，无法生成拖尾。", this);
            缺少模型渲染器已警告 = true;
        }

        return null;
    }

    private float 取模型最长轴长度(MeshRenderer renderer, Bounds worldBounds)
    {
        if (renderer == null)
        {
            return Mathf.Max(worldBounds.size.x, worldBounds.size.y, worldBounds.size.z);
        }

        MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            return Mathf.Max(worldBounds.size.x, worldBounds.size.y, worldBounds.size.z);
        }

        Vector3 localSize = meshFilter.sharedMesh.bounds.size;
        Vector3 lossyScale = renderer.transform.lossyScale;
        float x = Mathf.Abs(localSize.x * lossyScale.x);
        float y = Mathf.Abs(localSize.y * lossyScale.y);
        float z = Mathf.Abs(localSize.z * lossyScale.z);
        return Mathf.Max(x, y, z);
    }

    private Material 取拖尾材质()
    {
        Material sourceMaterial = 拖尾材质 != null ? 拖尾材质 : Resources.Load<Material>(默认材质路径);
        if (sourceMaterial == null)
        {
            if (!缺少材质已警告)
            {
                Debug.LogWarning($"[武器火焰Trail拖尾] {name} 找不到拖尾材质，也找不到 Resources/{默认材质路径}。", this);
                缺少材质已警告 = true;
            }

            return null;
        }

        缺少材质已警告 = false;
        if (运行时材质 == null || 运行时材质来源 != sourceMaterial)
        {
            if (运行时材质 != null)
            {
                销毁对象(运行时材质);
            }

            运行时材质 = new Material(sourceMaterial)
            {
                name = $"{sourceMaterial.name}_Trail运行时"
            };
            运行时材质来源 = sourceMaterial;
        }

        运行时材质.renderQueue = 无视深度 ? 3120 : 3000;
        if (运行时材质.HasProperty(ZTestId))
        {
            运行时材质.SetFloat(ZTestId, 无视深度 ? 8f : 4f);
        }

        return 运行时材质;
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

    private void 重置采样()
    {
        已采样 = false;
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

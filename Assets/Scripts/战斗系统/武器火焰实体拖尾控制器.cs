using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[AddComponentMenu("特效/武器火焰实体拖尾控制器")]
public sealed class 武器火焰实体拖尾控制器 : MonoBehaviour
{
    private const string 默认材质路径 = "武器火焰实体拖尾材质";

    private static readonly int ColorAId = Shader.PropertyToID("_ColorA");
    private static readonly int ColorBId = Shader.PropertyToID("_ColorB");
    private static readonly int Age01Id = Shader.PropertyToID("_Age01");
    private static readonly int SoftnessId = Shader.PropertyToID("_Softness");
    private static readonly int NoiseScaleId = Shader.PropertyToID("_NoiseScale");
    private static readonly int NoiseStrengthId = Shader.PropertyToID("_NoiseStrength");
    private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
    private static readonly int ZTestId = Shader.PropertyToID("_ZTest");

    [Header("绑定点")]
    [SerializeField] private Transform 刀柄点;
    [SerializeField] private Transform 刀尖点;
    [SerializeField] private Transform 拖尾容器;

    [Header("拖尾开关")]
    [SerializeField] private bool 启用拖尾 = true;
    [SerializeField] private bool 无视深度 = true;
    [SerializeField] private Material 拖尾材质;

    [Header("拖尾生成")]
    [SerializeField, Range(0.005f, 0.2f)] private float 生成间隔 = 0.018f;
    [SerializeField, Range(0.03f, 1f)] private float 拖尾持续时间 = 0.18f;
    [SerializeField, Range(0f, 20f)] private float 触发速度阈值 = 0.25f;
    [SerializeField, Range(0.1f, 3f)] private float 宽度倍率 = 1.08f;
    [SerializeField, Range(0, 80)] private int 最大片段数 = 24;

    [Header("视觉")]
    [SerializeField] private Color 外侧颜色 = new Color(1f, 0.16f, 0.02f, 0.75f);
    [SerializeField] private Color 内侧颜色 = new Color(1f, 0.82f, 0.18f, 0.95f);
    [SerializeField, Range(0.01f, 1f)] private float 边缘柔和 = 0.35f;
    [SerializeField, Range(0.1f, 40f)] private float 火焰噪声密度 = 8f;
    [SerializeField, Range(0f, 1f)] private float 火焰破碎强度 = 0.28f;
    [SerializeField, Range(0f, 6f)] private float 亮度 = 1.8f;

    private readonly List<拖尾片段> 片段列表 = new List<拖尾片段>();
    private MaterialPropertyBlock 参数块;
    private Material 运行时材质;
    private Material 运行时材质来源;
    private Vector3 上次刀柄位置;
    private Vector3 上次刀尖位置;
    private float 距上次生成时间;
    private double 上次更新时间;
    private bool 已采样;
    private bool 缺少绑定已警告;
    private bool 缺少材质已警告;

    private sealed class 拖尾片段
    {
        public GameObject 物体;
        public Mesh 网格;
        public MeshRenderer 渲染器;
        public float 已存在时间;
    }

    private void OnEnable()
    {
        上次更新时间 = 取当前时间();
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
        生成间隔 = Mathf.Clamp(生成间隔, 0.005f, 0.2f);
        拖尾持续时间 = Mathf.Clamp(拖尾持续时间, 0.03f, 1f);
        触发速度阈值 = Mathf.Max(0f, 触发速度阈值);
        宽度倍率 = Mathf.Clamp(宽度倍率, 0.1f, 3f);
        最大片段数 = Mathf.Clamp(最大片段数, 0, 80);
        边缘柔和 = Mathf.Clamp01(边缘柔和);
        火焰噪声密度 = Mathf.Clamp(火焰噪声密度, 0.1f, 40f);
        火焰破碎强度 = Mathf.Clamp01(火焰破碎强度);
        亮度 = Mathf.Clamp(亮度, 0f, 6f);
    }

    private void Update()
    {
        float deltaTime = 取DeltaTime();
        更新片段(deltaTime);

        if (!启用拖尾)
        {
            重置采样();
            return;
        }

        if (!检查绑定())
        {
            重置采样();
            return;
        }

        Material material = 取拖尾材质();
        if (material == null)
        {
            return;
        }

        Vector3 当前刀柄位置 = 刀柄点.position;
        Vector3 当前刀尖位置 = 刀尖点.position;
        if (!已采样)
        {
            上次刀柄位置 = 当前刀柄位置;
            上次刀尖位置 = 当前刀尖位置;
            已采样 = true;
            return;
        }

        距上次生成时间 += deltaTime;
        float speed = Mathf.Max(
            Vector3.Distance(上次刀柄位置, 当前刀柄位置),
            Vector3.Distance(上次刀尖位置, 当前刀尖位置)) / Mathf.Max(deltaTime, 0.0001f);

        if (距上次生成时间 >= 生成间隔 && speed >= 触发速度阈值)
        {
            创建片段(上次刀柄位置, 上次刀尖位置, 当前刀柄位置, 当前刀尖位置, material);
            距上次生成时间 = 0f;
        }

        上次刀柄位置 = 当前刀柄位置;
        上次刀尖位置 = 当前刀尖位置;
    }

    [ContextMenu("清空火焰实体拖尾")]
    public void 清空片段()
    {
        for (int i = 片段列表.Count - 1; i >= 0; i--)
        {
            销毁片段(片段列表[i]);
        }

        片段列表.Clear();
    }

    private void 创建片段(Vector3 上一刀柄, Vector3 上一刀尖, Vector3 当前刀柄, Vector3 当前刀尖, Material material)
    {
        if (最大片段数 <= 0)
        {
            return;
        }

        while (片段列表.Count >= 最大片段数)
        {
            销毁片段(片段列表[0]);
            片段列表.RemoveAt(0);
        }

        Vector3 上一中心 = (上一刀柄 + 上一刀尖) * 0.5f;
        Vector3 当前中心 = (当前刀柄 + 当前刀尖) * 0.5f;
        上一刀柄 = Vector3.LerpUnclamped(上一中心, 上一刀柄, 宽度倍率);
        上一刀尖 = Vector3.LerpUnclamped(上一中心, 上一刀尖, 宽度倍率);
        当前刀柄 = Vector3.LerpUnclamped(当前中心, 当前刀柄, 宽度倍率);
        当前刀尖 = Vector3.LerpUnclamped(当前中心, 当前刀尖, 宽度倍率);

        Transform parent = 拖尾容器 != null ? 拖尾容器 : null;
        GameObject segmentObject = new GameObject("武器火焰实体拖尾片段");
        segmentObject.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
        segmentObject.transform.SetParent(parent, false);
        segmentObject.transform.position = Vector3.zero;
        segmentObject.transform.rotation = Quaternion.identity;
        segmentObject.transform.localScale = Vector3.one;

        Matrix4x4 worldToSegment = segmentObject.transform.worldToLocalMatrix;

        Mesh mesh = new Mesh
        {
            name = "武器火焰实体拖尾网格",
            hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild
        };
        mesh.vertices = new[]
        {
            worldToSegment.MultiplyPoint3x4(上一刀柄),
            worldToSegment.MultiplyPoint3x4(上一刀尖),
            worldToSegment.MultiplyPoint3x4(当前刀柄),
            worldToSegment.MultiplyPoint3x4(当前刀尖)
        };
        mesh.uv = new[]
        {
            new Vector2(0f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 0f),
            new Vector2(1f, 1f)
        };
        mesh.triangles = new[] { 0, 1, 2, 2, 1, 3 };
        mesh.RecalculateBounds();

        MeshFilter meshFilter = segmentObject.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = mesh;

        MeshRenderer meshRenderer = segmentObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = material;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        meshRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        meshRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

        拖尾片段 segment = new 拖尾片段
        {
            物体 = segmentObject,
            网格 = mesh,
            渲染器 = meshRenderer,
            已存在时间 = 0f
        };
        写入片段参数(segment, 0f);
        片段列表.Add(segment);
    }

    private void 更新片段(float deltaTime)
    {
        for (int i = 片段列表.Count - 1; i >= 0; i--)
        {
            拖尾片段 segment = 片段列表[i];
            if (segment == null || segment.物体 == null)
            {
                片段列表.RemoveAt(i);
                continue;
            }

            segment.已存在时间 += deltaTime;
            float age01 = segment.已存在时间 / Mathf.Max(拖尾持续时间, 0.0001f);
            if (age01 >= 1f)
            {
                销毁片段(segment);
                片段列表.RemoveAt(i);
                continue;
            }

            写入片段参数(segment, age01);
        }
    }

    private void 写入片段参数(拖尾片段 segment, float age01)
    {
        if (segment == null || segment.渲染器 == null)
        {
            return;
        }

        if (参数块 == null)
        {
            参数块 = new MaterialPropertyBlock();
        }

        segment.渲染器.GetPropertyBlock(参数块);
        参数块.SetColor(ColorAId, 外侧颜色);
        参数块.SetColor(ColorBId, 内侧颜色);
        参数块.SetFloat(Age01Id, Mathf.Clamp01(age01));
        参数块.SetFloat(SoftnessId, 边缘柔和);
        参数块.SetFloat(NoiseScaleId, 火焰噪声密度);
        参数块.SetFloat(NoiseStrengthId, 火焰破碎强度);
        参数块.SetFloat(IntensityId, 亮度);
        segment.渲染器.SetPropertyBlock(参数块);
    }

    private bool 检查绑定()
    {
        if (刀柄点 != null && 刀尖点 != null)
        {
            缺少绑定已警告 = false;
            return true;
        }

        if (!缺少绑定已警告)
        {
            Debug.LogWarning($"[武器火焰实体拖尾] {name} 缺少刀柄点或刀尖点，无法生成实体拖尾。", this);
            缺少绑定已警告 = true;
        }

        return false;
    }

    private Material 取拖尾材质()
    {
        Material sourceMaterial = 拖尾材质 != null ? 拖尾材质 : Resources.Load<Material>(默认材质路径);
        if (sourceMaterial == null)
        {
            if (!缺少材质已警告)
            {
                Debug.LogWarning($"[武器火焰实体拖尾] {name} 找不到拖尾材质，也找不到 Resources/{默认材质路径}。", this);
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
                name = $"{sourceMaterial.name}_运行时"
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

    private float 取DeltaTime()
    {
        double currentTime = 取当前时间();
        float deltaTime = Mathf.Clamp((float)(currentTime - 上次更新时间), 0.0001f, 0.1f);
        上次更新时间 = currentTime;
        return deltaTime;
    }

    private static double 取当前时间()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            return UnityEditor.EditorApplication.timeSinceStartup;
        }
#endif

        return Time.timeAsDouble;
    }

    private void 重置采样()
    {
        已采样 = false;
        距上次生成时间 = 0f;
    }

    private void 销毁片段(拖尾片段 segment)
    {
        if (segment == null)
        {
            return;
        }

        if (segment.网格 != null)
        {
            销毁对象(segment.网格);
        }

        if (segment.物体 != null)
        {
            销毁对象(segment.物体);
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

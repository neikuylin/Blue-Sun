using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
[AddComponentMenu("特效/武器火焰附魔控制器")]
public sealed class 武器火焰附魔控制器 : MonoBehaviour
{
    private const string 默认火焰材质路径 = "武器火焰附魔Sprite材质";

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

    [SerializeField] private bool 启用附魔 = true;
    [SerializeField] private Material 火焰材质;
    [SerializeField] private Color 颜色 = Color.white;
    [SerializeField] private Color 暗部火焰颜色 = new Color(0.75f, 0.08f, 0.02f, 1f);
    [SerializeField] private Color 主火焰颜色 = new Color(1f, 0.32f, 0.04f, 1f);
    [SerializeField] private Color 核心火焰颜色 = new Color(1f, 0.9f, 0.28f, 1f);
    [SerializeField, Range(0f, 4f)] private float 火焰强度 = 1.15f;
    [SerializeField, Range(0f, 10f)] private float 火焰速度 = 2.4f;
    [SerializeField, Range(0.1f, 40f)] private float 火焰密度 = 11f;
    [SerializeField] private Vector2 流动方向 = Vector2.up;
    [SerializeField, Range(0f, 1f)] private float 原图保留强度 = 0.72f;
    [SerializeField, Range(0f, 8f)] private float 外扩火焰范围 = 2f;
    [SerializeField, Range(0f, 4f)] private float 外扩火焰强度 = 1.3f;
    [SerializeField, Range(0f, 1f)] private float 闪烁强度 = 0.22f;
    [SerializeField, Range(0f, 12f)] private float 闪烁速度 = 4f;
    [SerializeField, HideInInspector] private Material 原始材质;

    private SpriteRenderer spriteRenderer;
    private MaterialPropertyBlock propertyBlock;

    public bool 当前启用附魔 => 启用附魔;
    public Material 当前火焰材质 => 火焰材质;
    public Material 当前原始材质 => 原始材质;

    private void Reset()
    {
        取组件和参数块();
        记录原始材质();
        加载默认火焰材质();
        应用附魔设置();
    }

    private void OnEnable()
    {
        应用附魔设置();
    }

    private void OnDisable()
    {
        还原原始材质();
    }

    private void OnValidate()
    {
        火焰强度 = Mathf.Clamp(火焰强度, 0f, 4f);
        火焰速度 = Mathf.Clamp(火焰速度, 0f, 10f);
        火焰密度 = Mathf.Clamp(火焰密度, 0.1f, 40f);
        原图保留强度 = Mathf.Clamp01(原图保留强度);
        外扩火焰范围 = Mathf.Clamp(外扩火焰范围, 0f, 8f);
        外扩火焰强度 = Mathf.Clamp(外扩火焰强度, 0f, 4f);
        闪烁强度 = Mathf.Clamp01(闪烁强度);
        闪烁速度 = Mathf.Clamp(闪烁速度, 0f, 12f);
        应用附魔设置();
    }

    [ContextMenu("应用武器火焰附魔")]
    public void 应用附魔设置()
    {
        取组件和参数块();
        if (spriteRenderer == null)
        {
            Debug.LogWarning($"[武器火焰附魔] {name} 缺少 SpriteRenderer，无法应用火焰附魔。", this);
            return;
        }

        if (!启用附魔)
        {
            还原原始材质();
            return;
        }

        记录原始材质();
        加载默认火焰材质();
        if (火焰材质 == null)
        {
            Debug.LogWarning($"[武器火焰附魔] {name} 没有指定火焰材质，无法应用火焰附魔。", this);
            return;
        }

        spriteRenderer.sharedMaterial = 火焰材质;
        spriteRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(ColorId, 颜色);
        propertyBlock.SetColor(DarkFireColorId, 暗部火焰颜色);
        propertyBlock.SetColor(MainFireColorId, 主火焰颜色);
        propertyBlock.SetColor(CoreFireColorId, 核心火焰颜色);
        propertyBlock.SetFloat(FireIntensityId, 火焰强度);
        propertyBlock.SetFloat(FireSpeedId, 火焰速度);
        propertyBlock.SetFloat(FireScaleId, 火焰密度);
        propertyBlock.SetVector(FlowDirectionId, 取标准化流动方向());
        propertyBlock.SetFloat(OriginalKeepId, 原图保留强度);
        propertyBlock.SetFloat(OuterFireRangeId, 外扩火焰范围);
        propertyBlock.SetFloat(OuterFireIntensityId, 外扩火焰强度);
        propertyBlock.SetFloat(FlickerStrengthId, 闪烁强度);
        propertyBlock.SetFloat(FlickerSpeedId, 闪烁速度);
        spriteRenderer.SetPropertyBlock(propertyBlock);
    }

    [ContextMenu("还原武器原始材质")]
    public void 还原原始材质()
    {
        取组件和参数块();
        if (spriteRenderer == null)
        {
            return;
        }

        propertyBlock.Clear();
        spriteRenderer.SetPropertyBlock(propertyBlock);

        if (原始材质 != null)
        {
            spriteRenderer.sharedMaterial = 原始材质;
        }
    }

    private void 记录原始材质()
    {
        if (spriteRenderer == null || 原始材质 != null)
        {
            return;
        }

        if (火焰材质 != null && spriteRenderer.sharedMaterial == 火焰材质)
        {
            return;
        }

        原始材质 = spriteRenderer.sharedMaterial;
    }

    private void 加载默认火焰材质()
    {
        if (火焰材质 != null)
        {
            return;
        }

        火焰材质 = Resources.Load<Material>(默认火焰材质路径);
    }

    private Vector4 取标准化流动方向()
    {
        Vector2 direction = 流动方向;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = Vector2.up;
        }

        direction.Normalize();
        return new Vector4(direction.x, direction.y, 0f, 0f);
    }

    private void 取组件和参数块()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }
    }
}

using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[AddComponentMenu("特效/武器火焰附魔控制器")]
public sealed class 武器火焰附魔控制器 : MonoBehaviour
{
    private const string 默认火焰材质路径 = "武器火焰附魔模型材质";

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
    [SerializeField, HideInInspector] private Material[] 原始材质列表;

    private MeshRenderer meshRenderer;
    private MaterialPropertyBlock propertyBlock;

    public bool 当前启用附魔 => 启用附魔;
    public Material 当前火焰材质 => 火焰材质;
    public Material[] 当前原始材质列表 => 原始材质列表;

    private void Reset()
    {
        取组件和参数块();
        记录原始材质列表();
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
        if (meshRenderer == null)
        {
            Debug.LogWarning($"[武器火焰附魔] {name} 缺少 MeshRenderer，无法应用3D武器火焰附魔。", this);
            return;
        }

        if (!启用附魔)
        {
            还原原始材质();
            return;
        }

        记录原始材质列表();
        加载默认火焰材质();
        if (火焰材质 == null)
        {
            Debug.LogWarning($"[武器火焰附魔] {name} 没有指定火焰材质，无法应用火焰附魔。", this);
            return;
        }

        应用火焰材质到全部材质槽();
        meshRenderer.GetPropertyBlock(propertyBlock);
        写入原始材质参数(propertyBlock);
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
        meshRenderer.SetPropertyBlock(propertyBlock);
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

        if (火焰材质 != null && 全部材质都是火焰材质(currentMaterials))
        {
            return;
        }

        原始材质列表 = currentMaterials;
    }

    private bool 全部材质都是火焰材质(Material[] materials)
    {
        if (materials == null || materials.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < materials.Length; i++)
        {
            if (materials[i] != 火焰材质)
            {
                return false;
            }
        }

        return true;
    }

    private void 应用火焰材质到全部材质槽()
    {
        Material[] currentMaterials = meshRenderer.sharedMaterials;
        if (currentMaterials == null || currentMaterials.Length == 0)
        {
            meshRenderer.sharedMaterial = 火焰材质;
            return;
        }

        Material[] fireMaterials = new Material[currentMaterials.Length];
        for (int i = 0; i < fireMaterials.Length; i++)
        {
            fireMaterials[i] = 火焰材质;
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
        if (meshRenderer == null)
        {
            meshRenderer = GetComponent<MeshRenderer>();
        }

        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }
    }
}

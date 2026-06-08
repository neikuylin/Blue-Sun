using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class 渲染层级应用器 : MonoBehaviour
{
    private const string Below3DMaterialResourcePath = "渲染层级_低于3D不写深度Sprite材质";
    private const string Above3DMaterialResourcePath = "渲染层级_高于3D不写深度Sprite材质";
    private const string UndecidedMaterialResourcePath = "渲染层级_不决定不写深度Sprite材质";
    private const string UnlitBelow3DMaterialResourcePath = "渲染层级_低于3D不受光不写深度Sprite材质";
    private const string UnlitAbove3DMaterialResourcePath = "渲染层级_高于3D不受光不写深度Sprite材质";
    private const string UnlitUndecidedMaterialResourcePath = "渲染层级_不决定不受光不写深度Sprite材质";

    public enum 渲染层级模式
    {
        低于3D = 0,
        高于3D = 1,
        不决定 = 2
    }

    public enum 光照模式
    {
        受光 = 0,
        不受光 = 1
    }

    [Header("层级")]
    [Tooltip("低于3D：适合地板/背景。高于3D：适合高亮/提示/部分特效。不决定：按正常深度关系处理遮挡。")]
    [SerializeField] private 渲染层级模式 mode = 渲染层级模式.低于3D;
    [Tooltip("受光会响应主光和局部光。不受光保持贴图原本亮度，但仍保留当前层级和挖空规则。")]
    [SerializeField] private 光照模式 lightingMode = 光照模式.受光;
    [Tooltip("开启时会把目标渲染器的排序值改成下面的值。关闭时保留 SpriteRenderer 自身排序。")]
    [SerializeField] private bool overwriteSortingOrder;
    [Tooltip("同类2D渲染之间的前后顺序。数值越大越靠前。")]
    [SerializeField] private int sortingOrder = -10000;

    [Header("材质")]
    [Tooltip("开启时才会按渲染模式替换目标渲染器材质。关闭时只处理层级，不改材质。")]
    [SerializeField] private bool replaceMaterial;
    [Tooltip("低于3D时使用的材质。为空会自动从 Resources 加载。")]
    [SerializeField] private Material below3DMaterial;
    [Tooltip("高于3D时使用的材质。为空会自动从 Resources 加载。")]
    [SerializeField] private Material above3DMaterial;
    [Tooltip("不决定时使用的材质。为空会自动从 Resources 加载。")]
    [SerializeField] private Material undecidedMaterial;
    [Tooltip("低于3D且不受光时使用的材质。为空会自动从 Resources 加载。")]
    [SerializeField] private Material unlitBelow3DMaterial;
    [Tooltip("高于3D且不受光时使用的材质。为空会自动从 Resources 加载。")]
    [SerializeField] private Material unlitAbove3DMaterial;
    [Tooltip("不决定且不受光时使用的材质。为空会自动从 Resources 加载。")]
    [SerializeField] private Material unlitUndecidedMaterial;

    [Header("目标")]
    [Tooltip("是否包含未激活的子物体。")]
    [SerializeField] private bool includeInactive = true;
    [Tooltip("改 Inspector 参数时是否自动应用。关闭后可用右键菜单手动应用。")]
    [SerializeField] private bool applyOnValidate = true;
    [Tooltip("处理 SpriteRenderer。地板、2D装饰通常勾这个。")]
    [SerializeField] private bool applySpriteRenderers = true;
    [Tooltip("处理 ParticleSystemRenderer。会替换粒子材质，可能改变特效外观。")]
    [SerializeField] private bool applyParticleRenderers;
    [Tooltip("处理 LineRenderer。会替换线条材质。")]
    [SerializeField] private bool applyLineRenderers;
    [Tooltip("处理 TrailRenderer。会替换拖尾材质。")]
    [SerializeField] private bool applyTrailRenderers;
    [Tooltip("处理 MeshRenderer。会替换网格材质，谨慎使用。")]
    [SerializeField] private bool applyMeshRenderers;

    private void OnEnable()
    {
        Apply();
    }

    private void OnValidate()
    {
        if (applyOnValidate)
        {
            Apply();
        }
    }

    public bool 使用脚下格子判定遮挡 => mode == 渲染层级模式.不决定;

    [ContextMenu("应用渲染层级")]
    public void Apply()
    {
        Material targetMaterial = replaceMaterial ? ResolveMaterial() : null;

        if (applySpriteRenderers)
        {
            ApplyToSpriteRenderers(targetMaterial);
        }

        ApplyToRenderers<ParticleSystemRenderer>(targetMaterial, applyParticleRenderers);
        ApplyToRenderers<LineRenderer>(targetMaterial, applyLineRenderers);
        ApplyToRenderers<TrailRenderer>(targetMaterial, applyTrailRenderers);
        ApplyToRenderers<MeshRenderer>(targetMaterial, applyMeshRenderers);
    }

    private Material ResolveMaterial()
    {
        if (lightingMode == 光照模式.不受光)
        {
            switch (mode)
            {
                case 渲染层级模式.不决定:
                    return LoadMaterial(ref unlitUndecidedMaterial, UnlitUndecidedMaterialResourcePath);
                case 渲染层级模式.高于3D:
                    return LoadMaterial(ref unlitAbove3DMaterial, UnlitAbove3DMaterialResourcePath);
                default:
                    return LoadMaterial(ref unlitBelow3DMaterial, UnlitBelow3DMaterialResourcePath);
            }
        }

        switch (mode)
        {
            case 渲染层级模式.不决定:
                return LoadMaterial(ref undecidedMaterial, UndecidedMaterialResourcePath);
            case 渲染层级模式.高于3D:
                return LoadMaterial(ref above3DMaterial, Above3DMaterialResourcePath);
            default:
                return LoadMaterial(ref below3DMaterial, Below3DMaterialResourcePath);
        }
    }

    private static Material LoadMaterial(ref Material material, string resourcePath)
    {
        if (material == null)
        {
            material = Resources.Load<Material>(resourcePath);
        }

        return material;
    }

    private void ApplyToSpriteRenderers(Material targetMaterial)
    {
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(includeInactive);
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer spriteRenderer = renderers[i];
            if (spriteRenderer == null)
            {
                continue;
            }

            if (overwriteSortingOrder)
            {
                spriteRenderer.sortingOrder = sortingOrder;
            }

            if (targetMaterial != null)
            {
                spriteRenderer.sharedMaterial = targetMaterial;
            }
        }
    }

    private void ApplyToRenderers<T>(Material targetMaterial, bool enabled) where T : Renderer
    {
        if (!enabled)
        {
            return;
        }

        T[] renderers = GetComponentsInChildren<T>(includeInactive);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            if (overwriteSortingOrder)
            {
                renderer.sortingOrder = sortingOrder;
            }

            if (targetMaterial != null)
            {
                renderer.sharedMaterial = targetMaterial;
            }
        }
    }
}

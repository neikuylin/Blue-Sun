using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class 渲染层级应用器 : MonoBehaviour
{
    private const string Below3DMaterialResourcePath = "渲染层级_低于3D不写深度Sprite材质";
    private const string Above3DMaterialResourcePath = "渲染层级_高于3D不写深度Sprite材质";

    public enum 渲染层级模式
    {
        低于3D = 0,
        高于3D = 1
    }

    [Header("层级")]
    [SerializeField] private 渲染层级模式 mode = 渲染层级模式.低于3D;
    [SerializeField] private int sortingOrder = -10000;

    [Header("材质")]
    [SerializeField] private Material below3DMaterial;
    [SerializeField] private Material above3DMaterial;

    [Header("目标")]
    [SerializeField] private bool includeInactive = true;
    [SerializeField] private bool applyOnValidate = true;
    [SerializeField] private bool applySpriteRenderers = true;
    [SerializeField] private bool applyParticleRenderers;
    [SerializeField] private bool applyLineRenderers;
    [SerializeField] private bool applyTrailRenderers;
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

    [ContextMenu("应用渲染层级")]
    public void Apply()
    {
        Material targetMaterial = ResolveMaterial();
        if (targetMaterial == null)
        {
            return;
        }

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
        if (mode == 渲染层级模式.高于3D)
        {
            if (above3DMaterial == null)
            {
                above3DMaterial = Resources.Load<Material>(Above3DMaterialResourcePath);
            }

            return above3DMaterial;
        }

        if (below3DMaterial == null)
        {
            below3DMaterial = Resources.Load<Material>(Below3DMaterialResourcePath);
        }

        return below3DMaterial;
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

            spriteRenderer.sortingOrder = sortingOrder;
            spriteRenderer.sharedMaterial = targetMaterial;
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

            renderer.sortingOrder = sortingOrder;
            renderer.sharedMaterial = targetMaterial;
        }
    }
}

using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("战斗/换房时墙体高于3D材质切换器")]
public sealed class 换房时墙体高于3D材质切换器 : MonoBehaviour
{
    private const string Above3DMaterialResourcePath = "渲染层级_高于3D不写深度Sprite材质";

    [Header("材质")]
    [InspectorName("高于3D不写深度材质")]
    [SerializeField] private Material 高于3D不写深度材质;

    [Header("目标")]
    [InspectorName("包含未激活子物体")]
    [SerializeField] private bool 包含未激活子物体 = true;
    [InspectorName("处理SpriteRenderer")]
    [SerializeField] private bool 处理SpriteRenderer = true;
    [InspectorName("处理ParticleSystemRenderer")]
    [SerializeField] private bool 处理ParticleSystemRenderer;
    [InspectorName("处理LineRenderer")]
    [SerializeField] private bool 处理LineRenderer;
    [InspectorName("处理TrailRenderer")]
    [SerializeField] private bool 处理TrailRenderer;
    [InspectorName("处理MeshRenderer")]
    [SerializeField] private bool 处理MeshRenderer;

    private void OnEnable()
    {
        BattleTurnSystem.换房移动开始 += On换房移动开始;
    }

    private void OnDisable()
    {
        BattleTurnSystem.换房移动开始 -= On换房移动开始;
    }

    [ContextMenu("切换为高于3D不写深度材质")]
    public void 切换为高于3D不写深度材质()
    {
        Material material = ResolveMaterial();
        if (material == null)
        {
            Debug.LogError("换房时墙体高于3D材质切换器：找不到高于3D不写深度材质。", this);
            return;
        }

        if (处理SpriteRenderer)
        {
            ApplyToRenderers<SpriteRenderer>(material);
        }

        if (处理ParticleSystemRenderer)
        {
            ApplyToRenderers<ParticleSystemRenderer>(material);
        }

        if (处理LineRenderer)
        {
            ApplyToRenderers<LineRenderer>(material);
        }

        if (处理TrailRenderer)
        {
            ApplyToRenderers<TrailRenderer>(material);
        }

        if (处理MeshRenderer)
        {
            ApplyToRenderers<MeshRenderer>(material);
        }
    }

    private void On换房移动开始(MapTemplateDatabase.ConnectionDirection direction)
    {
        切换为高于3D不写深度材质();
    }

    private Material ResolveMaterial()
    {
        if (高于3D不写深度材质 == null)
        {
            高于3D不写深度材质 = Resources.Load<Material>(Above3DMaterialResourcePath);
        }

        return 高于3D不写深度材质;
    }

    private void ApplyToRenderers<T>(Material material) where T : Renderer
    {
        T[] renderers = GetComponentsInChildren<T>(包含未激活子物体);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }
        }
    }
}

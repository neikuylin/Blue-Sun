using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[AddComponentMenu("战斗/精灵局部光硬边控制器")]
public sealed class 精灵局部光硬边控制器 : MonoBehaviour
{
    private const string ProjectorName = "硬边光斑_自动生成";
    private const string ProjectorShaderName = "项目/渲染/精灵硬边局部光投影";
    private const string ProjectorShaderResourcePath = "精灵硬边局部光投影";

    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
    private static readonly int ThresholdId = Shader.PropertyToID("_Threshold");
    private static readonly int SoftnessId = Shader.PropertyToID("_Softness");
    private static readonly int SpotEnabledId = Shader.PropertyToID("_SpotEnabled");
    private static readonly int SpotDirectionId = Shader.PropertyToID("_SpotDirection");
    private static readonly int SpotOuterCosId = Shader.PropertyToID("_SpotOuterCos");
    private static readonly int SpotSoftnessId = Shader.PropertyToID("_SpotSoftness");

    private static Mesh quadMesh;

    [Header("绑定光源")]
    [InspectorName("目标 Light")]
    [Tooltip("只读取这盏 Light 的参数生成硬边光斑。留空时使用同物体上的 Light。")]
    [SerializeField] private Light targetLight;

    [InspectorName("启用硬边光斑")]
    [SerializeField] private bool hardEdge = true;

    [Header("光斑")]
    [InspectorName("颜色倍率")]
    [SerializeField] private Color colorMultiplier = Color.white;

    [InspectorName("强度倍率")]
    [Min(0f)]
    [SerializeField] private float intensityMultiplier = 1f;

    [InspectorName("半径倍率")]
    [Min(0.001f)]
    [SerializeField] private float radiusMultiplier = 1f;

    [InspectorName("位置偏移")]
    [SerializeField] private Vector3 positionOffset;

    [Header("硬边")]
    [InspectorName("硬边阈值")]
    [Tooltip("数值越大，亮区越小。")]
    [Range(0f, 1f)]
    [SerializeField] private float threshold = 0.18f;

    [InspectorName("边缘过渡宽度")]
    [Tooltip("越接近 0 越硬，略大一点可以减少锯齿。")]
    [Range(0.001f, 0.25f)]
    [SerializeField] private float softness = 0.03f;

    [Header("投影")]
    [InspectorName("对齐主摄像机")]
    [Tooltip("开启后光斑平面朝向 Main Camera。关闭后使用本物体旋转。")]
    [SerializeField] private bool alignToMainCamera = true;

    [InspectorName("排序层")]
    [SerializeField] private string sortingLayerName = "Default";

    [InspectorName("排序值")]
    [SerializeField] private int sortingOrder = -8990;

    [Header("聚光灯")]
    [InspectorName("使用 Spot 角度")]
    [SerializeField] private bool useSpotAngle = true;

    [InspectorName("Spot 边缘过渡")]
    [Range(0.001f, 0.5f)]
    [SerializeField] private float spotSoftness = 0.05f;

    private MeshRenderer projectorRenderer;
    private MeshFilter projectorFilter;
    private Material projectorMaterial;
    private MaterialPropertyBlock propertyBlock;

    private void OnEnable()
    {
        ResolveDefaultTargetLight();
        Apply();
    }

    private void OnDisable()
    {
        SetProjectorVisible(false);
    }

    private void OnDestroy()
    {
        DestroyGeneratedResources();
    }

    private void OnValidate()
    {
        ResolveDefaultTargetLight();
        Apply();
    }

    private void LateUpdate()
    {
        Apply();
    }

    [ContextMenu("应用硬边光斑")]
    public void Apply()
    {
        if (!ShouldRender())
        {
            SetProjectorVisible(false);
            return;
        }

        EnsureProjector();
        if (projectorRenderer == null || projectorFilter == null || projectorMaterial == null)
        {
            return;
        }

        UpdateProjectorTransform();
        UpdateProjectorProperties();
        SetProjectorVisible(true);
    }

    private bool ShouldRender()
    {
        return hardEdge
            && isActiveAndEnabled
            && targetLight != null
            && targetLight.enabled
            && targetLight.type != LightType.Directional
            && targetLight.range > 0f
            && targetLight.intensity > 0f;
    }

    private void ResolveDefaultTargetLight()
    {
        if (targetLight == null)
        {
            targetLight = GetComponent<Light>();
        }
    }

    private void EnsureProjector()
    {
        if (projectorRenderer != null && projectorFilter != null && projectorMaterial != null)
        {
            return;
        }

        Transform projectorTransform = transform.Find(ProjectorName);
        if (projectorTransform == null)
        {
            GameObject projectorObject = new GameObject(ProjectorName);
            projectorObject.hideFlags = HideFlags.HideAndDontSave;
            projectorObject.transform.SetParent(transform, false);
            projectorTransform = projectorObject.transform;
        }
        else
        {
            projectorTransform.gameObject.hideFlags = HideFlags.HideAndDontSave;
        }

        projectorFilter = projectorTransform.GetComponent<MeshFilter>();
        if (projectorFilter == null)
        {
            projectorFilter = projectorTransform.gameObject.AddComponent<MeshFilter>();
        }

        projectorRenderer = projectorTransform.GetComponent<MeshRenderer>();
        if (projectorRenderer == null)
        {
            projectorRenderer = projectorTransform.gameObject.AddComponent<MeshRenderer>();
        }

        projectorFilter.sharedMesh = EnsureQuadMesh();
        projectorRenderer.sharedMaterial = EnsureProjectorMaterial();
        projectorRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        projectorRenderer.receiveShadows = false;
        projectorRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        projectorRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
    }

    private Material EnsureProjectorMaterial()
    {
        Shader shader = Resources.Load<Shader>(ProjectorShaderResourcePath);
        if (shader == null)
        {
            shader = Shader.Find(ProjectorShaderName);
        }

        if (shader == null)
        {
            return null;
        }

        if (projectorMaterial == null || projectorMaterial.shader != shader)
        {
            DestroyMaterial();
            projectorMaterial = new Material(shader)
            {
                name = "精灵硬边局部光投影_实例",
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        return projectorMaterial;
    }

    private static Mesh EnsureQuadMesh()
    {
        if (quadMesh != null)
        {
            return quadMesh;
        }

        quadMesh = new Mesh
        {
            name = "精灵硬边局部光投影Quad",
            hideFlags = HideFlags.HideAndDontSave
        };
        quadMesh.vertices = new[]
        {
            new Vector3(-1f, -1f, 0f),
            new Vector3(1f, -1f, 0f),
            new Vector3(1f, 1f, 0f),
            new Vector3(-1f, 1f, 0f)
        };
        quadMesh.uv = new[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f)
        };
        quadMesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        quadMesh.RecalculateBounds();
        return quadMesh;
    }

    private void UpdateProjectorTransform()
    {
        Transform projectorTransform = projectorRenderer.transform;
        Quaternion rotation = ResolveProjectorRotation();
        float radius = Mathf.Max(0.001f, targetLight.range * radiusMultiplier);

        projectorTransform.SetPositionAndRotation(targetLight.transform.position + positionOffset, rotation);
        projectorTransform.localScale = new Vector3(radius, radius, 1f);
    }

    private Quaternion ResolveProjectorRotation()
    {
        if (alignToMainCamera && Camera.main != null)
        {
            return Camera.main.transform.rotation;
        }

        return transform.rotation;
    }

    private void UpdateProjectorProperties()
    {
        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }

        projectorRenderer.GetPropertyBlock(propertyBlock);
        projectorRenderer.sortingLayerName = sortingLayerName;
        projectorRenderer.sortingOrder = sortingOrder;

        Color color = new Color(
            targetLight.color.r * colorMultiplier.r,
            targetLight.color.g * colorMultiplier.g,
            targetLight.color.b * colorMultiplier.b,
            targetLight.color.a * colorMultiplier.a);
        float intensity = Mathf.Max(0f, targetLight.intensity * intensityMultiplier);
        bool spotEnabled = useSpotAngle && targetLight.type == LightType.Spot;

        propertyBlock.SetColor(ColorId, color);
        propertyBlock.SetFloat(IntensityId, intensity);
        propertyBlock.SetFloat(ThresholdId, Mathf.Clamp01(threshold));
        propertyBlock.SetFloat(SoftnessId, Mathf.Max(0.001f, softness));
        propertyBlock.SetFloat(SpotEnabledId, spotEnabled ? 1f : 0f);
        propertyBlock.SetFloat(SpotOuterCosId, Mathf.Cos(targetLight.spotAngle * 0.5f * Mathf.Deg2Rad));
        propertyBlock.SetFloat(SpotSoftnessId, Mathf.Max(0.001f, spotSoftness));
        propertyBlock.SetVector(SpotDirectionId, ResolveSpotDirection(projectorRenderer.transform.rotation));
        projectorRenderer.SetPropertyBlock(propertyBlock);
    }

    private Vector4 ResolveSpotDirection(Quaternion projectorRotation)
    {
        Vector3 localDirection = Quaternion.Inverse(projectorRotation) * targetLight.transform.forward;
        Vector2 projected = new Vector2(localDirection.x, localDirection.y);
        if (projected.sqrMagnitude < 0.0001f)
        {
            projected = Vector2.up;
        }

        projected.Normalize();
        return new Vector4(projected.x, projected.y, 0f, 0f);
    }

    private void SetProjectorVisible(bool visible)
    {
        if (projectorRenderer != null)
        {
            projectorRenderer.enabled = visible;
        }
    }

    private void DestroyGeneratedResources()
    {
        DestroyMaterial();

        Transform projectorTransform = transform.Find(ProjectorName);
        if (projectorTransform == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(projectorTransform.gameObject);
        }
        else
        {
            DestroyImmediate(projectorTransform.gameObject);
        }
    }

    private void DestroyMaterial()
    {
        if (projectorMaterial == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(projectorMaterial);
        }
        else
        {
            DestroyImmediate(projectorMaterial);
        }

        projectorMaterial = null;
    }
}

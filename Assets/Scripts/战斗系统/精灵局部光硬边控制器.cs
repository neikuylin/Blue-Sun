using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(Light))]
[AddComponentMenu("战斗/精灵局部光硬边控制器")]
public sealed class 精灵局部光硬边控制器 : MonoBehaviour
{
    private static readonly int HardEdgeEnabledId = Shader.PropertyToID("_SpriteLocalLightHardEdgeEnabled");
    private static readonly int HardEdgeThresholdId = Shader.PropertyToID("_SpriteLocalLightHardEdgeThreshold");
    private static readonly int HardEdgeSoftnessId = Shader.PropertyToID("_SpriteLocalLightHardEdgeSoftness");

    private static 精灵局部光硬边控制器 activeController;

    [Header("精灵局部光")]
    [InspectorName("启用硬边")]
    [Tooltip("开启后，使用渲染层级受光材质的 Sprite 会把点光/聚光灯衰减切成硬边。")]
    [SerializeField] private bool hardEdge = true;
    [InspectorName("硬边阈值")]
    [Tooltip("硬边阈值。数值越大，亮区越小。")]
    [Range(0f, 1f)]
    [SerializeField] private float threshold = 0.18f;
    [InspectorName("边缘过渡宽度")]
    [Tooltip("边缘过渡宽度。越接近 0 越硬，略大一点可以减少锯齿。")]
    [Range(0.001f, 0.25f)]
    [SerializeField] private float softness = 0.03f;

    private void OnEnable()
    {
        activeController = this;
        Apply();
    }

    private void OnDisable()
    {
        if (activeController == this)
        {
            activeController = null;
            Clear();
        }
    }

    private void OnValidate()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        activeController = this;
        Apply();
    }

    private void Update()
    {
        if (activeController == this)
        {
            Apply();
        }
    }

    [ContextMenu("应用 Sprite 局部光硬边")]
    public void Apply()
    {
        Shader.SetGlobalFloat(HardEdgeEnabledId, hardEdge ? 1f : 0f);
        Shader.SetGlobalFloat(HardEdgeThresholdId, Mathf.Clamp01(threshold));
        Shader.SetGlobalFloat(HardEdgeSoftnessId, Mathf.Max(0.001f, softness));
    }

    private static void Clear()
    {
        Shader.SetGlobalFloat(HardEdgeEnabledId, 0f);
        Shader.SetGlobalFloat(HardEdgeThresholdId, 0f);
        Shader.SetGlobalFloat(HardEdgeSoftnessId, 0.03f);
    }
}

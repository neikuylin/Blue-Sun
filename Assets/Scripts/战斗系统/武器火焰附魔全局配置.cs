using UnityEngine;

[CreateAssetMenu(fileName = "武器火焰附魔全局配置", menuName = "配置/武器火焰附魔全局配置")]
public sealed class 武器火焰附魔全局配置 : ScriptableObject
{
    [Header("开关与材质")]
    public bool 启用附魔 = true;
    public Material 火焰材质;

    [Header("火焰颜色")]
    public Color 颜色 = Color.white;
    public Color 暗部火焰颜色 = new Color(0.75f, 0.08f, 0.02f, 1f);
    public Color 主火焰颜色 = new Color(1f, 0.32f, 0.04f, 1f);
    public Color 核心火焰颜色 = new Color(1f, 0.9f, 0.28f, 1f);

    [Header("火焰流动")]
    [Range(0f, 4f)] public float 火焰强度 = 1.15f;
    [Range(0f, 10f)] public float 火焰速度 = 2.4f;
    [Range(0.1f, 40f)] public float 火焰密度 = 11f;
    public Vector2 流动方向 = Vector2.up;
    [Range(0f, 1f)] public float 原图保留强度 = 0.72f;

    [Header("外扩包围火焰")]
    [Range(0f, 8f)] public float 外扩火焰范围 = 2f;
    [Range(0f, 4f)] public float 外扩火焰强度 = 1.3f;
    [Range(0f, 1f)] public float 闪烁强度 = 0.22f;
    [Range(0f, 12f)] public float 闪烁速度 = 4f;

    [Header("上飘火星粒子")]
    public bool 启用火星粒子 = true;
    public bool 火星无视深度 = true;
    public Material 火星粒子材质;
    [Range(0f, 80f)] public float 火星数量 = 10f;
    [Range(0.001f, 0.2f)] public float 火星大小 = 0.035f;
    [Range(0f, 5f)] public float 火星上升速度 = 1.2f;
    [Range(0f, 2f)] public float 火星左右扰动 = 0.35f;
    [Range(0.05f, 3f)] public float 火星生命周期 = 0.8f;
    public Color 火星圆点颜色 = new Color(1f, 0.86f, 0.24f, 1f);
    public Color 火星外发光颜色 = new Color(1f, 0.22f, 0.04f, 0.55f);
    public Color 火星起始颜色 = new Color(1f, 0.72f, 0.18f, 1f);
    public Color 火星结束颜色 = new Color(1f, 0.12f, 0.02f, 0f);
    [Range(0.05f, 2f)] public float 火星发射范围倍率 = 0.75f;

    [Header("Trail火焰拖尾")]
    public bool 启用Trail火焰拖尾 = true;
    public bool Trail无视深度;
    public Material Trail拖尾材质;
    [Range(0.1f, 4f)] public float Trail整体长轴倍率 = 0.61f;
    [Range(0.03f, 1f)] public float Trail持续时间 = 0.144f;
    [Range(0f, 20f)] public float Trail触发速度阈值 = 17f;
    [Range(0.01f, 1f)] public float Trail最小顶点距离 = 0.814f;
    [Range(0f, 1f)] public float Trail末端宽度倍率 = 1f;
    [Range(0f, 1f)] public float Trail低速宽度收缩 = 0.619f;
    public Color Trail外侧颜色 = new Color(1f, 0.16f, 0.02f, 0.75f);
    public Color Trail内侧颜色 = new Color(1f, 0.82f, 0.18f, 0.95f);
    [Range(0.01f, 1f)] public float Trail边缘柔和 = 0.01f;
    [Range(0.1f, 40f)] public float Trail火焰噪声密度 = 6.6f;
    [Range(0f, 1f)] public float Trail火焰破碎强度 = 0.162f;
    [Range(0f, 6f)] public float Trail亮度 = 1.03f;

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
        火星数量 = Mathf.Clamp(火星数量, 0f, 80f);
        火星大小 = Mathf.Clamp(火星大小, 0.001f, 0.2f);
        火星上升速度 = Mathf.Clamp(火星上升速度, 0f, 5f);
        火星左右扰动 = Mathf.Clamp(火星左右扰动, 0f, 2f);
        火星生命周期 = Mathf.Clamp(火星生命周期, 0.05f, 3f);
        火星发射范围倍率 = Mathf.Clamp(火星发射范围倍率, 0.05f, 2f);
        Trail整体长轴倍率 = Mathf.Clamp(Trail整体长轴倍率, 0.1f, 4f);
        Trail持续时间 = Mathf.Clamp(Trail持续时间, 0.03f, 1f);
        Trail触发速度阈值 = Mathf.Max(0f, Trail触发速度阈值);
        Trail最小顶点距离 = Mathf.Clamp(Trail最小顶点距离, 0.01f, 1f);
        Trail末端宽度倍率 = Mathf.Clamp01(Trail末端宽度倍率);
        Trail低速宽度收缩 = Mathf.Clamp01(Trail低速宽度收缩);
        Trail边缘柔和 = Mathf.Clamp01(Trail边缘柔和);
        Trail火焰噪声密度 = Mathf.Clamp(Trail火焰噪声密度, 0.1f, 40f);
        Trail火焰破碎强度 = Mathf.Clamp01(Trail火焰破碎强度);
        Trail亮度 = Mathf.Clamp(Trail亮度, 0f, 6f);
    }
}

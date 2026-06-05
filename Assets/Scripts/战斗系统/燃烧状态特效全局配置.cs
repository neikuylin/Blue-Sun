using UnityEngine;

[CreateAssetMenu(fileName = "燃烧状态特效全局配置", menuName = "战斗/燃烧状态特效全局配置")]
public sealed class 燃烧状态特效全局配置 : ScriptableObject
{
    public const string DefaultResourcePath = "燃烧状态特效全局配置";

    [Header("粒子外观")]
    public Material 火焰材质;
    public Mesh 粒子网格;
    public Color 火焰颜色 = new Color(1f, 0.38f, 0.08f, 0.72f);
    public Color 中心颜色 = new Color(1f, 0.92f, 0.6f, 1f);
    public Color 外围颜色 = new Color(1f, 0.15f, 0.02f, 1f);
    public float 结束透明度 = 0f;
    public float 火焰大小 = 0.12f;
    public float 火焰大小浮动 = 0.05f;

    [Header("贴图切帧")]
    public int 贴图横向格数 = 3;
    public int 贴图纵向格数 = 3;
    public int 贴图动画帧率 = 30;
    public int 贴图动画循环次数 = 1;

    [Header("发射")]
    public int 火焰数量 = 28;
    public int 最大粒子数 = 120;
    public float 火焰生命周期 = 0.65f;
    public float 上飘速度 = 0.35f;
    public float 表面散布厚度 = 0.015f;

    public static 燃烧状态特效全局配置 LoadDefault()
    {
        return Resources.Load<燃烧状态特效全局配置>(DefaultResourcePath);
    }

    private void OnValidate()
    {
        火焰数量 = Mathf.Max(0, 火焰数量);
        最大粒子数 = Mathf.Max(1, 最大粒子数);
        结束透明度 = Mathf.Clamp01(结束透明度);
        火焰大小 = Mathf.Max(0.0001f, 火焰大小);
        火焰大小浮动 = Mathf.Max(0f, 火焰大小浮动);
        贴图横向格数 = Mathf.Max(1, 贴图横向格数);
        贴图纵向格数 = Mathf.Max(1, 贴图纵向格数);
        贴图动画帧率 = Mathf.Max(1, 贴图动画帧率);
        贴图动画循环次数 = Mathf.Max(1, 贴图动画循环次数);
        火焰生命周期 = Mathf.Max(0.0001f, 火焰生命周期);
        上飘速度 = Mathf.Max(0f, 上飘速度);
        表面散布厚度 = Mathf.Max(0f, 表面散布厚度);
    }
}

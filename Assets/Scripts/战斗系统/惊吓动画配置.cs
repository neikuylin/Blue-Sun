using UnityEngine;

[CreateAssetMenu(fileName = DefaultResourcePath, menuName = "战斗/惊吓动画配置")]
public sealed class 惊吓动画配置 : ScriptableObject
{
    public const string DefaultResourcePath = "惊吓动画配置";

    [Header("资源")]
    [SerializeField] private GameObject 动画预制体;

    [Header("位置")]
    [SerializeField] private float 右侧偏移 = 0.45f;
    [SerializeField] private float 顶部偏移 = 0.25f;
    [SerializeField] private float 整体缩放 = 1f;

    [Header("播放")]
    [SerializeField] private int 渲染顺序 = 100;

    public GameObject 动画预制体资源 => 动画预制体;
    public float 右侧偏移量 => 右侧偏移;
    public float 顶部偏移量 => 顶部偏移;
    public float 整体缩放值 => Mathf.Max(0.01f, 整体缩放);
    public int 渲染顺序值 => 渲染顺序;

    public static 惊吓动画配置 加载默认配置()
    {
        return Resources.Load<惊吓动画配置>(DefaultResourcePath);
    }
}

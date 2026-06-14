using UnityEngine;

[DisallowMultipleComponent]
public sealed class 可被隐藏UI : MonoBehaviour
{
    public enum 移出方向
    {
        上,
        下
    }

    [SerializeField, Tooltip("隐藏时移出屏幕的方向；显示时会从同一方向移回。")]
    private 移出方向 方向 = 移出方向.上;

    [SerializeField, Min(0f), Tooltip("移出或移回所需秒数，使用未缩放时间。")]
    private float 动画时间 = 0.5f;

    public 移出方向 取得方向 => 方向;
    public float 取得动画时间 => Mathf.Max(0f, 动画时间);
}

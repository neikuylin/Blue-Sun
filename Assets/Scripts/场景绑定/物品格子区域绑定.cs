using UnityEngine;

[DisallowMultipleComponent]
public sealed class 物品格子区域绑定 : MonoBehaviour
{
    public enum 数据来源类型
    {
        仓库,
        背包
    }

    [Header("基础")]
    public 数据来源类型 数据来源 = 数据来源类型.仓库;

    [Header("区域")]
    public RectTransform 格子区域;
    public RectTransform 格子容器;

    [Header("生成")]
    public GameObject 格子模板;

    public RectTransform 区域根 => 格子区域 != null ? 格子区域 : transform as RectTransform;

    public RectTransform 已绑定格子容器 => 格子容器;
}

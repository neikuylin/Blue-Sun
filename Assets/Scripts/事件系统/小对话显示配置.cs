using TMPro;
using UnityEngine;

public sealed class 小对话显示配置 : ScriptableObject
{
    public const string 默认资源路径 = "小对话显示配置";

    public TMP_FontAsset 普通字体;

    public static 小对话显示配置 加载默认配置()
    {
        return Resources.Load<小对话显示配置>(默认资源路径);
    }
}

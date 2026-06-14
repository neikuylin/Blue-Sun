using UnityEditor;
using UnityEngine;

public static class Canvas界面显隐菜单
{
    [MenuItem("工具/界面/隐藏 Canvas UI")]
    private static void 隐藏Canvas界面()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("隐藏 Canvas UI 只能在运行模式中使用。");
            return;
        }

        Canvas界面显隐服务.隐藏普通界面();
    }

    [MenuItem("工具/界面/显示 Canvas UI")]
    private static void 显示Canvas界面()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("显示 Canvas UI 只能在运行模式中使用。");
            return;
        }

        Canvas界面显隐服务.显示普通界面();
    }

    [MenuItem("工具/界面/隐藏 Canvas UI", true)]
    [MenuItem("工具/界面/显示 Canvas UI", true)]
    private static bool 校验运行模式()
    {
        return Application.isPlaying;
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Canvas界面显隐服务
{
    private sealed class 物体状态
    {
        public GameObject 物体;
        public bool 原激活状态;
    }

    private static readonly List<物体状态> 物体状态列表 = new List<物体状态>();

    public static bool 已隐藏 { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void 重置运行状态()
    {
        SceneManager.sceneLoaded -= 场景加载完成;
        物体状态列表.Clear();
        已隐藏 = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void 初始化场景监听()
    {
        SceneManager.sceneLoaded -= 场景加载完成;
        SceneManager.sceneLoaded += 场景加载完成;
    }

    public static void 隐藏普通界面()
    {
        if (已隐藏)
        {
            return;
        }

        物体状态列表.Clear();
        已隐藏 = true;
        应用隐藏到当前对象();
        Debug.Log($"Canvas UI 已隐藏。当前已记录 {物体状态列表.Count} 个挂有“可被隐藏UI”组件的物体。");
    }

    public static void 显示普通界面()
    {
        if (!已隐藏)
        {
            return;
        }

        已隐藏 = false;
        for (int i = 0; i < 物体状态列表.Count; i++)
        {
            物体状态 状态 = 物体状态列表[i];
            if (状态.物体 != null)
            {
                状态.物体.SetActive(状态.原激活状态);
            }
        }

        物体状态列表.Clear();
        Debug.Log("Canvas UI 已恢复。");
    }

    private static void 场景加载完成(Scene 场景, LoadSceneMode 模式)
    {
        if (!已隐藏)
        {
            return;
        }

        应用隐藏到当前对象();
    }

    private static void 应用隐藏到当前对象()
    {
        可被隐藏UI[] 标记列表 =
            Object.FindObjectsByType<可被隐藏UI>(FindObjectsInactive.Include);
        for (int i = 0; i < 标记列表.Length; i++)
        {
            可被隐藏UI 标记 = 标记列表[i];
            if (标记 == null)
            {
                continue;
            }

            GameObject 目标物体 = 标记.gameObject;
            if (已记录(目标物体))
            {
                continue;
            }

            物体状态列表.Add(new 物体状态
            {
                物体 = 目标物体,
                原激活状态 = 目标物体.activeSelf
            });
            目标物体.SetActive(false);
        }
    }

    private static bool 已记录(GameObject 目标物体)
    {
        for (int i = 0; i < 物体状态列表.Count; i++)
        {
            物体状态 状态 = 物体状态列表[i];
            if (状态.物体 == 目标物体)
            {
                return true;
            }
        }

        return false;
    }
}

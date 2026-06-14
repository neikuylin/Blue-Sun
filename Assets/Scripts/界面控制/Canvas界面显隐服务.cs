using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Canvas界面显隐服务
{
    private sealed class 物体状态
    {
        public 可被隐藏UI 标记;
        public GameObject 物体;
        public RectTransform 矩形;
        public bool 原激活状态;
        public Vector2 原位置;
        public Vector2 隐藏位置;
        public Vector2 动画起始位置;
    }

    private sealed class 动画驱动器 : MonoBehaviour
    {
    }

    private static readonly List<物体状态> 物体状态列表 = new List<物体状态>();
    private static 动画驱动器 驱动器;
    private static Coroutine 当前动画;

    public static bool 已隐藏 { get; private set; }
    public static bool 正在播放动画 => 当前动画 != null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void 重置运行状态()
    {
        SceneManager.sceneLoaded -= 场景加载完成;
        物体状态列表.Clear();
        驱动器 = null;
        当前动画 = null;
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

        已隐藏 = true;
        if (物体状态列表.Count == 0)
        {
            收集当前对象();
        }

        for (int i = 0; i < 物体状态列表.Count; i++)
        {
            物体状态 状态 = 物体状态列表[i];
            if (状态.矩形 != null)
            {
                状态.动画起始位置 = 状态.矩形.anchoredPosition;
            }
        }

        开始动画(播放隐藏动画());
        Debug.Log($"Canvas UI 已隐藏。当前已记录 {物体状态列表.Count} 个挂有“可被隐藏UI”组件的物体。");
    }

    public static void 显示普通界面()
    {
        if (!已隐藏)
        {
            return;
        }

        已隐藏 = false;
        开始动画(播放显示动画());
        Debug.Log("Canvas UI 已恢复。");
    }

    private static void 场景加载完成(Scene 场景, LoadSceneMode 模式)
    {
        if (!已隐藏)
        {
            return;
        }

        清理已销毁状态();
        收集当前对象();
        立即应用隐藏状态();
    }

    private static void 收集当前对象()
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

            RectTransform 矩形 = 目标物体.transform as RectTransform;
            if (矩形 == null)
            {
                Debug.LogError($"可被隐藏UI：物体“{目标物体.name}”缺少 RectTransform，无法播放界面移出动画。", 目标物体);
                continue;
            }

            Vector2 原位置 = 矩形.anchoredPosition;
            物体状态列表.Add(new 物体状态
            {
                标记 = 标记,
                物体 = 目标物体,
                矩形 = 矩形,
                原激活状态 = 目标物体.activeSelf,
                原位置 = 原位置,
                隐藏位置 = 计算隐藏位置(标记, 矩形, 原位置),
                动画起始位置 = 原位置
            });
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

    private static Vector2 计算隐藏位置(可被隐藏UI 标记, RectTransform 矩形, Vector2 原位置)
    {
        Canvas 画布 = 矩形.GetComponentInParent<Canvas>();
        float 缩放 = 画布 != null ? Mathf.Max(0.0001f, 画布.scaleFactor) : 1f;
        float 移动距离 = Screen.height / 缩放 + Mathf.Abs(矩形.rect.height);
        float 方向符号 = 标记.取得方向 == 可被隐藏UI.移出方向.上 ? 1f : -1f;
        return 原位置 + Vector2.up * (移动距离 * 方向符号);
    }

    private static void 开始动画(IEnumerator 动画)
    {
        动画驱动器 当前驱动器 = 确保驱动器();
        if (当前动画 != null)
        {
            当前驱动器.StopCoroutine(当前动画);
            当前动画 = null;
        }

        当前动画 = 当前驱动器.StartCoroutine(播放动画包装(动画));
    }

    private static 动画驱动器 确保驱动器()
    {
        if (驱动器 != null)
        {
            return 驱动器;
        }

        GameObject 物体 = new GameObject("Canvas界面显隐服务");
        Object.DontDestroyOnLoad(物体);
        驱动器 = 物体.AddComponent<动画驱动器>();
        return 驱动器;
    }

    private static IEnumerator 播放隐藏动画()
    {
        float 最长时间 = 0f;
        for (int i = 0; i < 物体状态列表.Count; i++)
        {
            物体状态 状态 = 物体状态列表[i];
            if (状态.物体 != null && 状态.原激活状态)
            {
                最长时间 = Mathf.Max(最长时间, 状态.标记.取得动画时间);
            }
        }

        float 已过时间 = 0f;
        while (已过时间 < 最长时间)
        {
            已过时间 += Time.unscaledDeltaTime;
            for (int i = 0; i < 物体状态列表.Count; i++)
            {
                物体状态 状态 = 物体状态列表[i];
                if (状态.物体 == null || !状态.原激活状态)
                {
                    continue;
                }

                float 时间 = 状态.标记.取得动画时间;
                float 进度 = 时间 <= 0f ? 1f : Mathf.Clamp01(已过时间 / 时间);
                状态.矩形.anchoredPosition = Vector2.Lerp(状态.动画起始位置, 状态.隐藏位置, 平滑进度(进度));
            }

            yield return null;
        }

        立即应用隐藏状态();
    }

    private static IEnumerator 播放显示动画()
    {
        float 最长时间 = 0f;
        for (int i = 0; i < 物体状态列表.Count; i++)
        {
            物体状态 状态 = 物体状态列表[i];
            if (状态.物体 == null || !状态.原激活状态)
            {
                continue;
            }

            状态.物体.SetActive(true);
            状态.动画起始位置 = 状态.矩形.anchoredPosition;
            最长时间 = Mathf.Max(最长时间, 状态.标记.取得动画时间);
        }

        float 已过时间 = 0f;
        while (已过时间 < 最长时间)
        {
            已过时间 += Time.unscaledDeltaTime;
            for (int i = 0; i < 物体状态列表.Count; i++)
            {
                物体状态 状态 = 物体状态列表[i];
                if (状态.物体 == null || !状态.原激活状态)
                {
                    continue;
                }

                float 时间 = 状态.标记.取得动画时间;
                float 进度 = 时间 <= 0f ? 1f : Mathf.Clamp01(已过时间 / 时间);
                状态.矩形.anchoredPosition = Vector2.Lerp(状态.动画起始位置, 状态.原位置, 平滑进度(进度));
            }

            yield return null;
        }

        for (int i = 0; i < 物体状态列表.Count; i++)
        {
            物体状态 状态 = 物体状态列表[i];
            if (状态.物体 == null)
            {
                continue;
            }

            状态.矩形.anchoredPosition = 状态.原位置;
            状态.物体.SetActive(状态.原激活状态);
        }

        物体状态列表.Clear();
    }

    private static IEnumerator 播放动画包装(IEnumerator 动画)
    {
        yield return 动画;
        当前动画 = null;
    }

    private static void 立即应用隐藏状态()
    {
        for (int i = 0; i < 物体状态列表.Count; i++)
        {
            物体状态 状态 = 物体状态列表[i];
            if (状态.物体 == null)
            {
                continue;
            }

            状态.矩形.anchoredPosition = 状态.隐藏位置;
            状态.物体.SetActive(false);
        }
    }

    private static void 清理已销毁状态()
    {
        for (int i = 物体状态列表.Count - 1; i >= 0; i--)
        {
            if (物体状态列表[i].物体 == null)
            {
                物体状态列表.RemoveAt(i);
            }
        }
    }

    private static float 平滑进度(float 进度)
    {
        return 进度 * 进度 * (3f - 2f * 进度);
    }
}

using System.Collections;
using System.Collections.Generic;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class 小对话运行时 : MonoBehaviour
{
    private sealed class 播放请求
    {
        public string 内容ID = string.Empty;
        public Action 完成回调;
    }

    private const float 无配音持续秒数 = 2f;
    private const float 配音额外停留秒数 = 0.5f;
    private const float 底部距离 = 110f;
    private const float 头顶额外高度 = 0.35f;
    private const float 横向内边距 = 28f;
    private const float 纵向内边距 = 16f;
    private const float 底部最大宽度 = 1100f;
    private const float 头顶最大宽度 = 620f;

    private static 小对话运行时 实例;

    private readonly Queue<播放请求> 待播放请求 = new Queue<播放请求>();
    private RectTransform 画布矩形;
    private RectTransform 字幕底板;
    private TextMeshProUGUI 字幕文本;
    private AudioSource 配音播放器;
    private Coroutine 播放协程;
    private BattleUnit 当前跟随角色;
    private bool 当前固定到底部;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void 自动创建()
    {
        if (实例 != null)
        {
            return;
        }

        小对话运行时 已有实例 =
            FindAnyObjectByType<小对话运行时>(FindObjectsInactive.Include);
        if (已有实例 != null)
        {
            实例 = 已有实例;
            return;
        }

        GameObject 运行时物体 = new GameObject("小对话运行时");
        DontDestroyOnLoad(运行时物体);
        实例 = 运行时物体.AddComponent<小对话运行时>();
    }

    private void Awake()
    {
        if (实例 != null && 实例 != this)
        {
            Destroy(gameObject);
            return;
        }

        实例 = this;
        DontDestroyOnLoad(gameObject);
        创建字幕界面();
        创建配音播放器();
    }

    private void OnDestroy()
    {
        if (实例 == this)
        {
            实例 = null;
        }
    }

    private void LateUpdate()
    {
        if (字幕底板 == null || !字幕底板.gameObject.activeSelf || 当前固定到底部)
        {
            return;
        }

        if (当前跟随角色 == null || !尝试获取角色屏幕位置(当前跟随角色, out Vector2 屏幕位置))
        {
            当前固定到底部 = true;
            设置底部位置();
            更新字幕尺寸(底部最大宽度);
            return;
        }

        设置头顶位置(屏幕位置);
    }

    public static bool 播放内容并等待(string 内容ID, Action 完成回调)
    {
        自动创建();
        if (实例 == null || string.IsNullOrWhiteSpace(内容ID))
        {
            return false;
        }

        小对话内容数据库 数据库 = 小对话内容数据库.加载默认库();
        if (数据库 == null || 数据库.查找(内容ID) == null)
        {
            Debug.LogError($"小对话运行时：找不到小对话内容“{内容ID}”。");
            return false;
        }

        实例.待播放请求.Enqueue(new 播放请求
        {
            内容ID = 内容ID.Trim(),
            完成回调 = 完成回调
        });
        实例.确保播放队列();
        return true;
    }

    public static bool 播放对话组并等待(string 对话组ID, Action 完成回调)
    {
        自动创建();
        if (实例 == null || string.IsNullOrWhiteSpace(对话组ID))
        {
            return false;
        }

        小对话组数据库 组数据库 = 小对话组数据库.加载默认库();
        小对话组数据库.小对话组 对话组 = 组数据库 != null ? 组数据库.查找(对话组ID) : null;
        if (对话组 == null)
        {
            Debug.LogError($"小对话运行时：找不到小对话组“{对话组ID}”。");
            return false;
        }

        小对话组数据库.确保内容列表(对话组);
        List<string> 有效内容ID = new List<string>();
        for (int i = 0; i < 对话组.内容ID顺序.Count; i++)
        {
            string 内容ID = 对话组.内容ID顺序[i];
            if (!string.IsNullOrWhiteSpace(内容ID))
            {
                有效内容ID.Add(内容ID.Trim());
            }
        }

        if (有效内容ID.Count <= 0)
        {
            Debug.LogError($"小对话运行时：小对话组“{对话组ID}”没有可播放内容。");
            return false;
        }

        for (int i = 0; i < 有效内容ID.Count; i++)
        {
            实例.待播放请求.Enqueue(new 播放请求
            {
                内容ID = 有效内容ID[i],
                完成回调 = i == 有效内容ID.Count - 1 ? 完成回调 : null
            });
        }

        实例.确保播放队列();
        return true;
    }

    private void 确保播放队列()
    {
        if (播放协程 == null)
        {
            播放协程 = StartCoroutine(播放队列());
        }
    }

    private IEnumerator 播放队列()
    {
        while (待播放请求.Count > 0)
        {
            播放请求 请求 = 待播放请求.Dequeue();
            string 内容ID = 请求.内容ID;
            小对话内容数据库 数据库 = 小对话内容数据库.加载默认库();
            小对话内容数据库.小对话内容 内容 = 数据库 != null ? 数据库.查找(内容ID) : null;
            if (内容 == null)
            {
                Debug.LogError($"小对话运行时：播放时找不到小对话内容“{内容ID}”。");
                请求.完成回调?.Invoke();
                continue;
            }

            yield return 播放单条内容(内容);
            请求.完成回调?.Invoke();
        }

        隐藏字幕();
        播放协程 = null;
    }

    private IEnumerator 播放单条内容(小对话内容数据库.小对话内容 内容)
    {
        string 说话者 = 内容.说话者文本 ?? string.Empty;
        string 正文 = 内容.对话文本 ?? string.Empty;
        字幕文本.text = string.IsNullOrWhiteSpace(说话者) ? 正文 : $"{说话者}：{正文}";

        当前跟随角色 = null;
        当前固定到底部 = 内容.显示形式 == 小对话内容数据库.对话形式.屏幕底部;
        if (!当前固定到底部)
        {
            当前跟随角色 = 查找角色(内容.对话角色ID);
            if (当前跟随角色 == null || !尝试获取角色屏幕位置(当前跟随角色, out Vector2 屏幕位置))
            {
                当前固定到底部 = true;
            }
            else
            {
                设置头顶位置(屏幕位置);
            }
        }

        if (当前固定到底部)
        {
            设置底部位置();
        }

        更新字幕尺寸(当前固定到底部 ? 底部最大宽度 : 头顶最大宽度);
        字幕底板.gameObject.SetActive(true);

        float 持续时间 = 无配音持续秒数;
        if (内容.配音 != null)
        {
            配音播放器.clip = 内容.配音;
            配音播放器.Play();
            持续时间 = 内容.配音.length + 配音额外停留秒数;
        }

        float 已播放时间 = 0f;
        while (已播放时间 < 持续时间)
        {
            已播放时间 += Time.unscaledDeltaTime;
            yield return null;
        }

        if (配音播放器.isPlaying)
        {
            配音播放器.Stop();
        }
        配音播放器.clip = null;
        隐藏字幕();
    }

    private void 创建字幕界面()
    {
        GameObject 画布物体 = new GameObject(
            "小对话画布",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        画布物体.transform.SetParent(transform, false);

        Canvas 画布 = 画布物体.GetComponent<Canvas>();
        画布.renderMode = RenderMode.ScreenSpaceOverlay;
        画布.sortingOrder = 30000;

        CanvasScaler 缩放器 = 画布物体.GetComponent<CanvasScaler>();
        缩放器.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        缩放器.referenceResolution = new Vector2(1920f, 1080f);
        缩放器.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        缩放器.matchWidthOrHeight = 0.5f;

        GraphicRaycaster 射线检测 = 画布物体.GetComponent<GraphicRaycaster>();
        射线检测.enabled = false;
        画布矩形 = 画布物体.GetComponent<RectTransform>();

        GameObject 底板物体 = new GameObject(
            "字幕底板",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        底板物体.transform.SetParent(画布矩形, false);
        字幕底板 = 底板物体.GetComponent<RectTransform>();
        字幕底板.anchorMin = new Vector2(0.5f, 0.5f);
        字幕底板.anchorMax = new Vector2(0.5f, 0.5f);
        字幕底板.pivot = new Vector2(0.5f, 0.5f);

        Image 底板图像 = 底板物体.GetComponent<Image>();
        底板图像.color = new Color(0f, 0f, 0f, 0.62f);
        底板图像.raycastTarget = false;

        GameObject 文本物体 = new GameObject(
            "字幕文本",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        文本物体.transform.SetParent(字幕底板, false);
        RectTransform 文本矩形 = 文本物体.GetComponent<RectTransform>();
        文本矩形.anchorMin = Vector2.zero;
        文本矩形.anchorMax = Vector2.one;
        文本矩形.offsetMin = new Vector2(横向内边距, 纵向内边距);
        文本矩形.offsetMax = new Vector2(-横向内边距, -纵向内边距);

        字幕文本 = 文本物体.GetComponent<TextMeshProUGUI>();
        小对话显示配置 配置 = 小对话显示配置.加载默认配置();
        if (配置 != null && 配置.普通字体 != null)
        {
            字幕文本.font = 配置.普通字体;
        }
        字幕文本.fontSize = 34f;
        字幕文本.color = Color.white;
        字幕文本.alignment = TextAlignmentOptions.Center;
        字幕文本.textWrappingMode = TextWrappingModes.Normal;
        字幕文本.raycastTarget = false;
        字幕底板.gameObject.SetActive(false);
    }

    private void 创建配音播放器()
    {
        配音播放器 = gameObject.AddComponent<AudioSource>();
        配音播放器.playOnAwake = false;
        配音播放器.loop = false;
        配音播放器.priority = 0;
        配音播放器.spatialBlend = 0f;
        AudioRouting.ApplyVoice(配音播放器);
    }

    private void 更新字幕尺寸(float 最大宽度)
    {
        字幕文本.ForceMeshUpdate(true);
        Vector2 首选尺寸 = 字幕文本.GetPreferredValues(字幕文本.text, 最大宽度, 0f);
        float 内容宽度 = Mathf.Min(最大宽度, Mathf.Max(1f, 首选尺寸.x));
        float 内容高度 = Mathf.Max(字幕文本.fontSize, 首选尺寸.y);
        字幕底板.sizeDelta = new Vector2(
            内容宽度 + 横向内边距 * 2f,
            内容高度 + 纵向内边距 * 2f);
    }

    private void 设置底部位置()
    {
        字幕底板.anchorMin = new Vector2(0.5f, 0f);
        字幕底板.anchorMax = new Vector2(0.5f, 0f);
        字幕底板.anchoredPosition = new Vector2(0f, 底部距离);
    }

    private void 设置头顶位置(Vector2 屏幕位置)
    {
        字幕底板.anchorMin = new Vector2(0.5f, 0.5f);
        字幕底板.anchorMax = new Vector2(0.5f, 0.5f);
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                画布矩形,
                屏幕位置,
                null,
                out Vector2 本地位置))
        {
            字幕底板.anchoredPosition = 本地位置;
        }
    }

    private static BattleUnit 查找角色(string 角色ID)
    {
        if (string.IsNullOrWhiteSpace(角色ID))
        {
            return null;
        }

        BattleUnit[] 角色列表 =
            FindObjectsByType<BattleUnit>(FindObjectsInactive.Include);
        for (int i = 0; i < 角色列表.Length; i++)
        {
            BattleUnit 角色 = 角色列表[i];
            if (角色 != null &&
                角色.gameObject.activeInHierarchy &&
                string.Equals(角色.characterId, 角色ID.Trim(), System.StringComparison.Ordinal))
            {
                return 角色;
            }
        }

        return null;
    }

    private static bool 尝试获取角色屏幕位置(BattleUnit 角色, out Vector2 屏幕位置)
    {
        屏幕位置 = Vector2.zero;
        Camera 相机 = Camera.main;
        if (角色 == null || 相机 == null || !角色.gameObject.activeInHierarchy)
        {
            return false;
        }

        Vector3 世界位置 = 角色.transform.position + Vector3.up * (2f + 头顶额外高度);
        Renderer[] 渲染器 = 角色.GetComponentsInChildren<Renderer>(false);
        if (渲染器.Length > 0)
        {
            Bounds 合并边界 = 渲染器[0].bounds;
            for (int i = 1; i < 渲染器.Length; i++)
            {
                合并边界.Encapsulate(渲染器[i].bounds);
            }
            世界位置 = new Vector3(合并边界.center.x, 合并边界.max.y + 头顶额外高度, 合并边界.center.z);
        }

        Vector3 视口位置 = 相机.WorldToViewportPoint(世界位置);
        if (视口位置.z <= 0f ||
            视口位置.x < 0f || 视口位置.x > 1f ||
            视口位置.y < 0f || 视口位置.y > 1f)
        {
            return false;
        }

        屏幕位置 = 相机.WorldToScreenPoint(世界位置);
        return true;
    }

    private void 隐藏字幕()
    {
        if (字幕底板 != null)
        {
            字幕底板.gameObject.SetActive(false);
        }
        当前跟随角色 = null;
        当前固定到底部 = false;
    }
}

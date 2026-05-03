using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class 缩放开启动画 : MonoBehaviour
{
    public enum 缩放轴心位置
    {
        左下角,
        左,
        左上角,
        中上,
        右上角,
        右,
        右下角,
        中下,
        中
    }

    [Header("目标")]
    [SerializeField] private RectTransform 动画目标;
    [FormerlySerializedAs("自动改为左下轴心")]
    [SerializeField] private bool 自动改为指定轴心 = true;
    [SerializeField] private 缩放轴心位置 轴心位置 = 缩放轴心位置.左下角;

    [Header("Toggle")]
    [SerializeField] private Toggle 控制Toggle;
    [SerializeField] private bool 启用时监听Toggle = true;
    [SerializeField] private bool 启用时应用Toggle当前状态;

    [Header("时间")]
    [SerializeField] private float 打开时长 = 0.18f;
    [SerializeField] private float 关闭时长 = 0.14f;
    [SerializeField] private bool 使用未缩放时间 = true;

    [Header("状态")]
    [SerializeField] private bool 启用时播放开启动画 = true;
    [SerializeField] private bool 关闭完成后禁用物体 = true;

    [Header("曲线")]
    [SerializeField] private AnimationCurve 打开曲线 = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.75f, 1.08f),
        new Keyframe(1f, 1f));
    [SerializeField] private AnimationCurve 关闭曲线 = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    private readonly Vector3 隐藏缩放 = new Vector3(0.001f, 0.001f, 0.001f);
    private Vector3 原始缩放;
    private bool 已缓存原始缩放;
    private bool 已经过首次启用;
    private bool 已绑定Toggle监听;
    private Coroutine 动画协程;

    private RectTransform 当前目标
    {
        get
        {
            if (动画目标 == null)
            {
                动画目标 = transform as RectTransform;
            }

            return 动画目标;
        }
    }

    private GameObject 当前目标物体
    {
        get
        {
            RectTransform target = 当前目标;
            return target != null ? target.gameObject : gameObject;
        }
    }

    private void Reset()
    {
        动画目标 = transform as RectTransform;
    }

    private void Awake()
    {
        缓存原始缩放();
        准备指定轴心();
    }

    private void OnEnable()
    {
        绑定Toggle监听();
        缓存原始缩放();
        准备指定轴心();

        if (启用时应用Toggle当前状态 && 控制Toggle != null)
        {
            设置显示(控制Toggle.isOn);
            已经过首次启用 = true;
            return;
        }

        if (!启用时播放开启动画)
        {
            设置为打开状态();
            已经过首次启用 = true;
            return;
        }

        播放打开动画(!已经过首次启用);
        已经过首次启用 = true;
    }

    private void OnDisable()
    {
        解绑Toggle监听();
        if (动画协程 != null)
        {
            StopCoroutine(动画协程);
            动画协程 = null;
        }
    }

    public void 设置显示(bool 显示)
    {
        if (显示)
        {
            打开内容();
        }
        else
        {
            关闭内容();
        }
    }

    public void 应用Toggle状态(bool 是否开启)
    {
        设置显示(是否开启);
    }

    public void 打开内容()
    {
        缓存原始缩放();
        准备指定轴心();

        GameObject targetObject = 当前目标物体;
        bool targetIsSelf = targetObject == gameObject;
        bool wasInactive = !targetObject.activeSelf;
        if (wasInactive)
        {
            targetObject.SetActive(true);
            if (targetIsSelf)
            {
                return;
            }
        }

        if (!targetObject.activeInHierarchy)
        {
            return;
        }

        播放打开动画(wasInactive || !已经过首次启用);
        已经过首次启用 = true;
    }

    public void 关闭内容()
    {
        GameObject targetObject = 当前目标物体;
        if (!targetObject.activeInHierarchy)
        {
            return;
        }

        缓存原始缩放();
        准备指定轴心();
        播放关闭动画();
    }

    public void 切换显示()
    {
        设置显示(!当前目标物体.activeSelf);
    }

    private void 播放打开动画(bool 从隐藏状态开始)
    {
        RectTransform target = 当前目标;
        if (target == null)
        {
            return;
        }

        Vector3 起始缩放 = 从隐藏状态开始 ? 隐藏缩放 : target.localScale;
        启动动画(起始缩放, 原始缩放, Mathf.Max(0f, 打开时长), 打开曲线, false);
    }

    private void 播放关闭动画()
    {
        RectTransform target = 当前目标;
        if (target == null)
        {
            if (关闭完成后禁用物体)
            {
                当前目标物体.SetActive(false);
            }

            return;
        }

        启动动画(target.localScale, 隐藏缩放, Mathf.Max(0f, 关闭时长), 关闭曲线, 关闭完成后禁用物体, true);
    }

    private void 启动动画(Vector3 起始缩放, Vector3 目标缩放, float 时长, AnimationCurve 曲线, bool 完成后禁用, bool 反向解释曲线 = false)
    {
        if (动画协程 != null)
        {
            StopCoroutine(动画协程);
            动画协程 = null;
        }

        if (时长 <= 0f)
        {
            RectTransform target = 当前目标;
            if (target != null)
            {
                target.localScale = 目标缩放;
            }

            if (完成后禁用)
            {
                当前目标物体.SetActive(false);
            }

            return;
        }

        动画协程 = StartCoroutine(播放缩放流程(起始缩放, 目标缩放, 时长, 曲线, 完成后禁用, 反向解释曲线));
    }

    private IEnumerator 播放缩放流程(Vector3 起始缩放, Vector3 目标缩放, float 时长, AnimationCurve 曲线, bool 完成后禁用, bool 反向解释曲线)
    {
        RectTransform target = 当前目标;
        float elapsed = 0f;

        while (elapsed < 时长 && target != null)
        {
            float progress = Mathf.Clamp01(elapsed / 时长);
            float evaluated = 曲线 != null ? 曲线.Evaluate(progress) : progress;
            if (反向解释曲线)
            {
                evaluated = 1f - evaluated;
            }

            target.localScale = Vector3.LerpUnclamped(起始缩放, 目标缩放, evaluated);
            elapsed += 使用未缩放时间 ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }

        target = 当前目标;
        if (target != null)
        {
            target.localScale = 目标缩放;
        }

        动画协程 = null;

        if (完成后禁用)
        {
            当前目标物体.SetActive(false);
        }
    }

    private void 绑定Toggle监听()
    {
        if (!启用时监听Toggle || 控制Toggle == null || 已绑定Toggle监听)
        {
            return;
        }

        控制Toggle.onValueChanged.AddListener(应用Toggle状态);
        已绑定Toggle监听 = true;
    }

    private void 解绑Toggle监听()
    {
        if (控制Toggle == null || !已绑定Toggle监听)
        {
            return;
        }

        控制Toggle.onValueChanged.RemoveListener(应用Toggle状态);
        已绑定Toggle监听 = false;
    }

    private void 设置为打开状态()
    {
        RectTransform target = 当前目标;
        if (target != null)
        {
            target.localScale = 原始缩放;
        }
    }

    private void 缓存原始缩放()
    {
        RectTransform target = 当前目标;
        if (target == null || 已缓存原始缩放)
        {
            return;
        }

        原始缩放 = target.localScale;
        if (Mathf.Approximately(原始缩放.x, 0f) && Mathf.Approximately(原始缩放.y, 0f))
        {
            原始缩放 = Vector3.one;
        }

        已缓存原始缩放 = true;
    }

    private void 准备指定轴心()
    {
        RectTransform target = 当前目标;
        if (target == null || !自动改为指定轴心)
        {
            return;
        }

        设置轴心并保持位置(target, 获取轴心坐标(轴心位置));
    }

    private static Vector2 获取轴心坐标(缩放轴心位置 position)
    {
        switch (position)
        {
            case 缩放轴心位置.左下角:
                return new Vector2(0f, 0f);
            case 缩放轴心位置.左:
                return new Vector2(0f, 0.5f);
            case 缩放轴心位置.左上角:
                return new Vector2(0f, 1f);
            case 缩放轴心位置.中上:
                return new Vector2(0.5f, 1f);
            case 缩放轴心位置.右上角:
                return new Vector2(1f, 1f);
            case 缩放轴心位置.右:
                return new Vector2(1f, 0.5f);
            case 缩放轴心位置.右下角:
                return new Vector2(1f, 0f);
            case 缩放轴心位置.中下:
                return new Vector2(0.5f, 0f);
            case 缩放轴心位置.中:
                return new Vector2(0.5f, 0.5f);
            default:
                return Vector2.zero;
        }
    }

    private static void 设置轴心并保持位置(RectTransform target, Vector2 pivot)
    {
        if (target.pivot == pivot)
        {
            return;
        }

        Vector2 pivotDelta = target.pivot - pivot;
        Vector3 positionDelta = new Vector3(
            pivotDelta.x * target.rect.width * target.localScale.x,
            pivotDelta.y * target.rect.height * target.localScale.y,
            0f);

        target.pivot = pivot;
        target.localPosition -= positionDelta;
    }
}

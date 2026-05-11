using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[AddComponentMenu("战斗/墨血换房转场控制器")]
public sealed class 墨血换房转场控制器 : MonoBehaviour
{
    private const string DebugPrefix = "[换房转场Debug]";

    private static readonly int ProgressId = Shader.PropertyToID("_Progress");
    private static readonly int CurtainColorId = Shader.PropertyToID("_CurtainColor");

    private static 墨血换房转场控制器 activeController;
    private static bool 等待新场景揭开;
    private static bool 正在换房转场;

    [Header("引用")]
    [SerializeField, InspectorName("全屏遮罩图片")] private Image transitionImage;
    [SerializeField, InspectorName("转场材质")] private Material transitionMaterial;

    [Header("颜色")]
    [SerializeField, InspectorName("幕布颜色")] private Color curtainColor = Color.black;

    [Header("时间")]
    [SerializeField, InspectorName("盖屏时间")] private float coverDuration = 0.55f;
    [SerializeField, InspectorName("揭开时间")] private float revealDuration = 0.42f;

    private Material runtimeMaterial;
    private Coroutine transitionRoutine;
#if UNITY_EDITOR
    private bool editorPreviewEnabled;
    private float editorPreviewProgress;
#endif

    private void Awake()
    {
        Debug.Log($"{DebugPrefix} Awake。对象={name}，等待新场景揭开={等待新场景揭开}，正在换房转场={正在换房转场}，Image={DescribeImage()}，材质={DescribeMaterial()}。", this);

        activeController = this;
        EnsureRuntimeMaterial();
        ApplyMaterialSettings();

        if (等待新场景揭开)
        {
            Debug.Log($"{DebugPrefix} Awake 检测到等待新场景揭开，先把幕布设为全覆盖。对象={name}。", this);
            SetProgress(1f);
            SetImageVisible(true);
        }
        else
        {
            Debug.Log($"{DebugPrefix} Awake 没有等待揭开，关闭幕布。对象={name}。", this);
            SetProgress(0f);
            SetImageVisible(false);
        }
    }

    private void Start()
    {
        Debug.Log($"{DebugPrefix} Start。对象={name}，等待新场景揭开={等待新场景揭开}，正在换房转场={正在换房转场}。", this);

        if (!等待新场景揭开)
        {
            return;
        }

        等待新场景揭开 = false;
        Debug.Log($"{DebugPrefix} 新场景开始掀开幕布。对象={name}，揭开时间={revealDuration}。", this);
        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
        }

        transitionRoutine = StartCoroutine(PlayRevealOnly());
    }

    private void OnEnable()
    {
        Debug.Log($"{DebugPrefix} OnEnable。对象={name}。", this);
        activeController = this;
    }

    private void OnDisable()
    {
        if (activeController == this)
        {
            activeController = null;
        }
    }

    private void OnValidate()
    {
        coverDuration = Mathf.Max(0.01f, coverDuration);
        revealDuration = Mathf.Max(0.01f, revealDuration);
#if UNITY_EDITOR
        editorPreviewProgress = Mathf.Clamp01(editorPreviewProgress);

        if (!Application.isPlaying)
        {
            ApplyEditorPreview();
        }
#endif
    }

    public static bool 尝试播放换房转场(System.Action 盖满后执行)
    {
        Debug.Log($"{DebugPrefix} 收到播放请求。activeController={(activeController != null ? activeController.name : "空")}，正在换房转场={正在换房转场}。", activeController);

        if (正在换房转场)
        {
            Debug.Log($"{DebugPrefix} 播放请求被忽略：当前已经在换房转场中。", activeController);
            return true;
        }

        if (activeController == null || !activeController.CanPlay())
        {
            Debug.LogWarning($"{DebugPrefix} 无法播放转场，直接执行切房。activeController={(activeController != null ? activeController.name : "空")}。", activeController);
            盖满后执行?.Invoke();
            return false;
        }

        正在换房转场 = true;
        Debug.Log($"{DebugPrefix} 开始播放盖屏动画。控制器={activeController.name}。", activeController);
        activeController.PlayRoomTransition(盖满后执行);
        return true;
    }

    private bool CanPlay()
    {
        Material material = ResolveMaterial();
        bool canPlay = isActiveAndEnabled &&
            transitionImage != null &&
            material != null &&
            material.HasProperty(ProgressId);
        Debug.Log($"{DebugPrefix} CanPlay={canPlay}。对象={name}，isActiveAndEnabled={isActiveAndEnabled}，Image={DescribeImage()}，材质={DescribeMaterial()}，材质有_Progress={(material != null && material.HasProperty(ProgressId))}。", this);
        return canPlay;
    }

    private void PlayRoomTransition(System.Action coveredAction)
    {
        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
        }

        transitionRoutine = StartCoroutine(PlayCoverThenLoad(coveredAction));
    }

    private IEnumerator PlayCoverThenLoad(System.Action coveredAction)
    {
        Debug.Log($"{DebugPrefix} 盖屏协程开始。对象={name}，盖屏时间={coverDuration}。", this);
        EnsureRuntimeMaterial();
        ApplyMaterialSettings();
        SetImageVisible(true);
        yield return AnimateProgress(0f, 1f, coverDuration);

        Debug.Log($"{DebugPrefix} 盖屏完成，准备执行切房回调。对象={name}。", this);
        等待新场景揭开 = true;
        coveredAction?.Invoke();

        Debug.Log($"{DebugPrefix} 切房回调已执行，旧控制器停止后续转场，等待新场景控制器接力掀开。对象={name}。", this);
        yield break;
    }

    private IEnumerator PlayRevealOnly()
    {
        Debug.Log($"{DebugPrefix} 掀开协程开始。对象={name}，揭开时间={revealDuration}。", this);
        EnsureRuntimeMaterial();
        ApplyMaterialSettings();
        SetImageVisible(true);
        yield return AnimateProgress(1f, 0f, revealDuration);

        SetImageVisible(false);

        正在换房转场 = false;
        transitionRoutine = null;
        Debug.Log($"{DebugPrefix} 掀开完成，转场结束。对象={name}。", this);
    }

    private IEnumerator AnimateProgress(float from, float to, float duration)
    {
        float elapsed = 0f;
        Debug.Log($"{DebugPrefix} 进度动画开始：{from} -> {to}，duration={duration}，对象={name}。", this);
        SetProgress(from);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);
            SetProgress(Mathf.Lerp(from, to, t));
            yield return null;
        }

        SetProgress(to);
        Debug.Log($"{DebugPrefix} 进度动画结束：最终进度={to}，实际耗时={elapsed}，对象={name}。", this);
    }

    private void EnsureRuntimeMaterial()
    {
        if (transitionImage == null)
        {
            Debug.LogWarning($"{DebugPrefix} EnsureRuntimeMaterial 失败：transitionImage 为空。对象={name}。", this);
            return;
        }

        Material sourceMaterial = ResolveMaterial();
        if (sourceMaterial == null)
        {
            Debug.LogWarning($"{DebugPrefix} EnsureRuntimeMaterial 失败：源材质为空。对象={name}，Image={DescribeImage()}。", this);
            return;
        }

        if (runtimeMaterial == null || runtimeMaterial.shader != sourceMaterial.shader)
        {
            runtimeMaterial = new Material(sourceMaterial)
            {
                name = sourceMaterial.name + "（运行时）"
            };
        }

        if (transitionImage.material != runtimeMaterial)
        {
            transitionImage.material = runtimeMaterial;
            Debug.Log($"{DebugPrefix} 已把运行时材质挂到 Image。对象={name}，运行时材质={runtimeMaterial.name}。", this);
        }
    }

    private Material ResolveMaterial()
    {
        if (transitionMaterial != null)
        {
            return transitionMaterial;
        }

        return transitionImage != null ? transitionImage.material : null;
    }

    private void ApplyMaterialSettings()
    {
        Material material = runtimeMaterial != null ? runtimeMaterial : ResolveMaterial();
        if (material == null)
        {
            return;
        }

        material.SetColor(CurtainColorId, curtainColor);
    }

    private void SetProgress(float progress)
    {
        Material material = runtimeMaterial != null ? runtimeMaterial : ResolveMaterial();
        if (material != null)
        {
            material.SetFloat(ProgressId, Mathf.Clamp01(progress));
        }
        else
        {
            Debug.LogWarning($"{DebugPrefix} SetProgress 失败：材质为空。progress={progress}，对象={name}。", this);
        }
    }

    private void SetImageVisible(bool visible)
    {
        if (transitionImage != null)
        {
            transitionImage.enabled = visible;
            Debug.Log($"{DebugPrefix} 设置幕布 Image 显示={visible}。对象={name}，Image对象={transitionImage.gameObject.name}，ImageActive={transitionImage.gameObject.activeInHierarchy}。", this);
        }
        else
        {
            Debug.LogWarning($"{DebugPrefix} SetImageVisible 失败：transitionImage 为空。对象={name}。", this);
        }
    }

    private string DescribeImage()
    {
        if (transitionImage == null)
        {
            return "空";
        }

        return $"{transitionImage.gameObject.name}/enabled={transitionImage.enabled}/active={transitionImage.gameObject.activeInHierarchy}/material={(transitionImage.material != null ? transitionImage.material.name : "空")}";
    }

    private string DescribeMaterial()
    {
        Material material = runtimeMaterial != null ? runtimeMaterial : ResolveMaterial();
        if (material == null)
        {
            return "空";
        }

        return $"{material.name}/shader={(material.shader != null ? material.shader.name : "空")}";
    }

#if UNITY_EDITOR
    public void 编辑器设置预览进度(float progress)
    {
        editorPreviewProgress = Mathf.Clamp01(progress);
        editorPreviewEnabled = true;
        ApplyEditorPreview();
    }

    public void 编辑器关闭预览()
    {
        editorPreviewEnabled = false;
        SetProgress(0f);
        SetImageVisible(false);
    }

    private void ApplyEditorPreview()
    {
        EnsureRuntimeMaterial();
        ApplyMaterialSettings();
        SetProgress(editorPreviewEnabled ? editorPreviewProgress : 0f);
        SetImageVisible(editorPreviewEnabled);
    }
#endif
}

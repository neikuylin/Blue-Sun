using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[AddComponentMenu("战斗/墨血换房转场控制器")]
public sealed class 墨血换房转场控制器 : MonoBehaviour
{
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
        activeController = this;
        EnsureRuntimeMaterial();
        ApplyMaterialSettings();

        if (等待新场景揭开)
        {
            SetProgress(1f);
            SetImageVisible(true);
        }
        else
        {
            SetProgress(0f);
            SetImageVisible(false);
        }
    }

    private void Start()
    {
        if (!等待新场景揭开)
        {
            return;
        }

        等待新场景揭开 = false;
        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
        }

        transitionRoutine = StartCoroutine(PlayRevealOnly());
    }

    private void OnEnable()
    {
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
        if (正在换房转场)
        {
            return true;
        }

        if (activeController == null || !activeController.CanPlay())
        {
            盖满后执行?.Invoke();
            return false;
        }

        正在换房转场 = true;
        activeController.PlayRoomTransition(盖满后执行);
        return true;
    }

    private bool CanPlay()
    {
        Material material = ResolveMaterial();
        return isActiveAndEnabled &&
            transitionImage != null &&
            material != null &&
            material.HasProperty(ProgressId);
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
        EnsureRuntimeMaterial();
        ApplyMaterialSettings();
        SetImageVisible(true);
        yield return AnimateProgress(0f, 1f, coverDuration);

        等待新场景揭开 = true;
        coveredAction?.Invoke();

        if (!isActiveAndEnabled)
        {
            yield break;
        }

        等待新场景揭开 = false;
        yield return PlayRevealOnly();
    }

    private IEnumerator PlayRevealOnly()
    {
        EnsureRuntimeMaterial();
        ApplyMaterialSettings();
        SetImageVisible(true);
        yield return AnimateProgress(1f, 0f, revealDuration);

        SetImageVisible(false);

        正在换房转场 = false;
        transitionRoutine = null;
    }

    private IEnumerator AnimateProgress(float from, float to, float duration)
    {
        float elapsed = 0f;
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
    }

    private void EnsureRuntimeMaterial()
    {
        if (transitionImage == null)
        {
            return;
        }

        Material sourceMaterial = ResolveMaterial();
        if (sourceMaterial == null)
        {
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
    }

    private void SetImageVisible(bool visible)
    {
        if (transitionImage != null)
        {
            transitionImage.enabled = visible;
        }
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

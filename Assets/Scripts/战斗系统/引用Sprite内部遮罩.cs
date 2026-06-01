using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[AddComponentMenu("渲染/引用Sprite内部遮罩")]
public sealed class 引用Sprite内部遮罩 : MonoBehaviour
{
    private const string MaskObjectName = "自动生成_引用Sprite遮罩";

    private enum 遮罩显示区域
    {
        内部 = 0,
        外部 = 1
    }

    private enum 引用遮罩渲染层级
    {
        不决定 = 0,
        高于3D = 1,
        低于3D = 2
    }

    [SerializeField, InspectorName("遮罩来源物体")]
    private GameObject 遮罩来源物体;

    [SerializeField, InspectorName("来源物体包含子物体Sprite")]
    private bool 来源物体包含子物体Sprite = true;

    [SerializeField, InspectorName("目标SpriteRenderer")]
    private SpriteRenderer[] 目标SpriteRenderers;

    [SerializeField, InspectorName("目标为空时包含子物体")]
    private bool 目标为空时包含子物体 = true;

    [SerializeField, InspectorName("包含未激活子物体")]
    private bool 包含未激活子物体 = true;

    [SerializeField, InspectorName("同步来源位置旋转缩放")]
    private bool 同步来源位置旋转缩放 = true;

    [SerializeField, InspectorName("只使用引用遮罩")]
    private bool 只使用引用遮罩 = true;

    [SerializeField, InspectorName("遮罩显示区域")]
    private 遮罩显示区域 显示区域 = 遮罩显示区域.内部;

    [SerializeField, InspectorName("渲染层级")]
    private 引用遮罩渲染层级 渲染层级 = 引用遮罩渲染层级.不决定;

    [SerializeField, InspectorName("引用遮罩材质")]
    private Material 引用遮罩材质;

    [SerializeField, Range(0f, 1f), InspectorName("遮罩透明判定")]
    private float 遮罩透明判定 = 0.5f;

    [SerializeField, InspectorName("排序范围向前扩展")]
    private int 排序范围向前扩展 = 1;

    [SerializeField, InspectorName("排序范围向后扩展")]
    private int 排序范围向后扩展 = 1;

    private static readonly int ReferenceMaskTexId = Shader.PropertyToID("_ReferenceMaskTex");
    private static readonly int ReferenceMaskWorldToLocalId = Shader.PropertyToID("_ReferenceMaskWorldToLocal");
    private static readonly int ReferenceMaskBoundsId = Shader.PropertyToID("_ReferenceMaskBounds");
    private static readonly int ReferenceMaskUvRectId = Shader.PropertyToID("_ReferenceMaskUvRect");
    private static readonly int ReferenceMaskAlphaCutoffId = Shader.PropertyToID("_ReferenceMaskAlphaCutoff");
    private static readonly int ReferenceMaskFlipId = Shader.PropertyToID("_ReferenceMaskFlip");
    private static readonly int ReferenceMaskInvertId = Shader.PropertyToID("_ReferenceMaskInvert");

    private SpriteMask runtimeMask;
    private MaterialPropertyBlock materialPropertyBlock;

    private void OnEnable()
    {
        Apply();
    }

    private void OnValidate()
    {
        Apply();
    }

    private void LateUpdate()
    {
        Apply();
    }

    [ContextMenu("应用引用Sprite内部遮罩")]
    public void Apply()
    {
        SpriteRenderer sourceRenderer = ResolveSourceRenderer();
        if (sourceRenderer == null || sourceRenderer.sprite == null)
        {
            SetMaskActive(false);
            return;
        }

        SpriteRenderer[] targets = ResolveTargetRenderers();
        if (targets.Length == 0)
        {
            SetMaskActive(false);
            return;
        }

        if (只使用引用遮罩)
        {
            SetMaskActive(false);
            ApplyReferenceMaskMaterial(sourceRenderer, targets);
            return;
        }

        SpriteMask mask = EnsureMask();
        mask.sprite = sourceRenderer.sprite;
        mask.alphaCutoff = 遮罩透明判定;
        mask.isCustomRangeActive = true;

        ApplyTargetMaskInteraction(targets);
        ApplyMaskSortingRange(mask, targets);
        if (同步来源位置旋转缩放)
        {
            SyncMaskTransform(sourceRenderer.transform);
        }

        SetMaskActive(true);
    }

    private SpriteRenderer ResolveSourceRenderer()
    {
        if (遮罩来源物体 == null)
        {
            return null;
        }

        SpriteRenderer direct = 遮罩来源物体.GetComponent<SpriteRenderer>();
        if (direct != null)
        {
            return direct;
        }

        return 来源物体包含子物体Sprite
            ? 遮罩来源物体.GetComponentInChildren<SpriteRenderer>(包含未激活子物体)
            : null;
    }

    private SpriteRenderer[] ResolveTargetRenderers()
    {
        if (目标SpriteRenderers != null && 目标SpriteRenderers.Length > 0)
        {
            List<SpriteRenderer> validTargets = new List<SpriteRenderer>(目标SpriteRenderers.Length);
            for (int i = 0; i < 目标SpriteRenderers.Length; i++)
            {
                SpriteRenderer target = 目标SpriteRenderers[i];
                if (target != null)
                {
                    validTargets.Add(target);
                }
            }

            return validTargets.ToArray();
        }

        return 目标为空时包含子物体
            ? GetComponentsInChildren<SpriteRenderer>(包含未激活子物体)
            : GetComponents<SpriteRenderer>();
    }

    private SpriteMask EnsureMask()
    {
        if (runtimeMask != null)
        {
            return runtimeMask;
        }

        Transform maskTransform = transform.Find(MaskObjectName);
        if (maskTransform == null)
        {
            GameObject maskObject = new GameObject(MaskObjectName);
            maskObject.transform.SetParent(transform, false);
            maskTransform = maskObject.transform;
        }

        runtimeMask = maskTransform.GetComponent<SpriteMask>();
        if (runtimeMask == null)
        {
            runtimeMask = maskTransform.gameObject.AddComponent<SpriteMask>();
        }

        return runtimeMask;
    }

    private static void ApplyTargetMaskInteraction(SpriteRenderer[] targets)
    {
        for (int i = 0; i < targets.Length; i++)
        {
            SpriteRenderer target = targets[i];
            if (target != null)
            {
                target.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            }
        }
    }

    private void ApplyReferenceMaskMaterial(SpriteRenderer sourceRenderer, SpriteRenderer[] targets)
    {
        Material material = ResolveReferenceMaskMaterial();
        if (material == null)
        {
            Debug.LogWarning("引用Sprite内部遮罩：找不到材质或Shader“项目/渲染/引用Sprite内部遮罩”，无法隔离其它SpriteMask。", this);
            return;
        }

        Sprite sourceSprite = sourceRenderer.sprite;
        Rect sourceRect = sourceSprite.rect;
        Texture2D sourceTexture = sourceSprite.texture;
        Bounds sourceBounds = sourceSprite.bounds;
        Vector4 uvRect = new Vector4(
            sourceRect.x / sourceTexture.width,
            sourceRect.y / sourceTexture.height,
            sourceRect.width / sourceTexture.width,
            sourceRect.height / sourceTexture.height);
        Vector4 bounds = new Vector4(
            sourceBounds.min.x,
            sourceBounds.min.y,
            sourceBounds.size.x,
            sourceBounds.size.y);
        Vector4 flip = new Vector4(sourceRenderer.flipX ? 1f : 0f, sourceRenderer.flipY ? 1f : 0f, 0f, 0f);

        if (materialPropertyBlock == null)
        {
            materialPropertyBlock = new MaterialPropertyBlock();
        }

        for (int i = 0; i < targets.Length; i++)
        {
            SpriteRenderer target = targets[i];
            if (target == null)
            {
                continue;
            }

            target.maskInteraction = SpriteMaskInteraction.None;
            if (target.sharedMaterial != material)
            {
                target.sharedMaterial = material;
            }

            target.GetPropertyBlock(materialPropertyBlock);
            materialPropertyBlock.SetTexture(ReferenceMaskTexId, sourceTexture);
            materialPropertyBlock.SetMatrix(ReferenceMaskWorldToLocalId, sourceRenderer.transform.worldToLocalMatrix);
            materialPropertyBlock.SetVector(ReferenceMaskBoundsId, bounds);
            materialPropertyBlock.SetVector(ReferenceMaskUvRectId, uvRect);
            materialPropertyBlock.SetFloat(ReferenceMaskAlphaCutoffId, 遮罩透明判定);
            materialPropertyBlock.SetVector(ReferenceMaskFlipId, flip);
            materialPropertyBlock.SetFloat(ReferenceMaskInvertId, 显示区域 == 遮罩显示区域.外部 ? 1f : 0f);
            target.SetPropertyBlock(materialPropertyBlock);
        }
    }

    private Material ResolveReferenceMaskMaterial()
    {
        if (引用遮罩材质 != null)
        {
            return 引用遮罩材质;
        }

        Material layerMaterial = Resources.Load<Material>(GetReferenceMaskMaterialResourceName());
        if (layerMaterial != null)
        {
            return layerMaterial;
        }

        Shader shader = Shader.Find("项目/渲染/受光不写深度引用Sprite遮罩");
        return shader != null ? new Material(shader) { name = "引用Sprite内部遮罩_运行时材质" } : null;
    }

    private string GetReferenceMaskMaterialResourceName()
    {
        switch (渲染层级)
        {
            case 引用遮罩渲染层级.高于3D:
                return "引用Sprite遮罩_高于3D受光不写深度Sprite材质";
            case 引用遮罩渲染层级.低于3D:
                return "引用Sprite遮罩_低于3D受光不写深度Sprite材质";
            default:
                return "引用Sprite遮罩_不决定受光不写深度Sprite材质";
        }
    }

    private void ApplyMaskSortingRange(SpriteMask mask, SpriteRenderer[] targets)
    {
        int minOrder = int.MaxValue;
        int maxOrder = int.MinValue;
        int sortingLayerId = 0;

        for (int i = 0; i < targets.Length; i++)
        {
            SpriteRenderer target = targets[i];
            if (target == null)
            {
                continue;
            }

            minOrder = Mathf.Min(minOrder, target.sortingOrder);
            maxOrder = Mathf.Max(maxOrder, target.sortingOrder);
            sortingLayerId = target.sortingLayerID;
        }

        if (minOrder == int.MaxValue)
        {
            return;
        }

        mask.frontSortingLayerID = sortingLayerId;
        mask.backSortingLayerID = sortingLayerId;
        mask.frontSortingOrder = maxOrder + Mathf.Max(0, 排序范围向前扩展);
        mask.backSortingOrder = minOrder - Mathf.Max(0, 排序范围向后扩展);
    }

    private void SyncMaskTransform(Transform sourceTransform)
    {
        if (runtimeMask == null || sourceTransform == null)
        {
            return;
        }

        Transform maskTransform = runtimeMask.transform;
        maskTransform.position = sourceTransform.position;
        maskTransform.rotation = sourceTransform.rotation;

        Vector3 parentScale = maskTransform.parent != null ? maskTransform.parent.lossyScale : Vector3.one;
        Vector3 sourceScale = sourceTransform.lossyScale;
        maskTransform.localScale = new Vector3(
            SafeScale(sourceScale.x, parentScale.x),
            SafeScale(sourceScale.y, parentScale.y),
            SafeScale(sourceScale.z, parentScale.z));
    }

    private static float SafeScale(float source, float parent)
    {
        return Mathf.Abs(parent) > 0.0001f ? source / parent : source;
    }

    private void SetMaskActive(bool active)
    {
        if (runtimeMask == null)
        {
            Transform maskTransform = transform.Find(MaskObjectName);
            if (maskTransform != null)
            {
                runtimeMask = maskTransform.GetComponent<SpriteMask>();
            }
        }

        if (runtimeMask != null && runtimeMask.gameObject.activeSelf != active)
        {
            runtimeMask.gameObject.SetActive(active);
        }
    }
}

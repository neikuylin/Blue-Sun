using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[AddComponentMenu("战斗/角色遮挡挖空控制器")]
public sealed class Sprite角色遮挡挖空控制器 : MonoBehaviour
{
    private const int MaxRevealCount = 32;
    private const float UnitRefreshInterval = 0.25f;
    private const int TransparentQueue = 3000;
    private const int ZTestLessEqual = 4;
    private const int ZTestAlways = 8;
    private const int RevealDepthModeDisabled = 0;
    private const int RevealDepthModeAlways = 1;
    private const int RevealDepthModeDepthTest = 2;

    private static readonly int RevealEnabledId = Shader.PropertyToID("_OcclusionRevealEnabled");
    private static readonly int RevealCountId = Shader.PropertyToID("_OcclusionRevealCount");
    private static readonly int RevealRadiusPixelsId = Shader.PropertyToID("_OcclusionRevealRadiusPixels");
    private static readonly int RevealSoftnessPixelsId = Shader.PropertyToID("_OcclusionRevealSoftnessPixels");
    private static readonly int RevealCentersId = Shader.PropertyToID("_OcclusionRevealCenters");
    private static readonly int RevealDepthModeId = Shader.PropertyToID("_OcclusionRevealDepthMode");
    private static readonly int DissolveNoiseScaleId = Shader.PropertyToID("_DissolveNoiseScale");
    private static readonly int DissolveStrengthId = Shader.PropertyToID("_DissolveStrength");
    private static readonly int DissolveEdgeWidthId = Shader.PropertyToID("_DissolveEdgeWidth");
    private static readonly int ZTestId = Shader.PropertyToID("_ZTest");

    private static readonly Vector4[] RevealCenters = new Vector4[MaxRevealCount];
    private static readonly List<BattleUnit> CachedUnits = new List<BattleUnit>();
    private static float nextUnitRefreshTime;

    [Header("角色遮挡挖空")]
    [InspectorName("启用角色圆形挖空")]
    [SerializeField] private bool revealEnabled = true;

    [InspectorName("角色周围挖空半径（世界单位）")]
    [Min(0f)]
    [SerializeField] private float radiusWorld = 1.2f;

    [InspectorName("挖空边缘软化（世界单位）")]
    [Tooltip("0 是硬边；大于 0 时边缘会平滑过渡。")]
    [Min(0f)]
    [SerializeField] private float softnessWorld = 0.25f;

    [Header("边缘颗粒")]
    [InspectorName("颗粒尺寸（像素）")]
    [Tooltip("数值越小颗粒越密。")]
    [Range(1f, 32f)]
    [SerializeField] private float dissolveNoiseScale = 6f;

    [InspectorName("颗粒强度")]
    [Tooltip("0 为关闭颗粒，1 为最明显。")]
    [Range(0f, 1f)]
    [SerializeField] private float dissolveStrength = 0.45f;

    [InspectorName("颗粒边缘宽度（像素）")]
    [Tooltip("颗粒影响挖空边缘的屏幕像素宽度。")]
    [Range(0f, 128f)]
    [SerializeField] private float dissolveEdgeWidth = 18f;

    [Header("屏幕位置计算")]
    [InspectorName("用于计算角色屏幕位置的相机")]
    [Tooltip("为空时使用 Camera.main。")]
    [SerializeField] private Camera targetCamera;

    [Header("作用目标")]
    [InspectorName("目标Renderer（为空时使用当前物体Renderer）")]
    [SerializeField] private Renderer[] targetRenderers;

    private Renderer[] cachedRenderers;
    private MaterialPropertyBlock propertyBlock;

    private void OnEnable()
    {
        CacheRenderers();
        Apply();
    }

    private void OnDisable()
    {
        ClearReveal();
    }

    private void OnValidate()
    {
        CacheRenderers();
        Apply();
    }

    private void LateUpdate()
    {
        Apply();
    }

    [ContextMenu("应用遮挡挖空")]
    public void Apply()
    {
        Renderer[] renderers = ResolveRenderers();
        if (renderers.Length == 0)
        {
            return;
        }

        Camera cameraToUse = ResolveCamera();
        if (!revealEnabled || cameraToUse == null)
        {
            ClearReveal();
            return;
        }

        RefreshUnitsIfNeeded();
        int revealCount = BuildRevealCenters(cameraToUse);
        float revealDepth = ResolveRevealDepth(revealCount);
        float revealRadiusPixels = WorldLengthToScreenPixels(cameraToUse, revealDepth, radiusWorld);
        float revealSoftnessPixels = WorldLengthToScreenPixels(cameraToUse, revealDepth, softnessWorld);
        ApplyReveal(revealCount, revealRadiusPixels, revealSoftnessPixels);
    }

    private Camera ResolveCamera()
    {
        return targetCamera != null ? targetCamera : Camera.main;
    }

    private Renderer[] ResolveRenderers()
    {
        if (targetRenderers != null && targetRenderers.Length > 0)
        {
            return targetRenderers;
        }

        if (cachedRenderers == null || cachedRenderers.Length == 0)
        {
            CacheRenderers();
        }

        return cachedRenderers ?? System.Array.Empty<Renderer>();
    }

    private void CacheRenderers()
    {
        cachedRenderers = GetComponents<Renderer>();
    }

    private void RefreshUnitsIfNeeded()
    {
        float now = Application.isPlaying ? Time.unscaledTime : Time.realtimeSinceStartup;
        if (now >= nextUnitRefreshTime || CachedUnits.Count == 0)
        {
            CachedUnits.Clear();
            BattleUnit[] units = FindObjectsOfType<BattleUnit>(false);
            for (int i = 0; i < units.Length; i++)
            {
                BattleUnit unit = units[i];
                if (unit != null)
                {
                    CachedUnits.Add(unit);
                }
            }

            nextUnitRefreshTime = now + UnitRefreshInterval;
        }
    }

    private int BuildRevealCenters(Camera cameraToUse)
    {
        int writeIndex = 0;
        for (int i = 0; i < CachedUnits.Count && writeIndex < MaxRevealCount; i++)
        {
            BattleUnit unit = CachedUnits[i];
            if (unit == null || !unit.isActiveAndEnabled || !unit.IsAlive)
            {
                continue;
            }

            Vector3 screenPosition = cameraToUse.WorldToScreenPoint(unit.transform.position);
            if (screenPosition.z <= 0f)
            {
                continue;
            }

            RevealCenters[writeIndex] = new Vector4(screenPosition.x, screenPosition.y, screenPosition.z, 0f);
            writeIndex++;
        }

        for (int i = writeIndex; i < MaxRevealCount; i++)
        {
            RevealCenters[i] = Vector4.zero;
        }

        return writeIndex;
    }

    private static float ResolveRevealDepth(int revealCount)
    {
        for (int i = 0; i < revealCount; i++)
        {
            if (RevealCenters[i].z > 0f)
            {
                return RevealCenters[i].z;
            }
        }

        return 0f;
    }

    private static float WorldLengthToScreenPixels(Camera cameraToUse, float depth, float worldLength)
    {
        if (cameraToUse == null || depth <= 0f || worldLength <= 0f)
        {
            return 0f;
        }

        if (cameraToUse.orthographic)
        {
            return worldLength * cameraToUse.pixelHeight / (cameraToUse.orthographicSize * 2f);
        }

        Vector3 worldCenter = cameraToUse.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, depth));
        Vector3 worldEdge = worldCenter + cameraToUse.transform.right * worldLength;
        Vector3 screenCenter = cameraToUse.WorldToScreenPoint(worldCenter);
        Vector3 screenEdge = cameraToUse.WorldToScreenPoint(worldEdge);
        return Vector2.Distance(screenCenter, screenEdge);
    }

    private void ApplyReveal(int revealCount, float revealRadiusPixels, float revealSoftnessPixels)
    {
        Renderer[] renderers = ResolveRenderers();
        MaterialPropertyBlock block = GetPropertyBlock();

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            int depthMode = ResolveRevealDepthMode(renderer);
            renderer.GetPropertyBlock(block);
            block.SetInt(RevealEnabledId, revealCount > 0 && depthMode != RevealDepthModeDisabled ? 1 : 0);
            block.SetInt(RevealCountId, revealCount);
            block.SetInt(RevealDepthModeId, depthMode);
            block.SetFloat(RevealRadiusPixelsId, Mathf.Max(0f, revealRadiusPixels));
            block.SetFloat(RevealSoftnessPixelsId, Mathf.Max(0f, revealSoftnessPixels));
            block.SetFloat(DissolveNoiseScaleId, Mathf.Max(1f, dissolveNoiseScale));
            block.SetFloat(DissolveStrengthId, Mathf.Clamp01(dissolveStrength));
            block.SetFloat(DissolveEdgeWidthId, Mathf.Max(0f, dissolveEdgeWidth));
            block.SetVectorArray(RevealCentersId, RevealCenters);
            renderer.SetPropertyBlock(block);
        }
    }

    private void ClearReveal()
    {
        Renderer[] renderers = ResolveRenderers();
        if (renderers.Length == 0)
        {
            return;
        }

        MaterialPropertyBlock block = GetPropertyBlock();

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            renderer.GetPropertyBlock(block);
            block.SetInt(RevealEnabledId, 0);
            block.SetInt(RevealCountId, 0);
            block.SetInt(RevealDepthModeId, RevealDepthModeDisabled);
            renderer.SetPropertyBlock(block);
        }
    }

    private static int ResolveRevealDepthMode(Renderer renderer)
    {
        Material material = renderer.sharedMaterial;
        if (material == null)
        {
            return RevealDepthModeDisabled;
        }

        int renderQueue = material.renderQueue;
        if (renderQueue < TransparentQueue)
        {
            return RevealDepthModeDisabled;
        }

        int zTest = material.HasProperty(ZTestId) ? Mathf.RoundToInt(material.GetFloat(ZTestId)) : ZTestLessEqual;
        if (renderQueue > TransparentQueue || zTest == ZTestAlways)
        {
            return RevealDepthModeAlways;
        }

        if (zTest == ZTestLessEqual)
        {
            return RevealDepthModeDepthTest;
        }

        return RevealDepthModeDisabled;
    }

    private MaterialPropertyBlock GetPropertyBlock()
    {
        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }

        return propertyBlock;
    }
}

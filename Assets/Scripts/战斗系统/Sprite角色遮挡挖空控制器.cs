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
    private const int RevealDepthModeGridDepthTest = 3;

    private static readonly int RevealEnabledId = Shader.PropertyToID("_OcclusionRevealEnabled");
    private static readonly int RevealCountId = Shader.PropertyToID("_OcclusionRevealCount");
    private static readonly int RevealRadiusPixelsId = Shader.PropertyToID("_OcclusionRevealRadiusPixels");
    private static readonly int RevealSoftnessPixelsId = Shader.PropertyToID("_OcclusionRevealSoftnessPixels");
    private static readonly int RevealCentersId = Shader.PropertyToID("_OcclusionRevealCenters");
    private static readonly int RevealAnchorDepthKeyId = Shader.PropertyToID("_OcclusionRevealAnchorDepthKey");
    private static readonly int RevealDepthModeId = Shader.PropertyToID("_OcclusionRevealDepthMode");
    private static readonly int DissolveNoiseScaleId = Shader.PropertyToID("_DissolveNoiseScale");
    private static readonly int DissolveStrengthId = Shader.PropertyToID("_DissolveStrength");
    private static readonly int DissolveEdgeWidthId = Shader.PropertyToID("_DissolveEdgeWidth");
    private static readonly int DissolveScrollSpeedId = Shader.PropertyToID("_DissolveScrollSpeed");
    private static readonly int DissolveSmoothEdgesId = Shader.PropertyToID("_DissolveSmoothEdges");
    private static readonly int ZTestId = Shader.PropertyToID("_ZTest");

    private static readonly Vector4[] RevealCenters = new Vector4[MaxRevealCount];
    private static readonly List<BattleUnit> CachedUnits = new List<BattleUnit>();
    private static SpriteOcclusionRevealSettings cachedSettings;
    private static float nextUnitRefreshTime;

    [Header("作用目标")]
    [InspectorName("目标Renderer（为空时使用当前物体Renderer）")]
    [SerializeField] private Renderer[] targetRenderers;
    [InspectorName("包含子物体Renderer")]
    [SerializeField] private bool includeChildRenderers;
    [InspectorName("包含未激活子物体")]
    [SerializeField] private bool includeInactiveChildren = true;
    [InspectorName("无视高低3D都挖空")]
    [SerializeField] private bool revealRegardlessOfRenderLevel;

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

        Camera cameraToUse = Camera.main;
        SpriteOcclusionRevealSettings settings = ResolveSettings();
        if (settings == null || !settings.RevealEnabled || cameraToUse == null)
        {
            ClearReveal();
            BattleGrid.ClearOcclusionOccupiedCellShadow();
            return;
        }

        RefreshUnitsIfNeeded();
        int revealCount = BuildRevealCenters(cameraToUse);
        float revealDepth = ResolveRevealDepth(revealCount);
        float revealRadiusPixels = WorldLengthToScreenPixels(cameraToUse, revealDepth, settings.RadiusWorld);
        float revealSoftnessPixels = WorldLengthToScreenPixels(cameraToUse, revealDepth, settings.SoftnessWorld);
        ApplyReveal(cameraToUse, revealCount, revealRadiusPixels, revealSoftnessPixels, settings);
        BattleGrid.ApplyOcclusionOccupiedCellShadow(revealCount, revealRadiusPixels, revealSoftnessPixels, RevealCenters);
    }

    public void 开启无视高低3D都挖空()
    {
        revealRegardlessOfRenderLevel = true;
        Apply();
    }

    public void 关闭无视高低3D都挖空()
    {
        revealRegardlessOfRenderLevel = false;
        Apply();
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
        cachedRenderers = includeChildRenderers
            ? GetComponentsInChildren<Renderer>(includeInactiveChildren)
            : GetComponents<Renderer>();
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

            Vector3 screenPosition = cameraToUse.WorldToScreenPoint(unit.GetOcclusionRevealCenterWorldPosition());
            if (screenPosition.z <= 0f)
            {
                continue;
            }

            float anchorDepthKey = unit.GetOcclusionDepthKey(cameraToUse);
            RevealCenters[writeIndex] = new Vector4(screenPosition.x, screenPosition.y, screenPosition.z, anchorDepthKey);
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

    private void ApplyReveal(
        Camera cameraToUse,
        int revealCount,
        float revealRadiusPixels,
        float revealSoftnessPixels,
        SpriteOcclusionRevealSettings settings)
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

            int depthMode = ResolveRevealDepthMode(renderer, revealRegardlessOfRenderLevel);
            renderer.GetPropertyBlock(block);
            block.SetInt(RevealEnabledId, revealCount > 0 && depthMode != RevealDepthModeDisabled ? 1 : 0);
            block.SetInt(RevealCountId, revealCount);
            block.SetInt(RevealDepthModeId, depthMode);
            block.SetFloat(RevealRadiusPixelsId, Mathf.Max(0f, revealRadiusPixels));
            block.SetFloat(RevealSoftnessPixelsId, Mathf.Max(0f, revealSoftnessPixels));
            block.SetFloat(RevealAnchorDepthKeyId, ResolveAnchorDepthKey(cameraToUse, renderer));
            block.SetFloat(DissolveNoiseScaleId, settings.DissolveNoiseScale);
            block.SetFloat(DissolveStrengthId, settings.DissolveStrength);
            block.SetFloat(DissolveEdgeWidthId, settings.DissolveEdgeWidth);
            block.SetFloat(DissolveScrollSpeedId, settings.DissolveScrollSpeed);
            block.SetInt(DissolveSmoothEdgesId, settings.DissolveSmoothEdges ? 1 : 0);
            block.SetVectorArray(RevealCentersId, RevealCenters);
            renderer.SetPropertyBlock(block);
        }
    }

    private static float ResolveAnchorDepthKey(Camera cameraToUse, Renderer renderer)
    {
        if (cameraToUse == null || renderer == null)
        {
            return 0f;
        }

        BattleGridOcclusionAnchor gridAnchor = renderer.GetComponentInParent<BattleGridOcclusionAnchor>();
        if (BattleGridOcclusionAnchor.ShouldUseGridOcclusion(renderer) &&
            gridAnchor != null &&
            gridAnchor.TryGetDepthKey(cameraToUse, out float depthKey))
        {
            return depthKey;
        }

        Vector3 anchorWorldPosition = renderer.transform.position;
        战斗格子沙盘辅助 sandbox = renderer.GetComponentInParent<战斗格子沙盘辅助>();
        if (sandbox != null)
        {
            anchorWorldPosition = sandbox.GetCellCenterWorld(sandbox.AnchorCellInSandbox);
        }

        return cameraToUse.WorldToScreenPoint(anchorWorldPosition).y;
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

    private static int ResolveRevealDepthMode(Renderer renderer, bool revealRegardlessOfRenderLevel)
    {
        if (BattleGridOcclusionAnchor.ShouldUseGridOcclusion(renderer))
        {
            return RevealDepthModeGridDepthTest;
        }

        Material material = renderer.sharedMaterial;
        if (material == null)
        {
            return RevealDepthModeDisabled;
        }

        int renderQueue = material.renderQueue;
        if (renderQueue < TransparentQueue)
        {
            return revealRegardlessOfRenderLevel ? RevealDepthModeAlways : RevealDepthModeDisabled;
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

    private static SpriteOcclusionRevealSettings ResolveSettings()
    {
        if (cachedSettings == null)
        {
            cachedSettings = SpriteOcclusionRevealSettings.LoadDefault();
        }

        return cachedSettings;
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

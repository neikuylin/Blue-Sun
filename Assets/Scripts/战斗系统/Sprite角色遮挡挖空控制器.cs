using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[AddComponentMenu("战斗/角色遮挡挖空控制器")]
public sealed class Sprite角色遮挡挖空控制器 : MonoBehaviour
{
    private const int MaxRevealCount = 32;
    private const float UnitRefreshInterval = 0.25f;

    private static readonly int RevealEnabledId = Shader.PropertyToID("_OcclusionRevealEnabled");
    private static readonly int RevealCountId = Shader.PropertyToID("_OcclusionRevealCount");
    private static readonly int RevealRadiusPixelsId = Shader.PropertyToID("_OcclusionRevealRadiusPixels");
    private static readonly int RevealSoftnessPixelsId = Shader.PropertyToID("_OcclusionRevealSoftnessPixels");
    private static readonly int RevealCentersId = Shader.PropertyToID("_OcclusionRevealCenters");

    private static readonly Vector4[] RevealCenters = new Vector4[MaxRevealCount];
    private static readonly List<BattleUnit> CachedUnits = new List<BattleUnit>();
    private static float nextUnitRefreshTime;

    [Header("角色遮挡挖空")]
    [InspectorName("启用角色圆形挖空")]
    [SerializeField] private bool revealEnabled = true;

    [InspectorName("角色周围挖空半径（像素）")]
    [Min(0f)]
    [SerializeField] private float radiusPixels = 120f;

    [InspectorName("挖空边缘软化（像素）")]
    [Tooltip("0 是硬边；大于 0 时边缘会平滑过渡。")]
    [Min(0f)]
    [SerializeField] private float softnessPixels;

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
        ApplyReveal(revealCount);
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

            RevealCenters[writeIndex] = new Vector4(screenPosition.x, screenPosition.y, 0f, 0f);
            writeIndex++;
        }

        for (int i = writeIndex; i < MaxRevealCount; i++)
        {
            RevealCenters[i] = Vector4.zero;
        }

        return writeIndex;
    }

    private void ApplyReveal(int revealCount)
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

            renderer.GetPropertyBlock(block);
            block.SetInt(RevealEnabledId, revealCount > 0 ? 1 : 0);
            block.SetInt(RevealCountId, revealCount);
            block.SetFloat(RevealRadiusPixelsId, Mathf.Max(0f, radiusPixels));
            block.SetFloat(RevealSoftnessPixelsId, Mathf.Max(0f, softnessPixels));
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
            renderer.SetPropertyBlock(block);
        }
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

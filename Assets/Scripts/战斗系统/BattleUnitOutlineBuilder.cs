using System.Collections.Generic;
using UnityEngine;

public static class BattleUnitOutlineBuilder
{
    private const string OutlineMaskShaderName = "Battle/UnitOutlineMask";
    private const string OutlineShaderName = "Battle/UnitOutline";
    private const string OutlineMaskObjectPrefix = "__OutlineMask_";
    private const string OutlineObjectPrefix = "__Outline_";
    private static Material sharedOutlineMaskMaterial;
    private static Material sharedOutlineMaterial;

    public static void Apply(GameObject root, Color outlineColor, float outlineWidth)
    {
        if (root == null)
        {
            return;
        }

        Material outlineMaterial = GetOrCreateMaterial(outlineColor, outlineWidth);
        Material outlineMaskMaterial = GetOrCreateMaskMaterial();
        if (outlineMaterial == null || outlineMaskMaterial == null)
        {
            return;
        }

        MeshRenderer[] meshRenderers = root.GetComponentsInChildren<MeshRenderer>(true);
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            MeshRenderer renderer = meshRenderers[i];
            if (renderer == null || renderer.GetComponent<BattleUnitOutlineMarker>() != null)
            {
                continue;
            }

            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null)
            {
                continue;
            }

            CreateMeshOutline(renderer.transform, filter.sharedMesh, outlineMaskMaterial, outlineMaterial);
        }

        SkinnedMeshRenderer[] skinnedRenderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int i = 0; i < skinnedRenderers.Length; i++)
        {
            SkinnedMeshRenderer renderer = skinnedRenderers[i];
            if (renderer == null || renderer.GetComponent<BattleUnitOutlineMarker>() != null || renderer.sharedMesh == null)
            {
                continue;
            }

            CreateSkinnedOutline(renderer, outlineMaskMaterial, outlineMaterial);
        }
    }

    public static void ApplyOverlay(
        GameObject root,
        string outlineObjectPrefix,
        Color outlineColor,
        float outlineWidth,
        bool visibleByDefault = false)
    {
        if (root == null || string.IsNullOrWhiteSpace(outlineObjectPrefix))
        {
            return;
        }

        Material outlineMaterial = CreateMaterial(outlineColor, outlineWidth, outlineObjectPrefix + "Material");
        if (outlineMaterial == null)
        {
            return;
        }

        MeshRenderer[] meshRenderers = root.GetComponentsInChildren<MeshRenderer>(true);
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            MeshRenderer renderer = meshRenderers[i];
            if (renderer == null || renderer.GetComponent<BattleUnitOutlineMarker>() != null)
            {
                continue;
            }

            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null)
            {
                continue;
            }

            CreateMeshOverlay(renderer.transform, filter.sharedMesh, outlineMaterial, outlineObjectPrefix, visibleByDefault);
        }

        SkinnedMeshRenderer[] skinnedRenderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int i = 0; i < skinnedRenderers.Length; i++)
        {
            SkinnedMeshRenderer renderer = skinnedRenderers[i];
            if (renderer == null || renderer.GetComponent<BattleUnitOutlineMarker>() != null || renderer.sharedMesh == null)
            {
                continue;
            }

            CreateSkinnedOverlay(renderer, outlineMaterial, outlineObjectPrefix, visibleByDefault);
        }
    }

    private static Material GetOrCreateMaskMaterial()
    {
        Shader shader = Shader.Find(OutlineMaskShaderName);
        if (shader == null)
        {
            Debug.LogWarning($"BattleUnitOutlineBuilder: shader '{OutlineMaskShaderName}' not found.");
            return null;
        }

        if (sharedOutlineMaskMaterial == null || sharedOutlineMaskMaterial.shader != shader)
        {
            sharedOutlineMaskMaterial = new Material(shader)
            {
                name = "BattleUnitOutlineMaskMaterial"
            };
        }

        return sharedOutlineMaskMaterial;
    }

    private static Material GetOrCreateMaterial(Color outlineColor, float outlineWidth)
    {
        Shader shader = Shader.Find(OutlineShaderName);
        if (shader == null)
        {
            Debug.LogWarning($"BattleUnitOutlineBuilder: shader '{OutlineShaderName}' not found.");
            return null;
        }

        if (sharedOutlineMaterial == null || sharedOutlineMaterial.shader != shader)
        {
            sharedOutlineMaterial = new Material(shader)
            {
                name = "BattleUnitOutlineMaterial"
            };
        }

        sharedOutlineMaterial.SetColor("_OutlineColor", outlineColor);
        sharedOutlineMaterial.SetFloat("_OutlineWidth", Mathf.Max(0f, outlineWidth));
        return sharedOutlineMaterial;
    }

    private static Material CreateMaterial(Color outlineColor, float outlineWidth, string materialName)
    {
        Shader shader = Shader.Find(OutlineShaderName);
        if (shader == null)
        {
            Debug.LogWarning($"BattleUnitOutlineBuilder: shader '{OutlineShaderName}' not found.");
            return null;
        }

        Material material = new Material(shader)
        {
            name = materialName
        };
        material.SetColor("_OutlineColor", outlineColor);
        material.SetFloat("_OutlineWidth", Mathf.Max(0f, outlineWidth));
        return material;
    }

    private static void CreateMeshOutline(Transform sourceTransform, Mesh sourceMesh, Material outlineMaskMaterial, Material outlineMaterial)
    {
        if (sourceTransform == null || sourceMesh == null || outlineMaskMaterial == null || outlineMaterial == null || HasOutlineChild(sourceTransform))
        {
            return;
        }

        CreateMeshMask(sourceTransform, sourceMesh, outlineMaskMaterial);

        GameObject outlineObject = new GameObject(OutlineObjectPrefix + sourceTransform.name);
        outlineObject.transform.SetParent(sourceTransform, false);
        outlineObject.transform.localPosition = Vector3.zero;
        outlineObject.transform.localRotation = Quaternion.identity;
        outlineObject.transform.localScale = Vector3.one;
        outlineObject.AddComponent<BattleUnitOutlineMarker>();

        MeshFilter filter = outlineObject.AddComponent<MeshFilter>();
        filter.sharedMesh = sourceMesh;

        MeshRenderer renderer = outlineObject.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = outlineMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
    }

    private static void CreateSkinnedOutline(SkinnedMeshRenderer sourceRenderer, Material outlineMaskMaterial, Material outlineMaterial)
    {
        if (sourceRenderer == null || outlineMaskMaterial == null || outlineMaterial == null || HasOutlineChild(sourceRenderer.transform))
        {
            return;
        }

        CreateSkinnedMask(sourceRenderer, outlineMaskMaterial);

        GameObject outlineObject = new GameObject(OutlineObjectPrefix + sourceRenderer.name);
        outlineObject.transform.SetParent(sourceRenderer.transform, false);
        outlineObject.transform.localPosition = Vector3.zero;
        outlineObject.transform.localRotation = Quaternion.identity;
        outlineObject.transform.localScale = Vector3.one;
        outlineObject.AddComponent<BattleUnitOutlineMarker>();

        SkinnedMeshRenderer outlineRenderer = outlineObject.AddComponent<SkinnedMeshRenderer>();
        outlineRenderer.sharedMesh = sourceRenderer.sharedMesh;
        outlineRenderer.rootBone = sourceRenderer.rootBone;
        outlineRenderer.bones = sourceRenderer.bones;
        outlineRenderer.localBounds = sourceRenderer.localBounds;
        outlineRenderer.updateWhenOffscreen = true;
        outlineRenderer.quality = sourceRenderer.quality;
        outlineRenderer.sharedMaterial = outlineMaterial;
        outlineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        outlineRenderer.receiveShadows = false;
        outlineRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        outlineRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
    }

    private static void CreateMeshMask(Transform sourceTransform, Mesh sourceMesh, Material outlineMaskMaterial)
    {
        GameObject maskObject = new GameObject(OutlineMaskObjectPrefix + sourceTransform.name);
        maskObject.transform.SetParent(sourceTransform, false);
        maskObject.transform.localPosition = Vector3.zero;
        maskObject.transform.localRotation = Quaternion.identity;
        maskObject.transform.localScale = Vector3.one;
        maskObject.AddComponent<BattleUnitOutlineMarker>();

        MeshFilter filter = maskObject.AddComponent<MeshFilter>();
        filter.sharedMesh = sourceMesh;

        MeshRenderer renderer = maskObject.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = outlineMaskMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
    }

    private static void CreateSkinnedMask(SkinnedMeshRenderer sourceRenderer, Material outlineMaskMaterial)
    {
        GameObject maskObject = new GameObject(OutlineMaskObjectPrefix + sourceRenderer.name);
        maskObject.transform.SetParent(sourceRenderer.transform, false);
        maskObject.transform.localPosition = Vector3.zero;
        maskObject.transform.localRotation = Quaternion.identity;
        maskObject.transform.localScale = Vector3.one;
        maskObject.AddComponent<BattleUnitOutlineMarker>();

        SkinnedMeshRenderer maskRenderer = maskObject.AddComponent<SkinnedMeshRenderer>();
        maskRenderer.sharedMesh = sourceRenderer.sharedMesh;
        maskRenderer.rootBone = sourceRenderer.rootBone;
        maskRenderer.bones = sourceRenderer.bones;
        maskRenderer.localBounds = sourceRenderer.localBounds;
        maskRenderer.updateWhenOffscreen = true;
        maskRenderer.quality = sourceRenderer.quality;
        maskRenderer.sharedMaterial = outlineMaskMaterial;
        maskRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        maskRenderer.receiveShadows = false;
        maskRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        maskRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
    }

    private static bool HasOutlineChild(Transform sourceTransform)
    {
        if (sourceTransform == null)
        {
            return true;
        }

        for (int i = 0; i < sourceTransform.childCount; i++)
        {
            Transform child = sourceTransform.GetChild(i);
            if (child == null)
            {
                continue;
            }

            if (child.GetComponent<BattleUnitOutlineMarker>() != null)
            {
                if (child.name.StartsWith(OutlineObjectPrefix) || child.name.StartsWith(OutlineMaskObjectPrefix))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool HasOutlineChildWithPrefix(Transform sourceTransform, string outlineObjectPrefix)
    {
        if (sourceTransform == null || string.IsNullOrWhiteSpace(outlineObjectPrefix))
        {
            return true;
        }

        for (int i = 0; i < sourceTransform.childCount; i++)
        {
            Transform child = sourceTransform.GetChild(i);
            if (child == null || child.GetComponent<BattleUnitOutlineMarker>() == null)
            {
                continue;
            }

            if (child.name.StartsWith(outlineObjectPrefix, System.StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static void CreateMeshOverlay(
        Transform sourceTransform,
        Mesh sourceMesh,
        Material outlineMaterial,
        string outlineObjectPrefix,
        bool visibleByDefault)
    {
        if (sourceTransform == null ||
            sourceMesh == null ||
            outlineMaterial == null ||
            HasOutlineChildWithPrefix(sourceTransform, outlineObjectPrefix))
        {
            return;
        }

        GameObject outlineObject = new GameObject(outlineObjectPrefix + sourceTransform.name);
        outlineObject.transform.SetParent(sourceTransform, false);
        outlineObject.transform.localPosition = Vector3.zero;
        outlineObject.transform.localRotation = Quaternion.identity;
        outlineObject.transform.localScale = Vector3.one;
        outlineObject.AddComponent<BattleUnitOutlineMarker>();

        MeshFilter filter = outlineObject.AddComponent<MeshFilter>();
        filter.sharedMesh = sourceMesh;

        MeshRenderer renderer = outlineObject.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = outlineMaterial;
        renderer.enabled = visibleByDefault;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
    }

    private static void CreateSkinnedOverlay(
        SkinnedMeshRenderer sourceRenderer,
        Material outlineMaterial,
        string outlineObjectPrefix,
        bool visibleByDefault)
    {
        if (sourceRenderer == null ||
            outlineMaterial == null ||
            HasOutlineChildWithPrefix(sourceRenderer.transform, outlineObjectPrefix))
        {
            return;
        }

        GameObject outlineObject = new GameObject(outlineObjectPrefix + sourceRenderer.name);
        outlineObject.transform.SetParent(sourceRenderer.transform, false);
        outlineObject.transform.localPosition = Vector3.zero;
        outlineObject.transform.localRotation = Quaternion.identity;
        outlineObject.transform.localScale = Vector3.one;
        outlineObject.AddComponent<BattleUnitOutlineMarker>();

        SkinnedMeshRenderer outlineRenderer = outlineObject.AddComponent<SkinnedMeshRenderer>();
        outlineRenderer.sharedMesh = sourceRenderer.sharedMesh;
        outlineRenderer.rootBone = sourceRenderer.rootBone;
        outlineRenderer.bones = sourceRenderer.bones;
        outlineRenderer.localBounds = sourceRenderer.localBounds;
        outlineRenderer.updateWhenOffscreen = true;
        outlineRenderer.quality = sourceRenderer.quality;
        outlineRenderer.sharedMaterial = outlineMaterial;
        outlineRenderer.enabled = visibleByDefault;
        outlineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        outlineRenderer.receiveShadows = false;
        outlineRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        outlineRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
    }
}

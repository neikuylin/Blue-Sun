using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

public static class ProjectFontSwitcher
{
    private const string SansSourcePath =
        "Assets/字体包/思源黑体/SourceHanSansSC-Regular.otf";
    private const string SerifSourcePath =
        "Assets/字体包/思源宋体/SourceHanSerifSC-Regular.otf";
    private const string FontFolder =
        "Assets/字体包/Resources/Fonts & Materials";

    private const string LegacyNormalPath = FontFolder + "/普通.asset";
    private const string LegacyOutlinePath = FontFolder + "/描边.asset";
    private const string SansNormalPath = FontFolder + "/黑体普通.asset";
    private const string SansOutlinePath = FontFolder + "/黑体描边.asset";
    private const string SerifNormalPath = FontFolder + "/宋体普通.asset";
    private const string SerifOutlinePath = FontFolder + "/宋体描边.asset";

    private static readonly HashSet<string> CorpusExtensions =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".asset", ".cs", ".csv", ".hlsl", ".json", ".prefab",
            ".shader", ".txt", ".unity", ".xml",
        };

    private static readonly HashSet<string> ReferenceExtensions =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".asset", ".prefab", ".unity",
        };

    [MenuItem("工具/字体/切换宋体")]
    public static void SwitchToSerif()
    {
        SwitchFont(
            "宋体",
            SerifNormalPath,
            SerifOutlinePath,
            SansNormalPath,
            SansOutlinePath);
    }

    [MenuItem("工具/字体/切换黑体")]
    public static void SwitchToSans()
    {
        SwitchFont(
            "黑体",
            SansNormalPath,
            SansOutlinePath,
            SerifNormalPath,
            SerifOutlinePath);
    }

    [MenuItem("工具/创建字体/烘培普通")]
    public static void BakeNormalFonts()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        string characters = CollectProjectCharacters();
        RebuildFont(
            SansSourcePath,
            SansNormalPath,
            "黑体普通",
            characters,
            false);
        RebuildFont(
            SerifSourcePath,
            SerifNormalPath,
            "宋体普通",
            characters,
            false);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.SaveAssets();
        Debug.Log("黑体和宋体普通字体已按当前项目字符完成烘培。");
    }

    [MenuItem("工具/创建字体/烘培描边")]
    public static void BakeOutlineFonts()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        string characters = CollectProjectCharacters();
        RebuildFont(
            SansSourcePath,
            SansOutlinePath,
            "黑体描边",
            characters,
            true);
        RebuildFont(
            SerifSourcePath,
            SerifOutlinePath,
            "宋体描边",
            characters,
            true);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.SaveAssets();
        Debug.Log("黑体和宋体描边字体已按当前项目字符完成烘培，并启用 Outline 功能。");
    }

    private static void SwitchFont(
        string targetName,
        string targetNormalPath,
        string targetOutlinePath,
        string otherNormalPath,
        string otherOutlinePath)
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        MoveLegacySansAssets();
        EnsureAllFontAssets();

        TMP_FontAsset targetNormal =
            AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(targetNormalPath);
        TMP_FontAsset targetOutline =
            AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(targetOutlinePath);
        TMP_FontAsset otherNormal =
            AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(otherNormalPath);
        TMP_FontAsset otherOutline =
            AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(otherOutlinePath);

        Validate(targetNormal, targetName + "普通");
        Validate(targetOutline, targetName + "描边");
        Validate(otherNormal, "备用普通");
        Validate(otherOutline, "备用描边");

        int changedFiles = 0;
        changedFiles += ReplaceReferences(otherNormal, targetNormal);
        changedFiles += ReplaceReferences(otherOutline, targetOutline);

        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.SaveAssets();
        Debug.Log(
            $"字体已切换为思源{targetName}，更新资源文件 {changedFiles} 个。");
    }

    private static void MoveLegacySansAssets()
    {
        MoveAssetIfNeeded(LegacyNormalPath, SansNormalPath);
        MoveAssetIfNeeded(LegacyOutlinePath, SansOutlinePath);
    }

    private static void MoveAssetIfNeeded(string sourcePath, string targetPath)
    {
        if (AssetDatabase.LoadMainAssetAtPath(targetPath) != null ||
            AssetDatabase.LoadMainAssetAtPath(sourcePath) == null)
        {
            return;
        }

        string error = AssetDatabase.MoveAsset(sourcePath, targetPath);
        if (!string.IsNullOrEmpty(error))
        {
            throw new InvalidOperationException(
                $"移动字体资产失败：{sourcePath} -> {targetPath}\n{error}");
        }
    }

    private static void EnsureAllFontAssets()
    {
        string characters = null;
        EnsureFontPair(
            SansSourcePath,
            SansNormalPath,
            SansOutlinePath,
            "黑体",
            ref characters);
        EnsureFontPair(
            SerifSourcePath,
            SerifNormalPath,
            SerifOutlinePath,
            "宋体",
            ref characters);
        AssetDatabase.SaveAssets();
    }

    private static void EnsureFontPair(
        string sourcePath,
        string normalPath,
        string outlinePath,
        string familyName,
        ref string characters)
    {
        bool needsNormal =
            AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(normalPath) == null;
        bool needsOutline =
            AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(outlinePath) == null;
        if (!needsNormal && !needsOutline)
        {
            return;
        }

        Font sourceFont = LoadSourceFont(sourcePath);
        if (characters == null)
        {
            characters = CollectProjectCharacters();
        }

        if (needsNormal)
        {
            CreateFontAsset(
                sourceFont,
                normalPath,
                familyName + "普通",
                characters,
                false);
        }

        if (needsOutline)
        {
            CreateFontAsset(
                sourceFont,
                outlinePath,
                familyName + "描边",
                characters,
                true);
        }
    }

    private static void RebuildFont(
        string sourcePath,
        string targetPath,
        string assetName,
        string characters,
        bool outline)
    {
        TMP_FontAsset previous =
            AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(targetPath);
        string temporaryPath =
            FontFolder + "/__" + assetName + "重建.asset";
        AssetDatabase.DeleteAsset(temporaryPath);

        TMP_FontAsset rebuilt = CreateFontAsset(
            LoadSourceFont(sourcePath),
            temporaryPath,
            assetName,
            characters,
            outline);
        if (previous != null)
        {
            ReplaceReferences(previous, rebuilt);
            AssetDatabase.DeleteAsset(targetPath);
        }

        string error = AssetDatabase.MoveAsset(temporaryPath, targetPath);
        if (!string.IsNullOrEmpty(error))
        {
            throw new InvalidOperationException(
                $"替换字体失败：{targetPath}\n{error}");
        }
    }

    private static Font LoadSourceFont(string sourcePath)
    {
        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(sourcePath);
        if (sourceFont == null)
        {
            throw new InvalidOperationException(
                "无法导入字体源文件：" + sourcePath);
        }

        TrueTypeFontImporter importer =
            AssetImporter.GetAtPath(sourcePath) as TrueTypeFontImporter;
        if (importer != null && !importer.includeFontData)
        {
            importer.includeFontData = true;
            importer.SaveAndReimport();
            sourceFont = AssetDatabase.LoadAssetAtPath<Font>(sourcePath);
        }

        return sourceFont;
    }

    private static TMP_FontAsset CreateFontAsset(
        Font sourceFont,
        string assetPath,
        string assetName,
        string characters,
        bool outline)
    {
        FontEngine.InitializeFontEngine();
        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
            sourceFont,
            64,
            outline ? 9 : 2,
            outline ? GlyphRenderMode.SDFAA : GlyphRenderMode.SMOOTH_HINTED,
            4096,
            4096,
            AtlasPopulationMode.Dynamic,
            false);
        if (fontAsset == null)
        {
            throw new InvalidOperationException(
                "TMP 无法创建字体资源：" + assetName);
        }

        fontAsset.name = assetName;
        fontAsset.atlasTexture.name = assetName + " Atlas";
        fontAsset.material.name = assetName + " Atlas Material";
        AssetDatabase.CreateAsset(fontAsset, assetPath);
        AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
        AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);

        bool allAdded = fontAsset.TryAddCharacters(
            characters,
            out string missingCharacters,
            true);
        if (!allAdded && !string.IsNullOrEmpty(missingCharacters))
        {
            Debug.LogWarning(
                $"{assetName}有 {missingCharacters.Length} 个字符不在源字体中。");
        }

        fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;
        if (outline)
        {
            ConfigureOutlineMaterial(fontAsset.material);
        }

        SerializedObject serialized = new SerializedObject(fontAsset);
        SerializedProperty clearDynamic =
            serialized.FindProperty("m_ClearDynamicDataOnBuild");
        if (clearDynamic != null)
        {
            clearDynamic.boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorUtility.SetDirty(fontAsset);
        EditorUtility.SetDirty(fontAsset.material);
        return fontAsset;
    }

    private static void ConfigureOutlineMaterial(Material material)
    {
        Shader shader = Shader.Find("TextMeshPro/Mobile/Distance Field");
        if (shader == null)
        {
            throw new InvalidOperationException(
                "找不到 TextMeshPro Mobile SDF Shader。");
        }

        material.shader = shader;
        material.EnableKeyword(ShaderUtilities.Keyword_Outline);
        material.DisableKeyword(ShaderUtilities.Keyword_Underlay);
        material.DisableKeyword("UNDERLAY_INNER");
        material.SetColor(ShaderUtilities.ID_FaceColor, Color.white);
        material.SetFloat(ShaderUtilities.ID_FaceDilate, 0f);
        material.SetColor(ShaderUtilities.ID_OutlineColor, Color.black);
        material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.15f);
        material.SetFloat(ShaderUtilities.ID_OutlineSoftness, 0f);
        material.SetFloat(ShaderUtilities.ID_WeightNormal, 0f);
        material.SetFloat(ShaderUtilities.ID_WeightBold, 0.5f);
        EditorUtility.SetDirty(material);
    }

    private static string CollectProjectCharacters()
    {
        HashSet<char> characters = new HashSet<char>();
        for (char value = ' '; value <= '~'; value++)
        {
            characters.Add(value);
        }

        string assetsRoot =
            Path.Combine(Directory.GetCurrentDirectory(), "Assets");
        foreach (string path in Directory.GetFiles(
                     assetsRoot,
                     "*",
                     SearchOption.AllDirectories))
        {
            if (!CorpusExtensions.Contains(Path.GetExtension(path)))
            {
                continue;
            }

            string text;
            try
            {
                text = File.ReadAllText(path, Encoding.UTF8);
            }
            catch
            {
                continue;
            }

            foreach (char value in text)
            {
                if (!char.IsControl(value) && !char.IsSurrogate(value))
                {
                    characters.Add(value);
                }
            }

            foreach (Match match in Regex.Matches(
                         text,
                         @"\\u([0-9a-fA-F]{4})"))
            {
                characters.Add((char)int.Parse(
                    match.Groups[1].Value,
                    NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture));
            }
        }

        List<char> ordered = new List<char>(characters);
        ordered.Sort();
        return new string(ordered.ToArray());
    }

    private static int ReplaceReferences(
        TMP_FontAsset source,
        TMP_FontAsset target)
    {
        string sourceGuid = AssetDatabase.AssetPathToGUID(
            AssetDatabase.GetAssetPath(source));
        string targetGuid = AssetDatabase.AssetPathToGUID(
            AssetDatabase.GetAssetPath(target));
        long targetMaterialId = GetLocalId(target.material);
        Regex reference = new Regex(
            @"\{fileID:\s*(-?\d+),\s*guid:\s*" +
            Regex.Escape(sourceGuid) +
            @",\s*type:\s*2\}");
        UTF8Encoding encoding = new UTF8Encoding(false);
        string assetsRoot =
            Path.Combine(Directory.GetCurrentDirectory(), "Assets");
        int changedFiles = 0;

        foreach (string path in Directory.GetFiles(
                     assetsRoot,
                     "*",
                     SearchOption.AllDirectories))
        {
            if (!ReferenceExtensions.Contains(Path.GetExtension(path)))
            {
                continue;
            }

            string text = File.ReadAllText(path, Encoding.UTF8);
            if (text.IndexOf(sourceGuid, StringComparison.Ordinal) < 0)
            {
                continue;
            }

            string replaced = reference.Replace(
                text,
                match =>
                {
                    long oldFileId = long.Parse(
                        match.Groups[1].Value,
                        CultureInfo.InvariantCulture);
                    long newFileId =
                        oldFileId == 11400000
                            ? 11400000
                            : targetMaterialId;
                    return
                        $"{{fileID: {newFileId}, guid: {targetGuid}, type: 2}}";
                });
            if (string.Equals(text, replaced, StringComparison.Ordinal))
            {
                continue;
            }

            File.WriteAllText(path, replaced, encoding);
            changedFiles++;
        }

        return changedFiles;
    }

    private static long GetLocalId(UnityEngine.Object asset)
    {
        if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                asset,
                out _,
                out long localId))
        {
            throw new InvalidOperationException(
                "无法读取 TMP 材质本地文件编号。");
        }

        return localId;
    }

    private static void Validate(TMP_FontAsset fontAsset, string name)
    {
        if (fontAsset == null ||
            fontAsset.material == null ||
            fontAsset.atlasTexture == null ||
            fontAsset.characterTable.Count < 100 ||
            fontAsset.atlasPopulationMode != AtlasPopulationMode.Static)
        {
            throw new InvalidOperationException(
                name + "字体资源校验失败。");
        }
    }
}

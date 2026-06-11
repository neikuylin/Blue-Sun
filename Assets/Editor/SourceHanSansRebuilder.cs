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

public static class SourceHanSansRebuilder
{
    private const string SourceFontPath =
        "Assets/字体包/思源黑体/SourceHanSansSC-Regular.otf";
    private const string FontFolder =
        "Assets/字体包/Resources/Fonts & Materials";
    private const string NormalPath = FontFolder + "/普通.asset";
    private const string OutlinePath = FontFolder + "/描边.asset";
    private const string OldNormalGuid = "798f01969f29a9144b0010f9e4aebf27";
    private const string OldOutlineGuid = "7442cab02a003e54ebcbf1eaa727c3dd";

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

    [MenuItem("工具/字体/从零重建思源黑体")]
    public static void Run()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
        if (sourceFont == null)
        {
            throw new InvalidOperationException(
                "无法导入官方思源黑体：" + SourceFontPath);
        }

        TrueTypeFontImporter importer =
            AssetImporter.GetAtPath(SourceFontPath) as TrueTypeFontImporter;
        if (importer != null && !importer.includeFontData)
        {
            importer.includeFontData = true;
            importer.SaveAndReimport();
            sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
        }

        string characters = CollectProjectCharacters();
        TMP_FontAsset normal = CreateFontAsset(
            sourceFont,
            NormalPath,
            "普通",
            characters,
            false);
        TMP_FontAsset outline = CreateFontAsset(
            sourceFont,
            OutlinePath,
            "描边",
            characters,
            true);
        AssetDatabase.SaveAssets();

        string normalGuid = AssetDatabase.AssetPathToGUID(NormalPath);
        string outlineGuid = AssetDatabase.AssetPathToGUID(OutlinePath);
        long normalMaterialId = GetLocalId(normal.material);
        long outlineMaterialId = GetLocalId(outline.material);

        ReplaceReferences(
            OldNormalGuid,
            normalGuid,
            normalMaterialId);
        ReplaceReferences(
            OldOutlineGuid,
            outlineGuid,
            outlineMaterialId);

        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.SaveAssets();

        normal = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(NormalPath);
        outline = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OutlinePath);
        Validate(normal, "普通");
        Validate(outline, "描边");

        Debug.Log(
            $"思源黑体从零重建完成。预烘焙字符：{characters.Length}，" +
            $"普通字符：{normal.characterTable.Count}，" +
            $"描边字符：{outline.characterTable.Count}，" +
            $"普通 GUID：{normalGuid}，描边 GUID：{outlineGuid}");
    }

    private static TMP_FontAsset CreateFontAsset(
        Font sourceFont,
        string assetPath,
        string assetName,
        string characters,
        bool outline)
    {
        if (AssetDatabase.LoadMainAssetAtPath(assetPath) != null)
        {
            AssetDatabase.DeleteAsset(assetPath);
        }

        FontEngine.InitializeFontEngine();
        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
            sourceFont,
            64,
            2,
            GlyphRenderMode.SMOOTH_HINTED,
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

    private static void ReplaceReferences(
        string oldGuid,
        string newGuid,
        long materialId)
    {
        Regex reference = new Regex(
            @"\{fileID:\s*(-?\d+),\s*guid:\s*" +
            Regex.Escape(oldGuid) +
            @",\s*type:\s*2\}");
        UTF8Encoding encoding = new UTF8Encoding(false);
        string assetsRoot =
            Path.Combine(Directory.GetCurrentDirectory(), "Assets");

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
            if (text.IndexOf(oldGuid, StringComparison.Ordinal) < 0)
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
                        oldFileId == 11400000 ? 11400000 : materialId;
                    return
                        $"{{fileID: {newFileId}, guid: {newGuid}, type: 2}}";
                });
            if (!string.Equals(text, replaced, StringComparison.Ordinal))
            {
                File.WriteAllText(path, replaced, encoding);
            }
        }
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
                name + "字体重建校验失败。");
        }
    }
}

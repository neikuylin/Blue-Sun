using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

public static class SourceHanSerifMigration
{
    private const string SourceFontPath =
        "Assets/字体包/思源宋体/SourceHanSerifSC-Regular.otf";

    private static readonly string[] TargetFontAssetPaths =
    {
        "Assets/字体包/Resources/Fonts & Materials/普通.asset",
        "Assets/字体包/Resources/Fonts & Materials/描边.asset",
    };

    public static void Run()
    {
        AssetDatabase.ImportAsset(
            SourceFontPath,
            ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
        if (sourceFont == null)
        {
            throw new InvalidOperationException($"无法导入思源宋体：{SourceFontPath}");
        }

        string sourceFontGuid = AssetDatabase.AssetPathToGUID(SourceFontPath);
        foreach (string assetPath in TargetFontAssetPaths)
        {
            MigrateFontAsset(assetPath, sourceFont, sourceFontGuid);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("思源宋体迁移完成：普通和描边字体资产已保留原 GUID 并切换源字体。");
    }

    private static void MigrateFontAsset(
        string assetPath,
        Font sourceFont,
        string sourceFontGuid)
    {
        TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        if (fontAsset == null)
        {
            throw new InvalidOperationException($"找不到 TMP 字体资产：{assetPath}");
        }

        SerializedObject serializedFontAsset = new SerializedObject(fontAsset);
        SetObjectReference(serializedFontAsset, "m_SourceFontFile", sourceFont);
        SetString(serializedFontAsset, "m_SourceFontFileGUID", sourceFontGuid);
        SetString(serializedFontAsset, "m_SourceFontFilePath", string.Empty);
        SetString(
            serializedFontAsset,
            "m_CreationSettings.sourceFontFileGUID",
            sourceFontGuid);
        SetString(
            serializedFontAsset,
            "m_CreationSettings.sourceFontFileName",
            sourceFont.name);
        SetInteger(
            serializedFontAsset,
            "m_AtlasPopulationMode",
            (int)AtlasPopulationMode.Dynamic);
        serializedFontAsset.ApplyModifiedPropertiesWithoutUndo();

        FontEngine.InitializeFontEngine();
        FontEngineError loadResult = FontEngine.LoadFontFace(
            sourceFont,
            fontAsset.faceInfo.pointSize,
            0);
        if (loadResult != FontEngineError.Success)
        {
            throw new InvalidOperationException(
                $"无法读取思源宋体度量信息：{loadResult}");
        }

        fontAsset.faceInfo = FontEngine.GetFaceInfo();
        fontAsset.ClearFontAssetData();
        EditorUtility.SetDirty(fontAsset);
        Debug.Log($"已迁移 TMP 字体资产：{assetPath}");
    }

    private static void SetString(
        SerializedObject serializedObject,
        string propertyPath,
        string value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyPath);
        if (property != null)
        {
            property.stringValue = value;
        }
    }

    private static void SetInteger(
        SerializedObject serializedObject,
        string propertyPath,
        int value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyPath);
        if (property != null)
        {
            property.intValue = value;
        }
    }

    private static void SetObjectReference(
        SerializedObject serializedObject,
        string propertyPath,
        UnityEngine.Object value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyPath);
        if (property != null)
        {
            property.objectReferenceValue = value;
        }
    }
}

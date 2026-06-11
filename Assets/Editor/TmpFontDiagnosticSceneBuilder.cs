using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

public static class TmpFontDiagnosticSceneBuilder
{
    private const string SourceFontPath =
        "Assets/字体包/思源黑体/SourceHanSansSC-Regular.otf";
    private const string OutputFolder = "Assets/字体诊断";
    private const string ScenePath = OutputFolder + "/字体描边诊断.unity";
    private const string TestText = "开始按钮  返回  对话文字  ABC 123";
    private const string FocusText = "开始按钮";
    private const string TestCharacters =
        TestText + "SDF_SDFAA_HINTEDFULLUNITYLIBERATION";
    private const string LiberationSansPath =
        "Assets/字体包/Resources/Fonts & Materials/LiberationSans SDF.asset";

    [MenuItem("工具/字体/生成隔离诊断场景 %#t")]
    public static void Build()
    {
        EnsureOutputFolder();
        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
        if (sourceFont == null)
        {
            throw new InvalidOperationException("找不到测试源字体：" + SourceFontPath);
        }

        TMP_FontAsset sdfaa = CreateTestFont(
            sourceFont,
            "测试_精度_SDFAA",
            GlyphRenderMode.SDFAA,
            128,
            13,
            2048,
            FocusText);
        TMP_FontAsset sdf8 = CreateTestFont(
            sourceFont,
            "测试_精度_SDF8",
            GlyphRenderMode.SDF8,
            128,
            13,
            2048,
            FocusText);
        TMP_FontAsset sdf16 = CreateTestFont(
            sourceFont,
            "测试_精度_SDF16",
            GlyphRenderMode.SDF16,
            128,
            13,
            2048,
            FocusText);
        TMP_FontAsset sdf32 = CreateTestFont(
            sourceFont,
            "测试_精度_SDF32",
            GlyphRenderMode.SDF32,
            128,
            13,
            2048,
            FocusText);

        Scene scene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene,
            NewSceneMode.Single);
        CreateCamera();
        Canvas canvas = CreateCanvas();
        CreateTextRow(
            canvas.transform,
            sdfaa,
            sdfaa.material,
            "SDFAA 快速模式",
            180f,
            FocusText);
        CreateTextRow(
            canvas.transform,
            sdf8,
            sdf8.material,
            "SDF8 精确模式",
            60f,
            FocusText);
        CreateTextRow(
            canvas.transform,
            sdf16,
            sdf16.material,
            "SDF16 精确模式",
            -60f,
            FocusText);
        CreateTextRow(
            canvas.transform,
            sdf32,
            sdf32.material,
            "SDF32 精确模式",
            -180f,
            FocusText);
        CreateEventSystem();

        EditorSceneManager.SaveScene(scene, ScenePath);
        ExportAtlas(sdfaa, "Atlas_SDFAA.png");
        ExportAtlas(sdf8, "Atlas_SDF8.png");
        ExportAtlas(sdf16, "Atlas_SDF16.png");
        ExportAtlas(sdf32, "Atlas_SDF32.png");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        Debug.Log("字体隔离诊断场景生成完成：" + ScenePath);
    }

    private static void ExportAtlas(TMP_FontAsset fontAsset, string fileName)
    {
        Texture2D atlas = fontAsset.atlasTexture;
        if (atlas == null || !atlas.isReadable)
        {
            throw new InvalidOperationException(
                "测试字体图集不可读：" + fontAsset.name);
        }

        string outputPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            OutputFolder,
            fileName);
        File.WriteAllBytes(outputPath, atlas.EncodeToPNG());
    }

    [MenuItem("工具/字体/打印 TMP 诊断 Shader")]
    [MenuItem("Tools/Fonts/Export Diagnostic Atlas Alpha %#e")]
    public static void ExportDiagnosticAtlasAlpha()
    {
        ExportExistingAtlasAlpha("SDFAA");
        ExportExistingAtlasAlpha("SDF8");
        ExportExistingAtlasAlpha("SDF16");
        ExportExistingAtlasAlpha("SDF32");
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Debug.Log("Diagnostic atlas alpha images exported.");
    }

    private static void ExportExistingAtlasAlpha(string mode)
    {
        string[] guids = AssetDatabase.FindAssets(
            "t:TMP_FontAsset",
            new[] { OutputFolder });
        TMP_FontAsset fontAsset = null;
        foreach (string guid in guids)
        {
            TMP_FontAsset candidate =
                AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                    AssetDatabase.GUIDToAssetPath(guid));
            if (candidate != null &&
                candidate.name.EndsWith("_" + mode, StringComparison.Ordinal))
            {
                fontAsset = candidate;
                break;
            }
        }

        if (fontAsset == null)
        {
            throw new InvalidOperationException(
                "Diagnostic font asset not found: " + mode);
        }

        Texture2D atlas = fontAsset.atlasTexture;
        Color32[] source = atlas.GetPixels32();
        Color32[] visibleAlpha = new Color32[source.Length];
        for (int i = 0; i < source.Length; i++)
        {
            byte alpha = source[i].a;
            visibleAlpha[i] = new Color32(alpha, alpha, alpha, 255);
        }

        Texture2D output = new Texture2D(
            atlas.width,
            atlas.height,
            TextureFormat.RGBA32,
            false,
            true);
        output.SetPixels32(visibleAlpha);
        output.Apply(false, false);

        string outputPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            OutputFolder,
            "AtlasAlpha_" + mode + ".png");
        File.WriteAllBytes(outputPath, output.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(output);
    }

    [MenuItem("Tools/Fonts/Log Diagnostic Mesh Scale %#m")]
    public static void LogDiagnosticMeshScale()
    {
        TextMeshProUGUI[] texts =
            UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>(
                FindObjectsSortMode.None);
        foreach (TextMeshProUGUI text in texts)
        {
            text.ForceMeshUpdate();
            TMP_MeshInfo[] meshInfo = text.textInfo.meshInfo;
            for (int meshIndex = 0; meshIndex < meshInfo.Length; meshIndex++)
            {
                Vector2[] uv2 = meshInfo[meshIndex].uvs2;
                if (uv2 == null || uv2.Length == 0)
                {
                    continue;
                }

                float min = float.PositiveInfinity;
                float max = float.NegativeInfinity;
                for (int i = 0; i < meshInfo[meshIndex].vertexCount; i++)
                {
                    float value = uv2[i].y;
                    min = Mathf.Min(min, value);
                    max = Mathf.Max(max, value);
                }

                Debug.Log(
                    $"{text.transform.parent.name}: TEXCOORD1.y " +
                    $"min={min:R}, max={max:R}, fontSize={text.fontSize:R}, " +
                    $"canvasScale={text.canvas.scaleFactor:R}");
            }
        }
    }

    [MenuItem("Tools/Fonts/Add Derivative Shader Comparison %#d")]
    public static void AddDerivativeShaderComparison()
    {
        const string fontPath =
            OutputFolder + "/测试_精度_SDFAA.asset";
        const string materialPath =
            OutputFolder + "/测试_屏幕导数材质.mat";

        TMP_FontAsset fontAsset =
            AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
        Shader shader = Shader.Find("Hidden/TMP/SDF Derivative Diagnostic");
        if (fontAsset == null || shader == null)
        {
            throw new InvalidOperationException(
                "Derivative comparison dependencies are not ready.");
        }

        AssetDatabase.DeleteAsset(materialPath);
        Material material = new Material(shader)
        {
            name = "测试_屏幕导数材质",
        };
        material.SetTexture("_MainTex", fontAsset.atlasTexture);
        material.SetColor("_FaceColor", Color.white);
        AssetDatabase.CreateAsset(material, materialPath);

        Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        Transform previous = canvas.transform.Find("屏幕导数 Shader");
        if (previous != null)
        {
            UnityEngine.Object.DestroyImmediate(previous.gameObject);
        }

        CreateTextRow(
            canvas.transform,
            fontAsset,
            material,
            "屏幕导数 Shader",
            -300f,
            FocusText);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("Derivative shader comparison added.");
    }

    public static void LogDiagnosticShaders()
    {
        string[] shaderNames =
        {
            "TextMeshPro/Distance Field",
            "TextMeshPro/Mobile/Distance Field",
            "TextMeshPro/Distance Field Overlay",
            "TextMeshPro/Mobile/Distance Field - Masking",
        };

        foreach (string shaderName in shaderNames)
        {
            Shader shader = Shader.Find(shaderName);
            Debug.Log(
                shader == null
                    ? "TMP Shader 未找到：" + shaderName
                    : $"TMP Shader：{shaderName}，路径：{AssetDatabase.GetAssetPath(shader)}");
        }
    }

    private static TMP_FontAsset CreateTestFont(
        Font sourceFont,
        string assetName,
        GlyphRenderMode renderMode,
        int samplingPointSize = 64,
        int padding = 9,
        int atlasSize = 1024,
        string characters = TestCharacters,
        float perspectiveFilter = 0.875f)
    {
        string path = OutputFolder + "/" + assetName + ".asset";
        AssetDatabase.DeleteAsset(path);

        FontEngine.InitializeFontEngine();
        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
            sourceFont,
            samplingPointSize,
            padding,
            renderMode,
            atlasSize,
            atlasSize,
            AtlasPopulationMode.Dynamic,
            false);
        if (fontAsset == null)
        {
            throw new InvalidOperationException("无法生成测试字体：" + assetName);
        }

        fontAsset.name = assetName;
        fontAsset.atlasTexture.name = assetName + " Atlas";
        fontAsset.material.name = assetName + " Material";
        AssetDatabase.CreateAsset(fontAsset, path);
        AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
        AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        fontAsset.TryAddCharacters(characters, out _, true);
        fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;

        Material material = fontAsset.material;
        material.DisableKeyword(ShaderUtilities.Keyword_Outline);
        material.DisableKeyword(ShaderUtilities.Keyword_Underlay);
        material.DisableKeyword("UNDERLAY_INNER");
        material.SetFloat(ShaderUtilities.ID_FaceDilate, 0f);
        material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0f);
        material.SetFloat(ShaderUtilities.ID_OutlineSoftness, 0f);
        material.SetFloat(ShaderUtilities.ID_WeightNormal, 0f);
        material.SetFloat(ShaderUtilities.ID_WeightBold, 0f);
        material.SetFloat(
            Shader.PropertyToID("_PerspectiveFilter"),
            perspectiveFilter);

        EditorUtility.SetDirty(fontAsset);
        EditorUtility.SetDirty(material);
        return fontAsset;
    }

    private static Material CreateFullSdfMaterial(TMP_FontAsset fontAsset)
    {
        const string path = OutputFolder + "/测试_SDFAA_FullShader.mat";
        AssetDatabase.DeleteAsset(path);

        Shader shader = Shader.Find("TextMeshPro/Distance Field");
        if (shader == null)
        {
            throw new InvalidOperationException(
                "找不到 TextMeshPro/Distance Field Shader。");
        }

        Material material = new Material(shader)
        {
            name = "测试_SDFAA_FullShader",
        };
        material.SetTexture(ShaderUtilities.ID_MainTex, fontAsset.atlasTexture);
        material.SetFloat(
            ShaderUtilities.ID_GradientScale,
            fontAsset.material.GetFloat(ShaderUtilities.ID_GradientScale));
        material.SetFloat(ShaderUtilities.ID_TextureWidth, fontAsset.atlasWidth);
        material.SetFloat(ShaderUtilities.ID_TextureHeight, fontAsset.atlasHeight);
        material.SetFloat(ShaderUtilities.ID_FaceDilate, 0f);
        material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0f);
        material.SetFloat(ShaderUtilities.ID_OutlineSoftness, 0f);
        material.SetFloat(ShaderUtilities.ID_WeightNormal, 0f);
        material.SetFloat(ShaderUtilities.ID_WeightBold, 0f);
        material.DisableKeyword(ShaderUtilities.Keyword_Outline);
        material.DisableKeyword(ShaderUtilities.Keyword_Underlay);
        material.DisableKeyword("UNDERLAY_INNER");
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void EnsureOutputFolder()
    {
        if (!AssetDatabase.IsValidFolder(OutputFolder))
        {
            AssetDatabase.CreateFolder("Assets", Path.GetFileName(OutputFolder));
        }
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.12f, 0.12f, 0.12f, 1f);
        camera.orthographic = true;
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
    }

    private static Canvas CreateCanvas()
    {
        GameObject canvasObject = new GameObject(
            "Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.additionalShaderChannels =
            AdditionalCanvasShaderChannels.TexCoord1 |
            AdditionalCanvasShaderChannels.Normal |
            AdditionalCanvasShaderChannels.Tangent;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    private static void CreateTextRow(
        Transform parent,
        TMP_FontAsset fontAsset,
        Material material,
        string modeName,
        float y,
        string sampleText = TestText)
    {
        GameObject panelObject = new GameObject(
            modeName,
            typeof(RectTransform),
            typeof(Image));
        panelObject.transform.SetParent(parent, false);
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(1500f, 100f);
        panelRect.anchoredPosition = new Vector2(0f, y);
        panelObject.GetComponent<Image>().color = Color.white;

        GameObject textObject = new GameObject(
            "Text",
            typeof(RectTransform),
            typeof(TextMeshProUGUI));
        textObject.transform.SetParent(panelObject.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(30f, 10f);
        textRect.offsetMax = new Vector2(-30f, -10f);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = modeName + "    " + sampleText;
        text.font = fontAsset;
        text.fontSharedMaterial = material;
        text.fontSize = 48f;
        text.fontStyle = FontStyles.Normal;
        text.fontWeight = FontWeight.Regular;
        text.color = Color.black;
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = false;
        text.raycastTarget = false;
    }

    private static void CreateEventSystem()
    {
        new GameObject(
            "EventSystem",
            typeof(EventSystem),
            typeof(StandaloneInputModule));
    }
}

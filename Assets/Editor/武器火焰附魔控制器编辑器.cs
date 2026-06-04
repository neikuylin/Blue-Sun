using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(武器火焰附魔控制器))]
public sealed class 武器火焰附魔控制器编辑器 : Editor
{
    private const string 全局配置路径 = "武器火焰附魔全局配置";

    private SerializedObject 全局配置序列化对象;

    private void OnEnable()
    {
        ApplySelectedControllers();
    }

    public override void OnInspectorGUI()
    {
        武器火焰附魔控制器 controller = target as 武器火焰附魔控制器;
        DrawStatus(controller);

        武器火焰附魔全局配置 config = controller != null ? controller.当前全局配置 : null;
        if (config == null)
        {
            EditorGUILayout.HelpBox($"找不到 Resources/{全局配置路径}。所有武器火焰附魔都不会生效。", MessageType.Error);
            return;
        }

        if (全局配置序列化对象 == null || 全局配置序列化对象.targetObject != config)
        {
            全局配置序列化对象 = new SerializedObject(config);
        }

        全局配置序列化对象.Update();

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("全项目共用配置", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("这里编辑的是全局配置。所有挂了武器火焰附魔控制器的武器都会读取同一份参数。", MessageType.Info);

        EditorGUI.BeginChangeCheck();
        DrawGlobalConfigFields();
        bool changed = EditorGUI.EndChangeCheck();

        全局配置序列化对象.ApplyModifiedProperties();
        if (changed)
        {
            EditorUtility.SetDirty(config);
            ApplySelectedControllers();
        }

        EditorGUILayout.Space(8f);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("应用火焰附魔"))
            {
                ApplySelectedControllers();
            }

            if (GUILayout.Button("还原原始材质"))
            {
                RestoreSelectedControllers();
            }
        }
    }

    private void DrawGlobalConfigFields()
    {
        EditorGUILayout.LabelField("开关与材质", EditorStyles.boldLabel);
        DrawProperty("启用附魔", "启用附魔");
        DrawProperty("火焰材质", "火焰材质");

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("火焰颜色", EditorStyles.boldLabel);
        DrawProperty("颜色", "模型颜色");
        DrawProperty("暗部火焰颜色", "暗部火焰颜色");
        DrawProperty("主火焰颜色", "主火焰颜色");
        DrawProperty("核心火焰颜色", "核心火焰颜色");

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("火焰流动", EditorStyles.boldLabel);
        DrawProperty("火焰强度", "火焰强度");
        DrawProperty("火焰速度", "火焰速度");
        DrawProperty("火焰密度", "火焰密度");
        DrawProperty("流动方向", "流动方向");
        DrawProperty("原图保留强度", "原图保留强度");

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("外扩包围火焰", EditorStyles.boldLabel);
        DrawProperty("外扩火焰范围", "外扩火焰范围");
        DrawProperty("外扩火焰强度", "外扩火焰强度");
        DrawProperty("闪烁强度", "闪烁强度");
        DrawProperty("闪烁速度", "闪烁速度");

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("上飘火星粒子", EditorStyles.boldLabel);
        DrawProperty("启用火星粒子", "启用火星粒子");
        SerializedProperty enableSpark = 全局配置序列化对象.FindProperty("启用火星粒子");
        if (enableSpark != null && enableSpark.boolValue)
        {
            DrawProperty("火星无视深度", "火星无视深度");
            DrawProperty("火星粒子材质", "火星粒子材质");
            DrawProperty("火星数量", "火星数量");
            DrawProperty("火星大小", "火星大小");
            DrawProperty("火星上升速度", "火星上升速度");
            DrawProperty("火星左右扰动", "火星左右扰动");
            DrawProperty("火星生命周期", "火星生命周期");
            DrawProperty("火星圆点颜色", "火星圆点颜色");
            DrawProperty("火星外发光颜色", "火星外发光颜色");
            DrawProperty("火星起始颜色", "生命周期起始叠色");
            DrawProperty("火星结束颜色", "生命周期结束叠色");
            DrawProperty("火星发射范围倍率", "火星发射范围倍率");
        }

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Trail火焰拖尾", EditorStyles.boldLabel);
        DrawProperty("启用Trail火焰拖尾", "启用Trail火焰拖尾");
        SerializedProperty enableTrail = 全局配置序列化对象.FindProperty("启用Trail火焰拖尾");
        if (enableTrail != null && enableTrail.boolValue)
        {
            DrawProperty("Trail无视深度", "Trail无视深度");
            DrawProperty("Trail拖尾材质", "Trail拖尾材质");
            DrawProperty("Trail整体长轴倍率", "整体长轴倍率");
            DrawProperty("Trail持续时间", "持续时间");
            DrawProperty("Trail触发速度阈值", "触发速度阈值");
            DrawProperty("Trail最小顶点距离", "最小顶点距离");
            DrawProperty("Trail末端宽度倍率", "末端宽度倍率");
            DrawProperty("Trail低速宽度收缩", "低速宽度收缩");
            DrawProperty("Trail外侧颜色", "外侧颜色");
            DrawProperty("Trail内侧颜色", "内侧颜色");
            DrawProperty("Trail边缘柔和", "边缘柔和");
            DrawProperty("Trail火焰噪声密度", "火焰噪声密度");
            DrawProperty("Trail火焰破碎强度", "火焰破碎强度");
            DrawProperty("Trail亮度", "亮度");
        }
    }

    private void DrawProperty(string propertyName, string label)
    {
        SerializedProperty property = 全局配置序列化对象.FindProperty(propertyName);
        if (property == null)
        {
            EditorGUILayout.HelpBox($"全局配置缺少字段：{propertyName}", MessageType.Error);
            return;
        }

        EditorGUILayout.PropertyField(property, new GUIContent(label));
    }

    private static void DrawStatus(武器火焰附魔控制器 controller)
    {
        if (controller == null)
        {
            return;
        }

        MeshRenderer meshRenderer = controller.GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            EditorGUILayout.HelpBox("这个对象缺少 MeshRenderer，无法应用3D武器火焰附魔。请挂到真正带模型渲染器的武器物体上。", MessageType.Error);
            return;
        }

        Material[] originalMaterials = controller.当前原始材质列表;
        EditorGUILayout.HelpBox(
            $"当前MeshRenderer材质槽数量：{meshRenderer.sharedMaterials.Length}\n记录的原始材质槽数量：{(originalMaterials != null ? originalMaterials.Length : 0)}",
            MessageType.Info);
    }

    private void ApplySelectedControllers()
    {
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] is 武器火焰附魔控制器 controller)
            {
                controller.应用附魔设置();
                EditorUtility.SetDirty(controller);
            }
        }

        SceneView.RepaintAll();
    }

    private void RestoreSelectedControllers()
    {
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] is 武器火焰附魔控制器 controller)
            {
                controller.还原原始材质();
                EditorUtility.SetDirty(controller);
            }
        }

        SceneView.RepaintAll();
    }
}

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(武器火焰附魔控制器))]
public sealed class 武器火焰附魔控制器编辑器 : Editor
{
    private SerializedProperty 启用附魔;
    private SerializedProperty 火焰材质;
    private SerializedProperty 颜色;
    private SerializedProperty 暗部火焰颜色;
    private SerializedProperty 主火焰颜色;
    private SerializedProperty 核心火焰颜色;
    private SerializedProperty 火焰强度;
    private SerializedProperty 火焰速度;
    private SerializedProperty 火焰密度;
    private SerializedProperty 流动方向;
    private SerializedProperty 原图保留强度;
    private SerializedProperty 外扩火焰范围;
    private SerializedProperty 外扩火焰强度;
    private SerializedProperty 闪烁强度;
    private SerializedProperty 闪烁速度;

    private void OnEnable()
    {
        启用附魔 = serializedObject.FindProperty("启用附魔");
        火焰材质 = serializedObject.FindProperty("火焰材质");
        颜色 = serializedObject.FindProperty("颜色");
        暗部火焰颜色 = serializedObject.FindProperty("暗部火焰颜色");
        主火焰颜色 = serializedObject.FindProperty("主火焰颜色");
        核心火焰颜色 = serializedObject.FindProperty("核心火焰颜色");
        火焰强度 = serializedObject.FindProperty("火焰强度");
        火焰速度 = serializedObject.FindProperty("火焰速度");
        火焰密度 = serializedObject.FindProperty("火焰密度");
        流动方向 = serializedObject.FindProperty("流动方向");
        原图保留强度 = serializedObject.FindProperty("原图保留强度");
        外扩火焰范围 = serializedObject.FindProperty("外扩火焰范围");
        外扩火焰强度 = serializedObject.FindProperty("外扩火焰强度");
        闪烁强度 = serializedObject.FindProperty("闪烁强度");
        闪烁速度 = serializedObject.FindProperty("闪烁速度");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        武器火焰附魔控制器 controller = target as 武器火焰附魔控制器;
        DrawStatus(controller);

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.LabelField("开关与材质", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(启用附魔, new GUIContent("启用附魔"));
        EditorGUILayout.PropertyField(火焰材质, new GUIContent("火焰材质", "为空时会尝试读取 Resources/武器火焰附魔Sprite材质。"));

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("火焰颜色", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(颜色, new GUIContent("Sprite颜色"));
        EditorGUILayout.PropertyField(暗部火焰颜色, new GUIContent("暗部火焰颜色"));
        EditorGUILayout.PropertyField(主火焰颜色, new GUIContent("主火焰颜色"));
        EditorGUILayout.PropertyField(核心火焰颜色, new GUIContent("核心火焰颜色"));

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("火焰流动", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(火焰强度, new GUIContent("火焰强度"));
        EditorGUILayout.PropertyField(火焰速度, new GUIContent("火焰速度"));
        EditorGUILayout.PropertyField(火焰密度, new GUIContent("火焰密度"));
        EditorGUILayout.PropertyField(流动方向, new GUIContent("流动方向", "以Sprite UV方向为基准，(0,1)通常表示向上流动。"));
        EditorGUILayout.PropertyField(原图保留强度, new GUIContent("原图保留强度", "越高越能看清武器原图，越低越像被火焰吞掉。"));

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("外扩包围火焰", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(外扩火焰范围, new GUIContent("外扩火焰范围", "只会显示在Sprite图片自带的透明留白里。"));
        EditorGUILayout.PropertyField(外扩火焰强度, new GUIContent("外扩火焰强度"));
        EditorGUILayout.PropertyField(闪烁强度, new GUIContent("闪烁强度"));
        EditorGUILayout.PropertyField(闪烁速度, new GUIContent("闪烁速度"));

        bool changed = EditorGUI.EndChangeCheck();
        serializedObject.ApplyModifiedProperties();
        if (changed)
        {
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

    private static void DrawStatus(武器火焰附魔控制器 controller)
    {
        if (controller == null)
        {
            return;
        }

        SpriteRenderer spriteRenderer = controller.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            EditorGUILayout.HelpBox("这个对象缺少 SpriteRenderer，无法应用武器火焰附魔。", MessageType.Error);
            return;
        }

        if (controller.当前火焰材质 == null)
        {
            EditorGUILayout.HelpBox("没有指定火焰材质。为空时脚本会尝试读取 Resources/武器火焰附魔Sprite材质。", MessageType.Info);
        }

        if (spriteRenderer.sprite == null)
        {
            EditorGUILayout.HelpBox("SpriteRenderer 没有 Sprite，材质会被应用，但不会显示火焰。", MessageType.Warning);
        }

        EditorGUILayout.HelpBox(
            $"当前SpriteRenderer材质：{(spriteRenderer.sharedMaterial != null ? spriteRenderer.sharedMaterial.name : "无")}\n记录的原始材质：{(controller.当前原始材质 != null ? controller.当前原始材质.name : "无")}",
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

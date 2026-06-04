using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(武器火焰实体拖尾控制器))]
public sealed class 武器火焰实体拖尾控制器编辑器 : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawBindingStatus();
        EditorGUILayout.Space(6f);

        DrawSection("生成方式", "生成方式");
        DrawModeFields();
        DrawSection("拖尾开关", "启用拖尾", "无视深度", "拖尾材质");
        DrawGenerationFields();
        DrawSection("视觉", "外侧颜色", "内侧颜色", "边缘柔和", "火焰噪声密度", "火焰破碎强度", "亮度");

        EditorGUILayout.Space(8f);
        if (GUILayout.Button("清空火焰实体拖尾"))
        {
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] is 武器火焰实体拖尾控制器 controller)
                {
                    controller.清空片段();
                    EditorUtility.SetDirty(controller);
                }
            }

            SceneView.RepaintAll();
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawBindingStatus()
    {
        SerializedProperty mode = serializedObject.FindProperty("生成方式");
        if (mode != null && mode.enumValueIndex == 0)
        {
            SerializedProperty renderer = serializedObject.FindProperty("目标模型渲染器");
            if (renderer == null || renderer.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("当前是按模型整体生成。目标模型渲染器为空时，运行时会读取本物体上的 MeshRenderer。", MessageType.Info);
                return;
            }

            EditorGUILayout.HelpBox("当前是按模型整体生成。拖尾大小会按目标模型渲染器的包围盒计算。", MessageType.Info);
            return;
        }

        SerializedProperty hilt = serializedObject.FindProperty("刀柄点");
        SerializedProperty tip = serializedObject.FindProperty("刀尖点");
        if (hilt == null || tip == null || hilt.objectReferenceValue == null || tip.objectReferenceValue == null)
        {
            EditorGUILayout.HelpBox("需要绑定刀柄点和刀尖点。脚本会用上一帧到当前帧的两点位置生成实体火焰面片。", MessageType.Warning);
            return;
        }

        EditorGUILayout.HelpBox("已绑定刀柄点和刀尖点。挥动速度超过阈值时会生成世界空间实体拖尾。", MessageType.Info);
    }

    private void DrawModeFields()
    {
        SerializedProperty mode = serializedObject.FindProperty("生成方式");
        if (mode == null)
        {
            EditorGUILayout.HelpBox("缺少字段：生成方式", MessageType.Error);
            return;
        }

        if (mode.enumValueIndex == 0)
        {
            DrawSection("模型整体", "目标模型渲染器", "整体长轴倍率", "速度长度倍率", "前端回退倍率", "拖尾容器");
            return;
        }

        DrawSection("绑定点", "刀柄点", "刀尖点", "拖尾容器");
    }

    private void DrawGenerationFields()
    {
        SerializedProperty mode = serializedObject.FindProperty("生成方式");
        if (mode != null && mode.enumValueIndex == 1)
        {
            DrawSection("拖尾生成", "生成间隔", "拖尾持续时间", "触发速度阈值", "宽度倍率", "最大片段数");
            return;
        }

        DrawSection("拖尾生成", "生成间隔", "拖尾持续时间", "触发速度阈值", "最大片段数");
    }

    private void DrawSection(string title, params string[] propertyNames)
    {
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        for (int i = 0; i < propertyNames.Length; i++)
        {
            DrawProperty(propertyNames[i]);
        }

        EditorGUILayout.Space(6f);
    }

    private void DrawProperty(string propertyName)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            EditorGUILayout.HelpBox($"缺少字段：{propertyName}", MessageType.Error);
            return;
        }

        EditorGUILayout.PropertyField(property, new GUIContent(propertyName));
    }
}

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(弓弦目标拉伸器))]
public sealed class 弓弦目标拉伸器编辑器 : Editor
{
    private SerializedProperty 弦骨骼名称;
    private SerializedProperty 目标点名称;
    private SerializedProperty 拉弦目标点;
    private SerializedProperty 拉弦进度;
    private SerializedProperty 动画状态驱动启用;
    private SerializedProperty 角色动画器;
    private SerializedProperty 动画规则配置;

    private void OnEnable()
    {
        弦骨骼名称 = serializedObject.FindProperty("弦骨骼名称");
        目标点名称 = serializedObject.FindProperty("目标点名称");
        拉弦目标点 = serializedObject.FindProperty("拉弦目标点");
        拉弦进度 = serializedObject.FindProperty("拉弦进度");
        动画状态驱动启用 = serializedObject.FindProperty("动画状态驱动启用");
        角色动画器 = serializedObject.FindProperty("角色动画器");
        动画规则配置 = serializedObject.FindProperty("动画规则配置");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("弓弦目标拉伸", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(弦骨骼名称, new GUIContent("弦骨骼名称"));
        EditorGUILayout.PropertyField(目标点名称, new GUIContent("目标点名称"));
        EditorGUILayout.PropertyField(拉弦目标点, new GUIContent("拉弦目标点"));

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("动画状态驱动", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(动画状态驱动启用, new GUIContent("启用动画状态驱动"));
        EditorGUILayout.PropertyField(角色动画器, new GUIContent("角色动画器"));
        EditorGUILayout.PropertyField(动画规则配置, new GUIContent("动画规则配置"));

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("手动调试", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(拉弦进度, new GUIContent("拉弦进度"));
        bool 拉弦进度已变化 = EditorGUI.EndChangeCheck();

        bool 属性已变化 = serializedObject.ApplyModifiedProperties();
        if (拉弦进度已变化 && 属性已变化)
        {
            自动应用当前拉弦();
        }

        EditorGUILayout.Space(8f);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("查找引用"))
            {
                查找引用();
            }

            if (GUILayout.Button("记录当前姿态"))
            {
                记录当前姿态();
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("应用当前拉弦"))
            {
                应用当前拉弦();
            }

            if (GUILayout.Button("复位拉弦"))
            {
                复位拉弦();
            }
        }
    }

    private void 查找引用()
    {
        弓弦目标拉伸器 drawer = (弓弦目标拉伸器)target;
        Undo.RecordObject(drawer, "查找弓弦拉伸引用");
        bool success = drawer.重新查找引用(out string result);
        标记已修改(drawer);

        if (success)
        {
            Debug.Log(result, drawer);
            return;
        }

        Debug.LogError(result, drawer);
    }

    private void 记录当前姿态()
    {
        弓弦目标拉伸器 drawer = (弓弦目标拉伸器)target;
        Undo.RecordObject(drawer, "记录弓弦初始姿态");
        drawer.重新记录初始姿态();
        标记已修改(drawer);
        Debug.Log("已记录当前弓弦初始姿态。", drawer);
    }

    private void 应用当前拉弦()
    {
        弓弦目标拉伸器 drawer = (弓弦目标拉伸器)target;
        Undo.RegisterFullObjectHierarchyUndo(drawer.gameObject, "应用当前拉弦");
        drawer.应用当前拉弦();
        标记已修改(drawer);
    }

    private void 复位拉弦()
    {
        弓弦目标拉伸器 drawer = (弓弦目标拉伸器)target;
        Undo.RegisterFullObjectHierarchyUndo(drawer.gameObject, "复位拉弦");
        drawer.复位拉弦();
        标记已修改(drawer);
    }

    private void 自动应用当前拉弦()
    {
        弓弦目标拉伸器 drawer = (弓弦目标拉伸器)target;
        Undo.RegisterFullObjectHierarchyUndo(drawer.gameObject, "调整弓弦拉弦进度");
        drawer.应用当前拉弦();
        标记已修改(drawer);
    }

    private static void 标记已修改(弓弦目标拉伸器 drawer)
    {
        EditorUtility.SetDirty(drawer);
        if (drawer != null && drawer.gameObject != null)
        {
            EditorUtility.SetDirty(drawer.gameObject);
        }
    }
}

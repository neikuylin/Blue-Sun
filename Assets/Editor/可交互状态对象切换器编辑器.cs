using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(可交互状态对象切换器))]
public sealed class 可交互状态对象切换器编辑器 : Editor
{
    private SerializedProperty 表现方式;
    private SerializedProperty 普通状态对象;
    private SerializedProperty 悬浮状态对象;
    private SerializedProperty 选中状态对象;
    private SerializedProperty 包含未激活Sprite;
    private SerializedProperty 普通颜色;
    private SerializedProperty 悬浮颜色;
    private SerializedProperty 选中颜色;
    private SerializedProperty 启用互斥选中;

    private void OnEnable()
    {
        表现方式 = serializedObject.FindProperty("表现方式");
        普通状态对象 = serializedObject.FindProperty("普通状态对象");
        悬浮状态对象 = serializedObject.FindProperty("悬浮状态对象");
        选中状态对象 = serializedObject.FindProperty("选中状态对象");
        包含未激活Sprite = serializedObject.FindProperty("包含未激活Sprite");
        普通颜色 = serializedObject.FindProperty("普通颜色");
        悬浮颜色 = serializedObject.FindProperty("悬浮颜色");
        选中颜色 = serializedObject.FindProperty("选中颜色");
        启用互斥选中 = serializedObject.FindProperty("启用互斥选中");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("状态表现", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(表现方式, new GUIContent("表现方式"));
        if ((可交互状态对象切换器.状态表现方式)表现方式.intValue == 可交互状态对象切换器.状态表现方式.颜色染色)
        {
            DrawColorTintSettings();
        }
        else
        {
            DrawObjectSwitchSettings();
        }

        EditorGUILayout.Space(6f);
        EditorGUILayout.PropertyField(启用互斥选中, new GUIContent("启用互斥选中"));
        EditorGUILayout.HelpBox("挂到可互动对象根物体或子物体上。统一点击接入器命中该对象时，会自动调用悬浮状态，点击门或物件时会标记选中。", MessageType.Info);

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawObjectSwitchSettings()
    {
        EditorGUILayout.LabelField("状态对象", EditorStyles.boldLabel);
        DrawGameObjectField(普通状态对象, "普通状态对象");
        DrawGameObjectField(悬浮状态对象, "悬浮状态对象");
        DrawGameObjectField(选中状态对象, "选中状态对象");
    }

    private void DrawColorTintSettings()
    {
        EditorGUILayout.LabelField("颜色染色", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(包含未激活Sprite, new GUIContent("包含未激活Sprite", "自动读取子级 SpriteRenderer 时是否包含未激活对象。"));
        EditorGUILayout.PropertyField(普通颜色, new GUIContent("普通颜色"));
        EditorGUILayout.PropertyField(悬浮颜色, new GUIContent("悬浮颜色"));
        EditorGUILayout.PropertyField(选中颜色, new GUIContent("选中颜色"));

        if (GUILayout.Button("刷新Sprite染色对象"))
        {
            serializedObject.ApplyModifiedProperties();
            刷新选中对象();
            serializedObject.Update();
        }

        EditorGUILayout.HelpBox("颜色染色会读取当前对象下面所有 SpriteRenderer，并在普通、悬浮、选中状态之间切换颜色。", MessageType.Info);
    }

    private void 刷新选中对象()
    {
        for (int i = 0; i < targets.Length; i++)
        {
            可交互状态对象切换器 组件 = targets[i] as 可交互状态对象切换器;
            if (组件 == null)
            {
                continue;
            }

            Undo.RecordObject(组件, "刷新Sprite染色对象");
            组件.刷新Sprite染色对象();
            EditorUtility.SetDirty(组件);
        }
    }

    private static void DrawGameObjectField(SerializedProperty property, string label)
    {
        EditorGUILayout.PropertyField(property, new GUIContent(label));
        GameObject value = property.objectReferenceValue as GameObject;
        if (value != null && value.GetComponent<SpriteRenderer>() == null)
        {
            EditorGUILayout.HelpBox($"{label} 通常应拖带 SpriteRenderer 的 GameObject。", MessageType.Warning);
        }
    }
}

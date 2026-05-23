using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(战技内容视图))]
public sealed class 战技内容视图编辑器 : Editor
{
    private SerializedProperty 战技图标;
    private SerializedProperty 战技名字文本;
    private SerializedProperty 命中率文本;
    private SerializedProperty 战技伤害文本;
    private SerializedProperty 战技描述文本;
    private SerializedProperty 使用者文本;

    private void OnEnable()
    {
        战技图标 = serializedObject.FindProperty("战技图标");
        战技名字文本 = serializedObject.FindProperty("战技名字文本");
        命中率文本 = serializedObject.FindProperty("命中率文本");
        战技伤害文本 = serializedObject.FindProperty("战技伤害文本");
        战技描述文本 = serializedObject.FindProperty("战技描述文本");
        使用者文本 = serializedObject.FindProperty("使用者文本");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("战技内容字段绑定", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(战技图标, new GUIContent("战技图标"));
        EditorGUILayout.PropertyField(战技名字文本, new GUIContent("战技名字文本"));
        EditorGUILayout.PropertyField(命中率文本, new GUIContent("命中率文本"));
        EditorGUILayout.PropertyField(战技伤害文本, new GUIContent("战技伤害文本"));
        EditorGUILayout.PropertyField(战技描述文本, new GUIContent("战技描述文本"));
        EditorGUILayout.PropertyField(使用者文本, new GUIContent("使用者文本"));

        if (有缺失绑定())
        {
            EditorGUILayout.HelpBox("存在未绑定字段。运行时不会按名字查找，会直接输出黄色警告。", MessageType.Warning);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private bool 有缺失绑定()
    {
        return 引用为空(战技图标) ||
            引用为空(战技名字文本) ||
            引用为空(命中率文本) ||
            引用为空(战技伤害文本) ||
            引用为空(战技描述文本) ||
            引用为空(使用者文本);
    }

    private static bool 引用为空(SerializedProperty property)
    {
        return property == null || property.objectReferenceValue == null;
    }
}

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(可交互状态对象切换器))]
public sealed class 可交互状态对象切换器编辑器 : Editor
{
    private SerializedProperty 普通状态对象;
    private SerializedProperty 悬浮状态对象;
    private SerializedProperty 选中状态对象;
    private SerializedProperty 启用互斥选中;

    private void OnEnable()
    {
        普通状态对象 = serializedObject.FindProperty("普通状态对象");
        悬浮状态对象 = serializedObject.FindProperty("悬浮状态对象");
        选中状态对象 = serializedObject.FindProperty("选中状态对象");
        启用互斥选中 = serializedObject.FindProperty("启用互斥选中");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("状态对象", EditorStyles.boldLabel);
        DrawGameObjectField(普通状态对象, "普通状态对象");
        DrawGameObjectField(悬浮状态对象, "悬浮状态对象");
        DrawGameObjectField(选中状态对象, "选中状态对象");

        EditorGUILayout.Space(6f);
        EditorGUILayout.PropertyField(启用互斥选中, new GUIContent("启用互斥选中"));
        EditorGUILayout.HelpBox("挂到可互动对象根物体或子物体上。统一点击接入器命中该对象时，会自动调用悬浮状态。", MessageType.Info);

        serializedObject.ApplyModifiedProperties();
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

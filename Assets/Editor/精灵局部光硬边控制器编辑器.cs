using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(精灵局部光硬边控制器))]
public sealed class 精灵局部光硬边控制器编辑器 : Editor
{
    private SerializedProperty hardEdge;
    private SerializedProperty threshold;
    private SerializedProperty softness;

    private void OnEnable()
    {
        hardEdge = serializedObject.FindProperty("hardEdge");
        threshold = serializedObject.FindProperty("threshold");
        softness = serializedObject.FindProperty("softness");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("精灵局部光", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(hardEdge, new GUIContent("启用硬边", "开启后，使用渲染层级受光材质的精灵会把点光/聚光灯衰减切成硬边。"));
        EditorGUILayout.PropertyField(threshold, new GUIContent("硬边阈值", "数值越大，亮区越小。"));
        EditorGUILayout.PropertyField(softness, new GUIContent("边缘过渡宽度", "越接近 0 越硬，略大一点可以减少锯齿。"));

        serializedObject.ApplyModifiedProperties();
    }
}

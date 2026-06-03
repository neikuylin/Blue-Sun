using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(启程最大人数栏位控制器))]
public sealed class 启程最大人数栏位控制器编辑器 : Editor
{
    private SerializedProperty 记录的最大人数;
    private SerializedProperty 第1个玩家栏位按钮;
    private SerializedProperty 第2个玩家栏位按钮;
    private SerializedProperty 第3个玩家栏位按钮;
    private SerializedProperty 第4个玩家栏位按钮;

    private void OnEnable()
    {
        记录的最大人数 = serializedObject.FindProperty("记录的最大人数");
        第1个玩家栏位按钮 = serializedObject.FindProperty("第1个玩家栏位按钮");
        第2个玩家栏位按钮 = serializedObject.FindProperty("第2个玩家栏位按钮");
        第3个玩家栏位按钮 = serializedObject.FindProperty("第3个玩家栏位按钮");
        第4个玩家栏位按钮 = serializedObject.FindProperty("第4个玩家栏位按钮");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.PropertyField(记录的最大人数, new GUIContent("记录的最大人数"));
        }

        EditorGUILayout.Space(6f);
        EditorGUILayout.PropertyField(第1个玩家栏位按钮, new GUIContent("第1个玩家栏位按钮"));
        EditorGUILayout.PropertyField(第2个玩家栏位按钮, new GUIContent("第2个玩家栏位按钮"));
        EditorGUILayout.PropertyField(第3个玩家栏位按钮, new GUIContent("第3个玩家栏位按钮"));
        EditorGUILayout.PropertyField(第4个玩家栏位按钮, new GUIContent("第4个玩家栏位按钮"));

        if (第1个玩家栏位按钮.objectReferenceValue == null ||
            第2个玩家栏位按钮.objectReferenceValue == null ||
            第3个玩家栏位按钮.objectReferenceValue == null ||
            第4个玩家栏位按钮.objectReferenceValue == null)
        {
            EditorGUILayout.HelpBox("有玩家栏位按钮没有绑定，运行时会黄字提示。", MessageType.Warning);
        }

        EditorGUILayout.HelpBox("记录的最大人数运行时从副本选择记录对应的地图模板读取，不能在这里手动修改。", MessageType.Info);

        serializedObject.ApplyModifiedProperties();
    }
}

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(格子物件触发器))]
public sealed class 格子物件触发器编辑器 : Editor
{
    private SerializedProperty 触发器名称;
    private SerializedProperty 触发格列表;
    private SerializedProperty 到达后触发;

    private void OnEnable()
    {
        触发器名称 = serializedObject.FindProperty("触发器名称");
        触发格列表 = serializedObject.FindProperty("触发格列表");
        到达后触发 = serializedObject.FindProperty("到达后触发");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(触发器名称, new GUIContent("触发器名称"));
        EditorGUILayout.PropertyField(触发格列表, new GUIContent("触发格列表"), true);
        EditorGUILayout.PropertyField(到达后触发, new GUIContent("到达后触发"));

        serializedObject.ApplyModifiedProperties();
    }
}

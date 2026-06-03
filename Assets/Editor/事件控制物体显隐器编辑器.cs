using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(事件控制物体显隐器))]
public sealed class 事件控制物体显隐器编辑器 : Editor
{
    private SerializedProperty 事件ID;
    private SerializedProperty 目标物体;

    private void OnEnable()
    {
        事件ID = serializedObject.FindProperty("事件ID");
        目标物体 = serializedObject.FindProperty("目标物体");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(事件ID, new GUIContent("事件ID"));
        EditorGUILayout.PropertyField(目标物体, new GUIContent("目标物体"));

        string resolvedId = string.IsNullOrWhiteSpace(事件ID.stringValue) ? string.Empty : 事件ID.stringValue.Trim();
        if (string.IsNullOrEmpty(resolvedId))
        {
            EditorGUILayout.HelpBox("没有填写事件ID，运行时不会控制目标物体。", MessageType.Warning);
        }
        else
        {
            EventDatabase database = EventDatabase.LoadDefault();
            if (database == null || database.FindEntry(resolvedId) == null)
            {
                EditorGUILayout.HelpBox($"事件库里找不到事件ID：{resolvedId}", MessageType.Warning);
            }
        }

        if (目标物体.objectReferenceValue == null)
        {
            EditorGUILayout.HelpBox("没有绑定目标物体，运行时不会有显隐对象。", MessageType.Warning);
        }

        serializedObject.ApplyModifiedProperties();
    }
}

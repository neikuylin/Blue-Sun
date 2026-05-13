using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(房间方向按钮))]
public sealed class 房间方向按钮编辑器 : Editor
{
    private SerializedProperty direction;

    private void OnEnable()
    {
        direction = serializedObject.FindProperty("direction");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("方向", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(direction, new GUIContent("方向"));

        serializedObject.ApplyModifiedProperties();
    }
}

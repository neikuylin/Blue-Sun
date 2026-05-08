using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(房间方向按钮))]
public sealed class 房间方向按钮编辑器 : Editor
{
    private SerializedProperty direction;
    private SerializedProperty normalStateObject;
    private SerializedProperty highlightedStateObject;
    private SerializedProperty selectedStateObject;

    private void OnEnable()
    {
        direction = serializedObject.FindProperty("direction");
        normalStateObject = serializedObject.FindProperty("normalStateObject");
        highlightedStateObject = serializedObject.FindProperty("highlightedStateObject");
        selectedStateObject = serializedObject.FindProperty("selectedStateObject");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("方向", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(direction, new GUIContent("方向"));

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("状态对象", EditorStyles.boldLabel);
        DrawSpriteRendererGameObjectField(normalStateObject, "普通状态对象");
        DrawSpriteRendererGameObjectField(highlightedStateObject, "悬浮状态对象");
        DrawSpriteRendererGameObjectField(selectedStateObject, "选中状态对象");

        serializedObject.ApplyModifiedProperties();
    }

    private static void DrawSpriteRendererGameObjectField(SerializedProperty property, string label)
    {
        EditorGUILayout.PropertyField(property, new GUIContent(label));
        GameObject value = property.objectReferenceValue as GameObject;
        if (value != null && value.GetComponent<SpriteRenderer>() == null)
        {
            EditorGUILayout.HelpBox($"{label} 必须是带有 SpriteRenderer 组件的 GameObject。", MessageType.Error);
        }
    }
}

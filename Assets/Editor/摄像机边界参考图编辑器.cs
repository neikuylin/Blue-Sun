using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(摄像机边界参考图))]
public sealed class 摄像机边界参考图编辑器 : Editor
{
    private SerializedProperty direction;
    private SerializedProperty targetSpriteObject;
    private SerializedProperty positionOffset;

    private void OnEnable()
    {
        direction = serializedObject.FindProperty("direction");
        targetSpriteObject = serializedObject.FindProperty("targetSpriteObject");
        positionOffset = serializedObject.FindProperty("positionOffset");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(direction, new GUIContent("边界方向"));
        DrawSpriteRendererGameObjectField(targetSpriteObject, "目标Sprite物体");
        EditorGUILayout.PropertyField(positionOffset, new GUIContent("位置偏移"));

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

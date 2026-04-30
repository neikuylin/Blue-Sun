using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Sprite角色遮挡挖空控制器))]
public sealed class Sprite角色遮挡挖空控制器编辑器 : Editor
{
    private SerializedProperty revealEnabled;
    private SerializedProperty radiusPixels;
    private SerializedProperty softnessPixels;
    private SerializedProperty targetCamera;

    private void OnEnable()
    {
        revealEnabled = serializedObject.FindProperty("revealEnabled");
        radiusPixels = serializedObject.FindProperty("radiusPixels");
        softnessPixels = serializedObject.FindProperty("softnessPixels");
        targetCamera = serializedObject.FindProperty("targetCamera");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("角色遮挡挖空", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(revealEnabled, new GUIContent("启用角色圆形挖空"));
        EditorGUILayout.PropertyField(radiusPixels, new GUIContent("角色周围挖空半径（像素）"));
        EditorGUILayout.PropertyField(softnessPixels, new GUIContent("挖空边缘软化（像素）", "0 是硬边；大于 0 时边缘会平滑过渡。"));

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("屏幕位置计算", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(targetCamera, new GUIContent("用于计算角色屏幕位置的相机", "为空时使用 Camera.main。"));

        serializedObject.ApplyModifiedProperties();
    }
}

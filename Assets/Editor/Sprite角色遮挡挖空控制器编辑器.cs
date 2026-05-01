using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Sprite角色遮挡挖空控制器))]
public sealed class Sprite角色遮挡挖空控制器编辑器 : Editor
{
    private SerializedProperty revealEnabled;
    private SerializedProperty radiusWorld;
    private SerializedProperty softnessWorld;
    private SerializedProperty dissolveNoiseScale;
    private SerializedProperty dissolveStrength;
    private SerializedProperty dissolveEdgeWidth;
    private SerializedProperty targetCamera;
    private SerializedProperty targetRenderers;

    private void OnEnable()
    {
        revealEnabled = serializedObject.FindProperty("revealEnabled");
        radiusWorld = serializedObject.FindProperty("radiusWorld");
        softnessWorld = serializedObject.FindProperty("softnessWorld");
        dissolveNoiseScale = serializedObject.FindProperty("dissolveNoiseScale");
        dissolveStrength = serializedObject.FindProperty("dissolveStrength");
        dissolveEdgeWidth = serializedObject.FindProperty("dissolveEdgeWidth");
        targetCamera = serializedObject.FindProperty("targetCamera");
        targetRenderers = serializedObject.FindProperty("targetRenderers");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("角色遮挡挖空", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(revealEnabled, new GUIContent("启用角色圆形挖空"));
        EditorGUILayout.PropertyField(radiusWorld, new GUIContent("角色周围挖空半径（世界单位）"));
        EditorGUILayout.PropertyField(softnessWorld, new GUIContent("挖空边缘软化（世界单位）", "0 是硬边；大于 0 时边缘会平滑过渡。"));

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("边缘颗粒", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(dissolveNoiseScale, new GUIContent("颗粒尺寸（像素）", "数值越小颗粒越密。"));
        EditorGUILayout.PropertyField(dissolveStrength, new GUIContent("颗粒强度", "0 为关闭颗粒，1 为最明显。"));
        EditorGUILayout.PropertyField(dissolveEdgeWidth, new GUIContent("颗粒边缘宽度（像素）", "颗粒影响挖空边缘的屏幕像素宽度。"));

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("屏幕位置计算", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(targetCamera, new GUIContent("用于计算角色屏幕位置的相机", "为空时使用 Camera.main。"));

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("作用目标", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(targetRenderers, new GUIContent("目标Renderer（为空时使用当前物体Renderer）"), true);

        serializedObject.ApplyModifiedProperties();
    }
}

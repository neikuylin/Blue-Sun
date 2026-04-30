using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(精灵局部光硬边控制器))]
public sealed class 精灵局部光硬边控制器编辑器 : Editor
{
    private SerializedProperty targetLight;
    private SerializedProperty hardEdge;
    private SerializedProperty colorMultiplier;
    private SerializedProperty intensityMultiplier;
    private SerializedProperty radiusMultiplier;
    private SerializedProperty positionOffset;
    private SerializedProperty threshold;
    private SerializedProperty softness;
    private SerializedProperty alignToMainCamera;
    private SerializedProperty sortingLayerName;
    private SerializedProperty sortingOrder;
    private SerializedProperty useSpotAngle;
    private SerializedProperty spotSoftness;

    private void OnEnable()
    {
        targetLight = serializedObject.FindProperty("targetLight");
        hardEdge = serializedObject.FindProperty("hardEdge");
        colorMultiplier = serializedObject.FindProperty("colorMultiplier");
        intensityMultiplier = serializedObject.FindProperty("intensityMultiplier");
        radiusMultiplier = serializedObject.FindProperty("radiusMultiplier");
        positionOffset = serializedObject.FindProperty("positionOffset");
        threshold = serializedObject.FindProperty("threshold");
        softness = serializedObject.FindProperty("softness");
        alignToMainCamera = serializedObject.FindProperty("alignToMainCamera");
        sortingLayerName = serializedObject.FindProperty("sortingLayerName");
        sortingOrder = serializedObject.FindProperty("sortingOrder");
        useSpotAngle = serializedObject.FindProperty("useSpotAngle");
        spotSoftness = serializedObject.FindProperty("spotSoftness");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("绑定光源", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(targetLight, new GUIContent("目标 Light", "只读取这盏 Light 的参数生成硬边光斑。留空时使用同物体上的 Light。"));
        EditorGUILayout.PropertyField(hardEdge, new GUIContent("启用硬边光斑"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("光斑", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(colorMultiplier, new GUIContent("颜色倍率"));
        EditorGUILayout.PropertyField(intensityMultiplier, new GUIContent("强度倍率"));
        EditorGUILayout.PropertyField(radiusMultiplier, new GUIContent("半径倍率"));
        EditorGUILayout.PropertyField(positionOffset, new GUIContent("位置偏移"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("硬边", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(threshold, new GUIContent("硬边阈值", "数值越大，亮区越小。"));
        EditorGUILayout.PropertyField(softness, new GUIContent("边缘过渡宽度", "越接近 0 越硬，略大一点可以减少锯齿。"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("投影", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(alignToMainCamera, new GUIContent("对齐主摄像机"));
        EditorGUILayout.PropertyField(sortingLayerName, new GUIContent("排序层"));
        EditorGUILayout.PropertyField(sortingOrder, new GUIContent("排序值"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("聚光灯", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(useSpotAngle, new GUIContent("使用 Spot 角度"));
        EditorGUILayout.PropertyField(spotSoftness, new GUIContent("Spot 边缘过渡"));

        serializedObject.ApplyModifiedProperties();
    }
}

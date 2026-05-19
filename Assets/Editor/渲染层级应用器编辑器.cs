using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(渲染层级应用器))]
public sealed class 渲染层级应用器编辑器 : Editor
{
    private SerializedProperty mode;
    private SerializedProperty lightingMode;
    private SerializedProperty overwriteSortingOrder;
    private SerializedProperty sortingOrder;
    private SerializedProperty replaceMaterial;
    private SerializedProperty below3DMaterial;
    private SerializedProperty above3DMaterial;
    private SerializedProperty undecidedMaterial;
    private SerializedProperty unlitBelow3DMaterial;
    private SerializedProperty unlitAbove3DMaterial;
    private SerializedProperty unlitUndecidedMaterial;
    private SerializedProperty includeInactive;
    private SerializedProperty applyOnValidate;
    private SerializedProperty applySpriteRenderers;
    private SerializedProperty applyParticleRenderers;
    private SerializedProperty applyLineRenderers;
    private SerializedProperty applyTrailRenderers;
    private SerializedProperty applyMeshRenderers;

    private void OnEnable()
    {
        mode = serializedObject.FindProperty("mode");
        lightingMode = serializedObject.FindProperty("lightingMode");
        overwriteSortingOrder = serializedObject.FindProperty("overwriteSortingOrder");
        sortingOrder = serializedObject.FindProperty("sortingOrder");
        replaceMaterial = serializedObject.FindProperty("replaceMaterial");
        below3DMaterial = serializedObject.FindProperty("below3DMaterial");
        above3DMaterial = serializedObject.FindProperty("above3DMaterial");
        undecidedMaterial = serializedObject.FindProperty("undecidedMaterial");
        unlitBelow3DMaterial = serializedObject.FindProperty("unlitBelow3DMaterial");
        unlitAbove3DMaterial = serializedObject.FindProperty("unlitAbove3DMaterial");
        unlitUndecidedMaterial = serializedObject.FindProperty("unlitUndecidedMaterial");
        includeInactive = serializedObject.FindProperty("includeInactive");
        applyOnValidate = serializedObject.FindProperty("applyOnValidate");
        applySpriteRenderers = serializedObject.FindProperty("applySpriteRenderers");
        applyParticleRenderers = serializedObject.FindProperty("applyParticleRenderers");
        applyLineRenderers = serializedObject.FindProperty("applyLineRenderers");
        applyTrailRenderers = serializedObject.FindProperty("applyTrailRenderers");
        applyMeshRenderers = serializedObject.FindProperty("applyMeshRenderers");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("层级", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(mode, new GUIContent("渲染模式", "低于3D适合地板/背景；高于3D适合高亮/提示/部分特效；不决定按正常深度关系处理遮挡。"));
        EditorGUILayout.PropertyField(lightingMode, new GUIContent("光照模式", "受光会响应主光和局部光；不受光保持贴图原本亮度。"));
        EditorGUILayout.PropertyField(overwriteSortingOrder, new GUIContent("覆盖排序值", "关闭后不修改 SpriteRenderer 自身的排序值。"));
        EditorGUILayout.PropertyField(sortingOrder, new GUIContent("2D排序值", "同类2D渲染之间的前后顺序。数值越大越靠前。"));

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("材质", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(replaceMaterial, new GUIContent("替换材质", "关闭时只应用排序和目标筛选，不修改 Renderer 材质。"));
        EditorGUILayout.PropertyField(below3DMaterial, new GUIContent("低于3D材质", "为空会自动从 Resources 加载。"));
        EditorGUILayout.PropertyField(above3DMaterial, new GUIContent("高于3D材质", "为空会自动从 Resources 加载。"));
        EditorGUILayout.PropertyField(undecidedMaterial, new GUIContent("不决定材质", "为空会自动从 Resources 加载。"));
        EditorGUILayout.PropertyField(unlitBelow3DMaterial, new GUIContent("低于3D不受光材质", "为空会自动从 Resources 加载。"));
        EditorGUILayout.PropertyField(unlitAbove3DMaterial, new GUIContent("高于3D不受光材质", "为空会自动从 Resources 加载。"));
        EditorGUILayout.PropertyField(unlitUndecidedMaterial, new GUIContent("不决定不受光材质", "为空会自动从 Resources 加载。"));

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("目标", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(includeInactive, new GUIContent("包含未激活子物体"));
        EditorGUILayout.PropertyField(applyOnValidate, new GUIContent("修改后自动应用"));
        EditorGUILayout.PropertyField(applySpriteRenderers, new GUIContent("应用到 SpriteRenderer"));
        EditorGUILayout.PropertyField(applyParticleRenderers, new GUIContent("应用到粒子渲染器", "会替换粒子材质，可能改变特效外观。"));
        EditorGUILayout.PropertyField(applyLineRenderers, new GUIContent("应用到线条渲染器"));
        EditorGUILayout.PropertyField(applyTrailRenderers, new GUIContent("应用到拖尾渲染器"));
        EditorGUILayout.PropertyField(applyMeshRenderers, new GUIContent("应用到网格渲染器", "会替换网格材质，谨慎使用。"));

        EditorGUILayout.Space(8f);
        if (GUILayout.Button("应用渲染层级"))
        {
            ApplyToTargets();
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void ApplyToTargets()
    {
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] is 渲染层级应用器 applier)
            {
                applier.Apply();
                EditorUtility.SetDirty(applier);
            }
        }
    }
}

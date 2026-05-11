using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(墨血换房转场控制器))]
public sealed class 墨血换房转场控制器编辑器 : Editor
{
    private SerializedProperty transitionImage;
    private SerializedProperty transitionMaterial;
    private SerializedProperty curtainColor;
    private SerializedProperty coverDuration;
    private SerializedProperty revealDuration;
    private float previewProgress = 0.5f;

    private void OnEnable()
    {
        transitionImage = serializedObject.FindProperty("transitionImage");
        transitionMaterial = serializedObject.FindProperty("transitionMaterial");
        curtainColor = serializedObject.FindProperty("curtainColor");
        coverDuration = serializedObject.FindProperty("coverDuration");
        revealDuration = serializedObject.FindProperty("revealDuration");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("引用", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(transitionImage, new GUIContent("全屏遮罩图片"));
        EditorGUILayout.PropertyField(transitionMaterial, new GUIContent("转场材质"));

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("颜色", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(curtainColor, new GUIContent("幕布颜色"));

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("时间", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(coverDuration, new GUIContent("盖屏时间"));
        EditorGUILayout.PropertyField(revealDuration, new GUIContent("揭开时间"));

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("编辑器预览", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        previewProgress = EditorGUILayout.Slider(new GUIContent("预览覆盖进度"), previewProgress, 0f, 1f);
        bool previewChanged = EditorGUI.EndChangeCheck();

        serializedObject.ApplyModifiedProperties();

        if (previewChanged || GUILayout.Button("应用预览"))
        {
            ApplyPreview();
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("预览全透明"))
        {
            SetPreviewProgress(0f);
        }

        if (GUILayout.Button("预览半覆盖"))
        {
            SetPreviewProgress(0.5f);
        }

        if (GUILayout.Button("预览全覆盖"))
        {
            SetPreviewProgress(1f);
        }
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("关闭预览"))
        {
            ClosePreview();
        }
    }

    private void ApplyPreview()
    {
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] is 墨血换房转场控制器 controller)
            {
                controller.编辑器设置预览进度(previewProgress);
                EditorUtility.SetDirty(controller);
            }
        }
    }

    private void SetPreviewProgress(float progress)
    {
        serializedObject.Update();
        previewProgress = progress;
        serializedObject.ApplyModifiedProperties();
        ApplyPreview();
    }

    private void ClosePreview()
    {
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] is 墨血换房转场控制器 controller)
            {
                controller.编辑器关闭预览();
                EditorUtility.SetDirty(controller);
            }
        }
    }
}

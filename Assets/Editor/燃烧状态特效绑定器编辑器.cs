using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(燃烧状态特效绑定器))]
public sealed class 燃烧状态特效绑定器编辑器 : Editor
{
    private const string SettingsAssetPath = "Assets/Resources/燃烧状态特效全局配置.asset";

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("燃烧状态特效", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("直接挂到角色根物体。脚本会读取当前物体和子物体里的 SkinnedMeshRenderer，并从角色模型表面发射小而散的火焰粒子。参数统一读取 Resources/燃烧状态特效全局配置。", MessageType.Info);

        燃烧状态特效全局配置 settings = AssetDatabase.LoadAssetAtPath<燃烧状态特效全局配置>(SettingsAssetPath);
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.ObjectField("全局配置", settings, typeof(燃烧状态特效全局配置), false);
        }

        if (settings == null)
        {
            EditorGUILayout.HelpBox("缺少全局配置：Assets/Resources/燃烧状态特效全局配置.asset", MessageType.Warning);
        }
        else if (GUILayout.Button("选中全局配置", GUILayout.Width(120f)))
        {
            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);
        }

        serializedObject.ApplyModifiedProperties();
    }
}

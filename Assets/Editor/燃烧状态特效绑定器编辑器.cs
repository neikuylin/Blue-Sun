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
        else
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("全局参数", EditorStyles.boldLabel);

            SerializedObject settingsObject = new SerializedObject(settings);
            settingsObject.Update();

            EditorGUI.BeginChangeCheck();
            SerializedProperty property = settingsObject.GetIterator();
            bool enterChildren = true;
            while (property.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (property.propertyPath == "m_Script")
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.PropertyField(property, true);
                    }
                }
                else
                {
                    EditorGUILayout.PropertyField(property, true);
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                settingsObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(settings);
                刷新选中目标();
            }
            else
            {
                settingsObject.ApplyModifiedProperties();
            }

            EditorGUILayout.Space(4f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("刷新预览", GUILayout.Width(120f)))
                {
                    刷新选中目标();
                }

                if (GUILayout.Button("选中全局配置", GUILayout.Width(120f)))
                {
                    Selection.activeObject = settings;
                    EditorGUIUtility.PingObject(settings);
                }
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void 刷新选中目标()
    {
        for (int i = 0; i < targets.Length; i++)
        {
            燃烧状态特效绑定器 binder = targets[i] as 燃烧状态特效绑定器;
            if (binder == null)
            {
                continue;
            }

            binder.刷新特效预览();
            EditorUtility.SetDirty(binder);
        }
    }
}

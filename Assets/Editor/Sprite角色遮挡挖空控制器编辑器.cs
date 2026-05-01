using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Sprite角色遮挡挖空控制器))]
public sealed class Sprite角色遮挡挖空控制器编辑器 : Editor
{
    private const string SettingsAssetPath = "Assets/Resources/SpriteOcclusionRevealSettings.asset";

    private SpriteOcclusionRevealSettings settings;
    private SerializedObject settingsObject;
    private SerializedProperty sharedRevealEnabled;
    private SerializedProperty sharedRadiusWorld;
    private SerializedProperty sharedSoftnessWorld;
    private SerializedProperty sharedDissolveNoiseScale;
    private SerializedProperty sharedDissolveStrength;
    private SerializedProperty sharedDissolveEdgeWidth;
    private SerializedProperty sharedDissolveScrollSpeed;
    private SerializedProperty sharedDissolveSmoothEdges;
    private SerializedProperty targetRenderers;

    private void OnEnable()
    {
        settings = AssetDatabase.LoadAssetAtPath<SpriteOcclusionRevealSettings>(SettingsAssetPath);
        if (settings != null)
        {
            settingsObject = new SerializedObject(settings);
            sharedRevealEnabled = settingsObject.FindProperty("revealEnabled");
            sharedRadiusWorld = settingsObject.FindProperty("radiusWorld");
            sharedSoftnessWorld = settingsObject.FindProperty("softnessWorld");
            sharedDissolveNoiseScale = settingsObject.FindProperty("dissolveNoiseScale");
            sharedDissolveStrength = settingsObject.FindProperty("dissolveStrength");
            sharedDissolveEdgeWidth = settingsObject.FindProperty("dissolveEdgeWidth");
            sharedDissolveScrollSpeed = settingsObject.FindProperty("dissolveScrollSpeed");
            sharedDissolveSmoothEdges = settingsObject.FindProperty("dissolveSmoothEdges");
        }

        targetRenderers = serializedObject.FindProperty("targetRenderers");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawSharedSettings();

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("作用目标", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(targetRenderers, new GUIContent("目标Renderer（为空时使用当前物体Renderer）"), true);

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawSharedSettings()
    {
        if (settingsObject == null)
        {
            EditorGUILayout.HelpBox($"缺少共享配置：{SettingsAssetPath}", MessageType.Error);
            return;
        }

        settingsObject.Update();

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.LabelField("角色遮挡挖空（全项目统一）", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(sharedRevealEnabled, new GUIContent("启用角色圆形挖空"));
        EditorGUILayout.PropertyField(sharedRadiusWorld, new GUIContent("角色周围挖空半径（世界单位）"));
        EditorGUILayout.PropertyField(sharedSoftnessWorld, new GUIContent("挖空边缘软化（世界单位）", "0 是硬边；大于 0 时边缘会平滑过渡。"));

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("边缘颗粒（全项目统一）", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(sharedDissolveNoiseScale, new GUIContent("颗粒尺寸（像素）", "数值越小颗粒越密。"));
        EditorGUILayout.PropertyField(sharedDissolveStrength, new GUIContent("颗粒强度", "0 为关闭颗粒，1 为最明显。"));
        EditorGUILayout.PropertyField(sharedDissolveEdgeWidth, new GUIContent("颗粒边缘宽度（像素）", "颗粒影响挖空边缘的屏幕像素宽度。"));
        EditorGUILayout.PropertyField(sharedDissolveScrollSpeed, new GUIContent("颗粒滚动速度（像素/秒）", "正负值控制上下滚动方向。"));
        EditorGUILayout.PropertyField(sharedDissolveSmoothEdges, new GUIContent("颗粒边缘融合", "勾选时使用 smoothstep 软融合，取消勾选时使用硬边。"));

        if (!EditorGUI.EndChangeCheck())
        {
            return;
        }

        settingsObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(settings);
        RefreshLoadedControllers();
    }

    private static void RefreshLoadedControllers()
    {
        Sprite角色遮挡挖空控制器[] controllers = Resources.FindObjectsOfTypeAll<Sprite角色遮挡挖空控制器>();
        for (int i = 0; i < controllers.Length; i++)
        {
            Sprite角色遮挡挖空控制器 controller = controllers[i];
            if (controller == null || controller.gameObject == null || !controller.gameObject.scene.IsValid())
            {
                continue;
            }

            controller.Apply();
        }
    }
}

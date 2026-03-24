using UnityEditor;
using UnityEngine;

public sealed class BattleMusicEditorWindow : EditorWindow
{
    private const string AssetFolder = "Assets/Resources";
    private const string AssetPath = AssetFolder + "/BattleMusicSettings.asset";

    private SerializedObject settingsObject;

    [MenuItem("Tools/音乐/音乐编辑器")]
    private static void Open()
    {
        BattleMusicEditorWindow window = GetWindow<BattleMusicEditorWindow>("音乐编辑器");
        window.minSize = new Vector2(420f, 220f);
        window.Show();
        window.Focus();
    }

    private void OnGUI()
    {
        BattleMusicSettings settings = EnsureSettings();
        if (settings == null)
        {
            EditorGUILayout.HelpBox("未找到 BattleMusicSettings.asset。", MessageType.Error);
            return;
        }

        if (settingsObject == null || settingsObject.targetObject != settings)
        {
            settingsObject = new SerializedObject(settings);
        }

        settingsObject.Update();

        EditorGUILayout.LabelField("战斗模式音乐", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("这里配置战斗模式和探索模式共用的场景 BGM。", MessageType.Info);
        EditorGUILayout.Space(6f);
        EditorGUILayout.PropertyField(settingsObject.FindProperty("combatMusic"), new GUIContent("战斗音乐"));
        EditorGUILayout.PropertyField(settingsObject.FindProperty("explorationMusic"), new GUIContent("探索音乐"));
        EditorGUILayout.Slider(settingsObject.FindProperty("volume"), 0f, 1f, new GUIContent("音乐音量"));

        if (settingsObject.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }
    }

    private static BattleMusicSettings EnsureSettings()
    {
        BattleMusicSettings settings = AssetDatabase.LoadAssetAtPath<BattleMusicSettings>(AssetPath);
        if (settings != null)
        {
            return settings;
        }

        if (!AssetDatabase.IsValidFolder(AssetFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        settings = CreateInstance<BattleMusicSettings>();
        AssetDatabase.CreateAsset(settings, AssetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return settings;
    }
}

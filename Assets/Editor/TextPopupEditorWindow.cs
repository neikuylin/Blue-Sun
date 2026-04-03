using UnityEditor;
using UnityEngine;

public sealed class TextPopupEditorWindow : EditorWindow
{
    private const string AssetFolder = "Assets/Resources";
    private const string AssetPath = AssetFolder + "/TextPopupDatabase.asset";

    private SerializedObject databaseObject;

    [MenuItem("Tools/文本/文本弹窗")]
    private static void Open()
    {
        TextPopupEditorWindow window = GetWindow<TextPopupEditorWindow>("文本弹窗");
        window.minSize = new Vector2(520f, 240f);
        window.Show();
        window.Focus();
    }

    private void OnGUI()
    {
        TextPopupDatabase database = EnsureDatabase();
        if (database == null)
        {
            EditorGUILayout.HelpBox("文本弹窗库资源创建失败。", MessageType.Error);
            return;
        }

        if (databaseObject == null || databaseObject.targetObject != database)
        {
            databaseObject = new SerializedObject(database);
        }

        databaseObject.Update();

        EditorGUILayout.LabelField("文本弹窗配置", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("这里统一绑定伤害、MISS、效果的文本弹窗 GameObject。需要使用 Prefab 资源，不能拖场景实例。", MessageType.Info);
        EditorGUILayout.Space(6f);

        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.PropertyField(databaseObject.FindProperty("damagePopupTextObject"), new GUIContent("伤害弹字GameObject"));
            EditorGUILayout.PropertyField(databaseObject.FindProperty("missPopupTextObject"), new GUIContent("MISS弹字GameObject"));
            EditorGUILayout.PropertyField(databaseObject.FindProperty("effectPopupTextObject"), new GUIContent("效果弹字GameObject"));
        }

        if (databaseObject.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
        }
    }

    private static TextPopupDatabase EnsureDatabase()
    {
        TextPopupDatabase database = AssetDatabase.LoadAssetAtPath<TextPopupDatabase>(AssetPath);
        if (database != null)
        {
            return database;
        }

        if (!AssetDatabase.IsValidFolder(AssetFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        database = CreateInstance<TextPopupDatabase>();
        AssetDatabase.CreateAsset(database, AssetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return database;
    }
}

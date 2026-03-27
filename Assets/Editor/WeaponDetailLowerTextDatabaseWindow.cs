using UnityEditor;
using UnityEngine;

public sealed class WeaponDetailLowerTextDatabaseWindow : EditorWindow
{
    private const string AssetPath = "Assets/Resources/WeaponDetailLowerTextDatabase.asset";

    private WeaponDetailLowerTextDatabase database;

    [MenuItem("Tools/物品/武器详细下文本")]
    private static void Open()
    {
        WeaponDetailLowerTextDatabaseWindow window = GetWindow<WeaponDetailLowerTextDatabaseWindow>();
        window.titleContent = new GUIContent("武器详细下文本");
        window.minSize = new Vector2(420f, 180f);
        window.Show();
    }

    private void OnEnable()
    {
        database = LoadOrCreateDatabase();
    }

    private void OnGUI()
    {
        database = database != null ? database : LoadOrCreateDatabase();
        if (database == null)
        {
            EditorGUILayout.HelpBox("武器详细下文本配置加载失败。", MessageType.Error);
            return;
        }

        EditorGUILayout.LabelField("武器详细下文本", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("使用 x 作为数值占位符。不写 x 时，会把数值直接追加到文本末尾。", MessageType.Info);
        EditorGUILayout.Space(6f);

        database.criticalChanceFormat = EditorGUILayout.TextField("暴击率格式", database.criticalChanceFormat ?? string.Empty);
        database.criticalDamageFormat = EditorGUILayout.TextField("暴击伤害格式", database.criticalDamageFormat ?? string.Empty);

        EditorGUILayout.Space(10f);
        if (GUILayout.Button("保存"))
        {
            SaveDatabase();
        }
    }

    private static WeaponDetailLowerTextDatabase LoadOrCreateDatabase()
    {
        WeaponDetailLowerTextDatabase asset = AssetDatabase.LoadAssetAtPath<WeaponDetailLowerTextDatabase>(AssetPath);
        if (asset != null)
        {
            return asset;
        }

        asset = CreateInstance<WeaponDetailLowerTextDatabase>();
        AssetDatabase.CreateAsset(asset, AssetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return asset;
    }

    private void SaveDatabase()
    {
        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}

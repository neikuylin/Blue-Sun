using UnityEditor;
using UnityEngine;

public sealed class ItemQualityBackgroundDatabaseWindow : EditorWindow
{
    private const string AssetPath = "Assets/Resources/ItemQualityBackgroundDatabase.asset";

    private ItemQualityBackgroundDatabase database;

    [MenuItem("Tools/物品/物品品质图库")]
    private static void Open()
    {
        ItemQualityBackgroundDatabaseWindow window = GetWindow<ItemQualityBackgroundDatabaseWindow>();
        window.titleContent = new GUIContent("物品品质图库");
        window.minSize = new Vector2(460f, 280f);
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
            EditorGUILayout.HelpBox("物品品质底图数据库加载失败。", MessageType.Error);
            return;
        }

        EditorGUILayout.LabelField("跨场景物品品质底图", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("旅程和战斗界面都会优先读取这套配置。现在每个品质支持 1x1 和 1x2 两种底图预制体。", MessageType.Info);
        EditorGUILayout.Space(6f);

        DrawEntry("普通（白）", database.Common);
        DrawEntry("优秀（蓝）", database.Excellent);
        DrawEntry("史诗（紫）", database.Epic);
        DrawEntry("赐福（金）", database.Blessed);

        EditorGUILayout.Space(10f);
        if (GUILayout.Button("保存"))
        {
            SaveDatabase();
        }
    }

    private static void DrawEntry(string label, ItemQualityBackgroundDatabase.QualityBackgroundEntry entry)
    {
        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            entry.oneByOnePrefab = (GameObject)EditorGUILayout.ObjectField("1x1 底图预制体", entry.oneByOnePrefab, typeof(GameObject), false);
            entry.oneByTwoPrefab = (GameObject)EditorGUILayout.ObjectField("1x2 底图预制体", entry.oneByTwoPrefab, typeof(GameObject), false);
        }
    }

    private static ItemQualityBackgroundDatabase LoadOrCreateDatabase()
    {
        ItemQualityBackgroundDatabase asset = AssetDatabase.LoadAssetAtPath<ItemQualityBackgroundDatabase>(AssetPath);
        if (asset != null)
        {
            return asset;
        }

        asset = CreateInstance<ItemQualityBackgroundDatabase>();
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

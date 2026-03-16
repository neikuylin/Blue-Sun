using UnityEditor;
using UnityEngine;

public sealed class ItemTooltipPrefabDatabaseWindow : EditorWindow
{
    private const string AssetPath = "Assets/Resources/ItemTooltipPrefabDatabase.asset";

    private ItemTooltipPrefabDatabase database;

    [MenuItem("Tools/物品/物品详情预制体库")]
    private static void Open()
    {
        ItemTooltipPrefabDatabaseWindow window = GetWindow<ItemTooltipPrefabDatabaseWindow>();
        window.titleContent = new GUIContent("物品详情预制体库");
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
            EditorGUILayout.HelpBox("物品详情预制体数据库加载失败。", MessageType.Error);
            return;
        }

        EditorGUILayout.LabelField("跨场景物品详情预制体", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("单双手武器详情和四种品质底图都从这里读取。", MessageType.Info);
        EditorGUILayout.Space(6f);

        database.oneHandedTwoHandedTooltipPrefab = (GameObject)EditorGUILayout.ObjectField("单双手武器详情预制体", database.oneHandedTwoHandedTooltipPrefab, typeof(GameObject), false);

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("品质底图", EditorStyles.boldLabel);
        database.commonBackgroundPrefab = (GameObject)EditorGUILayout.ObjectField("普通（白）底图", database.commonBackgroundPrefab, typeof(GameObject), false);
        database.excellentBackgroundPrefab = (GameObject)EditorGUILayout.ObjectField("优秀（蓝）底图", database.excellentBackgroundPrefab, typeof(GameObject), false);
        database.epicBackgroundPrefab = (GameObject)EditorGUILayout.ObjectField("史诗（紫）底图", database.epicBackgroundPrefab, typeof(GameObject), false);
        database.blessedBackgroundPrefab = (GameObject)EditorGUILayout.ObjectField("赐福（金）底图", database.blessedBackgroundPrefab, typeof(GameObject), false);

        EditorGUILayout.Space(10f);
        if (GUILayout.Button("保存"))
        {
            SaveDatabase();
        }
    }

    private static ItemTooltipPrefabDatabase LoadOrCreateDatabase()
    {
        ItemTooltipPrefabDatabase asset = AssetDatabase.LoadAssetAtPath<ItemTooltipPrefabDatabase>(AssetPath);
        if (asset != null)
        {
            return asset;
        }

        asset = CreateInstance<ItemTooltipPrefabDatabase>();
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

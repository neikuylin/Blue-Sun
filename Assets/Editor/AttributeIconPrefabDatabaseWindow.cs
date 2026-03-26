using UnityEditor;
using UnityEngine;

public sealed class AttributeIconPrefabDatabaseWindow : EditorWindow
{
    private const string AssetPath = "Assets/Resources/AttributeIconPrefabDatabase.asset";

    private static readonly string[] DefaultAttributeIds =
    {
        "物理",
        "火焰",
        "腐败",
        "寒冷"
    };

    private AttributeIconPrefabDatabase database;
    private Vector2 scrollPosition;

    [MenuItem("Tools/角色属性/属性图标绑定")]
    private static void Open()
    {
        AttributeIconPrefabDatabaseWindow window = GetWindow<AttributeIconPrefabDatabaseWindow>();
        window.titleContent = new GUIContent("属性图标绑定");
        window.minSize = new Vector2(460f, 420f);
        window.Show();
    }

    private void OnEnable()
    {
        database = LoadOrCreateDatabase();
        EnsureDefaultEntries(database);
    }

    private void OnGUI()
    {
        database = database != null ? database : LoadOrCreateDatabase();
        if (database == null)
        {
            EditorGUILayout.HelpBox("属性图标预制体库加载失败。", MessageType.Error);
            return;
        }

        EnsureDefaultEntries(database);

        EditorGUILayout.LabelField("属性图标 Prefab 绑定", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("这里只做属性 ID 到 prefab 的绑定，不接任何运行时功能。", MessageType.Info);
        EditorGUILayout.Space(6f);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        for (int i = 0; i < database.Entries.Count; i++)
        {
            AttributeIconPrefabDatabase.Entry entry = database.Entries[i];
            if (entry == null)
            {
                continue;
            }

            EditorGUILayout.BeginVertical("box");
            entry.attributeId = EditorGUILayout.TextField("属性 ID", entry.attributeId);
            entry.prefab = (GameObject)EditorGUILayout.ObjectField("Prefab", entry.prefab, typeof(GameObject), false);
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(8f);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("新增条目"))
        {
            database.Entries.Add(new AttributeIconPrefabDatabase.Entry());
            EditorUtility.SetDirty(database);
        }

        if (GUILayout.Button("保存"))
        {
            SaveDatabase();
        }
        EditorGUILayout.EndHorizontal();
    }

    private static AttributeIconPrefabDatabase LoadOrCreateDatabase()
    {
        AttributeIconPrefabDatabase asset = AssetDatabase.LoadAssetAtPath<AttributeIconPrefabDatabase>(AssetPath);
        if (asset != null)
        {
            return asset;
        }

        asset = CreateInstance<AttributeIconPrefabDatabase>();
        EnsureDefaultEntries(asset);
        AssetDatabase.CreateAsset(asset, AssetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return asset;
    }

    private static void EnsureDefaultEntries(AttributeIconPrefabDatabase asset)
    {
        if (asset == null)
        {
            return;
        }

        for (int i = 0; i < DefaultAttributeIds.Length; i++)
        {
            string attributeId = DefaultAttributeIds[i];
            if (asset.FindEntry(attributeId) != null)
            {
                continue;
            }

            asset.Entries.Add(new AttributeIconPrefabDatabase.Entry
            {
                attributeId = attributeId
            });
        }
    }

    private void SaveDatabase()
    {
        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}

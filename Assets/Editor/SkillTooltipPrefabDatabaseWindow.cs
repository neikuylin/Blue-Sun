using UnityEditor;
using UnityEngine;

public sealed class SkillTooltipPrefabDatabaseWindow : EditorWindow
{
    private const string AssetPath = "Assets/Resources/CombatArtTooltipPrefabDatabase.asset";

    private SkillTooltipPrefabDatabase database;

    [MenuItem("Tools/技能/战技内容预制体库")]
    private static void Open()
    {
        SkillTooltipPrefabDatabaseWindow window = GetWindow<SkillTooltipPrefabDatabaseWindow>();
        window.titleContent = new GUIContent("战技内容预制体库");
        window.minSize = new Vector2(420f, 160f);
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
            EditorGUILayout.HelpBox("技能详情预制体数据库加载失败。", MessageType.Error);
            return;
        }

        EditorGUILayout.LabelField("跨场景战技内容预制体", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("当前只服务战技。法术后面单独做，不混在这里。", MessageType.Info);
        EditorGUILayout.Space(6f);
        database.combatArtTooltipPrefab = (GameObject)EditorGUILayout.ObjectField("战技内容预制体", database.combatArtTooltipPrefab, typeof(GameObject), false);

        EditorGUILayout.Space(10f);
        if (GUILayout.Button("保存"))
        {
            SaveDatabase();
        }
    }

    private static SkillTooltipPrefabDatabase LoadOrCreateDatabase()
    {
        SkillTooltipPrefabDatabase asset = AssetDatabase.LoadAssetAtPath<SkillTooltipPrefabDatabase>(AssetPath);
        if (asset != null)
        {
            return asset;
        }

        asset = CreateInstance<SkillTooltipPrefabDatabase>();
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

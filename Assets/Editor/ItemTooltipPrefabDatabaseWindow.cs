using System;
using UnityEditor;
using UnityEngine;

public sealed class ItemTooltipPrefabDatabaseWindow : EditorWindow
{
    private const string AssetPath = "Assets/Resources/ItemTooltipPrefabDatabase.asset";

    private ItemTooltipPrefabDatabase database;

    [MenuItem("Tools/\u7269\u54c1/\u7269\u54c1\u8be6\u60c5\u9884\u5236\u4f53\u5e93")]
    private static void Open()
    {
        ItemTooltipPrefabDatabaseWindow window = GetWindow<ItemTooltipPrefabDatabaseWindow>();
        window.titleContent = new GUIContent("\u7269\u54c1\u8be6\u60c5\u9884\u5236\u4f53\u5e93");
        window.minSize = new Vector2(420f, 180f);
        window.Show();
    }

    private void OnEnable()
    {
        database = LoadOrCreateDatabase();
        EnsureDatabaseEntries();
    }

    private void OnGUI()
    {
        database = database != null ? database : LoadOrCreateDatabase();
        EnsureDatabaseEntries();
        if (database == null)
        {
            EditorGUILayout.HelpBox("\u7269\u54c1\u8be6\u60c5\u9884\u5236\u4f53\u6570\u636e\u5e93\u52a0\u8f7d\u5931\u8d25\u3002", MessageType.Error);
            return;
        }

        EditorGUILayout.LabelField("\u8de8\u573a\u666f\u7269\u54c1\u8be6\u60c5\u9884\u5236\u4f53", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "\u6bcf\u79cd\u6b66\u5668\u5206\u7c7b\u90fd\u53ef\u4ee5\u5355\u72ec\u914d\u7f6e\u5bf9\u5e94\u7684\u7269\u54c1\u8be6\u60c5\u9884\u5236\u4f53\u3002\u65b0\u589e\u6b66\u5668\u5206\u7c7b\u540e\uff0c\u8fd9\u91cc\u4f1a\u81ea\u52a8\u589e\u52a0\u4e00\u6761\u8bbe\u7f6e\u9879\u3002",
            MessageType.Info);
        EditorGUILayout.Space(6f);

        Array categories = Enum.GetValues(typeof(ItemDatabase.WeaponCategory));
        for (int i = 0; i < categories.Length; i++)
        {
            ItemDatabase.WeaponCategory category = (ItemDatabase.WeaponCategory)categories.GetValue(i);
            if (category == ItemDatabase.WeaponCategory.None)
            {
                continue;
            }

            GameObject currentPrefab = database.GetWeaponTooltipPrefab(category);
            GameObject nextPrefab = (GameObject)EditorGUILayout.ObjectField(
                GetWeaponTooltipLabel(category),
                currentPrefab,
                typeof(GameObject),
                false);
            if (nextPrefab != currentPrefab)
            {
                database.SetWeaponTooltipPrefab(category, nextPrefab);
                EditorUtility.SetDirty(database);
            }
        }

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("\u54c1\u8d28\u5e95\u56fe", EditorStyles.boldLabel);
        database.commonBackgroundPrefab = (GameObject)EditorGUILayout.ObjectField("\u666e\u901a\uff08\u767d\uff09\u5e95\u56fe", database.commonBackgroundPrefab, typeof(GameObject), false);
        database.excellentBackgroundPrefab = (GameObject)EditorGUILayout.ObjectField("\u4f18\u79c0\uff08\u84dd\uff09\u5e95\u56fe", database.excellentBackgroundPrefab, typeof(GameObject), false);
        database.epicBackgroundPrefab = (GameObject)EditorGUILayout.ObjectField("\u53f2\u8bd7\uff08\u7d2b\uff09\u5e95\u56fe", database.epicBackgroundPrefab, typeof(GameObject), false);
        database.blessedBackgroundPrefab = (GameObject)EditorGUILayout.ObjectField("\u8d50\u798f\uff08\u91d1\uff09\u5e95\u56fe", database.blessedBackgroundPrefab, typeof(GameObject), false);

        EditorGUILayout.Space(10f);
        if (GUILayout.Button("\u4fdd\u5b58"))
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

    private void EnsureDatabaseEntries()
    {
        if (database == null || !database.EnsureWeaponTooltipEntries())
        {
            return;
        }

        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();
    }

    private static string GetWeaponTooltipLabel(ItemDatabase.WeaponCategory category)
    {
        switch (category)
        {
            case ItemDatabase.WeaponCategory.OneHanded:
                return "\u5355\u624b\u6b66\u5668\u8be6\u60c5\u9884\u5236\u4f53";
            case ItemDatabase.WeaponCategory.TwoHanded:
                return "\u53cc\u624b\u6b66\u5668\u8be6\u60c5\u9884\u5236\u4f53";
            case ItemDatabase.WeaponCategory.Bow:
                return "\u5f13\u7bad\u8be6\u60c5\u9884\u5236\u4f53";
            default:
                return $"{ObjectNames.NicifyVariableName(category.ToString())}\u8be6\u60c5\u9884\u5236\u4f53";
        }
    }
}

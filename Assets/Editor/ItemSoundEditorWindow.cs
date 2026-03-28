using UnityEditor;
using UnityEngine;

public sealed class ItemSoundEditorWindow : EditorWindow
{
    private const string AssetFolder = "Assets/Resources";
    private const string AssetPath = AssetFolder + "/ItemSoundDatabase.asset";

    private Vector2 scroll;
    private SerializedObject databaseObject;

    [MenuItem("Tools/音效/物品")]
    private static void Open()
    {
        ItemSoundEditorWindow window = GetWindow<ItemSoundEditorWindow>("物品音效");
        window.minSize = new Vector2(460f, 320f);
        window.Show();
        window.Focus();
    }

    private void OnGUI()
    {
        ItemSoundDatabase database = EnsureDatabase();
        if (database == null)
        {
            EditorGUILayout.HelpBox("物品音效库创建失败。", MessageType.Error);
            return;
        }

        if (databaseObject == null || databaseObject.targetObject != database)
        {
            databaseObject = new SerializedObject(database);
        }

        EnsureAllCategories(database);
        databaseObject.Update();

        EditorGUILayout.LabelField("物品音效编辑器", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("为装备、消耗品、材料、补给分别配置交互音效。拖动、放置、装备时会按物品类别播放。", MessageType.Info);
        EditorGUILayout.Space(6f);

        scroll = EditorGUILayout.BeginScrollView(scroll);
        SerializedProperty entriesProperty = databaseObject.FindProperty("entries");
        for (int i = 0; i < entriesProperty.arraySize; i++)
        {
            DrawEntry(entriesProperty.GetArrayElementAtIndex(i));
        }

        EditorGUILayout.Space(6f);
        DrawSkillMoveEntry(databaseObject);
        EditorGUILayout.EndScrollView();

        if (databaseObject.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
        }
    }

    private static void DrawEntry(SerializedProperty entryProperty)
    {
        if (entryProperty == null)
        {
            return;
        }

        SerializedProperty categoryProperty = entryProperty.FindPropertyRelative("category");
        SerializedProperty clipProperty = entryProperty.FindPropertyRelative("clip");
        SerializedProperty volumeProperty = entryProperty.FindPropertyRelative("volume");

        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(categoryProperty, new GUIContent("物品类别"));
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.PropertyField(clipProperty, new GUIContent("音效"));
            EditorGUILayout.Slider(volumeProperty, 0f, 1f, new GUIContent("音量"));
        }
    }

    private static void DrawSkillMoveEntry(SerializedObject databaseObject)
    {
        if (databaseObject == null)
        {
            return;
        }

        SerializedProperty clipProperty = databaseObject.FindProperty("skillMoveClip");
        SerializedProperty volumeProperty = databaseObject.FindProperty("skillMoveVolume");
        if (clipProperty == null || volumeProperty == null)
        {
            return;
        }

        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("技能位移音效", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(clipProperty, new GUIContent("音效"));
            EditorGUILayout.Slider(volumeProperty, 0f, 1f, new GUIContent("音量"));
        }
    }

    private static ItemSoundDatabase EnsureDatabase()
    {
        ItemSoundDatabase database = AssetDatabase.LoadAssetAtPath<ItemSoundDatabase>(AssetPath);
        if (database != null)
        {
            return database;
        }

        if (!AssetDatabase.IsValidFolder(AssetFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        database = CreateInstance<ItemSoundDatabase>();
        AssetDatabase.CreateAsset(database, AssetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EnsureAllCategories(database);
        return database;
    }

    private static void EnsureAllCategories(ItemSoundDatabase database)
    {
        if (database == null)
        {
            return;
        }

        bool changed = false;
        foreach (ItemDatabase.ItemCategory category in System.Enum.GetValues(typeof(ItemDatabase.ItemCategory)))
        {
            if (database.FindEntry(category) != null)
            {
                continue;
            }

            database.Entries.Add(new ItemSoundDatabase.CategorySoundEntry
            {
                category = category,
                volume = 1f
            });
            changed = true;
        }

        database.Entries.Sort((left, right) => left.category.CompareTo(right.category));

        if (changed)
        {
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
        }
    }
}

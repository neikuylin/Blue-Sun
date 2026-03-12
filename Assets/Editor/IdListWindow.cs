using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class IdListWindow : EditorWindow
{
    private const string ResourceFolder = "Assets/Resources";
    private const string StatAssetPath = ResourceFolder + "/CharacterStatDatabase.asset";
    private const string BindingAssetPath = ResourceFolder + "/BattleCharacterBindings.asset";
    private const string TimelineAssetPath = ResourceFolder + "/TurnTimelineButtonDatabase.asset";

    private Vector2 scroll;
    private string newId = string.Empty;

    [MenuItem("Tools/ID列表")]
    private static void Open()
    {
        IdListWindow window = GetWindow<IdListWindow>("ID列表");
        window.minSize = new Vector2(540f, 420f);
        window.Show();
        window.Focus();
    }

    private void OnGUI()
    {
        CharacterStatDatabase statDatabase = EnsureStatDatabase();
        BattleCharacterBindingDatabase bindingDatabase = EnsureBindingDatabase();
        TurnTimelineButtonDatabase timelineDatabase = EnsureTimelineDatabase();

        EditorGUILayout.LabelField("ID列表", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "集中新增或删除全局 ID，并同步到属性库、战斗绑定库、时间轴绑定库。",
            MessageType.Info);

        using (new EditorGUILayout.HorizontalScope())
        {
            newId = EditorGUILayout.TextField("新ID", newId);
            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(newId)))
            {
                if (GUILayout.Button("新增", GUILayout.Width(100f)))
                {
                    AddId(newId.Trim(), statDatabase, bindingDatabase, timelineDatabase);
                    newId = string.Empty;
                }
            }
        }

        EditorGUILayout.Space(8f);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("刷新"))
            {
                Repaint();
            }

            if (GUILayout.Button("全部排序"))
            {
                SortAll(statDatabase, bindingDatabase, timelineDatabase);
            }
        }

        EditorGUILayout.Space(8f);

        List<string> allIds = CollectAllIds(statDatabase, bindingDatabase, timelineDatabase);
        scroll = EditorGUILayout.BeginScrollView(scroll);
        for (int i = 0; i < allIds.Count; i++)
        {
            string id = allIds[i];
            using (new EditorGUILayout.HorizontalScope("box"))
            {
                EditorGUILayout.LabelField(id, GUILayout.MinWidth(180f));
                EditorGUILayout.LabelField(BuildPresenceLabel(id, statDatabase, bindingDatabase, timelineDatabase));

                if (GUILayout.Button("Remove", GUILayout.Width(90f)))
                {
                    if (EditorUtility.DisplayDialog(
                        "删除ID",
                        $"要从属性库、战斗绑定库、时间轴绑定库中删除 ID “{id}” 吗？",
                        "删除",
                        "取消"))
                    {
                        RemoveId(id, statDatabase, bindingDatabase, timelineDatabase);
                        GUIUtility.ExitGUI();
                    }
                }
            }
        }
        EditorGUILayout.EndScrollView();
    }

    private static void AddId(
        string id,
        CharacterStatDatabase statDatabase,
        BattleCharacterBindingDatabase bindingDatabase,
        TurnTimelineButtonDatabase timelineDatabase)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return;
        }

        bool changed = false;

        if (statDatabase != null && statDatabase.FindEntry(id) == null)
        {
            statDatabase.Entries.Add(new CharacterStatDatabase.StatEntry
            {
                characterId = id,
                actionPoints = 4
            });
            EditorUtility.SetDirty(statDatabase);
            changed = true;
        }

        if (bindingDatabase != null && bindingDatabase.FindBinding(id) == null)
        {
            bindingDatabase.Entries.Add(new BattleCharacterBindingDatabase.BindingEntry
            {
                characterId = id,
                displayName = id,
                useAutoVisualAnchor = true
            });
            EditorUtility.SetDirty(bindingDatabase);
            changed = true;
        }

        if (timelineDatabase != null && timelineDatabase.FindEntry(id) == null)
        {
            timelineDatabase.Entries.Add(new TurnTimelineButtonDatabase.Entry
            {
                characterId = id,
                buttonPrefab = null
            });
            EditorUtility.SetDirty(timelineDatabase);
            changed = true;
        }

        if (changed)
        {
            AssetDatabase.SaveAssets();
        }
    }

    private static void RemoveId(
        string id,
        CharacterStatDatabase statDatabase,
        BattleCharacterBindingDatabase bindingDatabase,
        TurnTimelineButtonDatabase timelineDatabase)
    {
        bool changed = false;

        changed |= RemoveStatEntry(id, statDatabase);
        changed |= RemoveBindingEntry(id, bindingDatabase);
        changed |= RemoveTimelineEntry(id, timelineDatabase);

        if (changed)
        {
            AssetDatabase.SaveAssets();
        }
    }

    private static bool RemoveStatEntry(string id, CharacterStatDatabase database)
    {
        if (database == null)
        {
            return false;
        }

        for (int i = database.Entries.Count - 1; i >= 0; i--)
        {
            CharacterStatDatabase.StatEntry entry = database.Entries[i];
            if (entry != null && string.Equals(entry.characterId, id, StringComparison.Ordinal))
            {
                database.Entries.RemoveAt(i);
                EditorUtility.SetDirty(database);
                return true;
            }
        }

        return false;
    }

    private static bool RemoveBindingEntry(string id, BattleCharacterBindingDatabase database)
    {
        if (database == null)
        {
            return false;
        }

        for (int i = database.Entries.Count - 1; i >= 0; i--)
        {
            BattleCharacterBindingDatabase.BindingEntry entry = database.Entries[i];
            if (entry != null && string.Equals(entry.characterId, id, StringComparison.Ordinal))
            {
                database.Entries.RemoveAt(i);
                EditorUtility.SetDirty(database);
                return true;
            }
        }

        return false;
    }

    private static bool RemoveTimelineEntry(string id, TurnTimelineButtonDatabase database)
    {
        if (database == null)
        {
            return false;
        }

        for (int i = database.Entries.Count - 1; i >= 0; i--)
        {
            TurnTimelineButtonDatabase.Entry entry = database.Entries[i];
            if (entry != null && string.Equals(entry.characterId, id, StringComparison.Ordinal))
            {
                database.Entries.RemoveAt(i);
                EditorUtility.SetDirty(database);
                return true;
            }
        }

        return false;
    }

    private static void SortAll(
        CharacterStatDatabase statDatabase,
        BattleCharacterBindingDatabase bindingDatabase,
        TurnTimelineButtonDatabase timelineDatabase)
    {
        if (statDatabase != null)
        {
            statDatabase.Entries.Sort((a, b) => string.Compare(a?.characterId, b?.characterId, StringComparison.Ordinal));
            EditorUtility.SetDirty(statDatabase);
        }

        if (bindingDatabase != null)
        {
            bindingDatabase.Entries.Sort((a, b) => string.Compare(a?.characterId, b?.characterId, StringComparison.Ordinal));
            EditorUtility.SetDirty(bindingDatabase);
        }

        if (timelineDatabase != null)
        {
            timelineDatabase.Entries.Sort((a, b) => string.Compare(a?.characterId, b?.characterId, StringComparison.Ordinal));
            EditorUtility.SetDirty(timelineDatabase);
        }

        AssetDatabase.SaveAssets();
    }

    private static string BuildPresenceLabel(
        string id,
        CharacterStatDatabase statDatabase,
        BattleCharacterBindingDatabase bindingDatabase,
        TurnTimelineButtonDatabase timelineDatabase)
    {
        bool hasStats = statDatabase != null && statDatabase.FindEntry(id) != null;
        bool hasBinding = bindingDatabase != null && bindingDatabase.FindBinding(id) != null;
        bool hasTimeline = timelineDatabase != null && timelineDatabase.FindEntry(id) != null;
        return $"属性:{BoolToYesNo(hasStats)}  绑定:{BoolToYesNo(hasBinding)}  时间轴:{BoolToYesNo(hasTimeline)}";
    }

    private static string BoolToYesNo(bool value)
    {
        return value ? "有" : "无";
    }

    private static List<string> CollectAllIds(
        CharacterStatDatabase statDatabase,
        BattleCharacterBindingDatabase bindingDatabase,
        TurnTimelineButtonDatabase timelineDatabase)
    {
        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);

        if (statDatabase != null)
        {
            for (int i = 0; i < statDatabase.Entries.Count; i++)
            {
                CharacterStatDatabase.StatEntry entry = statDatabase.Entries[i];
                if (entry != null && !string.IsNullOrWhiteSpace(entry.characterId))
                {
                    ids.Add(entry.characterId);
                }
            }
        }

        if (bindingDatabase != null)
        {
            for (int i = 0; i < bindingDatabase.Entries.Count; i++)
            {
                BattleCharacterBindingDatabase.BindingEntry entry = bindingDatabase.Entries[i];
                if (entry != null && !string.IsNullOrWhiteSpace(entry.characterId))
                {
                    ids.Add(entry.characterId);
                }
            }
        }

        if (timelineDatabase != null)
        {
            for (int i = 0; i < timelineDatabase.Entries.Count; i++)
            {
                TurnTimelineButtonDatabase.Entry entry = timelineDatabase.Entries[i];
                if (entry != null && !string.IsNullOrWhiteSpace(entry.characterId))
                {
                    ids.Add(entry.characterId);
                }
            }
        }

        List<string> result = new List<string>(ids);
        result.Sort(StringComparer.Ordinal);
        return result;
    }

    private static CharacterStatDatabase EnsureStatDatabase()
    {
        CharacterStatDatabase database = AssetDatabase.LoadAssetAtPath<CharacterStatDatabase>(StatAssetPath);
        if (database != null)
        {
            return database;
        }

        EnsureResourceFolder();
        database = CreateInstance<CharacterStatDatabase>();
        AssetDatabase.CreateAsset(database, StatAssetPath);
        AssetDatabase.SaveAssets();
        return database;
    }

    private static BattleCharacterBindingDatabase EnsureBindingDatabase()
    {
        BattleCharacterBindingDatabase database = AssetDatabase.LoadAssetAtPath<BattleCharacterBindingDatabase>(BindingAssetPath);
        if (database != null)
        {
            return database;
        }

        EnsureResourceFolder();
        database = CreateInstance<BattleCharacterBindingDatabase>();
        AssetDatabase.CreateAsset(database, BindingAssetPath);
        AssetDatabase.SaveAssets();
        return database;
    }

    private static TurnTimelineButtonDatabase EnsureTimelineDatabase()
    {
        TurnTimelineButtonDatabase database = AssetDatabase.LoadAssetAtPath<TurnTimelineButtonDatabase>(TimelineAssetPath);
        if (database != null)
        {
            return database;
        }

        EnsureResourceFolder();
        database = CreateInstance<TurnTimelineButtonDatabase>();
        AssetDatabase.CreateAsset(database, TimelineAssetPath);
        AssetDatabase.SaveAssets();
        return database;
    }

    private static void EnsureResourceFolder()
    {
        if (!AssetDatabase.IsValidFolder(ResourceFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }
    }
}

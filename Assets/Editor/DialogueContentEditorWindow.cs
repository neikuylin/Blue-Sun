using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class DialogueContentEditorWindow : EditorWindow
{
    private const string ResourceFolder = "Assets/Resources";
    private const string AssetPath = ResourceFolder + "/DialogueContentDatabase.asset";

    private Vector2 scroll;
    private string newId = string.Empty;
    private readonly Dictionary<string, bool> foldoutStates = new Dictionary<string, bool>();
    private readonly Dictionary<string, bool> interactionFoldoutStates = new Dictionary<string, bool>();

    [MenuItem("Tools/Event/Dialogue Content Editor")]
    private static void Open()
    {
        DialogueContentEditorWindow window = GetWindow<DialogueContentEditorWindow>("Dialogue Content Editor");
        window.minSize = new Vector2(920f, 680f);
        window.Show();
        window.Focus();
    }

    private void OnGUI()
    {
        DialogueContentDatabase database = EnsureDatabase();
        DialogueRoleNameDatabase roleNameDatabase = DialogueRoleNameDatabase.LoadDefault();
        DialogueGroupDatabase groupDatabase = DialogueGroupDatabase.LoadDefault();
        if (database == null)
        {
            EditorGUILayout.HelpBox("Failed to create or load DialogueContentDatabase.", MessageType.Error);
            return;
        }

        EditorGUILayout.LabelField("Dialogue Content Editor", EditorStyles.boldLabel);
        EditorGUILayout.Space(4f);

        using (new EditorGUILayout.HorizontalScope())
        {
            newId = EditorGUILayout.TextField("New Dialogue ID", newId);
            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(newId)))
            {
                if (GUILayout.Button("Add", GUILayout.Width(72f)))
                {
                    Undo.RecordObject(database, "Add Dialogue Content");
                    database.GetOrCreateEntry(newId);
                    SaveAsset(database);
                    foldoutStates[newId.Trim()] = true;
                    newId = string.Empty;
                }
            }
        }

        EditorGUILayout.Space(8f);
        scroll = EditorGUILayout.BeginScrollView(scroll);
        for (int i = 0; i < database.Entries.Count; i++)
        {
            DrawEntry(database, database.Entries[i], i, roleNameDatabase, groupDatabase);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawEntry(
        DialogueContentDatabase database,
        DialogueContentDatabase.DialogueContentEntry entry,
        int index,
        DialogueRoleNameDatabase roleNameDatabase,
        DialogueGroupDatabase groupDatabase)
    {
        if (entry == null)
        {
            return;
        }

        DialogueContentDatabase.EnsureEntry(entry);

        using (new EditorGUILayout.VerticalScope("box"))
        {
            string foldoutKey = string.IsNullOrWhiteSpace(entry.id) ? $"__index_{index}" : entry.id;

            using (new EditorGUILayout.HorizontalScope())
            {
                bool isExpanded = GetFoldoutState(foldoutKey);
                string title = string.IsNullOrWhiteSpace(entry.id) ? $"内容 {index + 1}" : entry.id;
                bool nextExpanded = EditorGUILayout.Foldout(isExpanded, title, true);
                if (nextExpanded != isExpanded)
                {
                    foldoutStates[foldoutKey] = nextExpanded;
                }

                if (GUILayout.Button("Delete", GUILayout.Width(72f)))
                {
                    Undo.RecordObject(database, "Delete Dialogue Content");
                    database.Entries.RemoveAt(index);
                    foldoutStates.Remove(foldoutKey);
                    interactionFoldoutStates.Remove(foldoutKey);
                    SaveAsset(database);
                    GUIUtility.ExitGUI();
                }
            }

            if (!GetFoldoutState(foldoutKey))
            {
                return;
            }

            string nextId = EditorGUILayout.TextField("Dialogue ID", entry.id);
            if (!string.Equals(nextId, entry.id, StringComparison.Ordinal))
            {
                Undo.RecordObject(database, "Edit Dialogue ID");
                string oldKey = foldoutKey;
                entry.id = nextId;
                string newKey = string.IsNullOrWhiteSpace(entry.id) ? $"__index_{index}" : entry.id;
                bool expanded = GetFoldoutState(oldKey);
                bool interactionExpanded = GetInteractionFoldoutState(oldKey);
                foldoutStates.Remove(oldKey);
                interactionFoldoutStates.Remove(oldKey);
                foldoutStates[newKey] = expanded;
                interactionFoldoutStates[newKey] = interactionExpanded;
                SaveAsset(database);
                foldoutKey = newKey;
            }

            string nextRoleNameId = DrawIdPopup("Role Name", entry.roleNameId, GetRoleNameIds(roleNameDatabase));
            if (!string.Equals(nextRoleNameId, entry.roleNameId, StringComparison.Ordinal))
            {
                Undo.RecordObject(database, "Edit Role Name");
                entry.roleNameId = nextRoleNameId;
                SaveAsset(database);
            }

            GameObject nextPortraitPrefab = (GameObject)EditorGUILayout.ObjectField("Portrait Prefab", entry.portraitPrefab, typeof(GameObject), false);
            if (nextPortraitPrefab != entry.portraitPrefab)
            {
                Undo.RecordObject(database, "Edit Portrait Prefab");
                entry.portraitPrefab = nextPortraitPrefab;
                SaveAsset(database);
            }

            DialogueContentDatabase.DialogueViewSide nextViewSide =
                (DialogueContentDatabase.DialogueViewSide)EditorGUILayout.EnumPopup("View Side", entry.viewSide);
            if (nextViewSide != entry.viewSide)
            {
                Undo.RecordObject(database, "Edit View Side");
                entry.viewSide = nextViewSide;
                SaveAsset(database);
            }

            string nextContent = EditorGUILayout.TextArea(entry.content, GUILayout.MinHeight(90f));
            if (!string.Equals(nextContent, entry.content, StringComparison.Ordinal))
            {
                Undo.RecordObject(database, "Edit Dialogue Content");
                entry.content = nextContent;
                SaveAsset(database);
            }

            EditorGUILayout.Space(6f);
            DrawInteractionSection(database, entry, foldoutKey, groupDatabase);
        }
    }

    private void DrawInteractionSection(
        DialogueContentDatabase database,
        DialogueContentDatabase.DialogueContentEntry entry,
        string foldoutKey,
        DialogueGroupDatabase groupDatabase)
    {
        using (new EditorGUILayout.VerticalScope("box"))
        {
            bool expanded = GetInteractionFoldoutState(foldoutKey);
            bool nextExpanded = EditorGUILayout.Foldout(expanded, $"Interactions ({entry.interactions.Count})", true);
            if (nextExpanded != expanded)
            {
                interactionFoldoutStates[foldoutKey] = nextExpanded;
            }

            if (!GetInteractionFoldoutState(foldoutKey))
            {
                return;
            }

            for (int i = 0; i < entry.interactions.Count; i++)
            {
                DrawInteractionEntry(database, entry, i, groupDatabase);
            }

            if (GUILayout.Button("Add Interaction", GUILayout.Width(120f)))
            {
                Undo.RecordObject(database, "Add Interaction");
                entry.interactions.Add(new DialogueContentDatabase.InteractionEntry());
                SaveAsset(database);
            }
        }
    }

    private void DrawInteractionEntry(
        DialogueContentDatabase database,
        DialogueContentDatabase.DialogueContentEntry contentEntry,
        int index,
        DialogueGroupDatabase groupDatabase)
    {
        if (contentEntry == null || contentEntry.interactions == null || index < 0 || index >= contentEntry.interactions.Count)
        {
            return;
        }

        DialogueContentDatabase.InteractionEntry interaction = contentEntry.interactions[index];
        if (interaction == null)
        {
            return;
        }

        using (new EditorGUILayout.VerticalScope("box"))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"Interaction {index + 1}", EditorStyles.boldLabel);
                if (GUILayout.Button("Delete", GUILayout.Width(72f)))
                {
                    Undo.RecordObject(database, "Delete Interaction");
                    contentEntry.interactions.RemoveAt(index);
                    SaveAsset(database);
                    GUIUtility.ExitGUI();
                }
            }

            string nextButtonText = EditorGUILayout.TextField("Button Text", interaction.buttonText);
            if (!string.Equals(nextButtonText, interaction.buttonText, StringComparison.Ordinal))
            {
                Undo.RecordObject(database, "Edit Interaction Text");
                interaction.buttonText = nextButtonText;
                SaveAsset(database);
            }

            DialogueContentDatabase.InteractionType nextInteractionType =
                (DialogueContentDatabase.InteractionType)EditorGUILayout.EnumPopup("Interaction Type", interaction.interactionType);
            if (nextInteractionType != interaction.interactionType)
            {
                Undo.RecordObject(database, "Edit Interaction Type");
                interaction.interactionType = nextInteractionType;
                SaveAsset(database);
            }

            GameObject nextInteractionPrefab = (GameObject)EditorGUILayout.ObjectField(
                "Interaction Prefab",
                interaction.interactionPrefab,
                typeof(GameObject),
                false);
            if (nextInteractionPrefab != interaction.interactionPrefab)
            {
                Undo.RecordObject(database, "Edit Interaction Prefab");
                interaction.interactionPrefab = nextInteractionPrefab;
                SaveAsset(database);
            }

            switch (interaction.interactionType)
            {
                case DialogueContentDatabase.InteractionType.Button:
                    EditorGUILayout.HelpBox("Button interactions only keep an entry point for now.", MessageType.None);
                    break;

                case DialogueContentDatabase.InteractionType.JumpToDialogueGroup:
                {
                    string nextTargetDialogueGroupId = DrawIdPopup(
                        "Target Dialogue Group",
                        interaction.targetDialogueGroupId,
                        GetDialogueGroupIds(groupDatabase));
                    if (!string.Equals(nextTargetDialogueGroupId, interaction.targetDialogueGroupId, StringComparison.Ordinal))
                    {
                        Undo.RecordObject(database, "Edit Target Dialogue Group");
                        interaction.targetDialogueGroupId = nextTargetDialogueGroupId;
                        SaveAsset(database);
                    }

                    break;
                }

                case DialogueContentDatabase.InteractionType.ContinueDialogue:
                    EditorGUILayout.HelpBox("Continue to the next line in the current dialogue group.", MessageType.None);
                    break;
            }
        }
    }

    private bool GetFoldoutState(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return true;
        }

        if (foldoutStates.TryGetValue(key, out bool expanded))
        {
            return expanded;
        }

        foldoutStates[key] = true;
        return true;
    }

    private bool GetInteractionFoldoutState(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return true;
        }

        if (interactionFoldoutStates.TryGetValue(key, out bool expanded))
        {
            return expanded;
        }

        interactionFoldoutStates[key] = true;
        return true;
    }

    private string DrawIdPopup(string label, string currentValue, List<string> values)
    {
        List<string> options = new List<string> { string.Empty };
        if (values != null)
        {
            options.AddRange(values);
        }

        int currentIndex = 0;
        for (int i = 0; i < options.Count; i++)
        {
            if (string.Equals(options[i], currentValue, StringComparison.Ordinal))
            {
                currentIndex = i;
                break;
            }
        }

        string[] displayOptions = new string[options.Count];
        for (int i = 0; i < options.Count; i++)
        {
            displayOptions[i] = string.IsNullOrWhiteSpace(options[i]) ? "None" : options[i];
        }

        int nextIndex = EditorGUILayout.Popup(label, currentIndex, displayOptions);
        if (nextIndex < 0 || nextIndex >= options.Count)
        {
            return currentValue;
        }

        return options[nextIndex];
    }

    private static List<string> GetRoleNameIds(DialogueRoleNameDatabase database)
    {
        List<string> result = new List<string>();
        if (database == null)
        {
            return result;
        }

        for (int i = 0; i < database.Entries.Count; i++)
        {
            DialogueRoleNameDatabase.RoleNameEntry entry = database.Entries[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.id))
            {
                continue;
            }

            result.Add(entry.id);
        }

        return result;
    }

    private static List<string> GetDialogueGroupIds(DialogueGroupDatabase database)
    {
        List<string> result = new List<string>();
        if (database == null)
        {
            return result;
        }

        for (int i = 0; i < database.Entries.Count; i++)
        {
            DialogueGroupDatabase.DialogueGroupEntry entry = database.Entries[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.id))
            {
                continue;
            }

            result.Add(entry.id);
        }

        return result;
    }

    private static DialogueContentDatabase EnsureDatabase()
    {
        DialogueContentDatabase database = AssetDatabase.LoadAssetAtPath<DialogueContentDatabase>(AssetPath);
        if (database != null)
        {
            return database;
        }

        EnsureResourceFolder();
        database = CreateInstance<DialogueContentDatabase>();
        AssetDatabase.CreateAsset(database, AssetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return database;
    }

    private static void EnsureResourceFolder()
    {
        if (!AssetDatabase.IsValidFolder(ResourceFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }
    }

    private static void SaveAsset(ScriptableObject asset)
    {
        if (asset == null)
        {
            return;
        }

        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
    }
}

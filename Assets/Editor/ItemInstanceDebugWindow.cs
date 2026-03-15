using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class ItemInstanceDebugWindow : EditorWindow
{
    private ItemDatabase database;
    private Vector2 scroll;

    [MenuItem("Tools/Items/Runtime Item Instances")]
    private static void Open()
    {
        ItemInstanceDebugWindow window = GetWindow<ItemInstanceDebugWindow>();
        window.titleContent = new GUIContent("Item Instances");
        window.minSize = new Vector2(520f, 420f);
        window.Show();
    }

    private void OnEnable()
    {
        database = ItemDatabase.LoadDefault();
    }

    private void OnGUI()
    {
        database = database != null ? database : ItemDatabase.LoadDefault();

        EditorGUILayout.LabelField("Runtime Item Instances", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Reads the current runtime slot data. This is instance data, not the ItemDatabase definition.",
            MessageType.Info);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to inspect current item instances.", MessageType.Info);
            return;
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Refresh", GUILayout.Width(80f)))
            {
                Repaint();
            }
        }

        scroll = EditorGUILayout.BeginScrollView(scroll);
        DrawSection("Backpack Instances", InventoryShortcutRuntimeBinder.GetBackpackSnapshots());
        EditorGUILayout.Space(8f);
        DrawSection("Warehouse Instances", InventoryShortcutRuntimeBinder.GetWarehouseSnapshots());
        EditorGUILayout.Space(8f);
        DrawEquipmentSections();
        EditorGUILayout.EndScrollView();
    }

    private void DrawEquipmentSections()
    {
        List<string> characterIds = InventoryShortcutRuntimeBinder.GetEquipmentCharacterIds();
        if (characterIds.Count == 0)
        {
            EditorGUILayout.HelpBox("No runtime equipment instance data found yet.", MessageType.Info);
            return;
        }

        for (int i = 0; i < characterIds.Count; i++)
        {
            string characterId = characterIds[i];
            List<InventoryShortcutRuntimeBinder.ItemSlotSnapshot> snapshots =
                InventoryShortcutRuntimeBinder.GetEquipmentSnapshots(characterId);
            DrawSection($"Equipment Instances - {characterId}", snapshots);
            if (i < characterIds.Count - 1)
            {
                EditorGUILayout.Space(8f);
            }
        }
    }

    private void DrawSection(string title, List<InventoryShortcutRuntimeBinder.ItemSlotSnapshot> snapshots)
    {
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

        if (snapshots == null || snapshots.Count == 0)
        {
            EditorGUILayout.HelpBox("No data available.", MessageType.None);
            return;
        }

        for (int i = 0; i < snapshots.Count; i++)
        {
            InventoryShortcutRuntimeBinder.ItemSlotSnapshot snapshot = snapshots[i];
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField($"Slot {snapshot.index}");

                if (snapshot.isEmpty || string.IsNullOrWhiteSpace(snapshot.itemId))
                {
                    EditorGUILayout.LabelField("State", "Empty");
                    continue;
                }

                ItemDatabase.ItemEntry entry = database != null ? database.FindEntry(snapshot.itemId) : null;
                EditorGUILayout.LabelField("itemId", snapshot.itemId);
                EditorGUILayout.LabelField(
                    "Display Name",
                    entry != null && !string.IsNullOrWhiteSpace(entry.displayName) ? entry.displayName : snapshot.itemId);
                EditorGUILayout.LabelField("Count", snapshot.count.ToString());
                EditorGUILayout.LabelField("Max Stack", snapshot.maxStack.ToString());
                EditorGUILayout.LabelField(
                    "Definition Lookup",
                    entry != null ? "Resolved in ItemDatabase" : "Missing from ItemDatabase");
            }
        }
    }
}

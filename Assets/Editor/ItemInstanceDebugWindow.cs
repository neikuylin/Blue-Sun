using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class ItemInstanceDebugWindow : EditorWindow
{
    private static readonly string[] SectionLabels =
    {
        "\u4ed3\u5e93",
        "\u80cc\u5305",
        "\u89d2\u8272"
    };

    private ItemDatabase database;
    private Vector2 scroll;
    private int selectedSection;
    private int selectedCharacterIndex;
    private int warehouseSlotCountDraft;
    private int backpackSlotCountDraft;
    private readonly Dictionary<string, int> equipmentSlotCountDrafts = new Dictionary<string, int>();

    [MenuItem("Tools/\u7269\u54c1/\u73b0\u6709\u7269\u54c1\u5b9e\u4f8b")]
    private static void Open()
    {
        ItemInstanceDebugWindow window = GetWindow<ItemInstanceDebugWindow>();
        window.titleContent = new GUIContent("\u73b0\u6709\u7269\u54c1\u5b9e\u4f8b");
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

        EditorGUILayout.LabelField("\u73b0\u6709\u7269\u54c1\u5b9e\u4f8b", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "\u8fd9\u91cc\u8bfb\u53d6\u7684\u662f\u8fd0\u884c\u65f6\u69fd\u4f4d\u91cc\u7684\u5f53\u524d\u5b9e\u4f8b\u6570\u636e\uff0c\u4e0d\u662f ItemDatabase \u91cc\u7684\u5b9a\u4e49\u672c\u4f53\u3002",
            MessageType.Info);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("\u8bf7\u5148\u8fdb\u5165 Play \u6a21\u5f0f\u540e\u518d\u67e5\u770b\u5f53\u524d\u5b9e\u4f8b\u3002", MessageType.Info);
            return;
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            selectedSection = GUILayout.Toolbar(selectedSection, SectionLabels);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("\u5237\u65b0", GUILayout.Width(80f)))
            {
                Repaint();
            }
        }

        EditorGUILayout.Space(8f);
        scroll = EditorGUILayout.BeginScrollView(scroll);
        DrawSelectedSection();
        EditorGUILayout.EndScrollView();
    }

    private void DrawSelectedSection()
    {
        switch (selectedSection)
        {
            case 0:
                DrawSection("\u4ed3\u5e93\u5b9e\u4f8b", InventoryShortcutRuntimeBinder.GetWarehouseSnapshots(), string.Empty);
                break;
            case 1:
                DrawSection("\u80cc\u5305\u5b9e\u4f8b", InventoryShortcutRuntimeBinder.GetBackpackSnapshots(), string.Empty);
                break;
            default:
                DrawCharacterSection();
                break;
        }
    }

    private void DrawCharacterSection()
    {
        List<string> characterIds = InventoryShortcutRuntimeBinder.GetEquipmentCharacterIds();
        if (characterIds.Count == 0)
        {
            EditorGUILayout.HelpBox("\u5f53\u524d\u8fd8\u6ca1\u6709\u89d2\u8272\u88c5\u5907\u5b9e\u4f8b\u6570\u636e\u3002", MessageType.Info);
            return;
        }

        selectedCharacterIndex = Mathf.Clamp(selectedCharacterIndex, 0, characterIds.Count - 1);
        selectedCharacterIndex = EditorGUILayout.Popup(
            "\u89d2\u8272",
            selectedCharacterIndex,
            characterIds.ToArray());

        string characterId = characterIds[selectedCharacterIndex];
        DrawSection(
            $"\u89d2\u8272\u88c5\u5907\u5b9e\u4f8b - {characterId}",
            InventoryShortcutRuntimeBinder.GetEquipmentSnapshots(characterId),
            characterId);
    }

    private void DrawSection(string title, List<InventoryShortcutRuntimeBinder.ItemSlotSnapshot> snapshots, string ownerCharacterId)
    {
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

        if (snapshots == null || snapshots.Count == 0)
        {
            EditorGUILayout.HelpBox("\u6ca1\u6709\u53ef\u8bfb\u53d6\u7684\u6570\u636e\u3002", MessageType.None);
            return;
        }

        using (new EditorGUILayout.VerticalScope("box"))
        {
            int slotCountDraft = GetSlotCountDraft(ownerCharacterId, snapshots != null ? snapshots.Count : 0);
            EditorGUI.BeginChangeCheck();
            slotCountDraft = EditorGUILayout.IntField("\u66F4\u6539\u69FD\u4F4D\u6570\u91CF", slotCountDraft);
            if (EditorGUI.EndChangeCheck())
            {
                SetSlotCountDraft(ownerCharacterId, Mathf.Max(0, slotCountDraft));
            }
        }

        EditorGUILayout.Space(6f);

        for (int i = 0; i < snapshots.Count; i++)
        {
            InventoryShortcutRuntimeBinder.ItemSlotSnapshot snapshot = snapshots[i];
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField($"\u69fd\u4f4d {snapshot.index}");

                if (snapshot.isEmpty || string.IsNullOrWhiteSpace(snapshot.itemId))
                {
                    EditorGUILayout.LabelField("\u72b6\u6001", "\u7a7a");
                    continue;
                }

                ItemDatabase.ItemEntry entry = database != null ? database.FindEntry(snapshot.itemId) : null;
                EditorGUILayout.LabelField("itemId", snapshot.itemId);
                EditorGUILayout.LabelField(
                    "\u663e\u793a\u540d",
                    entry != null && !string.IsNullOrWhiteSpace(entry.displayName) ? entry.displayName : snapshot.itemId);
                EditorGUILayout.LabelField("\u6570\u91cf", snapshot.count.ToString());
                EditorGUILayout.LabelField("\u5355\u683c\u4e0a\u9650", snapshot.maxStack.ToString());
                EditorGUILayout.LabelField(
                    "\u88c5\u5907\u8005",
                    string.IsNullOrWhiteSpace(ownerCharacterId) ? "-" : ownerCharacterId);
                EditorGUILayout.LabelField(
                    "\u653b\u51fb\u529b",
                    GetAttackPowerText(snapshot.itemId, ownerCharacterId));
                EditorGUILayout.LabelField(
                    "\u5b9a\u4e49\u8d44\u6e90",
                    entry != null ? "\u5df2\u547d\u4e2d ItemDatabase" : "\u672a\u547d\u4e2d ItemDatabase");
            }
        }
    }

    private static string GetAttackPowerText(string itemId, string ownerCharacterId)
    {
        string value = InventoryShortcutRuntimeBinder.GetAttackPowerDisplayTextForCharacter(itemId, ownerCharacterId);
        return string.IsNullOrWhiteSpace(value) ? "-" : value;
    }

    private int GetSlotCountDraft(string ownerCharacterId, int fallbackValue)
    {
        if (string.IsNullOrWhiteSpace(ownerCharacterId))
        {
            if (selectedSection == 0)
            {
                warehouseSlotCountDraft = InventoryShortcutRuntimeBinder.GetWarehouseUsableSlotCount();
                if (warehouseSlotCountDraft < 0 && fallbackValue > 0)
                {
                    warehouseSlotCountDraft = fallbackValue;
                }
                return warehouseSlotCountDraft;
            }

            backpackSlotCountDraft = InventoryShortcutRuntimeBinder.GetBackpackUsableSlotCount();
            if (backpackSlotCountDraft < 0 && fallbackValue > 0)
            {
                backpackSlotCountDraft = fallbackValue;
            }

            return backpackSlotCountDraft;
        }

        int value = InventoryShortcutRuntimeBinder.GetEquipmentUsableSlotCount(ownerCharacterId);
        if (value < 0 && fallbackValue > 0)
        {
            value = fallbackValue;
        }

        equipmentSlotCountDrafts[ownerCharacterId] = value;
        return value;
    }

    private void SetSlotCountDraft(string ownerCharacterId, int value)
    {
        if (string.IsNullOrWhiteSpace(ownerCharacterId))
        {
            if (selectedSection == 0)
            {
                warehouseSlotCountDraft = value;
                InventoryShortcutRuntimeBinder.SetWarehouseUsableSlotCount(value);
                return;
            }

            backpackSlotCountDraft = value;
            InventoryShortcutRuntimeBinder.SetBackpackUsableSlotCount(value);
            return;
        }

        equipmentSlotCountDrafts[ownerCharacterId] = value;
        InventoryShortcutRuntimeBinder.SetEquipmentUsableSlotCount(ownerCharacterId, value);
    }
}

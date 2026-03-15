using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class InventoryDebugWindow : EditorWindow
{
    private ItemDatabase database;
    private ItemDatabase.ItemCategory selectedCategory = ItemDatabase.ItemCategory.Equipment;
    private ItemDatabase.EquipmentSlotType selectedEquipmentSlot = ItemDatabase.EquipmentSlotType.MainHand;
    private ItemDatabase.WeaponCategory selectedWeaponCategory = ItemDatabase.WeaponCategory.None;
    private int selectedItemIndex;
    private int addCount = 1;

    private int removeSlot;
    private int removeCount = 1;

    private int moveFrom;
    private int moveTo = 1;

    private Vector2 scroll;

    [MenuItem("Tools/\u7269\u54c1/\u80cc\u5305\u8c03\u8bd5\u7a97\u53e3")]
    private static void Open()
    {
        InventoryDebugWindow window = CreateInstance<InventoryDebugWindow>();
        window.titleContent = new GUIContent("\u80cc\u5305\u8c03\u8bd5");
        window.minSize = new Vector2(360f, 420f);
        window.maxSize = new Vector2(900f, 1200f);
        window.position = new Rect(160f, 120f, 420f, 520f);
        window.ShowUtility();
        window.Focus();
    }

    private void OnEnable()
    {
        database = ItemDatabase.LoadDefault();
    }

    private void OnGUI()
    {
        database = database != null ? database : ItemDatabase.LoadDefault();

        EditorGUILayout.LabelField("\u80cc\u5305\u8c03\u8bd5\u5668", EditorStyles.boldLabel);
        EditorGUILayout.Space(4f);

        if (database == null)
        {
            EditorGUILayout.HelpBox("\u672a\u627e\u5230\u7269\u54c1\u6570\u636e\u5e93\u3002\u5148\u6253\u5f00 Tools/\u7269\u54c1/\u7269\u54c1\u6570\u636e\u5e93 \u521b\u5efa\u3002", MessageType.Warning);
            return;
        }

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("\u8bf7\u5148\u8fdb\u5165 Play \u6a21\u5f0f\uff0c\u518d\u4f7f\u7528\u672c\u7a97\u53e3\u3002", MessageType.Info);
            return;
        }

        DrawAddPanel();
        EditorGUILayout.Space(8f);
        DrawRemovePanel();
        EditorGUILayout.Space(8f);
        DrawMovePanel();
        EditorGUILayout.Space(10f);
        DrawSlotsPanel();
    }

    private void DrawAddPanel()
    {
        EditorGUILayout.LabelField("\u6dfb\u52a0\u7269\u54c1", EditorStyles.boldLabel);

        selectedCategory = (ItemDatabase.ItemCategory)EditorGUILayout.Popup(
            "\u5206\u7c7b",
            (int)selectedCategory,
            ItemEditorLabels.CategoryLabels);

        if (selectedCategory == ItemDatabase.ItemCategory.Equipment)
        {
            selectedEquipmentSlot = (ItemDatabase.EquipmentSlotType)EditorGUILayout.Popup(
                "\u88c5\u5907\u90e8\u4f4d",
                (int)selectedEquipmentSlot,
                ItemEditorLabels.EquipmentSlotLabels);

            DrawWeaponCategoryPopup(selectedEquipmentSlot, ref selectedWeaponCategory);
        }
        else
        {
            selectedEquipmentSlot = ItemDatabase.EquipmentSlotType.None;
            selectedWeaponCategory = ItemDatabase.WeaponCategory.None;
        }

        List<ItemDatabase.ItemEntry> entries = database.FindEntries(selectedCategory, selectedEquipmentSlot, selectedWeaponCategory);
        if (entries.Count == 0)
        {
            EditorGUILayout.HelpBox("\u5f53\u524d\u5206\u7c7b\u4e0b\u6ca1\u6709\u7269\u54c1\u5b9a\u4e49\u3002", MessageType.Info);
            return;
        }

        string[] options = BuildItemOptions(entries);
        selectedItemIndex = Mathf.Clamp(selectedItemIndex, 0, options.Length - 1);
        selectedItemIndex = EditorGUILayout.Popup("\u7269\u54c1", selectedItemIndex, options);

        ItemDatabase.ItemEntry selectedEntry = entries[selectedItemIndex];
        EditorGUILayout.ObjectField("\u9884\u5236\u4f53", selectedEntry.prefab, typeof(GameObject), false);
        EditorGUILayout.HelpBox(
            selectedEntry.category == ItemDatabase.ItemCategory.Equipment
                ? "\u88c5\u5907\u7c7b\u5355\u683c\u4e0a\u9650\u56fa\u5b9a\u4e3a 1\u3002"
                : "\u6d88\u8017\u54c1\u3001\u6750\u6599\u3001\u8865\u7ed9\u5355\u683c\u4e0a\u9650\u56fa\u5b9a\u4e3a 5\u3002",
            MessageType.None);

        addCount = Mathf.Max(1, EditorGUILayout.IntField("\u6570\u91cf", addCount));

        using (new EditorGUI.DisabledScope(selectedEntry == null || selectedEntry.prefab == null))
        {
            if (GUILayout.Button("\u6dfb\u52a0\u7269\u54c1"))
            {
                int remain = InventoryShortcutRuntimeBinder.AddItem(selectedEntry, addCount);
                Debug.Log($"[\u80cc\u5305\u8c03\u8bd5] \u6dfb\u52a0\u5b8c\u6210\uff0c\u7269\u54c1={selectedEntry.itemId}\uff0c\u5269\u4f59\u672a\u653e\u5165\u6570\u91cf={remain}");
                Repaint();
            }
        }
    }

    private void DrawRemovePanel()
    {
        EditorGUILayout.LabelField("\u79fb\u9664\u7269\u54c1", EditorStyles.boldLabel);

        int maxIndex = Mathf.Max(0, InventoryShortcutRuntimeBinder.BackpackSlotCount - 1);
        removeSlot = Mathf.Clamp(EditorGUILayout.IntField("\u683c\u5b50\u7d22\u5f15", removeSlot), 0, maxIndex);
        removeCount = Mathf.Max(1, EditorGUILayout.IntField("\u79fb\u9664\u6570\u91cf", removeCount));

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("\u79fb\u9664"))
            {
                bool ok = InventoryShortcutRuntimeBinder.RemoveItemAt(removeSlot, removeCount);
                Debug.Log($"[\u80cc\u5305\u8c03\u8bd5] \u79fb\u9664 \u7d22\u5f15={removeSlot} \u6570\u91cf={removeCount} \u7ed3\u679c={ok}");
                Repaint();
            }

            if (GUILayout.Button("\u6e05\u7a7a\u6b64\u683c"))
            {
                bool ok = InventoryShortcutRuntimeBinder.RemoveItemAt(removeSlot, int.MaxValue);
                Debug.Log($"[\u80cc\u5305\u8c03\u8bd5] \u6e05\u7a7a\u683c\u5b50 \u7d22\u5f15={removeSlot} \u7ed3\u679c={ok}");
                Repaint();
            }
        }
    }

    private void DrawMovePanel()
    {
        EditorGUILayout.LabelField("\u79fb\u52a8/\u4ea4\u6362", EditorStyles.boldLabel);

        int maxIndex = Mathf.Max(0, InventoryShortcutRuntimeBinder.BackpackSlotCount - 1);
        moveFrom = Mathf.Clamp(EditorGUILayout.IntField("\u8d77\u59cb\u683c", moveFrom), 0, maxIndex);
        moveTo = Mathf.Clamp(EditorGUILayout.IntField("\u76ee\u6807\u683c", moveTo), 0, maxIndex);

        if (GUILayout.Button("\u6267\u884c\u79fb\u52a8/\u4ea4\u6362"))
        {
            bool ok = InventoryShortcutRuntimeBinder.MoveItem(moveFrom, moveTo);
            Debug.Log($"[\u80cc\u5305\u8c03\u8bd5] \u79fb\u52a8/\u4ea4\u6362 from={moveFrom} to={moveTo} \u7ed3\u679c={ok}");
            Repaint();
        }
    }

    private void DrawSlotsPanel()
    {
        EditorGUILayout.LabelField("\u5f53\u524d\u80cc\u5305\u6570\u636e", EditorStyles.boldLabel);

        int count = InventoryShortcutRuntimeBinder.BackpackSlotCount;
        if (count <= 0)
        {
            EditorGUILayout.HelpBox("\u672a\u68c0\u6d4b\u5230\u80cc\u5305\u69fd\u4f4d\u6570\u636e\u3002", MessageType.Warning);
            return;
        }

        scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(260f));
        for (int i = 0; i < count; i++)
        {
            if (!InventoryShortcutRuntimeBinder.TryGetBackpackSlotData(i, out InventoryShortcutRuntimeBinder.ItemSlotData slot))
            {
                continue;
            }

            string text = slot.IsEmpty
                ? $"[{i}](\u7a7a)"
                : $"[{i}] {slot.itemId} \u6570\u91cf:{slot.count} \u5355\u683c\u4e0a\u9650:{slot.maxStack}";

            EditorGUILayout.LabelField(text);
        }

        EditorGUILayout.EndScrollView();
    }

    private static void DrawWeaponCategoryPopup(
        ItemDatabase.EquipmentSlotType equipmentSlot,
        ref ItemDatabase.WeaponCategory weaponCategory)
    {
        if (!ItemDatabase.ShouldFilterWeaponCategory(equipmentSlot))
        {
            weaponCategory = ItemDatabase.WeaponCategory.None;
            return;
        }

        string[] labels = ItemEditorLabels.GetWeaponCategoryLabels(equipmentSlot);
        int popupIndex = ItemEditorLabels.ToWeaponCategoryPopupIndex(equipmentSlot, weaponCategory);
        popupIndex = EditorGUILayout.Popup("\u6b66\u5668\u5206\u7c7b", popupIndex, labels);
        weaponCategory = ItemEditorLabels.FromWeaponCategoryPopupIndex(equipmentSlot, popupIndex);
    }

    private static string[] BuildItemOptions(List<ItemDatabase.ItemEntry> entries)
    {
        string[] options = new string[entries.Count];
        for (int i = 0; i < entries.Count; i++)
        {
            ItemDatabase.ItemEntry entry = entries[i];
            options[i] = entry != null ? entry.itemId : "(\u7a7a)";
        }

        return options;
    }
}

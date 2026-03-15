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

    [MenuItem("Tools/物品/背包调试窗口")]
    private static void Open()
    {
        InventoryDebugWindow window = CreateInstance<InventoryDebugWindow>();
        window.titleContent = new GUIContent("背包调试");
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

        EditorGUILayout.LabelField("背包调试器", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        if (database == null)
        {
            EditorGUILayout.HelpBox("未找到物品数据库。先打开 Tools/物品/物品数据库 创建。", MessageType.Warning);
            return;
        }

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("请先进入 Play 模式，再使用本窗口。", MessageType.Info);
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
        EditorGUILayout.LabelField("塞入物品", EditorStyles.boldLabel);

        selectedCategory = (ItemDatabase.ItemCategory)EditorGUILayout.Popup(
            "类别",
            (int)selectedCategory,
            ItemEditorLabels.CategoryLabels);

        if (selectedCategory == ItemDatabase.ItemCategory.Equipment)
        {
            selectedEquipmentSlot = (ItemDatabase.EquipmentSlotType)EditorGUILayout.Popup(
                "装备部位",
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
            EditorGUILayout.HelpBox("当前分类下没有物品定义。", MessageType.Info);
            return;
        }

        string[] options = BuildItemOptions(entries);
        selectedItemIndex = Mathf.Clamp(selectedItemIndex, 0, options.Length - 1);
        selectedItemIndex = EditorGUILayout.Popup("物品", selectedItemIndex, options);

        ItemDatabase.ItemEntry selectedEntry = entries[selectedItemIndex];
        EditorGUILayout.ObjectField("预制体", selectedEntry.prefab, typeof(GameObject), false);
        EditorGUILayout.LabelField("单格上限", selectedEntry.category == ItemDatabase.ItemCategory.Equipment ? "1" : "5");

        addCount = Mathf.Max(1, EditorGUILayout.IntField("数量", addCount));

        using (new EditorGUI.DisabledScope(selectedEntry == null || selectedEntry.prefab == null))
        {
            if (GUILayout.Button("添加物品"))
            {
                int remain = InventoryShortcutRuntimeBinder.AddItem(selectedEntry, addCount);
                Debug.Log($"[背包调试] 添加完成，物品={selectedEntry.itemId}，剩余未放入数量={remain}");
                Repaint();
            }
        }
    }

    private void DrawRemovePanel()
    {
        EditorGUILayout.LabelField("移除物品", EditorStyles.boldLabel);

        int maxIndex = Mathf.Max(0, InventoryShortcutRuntimeBinder.BackpackSlotCount - 1);
        removeSlot = Mathf.Clamp(EditorGUILayout.IntField("格子索引", removeSlot), 0, maxIndex);
        removeCount = Mathf.Max(1, EditorGUILayout.IntField("移除数量", removeCount));

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("移除"))
            {
                bool ok = InventoryShortcutRuntimeBinder.RemoveItemAt(removeSlot, removeCount);
                Debug.Log($"[背包调试] 移除 索引={removeSlot} 数量={removeCount} 结果={ok}");
                Repaint();
            }

            if (GUILayout.Button("清空此格"))
            {
                bool ok = InventoryShortcutRuntimeBinder.RemoveItemAt(removeSlot, int.MaxValue);
                Debug.Log($"[背包调试] 清空格子 索引={removeSlot} 结果={ok}");
                Repaint();
            }
        }
    }

    private void DrawMovePanel()
    {
        EditorGUILayout.LabelField("移动/交换", EditorStyles.boldLabel);

        int maxIndex = Mathf.Max(0, InventoryShortcutRuntimeBinder.BackpackSlotCount - 1);
        moveFrom = Mathf.Clamp(EditorGUILayout.IntField("起始格", moveFrom), 0, maxIndex);
        moveTo = Mathf.Clamp(EditorGUILayout.IntField("目标格", moveTo), 0, maxIndex);

        if (GUILayout.Button("执行移动/交换"))
        {
            bool ok = InventoryShortcutRuntimeBinder.MoveItem(moveFrom, moveTo);
            Debug.Log($"[背包调试] 移动/交换 from={moveFrom} to={moveTo} 结果={ok}");
            Repaint();
        }
    }

    private void DrawSlotsPanel()
    {
        EditorGUILayout.LabelField("当前背包数据", EditorStyles.boldLabel);

        int count = InventoryShortcutRuntimeBinder.BackpackSlotCount;
        if (count <= 0)
        {
            EditorGUILayout.HelpBox("未检测到背包槽位数据。", MessageType.Warning);
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
                ? $"[{i}]（空）"
                : $"[{i}] {slot.itemId} 数量:{slot.count} 单格上限:{slot.maxStack}";

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
        popupIndex = EditorGUILayout.Popup("武器分类", popupIndex, labels);
        weaponCategory = ItemEditorLabels.FromWeaponCategoryPopupIndex(equipmentSlot, popupIndex);
    }

    private static string[] BuildItemOptions(List<ItemDatabase.ItemEntry> entries)
    {
        string[] options = new string[entries.Count];
        for (int i = 0; i < entries.Count; i++)
        {
            ItemDatabase.ItemEntry entry = entries[i];
            options[i] = entry != null ? entry.itemId : "（空）";
        }

        return options;
    }
}

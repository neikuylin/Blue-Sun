using UnityEditor;
using UnityEngine;

public class InventoryDebugWindow : EditorWindow
{
    private string itemId = "test_item";
    private Sprite itemSprite;
    private int addCount = 1;
    private int maxStack = 99;

    private int removeSlot = 0;
    private int removeCount = 1;

    private int moveFrom = 0;
    private int moveTo = 1;

    private Vector2 scroll;

    [MenuItem("Tools/背包/调试窗口")]
    private static void Open()
    {
        GetWindow<InventoryDebugWindow>("背包调试");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("背包调试器", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("请先进入 Play 模式，再使用本窗口。", MessageType.Info);
            return;
        }

        DrawAddPanel();
        EditorGUILayout.Space(8);
        DrawRemovePanel();
        EditorGUILayout.Space(8);
        DrawMovePanel();
        EditorGUILayout.Space(10);
        DrawSlotsPanel();
    }

    private void DrawAddPanel()
    {
        EditorGUILayout.LabelField("塞入物品", EditorStyles.boldLabel);
        itemId = EditorGUILayout.TextField("物品ID", itemId);
        itemSprite = (Sprite)EditorGUILayout.ObjectField("图标", itemSprite, typeof(Sprite), false);
        addCount = Mathf.Max(1, EditorGUILayout.IntField("数量", addCount));
        maxStack = Mathf.Max(1, EditorGUILayout.IntField("单格上限", maxStack));

        using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(itemId) || itemSprite == null))
        {
            if (GUILayout.Button("添加物品"))
            {
                int remain = InventoryShortcutRuntimeBinder.AddItem(itemId, itemSprite, addCount, maxStack);
                Debug.Log($"[背包调试] 添加完成，剩余未放入数量={remain}");
                Repaint();
            }
        }
    }

    private void DrawRemovePanel()
    {
        EditorGUILayout.LabelField("移除物品", EditorStyles.boldLabel);
        int maxIndex = Mathf.Max(0, InventoryShortcutRuntimeBinder.WarehouseSlotCount - 1);
        removeSlot = Mathf.Clamp(EditorGUILayout.IntField("格子索引", removeSlot), 0, maxIndex);
        removeCount = Mathf.Max(1, EditorGUILayout.IntField("移除数量", removeCount));

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("移除"))
            {
                bool ok = InventoryShortcutRuntimeBinder.RemoveItemAt(removeSlot, removeCount);
                Debug.Log($"[背包调试] 移除  索引={removeSlot} 数量={removeCount} 结果={ok}");
                Repaint();
            }

            if (GUILayout.Button("清空此格"))
            {
                bool ok = InventoryShortcutRuntimeBinder.RemoveItemAt(removeSlot, int.MaxValue);
                Debug.Log($"[背包调试] 清空格子  索引={removeSlot} 结果={ok}");
                Repaint();
            }
        }
    }

    private void DrawMovePanel()
    {
        EditorGUILayout.LabelField("移动/交换", EditorStyles.boldLabel);
        int maxIndex = Mathf.Max(0, InventoryShortcutRuntimeBinder.WarehouseSlotCount - 1);
        moveFrom = Mathf.Clamp(EditorGUILayout.IntField("起始格", moveFrom), 0, maxIndex);
        moveTo = Mathf.Clamp(EditorGUILayout.IntField("目标格", moveTo), 0, maxIndex);

        if (GUILayout.Button("执行移动/交换"))
        {
            bool ok = InventoryShortcutRuntimeBinder.MoveItem(moveFrom, moveTo);
            Debug.Log($"[背包调试] 移动/交换  from={moveFrom} to={moveTo} 结果={ok}");
            Repaint();
        }
    }

    private void DrawSlotsPanel()
    {
        EditorGUILayout.LabelField("当前仓库数据", EditorStyles.boldLabel);

        int count = InventoryShortcutRuntimeBinder.WarehouseSlotCount;
        if (count <= 0)
        {
            EditorGUILayout.HelpBox("未检测到仓库槽位数据。", MessageType.Warning);
            return;
        }

        scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(260));
        for (int i = 0; i < count; i++)
        {
            if (!InventoryShortcutRuntimeBinder.TryGetWarehouseSlotData(i, out var slot))
            {
                continue;
            }

            string text = slot.IsEmpty
                ? $"[{i}] （空）"
                : $"[{i}] {slot.itemId}  数量:{slot.count}  单格上限:{slot.maxStack}";

            EditorGUILayout.LabelField(text);
        }

        EditorGUILayout.EndScrollView();
    }
}

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(装备武器测试))]
public sealed class 装备武器测试编辑器 : Editor
{
    private SerializedProperty 物品数据库;
    private SerializedProperty 武器物品ID;
    private SerializedProperty 直接武器模型预制体;
    private SerializedProperty 直接武器类型;
    private SerializedProperty 应用战斗黑色描边;

    private void OnEnable()
    {
        物品数据库 = serializedObject.FindProperty("物品数据库");
        武器物品ID = serializedObject.FindProperty("武器物品ID");
        直接武器模型预制体 = serializedObject.FindProperty("直接武器模型预制体");
        直接武器类型 = serializedObject.FindProperty("直接武器类型");
        应用战斗黑色描边 = serializedObject.FindProperty("应用战斗黑色描边");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        绘制武器选择();
        绘制操作按钮();

        serializedObject.ApplyModifiedProperties();
    }

    private void 绘制武器选择()
    {
        EditorGUILayout.LabelField("战斗武器预览", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(物品数据库, new GUIContent("物品数据库"));

        ItemDatabase database = 物品数据库.objectReferenceValue as ItemDatabase;
        if (database == null)
        {
            database = ItemDatabase.LoadDefault();
            if (database != null)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.ObjectField("自动使用数据库", database, typeof(ItemDatabase), false);
                }
            }
        }

        绘制数据库武器下拉(database);
        EditorGUILayout.Space(6f);
        EditorGUILayout.PropertyField(直接武器模型预制体, new GUIContent("直接武器模型预制体"));
        EditorGUILayout.PropertyField(直接武器类型, new GUIContent("直接武器类型"));
        EditorGUILayout.PropertyField(应用战斗黑色描边, new GUIContent("应用战斗黑色描边"));

        if (直接武器模型预制体.objectReferenceValue != null)
        {
            EditorGUILayout.HelpBox("已指定直接武器模型预制体，生成时会优先使用它，不读取上面的数据库武器。", MessageType.Info);
        }
    }

    private void 绘制数据库武器下拉(ItemDatabase database)
    {
        if (database == null)
        {
            EditorGUILayout.HelpBox("没有指定物品数据库，也没有找到 Resources/ItemDatabase。", MessageType.Warning);
            EditorGUILayout.PropertyField(武器物品ID, new GUIContent("武器物品ID"));
            return;
        }

        List<ItemDatabase.ItemEntry> entries = 取得可预览武器(database);
        if (entries.Count == 0)
        {
            EditorGUILayout.HelpBox("物品数据库里没有可预览的武器模型。", MessageType.Warning);
            EditorGUILayout.PropertyField(武器物品ID, new GUIContent("武器物品ID"));
            return;
        }

        string[] options = new string[entries.Count + 1];
        options[0] = "不从数据库选择";
        int currentIndex = 0;
        for (int i = 0; i < entries.Count; i++)
        {
            ItemDatabase.ItemEntry entry = entries[i];
            options[i + 1] = string.IsNullOrWhiteSpace(entry.displayName)
                ? entry.itemId
                : $"{entry.displayName} ({entry.itemId})";

            if (entry.itemId == 武器物品ID.stringValue)
            {
                currentIndex = i + 1;
            }
        }

        int nextIndex = EditorGUILayout.Popup("数据库武器", currentIndex, options);
        if (nextIndex != currentIndex)
        {
            武器物品ID.stringValue = nextIndex <= 0 ? string.Empty : entries[nextIndex - 1].itemId;
        }

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.PropertyField(武器物品ID, new GUIContent("当前武器物品ID"));
        }
    }

    private void 绘制操作按钮()
    {
        EditorGUILayout.Space(8f);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("生成测试装备"))
            {
                serializedObject.ApplyModifiedProperties();
                生成测试装备();
            }

            if (GUILayout.Button("清理测试装备"))
            {
                serializedObject.ApplyModifiedProperties();
                清理测试装备();
            }
        }
    }

    private void 生成测试装备()
    {
        装备武器测试 tester = (装备武器测试)target;
        Undo.RegisterFullObjectHierarchyUndo(tester.gameObject, "生成测试装备");

        bool success = tester.生成测试装备(out string result);
        标记对象已修改(tester);

        if (success)
        {
            Debug.Log(result, tester.gameObject);
            return;
        }

        Debug.LogError(result, tester.gameObject);
    }

    private void 清理测试装备()
    {
        装备武器测试 tester = (装备武器测试)target;
        Undo.RegisterFullObjectHierarchyUndo(tester.gameObject, "清理测试装备");
        tester.清理测试装备();
        标记对象已修改(tester);
        Debug.Log("已清理测试装备。", tester.gameObject);
    }

    private static List<ItemDatabase.ItemEntry> 取得可预览武器(ItemDatabase database)
    {
        List<ItemDatabase.ItemEntry> result = new List<ItemDatabase.ItemEntry>();
        if (database == null || database.Entries == null)
        {
            return result;
        }

        for (int i = 0; i < database.Entries.Count; i++)
        {
            ItemDatabase.ItemEntry entry = database.Entries[i];
            if (entry == null ||
                entry.category != ItemDatabase.ItemCategory.Equipment ||
                entry.weaponModelPrefab == null ||
                !ItemDatabase.SupportsWeaponModelPrefab(entry.equipmentSlot))
            {
                continue;
            }

            result.Add(entry);
        }

        return result;
    }

    private static void 标记对象已修改(装备武器测试 tester)
    {
        EditorUtility.SetDirty(tester);
        EditorUtility.SetDirty(tester.gameObject);

        if (PrefabUtility.IsPartOfPrefabInstance(tester.gameObject))
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(tester);
            PrefabUtility.RecordPrefabInstancePropertyModifications(tester.gameObject);
        }
    }
}

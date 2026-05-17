using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(装备武器测试))]
public sealed class 装备武器测试编辑器 : Editor
{
    private SerializedProperty 物品数据库;
    private SerializedProperty 战斗角色绑定库;
    private SerializedProperty 角色ID;
    private SerializedProperty 武器物品ID;
    private SerializedProperty 直接武器模型预制体;
    private SerializedProperty 直接武器类型;
    private SerializedProperty 生成前应用战斗模型倍率;
    private SerializedProperty 应用战斗黑色描边;

    private void OnEnable()
    {
        物品数据库 = serializedObject.FindProperty("物品数据库");
        战斗角色绑定库 = serializedObject.FindProperty("战斗角色绑定库");
        角色ID = serializedObject.FindProperty("角色ID");
        武器物品ID = serializedObject.FindProperty("武器物品ID");
        直接武器模型预制体 = serializedObject.FindProperty("直接武器模型预制体");
        直接武器类型 = serializedObject.FindProperty("直接武器类型");
        生成前应用战斗模型倍率 = serializedObject.FindProperty("生成前应用战斗模型倍率");
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
        绘制角色选择();
        EditorGUILayout.Space(6f);
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
        EditorGUILayout.PropertyField(生成前应用战斗模型倍率, new GUIContent("生成前应用战斗模型倍率"));
        EditorGUILayout.PropertyField(应用战斗黑色描边, new GUIContent("应用战斗黑色描边"));

        if (直接武器模型预制体.objectReferenceValue != null)
        {
            EditorGUILayout.HelpBox("已指定直接武器模型预制体，生成时会优先使用它，不读取上面的数据库武器。", MessageType.Info);
        }
    }

    private void 绘制角色选择()
    {
        EditorGUILayout.PropertyField(战斗角色绑定库, new GUIContent("战斗角色绑定库"));

        BattleCharacterBindingDatabase database = 战斗角色绑定库.objectReferenceValue as BattleCharacterBindingDatabase;
        if (database == null)
        {
            database = BattleCharacterBindingDatabase.LoadDefault();
            if (database != null)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.ObjectField("自动使用绑定库", database, typeof(BattleCharacterBindingDatabase), false);
                }
            }
        }

        绘制角色下拉(database);
    }

    private void 绘制角色下拉(BattleCharacterBindingDatabase database)
    {
        if (database == null)
        {
            EditorGUILayout.HelpBox("没有指定战斗角色绑定库，也没有找到 Resources/BattleCharacterBindings。", MessageType.Warning);
            EditorGUILayout.PropertyField(角色ID, new GUIContent("角色ID"));
            return;
        }

        List<BattleCharacterBindingDatabase.BindingEntry> entries = 取得可用角色绑定(database);
        if (entries.Count == 0)
        {
            EditorGUILayout.HelpBox("战斗角色绑定库里没有可用角色。", MessageType.Warning);
            EditorGUILayout.PropertyField(角色ID, new GUIContent("角色ID"));
            return;
        }

        string[] options = new string[entries.Count + 1];
        options[0] = "不应用角色倍率";
        int currentIndex = 0;
        for (int i = 0; i < entries.Count; i++)
        {
            BattleCharacterBindingDatabase.BindingEntry entry = entries[i];
            string displayName = string.IsNullOrWhiteSpace(entry.displayName) ? entry.characterId : entry.displayName;
            options[i + 1] = $"{displayName} ({entry.characterId})  倍率 {entry.modelScale}";

            if (entry.characterId == 角色ID.stringValue)
            {
                currentIndex = i + 1;
            }
        }

        int nextIndex = EditorGUILayout.Popup("战斗角色", currentIndex, options);
        if (nextIndex != currentIndex)
        {
            角色ID.stringValue = nextIndex <= 0 ? string.Empty : entries[nextIndex - 1].characterId;
        }

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.PropertyField(角色ID, new GUIContent("当前角色ID"));
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

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("应用战斗模型倍率"))
            {
                serializedObject.ApplyModifiedProperties();
                应用战斗模型倍率();
            }

            if (GUILayout.Button("清理并恢复缩放"))
            {
                serializedObject.ApplyModifiedProperties();
                清理并恢复缩放();
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

    private void 应用战斗模型倍率()
    {
        装备武器测试 tester = (装备武器测试)target;
        Undo.RegisterFullObjectHierarchyUndo(tester.gameObject, "应用战斗模型倍率");

        bool success = tester.应用战斗模型倍率(out string result);
        标记对象已修改(tester);

        if (success)
        {
            Debug.Log(result, tester.gameObject);
            return;
        }

        Debug.LogError(result, tester.gameObject);
    }

    private void 清理并恢复缩放()
    {
        装备武器测试 tester = (装备武器测试)target;
        Undo.RegisterFullObjectHierarchyUndo(tester.gameObject, "清理测试装备并恢复缩放");
        tester.清理测试装备并恢复缩放();
        标记对象已修改(tester);
        Debug.Log("已清理测试装备并恢复原始缩放。", tester.gameObject);
    }

    private static List<BattleCharacterBindingDatabase.BindingEntry> 取得可用角色绑定(BattleCharacterBindingDatabase database)
    {
        List<BattleCharacterBindingDatabase.BindingEntry> result = new List<BattleCharacterBindingDatabase.BindingEntry>();
        if (database == null || database.Entries == null)
        {
            return result;
        }

        for (int i = 0; i < database.Entries.Count; i++)
        {
            BattleCharacterBindingDatabase.BindingEntry entry = database.Entries[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.characterId))
            {
                continue;
            }

            result.Add(entry);
        }

        return result;
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

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class 效果特效全局配置编辑器工具
{
    private const string ConfigAssetPath = "Assets/Resources/效果特效全局配置.asset";

    private static readonly List<string> 效果ID列表 = new List<string>();
    private static readonly List<string> 效果显示列表 = new List<string>();

    public static 效果特效全局配置 读取全局配置()
    {
        return AssetDatabase.LoadAssetAtPath<效果特效全局配置>(ConfigAssetPath);
    }

    public static void 绘制绑定列表(string 标题, string 列表字段名, bool 要求武器特效接口, bool 要求效果特效接口)
    {
        效果特效全局配置 config = 读取全局配置();
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.ObjectField("全局配置", config, typeof(效果特效全局配置), false);
        }

        if (config == null)
        {
            EditorGUILayout.HelpBox($"缺少全局配置：{ConfigAssetPath}", MessageType.Error);
            return;
        }

        SerializedObject configObject = new SerializedObject(config);
        configObject.Update();

        SerializedProperty listProperty = configObject.FindProperty(列表字段名);
        if (listProperty == null)
        {
            EditorGUILayout.HelpBox($"全局配置缺少字段：{列表字段名}", MessageType.Error);
            return;
        }

        刷新效果选项();
        EditorGUILayout.LabelField(标题, EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        绘制条目列表(listProperty, 要求武器特效接口, 要求效果特效接口);

        bool changedByButton = false;
        if (GUILayout.Button("新增效果特效"))
        {
            int index = listProperty.arraySize;
            listProperty.InsertArrayElementAtIndex(index);
            SerializedProperty entry = listProperty.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("效果ID").stringValue = 效果ID列表.Count > 0 ? 效果ID列表[0] : string.Empty;
            entry.FindPropertyRelative("特效脚本类型名").stringValue = string.Empty;
            changedByButton = true;
        }

        bool changed = EditorGUI.EndChangeCheck() || changedByButton;
        configObject.ApplyModifiedProperties();
        if (changed)
        {
            EditorUtility.SetDirty(config);
            刷新场景桥接器();
        }
    }

    private static void 刷新场景桥接器()
    {
        效果特效状态启用脚本[] modelBridges = UnityEngine.Object.FindObjectsOfType<效果特效状态启用脚本>(true);
        for (int i = 0; i < modelBridges.Length; i++)
        {
            if (modelBridges[i] != null)
            {
                modelBridges[i].刷新效果特效状态();
            }
        }

        武器特效状态桥接器[] weaponBridges = UnityEngine.Object.FindObjectsOfType<武器特效状态桥接器>(true);
        for (int i = 0; i < weaponBridges.Length; i++)
        {
            if (weaponBridges[i] != null)
            {
                weaponBridges[i].刷新武器特效状态();
            }
        }
    }

    private static void 绘制条目列表(SerializedProperty listProperty, bool 要求武器特效接口, bool 要求效果特效接口)
    {
        for (int i = 0; i < listProperty.arraySize; i++)
        {
            SerializedProperty entry = listProperty.GetArrayElementAtIndex(i);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"特效 {i + 1}", EditorStyles.boldLabel);
                    if (GUILayout.Button("删除", GUILayout.Width(60f)))
                    {
                        listProperty.DeleteArrayElementAtIndex(i);
                        break;
                    }
                }

                绘制效果ID(entry.FindPropertyRelative("效果ID"));
                绘制脚本类型(entry.FindPropertyRelative("特效脚本类型名"), 要求武器特效接口, 要求效果特效接口);
                SerializedProperty previewProperty = entry.FindPropertyRelative("预览启用");
                if (previewProperty != null)
                {
                    EditorGUILayout.PropertyField(previewProperty, new GUIContent("预览启用"));
                }
            }
        }
    }

    private static void 绘制效果ID(SerializedProperty effectIdProperty)
    {
        if (effectIdProperty == null)
        {
            EditorGUILayout.HelpBox("缺少字段：效果ID", MessageType.Error);
            return;
        }

        if (效果ID列表.Count == 0)
        {
            EditorGUILayout.PropertyField(effectIdProperty, new GUIContent("效果ID"));
            EditorGUILayout.HelpBox("效果库为空或找不到 Resources/EffectDatabase。", MessageType.Warning);
            return;
        }

        int selectedIndex = 效果ID列表.IndexOf(effectIdProperty.stringValue);
        if (selectedIndex < 0)
        {
            selectedIndex = 0;
        }

        selectedIndex = EditorGUILayout.Popup("效果ID", selectedIndex, 效果显示列表.ToArray());
        effectIdProperty.stringValue = 效果ID列表[Mathf.Clamp(selectedIndex, 0, 效果ID列表.Count - 1)];
    }

    private static void 绘制脚本类型(SerializedProperty scriptTypeProperty, bool 要求武器特效接口, bool 要求效果特效接口)
    {
        if (scriptTypeProperty == null)
        {
            EditorGUILayout.HelpBox("缺少字段：特效脚本类型名", MessageType.Error);
            return;
        }

        MonoScript currentScript = ResolveMonoScript(scriptTypeProperty.stringValue);
        MonoScript nextScript = EditorGUILayout.ObjectField("特效脚本", currentScript, typeof(MonoScript), false) as MonoScript;
        if (nextScript != currentScript)
        {
            Type type = nextScript != null ? nextScript.GetClass() : null;
            scriptTypeProperty.stringValue = type != null ? $"{type.FullName}, {type.Assembly.GetName().Name}" : string.Empty;
        }

        绘制脚本校验(scriptTypeProperty.stringValue, 要求武器特效接口, 要求效果特效接口);
    }

    private static void 绘制脚本校验(string scriptTypeName, bool 要求武器特效接口, bool 要求效果特效接口)
    {
        if (string.IsNullOrWhiteSpace(scriptTypeName))
        {
            EditorGUILayout.HelpBox("请选择一个特效脚本。", MessageType.Warning);
            return;
        }

        Type type = 解析特效脚本类型(scriptTypeName);
        if (type == null)
        {
            EditorGUILayout.HelpBox($"找不到脚本类型：{scriptTypeName}", MessageType.Warning);
            return;
        }

        if (!typeof(MonoBehaviour).IsAssignableFrom(type))
        {
            EditorGUILayout.HelpBox("选择的脚本不是 MonoBehaviour，运行时不能作为组件添加。", MessageType.Warning);
            return;
        }

        if (要求武器特效接口 && !typeof(武器特效开关接口).IsAssignableFrom(type))
        {
            EditorGUILayout.HelpBox("武器特效脚本必须实现“武器特效开关接口”。", MessageType.Warning);
            return;
        }

        if (要求效果特效接口 && !typeof(效果特效开关接口).IsAssignableFrom(type))
        {
            EditorGUILayout.HelpBox("模型状态特效脚本必须实现“效果特效开关接口”。", MessageType.Warning);
            return;
        }

        EditorGUILayout.HelpBox($"已绑定脚本：{type.Name}", MessageType.Info);
    }

    private static MonoScript ResolveMonoScript(string scriptTypeName)
    {
        if (string.IsNullOrWhiteSpace(scriptTypeName))
        {
            return null;
        }

        Type type = 解析特效脚本类型(scriptTypeName);
        if (type == null)
        {
            return null;
        }

        string[] guids = AssetDatabase.FindAssets("t:MonoScript");
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
            if (script == null || script.GetClass() != type)
            {
                continue;
            }

            return script;
        }

        return null;
    }

    private static Type 解析特效脚本类型(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
        {
            return null;
        }

        string normalized = typeName.Replace("\r", string.Empty).Replace("\n", string.Empty).Trim();
        while (normalized.Contains("  "))
        {
            normalized = normalized.Replace("  ", " ");
        }

        return Type.GetType(normalized);
    }

    private static void 刷新效果选项()
    {
        效果ID列表.Clear();
        效果显示列表.Clear();

        EffectDatabase database = EffectDatabase.LoadDefault();
        if (database == null || database.Entries == null)
        {
            return;
        }

        for (int i = 0; i < database.Entries.Count; i++)
        {
            EffectDatabase.EffectEntry entry = database.Entries[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.effectId))
            {
                continue;
            }

            效果ID列表.Add(entry.effectId);
            string displayName = string.IsNullOrWhiteSpace(entry.displayName) ? entry.effectId : entry.displayName;
            效果显示列表.Add($"{displayName} ({entry.effectId})");
        }
    }
}

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(武器特效状态桥接器))]
public sealed class 武器特效状态桥接器编辑器 : Editor
{
    private readonly List<string> 效果ID列表 = new List<string>();
    private readonly List<string> 效果显示列表 = new List<string>();

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("效果驱动武器特效", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("先把武器特效脚本拖进来。角色身上拥有指定效果时，启用绑定脚本；效果消失时关闭。拖入的脚本必须实现“武器特效开关接口”。", MessageType.Info);

        SerializedProperty listProperty = serializedObject.FindProperty("效果驱动特效列表");
        if (listProperty == null)
        {
            EditorGUILayout.HelpBox("缺少字段：效果驱动特效列表", MessageType.Error);
            serializedObject.ApplyModifiedProperties();
            return;
        }

        刷新效果选项();
        DrawEntries(listProperty);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("新增效果特效"))
            {
                int index = listProperty.arraySize;
                listProperty.InsertArrayElementAtIndex(index);
                SerializedProperty entry = listProperty.GetArrayElementAtIndex(index);
                entry.FindPropertyRelative("效果ID").stringValue = 效果ID列表.Count > 0 ? 效果ID列表[0] : string.Empty;
                entry.FindPropertyRelative("特效脚本").objectReferenceValue = null;
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawEntries(SerializedProperty listProperty)
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

                DrawEffectId(entry.FindPropertyRelative("效果ID"));

                SerializedProperty scriptProperty = entry.FindPropertyRelative("特效脚本");
                EditorGUILayout.PropertyField(scriptProperty, new GUIContent("特效脚本"));
                DrawScriptValidation(scriptProperty);
            }
        }
    }

    private void DrawEffectId(SerializedProperty effectIdProperty)
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

    private static void DrawScriptValidation(SerializedProperty scriptProperty)
    {
        if (scriptProperty == null)
        {
            return;
        }

        MonoBehaviour script = scriptProperty.objectReferenceValue as MonoBehaviour;
        if (script == null)
        {
            EditorGUILayout.HelpBox("请拖入一个武器特效脚本。", MessageType.Warning);
            return;
        }

        if (script is 武器特效开关接口)
        {
            EditorGUILayout.HelpBox("已接入：这个脚本可以被效果开关控制。", MessageType.Info);
            return;
        }

        EditorGUILayout.HelpBox("这个脚本没有实现“武器特效开关接口”，运行时不会被桥接器控制。", MessageType.Warning);
    }

    private void 刷新效果选项()
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

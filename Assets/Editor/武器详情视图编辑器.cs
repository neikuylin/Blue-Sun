using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(武器详情视图))]
public sealed class 武器详情视图编辑器 : Editor
{
    private static readonly string[] 字段 =
    {
        "背景图",
        "物品图标",
        "物品名字文本",
        "品质文本",
        "武器分类文本",
        "装备者文本",
        "攻击力文本",
        "固定伤害文本",
        "属性加成文本",
        "文本介绍文本",
        "附带技能文本",
        "附带技能图标区域",
        "下背景",
        "下文本内容",
        "展开提示"
    };

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.HelpBox("这个组件只负责单/双手武器详情 prefab 的显示绑定。悬停触发仍由仓库/背包格子负责。", MessageType.Info);

        for (int i = 0; i < 字段.Length; i++)
        {
            SerializedProperty property = serializedObject.FindProperty(字段[i]);
            if (property == null)
            {
                continue;
            }

            EditorGUILayout.PropertyField(property, new GUIContent(字段[i]));
            if (property.objectReferenceValue == null && 字段[i] != "固定伤害文本")
            {
                EditorGUILayout.HelpBox($"缺少绑定：{字段[i]}", MessageType.Warning);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(战技内容视图))]
public sealed class 战技内容视图编辑器 : Editor
{
    private static readonly string[] 字段 =
    {
        "战技图标",
        "战技名字文本",
        "命中率文本",
        "战技伤害文本",
        "战技描述文本",
        "使用者文本"
    };

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.HelpBox("这个组件只负责战技内容 prefab 的显示绑定。悬停触发和数据读取由运行时负责。", MessageType.Info);

        for (int i = 0; i < 字段.Length; i++)
        {
            SerializedProperty property = serializedObject.FindProperty(字段[i]);
            if (property == null)
            {
                continue;
            }

            EditorGUILayout.PropertyField(property, new GUIContent(字段[i]));
            if (property.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox($"缺少绑定：{字段[i]}", MessageType.Warning);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}

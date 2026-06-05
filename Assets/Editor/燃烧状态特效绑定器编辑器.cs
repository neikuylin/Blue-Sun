using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(燃烧状态特效绑定器))]
public sealed class 燃烧状态特效绑定器编辑器 : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("燃烧状态特效", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("挂到角色子物体后，会读取父级 BattleUnit 的 SkinnedMeshRenderer，并从角色模型表面发射小而散的火焰粒子。", MessageType.Info);

        DrawProperty("火焰材质", "火焰材质");
        DrawProperty("火焰数量", "每秒火焰数量");
        DrawProperty("最大粒子数", "最大粒子数");
        DrawProperty("火焰大小", "火焰大小");
        DrawProperty("火焰大小浮动", "火焰大小浮动");
        DrawProperty("火焰生命周期", "火焰生命周期");
        DrawProperty("上飘速度", "上飘速度");
        DrawProperty("表面散布厚度", "表面散布厚度");
        DrawProperty("火焰颜色", "火焰颜色");

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawProperty(string propertyName, string label)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            EditorGUILayout.HelpBox($"缺少字段：{propertyName}", MessageType.Error);
            return;
        }

        EditorGUILayout.PropertyField(property, new GUIContent(label));
    }
}

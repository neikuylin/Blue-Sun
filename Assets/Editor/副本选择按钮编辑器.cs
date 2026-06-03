using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(副本选择按钮))]
public sealed class 副本选择按钮编辑器 : Editor
{
    private SerializedProperty 地图模板ID;

    private void OnEnable()
    {
        地图模板ID = serializedObject.FindProperty("地图模板ID");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(地图模板ID, new GUIContent("地图模板ID"));

        string resolvedId = string.IsNullOrWhiteSpace(地图模板ID.stringValue) ? string.Empty : 地图模板ID.stringValue.Trim();
        if (string.IsNullOrEmpty(resolvedId))
        {
            EditorGUILayout.HelpBox("没有填写地图模板ID，点击时不会记录副本选择。", MessageType.Warning);
        }
        else
        {
            MapTemplateDatabase database = MapTemplateDatabase.LoadDefault();
            if (database == null || database.FindEntry(resolvedId) == null)
            {
                EditorGUILayout.HelpBox($"地图模板库里找不到模板ID：{resolvedId}", MessageType.Warning);
            }
        }

        EditorGUILayout.HelpBox("把按钮 OnClick 同时绑定本组件的“选择副本”和场景切换器。选择副本会记录地图模板，并重置该副本运行状态。", MessageType.Info);

        serializedObject.ApplyModifiedProperties();
    }
}

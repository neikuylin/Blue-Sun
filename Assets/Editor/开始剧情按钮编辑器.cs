using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(开始剧情按钮))]
public sealed class 开始剧情按钮编辑器 : Editor
{
    private SerializedProperty 剧情ID属性;

    private void OnEnable()
    {
        剧情ID属性 = serializedObject.FindProperty("剧情ID");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        剧情数据库 database = 剧情数据库.加载默认数据库();
        List<string> storyIds = CollectStoryIds(database);
        if (storyIds.Count == 0)
        {
            EditorGUILayout.HelpBox("剧情数据库中没有可选择的剧情。", MessageType.Warning);
            EditorGUILayout.PropertyField(剧情ID属性, new GUIContent("剧情 ID"));
        }
        else
        {
            int selectedIndex = Mathf.Max(0, storyIds.IndexOf(剧情ID属性.stringValue));
            int newIndex = EditorGUILayout.Popup("进入剧情", selectedIndex, storyIds.ToArray());
            剧情ID属性.stringValue = storyIds[newIndex];
        }

        EditorGUILayout.HelpBox(
            "把 Button 的 On Click 绑定到“开始剧情按钮 -> 开始新游戏并播放剧情”。",
            MessageType.Info);

        serializedObject.ApplyModifiedProperties();
    }

    private static List<string> CollectStoryIds(剧情数据库 database)
    {
        List<string> result = new List<string>();
        if (database == null)
        {
            return result;
        }

        List<剧情数据库.剧情条目> stories = database.取得剧情列表();
        for (int i = 0; i < stories.Count; i++)
        {
            剧情数据库.剧情条目 story = stories[i];
            if (story == null || string.IsNullOrWhiteSpace(story.剧情ID))
            {
                continue;
            }

            result.Add(story.剧情ID);
        }

        return result;
    }
}

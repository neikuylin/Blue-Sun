using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(清空房间墙体动画控制器))]
public sealed class 清空房间墙体动画控制器编辑器 : Editor
{
    private SerializedProperty 进房间时倒放后要停在第一帧的动画;
    private SerializedProperty 进房间时播放的音频组件;
    private SerializedProperty 切房间时响应的房间按钮;
    private SerializedProperty 切房间时播放的正向音频组件;
    private SerializedProperty 切房间时关闭的物体;
    private SerializedProperty 清空房间后开启的物体;
    private SerializedProperty 清空房间后关闭的物体;

    private void OnEnable()
    {
        进房间时倒放后要停在第一帧的动画 = serializedObject.FindProperty("进房间时倒放后要停在第一帧的动画");
        进房间时播放的音频组件 = serializedObject.FindProperty("进房间时播放的音频组件");
        切房间时响应的房间按钮 = serializedObject.FindProperty("切房间时响应的房间按钮");
        切房间时播放的正向音频组件 = serializedObject.FindProperty("切房间时播放的正向音频组件");
        切房间时关闭的物体 = serializedObject.FindProperty("切房间时关闭的物体");
        清空房间后开启的物体 = serializedObject.FindProperty("清空房间后开启的物体");
        清空房间后关闭的物体 = serializedObject.FindProperty("清空房间后关闭的物体");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("进房间时", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(进房间时倒放后要停在第一帧的动画, new GUIContent("进房间时倒放后要停在第一帧的动画"), true);
        EditorGUILayout.PropertyField(进房间时播放的音频组件, new GUIContent("进房间时播放的音频组件"), true);

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("清空房间后", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(清空房间后开启的物体, new GUIContent("清空房间后开启的物体"), true);
        EditorGUILayout.PropertyField(清空房间后关闭的物体, new GUIContent("清空房间后关闭的物体"), true);

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("公开动作", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(切房间时响应的房间按钮, new GUIContent("切房间时响应的房间按钮"));
        if (切房间时响应的房间按钮.objectReferenceValue == null)
        {
            EditorGUILayout.HelpBox("未绑定房间方向按钮时，本组件不会响应切房间公开动作。进房间时和清空房间后的逻辑不受影响。", MessageType.Warning);
        }

        EditorGUILayout.PropertyField(切房间时播放的正向音频组件, new GUIContent("切房间时播放的正向音频组件"), true);
        EditorGUILayout.PropertyField(切房间时关闭的物体, new GUIContent("切房间时关闭的物体"), true);
        using (new EditorGUI.DisabledScope(true))
        {
            GUILayout.Button("播放切房间时正向动画");
            GUILayout.Button("播放切房间时正向音频");
        }

        EditorGUILayout.HelpBox("公开动作只响应这里绑定的房间方向按钮；进房间时和清空房间后的逻辑不受这个绑定影响。", MessageType.Info);

        serializedObject.ApplyModifiedProperties();
    }
}

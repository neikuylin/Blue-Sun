using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(清空房间墙体动画控制器))]
public sealed class 清空房间墙体动画控制器编辑器 : Editor
{
    private SerializedProperty 进房间时倒放后要停在第一帧的动画;
    private SerializedProperty 进房间时播放的音频组件;
    private SerializedProperty 清空房间后开启的物体;
    private SerializedProperty 清空房间后关闭的物体;
    private SerializedProperty 清空房间后开启的音频组件;

    private void OnEnable()
    {
        进房间时倒放后要停在第一帧的动画 = serializedObject.FindProperty("进房间时倒放后要停在第一帧的动画");
        进房间时播放的音频组件 = serializedObject.FindProperty("进房间时播放的音频组件");
        清空房间后开启的物体 = serializedObject.FindProperty("清空房间后开启的物体");
        清空房间后关闭的物体 = serializedObject.FindProperty("清空房间后关闭的物体");
        清空房间后开启的音频组件 = serializedObject.FindProperty("清空房间后开启的音频组件");
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
        EditorGUILayout.PropertyField(清空房间后开启的音频组件, new GUIContent("清空房间后开启的音频组件"), true);

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("公开动作", EditorStyles.boldLabel);
        using (new EditorGUI.DisabledScope(true))
        {
            GUILayout.Button("播放进房间时正向动画");
        }

        EditorGUILayout.HelpBox("这个动作只由门按钮流程调用：角色到达门口触发格后播放，播完再走向最终切换格。", MessageType.Info);

        serializedObject.ApplyModifiedProperties();
    }
}

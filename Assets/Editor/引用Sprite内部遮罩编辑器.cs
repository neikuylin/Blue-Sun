using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(引用Sprite内部遮罩))]
public sealed class 引用Sprite内部遮罩编辑器 : Editor
{
    private SerializedProperty 遮罩来源物体;
    private SerializedProperty 来源物体包含子物体Sprite;
    private SerializedProperty 目标SpriteRenderers;
    private SerializedProperty 目标为空时包含子物体;
    private SerializedProperty 包含未激活子物体;
    private SerializedProperty 同步来源位置旋转缩放;
    private SerializedProperty 只使用引用遮罩;
    private SerializedProperty 引用遮罩材质;
    private SerializedProperty 遮罩透明判定;
    private SerializedProperty 排序范围向前扩展;
    private SerializedProperty 排序范围向后扩展;

    private void OnEnable()
    {
        遮罩来源物体 = serializedObject.FindProperty("遮罩来源物体");
        来源物体包含子物体Sprite = serializedObject.FindProperty("来源物体包含子物体Sprite");
        目标SpriteRenderers = serializedObject.FindProperty("目标SpriteRenderers");
        目标为空时包含子物体 = serializedObject.FindProperty("目标为空时包含子物体");
        包含未激活子物体 = serializedObject.FindProperty("包含未激活子物体");
        同步来源位置旋转缩放 = serializedObject.FindProperty("同步来源位置旋转缩放");
        只使用引用遮罩 = serializedObject.FindProperty("只使用引用遮罩");
        引用遮罩材质 = serializedObject.FindProperty("引用遮罩材质");
        遮罩透明判定 = serializedObject.FindProperty("遮罩透明判定");
        排序范围向前扩展 = serializedObject.FindProperty("排序范围向前扩展");
        排序范围向后扩展 = serializedObject.FindProperty("排序范围向后扩展");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.HelpBox(
            "用途：挂在需要被限制显示的Sprite物体上，拖入另一个带SpriteRenderer的物体作为遮罩来源。来源物体不需要加组件。",
            MessageType.Info);

        EditorGUILayout.LabelField("遮罩来源", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(遮罩来源物体, new GUIContent("遮罩来源物体"));
        EditorGUILayout.PropertyField(来源物体包含子物体Sprite, new GUIContent("来源物体包含子物体Sprite"));

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("被遮罩目标", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(目标SpriteRenderers, new GUIContent("目标SpriteRenderer"), true);
        if (目标SpriteRenderers.arraySize == 0)
        {
            EditorGUILayout.PropertyField(目标为空时包含子物体, new GUIContent("目标为空时包含子物体"));
        }

        EditorGUILayout.PropertyField(包含未激活子物体, new GUIContent("包含未激活子物体"));

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("自动生成SpriteMask", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(只使用引用遮罩, new GUIContent("只使用引用遮罩"));
        if (只使用引用遮罩.boolValue)
        {
            EditorGUILayout.HelpBox(
                "开启后不会使用Unity的SpriteMaskInteraction，因此不会吃到其它非引用遮罩。目标会改用引用遮罩材质显示。",
                MessageType.Info);
            EditorGUILayout.PropertyField(引用遮罩材质, new GUIContent("引用遮罩材质"));
        }

        EditorGUILayout.PropertyField(遮罩透明判定, new GUIContent("遮罩透明判定"));
        EditorGUILayout.PropertyField(同步来源位置旋转缩放, new GUIContent("同步来源位置旋转缩放"));
        if (!只使用引用遮罩.boolValue)
        {
            EditorGUILayout.PropertyField(排序范围向前扩展, new GUIContent("排序范围向前扩展"));
            EditorGUILayout.PropertyField(排序范围向后扩展, new GUIContent("排序范围向后扩展"));
        }

        if (GUILayout.Button("立即应用引用遮罩"))
        {
            for (int i = 0; i < targets.Length; i++)
            {
                引用Sprite内部遮罩 controller = targets[i] as 引用Sprite内部遮罩;
                if (controller != null)
                {
                    controller.Apply();
                    EditorUtility.SetDirty(controller);
                }
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}

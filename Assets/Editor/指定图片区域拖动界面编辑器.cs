using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[CustomEditor(typeof(指定图片区域拖动界面))]
public sealed class 指定图片区域拖动界面编辑器 : Editor
{
    private SerializedProperty 拖动输入口;

    private void OnEnable()
    {
        拖动输入口 = serializedObject.FindProperty("拖动输入口");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("拖动设置", EditorStyles.boldLabel);
        指定图片区域拖动界面 组件 = target as 指定图片区域拖动界面;
        if (组件 != null && (组件.transform as RectTransform) == null)
        {
            EditorGUILayout.HelpBox("这个脚本需要挂在 UI 对象上，也就是带 RectTransform 的 GameObject。", MessageType.Warning);
        }

        EditorGUILayout.PropertyField(拖动输入口, new GUIContent("拖动输入口", "把带 Image 的背景框 GameObject 拖到这里。只有从这个图片区域按住拖动时，才会移动当前挂脚本的界面对象。"));
        绘制拖动输入口提示();

        serializedObject.ApplyModifiedProperties();
    }

    private void 绘制拖动输入口提示()
    {
        GameObject value = 拖动输入口.objectReferenceValue as GameObject;
        if (value == null)
        {
            EditorGUILayout.HelpBox("请拖入带 Image 的背景框 GameObject。", MessageType.Info);
            return;
        }

        if (value.GetComponent<Image>() == null)
        {
            EditorGUILayout.HelpBox("拖动输入口必须是带 Image 组件的 GameObject。", MessageType.Warning);
            return;
        }

        if ((value.transform as RectTransform) == null)
        {
            EditorGUILayout.HelpBox("拖动输入口必须是 UI 对象，需要带 RectTransform。", MessageType.Warning);
        }
    }
}

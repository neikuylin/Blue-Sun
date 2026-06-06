using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(效果特效状态启用脚本))]
public sealed class 效果特效状态启用脚本编辑器 : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("效果驱动模型特效", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("挂到角色模型物体上。绑定数据来自全局配置；角色拥有指定效果时自动添加并启用对应模型特效组件，效果消失时关闭组件。", MessageType.Info);
        效果特效全局配置编辑器工具.绘制绑定列表("模型特效全局绑定", "模型特效绑定列表", false, true);

        serializedObject.ApplyModifiedProperties();
    }
}

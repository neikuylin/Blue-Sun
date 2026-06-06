using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(武器特效状态桥接器))]
public sealed class 武器特效状态桥接器编辑器 : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("效果驱动武器特效", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("挂到武器物体上。绑定数据来自全局配置；角色拥有指定效果时自动添加并启用对应武器特效组件，效果消失时关闭。武器特效脚本必须实现“武器特效开关接口”。", MessageType.Info);
        效果特效全局配置编辑器工具.绘制绑定列表("武器特效全局绑定", "武器特效绑定列表", true, false);

        serializedObject.ApplyModifiedProperties();
    }
}

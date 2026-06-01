using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(装备面板绑定))]
public sealed class 装备面板绑定编辑器 : Editor
{
    private SerializedProperty equipmentContainer;
    private SerializedProperty 主手栏位;
    private SerializedProperty 副手栏位;
    private SerializedProperty 头盔栏位;
    private SerializedProperty 胸甲栏位;
    private SerializedProperty 手套栏位;
    private SerializedProperty 鞋子栏位;
    private SerializedProperty 腿甲栏位;
    private SerializedProperty 饰品栏位;
    private SerializedProperty returnTarget;
    private SerializedProperty 不可拖入变暗颜色;

    private void OnEnable()
    {
        equipmentContainer = serializedObject.FindProperty("equipmentContainer");
        主手栏位 = serializedObject.FindProperty("主手栏位");
        副手栏位 = serializedObject.FindProperty("副手栏位");
        头盔栏位 = serializedObject.FindProperty("头盔栏位");
        胸甲栏位 = serializedObject.FindProperty("胸甲栏位");
        手套栏位 = serializedObject.FindProperty("手套栏位");
        鞋子栏位 = serializedObject.FindProperty("鞋子栏位");
        腿甲栏位 = serializedObject.FindProperty("腿甲栏位");
        饰品栏位 = serializedObject.FindProperty("饰品栏位");
        returnTarget = serializedObject.FindProperty("returnTarget");
        不可拖入变暗颜色 = serializedObject.FindProperty("不可拖入变暗颜色");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("装备栏位绑定", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(equipmentContainer, new GUIContent("装备容器"));
        EditorGUILayout.PropertyField(主手栏位, new GUIContent("主手栏位"));
        EditorGUILayout.PropertyField(副手栏位, new GUIContent("副手栏位"));
        EditorGUILayout.PropertyField(头盔栏位, new GUIContent("头盔栏位"));
        EditorGUILayout.PropertyField(胸甲栏位, new GUIContent("胸甲栏位"));
        EditorGUILayout.PropertyField(手套栏位, new GUIContent("手套栏位"));
        EditorGUILayout.PropertyField(鞋子栏位, new GUIContent("鞋子栏位"));
        EditorGUILayout.PropertyField(腿甲栏位, new GUIContent("腿甲栏位"));
        EditorGUILayout.PropertyField(饰品栏位, new GUIContent("饰品栏位"));
        EditorGUILayout.PropertyField(returnTarget, new GUIContent("回流目标"));

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("拖拽提示", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(不可拖入变暗颜色, new GUIContent("不可拖入变暗颜色"));
        EditorGUILayout.HelpBox("拖动装备或武器时，不兼容的装备栏位以及栏位内已有图片会变成这个颜色；拖拽结束后恢复原颜色。", MessageType.Info);

        serializedObject.ApplyModifiedProperties();
    }
}

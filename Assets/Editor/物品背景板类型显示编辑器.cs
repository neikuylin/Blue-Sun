using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[CustomEditor(typeof(物品背景板类型显示))]
[CanEditMultipleObjects]
public sealed class 物品背景板类型显示编辑器 : Editor
{
    private SerializedProperty 类型文字对象;
    private SerializedProperty 技能仓库入口对象;
    private SerializedProperty 属性入口对象;
    private SerializedProperty 物品区域绑定对象;
    private SerializedProperty 仓库图片对象;
    private SerializedProperty 技能仓库图片对象;
    private SerializedProperty 属性图片对象;
    private SerializedProperty 背包图片对象;
    private SerializedProperty 宝箱图片对象;

    private void OnEnable()
    {
        类型文字对象 = serializedObject.FindProperty("类型文字对象");
        技能仓库入口对象 = serializedObject.FindProperty("技能仓库入口对象");
        属性入口对象 = serializedObject.FindProperty("属性入口对象");
        物品区域绑定对象 = serializedObject.FindProperty("物品区域绑定对象");
        仓库图片对象 = serializedObject.FindProperty("仓库图片对象");
        技能仓库图片对象 = serializedObject.FindProperty("技能仓库图片对象");
        属性图片对象 = serializedObject.FindProperty("属性图片对象");
        背包图片对象 = serializedObject.FindProperty("背包图片对象");
        宝箱图片对象 = serializedObject.FindProperty("宝箱图片对象");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("文字", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(类型文字对象, new GUIContent("类型文字", "拖入带 TextMeshProUGUI 的文字物体。"));
        DrawComponentStatus(类型文字对象, typeof(TMP_Text), "类型文字需要 TextMeshProUGUI 组件。");

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("大类入口", EditorStyles.boldLabel);
        DrawToggleProperty(技能仓库入口对象, "技能仓库入口", "拖入对应技能仓库的 Toggle 物体，也可以拖入带 Toggle 子物体的按钮。");
        DrawToggleProperty(属性入口对象, "属性入口", "拖入对应属性的 Toggle 物体，也可以拖入带 Toggle 子物体的按钮。");

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("物品细分", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(物品区域绑定对象, new GUIContent("物品区域绑定", "拖入带物品格子区域绑定的物体，用它的数据来源决定仓库、背包、宝箱。"));
        DrawComponentStatus(物品区域绑定对象, typeof(物品格子区域绑定), "物品区域绑定需要物品格子区域绑定组件。");

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("图片", EditorStyles.boldLabel);
        DrawImageProperty(仓库图片对象, "仓库图片");
        DrawImageProperty(技能仓库图片对象, "技能仓库图片");
        DrawImageProperty(属性图片对象, "属性图片");
        DrawImageProperty(背包图片对象, "背包图片");
        DrawImageProperty(宝箱图片对象, "宝箱图片");

        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space(8f);
        DrawRuntimeStatus();

        EditorGUILayout.Space(6f);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("按子物体名称自动绑定"))
            {
                AutoBindChildren();
            }

            if (GUILayout.Button("刷新显示预览"))
            {
                RefreshTargets();
            }
        }
    }

    private static void DrawImageProperty(SerializedProperty property, string label)
    {
        EditorGUILayout.PropertyField(property, new GUIContent(label, "拖入带 Image 组件的图片物体。"));
        DrawComponentStatus(property, typeof(Image), label + " 需要 Image 组件。");
    }

    private static void DrawToggleProperty(SerializedProperty property, string label, string tooltip)
    {
        EditorGUILayout.PropertyField(property, new GUIContent(label, tooltip));
        DrawToggleStatus(property, label + " 需要 Toggle 组件，或子物体里有 Toggle 组件。");
    }

    private static void DrawComponentStatus(SerializedProperty property, System.Type componentType, string error)
    {
        if (property == null || property.hasMultipleDifferentValues)
        {
            return;
        }

        GameObject gameObject = property.objectReferenceValue as GameObject;
        if (gameObject == null)
        {
            EditorGUILayout.HelpBox("未绑定。", MessageType.Warning);
            return;
        }

        if (gameObject.GetComponent(componentType) == null)
        {
            EditorGUILayout.HelpBox(error, MessageType.Error);
        }
    }

    private static void DrawToggleStatus(SerializedProperty property, string error)
    {
        if (property == null || property.hasMultipleDifferentValues)
        {
            return;
        }

        GameObject gameObject = property.objectReferenceValue as GameObject;
        if (gameObject == null)
        {
            EditorGUILayout.HelpBox("未绑定。", MessageType.Warning);
            return;
        }

        if (gameObject.GetComponent<Toggle>() == null && gameObject.GetComponentInChildren<Toggle>(true) == null)
        {
            EditorGUILayout.HelpBox(error, MessageType.Error);
        }
    }

    private void DrawRuntimeStatus()
    {
        if (targets.Length != 1)
        {
            return;
        }

        物品背景板类型显示 display = target as 物品背景板类型显示;
        if (display == null)
        {
            return;
        }

        物品背景板类型显示.显示类型 type = display.当前显示类型;
        string label = 物品背景板类型显示.获取显示名称(type);
        if (string.IsNullOrEmpty(label))
        {
            EditorGUILayout.HelpBox("当前无法识别类型。请检查技能仓库/属性入口是否绑定 Toggle；物品类显示需要绑定物品区域绑定。", MessageType.Warning);
            return;
        }

        EditorGUILayout.HelpBox("当前识别类型：" + label, MessageType.Info);
    }

    private void AutoBindChildren()
    {
        for (int i = 0; i < targets.Length; i++)
        {
            物品背景板类型显示 display = targets[i] as 物品背景板类型显示;
            if (display == null)
            {
                continue;
            }

            Undo.RecordObject(display, "自动绑定物品背景板类型显示");
            display.按子物体名称自动绑定();
            EditorUtility.SetDirty(display);
        }

        serializedObject.Update();
    }

    private void RefreshTargets()
    {
        for (int i = 0; i < targets.Length; i++)
        {
            物品背景板类型显示 display = targets[i] as 物品背景板类型显示;
            if (display == null)
            {
                continue;
            }

            display.刷新显示();
            EditorUtility.SetDirty(display);
        }
    }
}

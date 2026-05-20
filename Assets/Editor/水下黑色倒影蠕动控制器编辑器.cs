using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(水下黑色倒影蠕动控制器))]
public sealed class 水下黑色倒影蠕动控制器编辑器 : Editor
{
    private SerializedProperty 方向;
    private SerializedProperty 东西南北基准;
    private SerializedProperty 方向激烈程度;
    private SerializedProperty 方向蠕动密度;
    private SerializedProperty 方向推进速度;
    private SerializedProperty 横切撕扯;

    private void OnEnable()
    {
        方向 = serializedObject.FindProperty("方向");
        东西南北基准 = serializedObject.FindProperty("东西南北基准");
        方向激烈程度 = serializedObject.FindProperty("方向激烈程度");
        方向蠕动密度 = serializedObject.FindProperty("方向蠕动密度");
        方向推进速度 = serializedObject.FindProperty("方向推进速度");
        横切撕扯 = serializedObject.FindProperty("横切撕扯");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        水下黑色倒影蠕动控制器 controller = target as 水下黑色倒影蠕动控制器;
        DrawMaterialStatus(controller);

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.LabelField("流向", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(方向, new GUIContent("蠕动方向", "无方向就是原本适合平静水下的蠕动。东、南、西、北会叠加更激烈的方向流动。"));
        EditorGUILayout.PropertyField(东西南北基准, new GUIContent("东西南北基准", "战斗摄像机投影会使用48.6、45度视角下的横纵方向，和黑色尖条流动粒子系统一致。"));

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("激流参数", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(方向激烈程度, new GUIContent("方向激烈程度", "方向流动额外拉扯的强度。0等于只保留原本无方向蠕动。"));
        EditorGUILayout.PropertyField(方向蠕动密度, new GUIContent("方向蠕动密度", "方向水流纹理的密度，数值越大变化越碎。"));
        EditorGUILayout.PropertyField(方向推进速度, new GUIContent("方向推进速度", "方向水流向前推进的速度。"));
        EditorGUILayout.PropertyField(横切撕扯, new GUIContent("横切撕扯", "垂直于流向的摆动强度，用来让激流边缘更乱。"));

        bool changed = EditorGUI.EndChangeCheck();
        serializedObject.ApplyModifiedProperties();
        if (changed)
        {
            ApplySelectedControllers();
        }

        EditorGUILayout.Space(8f);
        if (GUILayout.Button("应用蠕动方向"))
        {
            ApplySelectedControllers();
        }
    }

    private static void DrawMaterialStatus(水下黑色倒影蠕动控制器 controller)
    {
        if (controller == null)
        {
            return;
        }

        SpriteRenderer renderer = controller.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            EditorGUILayout.HelpBox("这个对象缺少 SpriteRenderer，无法控制 Sprite 材质。", MessageType.Error);
            return;
        }

        Material material = renderer.sharedMaterial;
        if (material == null)
        {
            EditorGUILayout.HelpBox("SpriteRenderer 没有指定材质。请使用水下黑色倒影蠕动Sprite材质。", MessageType.Warning);
            return;
        }

        if (material.shader == null || material.shader.name != "项目/特效/水下黑色倒影蠕动Sprite")
        {
            EditorGUILayout.HelpBox("当前材质不是水下黑色倒影蠕动Sprite Shader。控制器仍会写入参数，但这个材质可能不会响应。", MessageType.Warning);
            return;
        }

        Vector2 materialDirection = controller.当前材质方向;
        EditorGUILayout.HelpBox(
            $"当前材质方向：({materialDirection.x:0.###}, {materialDirection.y:0.###})",
            MessageType.Info);
    }

    private void ApplySelectedControllers()
    {
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] is 水下黑色倒影蠕动控制器 controller)
            {
                controller.应用蠕动方向();
                EditorUtility.SetDirty(controller);
            }
        }

        SceneView.RepaintAll();
    }
}

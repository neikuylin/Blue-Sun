using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(水纹半月粒子系统))]
public sealed class 水纹半月粒子系统编辑器 : Editor
{
    private SerializedProperty 粒子材质模板;
    private SerializedProperty 粒子颜色;
    private SerializedProperty 地面浮起;
    private SerializedProperty 编辑模式预览;
    private SerializedProperty 每秒数量;
    private SerializedProperty 最大数量;
    private SerializedProperty 生命周期;
    private SerializedProperty 扩散距离;
    private SerializedProperty 起始尺寸;
    private SerializedProperty 结束尺寸;
    private SerializedProperty 起始角度;
    private SerializedProperty 结束角度;
    private SerializedProperty 粒子朝向扩散方向;
    private SerializedProperty 随机旋转偏移;
    private SerializedProperty 半月弧度;
    private SerializedProperty 弧宽比例;
    private SerializedProperty 弧线段数;

    private void OnEnable()
    {
        粒子材质模板 = serializedObject.FindProperty("粒子材质模板");
        粒子颜色 = serializedObject.FindProperty("粒子颜色");
        地面浮起 = serializedObject.FindProperty("地面浮起");
        编辑模式预览 = serializedObject.FindProperty("编辑模式预览");
        每秒数量 = serializedObject.FindProperty("每秒数量");
        最大数量 = serializedObject.FindProperty("最大数量");
        生命周期 = serializedObject.FindProperty("生命周期");
        扩散距离 = serializedObject.FindProperty("扩散距离");
        起始尺寸 = serializedObject.FindProperty("起始尺寸");
        结束尺寸 = serializedObject.FindProperty("结束尺寸");
        起始角度 = serializedObject.FindProperty("起始角度");
        结束角度 = serializedObject.FindProperty("结束角度");
        粒子朝向扩散方向 = serializedObject.FindProperty("粒子朝向扩散方向");
        随机旋转偏移 = serializedObject.FindProperty("随机旋转偏移");
        半月弧度 = serializedObject.FindProperty("半月弧度");
        弧宽比例 = serializedObject.FindProperty("弧宽比例");
        弧线段数 = serializedObject.FindProperty("弧线段数");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUI.BeginChangeCheck();

        DrawAppearanceSettings();
        DrawEmissionSettings();
        DrawAngleSettings();
        DrawShapeSettings();

        bool changed = EditorGUI.EndChangeCheck();
        serializedObject.ApplyModifiedProperties();
        if (changed)
        {
            ApplySelectedSystems(false);
        }

        EditorGUILayout.Space(8f);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("应用设置"))
            {
                ApplySelectedSystems(false);
            }

            if (GUILayout.Button("重新预览"))
            {
                ApplySelectedSystems(true);
            }
        }
    }

    private void DrawAppearanceSettings()
    {
        EditorGUILayout.LabelField("外观", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(粒子材质模板, new GUIContent("粒子材质模板", "可选。为空时自动使用透明粒子材质。"));
        EditorGUILayout.PropertyField(粒子颜色, new GUIContent("粒子颜色", "水纹半月粒子的颜色和初始透明度。"));
        EditorGUILayout.PropertyField(地面浮起, new GUIContent("地面浮起", "粒子离对象原点所在平面的高度，避免和地面重叠闪烁。"));
        EditorGUILayout.PropertyField(编辑模式预览, new GUIContent("编辑模式预览", "开启后不进入播放模式也会在Scene里播放预览。"));
    }

    private void DrawEmissionSettings()
    {
        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("扩散", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(每秒数量, new GUIContent("每秒数量", "每秒生成多少个半月水纹粒子。"));
        EditorGUILayout.PropertyField(最大数量, new GUIContent("最大数量", "同时存在的最大粒子数量。"));
        EditorGUILayout.PropertyField(生命周期, new GUIContent("生命周期范围", "每个粒子从出现到消失的时间范围。"));
        EditorGUILayout.PropertyField(扩散距离, new GUIContent("扩散距离范围", "粒子从中心向外扩散的最终距离范围。"));
        EditorGUILayout.PropertyField(起始尺寸, new GUIContent("起始尺寸范围", "粒子刚出现时的尺寸范围。"));
        EditorGUILayout.PropertyField(结束尺寸, new GUIContent("结束尺寸范围", "粒子消失前放大的尺寸范围。"));
    }

    private void DrawAngleSettings()
    {
        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("角度范围", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(起始角度, new GUIContent("起始角度", "0度是本地X正方向，90度是本地Z正方向。结束角度小于起始角度时会跨过360度。"));
        EditorGUILayout.PropertyField(结束角度, new GUIContent("结束角度", "控制粒子发射扇区的结束角度。"));
        EditorGUILayout.PropertyField(粒子朝向扩散方向, new GUIContent("粒子朝向扩散方向", "开启后半月弧形会朝向自身扩散方向。"));
        EditorGUILayout.PropertyField(随机旋转偏移, new GUIContent("随机旋转偏移", "给每个粒子增加少量随机旋转，避免水纹完全一致。"));
    }

    private void DrawShapeSettings()
    {
        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("半月形状", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(半月弧度, new GUIContent("半月弧度", "单个粒子的弧形张开角度。180度就是半月。"));
        EditorGUILayout.PropertyField(弧宽比例, new GUIContent("弧宽比例", "弧形线条本身的宽度。"));
        EditorGUILayout.PropertyField(弧线段数, new GUIContent("弧线段数", "数值越高弧线越圆滑。"));
    }

    private void OnSceneGUI()
    {
        水纹半月粒子系统 ripple = target as 水纹半月粒子系统;
        if (ripple == null)
        {
            return;
        }

        DrawAnglePreview(ripple);
    }

    private static void DrawAnglePreview(水纹半月粒子系统 ripple)
    {
        Transform transform = ripple.transform;
        Vector3 center = transform.position;
        Vector3 normal = transform.up;
        float radius = Mathf.Max(0.1f, ripple.Scene最大扩散距离);
        float startAngle = ripple.Scene起始角度;
        float span = ripple.Scene角度跨度;

        Color sourceColor = ripple.Scene参考颜色;
        Color fillColor = new Color(sourceColor.r, sourceColor.g, sourceColor.b, 0.08f);
        Color lineColor = new Color(sourceColor.r, sourceColor.g, sourceColor.b, 0.65f);
        if (Mathf.Max(sourceColor.r, sourceColor.g, sourceColor.b) <= 0.05f)
        {
            fillColor = new Color(0f, 0f, 0f, 0.08f);
            lineColor = new Color(0f, 0f, 0f, 0.65f);
        }

        Color oldColor = Handles.color;
        Vector3 startDirection = AngleToWorldDirection(transform, startAngle);
        Handles.color = fillColor;
        if (span > 0.001f)
        {
            Handles.DrawSolidArc(center, normal, startDirection, span, radius);
        }

        Handles.color = lineColor;
        Handles.DrawWireDisc(center, normal, radius);
        Handles.DrawLine(center, center + startDirection * radius);
        Handles.DrawLine(center, center + AngleToWorldDirection(transform, startAngle + span) * radius);
        Handles.Label(center + normal * 0.2f, $"水纹角度 {startAngle:0.#}° -> {ripple.Scene结束角度:0.#}°");
        Handles.color = oldColor;
    }

    private static Vector3 AngleToWorldDirection(Transform transform, float angle)
    {
        float radians = angle * Mathf.Deg2Rad;
        Vector3 localDirection = new Vector3(Mathf.Cos(radians), 0f, Mathf.Sin(radians));
        return transform.TransformDirection(localDirection).normalized;
    }

    private void ApplySelectedSystems(bool restartPreview)
    {
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] is 水纹半月粒子系统 ripple)
            {
                if (restartPreview)
                {
                    ripple.重新预览();
                }

                ripple.应用设置();
                EditorUtility.SetDirty(ripple);
            }
        }

        SceneView.RepaintAll();
    }
}

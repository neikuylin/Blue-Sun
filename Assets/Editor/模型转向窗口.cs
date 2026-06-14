using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class 模型转向窗口 : EditorWindow
{
    private readonly List<GameObject> 场景对象列表 = new List<GameObject>();
    private readonly List<string> 场景对象显示名列表 = new List<string>();

    private string 角色ID输入 = string.Empty;
    private int 选中对象索引;
    private int 选中对象实例ID;
    private float 转动九十度时间 = 0.3f;

    private Transform 正在转向对象;
    private Quaternion 目标旋转;
    private double 上次更新时间;

    [MenuItem("Tools/模型/模型转向")]
    private static void 打开()
    {
        模型转向窗口 窗口 = GetWindow<模型转向窗口>("模型转向");
        窗口.minSize = new Vector2(430f, 220f);
        窗口.Show();
        窗口.Focus();
    }

    private void OnEnable()
    {
        EditorApplication.hierarchyChanged += 刷新场景对象;
        EditorApplication.update += 更新平滑转向;
        刷新场景对象();
    }

    private void OnDisable()
    {
        EditorApplication.hierarchyChanged -= 刷新场景对象;
        EditorApplication.update -= 更新平滑转向;
        正在转向对象 = null;
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("模型转向", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "列出当前战场中已经生成的角色模型。选择模型后，按项目定义的东、南、西、北设置世界朝向。只修改世界 Y 轴旋转。",
            MessageType.Info);

        EditorGUI.BeginChangeCheck();
        string 新角色ID输入 = EditorGUILayout.TextField("角色 ID", 角色ID输入);
        if (EditorGUI.EndChangeCheck())
        {
            角色ID输入 = 新角色ID输入;
            按角色ID同步下拉选择();
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("刷新", GUILayout.Width(60f)))
            {
                刷新场景对象();
            }
        }

        if (场景对象列表.Count <= 1)
        {
            EditorGUILayout.HelpBox(
                "当前战场中没有已生成的角色模型。请先进入战斗并生成战场单位。",
                MessageType.Warning);
            return;
        }

        选中对象索引 = Mathf.Clamp(选中对象索引, 0, 场景对象列表.Count - 1);
        EditorGUI.BeginChangeCheck();
        选中对象索引 = EditorGUILayout.Popup(
            "战场模型",
            选中对象索引,
            场景对象显示名列表.ToArray());
        if (EditorGUI.EndChangeCheck())
        {
            GameObject 选中对象 = 取得选中对象();
            选中对象实例ID = 选中对象 != null ? 选中对象.GetInstanceID() : 0;
            BattleUnit 单位 = 选中对象 != null ? 选中对象.GetComponent<BattleUnit>() : null;
            角色ID输入 = 单位 != null ? 单位.characterId : string.Empty;
        }

        GameObject 当前对象 = 取得选中对象();
        BattleUnit 当前单位 = 当前对象 != null ? 当前对象.GetComponent<BattleUnit>() : null;
        if (!string.IsNullOrWhiteSpace(角色ID输入) &&
            (当前单位 == null ||
             !string.Equals(当前单位.characterId, 角色ID输入.Trim(), System.StringComparison.Ordinal)))
        {
            EditorGUILayout.HelpBox("当前输入的角色 ID 在战场中没有匹配模型。", MessageType.Warning);
            当前对象 = null;
        }

        using (new EditorGUI.DisabledScope(当前对象 == null))
        {
            if (当前对象 != null)
            {
                EditorGUILayout.ObjectField("当前对象", 当前对象, typeof(GameObject), true);
                EditorGUILayout.Vector3Field("当前世界旋转", 当前对象.transform.eulerAngles);
            }

            float 朝向修正 = BattleAnimationSettingsResolver.ResolveIdleYawOffset();
            EditorGUILayout.LabelField("项目朝向修正", $"{朝向修正:0.##}°");
            转动九十度时间 = Mathf.Max(
                0.01f,
                EditorGUILayout.FloatField("转动 90° 时间（秒）", 转动九十度时间));
            EditorGUILayout.LabelField("当前转向速度", $"{90f / 转动九十度时间:0.##}°/秒");

            EditorGUILayout.Space(8f);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("北", GUILayout.Width(90f), GUILayout.Height(30f)))
                {
                    开始模型转向(当前对象, 0f, "模型转向：北");
                }
                GUILayout.FlexibleSpace();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("西", GUILayout.Height(30f)))
                {
                    开始模型转向(当前对象, 270f, "模型转向：西");
                }

                if (GUILayout.Button("南", GUILayout.Height(30f)))
                {
                    开始模型转向(当前对象, 180f, "模型转向：南");
                }

                if (GUILayout.Button("东", GUILayout.Height(30f)))
                {
                    开始模型转向(当前对象, 90f, "模型转向：东");
                }
            }
        }
    }

    private void 刷新场景对象()
    {
        int 旧实例ID = 选中对象实例ID;
        BattleUnit[] 所有单位 = Object.FindObjectsByType<BattleUnit>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        场景对象列表.Clear();
        场景对象显示名列表.Clear();

        List<场景对象条目> 条目列表 = new List<场景对象条目>();
        for (int i = 0; i < 所有单位.Length; i++)
        {
            BattleUnit 单位 = 所有单位[i];
            GameObject 对象 = 单位 != null ? 单位.gameObject : null;
            if (对象 == null || !对象.scene.IsValid() || !对象.scene.isLoaded)
            {
                continue;
            }

            string 层级路径 = 取得层级路径(对象.transform);
            string 角色名称 = !string.IsNullOrWhiteSpace(单位.unitName)
                ? 单位.unitName
                : !string.IsNullOrWhiteSpace(单位.characterId) ? 单位.characterId : 对象.name;
            string 显示名 = $"{角色名称} ({单位.characterId})  [{对象.scene.name}/{层级路径}]";

            条目列表.Add(new 场景对象条目(对象, 显示名));
        }

        条目列表.Sort((左, 右) =>
            string.Compare(左.显示名, 右.显示名, System.StringComparison.OrdinalIgnoreCase));

        选中对象索引 = 0;
        场景对象列表.Add(null);
        场景对象显示名列表.Add("（未选择）");
        for (int i = 0; i < 条目列表.Count; i++)
        {
            场景对象列表.Add(条目列表[i].对象);
            场景对象显示名列表.Add(条目列表[i].显示名);
            if (条目列表[i].对象.GetInstanceID() == 旧实例ID)
            {
                选中对象索引 = i + 1;
            }
        }

        GameObject 当前对象 = 取得选中对象();
        选中对象实例ID = 当前对象 != null ? 当前对象.GetInstanceID() : 0;
        if (当前对象 != null)
        {
            BattleUnit 当前单位 = 当前对象.GetComponent<BattleUnit>();
            角色ID输入 = 当前单位 != null ? 当前单位.characterId : string.Empty;
        }
        else
        {
            按角色ID同步下拉选择();
        }

        Repaint();
    }

    private void 按角色ID同步下拉选择()
    {
        string 目标ID = string.IsNullOrWhiteSpace(角色ID输入) ? string.Empty : 角色ID输入.Trim();
        选中对象索引 = 0;
        选中对象实例ID = 0;
        if (string.IsNullOrEmpty(目标ID))
        {
            return;
        }

        for (int i = 1; i < 场景对象列表.Count; i++)
        {
            GameObject 对象 = 场景对象列表[i];
            BattleUnit 单位 = 对象 != null ? 对象.GetComponent<BattleUnit>() : null;
            if (单位 == null ||
                !string.Equals(单位.characterId, 目标ID, System.StringComparison.Ordinal))
            {
                continue;
            }

            选中对象索引 = i;
            选中对象实例ID = 对象.GetInstanceID();
            return;
        }
    }

    private GameObject 取得选中对象()
    {
        return 选中对象索引 >= 0 && 选中对象索引 < 场景对象列表.Count
            ? 场景对象列表[选中对象索引]
            : null;
    }

    private void 开始模型转向(GameObject 对象, float 基础角度, string 撤销名称)
    {
        if (对象 == null)
        {
            return;
        }

        Transform 变换 = 对象.transform;
        if (!Application.isPlaying)
        {
            Undo.RecordObject(变换, 撤销名称);
        }

        Vector3 世界欧拉角 = 变换.eulerAngles;
        世界欧拉角.y = Mathf.Repeat(
            基础角度 + BattleAnimationSettingsResolver.ResolveIdleYawOffset(),
            360f);
        正在转向对象 = 变换;
        目标旋转 = Quaternion.Euler(世界欧拉角);
        上次更新时间 = EditorApplication.timeSinceStartup;
    }

    private void 更新平滑转向()
    {
        if (正在转向对象 == null)
        {
            return;
        }

        double 当前时间 = EditorApplication.timeSinceStartup;
        float 经过时间 = Mathf.Max(0f, (float)(当前时间 - 上次更新时间));
        上次更新时间 = 当前时间;

        float 每秒角度 = 90f / Mathf.Max(0.01f, 转动九十度时间);
        正在转向对象.rotation = Quaternion.RotateTowards(
            正在转向对象.rotation,
            目标旋转,
            每秒角度 * 经过时间);

        bool 已完成 = Quaternion.Angle(正在转向对象.rotation, 目标旋转) <= 0.01f;
        if (已完成)
        {
            正在转向对象.rotation = 目标旋转;
            完成模型转向();
        }

        SceneView.RepaintAll();
        Repaint();
    }

    private void 完成模型转向()
    {
        Transform 已完成对象 = 正在转向对象;
        正在转向对象 = null;
        if (已完成对象 == null || Application.isPlaying)
        {
            return;
        }

        EditorUtility.SetDirty(已完成对象);
        Scene 场景 = 已完成对象.gameObject.scene;
        if (场景.IsValid())
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(场景);
        }
    }

    private static string 取得层级路径(Transform 变换)
    {
        if (变换 == null)
        {
            return string.Empty;
        }

        string 路径 = 变换.name;
        Transform 当前父级 = 变换.parent;
        while (当前父级 != null)
        {
            路径 = 当前父级.name + "/" + 路径;
            当前父级 = 当前父级.parent;
        }

        return 路径;
    }

    private readonly struct 场景对象条目
    {
        public readonly GameObject 对象;
        public readonly string 显示名;

        public 场景对象条目(GameObject 对象, string 显示名)
        {
            this.对象 = 对象;
            this.显示名 = 显示名;
        }
    }
}

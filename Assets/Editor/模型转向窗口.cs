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
    private 剧情数据库.转向面向对象 面向对象 = 剧情数据库.转向面向对象.方向;
    private 剧情数据库.敌人选择方式 敌人选择 = 剧情数据库.敌人选择方式.确切敌人;
    private string 目标敌人ID = string.Empty;

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
        转动九十度时间 = BattleAnimationSettingsResolver.ResolveModelTurn90Duration();
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
            "列出当前战场中已经生成的角色模型。可以按项目方向转向，也可以面向确切敌人或最近敌人。只修改世界 Y 轴旋转。",
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
            绘制共享转向配置();
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

            面向对象 = (剧情数据库.转向面向对象)EditorGUILayout.EnumPopup("面向对象", 面向对象);
            if (面向对象 == 剧情数据库.转向面向对象.方向)
            {
                绘制方向按钮(当前对象);
            }
            else
            {
                绘制敌人转向(当前单位);
            }
        }

        绘制共享转向配置();
    }

    private void 绘制方向按钮(GameObject 当前对象)
    {
        EditorGUILayout.Space(8f);
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("北", GUILayout.Width(90f), GUILayout.Height(30f)))
            {
                开始模型转向(
                    当前对象,
                    模型转向服务.计算方向旋转(当前对象.transform, 剧情数据库.模型朝向.北),
                    "模型转向：北");
            }
            GUILayout.FlexibleSpace();
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("西", GUILayout.Height(30f)))
            {
                开始模型转向(
                    当前对象,
                    模型转向服务.计算方向旋转(当前对象.transform, 剧情数据库.模型朝向.西),
                    "模型转向：西");
            }

            if (GUILayout.Button("南", GUILayout.Height(30f)))
            {
                开始模型转向(
                    当前对象,
                    模型转向服务.计算方向旋转(当前对象.transform, 剧情数据库.模型朝向.南),
                    "模型转向：南");
            }

            if (GUILayout.Button("东", GUILayout.Height(30f)))
            {
                开始模型转向(
                    当前对象,
                    模型转向服务.计算方向旋转(当前对象.transform, 剧情数据库.模型朝向.东),
                    "模型转向：东");
            }
        }
    }

    private void 绘制敌人转向(BattleUnit 当前单位)
    {
        敌人选择 = (剧情数据库.敌人选择方式)EditorGUILayout.EnumPopup("敌人", 敌人选择);
        BattleUnit 目标敌人;
        if (敌人选择 == 剧情数据库.敌人选择方式.确切敌人)
        {
            List<BattleUnit> 敌人列表 = new List<BattleUnit>();
            List<string> 敌人显示名列表 = new List<string>();
            for (int i = 1; i < 场景对象列表.Count; i++)
            {
                GameObject 对象 = 场景对象列表[i];
                BattleUnit 单位 = 对象 != null ? 对象.GetComponent<BattleUnit>() : null;
                if (!模型转向服务.是有效敌人(当前单位, 单位))
                {
                    continue;
                }

                敌人列表.Add(单位);
                敌人显示名列表.Add($"{单位.unitName} ({单位.characterId})");
            }

            if (敌人列表.Count == 0)
            {
                EditorGUILayout.HelpBox("当前战场中没有可选择的敌人。", MessageType.Warning);
                return;
            }

            int 目标索引 = 0;
            for (int i = 0; i < 敌人列表.Count; i++)
            {
                if (string.Equals(敌人列表[i].characterId, 目标敌人ID, System.StringComparison.Ordinal))
                {
                    目标索引 = i;
                    break;
                }
            }

            目标索引 = EditorGUILayout.Popup("敌人 ID", 目标索引, 敌人显示名列表.ToArray());
            目标敌人 = 敌人列表[目标索引];
            目标敌人ID = 目标敌人.characterId;
        }
        else
        {
            目标敌人 = 模型转向服务.查找最近敌人(当前单位);
            if (目标敌人 == null)
            {
                EditorGUILayout.HelpBox("当前单位没有可用的最近敌人。", MessageType.Warning);
                return;
            }

            EditorGUILayout.ObjectField("最近敌人", 目标敌人.gameObject, typeof(GameObject), true);
        }

        if (GUILayout.Button("面向敌人", GUILayout.Height(30f)))
        {
            开始模型转向(
                当前单位.gameObject,
                模型转向服务.计算面向单位旋转(当前单位, 目标敌人),
                $"模型转向：面向{目标敌人.characterId}");
        }
    }

    private void 绘制共享转向配置()
    {
        float 朝向修正 = BattleAnimationSettingsResolver.ResolveIdleYawOffset();
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("共享转向配置", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("项目朝向修正", $"{朝向修正:0.##}°");
        EditorGUI.BeginChangeCheck();
        float 新转动九十度时间 = Mathf.Max(
            0.01f,
            EditorGUILayout.DelayedFloatField("转动 90° 时间（秒）", 转动九十度时间));
        if (EditorGUI.EndChangeCheck())
        {
            保存转向时间(新转动九十度时间);
        }

        EditorGUILayout.LabelField("当前转向速度", $"{90f / 转动九十度时间:0.##}°/秒");
    }

    private void 保存转向时间(float 新时间)
    {
        转动九十度时间 = Mathf.Max(0.01f, 新时间);
        BattleAnimationSettings 设置 = BattleAnimationSettings.LoadDefault();
        if (设置 == null)
        {
            Debug.LogError("模型转向：找不到 Resources/BattleAnimationSettings 配置。");
            return;
        }

        Undo.RecordObject(设置, "修改模型转向速度");
        设置.modelTurn90Duration = 转动九十度时间;
        EditorUtility.SetDirty(设置);
        AssetDatabase.SaveAssets();
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

    private void 开始模型转向(GameObject 对象, Quaternion 新目标旋转, string 撤销名称)
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

        正在转向对象 = 变换;
        目标旋转 = 新目标旋转;
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

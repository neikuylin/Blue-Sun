using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class 惊吓动画工具窗口 : EditorWindow
{
    private readonly List<BattleUnit> 单位列表 = new List<BattleUnit>();
    private readonly List<string> 单位显示列表 = new List<string>();
    private int 选中单位索引;
    private SerializedObject 配置序列化对象;

    [MenuItem("工具/特效/惊吓动画")]
    private static void 打开窗口()
    {
        惊吓动画工具窗口 窗口 = GetWindow<惊吓动画工具窗口>("惊吓动画");
        窗口.minSize = new Vector2(430f, 310f);
        窗口.Show();
    }

    private void OnEnable()
    {
        刷新单位列表();
        刷新配置();
        EditorApplication.hierarchyChanged += 刷新单位列表;
    }

    private void OnDisable()
    {
        EditorApplication.hierarchyChanged -= 刷新单位列表;
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("惊吓动画", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "选择当前场景中的战斗单位，调整共享位置参数并播放预览。运行时调用“惊吓动画播放服务.播放(单位)”会使用同一份配置。",
            MessageType.Info);

        绘制单位选择();
        EditorGUILayout.Space(8f);
        绘制共享配置();
        EditorGUILayout.Space(8f);
        绘制播放按钮();
    }

    private void 绘制单位选择()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("播放单位", GUILayout.Width(70f));
            if (单位列表.Count == 0)
            {
                EditorGUILayout.LabelField("当前场景没有 BattleUnit");
            }
            else
            {
                选中单位索引 = Mathf.Clamp(选中单位索引, 0, 单位列表.Count - 1);
                选中单位索引 = EditorGUILayout.Popup(选中单位索引, 单位显示列表.ToArray());
            }

            if (GUILayout.Button("刷新", GUILayout.Width(55f)))
            {
                刷新单位列表();
            }
        }
    }

    private void 绘制共享配置()
    {
        惊吓动画配置 配置 = 惊吓动画配置.加载默认配置();
        if (配置 == null)
        {
            EditorGUILayout.HelpBox("找不到 Resources/惊吓动画配置。", MessageType.Error);
            if (GUILayout.Button("重新读取配置"))
            {
                刷新配置();
            }
            return;
        }

        if (配置序列化对象 == null || 配置序列化对象.targetObject != 配置)
        {
            配置序列化对象 = new SerializedObject(配置);
        }

        配置序列化对象.Update();
        EditorGUILayout.LabelField("共享参数", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(配置序列化对象.FindProperty("动画预制体"), new GUIContent("动画预制体"));
        EditorGUILayout.PropertyField(配置序列化对象.FindProperty("右侧偏移"), new GUIContent("右侧偏移"));
        EditorGUILayout.PropertyField(配置序列化对象.FindProperty("顶部偏移"), new GUIContent("顶部偏移"));
        EditorGUILayout.PropertyField(配置序列化对象.FindProperty("整体缩放"), new GUIContent("整体缩放"));
        EditorGUILayout.PropertyField(配置序列化对象.FindProperty("渲染顺序"), new GUIContent("渲染顺序"));
        if (配置序列化对象.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(配置);
            AssetDatabase.SaveAssets();
        }
    }

    private void 绘制播放按钮()
    {
        bool 可以播放 = Application.isPlaying &&
                    单位列表.Count > 0 &&
                    选中单位索引 >= 0 &&
                    选中单位索引 < 单位列表.Count;

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("播放预览需要进入 Play 模式。", MessageType.None);
        }

        using (new EditorGUI.DisabledScope(!可以播放))
        {
            if (GUILayout.Button("播放惊吓动画", GUILayout.Height(34f)))
            {
                惊吓动画播放服务.播放(单位列表[选中单位索引]);
            }
        }
    }

    private void 刷新配置()
    {
        惊吓动画配置 配置 = 惊吓动画配置.加载默认配置();
        配置序列化对象 = 配置 != null ? new SerializedObject(配置) : null;
        Repaint();
    }

    private void 刷新单位列表()
    {
        int 旧实例ID = 单位列表.Count > 0 &&
                   选中单位索引 >= 0 &&
                   选中单位索引 < 单位列表.Count &&
                   单位列表[选中单位索引] != null
            ? 单位列表[选中单位索引].GetInstanceID()
            : 0;

        单位列表.Clear();
        单位显示列表.Clear();
        BattleUnit[] 场景单位 = Object.FindObjectsByType<BattleUnit>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        for (int i = 0; i < 场景单位.Length; i++)
        {
            BattleUnit 单位 = 场景单位[i];
            if (单位 == null || !单位.gameObject.scene.IsValid())
            {
                continue;
            }

            单位列表.Add(单位);
            单位显示列表.Add($"{单位.unitName} ({单位.characterId}) [{单位.gameObject.name}]");
        }

        选中单位索引 = 0;
        for (int i = 0; i < 单位列表.Count; i++)
        {
            if (单位列表[i].GetInstanceID() == 旧实例ID)
            {
                选中单位索引 = i;
                break;
            }
        }

        Repaint();
    }
}

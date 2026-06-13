using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class 小对话组编辑器窗口 : EditorWindow
{
    private const string 资源目录 = "Assets/Resources";
    private const string 资源路径 = 资源目录 + "/小对话组数据库.asset";

    private readonly Dictionary<string, bool> 展开状态 = new Dictionary<string, bool>();
    private Vector2 滚动位置;
    private string 新ID = string.Empty;

    [MenuItem("Tools/事件/小对话组编辑器")]
    private static void 打开()
    {
        小对话组编辑器窗口 窗口 = GetWindow<小对话组编辑器窗口>("小对话组编辑器");
        窗口.minSize = new Vector2(760f, 560f);
        窗口.Show();
        窗口.Focus();
    }

    private void OnGUI()
    {
        小对话组数据库 数据库 = 确保数据库();
        小对话内容数据库 内容数据库 = 小对话内容数据库.加载默认库();
        if (数据库 == null)
        {
            EditorGUILayout.HelpBox("无法创建小对话组数据库。", MessageType.Error);
            return;
        }

        EditorGUILayout.LabelField("小对话组编辑器", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            新ID = EditorGUILayout.TextField("新增小对话组ID", 新ID);
            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(新ID)))
            {
                if (GUILayout.Button("新增", GUILayout.Width(72f)))
                {
                    Undo.RecordObject(数据库, "新增小对话组");
                    string 目标ID = 新ID.Trim();
                    数据库.获取或创建(目标ID);
                    展开状态[目标ID] = true;
                    新ID = string.Empty;
                    保存(数据库);
                }
            }
        }

        EditorGUILayout.Space(6f);
        滚动位置 = EditorGUILayout.BeginScrollView(滚动位置);
        for (int i = 0; i < 数据库.获取对话组列表.Count; i++)
        {
            绘制对话组(数据库, 数据库.获取对话组列表[i], i, 内容数据库);
        }
        EditorGUILayout.EndScrollView();
    }

    private void 绘制对话组(
        小对话组数据库 数据库,
        小对话组数据库.小对话组 对话组,
        int 索引,
        小对话内容数据库 内容数据库)
    {
        if (对话组 == null)
        {
            return;
        }

        小对话组数据库.确保内容列表(对话组);
        string 键 = string.IsNullOrWhiteSpace(对话组.id) ? $"__{索引}" : 对话组.id;
        using (new EditorGUILayout.VerticalScope("box"))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                bool 已展开 = 获取展开状态(键);
                展开状态[键] = EditorGUILayout.Foldout(已展开, string.IsNullOrWhiteSpace(对话组.id) ? $"小对话组 {索引 + 1}" : 对话组.id, true);
                if (GUILayout.Button("删除", GUILayout.Width(72f)))
                {
                    Undo.RecordObject(数据库, "删除小对话组");
                    数据库.获取对话组列表.RemoveAt(索引);
                    展开状态.Remove(键);
                    保存(数据库);
                    GUIUtility.ExitGUI();
                }
            }

            if (!获取展开状态(键))
            {
                return;
            }

            string 新ID = EditorGUILayout.TextField("小对话组ID", 对话组.id);
            if (新ID != 对话组.id)
            {
                Undo.RecordObject(数据库, "修改小对话组ID");
                对话组.id = 新ID;
                保存(数据库);
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("内容播放顺序", EditorStyles.boldLabel);
            for (int i = 0; i < 对话组.内容ID顺序.Count; i++)
            {
                绘制内容项(数据库, 对话组.内容ID顺序, i, 内容数据库);
            }

            if (GUILayout.Button("新增内容", GUILayout.Width(100f)))
            {
                Undo.RecordObject(数据库, "新增小对话组内容");
                对话组.内容ID顺序.Add(string.Empty);
                保存(数据库);
            }
        }
    }

    private static void 绘制内容项(
        小对话组数据库 数据库,
        List<string> 内容ID列表,
        int 索引,
        小对话内容数据库 内容数据库)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            string 当前ID = 内容ID列表[索引] ?? string.Empty;
            string 新ID = 绘制内容选择($"内容 {索引 + 1}", 当前ID, 内容数据库);
            if (新ID != 当前ID)
            {
                Undo.RecordObject(数据库, "修改小对话组内容");
                内容ID列表[索引] = 新ID;
                保存(数据库);
            }

            using (new EditorGUI.DisabledScope(索引 == 0))
            {
                if (GUILayout.Button("上移", GUILayout.Width(48f)))
                {
                    Undo.RecordObject(数据库, "上移小对话内容");
                    string 临时 = 内容ID列表[索引 - 1];
                    内容ID列表[索引 - 1] = 内容ID列表[索引];
                    内容ID列表[索引] = 临时;
                    保存(数据库);
                    GUIUtility.ExitGUI();
                }
            }

            using (new EditorGUI.DisabledScope(索引 >= 内容ID列表.Count - 1))
            {
                if (GUILayout.Button("下移", GUILayout.Width(48f)))
                {
                    Undo.RecordObject(数据库, "下移小对话内容");
                    string 临时 = 内容ID列表[索引 + 1];
                    内容ID列表[索引 + 1] = 内容ID列表[索引];
                    内容ID列表[索引] = 临时;
                    保存(数据库);
                    GUIUtility.ExitGUI();
                }
            }

            if (GUILayout.Button("删除", GUILayout.Width(48f)))
            {
                Undo.RecordObject(数据库, "删除小对话组内容");
                内容ID列表.RemoveAt(索引);
                保存(数据库);
                GUIUtility.ExitGUI();
            }
        }
    }

    private static string 绘制内容选择(string 标签, string 当前ID, 小对话内容数据库 数据库)
    {
        List<string> 选项 = new List<string> { "未选择" };
        if (数据库 != null)
        {
            for (int i = 0; i < 数据库.获取内容列表.Count; i++)
            {
                小对话内容数据库.小对话内容 内容 = 数据库.获取内容列表[i];
                if (内容 != null && !string.IsNullOrWhiteSpace(内容.id))
                {
                    选项.Add(内容.id);
                }
            }
        }

        int 当前索引 = Mathf.Max(0, 选项.IndexOf(当前ID));
        int 新索引 = EditorGUILayout.Popup(标签, 当前索引, 选项.ToArray());
        return 新索引 <= 0 ? string.Empty : 选项[新索引];
    }

    private bool 获取展开状态(string 键)
    {
        if (展开状态.TryGetValue(键, out bool 结果))
        {
            return 结果;
        }

        展开状态[键] = true;
        return true;
    }

    private static 小对话组数据库 确保数据库()
    {
        小对话组数据库 数据库 = AssetDatabase.LoadAssetAtPath<小对话组数据库>(资源路径);
        if (数据库 != null)
        {
            return 数据库;
        }

        数据库 = CreateInstance<小对话组数据库>();
        AssetDatabase.CreateAsset(数据库, 资源路径);
        AssetDatabase.SaveAssets();
        return 数据库;
    }

    private static void 保存(ScriptableObject 资源)
    {
        EditorUtility.SetDirty(资源);
        AssetDatabase.SaveAssets();
    }
}

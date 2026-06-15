using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class 配音音量编辑器窗口 : EditorWindow
{
    private enum 编辑模式
    {
        单条配音,
        按角色ID批量
    }

    private sealed class 配音条目
    {
        public string 类型;
        public string 内容ID;
        public string 角色ID;
        public AudioClip 音频;
        public ScriptableObject 数据库;
        public Func<float> 读取音量;
        public Action<float> 写入音量;

        public string 显示名称 =>
            $"[{类型}] {内容ID} | {角色ID} | {(音频 != null ? 音频.name : "无配音")}";
    }

    private readonly List<配音条目> 配音列表 = new List<配音条目>();
    private readonly List<string> 角色ID列表 = new List<string>();
    private readonly List<配音条目> 当前角色配音列表 = new List<配音条目>();

    private 编辑模式 当前模式;
    private int 单条索引;
    private int 角色ID索引;
    private float 批量音量 = 1f;
    private Vector2 滚动位置;

    [MenuItem("工具/音频/配音音量编辑器")]
    private static void 打开窗口()
    {
        配音音量编辑器窗口 窗口 = GetWindow<配音音量编辑器窗口>("配音音量");
        窗口.minSize = new Vector2(700f, 520f);
        窗口.Show();
        窗口.Focus();
    }

    private void OnEnable()
    {
        刷新数据();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("配音音量编辑器", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "只显示正式对话和小对话数据库中已经绑定的配音。音量倍率保存在每条对话内容中，不修改原始音频文件。",
            MessageType.Info);

        using (new EditorGUILayout.HorizontalScope())
        {
            当前模式 = (编辑模式)GUILayout.Toolbar((int)当前模式, new[] { "单条配音", "按角色 ID 批量" });
            if (GUILayout.Button("刷新", GUILayout.Width(70f)))
            {
                刷新数据();
            }
        }

        EditorGUILayout.Space(8f);
        if (配音列表.Count == 0)
        {
            EditorGUILayout.HelpBox("正式对话和小对话中没有已绑定的配音。", MessageType.Warning);
            return;
        }

        if (当前模式 == 编辑模式.单条配音)
        {
            绘制单条模式();
        }
        else
        {
            绘制角色批量模式();
        }
    }

    private void 绘制单条模式()
    {
        string[] 选项 = new string[配音列表.Count];
        for (int i = 0; i < 配音列表.Count; i++)
        {
            选项[i] = 配音列表[i].显示名称;
        }

        单条索引 = Mathf.Clamp(单条索引, 0, 配音列表.Count - 1);
        单条索引 = EditorGUILayout.Popup("选择配音", 单条索引, 选项);

        配音条目 条目 = 配音列表[单条索引];
        EditorGUILayout.Space(6f);
        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("类型", 条目.类型);
            EditorGUILayout.LabelField("内容 ID", 条目.内容ID);
            EditorGUILayout.LabelField("角色 ID", 条目.角色ID);
            EditorGUILayout.ObjectField("音频文件", 条目.音频, typeof(AudioClip), false);

            float 当前音量 = Mathf.Clamp(条目.读取音量(), 0f, 2f);
            EditorGUI.BeginChangeCheck();
            float 新音量 = EditorGUILayout.Slider("音量倍率", 当前音量, 0f, 2f);
            EditorGUILayout.LabelField("实际比例", $"{新音量 * 100f:0}%");
            if (EditorGUI.EndChangeCheck())
            {
                写入音量(条目, 新音量, "修改单条配音音量");
            }
        }
    }

    private void 绘制角色批量模式()
    {
        if (角色ID列表.Count == 0)
        {
            EditorGUILayout.HelpBox("已使用的配音中没有填写角色 ID。", MessageType.Warning);
            return;
        }

        角色ID索引 = Mathf.Clamp(角色ID索引, 0, 角色ID列表.Count - 1);
        int 新索引 = EditorGUILayout.Popup("角色 ID", 角色ID索引, 角色ID列表.ToArray());
        if (新索引 != 角色ID索引)
        {
            角色ID索引 = 新索引;
            刷新当前角色配音();
        }

        批量音量 = EditorGUILayout.Slider("统一音量倍率", 批量音量, 0f, 2f);
        EditorGUILayout.LabelField("实际比例", $"{批量音量 * 100f:0}%");

        using (new EditorGUI.DisabledScope(当前角色配音列表.Count == 0))
        {
            if (GUILayout.Button($"应用到该 ID 的全部配音（{当前角色配音列表.Count} 条）", GUILayout.Height(30f)))
            {
                应用批量音量();
            }
        }

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("该 ID 已使用的配音", EditorStyles.boldLabel);
        滚动位置 = EditorGUILayout.BeginScrollView(滚动位置);
        for (int i = 0; i < 当前角色配音列表.Count; i++)
        {
            配音条目 条目 = 当前角色配音列表[i];
            using (new EditorGUILayout.HorizontalScope("box"))
            {
                EditorGUILayout.LabelField($"[{条目.类型}] {条目.内容ID}", GUILayout.MinWidth(280f));
                EditorGUILayout.ObjectField(条目.音频, typeof(AudioClip), false, GUILayout.MinWidth(180f));
                EditorGUILayout.LabelField($"{Mathf.Clamp(条目.读取音量(), 0f, 2f) * 100f:0}%", GUILayout.Width(55f));
            }
        }
        EditorGUILayout.EndScrollView();
    }

    private void 刷新数据()
    {
        配音列表.Clear();
        角色ID列表.Clear();

        DialogueContentDatabase 正式对话数据库 = DialogueContentDatabase.LoadDefault();
        if (正式对话数据库 != null)
        {
            for (int i = 0; i < 正式对话数据库.Entries.Count; i++)
            {
                DialogueContentDatabase.DialogueContentEntry 内容 = 正式对话数据库.Entries[i];
                if (内容 == null || 内容.voiceClip == null)
                {
                    continue;
                }

                配音列表.Add(new 配音条目
                {
                    类型 = "正式对话",
                    内容ID = 规范化显示文本(内容.id),
                    角色ID = 规范化显示文本(内容.roleNameId),
                    音频 = 内容.voiceClip,
                    数据库 = 正式对话数据库,
                    读取音量 = () => 内容.voiceVolume,
                    写入音量 = 值 => 内容.voiceVolume = 值
                });
            }
        }

        小对话内容数据库 小对话数据库 = 小对话内容数据库.加载默认库();
        if (小对话数据库 != null)
        {
            for (int i = 0; i < 小对话数据库.获取内容列表.Count; i++)
            {
                小对话内容数据库.小对话内容 内容 = 小对话数据库.获取内容列表[i];
                if (内容 == null || 内容.配音 == null)
                {
                    continue;
                }

                配音列表.Add(new 配音条目
                {
                    类型 = "小对话",
                    内容ID = 规范化显示文本(内容.id),
                    角色ID = 规范化显示文本(内容.对话角色ID),
                    音频 = 内容.配音,
                    数据库 = 小对话数据库,
                    读取音量 = () => 内容.配音音量,
                    写入音量 = 值 => 内容.配音音量 = 值
                });
            }
        }

        配音列表.Sort((左, 右) =>
        {
            int 角色比较 = string.Compare(左.角色ID, 右.角色ID, StringComparison.Ordinal);
            return 角色比较 != 0
                ? 角色比较
                : string.Compare(左.内容ID, 右.内容ID, StringComparison.Ordinal);
        });

        for (int i = 0; i < 配音列表.Count; i++)
        {
            string 角色ID = 配音列表[i].角色ID;
            if (角色ID != "未填写" && !角色ID列表.Contains(角色ID))
            {
                角色ID列表.Add(角色ID);
            }
        }

        角色ID列表.Sort(StringComparer.Ordinal);
        单条索引 = Mathf.Clamp(单条索引, 0, Mathf.Max(0, 配音列表.Count - 1));
        角色ID索引 = Mathf.Clamp(角色ID索引, 0, Mathf.Max(0, 角色ID列表.Count - 1));
        刷新当前角色配音();
        Repaint();
    }

    private void 刷新当前角色配音()
    {
        当前角色配音列表.Clear();
        if (角色ID列表.Count == 0)
        {
            return;
        }

        string 目标角色ID = 角色ID列表[Mathf.Clamp(角色ID索引, 0, 角色ID列表.Count - 1)];
        for (int i = 0; i < 配音列表.Count; i++)
        {
            if (string.Equals(配音列表[i].角色ID, 目标角色ID, StringComparison.Ordinal))
            {
                当前角色配音列表.Add(配音列表[i]);
            }
        }
    }

    private void 应用批量音量()
    {
        HashSet<ScriptableObject> 已记录数据库 = new HashSet<ScriptableObject>();
        for (int i = 0; i < 当前角色配音列表.Count; i++)
        {
            配音条目 条目 = 当前角色配音列表[i];
            if (条目.数据库 != null && 已记录数据库.Add(条目.数据库))
            {
                Undo.RecordObject(条目.数据库, "批量修改配音音量");
            }

            条目.写入音量(Mathf.Clamp(批量音量, 0f, 2f));
        }

        foreach (ScriptableObject 数据库 in 已记录数据库)
        {
            EditorUtility.SetDirty(数据库);
        }

        AssetDatabase.SaveAssets();
    }

    private static void 写入音量(配音条目 条目, float 音量, string 撤销名称)
    {
        if (条目 == null || 条目.数据库 == null)
        {
            return;
        }

        Undo.RecordObject(条目.数据库, 撤销名称);
        条目.写入音量(Mathf.Clamp(音量, 0f, 2f));
        EditorUtility.SetDirty(条目.数据库);
        AssetDatabase.SaveAssets();
    }

    private static string 规范化显示文本(string 文本)
    {
        return string.IsNullOrWhiteSpace(文本) ? "未填写" : 文本.Trim();
    }
}

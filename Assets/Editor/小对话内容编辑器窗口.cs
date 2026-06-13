using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class 小对话内容编辑器窗口 : EditorWindow
{
    private const string 资源目录 = "Assets/Resources";
    private const string 资源路径 = 资源目录 + "/小对话内容数据库.asset";

    private readonly Dictionary<string, bool> 展开状态 = new Dictionary<string, bool>();
    private Vector2 滚动位置;
    private string 新ID = string.Empty;

    [MenuItem("Tools/事件/小对话内容编辑器")]
    private static void 打开()
    {
        小对话内容编辑器窗口 窗口 = GetWindow<小对话内容编辑器窗口>("小对话内容编辑器");
        窗口.minSize = new Vector2(760f, 600f);
        窗口.Show();
        窗口.Focus();
    }

    private void OnGUI()
    {
        小对话内容数据库 数据库 = 确保数据库();
        if (数据库 == null)
        {
            EditorGUILayout.HelpBox("无法创建小对话内容数据库。", MessageType.Error);
            return;
        }

        EditorGUILayout.LabelField("小对话内容编辑器", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("小对话独立于正式弹窗对话。角色头顶形式在角色离开屏幕后会永久切换到屏幕底部，直到本句结束。", MessageType.Info);

        using (new EditorGUILayout.HorizontalScope())
        {
            新ID = EditorGUILayout.TextField("新增内容ID", 新ID);
            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(新ID)))
            {
                if (GUILayout.Button("新增", GUILayout.Width(72f)))
                {
                    Undo.RecordObject(数据库, "新增小对话内容");
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
        for (int i = 0; i < 数据库.获取内容列表.Count; i++)
        {
            绘制内容(数据库, 数据库.获取内容列表[i], i);
        }
        EditorGUILayout.EndScrollView();
    }

    private void 绘制内容(小对话内容数据库 数据库, 小对话内容数据库.小对话内容 内容, int 索引)
    {
        if (内容 == null)
        {
            return;
        }

        string 键 = string.IsNullOrWhiteSpace(内容.id) ? $"__{索引}" : 内容.id;
        using (new EditorGUILayout.VerticalScope("box"))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                bool 已展开 = 获取展开状态(键);
                展开状态[键] = EditorGUILayout.Foldout(已展开, string.IsNullOrWhiteSpace(内容.id) ? $"内容 {索引 + 1}" : 内容.id, true);
                if (GUILayout.Button("删除", GUILayout.Width(72f)))
                {
                    Undo.RecordObject(数据库, "删除小对话内容");
                    数据库.获取内容列表.RemoveAt(索引);
                    展开状态.Remove(键);
                    保存(数据库);
                    GUIUtility.ExitGUI();
                }
            }

            if (!获取展开状态(键))
            {
                return;
            }

            EditorGUI.BeginChangeCheck();
            string 新内容ID = EditorGUILayout.TextField("ID", 内容.id);
            string 新角色ID = EditorGUILayout.TextField("对话角色ID", 内容.对话角色ID);
            小对话内容数据库.对话形式 新形式 =
                (小对话内容数据库.对话形式)EditorGUILayout.EnumPopup("对话形式", 内容.显示形式);
            string 新说话者 = EditorGUILayout.TextField("说话者文本", 内容.说话者文本);
            EditorGUILayout.LabelField("对话文本");
            string 新文本 = EditorGUILayout.TextArea(内容.对话文本, GUILayout.MinHeight(70f));
            AudioClip 新配音 = (AudioClip)EditorGUILayout.ObjectField("配音", 内容.配音, typeof(AudioClip), false);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(数据库, "修改小对话内容");
                内容.id = 新内容ID;
                内容.对话角色ID = 新角色ID;
                内容.显示形式 = 新形式;
                内容.说话者文本 = 新说话者;
                内容.对话文本 = 新文本;
                内容.配音 = 新配音;
                保存(数据库);
            }
        }
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

    private static 小对话内容数据库 确保数据库()
    {
        小对话内容数据库 数据库 = AssetDatabase.LoadAssetAtPath<小对话内容数据库>(资源路径);
        if (数据库 != null)
        {
            return 数据库;
        }

        数据库 = CreateInstance<小对话内容数据库>();
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

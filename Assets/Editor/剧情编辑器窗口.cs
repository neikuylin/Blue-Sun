using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class 剧情编辑器窗口 : EditorWindow
{
    private const string 资源目录 = "Assets/Resources";
    private const string 数据库路径 = 资源目录 + "/剧情数据库.asset";
    private const string 战斗副本场景名 = "战斗副本";

    private readonly Dictionary<string, bool> 剧情展开状态 = new Dictionary<string, bool>();
    private readonly Dictionary<string, bool> 步骤展开状态 = new Dictionary<string, bool>();

    private SerializedObject 数据库对象;
    private Vector2 滚动位置;
    private string 新剧情ID = string.Empty;
    private string 新备注 = string.Empty;
    private 剧情数据库.剧情步骤类型 新步骤类型 = 剧情数据库.剧情步骤类型.播放对话;
    private string 正在拖动步骤列表路径 = string.Empty;
    private int 正在拖动步骤索引 = -1;

    [MenuItem("Tools/剧情/剧情编辑器")]
    private static void 打开()
    {
        剧情编辑器窗口 窗口 = GetWindow<剧情编辑器窗口>("剧情编辑器");
        窗口.minSize = new Vector2(720f, 520f);
        窗口.Show();
        窗口.Focus();
    }

    private void OnGUI()
    {
        剧情数据库 数据库 = 确保数据库();
        if (数据库 == null)
        {
            EditorGUILayout.HelpBox("剧情数据库加载失败。", MessageType.Error);
            return;
        }

        数据库.确保剧情列表有效();
        if (数据库对象 == null || 数据库对象.targetObject != 数据库)
        {
            数据库对象 = new SerializedObject(数据库);
        }

        数据库对象.Update();

        EditorGUILayout.LabelField("剧情编辑器", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("当前只编辑剧情数据：播放对话、设置事件、切换场景。这里不会直接执行剧情。", MessageType.Info);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("保存", GUILayout.Width(90f)))
            {
                数据库对象.ApplyModifiedProperties();
                保存数据库(数据库);
            }
        }

        EditorGUILayout.Space(8f);
        绘制新增面板(数据库);
        EditorGUILayout.Space(8f);
        绘制剧情列表();

        if (数据库对象.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(数据库);
        }

        处理步骤拖动结束();
    }

    private void 绘制新增面板(剧情数据库 数据库)
    {
        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("新增剧情", EditorStyles.boldLabel);
            新剧情ID = EditorGUILayout.TextField("剧情ID", 新剧情ID);
            EditorGUILayout.LabelField("备注");
            新备注 = EditorGUILayout.TextArea(新备注, GUILayout.MinHeight(44f));

            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(新剧情ID)))
            {
                if (GUILayout.Button("新增剧情"))
                {
                    新增剧情(数据库);
                }
            }
        }
    }

    private void 绘制剧情列表()
    {
        SerializedProperty 剧情列表属性 = 数据库对象.FindProperty("剧情列表");
        if (剧情列表属性 == null)
        {
            EditorGUILayout.HelpBox("未找到剧情列表字段。", MessageType.Error);
            return;
        }

        EditorGUILayout.LabelField("现有剧情", EditorStyles.boldLabel);
        if (剧情列表属性.arraySize <= 0)
        {
            EditorGUILayout.HelpBox("还没有剧情。", MessageType.Info);
            return;
        }

        滚动位置 = EditorGUILayout.BeginScrollView(滚动位置);
        for (int i = 0; i < 剧情列表属性.arraySize; i++)
        {
            if (绘制剧情条目(剧情列表属性, i))
            {
                GUIUtility.ExitGUI();
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private bool 绘制剧情条目(SerializedProperty 剧情列表属性, int 索引)
    {
        SerializedProperty 条目属性 = 剧情列表属性.GetArrayElementAtIndex(索引);
        SerializedProperty 剧情ID属性 = 条目属性.FindPropertyRelative("剧情ID");
        SerializedProperty 备注属性 = 条目属性.FindPropertyRelative("备注");
        SerializedProperty 步骤列表属性 = 条目属性.FindPropertyRelative("步骤列表");
        string 展开键 = 取得剧情展开键(剧情ID属性, 索引);
        bool 已展开 = 取得展开状态(剧情展开状态, 展开键, true);

        using (new EditorGUILayout.VerticalScope("box"))
        {
            bool 新展开状态;
            using (new EditorGUILayout.HorizontalScope())
            {
                string 标题 = 剧情ID属性 != null && !string.IsNullOrWhiteSpace(剧情ID属性.stringValue)
                    ? 剧情ID属性.stringValue
                    : $"剧情 {索引 + 1}";

                新展开状态 = EditorGUILayout.Foldout(已展开, 标题, true);
                设置展开状态(剧情展开状态, 展开键, 新展开状态);

                if (GUILayout.Button("删除剧情", GUILayout.Width(82f)))
                {
                    剧情列表属性.DeleteArrayElementAtIndex(索引);
                    数据库对象.ApplyModifiedProperties();
                    保存数据库((剧情数据库)数据库对象.targetObject);
                    return true;
                }
            }

            if (!新展开状态)
            {
                return false;
            }

            if (剧情ID属性 != null)
            {
                EditorGUILayout.PropertyField(剧情ID属性, new GUIContent("剧情ID"));
            }

            if (备注属性 != null)
            {
                EditorGUILayout.PropertyField(备注属性, new GUIContent("备注"));
            }

            EditorGUILayout.Space(6f);
            绘制步骤列表(步骤列表属性, 展开键);
        }

        return false;
    }

    private void 绘制步骤列表(SerializedProperty 步骤列表属性, string 剧情展开键)
    {
        if (步骤列表属性 == null)
        {
            EditorGUILayout.HelpBox("未找到步骤列表字段。", MessageType.Error);
            return;
        }

        EditorGUILayout.LabelField("剧情步骤", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            新步骤类型 = (剧情数据库.剧情步骤类型)EditorGUILayout.EnumPopup("新增步骤类型", 新步骤类型);
            if (GUILayout.Button("新增步骤", GUILayout.Width(90f)))
            {
                新增步骤(步骤列表属性, 新步骤类型);
            }
        }

        if (步骤列表属性.arraySize <= 0)
        {
            EditorGUILayout.HelpBox("这个剧情还没有步骤。", MessageType.Info);
            return;
        }

        EditorGUILayout.Space(4f);
        for (int i = 0; i < 步骤列表属性.arraySize; i++)
        {
            if (绘制步骤条目(步骤列表属性, i, 剧情展开键))
            {
                GUIUtility.ExitGUI();
            }
        }
    }

    private bool 绘制步骤条目(SerializedProperty 步骤列表属性, int 索引, string 剧情展开键)
    {
        SerializedProperty 步骤属性 = 步骤列表属性.GetArrayElementAtIndex(索引);
        SerializedProperty 步骤类型属性 = 步骤属性.FindPropertyRelative("步骤类型");
        SerializedProperty 步骤备注属性 = 步骤属性.FindPropertyRelative("步骤备注");
        string 步骤键 = 剧情展开键 + "/步骤/" + 索引;
        bool 已展开 = 取得展开状态(步骤展开状态, 步骤键, true);

        using (new EditorGUILayout.VerticalScope("box"))
        {
            bool 新展开状态 = 绘制步骤标题行(步骤列表属性, 索引, 步骤键, 步骤类型属性, 已展开, out bool 需要退出);
            if (需要退出)
            {
                return true;
            }

            if (!新展开状态)
            {
                return false;
            }

            if (步骤类型属性 != null)
            {
                EditorGUILayout.PropertyField(步骤类型属性, new GUIContent("步骤类型"));
            }

            绘制步骤字段(步骤属性);

            if (步骤备注属性 != null)
            {
                EditorGUILayout.PropertyField(步骤备注属性, new GUIContent("步骤备注"));
            }
        }

        return false;
    }

    private bool 绘制步骤标题行(
        SerializedProperty 步骤列表属性,
        int 索引,
        string 步骤键,
        SerializedProperty 步骤类型属性,
        bool 已展开,
        out bool 需要退出)
    {
        需要退出 = false;
        Rect 行矩形 = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
        const float 拖动宽度 = 24f;
        const float 按钮宽度 = 48f;
        const float 删除宽度 = 52f;
        const float 间距 = 4f;

        Rect 拖动矩形 = new Rect(行矩形.x, 行矩形.y, 拖动宽度, 行矩形.height);
        Rect 删除矩形 = new Rect(行矩形.xMax - 删除宽度, 行矩形.y, 删除宽度, 行矩形.height);
        Rect 下移矩形 = new Rect(删除矩形.x - 间距 - 按钮宽度, 行矩形.y, 按钮宽度, 行矩形.height);
        Rect 上移矩形 = new Rect(下移矩形.x - 间距 - 按钮宽度, 行矩形.y, 按钮宽度, 行矩形.height);
        Rect 折叠矩形 = new Rect(
            拖动矩形.xMax + 间距,
            行矩形.y,
            Mathf.Max(40f, 上移矩形.x - 拖动矩形.xMax - 间距 * 2f),
            行矩形.height);

        EditorGUIUtility.AddCursorRect(拖动矩形, MouseCursor.Pan);
        GUI.Label(拖动矩形, "拖", EditorStyles.miniButton);
        处理步骤拖动输入(步骤列表属性, 索引, 拖动矩形, 行矩形, out 需要退出);
        if (需要退出)
        {
            return 已展开;
        }

        string 标题 = $"步骤 {索引 + 1}：{取得步骤类型名字(步骤类型属性)}";
        bool 新展开状态 = EditorGUI.Foldout(折叠矩形, 已展开, 标题, true);
        设置展开状态(步骤展开状态, 步骤键, 新展开状态);

        using (new EditorGUI.DisabledScope(索引 <= 0))
        {
            if (GUI.Button(上移矩形, "上移"))
            {
                移动步骤(步骤列表属性, 索引, 索引 - 1);
                需要退出 = true;
                return 新展开状态;
            }
        }

        using (new EditorGUI.DisabledScope(索引 >= 步骤列表属性.arraySize - 1))
        {
            if (GUI.Button(下移矩形, "下移"))
            {
                移动步骤(步骤列表属性, 索引, 索引 + 1);
                需要退出 = true;
                return 新展开状态;
            }
        }

        if (GUI.Button(删除矩形, "删除"))
        {
            删除步骤(步骤列表属性, 索引);
            需要退出 = true;
            return 新展开状态;
        }

        return 新展开状态;
    }

    private void 处理步骤拖动输入(
        SerializedProperty 步骤列表属性,
        int 索引,
        Rect 拖动矩形,
        Rect 行矩形,
        out bool 需要退出)
    {
        需要退出 = false;
        Event 当前事件 = Event.current;
        if (当前事件 == null)
        {
            return;
        }

        string 列表路径 = 步骤列表属性.propertyPath;
        if (当前事件.type == EventType.MouseDown && 当前事件.button == 0 && 拖动矩形.Contains(当前事件.mousePosition))
        {
            正在拖动步骤列表路径 = 列表路径;
            正在拖动步骤索引 = 索引;
            当前事件.Use();
            return;
        }

        bool 正在拖动当前列表 = 正在拖动步骤索引 >= 0 &&
            string.Equals(正在拖动步骤列表路径, 列表路径, System.StringComparison.Ordinal);
        if (!正在拖动当前列表)
        {
            return;
        }

        if (当前事件.type == EventType.MouseDrag && 行矩形.Contains(当前事件.mousePosition) && 正在拖动步骤索引 != 索引)
        {
            移动步骤(步骤列表属性, 正在拖动步骤索引, 索引);
            正在拖动步骤索引 = 索引;
            当前事件.Use();
            需要退出 = true;
        }
    }

    private void 移动步骤(SerializedProperty 步骤列表属性, int 原索引, int 新索引)
    {
        if (步骤列表属性 == null ||
            原索引 < 0 ||
            原索引 >= 步骤列表属性.arraySize ||
            新索引 < 0 ||
            新索引 >= 步骤列表属性.arraySize ||
            原索引 == 新索引)
        {
            return;
        }

        Undo.RecordObject(数据库对象.targetObject, "移动剧情步骤");
        步骤列表属性.MoveArrayElement(原索引, 新索引);
        数据库对象.ApplyModifiedProperties();
        保存数据库((剧情数据库)数据库对象.targetObject);
        Repaint();
    }

    private void 删除步骤(SerializedProperty 步骤列表属性, int 索引)
    {
        Undo.RecordObject(数据库对象.targetObject, "删除剧情步骤");
        步骤列表属性.DeleteArrayElementAtIndex(索引);
        数据库对象.ApplyModifiedProperties();
        保存数据库((剧情数据库)数据库对象.targetObject);
    }

    private void 处理步骤拖动结束()
    {
        Event 当前事件 = Event.current;
        if (当前事件 == null)
        {
            return;
        }

        if (当前事件.type == EventType.MouseUp || 当前事件.rawType == EventType.MouseUp)
        {
            正在拖动步骤列表路径 = string.Empty;
            正在拖动步骤索引 = -1;
        }
    }

    private void 绘制步骤字段(SerializedProperty 步骤属性)
    {
        SerializedProperty 步骤类型属性 = 步骤属性.FindPropertyRelative("步骤类型");
        if (步骤类型属性 == null)
        {
            return;
        }

        剧情数据库.剧情步骤类型 步骤类型 = (剧情数据库.剧情步骤类型)步骤类型属性.enumValueIndex;
        switch (步骤类型)
        {
            case 剧情数据库.剧情步骤类型.播放对话:
                绘制播放对话字段(步骤属性);
                break;
            case 剧情数据库.剧情步骤类型.设置事件:
                绘制设置事件字段(步骤属性);
                break;
            case 剧情数据库.剧情步骤类型.切换场景:
                绘制切换场景字段(步骤属性);
                break;
        }
    }

    private void 绘制播放对话字段(SerializedProperty 步骤属性)
    {
        SerializedProperty 对话组ID属性 = 步骤属性.FindPropertyRelative("对话组ID");
        if (对话组ID属性 == null)
        {
            return;
        }

        绘制ID选择或输入(对话组ID属性, "对话组ID", 读取对话组ID列表());
    }

    private void 绘制设置事件字段(SerializedProperty 步骤属性)
    {
        SerializedProperty 事件ID属性 = 步骤属性.FindPropertyRelative("事件ID");
        SerializedProperty 事件状态属性 = 步骤属性.FindPropertyRelative("事件状态");

        if (事件ID属性 != null)
        {
            绘制ID选择或输入(事件ID属性, "事件ID", 读取事件ID列表());
        }

        if (事件状态属性 != null)
        {
            事件状态属性.boolValue = EditorGUILayout.Toggle("设置为启用", 事件状态属性.boolValue);
        }
    }

    private void 绘制切换场景字段(SerializedProperty 步骤属性)
    {
        SerializedProperty 场景目标类型属性 = 步骤属性.FindPropertyRelative("目标类型");
        SerializedProperty 场景名属性 = 步骤属性.FindPropertyRelative("场景名");
        SerializedProperty 地图模板ID属性 = 步骤属性.FindPropertyRelative("地图模板ID");
        SerializedProperty 房间节点ID属性 = 步骤属性.FindPropertyRelative("房间节点ID");

        if (场景目标类型属性 != null)
        {
            EditorGUILayout.PropertyField(场景目标类型属性, new GUIContent("目标类型"));
        }

        剧情数据库.场景目标类型 目标类型 = 场景目标类型属性 != null
            ? (剧情数据库.场景目标类型)场景目标类型属性.enumValueIndex
            : 剧情数据库.场景目标类型.普通场景;

        if (目标类型 == 剧情数据库.场景目标类型.战斗副本)
        {
            if (场景名属性 != null)
            {
                场景名属性.stringValue = 战斗副本场景名;
            }

            EditorGUILayout.LabelField("目标场景", 战斗副本场景名);
            if (地图模板ID属性 != null)
            {
                绘制ID选择或输入(地图模板ID属性, "地图模板ID", 读取地图模板ID列表());
            }

            if (房间节点ID属性 != null)
            {
                string 地图模板ID = 地图模板ID属性 != null ? 地图模板ID属性.stringValue : string.Empty;
                绘制ID选择或输入(房间节点ID属性, "房间节点ID", 读取房间节点ID列表(地图模板ID));
            }
        }
        else if (场景名属性 != null)
        {
            绘制ID选择或输入(场景名属性, "场景名", 读取构建场景名列表());
        }
    }

    private void 新增剧情(剧情数据库 数据库)
    {
        SerializedProperty 剧情列表属性 = 数据库对象.FindProperty("剧情列表");
        if (剧情列表属性 == null)
        {
            return;
        }

        Undo.RecordObject(数据库, "新增剧情");
        int 新索引 = 剧情列表属性.arraySize;
        剧情列表属性.InsertArrayElementAtIndex(新索引);

        SerializedProperty 条目属性 = 剧情列表属性.GetArrayElementAtIndex(新索引);
        SerializedProperty 剧情ID属性 = 条目属性.FindPropertyRelative("剧情ID");
        SerializedProperty 备注属性 = 条目属性.FindPropertyRelative("备注");
        SerializedProperty 步骤列表属性 = 条目属性.FindPropertyRelative("步骤列表");

        if (剧情ID属性 != null)
        {
            剧情ID属性.stringValue = 新剧情ID.Trim();
        }

        if (备注属性 != null)
        {
            备注属性.stringValue = 新备注.Trim();
        }

        if (步骤列表属性 != null)
        {
            步骤列表属性.ClearArray();
        }

        新剧情ID = string.Empty;
        新备注 = string.Empty;
        数据库对象.ApplyModifiedProperties();
        保存数据库(数据库);
    }

    private void 新增步骤(SerializedProperty 步骤列表属性, 剧情数据库.剧情步骤类型 步骤类型)
    {
        Undo.RecordObject(数据库对象.targetObject, "新增剧情步骤");
        int 新索引 = 步骤列表属性.arraySize;
        步骤列表属性.InsertArrayElementAtIndex(新索引);
        SerializedProperty 步骤属性 = 步骤列表属性.GetArrayElementAtIndex(新索引);
        初始化步骤属性(步骤属性, 步骤类型);
        数据库对象.ApplyModifiedProperties();
        保存数据库((剧情数据库)数据库对象.targetObject);
    }

    private static void 初始化步骤属性(SerializedProperty 步骤属性, 剧情数据库.剧情步骤类型 步骤类型)
    {
        设置步骤类型(步骤属性.FindPropertyRelative("步骤类型"), 步骤类型);
        设置字符串值(步骤属性.FindPropertyRelative("对话组ID"), string.Empty);
        设置字符串值(步骤属性.FindPropertyRelative("事件ID"), string.Empty);
        设置布尔值(步骤属性.FindPropertyRelative("事件状态"), true);
        设置场景目标类型(步骤属性.FindPropertyRelative("目标类型"), 剧情数据库.场景目标类型.普通场景);
        设置字符串值(步骤属性.FindPropertyRelative("场景名"), string.Empty);
        设置字符串值(步骤属性.FindPropertyRelative("地图模板ID"), string.Empty);
        设置字符串值(步骤属性.FindPropertyRelative("房间节点ID"), string.Empty);
        设置字符串值(步骤属性.FindPropertyRelative("步骤备注"), string.Empty);
    }

    private static void 绘制ID选择或输入(SerializedProperty 属性, string 标题, List<string> ID列表)
    {
        if (ID列表 == null || ID列表.Count <= 0)
        {
            属性.stringValue = EditorGUILayout.TextField(标题, 属性.stringValue);
            return;
        }

        List<string> 值列表 = new List<string> { string.Empty };
        List<string> 显示列表 = new List<string> { "未选择" };

        string 当前值 = 属性.stringValue;
        if (!string.IsNullOrWhiteSpace(当前值) && !ID列表.Contains(当前值))
        {
            值列表.Add(当前值);
            显示列表.Add(当前值 + "（未在数据库中）");
        }

        for (int i = 0; i < ID列表.Count; i++)
        {
            string id = ID列表[i];
            if (string.IsNullOrWhiteSpace(id) || 值列表.Contains(id))
            {
                continue;
            }

            值列表.Add(id);
            显示列表.Add(id);
        }

        int 当前索引 = Mathf.Max(0, 值列表.IndexOf(当前值));
        int 新索引 = EditorGUILayout.Popup(标题, 当前索引, 显示列表.ToArray());
        if (新索引 >= 0 && 新索引 < 值列表.Count)
        {
            属性.stringValue = 值列表[新索引];
        }
    }

    private static List<string> 读取对话组ID列表()
    {
        DialogueGroupDatabase 数据库 = DialogueGroupDatabase.LoadDefault();
        List<string> 结果 = new List<string>();
        if (数据库 == null || 数据库.Entries == null)
        {
            return 结果;
        }

        for (int i = 0; i < 数据库.Entries.Count; i++)
        {
            DialogueGroupDatabase.DialogueGroupEntry 条目 = 数据库.Entries[i];
            if (条目 != null && !string.IsNullOrWhiteSpace(条目.id))
            {
                结果.Add(条目.id);
            }
        }

        return 结果;
    }

    private static List<string> 读取事件ID列表()
    {
        EventDatabase 数据库 = EventDatabase.LoadDefault();
        List<string> 结果 = new List<string>();
        if (数据库 == null || 数据库.Entries == null)
        {
            return 结果;
        }

        for (int i = 0; i < 数据库.Entries.Count; i++)
        {
            EventDatabase.EventEntry 条目 = 数据库.Entries[i];
            if (条目 != null && !string.IsNullOrWhiteSpace(条目.eventId))
            {
                结果.Add(条目.eventId);
            }
        }

        return 结果;
    }

    private static List<string> 读取地图模板ID列表()
    {
        MapTemplateDatabase 数据库 = MapTemplateDatabase.LoadDefault();
        List<string> 结果 = new List<string>();
        if (数据库 == null || 数据库.Entries == null)
        {
            return 结果;
        }

        for (int i = 0; i < 数据库.Entries.Count; i++)
        {
            MapTemplateDatabase.MapTemplateEntry 条目 = 数据库.Entries[i];
            if (条目 != null && !string.IsNullOrWhiteSpace(条目.templateId))
            {
                结果.Add(条目.templateId);
            }
        }

        return 结果;
    }

    private static List<string> 读取房间节点ID列表(string 地图模板ID)
    {
        List<string> 结果 = new List<string>();
        if (string.IsNullOrWhiteSpace(地图模板ID))
        {
            return 结果;
        }

        MapTemplateDatabase 数据库 = MapTemplateDatabase.LoadDefault();
        MapTemplateDatabase.MapTemplateEntry 模板 = 数据库 != null ? 数据库.FindEntry(地图模板ID) : null;
        if (模板 == null || 模板.nodes == null)
        {
            return 结果;
        }

        for (int i = 0; i < 模板.nodes.Count; i++)
        {
            MapTemplateDatabase.MapNodeEntry 房间 = 模板.nodes[i];
            if (房间 != null && !string.IsNullOrWhiteSpace(房间.nodeId))
            {
                结果.Add(房间.nodeId);
            }
        }

        return 结果;
    }

    private static List<string> 读取构建场景名列表()
    {
        List<string> 结果 = new List<string>();
        EditorBuildSettingsScene[] 场景列表 = EditorBuildSettings.scenes;
        for (int i = 0; i < 场景列表.Length; i++)
        {
            EditorBuildSettingsScene 场景 = 场景列表[i];
            if (场景 == null || string.IsNullOrWhiteSpace(场景.path))
            {
                continue;
            }

            string 场景名 = System.IO.Path.GetFileNameWithoutExtension(场景.path);
            if (!string.IsNullOrWhiteSpace(场景名) && !结果.Contains(场景名))
            {
                结果.Add(场景名);
            }
        }

        return 结果;
    }

    private static string 取得步骤类型名字(SerializedProperty 步骤类型属性)
    {
        if (步骤类型属性 == null)
        {
            return "未知";
        }

        剧情数据库.剧情步骤类型 步骤类型 = (剧情数据库.剧情步骤类型)步骤类型属性.enumValueIndex;
        return 步骤类型.ToString();
    }

    private static string 取得剧情展开键(SerializedProperty 剧情ID属性, int 索引)
    {
        if (剧情ID属性 != null && !string.IsNullOrWhiteSpace(剧情ID属性.stringValue))
        {
            return 剧情ID属性.stringValue;
        }

        return "剧情索引_" + 索引;
    }

    private static bool 取得展开状态(Dictionary<string, bool> 状态表, string 键, bool 默认值)
    {
        bool 值;
        if (状态表.TryGetValue(键, out 值))
        {
            return 值;
        }

        状态表[键] = 默认值;
        return 默认值;
    }

    private static void 设置展开状态(Dictionary<string, bool> 状态表, string 键, bool 值)
    {
        状态表[键] = 值;
    }

    private static void 设置字符串值(SerializedProperty 属性, string 值)
    {
        if (属性 != null)
        {
            属性.stringValue = 值;
        }
    }

    private static void 设置布尔值(SerializedProperty 属性, bool 值)
    {
        if (属性 != null)
        {
            属性.boolValue = 值;
        }
    }

    private static void 设置步骤类型(SerializedProperty 属性, 剧情数据库.剧情步骤类型 值)
    {
        if (属性 != null)
        {
            属性.enumValueIndex = (int)值;
        }
    }

    private static void 设置场景目标类型(SerializedProperty 属性, 剧情数据库.场景目标类型 值)
    {
        if (属性 != null)
        {
            属性.enumValueIndex = (int)值;
        }
    }

    private static 剧情数据库 确保数据库()
    {
        剧情数据库 数据库 = AssetDatabase.LoadAssetAtPath<剧情数据库>(数据库路径);
        if (数据库 != null)
        {
            return 数据库;
        }

        if (!AssetDatabase.IsValidFolder(资源目录))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        数据库 = CreateInstance<剧情数据库>();
        AssetDatabase.CreateAsset(数据库, 数据库路径);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return 数据库;
    }

    private static void 保存数据库(剧情数据库 数据库)
    {
        EditorUtility.SetDirty(数据库);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}

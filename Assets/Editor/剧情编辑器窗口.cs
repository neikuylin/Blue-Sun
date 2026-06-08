using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class 剧情编辑器窗口 : EditorWindow
{
    private const string 资源目录 = "Assets/Resources";
    private const string 数据库路径 = 资源目录 + "/剧情数据库.asset";
    private const string 战斗副本场景名 = "战斗副本";
    private const float 蓝图画布宽度 = 1800f;
    private const float 蓝图画布高度 = 900f;
    private const float 蓝图节点宽度 = 180f;
    private const float 蓝图节点高度 = 74f;
    private const float 蓝图左侧栏宽度 = 220f;
    private const float 蓝图右侧栏宽度 = 340f;
    private const float 蓝图视图高度 = 520f;

    private readonly Dictionary<string, bool> 剧情展开状态 = new Dictionary<string, bool>();
    private bool 剧情管理展开状态 = true;

    private SerializedObject 数据库对象;
    private Vector2 滚动位置;
    private Vector2 蓝图滚动位置;
    private Vector2 蓝图详情滚动位置;
    private string 新剧情ID = string.Empty;
    private string 新备注 = string.Empty;
    private 剧情数据库.剧情蓝图节点类型 新蓝图节点类型 = 剧情数据库.剧情蓝图节点类型.播放一句对话;
    private string 选中蓝图节点ID = string.Empty;
    private string 连线来源节点ID = string.Empty;

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
        EditorGUILayout.HelpBox("当前编辑剧情蓝图节点数据：播放对话、设置事件、切换场景、添加物品到装备栏、黑幕淡入淡出、角色播放动画。这里不会直接执行剧情。", MessageType.Info);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("保存", GUILayout.Width(90f)))
            {
                数据库对象.ApplyModifiedProperties();
                保存数据库(数据库);
            }
        }

        EditorGUILayout.Space(8f);
        绘制剧情管理面板(数据库);

        if (数据库对象.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(数据库);
        }

    }

    private void 绘制剧情管理面板(剧情数据库 数据库)
    {
        using (new EditorGUILayout.VerticalScope("box"))
        {
            剧情管理展开状态 = EditorGUILayout.Foldout(剧情管理展开状态, "剧情", true);
            if (!剧情管理展开状态)
            {
                return;
            }

            绘制新增剧情内容(数据库);
            EditorGUILayout.Space(8f);
            绘制剧情列表();
        }
    }

    private void 绘制新增剧情内容(剧情数据库 数据库)
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
        SerializedProperty 蓝图节点列表属性 = 条目属性.FindPropertyRelative("蓝图节点列表");
        SerializedProperty 蓝图连线列表属性 = 条目属性.FindPropertyRelative("蓝图连线列表");
        string 展开键 = 取得剧情展开键(剧情ID属性, 索引);
        bool 已展开 = 取得展开状态(剧情展开状态, 展开键, false);

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
            绘制蓝图编辑器(蓝图节点列表属性, 蓝图连线列表属性, 展开键);
        }

        return false;
    }

    private void 绘制蓝图编辑器(SerializedProperty 节点列表属性, SerializedProperty 连线列表属性, string 剧情展开键)
    {
        if (节点列表属性 == null || 连线列表属性 == null)
        {
            EditorGUILayout.HelpBox("未找到蓝图数据字段。", MessageType.Error);
            return;
        }

        EditorGUILayout.LabelField("剧情蓝图", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("左侧新增和连线，中间拖动画布节点，右侧编辑选中节点。一个节点可以连多个目标；一个节点也可以接多个来源。", MessageType.None);

        using (new EditorGUILayout.HorizontalScope())
        {
            绘制蓝图左侧栏(节点列表属性);

            绘制蓝图画布(节点列表属性, 连线列表属性);

            绘制选中蓝图节点详情(节点列表属性, 连线列表属性);
        }
    }

    private void 绘制蓝图左侧栏(SerializedProperty 节点列表属性)
    {
        using (new EditorGUILayout.VerticalScope("box", GUILayout.Width(蓝图左侧栏宽度), GUILayout.Height(蓝图视图高度)))
        {
            EditorGUILayout.LabelField("新增", EditorStyles.boldLabel);
            新蓝图节点类型 = (剧情数据库.剧情蓝图节点类型)EditorGUILayout.EnumPopup("节点类型", 新蓝图节点类型);
            if (GUILayout.Button("新增节点", GUILayout.Height(28f)))
            {
                新增蓝图节点(节点列表属性, 新蓝图节点类型);
            }

            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("连线", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(选中蓝图节点ID)))
            {
                if (GUILayout.Button("从选中节点开始连线", GUILayout.Height(28f)))
                {
                    连线来源节点ID = 选中蓝图节点ID;
                }
            }

            if (!string.IsNullOrWhiteSpace(连线来源节点ID))
            {
                EditorGUILayout.HelpBox($"连线中：{连线来源节点ID}\n点击中间画布里的目标节点完成连线。", MessageType.Info);
                if (GUILayout.Button("取消连线", GUILayout.Height(26f)))
                {
                    连线来源节点ID = string.Empty;
                }
            }
            else
            {
                EditorGUILayout.HelpBox("先选中节点，再开始连线。", MessageType.None);
            }

            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("当前选中", EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(string.IsNullOrWhiteSpace(选中蓝图节点ID) ? "未选中" : 选中蓝图节点ID, GUILayout.Height(34f));
        }
    }

    private void 绘制蓝图画布(SerializedProperty 节点列表属性, SerializedProperty 连线列表属性)
    {
        Rect 可视区域 = GUILayoutUtility.GetRect(1f, 蓝图视图高度, GUILayout.ExpandWidth(true));
        GUI.Box(可视区域, GUIContent.none);
        蓝图滚动位置 = GUI.BeginScrollView(可视区域, 蓝图滚动位置, new Rect(0f, 0f, 蓝图画布宽度, 蓝图画布高度));
        Rect 画布矩形 = new Rect(0f, 0f, 蓝图画布宽度, 蓝图画布高度);
        绘制蓝图网格(画布矩形);
        绘制蓝图连线(节点列表属性, 连线列表属性);
        绘制蓝图节点(节点列表属性, 连线列表属性);
        GUI.EndScrollView();
    }

    private static void 绘制蓝图网格(Rect 画布矩形)
    {
        Handles.BeginGUI();
        Color 旧颜色 = Handles.color;
        Handles.color = new Color(1f, 1f, 1f, 0.05f);
        for (float x = 0f; x <= 画布矩形.width; x += 80f)
        {
            Handles.DrawLine(new Vector3(x, 0f), new Vector3(x, 画布矩形.height));
        }

        for (float y = 0f; y <= 画布矩形.height; y += 80f)
        {
            Handles.DrawLine(new Vector3(0f, y), new Vector3(画布矩形.width, y));
        }

        Handles.color = 旧颜色;
        Handles.EndGUI();
    }

    private void 绘制蓝图连线(SerializedProperty 节点列表属性, SerializedProperty 连线列表属性)
    {
        Handles.BeginGUI();
        Color 旧颜色 = Handles.color;
        for (int i = 0; i < 连线列表属性.arraySize; i++)
        {
            SerializedProperty 连线属性 = 连线列表属性.GetArrayElementAtIndex(i);
            string 来源节点ID = 读取字符串(连线属性.FindPropertyRelative("来源节点ID"));
            string 目标节点ID = 读取字符串(连线属性.FindPropertyRelative("目标节点ID"));
            Rect 来源矩形;
            Rect 目标矩形;
            if (!尝试取得蓝图节点矩形(节点列表属性, 来源节点ID, out 来源矩形) ||
                !尝试取得蓝图节点矩形(节点列表属性, 目标节点ID, out 目标矩形))
            {
                continue;
            }

            Vector2 起点 = new Vector2(来源矩形.xMax, 来源矩形.center.y);
            Vector2 终点 = new Vector2(目标矩形.xMin, 目标矩形.center.y);
            Handles.color = new Color(0.9f, 0.9f, 0.9f, 0.92f);
            Handles.DrawAAPolyLine(3f, 起点, 终点);
            绘制蓝图箭头(起点, 终点);
        }

        Handles.color = 旧颜色;
        Handles.EndGUI();
    }

    private static void 绘制蓝图箭头(Vector2 起点, Vector2 终点)
    {
        Vector2 方向 = (终点 - 起点).normalized;
        if (方向.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Vector2 法线 = new Vector2(-方向.y, 方向.x);
        Vector2 尖端 = 终点;
        Vector2 左 = 尖端 - 方向 * 12f + 法线 * 5f;
        Vector2 右 = 尖端 - 方向 * 12f - 法线 * 5f;
        Handles.DrawAAConvexPolygon(尖端, 左, 右);
    }

    private void 绘制蓝图节点(SerializedProperty 节点列表属性, SerializedProperty 连线列表属性)
    {
        Event 当前事件 = Event.current;
        for (int i = 0; i < 节点列表属性.arraySize; i++)
        {
            SerializedProperty 节点属性 = 节点列表属性.GetArrayElementAtIndex(i);
            SerializedProperty 节点ID属性 = 节点属性.FindPropertyRelative("节点ID");
            SerializedProperty 节点类型属性 = 节点属性.FindPropertyRelative("节点类型");
            SerializedProperty 位置属性 = 节点属性.FindPropertyRelative("位置");
            string 节点ID = 读取字符串(节点ID属性);
            Rect 节点矩形 = 取得蓝图节点矩形(位置属性);
            bool 已选中 = string.Equals(选中蓝图节点ID, 节点ID, System.StringComparison.Ordinal);
            Color 原颜色 = GUI.color;
            GUI.color = 已选中 ? new Color(1f, 0.86f, 0.45f, 1f) : Color.white;
            GUI.Box(节点矩形, GUIContent.none);
            GUI.color = 原颜色;

            Rect 标题矩形 = new Rect(节点矩形.x + 8f, 节点矩形.y + 6f, 节点矩形.width - 16f, 20f);
            Rect ID矩形 = new Rect(节点矩形.x + 8f, 节点矩形.y + 30f, 节点矩形.width - 16f, 18f);
            Rect 提示矩形 = new Rect(节点矩形.x + 8f, 节点矩形.y + 50f, 节点矩形.width - 16f, 18f);
            GUI.Label(标题矩形, 取得蓝图节点类型名字(节点类型属性), EditorStyles.boldLabel);
            GUI.Label(ID矩形, 节点ID, EditorStyles.miniLabel);
            GUI.Label(提示矩形, $"入 {统计输入线数量(连线列表属性, 节点ID)} / 出 {统计输出线数量(连线列表属性, 节点ID)}", EditorStyles.miniLabel);

            if (当前事件 == null)
            {
                continue;
            }

            if (当前事件.type == EventType.MouseDown && 当前事件.button == 0 && 节点矩形.Contains(当前事件.mousePosition))
            {
                if (!string.IsNullOrWhiteSpace(连线来源节点ID) && !string.Equals(连线来源节点ID, 节点ID, System.StringComparison.Ordinal))
                {
                    新增蓝图连线(连线列表属性, 连线来源节点ID, 节点ID);
                    连线来源节点ID = string.Empty;
                }

                选中蓝图节点ID = 节点ID;
                当前事件.Use();
            }
            else if (当前事件.type == EventType.MouseDrag && 当前事件.button == 0 && 已选中 && 节点矩形.Contains(当前事件.mousePosition))
            {
                Vector2 当前位置 = 位置属性.vector2Value;
                位置属性.vector2Value = new Vector2(
                    Mathf.Clamp(当前位置.x + 当前事件.delta.x, 0f, 蓝图画布宽度 - 蓝图节点宽度),
                    Mathf.Clamp(当前位置.y + 当前事件.delta.y, 0f, 蓝图画布高度 - 蓝图节点高度));
                数据库对象.ApplyModifiedProperties();
                EditorUtility.SetDirty(数据库对象.targetObject);
                Repaint();
                当前事件.Use();
            }
        }
    }

    private void 绘制选中蓝图节点详情(SerializedProperty 节点列表属性, SerializedProperty 连线列表属性)
    {
        using (new EditorGUILayout.VerticalScope("box", GUILayout.Width(蓝图右侧栏宽度), GUILayout.Height(蓝图视图高度)))
        {
            SerializedProperty 节点属性 = 查找蓝图节点属性(节点列表属性, 选中蓝图节点ID);
            if (节点属性 == null)
            {
                EditorGUILayout.LabelField("节点详情", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("未选中蓝图节点。", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("节点详情", EditorStyles.boldLabel);
            bool 请求删除节点 = false;
            蓝图详情滚动位置 = EditorGUILayout.BeginScrollView(蓝图详情滚动位置);
            SerializedProperty 节点ID属性 = 节点属性.FindPropertyRelative("节点ID");
            SerializedProperty 节点类型属性 = 节点属性.FindPropertyRelative("节点类型");
            string 旧节点ID = 节点ID属性.stringValue;
            EditorGUILayout.PropertyField(节点ID属性, new GUIContent("节点ID"));
            EditorGUILayout.PropertyField(节点类型属性, new GUIContent("节点类型"));
            if (!string.Equals(旧节点ID, 节点ID属性.stringValue, System.StringComparison.Ordinal))
            {
                重命名蓝图节点引用(连线列表属性, 旧节点ID, 节点ID属性.stringValue);
                选中蓝图节点ID = 节点ID属性.stringValue;
            }

            绘制蓝图节点字段(节点列表属性, 连线列表属性, 节点属性, 节点类型属性);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("删除节点", GUILayout.Width(90f)))
                {
                    请求删除节点 = true;
                }

                if (GUILayout.Button("删除此节点所有连线", GUILayout.Width(140f)))
                {
                    删除蓝图节点相关连线(连线列表属性, 选中蓝图节点ID);
                }
            }

            EditorGUILayout.EndScrollView();

            if (请求删除节点)
            {
                删除蓝图节点(节点列表属性, 连线列表属性, 选中蓝图节点ID);
                选中蓝图节点ID = string.Empty;
                GUIUtility.ExitGUI();
            }
        }
    }

    private void 绘制播放对话字段(SerializedProperty 节点列表属性, SerializedProperty 连线列表属性, SerializedProperty 节点属性)
    {
        SerializedProperty 对话组ID属性 = 节点属性.FindPropertyRelative("对话组ID");
        if (对话组ID属性 == null)
        {
            return;
        }

        string 旧对话组ID = 对话组ID属性.stringValue;
        绘制ID选择或输入(对话组ID属性, "对话组ID", 读取对话组ID列表());
        if (!string.Equals(旧对话组ID, 对话组ID属性.stringValue, System.StringComparison.Ordinal))
        {
            生成对话组子节点(节点列表属性, 连线列表属性, 节点属性, 对话组ID属性.stringValue);
        }
    }

    private void 绘制设置事件字段(SerializedProperty 节点属性)
    {
        SerializedProperty 事件ID属性 = 节点属性.FindPropertyRelative("事件ID");
        SerializedProperty 事件状态属性 = 节点属性.FindPropertyRelative("事件状态");

        if (事件ID属性 != null)
        {
            绘制ID选择或输入(事件ID属性, "事件ID", 读取事件ID列表());
        }

        if (事件状态属性 != null)
        {
            事件状态属性.boolValue = EditorGUILayout.Toggle("设置为启用", 事件状态属性.boolValue);
        }
    }

    private void 绘制切换场景字段(SerializedProperty 节点属性)
    {
        SerializedProperty 场景目标类型属性 = 节点属性.FindPropertyRelative("目标类型");
        SerializedProperty 场景名属性 = 节点属性.FindPropertyRelative("场景名");
        SerializedProperty 地图模板ID属性 = 节点属性.FindPropertyRelative("地图模板ID");
        SerializedProperty 房间节点ID属性 = 节点属性.FindPropertyRelative("房间节点ID");

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

    private void 绘制添加物品到装备栏字段(SerializedProperty 节点属性)
    {
        SerializedProperty 角色ID属性 = 节点属性.FindPropertyRelative("角色ID");
        SerializedProperty 物品ID属性 = 节点属性.FindPropertyRelative("物品ID");
        SerializedProperty 装备格子索引属性 = 节点属性.FindPropertyRelative("装备格子索引");

        if (角色ID属性 != null)
        {
            绘制ID选择或输入(角色ID属性, "角色ID", 读取战斗角色ID列表());
        }

        if (物品ID属性 != null)
        {
            绘制ID选择或输入(物品ID属性, "物品ID", 读取物品ID列表());
        }

        if (装备格子索引属性 != null)
        {
            装备格子索引属性.intValue = EditorGUILayout.IntField("装备格子索引", 装备格子索引属性.intValue);
        }
    }

    private static void 绘制黑幕淡入字段(SerializedProperty 节点属性)
    {
        SerializedProperty 持续时间属性 = 节点属性.FindPropertyRelative("持续时间");
        SerializedProperty 目标不透明度属性 = 节点属性.FindPropertyRelative("目标不透明度");

        if (持续时间属性 != null)
        {
            持续时间属性.floatValue = EditorGUILayout.FloatField("持续时间（秒）", 持续时间属性.floatValue);
        }

        if (目标不透明度属性 != null)
        {
            目标不透明度属性.floatValue = EditorGUILayout.Slider("目标不透明度", 目标不透明度属性.floatValue, 0f, 1f);
        }
    }

    private static void 绘制黑幕淡出字段(SerializedProperty 节点属性)
    {
        SerializedProperty 持续时间属性 = 节点属性.FindPropertyRelative("持续时间");
        if (持续时间属性 != null)
        {
            持续时间属性.floatValue = EditorGUILayout.FloatField("持续时间（秒）", 持续时间属性.floatValue);
        }
    }

    private void 绘制角色播放动画字段(SerializedProperty 节点属性)
    {
        SerializedProperty 角色ID属性 = 节点属性.FindPropertyRelative("角色ID");
        SerializedProperty 动作控制器属性 = 节点属性.FindPropertyRelative("动作控制器");
        SerializedProperty 动画状态名属性 = 节点属性.FindPropertyRelative("动画状态名");

        if (角色ID属性 != null)
        {
            绘制ID选择或输入(角色ID属性, "角色ID", 读取战斗角色ID列表());
        }

        if (动作控制器属性 != null)
        {
            动作控制器属性.objectReferenceValue = EditorGUILayout.ObjectField(
                "动作控制器",
                动作控制器属性.objectReferenceValue,
                typeof(RuntimeAnimatorController),
                false);
        }

        if (动画状态名属性 != null)
        {
            动画状态名属性.stringValue = EditorGUILayout.TextField("动画状态名", 动画状态名属性.stringValue);
        }
    }

    private void 绘制蓝图节点字段(SerializedProperty 节点列表属性, SerializedProperty 连线列表属性, SerializedProperty 节点属性, SerializedProperty 节点类型属性)
    {
        if (节点类型属性 == null || 节点属性 == null)
        {
            return;
        }

        剧情数据库.剧情蓝图节点类型 节点类型 = (剧情数据库.剧情蓝图节点类型)节点类型属性.enumValueIndex;
        switch (节点类型)
        {
            case 剧情数据库.剧情蓝图节点类型.开始:
            case 剧情数据库.剧情蓝图节点类型.汇合:
                EditorGUILayout.HelpBox("这个节点只负责流程连接，不需要额外参数。", MessageType.None);
                break;
            case 剧情数据库.剧情蓝图节点类型.播放一句对话:
                绘制播放一句对话字段(节点属性);
                break;
            case 剧情数据库.剧情蓝图节点类型.播放对话组:
                绘制播放对话字段(节点列表属性, 连线列表属性, 节点属性);
                break;
            case 剧情数据库.剧情蓝图节点类型.设置事件:
                绘制设置事件字段(节点属性);
                break;
            case 剧情数据库.剧情蓝图节点类型.切换场景:
                绘制切换场景字段(节点属性);
                break;
            case 剧情数据库.剧情蓝图节点类型.添加物品到装备栏:
                绘制添加物品到装备栏字段(节点属性);
                break;
            case 剧情数据库.剧情蓝图节点类型.黑幕淡入:
                绘制黑幕淡入字段(节点属性);
                break;
            case 剧情数据库.剧情蓝图节点类型.黑幕淡出:
                绘制黑幕淡出字段(节点属性);
                break;
            case 剧情数据库.剧情蓝图节点类型.角色播放动画:
                绘制角色播放动画字段(节点属性);
                break;
            case 剧情数据库.剧情蓝图节点类型.等待:
                绘制等待字段(节点属性);
                break;
        }

        SerializedProperty 节点备注属性 = 节点属性.FindPropertyRelative("节点备注");
        if (节点备注属性 != null)
        {
            EditorGUILayout.PropertyField(节点备注属性, new GUIContent("节点备注"));
        }
    }

    private void 绘制播放一句对话字段(SerializedProperty 节点属性)
    {
        SerializedProperty 对话内容ID属性 = 节点属性.FindPropertyRelative("对话内容ID");
        if (对话内容ID属性 != null)
        {
            绘制ID选择或输入(对话内容ID属性, "对话内容ID", 读取对话内容ID列表());
        }
    }

    private static void 绘制等待字段(SerializedProperty 节点属性)
    {
        SerializedProperty 持续时间属性 = 节点属性.FindPropertyRelative("持续时间");
        if (持续时间属性 != null)
        {
            持续时间属性.floatValue = EditorGUILayout.FloatField("等待时间（秒）", 持续时间属性.floatValue);
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
        SerializedProperty 蓝图节点列表属性 = 条目属性.FindPropertyRelative("蓝图节点列表");
        SerializedProperty 蓝图连线列表属性 = 条目属性.FindPropertyRelative("蓝图连线列表");

        if (剧情ID属性 != null)
        {
            剧情ID属性.stringValue = 新剧情ID.Trim();
        }

        if (备注属性 != null)
        {
            备注属性.stringValue = 新备注.Trim();
        }

        if (蓝图节点列表属性 != null)
        {
            蓝图节点列表属性.ClearArray();
        }

        if (蓝图连线列表属性 != null)
        {
            蓝图连线列表属性.ClearArray();
        }

        新剧情ID = string.Empty;
        新备注 = string.Empty;
        数据库对象.ApplyModifiedProperties();
        保存数据库(数据库);
    }

    private static void 初始化蓝图节点属性(SerializedProperty 节点属性, 剧情数据库.剧情蓝图节点类型 节点类型)
    {
        if (节点属性 == null)
        {
            return;
        }

        设置字符串值(节点属性.FindPropertyRelative("对话组ID"), string.Empty);
        设置字符串值(节点属性.FindPropertyRelative("对话内容ID"), string.Empty);
        设置字符串值(节点属性.FindPropertyRelative("事件ID"), string.Empty);
        设置布尔值(节点属性.FindPropertyRelative("事件状态"), true);
        设置场景目标类型(节点属性.FindPropertyRelative("目标类型"), 剧情数据库.场景目标类型.普通场景);
        设置字符串值(节点属性.FindPropertyRelative("场景名"), string.Empty);
        设置字符串值(节点属性.FindPropertyRelative("地图模板ID"), string.Empty);
        设置字符串值(节点属性.FindPropertyRelative("房间节点ID"), string.Empty);
        设置字符串值(节点属性.FindPropertyRelative("角色ID"), string.Empty);
        设置字符串值(节点属性.FindPropertyRelative("物品ID"), string.Empty);
        设置整数值(节点属性.FindPropertyRelative("装备格子索引"), 0);
        设置浮点值(节点属性.FindPropertyRelative("持续时间"), 1f);
        设置浮点值(节点属性.FindPropertyRelative("目标不透明度"), 节点类型 == 剧情数据库.剧情蓝图节点类型.黑幕淡入 ? 1f : 0f);
        设置对象值(节点属性.FindPropertyRelative("动作控制器"), null);
        设置字符串值(节点属性.FindPropertyRelative("动画状态名"), string.Empty);
        设置字符串值(节点属性.FindPropertyRelative("节点备注"), string.Empty);
    }

    private void 新增蓝图节点(SerializedProperty 节点列表属性, 剧情数据库.剧情蓝图节点类型 节点类型)
    {
        Undo.RecordObject(数据库对象.targetObject, "新增剧情蓝图节点");
        int 新索引 = 节点列表属性.arraySize;
        节点列表属性.InsertArrayElementAtIndex(新索引);
        SerializedProperty 节点属性 = 节点列表属性.GetArrayElementAtIndex(新索引);
        SerializedProperty 节点ID属性 = 节点属性.FindPropertyRelative("节点ID");
        SerializedProperty 节点类型属性 = 节点属性.FindPropertyRelative("节点类型");
        SerializedProperty 位置属性 = 节点属性.FindPropertyRelative("位置");

        string 节点ID = 生成蓝图节点ID(节点列表属性, 节点类型);
        设置字符串值(节点ID属性, 节点ID);
        if (节点类型属性 != null)
        {
            节点类型属性.enumValueIndex = (int)节点类型;
        }

        if (位置属性 != null)
        {
            位置属性.vector2Value = new Vector2(80f + (新索引 % 5) * 230f, 80f + (新索引 / 5) * 120f);
        }

        初始化蓝图节点属性(节点属性, 节点类型);
        选中蓝图节点ID = 节点ID;
        数据库对象.ApplyModifiedProperties();
        保存数据库((剧情数据库)数据库对象.targetObject);
    }

    private void 新增蓝图连线(SerializedProperty 连线列表属性, string 来源节点ID, string 目标节点ID)
    {
        if (string.IsNullOrWhiteSpace(来源节点ID) || string.IsNullOrWhiteSpace(目标节点ID) || 蓝图连线已存在(连线列表属性, 来源节点ID, 目标节点ID))
        {
            return;
        }

        Undo.RecordObject(数据库对象.targetObject, "新增剧情蓝图连线");
        int 新索引 = 连线列表属性.arraySize;
        连线列表属性.InsertArrayElementAtIndex(新索引);
        SerializedProperty 连线属性 = 连线列表属性.GetArrayElementAtIndex(新索引);
        设置字符串值(连线属性.FindPropertyRelative("来源节点ID"), 来源节点ID);
        设置字符串值(连线属性.FindPropertyRelative("目标节点ID"), 目标节点ID);
        数据库对象.ApplyModifiedProperties();
        保存数据库((剧情数据库)数据库对象.targetObject);
    }

    private void 生成对话组子节点(SerializedProperty 节点列表属性, SerializedProperty 连线列表属性, SerializedProperty 对话组节点属性, string 对话组ID)
    {
        if (节点列表属性 == null || 连线列表属性 == null || 对话组节点属性 == null)
        {
            return;
        }

        string 对话组节点ID = 读取字符串(对话组节点属性.FindPropertyRelative("节点ID"));
        if (string.IsNullOrWhiteSpace(对话组节点ID))
        {
            return;
        }

        Undo.RecordObject(数据库对象.targetObject, "生成对话组逐句节点");
        删除对话组自动子节点(节点列表属性, 连线列表属性, 对话组节点ID);

        List<string> 对话内容ID列表 = 读取对话组内容ID列表(对话组ID);
        if (对话内容ID列表.Count <= 0)
        {
            数据库对象.ApplyModifiedProperties();
            保存数据库((剧情数据库)数据库对象.targetObject);
            return;
        }

        Vector2 起始位置 = 对话组节点属性.FindPropertyRelative("位置") != null
            ? 对话组节点属性.FindPropertyRelative("位置").vector2Value
            : Vector2.zero;
        string 上一个节点ID = 对话组节点ID;
        for (int i = 0; i < 对话内容ID列表.Count; i++)
        {
            string 对话内容ID = 对话内容ID列表[i];
            int 新索引 = 节点列表属性.arraySize;
            节点列表属性.InsertArrayElementAtIndex(新索引);
            SerializedProperty 新节点属性 = 节点列表属性.GetArrayElementAtIndex(新索引);
            string 新节点ID = 生成唯一蓝图节点ID(节点列表属性, $"{对话组节点ID}_对话_{i + 1}");

            设置字符串值(新节点属性.FindPropertyRelative("节点ID"), 新节点ID);
            SerializedProperty 节点类型属性 = 新节点属性.FindPropertyRelative("节点类型");
            if (节点类型属性 != null)
            {
                节点类型属性.enumValueIndex = (int)剧情数据库.剧情蓝图节点类型.播放一句对话;
            }

            SerializedProperty 位置属性 = 新节点属性.FindPropertyRelative("位置");
            if (位置属性 != null)
            {
                位置属性.vector2Value = new Vector2(起始位置.x + 240f, 起始位置.y + i * 105f);
            }

            初始化蓝图节点属性(新节点属性, 剧情数据库.剧情蓝图节点类型.播放一句对话);
            设置字符串值(新节点属性.FindPropertyRelative("对话内容ID"), 对话内容ID);
            设置字符串值(新节点属性.FindPropertyRelative("节点备注"), $"由对话组“{对话组ID}”自动生成");
            新增蓝图连线不保存(连线列表属性, 上一个节点ID, 新节点ID);
            上一个节点ID = 新节点ID;
        }

        数据库对象.ApplyModifiedProperties();
        保存数据库((剧情数据库)数据库对象.targetObject);
    }

    private void 删除对话组自动子节点(SerializedProperty 节点列表属性, SerializedProperty 连线列表属性, string 对话组节点ID)
    {
        string 自动前缀 = 对话组节点ID + "_对话_";
        for (int i = 节点列表属性.arraySize - 1; i >= 0; i--)
        {
            SerializedProperty 节点属性 = 节点列表属性.GetArrayElementAtIndex(i);
            string 节点ID = 读取字符串(节点属性.FindPropertyRelative("节点ID"));
            if (!节点ID.StartsWith(自动前缀, System.StringComparison.Ordinal))
            {
                continue;
            }

            删除蓝图节点相关连线不保存(连线列表属性, 节点ID);
            节点列表属性.DeleteArrayElementAtIndex(i);
        }

        删除蓝图节点相关连线不保存(连线列表属性, 对话组节点ID, 自动前缀);
    }

    private void 新增蓝图连线不保存(SerializedProperty 连线列表属性, string 来源节点ID, string 目标节点ID)
    {
        if (string.IsNullOrWhiteSpace(来源节点ID) || string.IsNullOrWhiteSpace(目标节点ID) || 蓝图连线已存在(连线列表属性, 来源节点ID, 目标节点ID))
        {
            return;
        }

        int 新索引 = 连线列表属性.arraySize;
        连线列表属性.InsertArrayElementAtIndex(新索引);
        SerializedProperty 连线属性 = 连线列表属性.GetArrayElementAtIndex(新索引);
        设置字符串值(连线属性.FindPropertyRelative("来源节点ID"), 来源节点ID);
        设置字符串值(连线属性.FindPropertyRelative("目标节点ID"), 目标节点ID);
    }

    private void 删除蓝图节点相关连线不保存(SerializedProperty 连线列表属性, string 节点ID)
    {
        删除蓝图节点相关连线不保存(连线列表属性, 节点ID, string.Empty);
    }

    private void 删除蓝图节点相关连线不保存(SerializedProperty 连线列表属性, string 节点ID, string 目标节点前缀)
    {
        if (连线列表属性 == null || string.IsNullOrWhiteSpace(节点ID))
        {
            return;
        }

        for (int i = 连线列表属性.arraySize - 1; i >= 0; i--)
        {
            SerializedProperty 连线属性 = 连线列表属性.GetArrayElementAtIndex(i);
            string 来源节点ID = 读取字符串(连线属性.FindPropertyRelative("来源节点ID"));
            string 目标节点ID = 读取字符串(连线属性.FindPropertyRelative("目标节点ID"));
            bool 只按前缀删除 = !string.IsNullOrWhiteSpace(目标节点前缀);
            bool 命中节点 = !只按前缀删除 &&
                (string.Equals(来源节点ID, 节点ID, System.StringComparison.Ordinal) ||
                    string.Equals(目标节点ID, 节点ID, System.StringComparison.Ordinal));
            bool 命中前缀 = !string.IsNullOrWhiteSpace(目标节点前缀) &&
                string.Equals(来源节点ID, 节点ID, System.StringComparison.Ordinal) &&
                目标节点ID.StartsWith(目标节点前缀, System.StringComparison.Ordinal);
            if (命中节点 || 命中前缀)
            {
                连线列表属性.DeleteArrayElementAtIndex(i);
            }
        }
    }

    private void 删除蓝图节点(SerializedProperty 节点列表属性, SerializedProperty 连线列表属性, string 节点ID)
    {
        Undo.RecordObject(数据库对象.targetObject, "删除剧情蓝图节点");
        删除蓝图节点相关连线(连线列表属性, 节点ID);
        for (int i = 节点列表属性.arraySize - 1; i >= 0; i--)
        {
            SerializedProperty 节点属性 = 节点列表属性.GetArrayElementAtIndex(i);
            if (string.Equals(读取字符串(节点属性.FindPropertyRelative("节点ID")), 节点ID, System.StringComparison.Ordinal))
            {
                节点列表属性.DeleteArrayElementAtIndex(i);
            }
        }

        数据库对象.ApplyModifiedProperties();
        保存数据库((剧情数据库)数据库对象.targetObject);
    }

    private void 删除蓝图节点相关连线(SerializedProperty 连线列表属性, string 节点ID)
    {
        if (连线列表属性 == null || string.IsNullOrWhiteSpace(节点ID))
        {
            return;
        }

        Undo.RecordObject(数据库对象.targetObject, "删除剧情蓝图连线");
        for (int i = 连线列表属性.arraySize - 1; i >= 0; i--)
        {
            SerializedProperty 连线属性 = 连线列表属性.GetArrayElementAtIndex(i);
            string 来源节点ID = 读取字符串(连线属性.FindPropertyRelative("来源节点ID"));
            string 目标节点ID = 读取字符串(连线属性.FindPropertyRelative("目标节点ID"));
            if (string.Equals(来源节点ID, 节点ID, System.StringComparison.Ordinal) ||
                string.Equals(目标节点ID, 节点ID, System.StringComparison.Ordinal))
            {
                连线列表属性.DeleteArrayElementAtIndex(i);
            }
        }

        数据库对象.ApplyModifiedProperties();
        保存数据库((剧情数据库)数据库对象.targetObject);
    }

    private static void 重命名蓝图节点引用(SerializedProperty 连线列表属性, string 旧节点ID, string 新节点ID)
    {
        if (连线列表属性 == null || string.IsNullOrWhiteSpace(旧节点ID))
        {
            return;
        }

        for (int i = 0; i < 连线列表属性.arraySize; i++)
        {
            SerializedProperty 连线属性 = 连线列表属性.GetArrayElementAtIndex(i);
            SerializedProperty 来源节点ID属性 = 连线属性.FindPropertyRelative("来源节点ID");
            SerializedProperty 目标节点ID属性 = 连线属性.FindPropertyRelative("目标节点ID");
            if (来源节点ID属性 != null && string.Equals(来源节点ID属性.stringValue, 旧节点ID, System.StringComparison.Ordinal))
            {
                来源节点ID属性.stringValue = 新节点ID;
            }

            if (目标节点ID属性 != null && string.Equals(目标节点ID属性.stringValue, 旧节点ID, System.StringComparison.Ordinal))
            {
                目标节点ID属性.stringValue = 新节点ID;
            }
        }
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

    private static List<string> 读取对话组内容ID列表(string 对话组ID)
    {
        List<string> 结果 = new List<string>();
        if (string.IsNullOrWhiteSpace(对话组ID))
        {
            return 结果;
        }

        DialogueGroupDatabase 数据库 = DialogueGroupDatabase.LoadDefault();
        DialogueGroupDatabase.DialogueGroupEntry 对话组 = 数据库 != null ? 数据库.FindEntry(对话组ID) : null;
        if (对话组 == null || 对话组.contentIds == null)
        {
            return 结果;
        }

        for (int i = 0; i < 对话组.contentIds.Count; i++)
        {
            string 对话内容ID = 对话组.contentIds[i];
            if (!string.IsNullOrWhiteSpace(对话内容ID))
            {
                结果.Add(对话内容ID.Trim());
            }
        }

        return 结果;
    }

    private static List<string> 读取对话内容ID列表()
    {
        DialogueContentDatabase 数据库 = DialogueContentDatabase.LoadDefault();
        List<string> 结果 = new List<string>();
        if (数据库 == null || 数据库.Entries == null)
        {
            return 结果;
        }

        for (int i = 0; i < 数据库.Entries.Count; i++)
        {
            DialogueContentDatabase.DialogueContentEntry 条目 = 数据库.Entries[i];
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

    private static List<string> 读取战斗角色ID列表()
    {
        BattleCharacterBindingDatabase 数据库 = BattleCharacterBindingDatabase.LoadDefault();
        List<string> 结果 = new List<string>();
        if (数据库 == null || 数据库.Entries == null)
        {
            return 结果;
        }

        for (int i = 0; i < 数据库.Entries.Count; i++)
        {
            BattleCharacterBindingDatabase.BindingEntry 条目 = 数据库.Entries[i];
            if (条目 != null && !string.IsNullOrWhiteSpace(条目.characterId))
            {
                结果.Add(条目.characterId);
            }
        }

        return 结果;
    }

    private static List<string> 读取物品ID列表()
    {
        ItemDatabase 数据库 = ItemDatabase.LoadDefault();
        List<string> 结果 = new List<string>();
        if (数据库 == null || 数据库.Entries == null)
        {
            return 结果;
        }

        for (int i = 0; i < 数据库.Entries.Count; i++)
        {
            ItemDatabase.ItemEntry 条目 = 数据库.Entries[i];
            if (条目 != null && !string.IsNullOrWhiteSpace(条目.itemId))
            {
                结果.Add(条目.itemId);
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

    private static string 取得蓝图节点类型名字(SerializedProperty 节点类型属性)
    {
        if (节点类型属性 == null)
        {
            return "未知";
        }

        剧情数据库.剧情蓝图节点类型 节点类型 = (剧情数据库.剧情蓝图节点类型)节点类型属性.enumValueIndex;
        return 节点类型.ToString();
    }

    private static Rect 取得蓝图节点矩形(SerializedProperty 位置属性)
    {
        Vector2 位置 = 位置属性 != null ? 位置属性.vector2Value : Vector2.zero;
        return new Rect(位置.x, 位置.y, 蓝图节点宽度, 蓝图节点高度);
    }

    private static bool 尝试取得蓝图节点矩形(SerializedProperty 节点列表属性, string 节点ID, out Rect 节点矩形)
    {
        SerializedProperty 节点属性 = 查找蓝图节点属性(节点列表属性, 节点ID);
        if (节点属性 == null)
        {
            节点矩形 = default;
            return false;
        }

        节点矩形 = 取得蓝图节点矩形(节点属性.FindPropertyRelative("位置"));
        return true;
    }

    private static SerializedProperty 查找蓝图节点属性(SerializedProperty 节点列表属性, string 节点ID)
    {
        if (节点列表属性 == null || string.IsNullOrWhiteSpace(节点ID))
        {
            return null;
        }

        for (int i = 0; i < 节点列表属性.arraySize; i++)
        {
            SerializedProperty 节点属性 = 节点列表属性.GetArrayElementAtIndex(i);
            if (string.Equals(读取字符串(节点属性.FindPropertyRelative("节点ID")), 节点ID, System.StringComparison.Ordinal))
            {
                return 节点属性;
            }
        }

        return null;
    }

    private static int 统计输入线数量(SerializedProperty 连线列表属性, string 节点ID)
    {
        int 数量 = 0;
        for (int i = 0; 连线列表属性 != null && i < 连线列表属性.arraySize; i++)
        {
            SerializedProperty 连线属性 = 连线列表属性.GetArrayElementAtIndex(i);
            if (string.Equals(读取字符串(连线属性.FindPropertyRelative("目标节点ID")), 节点ID, System.StringComparison.Ordinal))
            {
                数量++;
            }
        }

        return 数量;
    }

    private static int 统计输出线数量(SerializedProperty 连线列表属性, string 节点ID)
    {
        int 数量 = 0;
        for (int i = 0; 连线列表属性 != null && i < 连线列表属性.arraySize; i++)
        {
            SerializedProperty 连线属性 = 连线列表属性.GetArrayElementAtIndex(i);
            if (string.Equals(读取字符串(连线属性.FindPropertyRelative("来源节点ID")), 节点ID, System.StringComparison.Ordinal))
            {
                数量++;
            }
        }

        return 数量;
    }

    private static string 读取字符串(SerializedProperty 属性)
    {
        return 属性 != null ? 属性.stringValue ?? string.Empty : string.Empty;
    }

    private static bool 蓝图连线已存在(SerializedProperty 连线列表属性, string 来源节点ID, string 目标节点ID)
    {
        if (连线列表属性 == null)
        {
            return false;
        }

        for (int i = 0; i < 连线列表属性.arraySize; i++)
        {
            SerializedProperty 连线属性 = 连线列表属性.GetArrayElementAtIndex(i);
            if (string.Equals(读取字符串(连线属性.FindPropertyRelative("来源节点ID")), 来源节点ID, System.StringComparison.Ordinal) &&
                string.Equals(读取字符串(连线属性.FindPropertyRelative("目标节点ID")), 目标节点ID, System.StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static string 生成蓝图节点ID(SerializedProperty 节点列表属性, 剧情数据库.剧情蓝图节点类型 节点类型)
    {
        string 前缀 = 节点类型.ToString();
        int 序号 = 1;
        string 候选ID;
        do
        {
            候选ID = $"{前缀}_{序号}";
            序号++;
        }
        while (查找蓝图节点属性(节点列表属性, 候选ID) != null);

        return 候选ID;
    }

    private static string 生成唯一蓝图节点ID(SerializedProperty 节点列表属性, string 基础ID)
    {
        string 前缀 = string.IsNullOrWhiteSpace(基础ID) ? "节点" : 基础ID.Trim();
        string 候选ID = 前缀;
        int 序号 = 2;
        while (查找蓝图节点属性(节点列表属性, 候选ID) != null)
        {
            候选ID = $"{前缀}_{序号}";
            序号++;
        }

        return 候选ID;
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

    private static void 设置整数值(SerializedProperty 属性, int 值)
    {
        if (属性 != null)
        {
            属性.intValue = 值;
        }
    }

    private static void 设置浮点值(SerializedProperty 属性, float 值)
    {
        if (属性 != null)
        {
            属性.floatValue = 值;
        }
    }

    private static void 设置对象值(SerializedProperty 属性, UnityEngine.Object 值)
    {
        if (属性 != null)
        {
            属性.objectReferenceValue = 值;
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

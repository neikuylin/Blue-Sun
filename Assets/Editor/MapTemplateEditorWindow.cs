using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class MapTemplateEditorWindow : EditorWindow
{
    private const string ResourceFolder = "Assets/Resources";
    private const string MapTemplateAssetPath = ResourceFolder + "/MapTemplateDatabase.asset";
    private const string RoomTypeAssetPath = ResourceFolder + "/RoomTypeDatabase.asset";
    private const string EncounterPresetAssetPath = ResourceFolder + "/RoomEnemyPresetDatabase.asset";
    private const float CanvasWidth = 2400f;
    private const float CanvasHeight = 1600f;
    private const float NodeWidth = 148f;
    private const float NodeHeight = 64f;

    private Vector2 templateScroll;
    private Vector2 canvasScroll;
    private Vector2 inspectorScroll;
    private string newTemplateId = string.Empty;

    private string selectedTemplateId = string.Empty;
    private string selectedNodeId = string.Empty;
    private string connectSourceNodeId = string.Empty;

    private bool isDraggingNode;
    private string draggingNodeId = string.Empty;
    private Vector2 dragOffset;

    [MenuItem("Tools/地图/地图模板编辑器")]
    private static void Open()
    {
        MapTemplateEditorWindow window = GetWindow<MapTemplateEditorWindow>("地图模板编辑器");
        window.minSize = new Vector2(1180f, 720f);
        window.Show();
        window.Focus();
    }

    private void OnGUI()
    {
        MapTemplateDatabase database = EnsureDatabase();
        RoomTypeDatabase roomTypeDatabase = EnsureRoomTypeDatabase();
        RoomEnemyPresetDatabase encounterDatabase = EnsureEncounterDatabase();

        if (database == null)
        {
            EditorGUILayout.HelpBox("地图模板库创建失败。", MessageType.Error);
            return;
        }

        EnsureValidSelection(database);
        EditorGUI.BeginChangeCheck();

        DrawToolbar(database);
        EditorGUILayout.Space(6f);

        using (new EditorGUILayout.HorizontalScope())
        {
            DrawTemplateList(database);
            DrawCanvasPanel(database, roomTypeDatabase, encounterDatabase);
            DrawInspectorPanel(database, roomTypeDatabase, encounterDatabase);
        }

        if (EditorGUI.EndChangeCheck())
        {
            MarkDirty(database);
        }
    }

    private void DrawToolbar(MapTemplateDatabase database)
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            if (GUILayout.Button("保存", EditorStyles.toolbarButton, GUILayout.Width(72f)))
            {
                SaveAsset(database);
            }

            if (GUILayout.Button("新增模板", EditorStyles.toolbarButton, GUILayout.Width(84f)))
            {
                CreateTemplate(database);
            }

            using (new EditorGUI.DisabledScope(GetSelectedTemplate(database) == null))
            {
                if (GUILayout.Button("新增节点", EditorStyles.toolbarButton, GUILayout.Width(84f)))
                {
                    AddNodeToSelectedTemplate(database);
                }

                if (GUILayout.Button("45°自动布局", EditorStyles.toolbarButton, GUILayout.Width(108f)))
                {
                    AutoLayoutSelectedTemplate(database);
                }
            }

            GUILayout.Space(8f);
            newTemplateId = GUILayout.TextField(newTemplateId, EditorStyles.toolbarTextField, GUILayout.Width(200f));

            GUILayout.FlexibleSpace();

            if (!string.IsNullOrWhiteSpace(connectSourceNodeId))
            {
                GUILayout.Label($"连线中：{connectSourceNodeId}", EditorStyles.miniLabel);
                if (GUILayout.Button("取消连线", EditorStyles.toolbarButton, GUILayout.Width(84f)))
                {
                    connectSourceNodeId = string.Empty;
                }
            }
        }
    }

    private void DrawTemplateList(MapTemplateDatabase database)
    {
        using (new EditorGUILayout.VerticalScope(GUILayout.Width(250f)))
        {
            EditorGUILayout.LabelField("地图模板", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("左侧选模板，中间编辑节点和连线，右侧改节点内容。", MessageType.Info);

            templateScroll = EditorGUILayout.BeginScrollView(templateScroll, "box", GUILayout.ExpandHeight(true));
            List<MapTemplateDatabase.MapTemplateEntry> entries = database.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                MapTemplateDatabase.MapTemplateEntry template = entries[i];
                if (template == null)
                {
                    continue;
                }

                MapTemplateDatabase.EnsureValidTemplate(template);
                bool isSelected = string.Equals(selectedTemplateId, template.templateId, StringComparison.Ordinal);
                GUIStyle buttonStyle = isSelected ? EditorStyles.miniButtonMid : EditorStyles.miniButton;
                string label = string.IsNullOrWhiteSpace(template.displayName)
                    ? template.templateId
                    : $"{template.displayName} ({template.templateId})";

                if (GUILayout.Button(label, buttonStyle, GUILayout.Height(34f)))
                {
                    selectedTemplateId = template.templateId;
                    selectedNodeId = string.Empty;
                    connectSourceNodeId = string.Empty;
                    GUI.FocusControl(null);
                }
            }
            EditorGUILayout.EndScrollView();

            using (new EditorGUI.DisabledScope(GetSelectedTemplate(database) == null))
            {
                if (GUILayout.Button("删除当前模板"))
                {
                    RemoveSelectedTemplate(database);
                }
            }
        }
    }

    private void DrawCanvasPanel(
        MapTemplateDatabase database,
        RoomTypeDatabase roomTypeDatabase,
        RoomEnemyPresetDatabase encounterDatabase)
    {
        using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
        {
            EditorGUILayout.LabelField("地图画布", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("左键选中节点。拖动节点可微调位置。点“开始连线”后，再点目标节点就会建立连接。", MessageType.None);

            MapTemplateDatabase.MapTemplateEntry template = GetSelectedTemplate(database);
            if (template == null)
            {
                EditorGUILayout.HelpBox("先新增一个地图模板。", MessageType.Info);
                return;
            }

            canvasScroll = EditorGUILayout.BeginScrollView(canvasScroll, "box", GUILayout.ExpandHeight(true));
            Rect canvasRect = GUILayoutUtility.GetRect(CanvasWidth, CanvasHeight);
            EditorGUI.DrawRect(canvasRect, new Color(0.13f, 0.13f, 0.13f));
            DrawCanvasGrid(canvasRect);

            DrawConnections(template, canvasRect);
            DrawNodes(template, canvasRect, roomTypeDatabase, encounterDatabase);
            HandleCanvasMouse(template, canvasRect);

            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawInspectorPanel(
        MapTemplateDatabase database,
        RoomTypeDatabase roomTypeDatabase,
        RoomEnemyPresetDatabase encounterDatabase)
    {
        using (new EditorGUILayout.VerticalScope("box", GUILayout.Width(330f), GUILayout.ExpandHeight(true)))
        {
            EditorGUILayout.LabelField("详情", EditorStyles.boldLabel);

            MapTemplateDatabase.MapTemplateEntry template = GetSelectedTemplate(database);
            if (template == null)
            {
                EditorGUILayout.HelpBox("没有选中地图模板。", MessageType.Info);
                return;
            }

            inspectorScroll = EditorGUILayout.BeginScrollView(inspectorScroll);

            DrawTemplateInspector(database, template);

            EditorGUILayout.Space(8f);

            MapTemplateDatabase.MapNodeEntry node = GetSelectedNode(template);
            if (node == null)
            {
                EditorGUILayout.HelpBox("在中间画布点一个节点，右侧会显示节点详情。", MessageType.Info);
                EditorGUILayout.EndScrollView();
                return;
            }

            DrawNodeInspector(database, template, node, roomTypeDatabase, encounterDatabase);
            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawTemplateInspector(MapTemplateDatabase database, MapTemplateDatabase.MapTemplateEntry template)
    {
        EditorGUILayout.LabelField("模板信息", EditorStyles.boldLabel);
        string oldTemplateId = template.templateId;
        template.templateId = EditorGUILayout.TextField("模板ID", template.templateId);
        template.displayName = EditorGUILayout.TextField("模板名字", template.displayName);
        EditorGUILayout.LabelField("节点数量", template.nodes.Count.ToString());

        if (!string.Equals(oldTemplateId, template.templateId, StringComparison.Ordinal))
        {
            selectedTemplateId = template.templateId;
            MarkDirty(database);
        }

        if (string.IsNullOrWhiteSpace(template.displayName))
        {
            template.displayName = template.templateId;
        }
    }

    private void DrawNodeInspector(
        MapTemplateDatabase database,
        MapTemplateDatabase.MapTemplateEntry template,
        MapTemplateDatabase.MapNodeEntry node,
        RoomTypeDatabase roomTypeDatabase,
        RoomEnemyPresetDatabase encounterDatabase)
    {
        EditorGUILayout.LabelField("节点信息", EditorStyles.boldLabel);

        string oldNodeId = node.nodeId;
        node.nodeId = EditorGUILayout.TextField("节点ID", node.nodeId);
        node.displayName = EditorGUILayout.TextField("显示名字", node.displayName);
        node.layerIndex = EditorGUILayout.IntField("层级", node.layerIndex);

        if (string.IsNullOrWhiteSpace(node.displayName))
        {
            node.displayName = node.nodeId;
        }

        string[] roomTypeNames;
        string[] roomTypeIds;
        BuildRoomTypeOptions(roomTypeDatabase, out roomTypeNames, out roomTypeIds);
        int roomTypeIndex = FindOptionIndex(roomTypeIds, node.roomTypeId);
        int nextRoomTypeIndex = EditorGUILayout.Popup("房间类型", roomTypeIndex, roomTypeNames);
        if (nextRoomTypeIndex >= 0 && nextRoomTypeIndex < roomTypeIds.Length)
        {
            node.roomTypeId = roomTypeIds[nextRoomTypeIndex];
        }

        string[] encounterNames;
        string[] encounterIds;
        BuildEncounterOptions(encounterDatabase, out encounterNames, out encounterIds);
        int encounterIndex = FindOptionIndex(encounterIds, node.encounterPresetId);
        int nextEncounterIndex = EditorGUILayout.Popup("遭遇战预设", encounterIndex, encounterNames);
        if (nextEncounterIndex >= 0 && nextEncounterIndex < encounterIds.Length)
        {
            node.encounterPresetId = encounterIds[nextEncounterIndex];
        }

        EditorGUILayout.Vector2Field("画布位置", node.position);

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("连出到", EditorStyles.boldLabel);
        if (node.nextNodeIds.Count == 0)
        {
            EditorGUILayout.HelpBox("当前没有连出线。", MessageType.None);
        }

        for (int i = node.nextNodeIds.Count - 1; i >= 0; i--)
        {
            string targetNodeId = node.nextNodeIds[i];
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(targetNodeId);
                if (GUILayout.Button("移除", GUILayout.Width(72f)))
                {
                    node.nextNodeIds.RemoveAt(i);
                    MarkDirty(database);
                }
            }
        }

        EditorGUILayout.Space(6f);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("开始连线"))
            {
                connectSourceNodeId = node.nodeId;
            }

            if (GUILayout.Button("删除节点"))
            {
                RemoveNode(template, node.nodeId);
                selectedNodeId = string.Empty;
                connectSourceNodeId = string.Empty;
                MarkDirty(database);
                GUIUtility.ExitGUI();
            }
        }

        if (!string.Equals(oldNodeId, node.nodeId, StringComparison.Ordinal))
        {
            RenameNodeReferences(template, oldNodeId, node.nodeId);
            selectedNodeId = node.nodeId;
            if (string.Equals(connectSourceNodeId, oldNodeId, StringComparison.Ordinal))
            {
                connectSourceNodeId = node.nodeId;
            }
            MarkDirty(database);
        }
    }

    private void DrawCanvasGrid(Rect canvasRect)
    {
        Handles.BeginGUI();
        Color oldColor = Handles.color;
        Handles.color = new Color(1f, 1f, 1f, 0.05f);

        for (float x = 0f; x <= CanvasWidth; x += 80f)
        {
            Handles.DrawLine(
                new Vector3(canvasRect.x + x, canvasRect.y),
                new Vector3(canvasRect.x + x, canvasRect.y + CanvasHeight));
        }

        for (float y = 0f; y <= CanvasHeight; y += 80f)
        {
            Handles.DrawLine(
                new Vector3(canvasRect.x, canvasRect.y + y),
                new Vector3(canvasRect.x + CanvasWidth, canvasRect.y + y));
        }

        Handles.color = oldColor;
        Handles.EndGUI();
    }

    private void DrawConnections(MapTemplateDatabase.MapTemplateEntry template, Rect canvasRect)
    {
        Handles.BeginGUI();
        Color oldColor = Handles.color;

        for (int i = 0; i < template.nodes.Count; i++)
        {
            MapTemplateDatabase.MapNodeEntry source = template.nodes[i];
            if (source == null)
            {
                continue;
            }

            MapTemplateDatabase.EnsureValidNode(source);
            Rect sourceRect = ResolveNodeRect(canvasRect, source.position);
            for (int j = 0; j < source.nextNodeIds.Count; j++)
            {
                MapTemplateDatabase.MapNodeEntry target = FindNode(template, source.nextNodeIds[j]);
                if (target == null)
                {
                    continue;
                }

                Rect targetRect = ResolveNodeRect(canvasRect, target.position);
                Handles.color = string.Equals(connectSourceNodeId, source.nodeId, StringComparison.Ordinal)
                    ? new Color(1f, 0.82f, 0.25f, 1f)
                    : new Color(0.9f, 0.9f, 0.9f, 0.9f);
                Handles.DrawAAPolyLine(3f, sourceRect.center, targetRect.center);
            }
        }

        Handles.color = oldColor;
        Handles.EndGUI();
    }

    private void DrawNodes(
        MapTemplateDatabase.MapTemplateEntry template,
        Rect canvasRect,
        RoomTypeDatabase roomTypeDatabase,
        RoomEnemyPresetDatabase encounterDatabase)
    {
        Event evt = Event.current;
        for (int i = 0; i < template.nodes.Count; i++)
        {
            MapTemplateDatabase.MapNodeEntry node = template.nodes[i];
            if (node == null)
            {
                continue;
            }

            MapTemplateDatabase.EnsureValidNode(node);
            Rect drawRect = ResolveNodeRect(canvasRect, node.position);
            bool isSelected = string.Equals(selectedNodeId, node.nodeId, StringComparison.Ordinal);

            Color fillColor = ResolveNodeColor(node);
            EditorGUI.DrawRect(drawRect, fillColor);
            DrawNodeBorder(drawRect, isSelected, string.Equals(connectSourceNodeId, node.nodeId, StringComparison.Ordinal));

            string roomTypeName = ResolveRoomTypeName(roomTypeDatabase, node.roomTypeId);
            string encounterName = ResolveEncounterName(encounterDatabase, node.encounterPresetId);
            GUI.Label(
                new Rect(drawRect.x + 8f, drawRect.y + 6f, drawRect.width - 16f, 18f),
                string.IsNullOrWhiteSpace(node.displayName) ? node.nodeId : node.displayName,
                EditorStyles.boldLabel);
            GUI.Label(
                new Rect(drawRect.x + 8f, drawRect.y + 26f, drawRect.width - 16f, 16f),
                $"层 {node.layerIndex} | {roomTypeName}",
                EditorStyles.miniLabel);
            GUI.Label(
                new Rect(drawRect.x + 8f, drawRect.y + 42f, drawRect.width - 16f, 16f),
                string.IsNullOrWhiteSpace(encounterName) ? "未绑定遭遇战" : encounterName,
                EditorStyles.miniLabel);

            if (evt.type == EventType.MouseDown && evt.button == 0 && drawRect.Contains(evt.mousePosition))
            {
                selectedNodeId = node.nodeId;

                if (!string.IsNullOrWhiteSpace(connectSourceNodeId) && !string.Equals(connectSourceNodeId, node.nodeId, StringComparison.Ordinal))
                {
                    CreateConnection(template, connectSourceNodeId, node.nodeId);
                    MarkDirty(EnsureDatabase());
                    connectSourceNodeId = string.Empty;
                }
                else
                {
                    draggingNodeId = node.nodeId;
                    isDraggingNode = true;
                    dragOffset = evt.mousePosition - drawRect.position;
                }

                evt.Use();
            }
        }
    }

    private void HandleCanvasMouse(MapTemplateDatabase.MapTemplateEntry template, Rect canvasRect)
    {
        Event evt = Event.current;
        if (evt.type == EventType.MouseDrag && isDraggingNode && !string.IsNullOrWhiteSpace(draggingNodeId))
        {
            MapTemplateDatabase.MapNodeEntry node = FindNode(template, draggingNodeId);
            if (node != null)
            {
                node.position = evt.mousePosition - new Vector2(canvasRect.x, canvasRect.y) - dragOffset;
                node.position.x = Mathf.Clamp(node.position.x, 0f, CanvasWidth - NodeWidth);
                node.position.y = Mathf.Clamp(node.position.y, 0f, CanvasHeight - NodeHeight);
                MarkDirty(EnsureDatabase());
                Repaint();
                evt.Use();
            }
        }

        if (evt.type == EventType.MouseUp && isDraggingNode)
        {
            isDraggingNode = false;
            draggingNodeId = string.Empty;
            evt.Use();
        }

        if (evt.type == EventType.MouseDown && evt.button == 0 && canvasRect.Contains(evt.mousePosition))
        {
            bool clickedNode = false;
            for (int i = 0; i < template.nodes.Count; i++)
            {
                if (ResolveNodeRect(canvasRect, template.nodes[i].position).Contains(evt.mousePosition))
                {
                    clickedNode = true;
                    break;
                }
            }

            if (!clickedNode)
            {
                selectedNodeId = string.Empty;
            }
        }
    }

    private void CreateTemplate(MapTemplateDatabase database)
    {
        string templateId = string.IsNullOrWhiteSpace(newTemplateId)
            ? BuildNextTemplateId(database)
            : newTemplateId.Trim();
        MapTemplateDatabase.MapTemplateEntry entry = database.GetOrCreateEntry(templateId);
        if (entry == null)
        {
            return;
        }

        MapTemplateDatabase.EnsureValidTemplate(entry);
        if (string.IsNullOrWhiteSpace(entry.displayName))
        {
            entry.displayName = templateId;
        }

        selectedTemplateId = entry.templateId;
        selectedNodeId = string.Empty;
        connectSourceNodeId = string.Empty;
        newTemplateId = string.Empty;
        MarkDirty(database);
    }

    private void RemoveSelectedTemplate(MapTemplateDatabase database)
    {
        MapTemplateDatabase.MapTemplateEntry template = GetSelectedTemplate(database);
        if (template == null)
        {
            return;
        }

        database.RemoveEntry(template.templateId);
        selectedTemplateId = string.Empty;
        selectedNodeId = string.Empty;
        connectSourceNodeId = string.Empty;
        MarkDirty(database);
    }

    private void AddNodeToSelectedTemplate(MapTemplateDatabase database)
    {
        MapTemplateDatabase.MapTemplateEntry template = GetSelectedTemplate(database);
        if (template == null)
        {
            return;
        }

        MapTemplateDatabase.EnsureValidTemplate(template);
        string nodeId = BuildNextNodeId(template);
        int layerIndex = template.nodes.Count == 0 ? 0 : template.nodes[template.nodes.Count - 1].layerIndex + 1;
        MapTemplateDatabase.MapNodeEntry node = new MapTemplateDatabase.MapNodeEntry
        {
            nodeId = nodeId,
            displayName = nodeId,
            layerIndex = layerIndex,
            roomTypeId = RoomTypeDatabase.EncounterBattleTypeId,
            position = new Vector2(120f + template.nodes.Count * 160f, 120f + layerIndex * 96f)
        };
        template.nodes.Add(node);
        selectedNodeId = node.nodeId;
        MarkDirty(database);
    }

    private void AutoLayoutSelectedTemplate(MapTemplateDatabase database)
    {
        MapTemplateDatabase.MapTemplateEntry template = GetSelectedTemplate(database);
        if (template == null)
        {
            return;
        }

        Dictionary<int, List<MapTemplateDatabase.MapNodeEntry>> grouped = new Dictionary<int, List<MapTemplateDatabase.MapNodeEntry>>();
        for (int i = 0; i < template.nodes.Count; i++)
        {
            MapTemplateDatabase.MapNodeEntry node = template.nodes[i];
            if (node == null)
            {
                continue;
            }

            MapTemplateDatabase.EnsureValidNode(node);
            if (!grouped.TryGetValue(node.layerIndex, out List<MapTemplateDatabase.MapNodeEntry> list))
            {
                list = new List<MapTemplateDatabase.MapNodeEntry>();
                grouped.Add(node.layerIndex, list);
            }

            list.Add(node);
        }

        List<int> layers = new List<int>(grouped.Keys);
        layers.Sort();

        const float startX = 320f;
        const float startY = 120f;
        const float layerStepY = 180f;
        const float siblingStepX = 220f;
        const float diagonalOffsetX = 90f;

        for (int i = 0; i < layers.Count; i++)
        {
            List<MapTemplateDatabase.MapNodeEntry> layerNodes = grouped[layers[i]];
            layerNodes.Sort((a, b) => string.CompareOrdinal(a.nodeId, b.nodeId));
            float width = (layerNodes.Count - 1) * siblingStepX;
            float firstX = startX - width * 0.5f + i * diagonalOffsetX;
            float y = startY + i * layerStepY;

            for (int j = 0; j < layerNodes.Count; j++)
            {
                layerNodes[j].position = new Vector2(firstX + j * siblingStepX, y);
            }
        }

        MarkDirty(database);
    }

    private static void CreateConnection(MapTemplateDatabase.MapTemplateEntry template, string sourceNodeId, string targetNodeId)
    {
        MapTemplateDatabase.MapNodeEntry source = FindNode(template, sourceNodeId);
        MapTemplateDatabase.MapNodeEntry target = FindNode(template, targetNodeId);
        if (source == null || target == null)
        {
            return;
        }

        MapTemplateDatabase.EnsureValidNode(source);
        if (!source.nextNodeIds.Contains(target.nodeId))
        {
            source.nextNodeIds.Add(target.nodeId);
        }
    }

    private static void RemoveNode(MapTemplateDatabase.MapTemplateEntry template, string nodeId)
    {
        if (template == null || string.IsNullOrWhiteSpace(nodeId))
        {
            return;
        }

        for (int i = template.nodes.Count - 1; i >= 0; i--)
        {
            MapTemplateDatabase.MapNodeEntry node = template.nodes[i];
            if (node == null)
            {
                continue;
            }

            if (string.Equals(node.nodeId, nodeId, StringComparison.Ordinal))
            {
                template.nodes.RemoveAt(i);
            }
            else
            {
                node.nextNodeIds.RemoveAll(id => string.Equals(id, nodeId, StringComparison.Ordinal));
            }
        }
    }

    private static void RenameNodeReferences(MapTemplateDatabase.MapTemplateEntry template, string oldNodeId, string newNodeId)
    {
        if (template == null || string.IsNullOrWhiteSpace(oldNodeId) || string.IsNullOrWhiteSpace(newNodeId))
        {
            return;
        }

        for (int i = 0; i < template.nodes.Count; i++)
        {
            MapTemplateDatabase.MapNodeEntry node = template.nodes[i];
            if (node == null)
            {
                continue;
            }

            for (int j = 0; j < node.nextNodeIds.Count; j++)
            {
                if (string.Equals(node.nextNodeIds[j], oldNodeId, StringComparison.Ordinal))
                {
                    node.nextNodeIds[j] = newNodeId;
                }
            }
        }
    }

    private static MapTemplateDatabase.MapNodeEntry FindNode(MapTemplateDatabase.MapTemplateEntry template, string nodeId)
    {
        if (template == null || string.IsNullOrWhiteSpace(nodeId) || template.nodes == null)
        {
            return null;
        }

        for (int i = 0; i < template.nodes.Count; i++)
        {
            MapTemplateDatabase.MapNodeEntry node = template.nodes[i];
            if (node == null)
            {
                continue;
            }

            if (string.Equals(node.nodeId, nodeId, StringComparison.Ordinal))
            {
                return node;
            }
        }

        return null;
    }

    private MapTemplateDatabase.MapTemplateEntry GetSelectedTemplate(MapTemplateDatabase database)
    {
        return database != null ? database.FindEntry(selectedTemplateId) : null;
    }

    private MapTemplateDatabase.MapNodeEntry GetSelectedNode(MapTemplateDatabase.MapTemplateEntry template)
    {
        return FindNode(template, selectedNodeId);
    }

    private void EnsureValidSelection(MapTemplateDatabase database)
    {
        if (database == null)
        {
            selectedTemplateId = string.Empty;
            selectedNodeId = string.Empty;
            connectSourceNodeId = string.Empty;
            return;
        }

        if (GetSelectedTemplate(database) == null && database.Entries.Count > 0)
        {
            selectedTemplateId = database.Entries[0].templateId;
        }

        MapTemplateDatabase.MapTemplateEntry template = GetSelectedTemplate(database);
        if (template == null)
        {
            selectedNodeId = string.Empty;
            connectSourceNodeId = string.Empty;
            return;
        }

        if (FindNode(template, selectedNodeId) == null)
        {
            selectedNodeId = string.Empty;
        }

        if (FindNode(template, connectSourceNodeId) == null)
        {
            connectSourceNodeId = string.Empty;
        }
    }

    private static Rect ResolveNodeRect(Rect canvasRect, Vector2 position)
    {
        return new Rect(canvasRect.x + position.x, canvasRect.y + position.y, NodeWidth, NodeHeight);
    }

    private static void DrawNodeBorder(Rect rect, bool isSelected, bool isConnectSource)
    {
        Color borderColor = isConnectSource
            ? new Color(1f, 0.82f, 0.25f, 1f)
            : isSelected
                ? new Color(0.35f, 0.78f, 1f, 1f)
                : new Color(0f, 0f, 0f, 0.45f);

        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 2f), borderColor);
        EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 2f, rect.width, 2f), borderColor);
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, 2f, rect.height), borderColor);
        EditorGUI.DrawRect(new Rect(rect.xMax - 2f, rect.y, 2f, rect.height), borderColor);
    }

    private static Color ResolveNodeColor(MapTemplateDatabase.MapNodeEntry node)
    {
        string roomTypeId = node != null ? node.roomTypeId : string.Empty;
        if (string.Equals(roomTypeId, RoomTypeDatabase.EncounterBattleTypeId, StringComparison.Ordinal))
        {
            return new Color(0.42f, 0.23f, 0.23f, 1f);
        }

        int hash = string.IsNullOrWhiteSpace(roomTypeId) ? 0 : roomTypeId.GetHashCode();
        float hue = Mathf.Abs(hash % 1000) / 1000f;
        Color color = Color.HSVToRGB(hue, 0.35f, 0.42f);
        color.a = 1f;
        return color;
    }

    private static void BuildRoomTypeOptions(RoomTypeDatabase database, out string[] names, out string[] ids)
    {
        List<string> resolvedNames = new List<string>();
        List<string> resolvedIds = new List<string>();

        if (database != null && database.Entries != null)
        {
            for (int i = 0; i < database.Entries.Count; i++)
            {
                RoomTypeDatabase.RoomTypeEntry entry = database.Entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.roomTypeId))
                {
                    continue;
                }

                resolvedIds.Add(entry.roomTypeId);
                resolvedNames.Add(string.IsNullOrWhiteSpace(entry.displayName) ? entry.roomTypeId : entry.displayName);
            }
        }

        if (resolvedIds.Count == 0)
        {
            resolvedIds.Add(RoomTypeDatabase.EncounterBattleTypeId);
            resolvedNames.Add(RoomTypeDatabase.EncounterBattleTypeName);
        }

        ids = resolvedIds.ToArray();
        names = resolvedNames.ToArray();
    }

    private static void BuildEncounterOptions(RoomEnemyPresetDatabase database, out string[] names, out string[] ids)
    {
        List<string> resolvedNames = new List<string> { "未绑定" };
        List<string> resolvedIds = new List<string> { string.Empty };

        if (database != null && database.Entries != null)
        {
            for (int i = 0; i < database.Entries.Count; i++)
            {
                RoomEnemyPresetDatabase.RoomEnemyPresetEntry entry = database.Entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.presetId))
                {
                    continue;
                }

                if (!string.Equals(entry.roomTypeId, RoomTypeDatabase.EncounterBattleTypeId, StringComparison.Ordinal))
                {
                    continue;
                }

                resolvedIds.Add(entry.presetId);
                resolvedNames.Add(entry.presetId);
            }
        }

        ids = resolvedIds.ToArray();
        names = resolvedNames.ToArray();
    }

    private static int FindOptionIndex(string[] ids, string currentId)
    {
        if (ids == null || ids.Length == 0)
        {
            return 0;
        }

        for (int i = 0; i < ids.Length; i++)
        {
            if (string.Equals(ids[i], currentId, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return 0;
    }

    private static string ResolveRoomTypeName(RoomTypeDatabase database, string roomTypeId)
    {
        RoomTypeDatabase.RoomTypeEntry entry = database != null ? database.FindEntry(roomTypeId) : null;
        return entry != null && !string.IsNullOrWhiteSpace(entry.displayName) ? entry.displayName : roomTypeId;
    }

    private static string ResolveEncounterName(RoomEnemyPresetDatabase database, string encounterPresetId)
    {
        RoomEnemyPresetDatabase.RoomEnemyPresetEntry entry = database != null ? database.FindEntry(encounterPresetId) : null;
        return entry != null ? entry.presetId : string.Empty;
    }

    private static string BuildNextTemplateId(MapTemplateDatabase database)
    {
        int index = 1;
        while (database.FindEntry("map_template_" + index) != null)
        {
            index++;
        }

        return "map_template_" + index;
    }

    private static string BuildNextNodeId(MapTemplateDatabase.MapTemplateEntry template)
    {
        int index = 1;
        while (FindNode(template, "node_" + index) != null)
        {
            index++;
        }

        return "node_" + index;
    }

    private static void MarkDirty(ScriptableObject asset)
    {
        if (asset == null)
        {
            return;
        }

        EditorUtility.SetDirty(asset);
    }

    private static void SaveAsset(ScriptableObject asset)
    {
        if (asset == null)
        {
            return;
        }

        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
    }

    private static MapTemplateDatabase EnsureDatabase()
    {
        MapTemplateDatabase database = AssetDatabase.LoadAssetAtPath<MapTemplateDatabase>(MapTemplateAssetPath);
        if (database != null)
        {
            return database;
        }

        EnsureResourceFolder();
        database = CreateInstance<MapTemplateDatabase>();
        AssetDatabase.CreateAsset(database, MapTemplateAssetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return database;
    }

    private static RoomTypeDatabase EnsureRoomTypeDatabase()
    {
        RoomTypeDatabase database = AssetDatabase.LoadAssetAtPath<RoomTypeDatabase>(RoomTypeAssetPath);
        if (database != null)
        {
            RoomTypeDatabase.RoomTypeEntry encounterBattle = database.GetOrCreateEntry(RoomTypeDatabase.EncounterBattleTypeId);
            if (encounterBattle != null && !string.Equals(encounterBattle.displayName, RoomTypeDatabase.EncounterBattleTypeName, StringComparison.Ordinal))
            {
                encounterBattle.displayName = RoomTypeDatabase.EncounterBattleTypeName;
                SaveAsset(database);
            }

            return database;
        }

        EnsureResourceFolder();
        database = CreateInstance<RoomTypeDatabase>();
        RoomTypeDatabase.RoomTypeEntry created = database.GetOrCreateEntry(RoomTypeDatabase.EncounterBattleTypeId);
        if (created != null)
        {
            created.displayName = RoomTypeDatabase.EncounterBattleTypeName;
        }

        AssetDatabase.CreateAsset(database, RoomTypeAssetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return database;
    }

    private static RoomEnemyPresetDatabase EnsureEncounterDatabase()
    {
        RoomEnemyPresetDatabase database = AssetDatabase.LoadAssetAtPath<RoomEnemyPresetDatabase>(EncounterPresetAssetPath);
        if (database != null)
        {
            return database;
        }

        EnsureResourceFolder();
        database = CreateInstance<RoomEnemyPresetDatabase>();
        AssetDatabase.CreateAsset(database, EncounterPresetAssetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return database;
    }

    private static void EnsureResourceFolder()
    {
        if (!AssetDatabase.IsValidFolder(ResourceFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }
    }
}

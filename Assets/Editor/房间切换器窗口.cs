using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class 房间切换器窗口 : EditorWindow
{
    private const string StartNodeId = "入口";

    private Vector2 scrollPosition;
    private bool preserveCurrentRoomSnapshot = true;

    [MenuItem("Tools/战斗/房间切换器")]
    private static void Open()
    {
        房间切换器窗口 window = GetWindow<房间切换器窗口>("房间切换器");
        window.minSize = new Vector2(520f, 360f);
        window.Show();
        window.Focus();
    }

    private void OnGUI()
    {
        MapTemplateDatabase mapDatabase = MapTemplateDatabase.LoadDefault();
        if (mapDatabase == null || mapDatabase.Entries == null)
        {
            EditorGUILayout.HelpBox("缺少 MapTemplateDatabase。", MessageType.Error);
            return;
        }

        MapTemplateDatabase.MapTemplateEntry template = FindTemplate(mapDatabase, BattleBootstrap.CurrentDungeonTemplateId);
        if (template == null)
        {
            EditorGUILayout.HelpBox($"当前模板不存在：{BattleBootstrap.CurrentDungeonTemplateId}", MessageType.Error);
            return;
        }

        MapTemplateDatabase.MapNodeEntry currentNode = FindNode(template, BattleBootstrap.CurrentDungeonNodeId);
        HashSet<string> openedNodeIds = CollectOpenedNodeIds(template);
        格子模板数据库 gridDatabase = 格子模板数据库.LoadDefault();
        RoomTypeDatabase roomTypeDatabase = RoomTypeDatabase.LoadDefault();
        RoomEnemyPresetDatabase presetDatabase = RoomEnemyPresetDatabase.LoadDefault();

        EditorGUILayout.LabelField("房间切换器", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("当前场景", SceneManager.GetActiveScene().name);
        EditorGUILayout.LabelField("当前模板", BattleBootstrap.CurrentDungeonTemplateId);
        EditorGUILayout.LabelField("当前房间", BattleBootstrap.CurrentDungeonNodeId);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("非 Play 模式只按方向设置 BattleBootstrap 当前房间，不会载入战斗副本场景。", MessageType.Info);
        }

        preserveCurrentRoomSnapshot = EditorGUILayout.Toggle("切换前保存当前房间快照", preserveCurrentRoomSnapshot);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("刷新"))
            {
                Repaint();
            }
        }

        EditorGUILayout.Space(8f);
        DrawCurrentRoom(currentNode, openedNodeIds, gridDatabase, roomTypeDatabase, presetDatabase);

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("方向切换", EditorStyles.boldLabel);
        if (currentNode == null)
        {
            EditorGUILayout.HelpBox("当前房间节点不存在，不能按方向切换。", MessageType.Error);
            return;
        }

        DrawDirectionButtons(template, currentNode, openedNodeIds, gridDatabase, roomTypeDatabase, presetDatabase);

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("已开通房间", EditorStyles.boldLabel);
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        DrawOpenedRooms(template, openedNodeIds);
        EditorGUILayout.EndScrollView();
    }

    private void DrawDirectionButtons(
        MapTemplateDatabase.MapTemplateEntry template,
        MapTemplateDatabase.MapNodeEntry currentNode,
        HashSet<string> openedNodeIds,
        格子模板数据库 gridDatabase,
        RoomTypeDatabase roomTypeDatabase,
        RoomEnemyPresetDatabase presetDatabase)
    {
        DrawDirectionButton(template, currentNode, MapTemplateDatabase.ConnectionDirection.North, "北", openedNodeIds, gridDatabase, roomTypeDatabase, presetDatabase);

        using (new EditorGUILayout.HorizontalScope())
        {
            DrawDirectionButton(template, currentNode, MapTemplateDatabase.ConnectionDirection.West, "西", openedNodeIds, gridDatabase, roomTypeDatabase, presetDatabase);
            DrawDirectionButton(template, currentNode, MapTemplateDatabase.ConnectionDirection.East, "东", openedNodeIds, gridDatabase, roomTypeDatabase, presetDatabase);
        }

        DrawDirectionButton(template, currentNode, MapTemplateDatabase.ConnectionDirection.South, "南", openedNodeIds, gridDatabase, roomTypeDatabase, presetDatabase);
    }

    private void DrawDirectionButton(
        MapTemplateDatabase.MapTemplateEntry template,
        MapTemplateDatabase.MapNodeEntry currentNode,
        MapTemplateDatabase.ConnectionDirection direction,
        string directionLabel,
        HashSet<string> openedNodeIds,
        格子模板数据库 gridDatabase,
        RoomTypeDatabase roomTypeDatabase,
        RoomEnemyPresetDatabase presetDatabase)
    {
        string targetNodeId = FindConnectionTargetInDirection(currentNode, direction);
        MapTemplateDatabase.MapNodeEntry targetNode = FindNode(template, targetNodeId);
        string invalidReason = ResolveDirectionInvalidReason(
            targetNodeId,
            targetNode,
            openedNodeIds,
            gridDatabase,
            roomTypeDatabase,
            presetDatabase);
        bool canSwitch = string.IsNullOrEmpty(invalidReason);

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            string targetTitle = targetNode != null ? BuildRoomTitle(targetNode) : "无连接";
            EditorGUILayout.LabelField($"{directionLabel}：{targetTitle}", EditorStyles.boldLabel);

            if (!string.IsNullOrEmpty(invalidReason))
            {
                EditorGUILayout.HelpBox(invalidReason, MessageType.Warning);
            }

            using (new EditorGUI.DisabledScope(!canSwitch))
            {
                if (GUILayout.Button($"向{directionLabel}切换"))
                {
                    BattleBootstrap.DebugNavigateToDirection(direction, preserveCurrentRoomSnapshot);
                }
            }
        }
    }

    private static void DrawCurrentRoom(
        MapTemplateDatabase.MapNodeEntry node,
        HashSet<string> openedNodeIds,
        格子模板数据库 gridDatabase,
        RoomTypeDatabase roomTypeDatabase,
        RoomEnemyPresetDatabase presetDatabase)
    {
        if (node == null)
        {
            return;
        }

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField(BuildRoomTitle(node), EditorStyles.boldLabel);
            EditorGUILayout.LabelField("节点ID", NormalizeId(node.nodeId));
            EditorGUILayout.LabelField("房间类型", string.IsNullOrWhiteSpace(node.roomTypeId) ? "（空）" : node.roomTypeId.Trim());
            EditorGUILayout.LabelField("格子模板", string.IsNullOrWhiteSpace(node.battleGridTemplateId) ? "（空）" : node.battleGridTemplateId.Trim());
            EditorGUILayout.LabelField("遭遇预设", string.IsNullOrWhiteSpace(node.encounterPresetId) ? "（空）" : node.encounterPresetId.Trim());

            string invalidReason = ResolveNodeInvalidReason(node, openedNodeIds, gridDatabase, roomTypeDatabase, presetDatabase);
            if (!string.IsNullOrEmpty(invalidReason))
            {
                EditorGUILayout.HelpBox(invalidReason, MessageType.Warning);
            }
        }
    }

    private static void DrawOpenedRooms(MapTemplateDatabase.MapTemplateEntry template, HashSet<string> openedNodeIds)
    {
        if (template.nodes == null)
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

            string nodeId = NormalizeId(node.nodeId);
            if (!openedNodeIds.Contains(nodeId))
            {
                continue;
            }

            EditorGUILayout.LabelField(BuildRoomTitle(node));
        }
    }

    private static string ResolveDirectionInvalidReason(
        string targetNodeId,
        MapTemplateDatabase.MapNodeEntry targetNode,
        HashSet<string> openedNodeIds,
        格子模板数据库 gridDatabase,
        RoomTypeDatabase roomTypeDatabase,
        RoomEnemyPresetDatabase presetDatabase)
    {
        if (string.IsNullOrWhiteSpace(targetNodeId))
        {
            return "当前方向没有连接。";
        }

        if (targetNode == null)
        {
            return $"连接目标不存在：{targetNodeId}";
        }

        return ResolveNodeInvalidReason(targetNode, openedNodeIds, gridDatabase, roomTypeDatabase, presetDatabase);
    }

    private static string ResolveNodeInvalidReason(
        MapTemplateDatabase.MapNodeEntry node,
        HashSet<string> openedNodeIds,
        格子模板数据库 gridDatabase,
        RoomTypeDatabase roomTypeDatabase,
        RoomEnemyPresetDatabase presetDatabase)
    {
        if (node == null)
        {
            return "节点为空。";
        }

        string nodeId = NormalizeId(node.nodeId);
        if (string.IsNullOrEmpty(nodeId))
        {
            return "节点ID为空。";
        }

        if (openedNodeIds == null || !openedNodeIds.Contains(nodeId))
        {
            return $"节点未从 '{StartNodeId}' 连通。";
        }

        string roomTypeId = NormalizeId(node.roomTypeId);
        if (string.IsNullOrEmpty(roomTypeId))
        {
            return "房间类型为空。";
        }

        if (roomTypeDatabase == null)
        {
            return "缺少 RoomTypeDatabase。";
        }

        if (!HasRoomType(roomTypeDatabase, roomTypeId))
        {
            return $"房间类型不存在：{roomTypeId}";
        }

        string gridTemplateId = NormalizeId(node.battleGridTemplateId);
        if (string.IsNullOrEmpty(gridTemplateId))
        {
            return "缺少格子模板。";
        }

        if (gridDatabase == null)
        {
            return "缺少格子模板数据库。";
        }

        if (!HasGridTemplate(gridDatabase, gridTemplateId))
        {
            return $"格子模板不存在：{gridTemplateId}";
        }

        if (!string.Equals(roomTypeId, RoomTypeDatabase.EncounterBattleTypeId, StringComparison.Ordinal))
        {
            return string.Empty;
        }

        string presetId = NormalizeId(node.encounterPresetId);
        if (string.IsNullOrEmpty(presetId))
        {
            return "遭遇战房间缺少遭遇预设。";
        }

        if (presetDatabase == null)
        {
            return "缺少 RoomEnemyPresetDatabase。";
        }

        if (!HasEnemyPreset(presetDatabase, presetId))
        {
            return $"遭遇预设不存在：{presetId}";
        }

        return string.Empty;
    }

    private static string FindConnectionTargetInDirection(
        MapTemplateDatabase.MapNodeEntry node,
        MapTemplateDatabase.ConnectionDirection direction)
    {
        if (node == null || node.connections == null)
        {
            return string.Empty;
        }

        for (int i = 0; i < node.connections.Count; i++)
        {
            MapTemplateDatabase.MapConnectionEntry connection = node.connections[i];
            if (connection == null || connection.direction != direction || string.IsNullOrWhiteSpace(connection.targetNodeId))
            {
                continue;
            }

            return connection.targetNodeId.Trim();
        }

        return string.Empty;
    }

    private static bool HasRoomType(RoomTypeDatabase database, string roomTypeId)
    {
        if (database == null || database.Entries == null || string.IsNullOrWhiteSpace(roomTypeId))
        {
            return false;
        }

        string id = roomTypeId.Trim();
        for (int i = 0; i < database.Entries.Count; i++)
        {
            RoomTypeDatabase.RoomTypeEntry entry = database.Entries[i];
            if (entry != null && string.Equals(entry.roomTypeId, id, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasGridTemplate(格子模板数据库 database, string templateId)
    {
        if (database == null || database.Entries == null || string.IsNullOrWhiteSpace(templateId))
        {
            return false;
        }

        string id = templateId.Trim();
        for (int i = 0; i < database.Entries.Count; i++)
        {
            格子模板数据库.格子模板条目 entry = database.Entries[i];
            if (entry != null && string.Equals(entry.templateId, id, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasEnemyPreset(RoomEnemyPresetDatabase database, string presetId)
    {
        if (database == null || database.Entries == null || string.IsNullOrWhiteSpace(presetId))
        {
            return false;
        }

        string id = presetId.Trim();
        for (int i = 0; i < database.Entries.Count; i++)
        {
            RoomEnemyPresetDatabase.RoomEnemyPresetEntry entry = database.Entries[i];
            if (entry != null && string.Equals(entry.presetId, id, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static HashSet<string> CollectOpenedNodeIds(MapTemplateDatabase.MapTemplateEntry template)
    {
        HashSet<string> result = new HashSet<string>(StringComparer.Ordinal);
        if (template == null || template.nodes == null)
        {
            return result;
        }

        MapTemplateDatabase.MapNodeEntry startNode = FindNode(template, StartNodeId);
        if (startNode == null)
        {
            return result;
        }

        Queue<MapTemplateDatabase.MapNodeEntry> queue = new Queue<MapTemplateDatabase.MapNodeEntry>();
        result.Add(StartNodeId);
        queue.Enqueue(startNode);

        while (queue.Count > 0)
        {
            MapTemplateDatabase.MapNodeEntry current = queue.Dequeue();
            if (current == null || current.connections == null)
            {
                continue;
            }

            for (int i = 0; i < current.connections.Count; i++)
            {
                MapTemplateDatabase.MapConnectionEntry connection = current.connections[i];
                if (connection == null || string.IsNullOrWhiteSpace(connection.targetNodeId))
                {
                    continue;
                }

                string targetNodeId = connection.targetNodeId.Trim();
                if (!result.Add(targetNodeId))
                {
                    continue;
                }

                MapTemplateDatabase.MapNodeEntry targetNode = FindNode(template, targetNodeId);
                if (targetNode != null)
                {
                    queue.Enqueue(targetNode);
                }
            }
        }

        return result;
    }

    private static MapTemplateDatabase.MapTemplateEntry FindTemplate(MapTemplateDatabase database, string templateId)
    {
        if (database == null || database.Entries == null || string.IsNullOrWhiteSpace(templateId))
        {
            return null;
        }

        string resolvedTemplateId = templateId.Trim();
        for (int i = 0; i < database.Entries.Count; i++)
        {
            MapTemplateDatabase.MapTemplateEntry entry = database.Entries[i];
            if (entry != null && string.Equals(entry.templateId, resolvedTemplateId, StringComparison.Ordinal))
            {
                return entry;
            }
        }

        return null;
    }

    private static MapTemplateDatabase.MapNodeEntry FindNode(MapTemplateDatabase.MapTemplateEntry template, string nodeId)
    {
        if (template == null || template.nodes == null || string.IsNullOrWhiteSpace(nodeId))
        {
            return null;
        }

        string resolvedNodeId = nodeId.Trim();
        for (int i = 0; i < template.nodes.Count; i++)
        {
            MapTemplateDatabase.MapNodeEntry node = template.nodes[i];
            if (node != null && string.Equals(node.nodeId, resolvedNodeId, StringComparison.Ordinal))
            {
                return node;
            }
        }

        return null;
    }

    private static string BuildRoomTitle(MapTemplateDatabase.MapNodeEntry node)
    {
        if (node == null)
        {
            return "空节点";
        }

        string nodeId = NormalizeId(node.nodeId);
        string displayName = NormalizeId(node.displayName);
        if (string.IsNullOrEmpty(displayName) || string.Equals(displayName, nodeId, StringComparison.Ordinal))
        {
            return string.IsNullOrEmpty(nodeId) ? "未命名房间" : nodeId;
        }

        return $"{displayName} ({nodeId})";
    }

    private static string NormalizeId(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}

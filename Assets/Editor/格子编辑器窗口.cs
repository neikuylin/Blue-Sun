using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class 格子编辑器窗口 : EditorWindow
{
    private const string ResourceFolder = "Assets/Resources";
    private const string AssetPath = ResourceFolder + "/BattleGridTemplateDatabase.asset";
    private const float LeftPanelWidth = 240f;
    private const float RightPanelWidth = 320f;
    private const float ToolbarHeight = 36f;
    private const float CellSize = 28f;
    private const float CellGap = 2f;
    private const float HeaderSize = 28f;

    private enum 绘制工具
    {
        可用格 = 0,
        敌人出生位 = 1,
        玩家默认出生点 = 2,
        玩家东门出生点 = 3,
        玩家南门出生点 = 4,
        玩家西门出生点 = 5,
        玩家北门出生点 = 6,
        东门口 = 7,
        南门口 = 8,
        西门口 = 9,
        北门口 = 10
    }

    private enum 拖拽模式
    {
        无 = 0,
        涂格 = 1,
        擦除 = 2
    }

    private Vector2 templateListScroll;
    private Vector2 canvasScroll;
    private Vector2 detailScroll;
    private string selectedTemplateId = string.Empty;
    private string selectedEncounterPresetId = string.Empty;
    private string newTemplateId = string.Empty;
    private string newTemplateName = string.Empty;
    private 绘制工具 currentTool = 绘制工具.可用格;
    private int selectedPropVisualIndex = -1;
    private int selectedPetalExposureAreaIndex = -1;
    private readonly HashSet<string> expandedPropVisualKeys = new HashSet<string>();
    private readonly HashSet<string> expandedPetalExposureAreaKeys = new HashSet<string>();
    private readonly HashSet<string> expandedWallVisualKeys = new HashSet<string>();
    private 拖拽模式 currentDragMode = 拖拽模式.无;
    private Vector2Int lastPaintedCell = new Vector2Int(int.MinValue, int.MinValue);
    private Vector2Int exposureDragStartCell = new Vector2Int(int.MinValue, int.MinValue);

    [MenuItem("Tools/地图/格子编辑器")]
    private static void Open()
    {
        格子编辑器窗口 window = GetWindow<格子编辑器窗口>("格子编辑器");
        window.minSize = new Vector2(1260f, 720f);
        window.Show();
        window.Focus();
    }

    private void OnGUI()
    {
        格子模板数据库 database = EnsureDatabase();
        RoomEnemyPresetDatabase encounterDatabase = EnsureEncounterDatabase();
        if (database == null)
        {
            EditorGUILayout.HelpBox("格子模板库创建失败。", MessageType.Error);
            return;
        }

        EnsureSelection(database);
        EnsureEncounterSelection(encounterDatabase);
        DrawToolbar(database);
        EditorGUILayout.Space(6f);

        using (new EditorGUILayout.HorizontalScope())
        {
            DrawTemplateList(database);
            DrawCanvasPanel(database);
            DrawDetailPanel(database, encounterDatabase);
        }

        if (Event.current.type == EventType.MouseUp)
        {
            currentDragMode = 拖拽模式.无;
            lastPaintedCell = new Vector2Int(int.MinValue, int.MinValue);
            exposureDragStartCell = new Vector2Int(int.MinValue, int.MinValue);
        }
    }

    private void DrawToolbar(格子模板数据库 database)
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar, GUILayout.Height(ToolbarHeight)))
        {
            if (GUILayout.Button("保存", EditorStyles.toolbarButton, GUILayout.Width(72f)))
            {
                SaveAsset(database);
            }

            GUILayout.Space(8f);
            GUILayout.Label("新模板ID", GUILayout.Width(64f));
            newTemplateId = GUILayout.TextField(newTemplateId, EditorStyles.toolbarTextField, GUILayout.Width(180f));
            GUILayout.Label("新模板名称", GUILayout.Width(76f));
            newTemplateName = GUILayout.TextField(newTemplateName, EditorStyles.toolbarTextField, GUILayout.Width(180f));

            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(newTemplateId)))
            {
                if (GUILayout.Button("新建模板", EditorStyles.toolbarButton, GUILayout.Width(84f)))
                {
                    CreateTemplate(database);
                }
            }

            GUILayout.FlexibleSpace();
        }
    }

    private void DrawTemplateList(格子模板数据库 database)
    {
        using (new EditorGUILayout.VerticalScope("box", GUILayout.Width(LeftPanelWidth), GUILayout.ExpandHeight(true)))
        {
            EditorGUILayout.LabelField("模板列表", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("左侧切换模板，中间画格子，右侧改模板字段。", MessageType.Info);

            templateListScroll = EditorGUILayout.BeginScrollView(templateListScroll);
            List<格子模板数据库.格子模板条目> entries = database.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                格子模板数据库.格子模板条目 entry = entries[i];
                if (entry == null)
                {
                    continue;
                }

                格子模板数据库.EnsureValidEntry(entry);
                bool isSelected = string.Equals(selectedTemplateId, entry.templateId, StringComparison.Ordinal);
                string label = string.IsNullOrWhiteSpace(entry.displayName)
                    ? entry.templateId
                    : $"{entry.displayName} ({entry.templateId})";

                if (GUILayout.Button(label, isSelected ? EditorStyles.miniButtonMid : EditorStyles.miniButton, GUILayout.Height(34f)))
                {
                    selectedTemplateId = entry.templateId;
                    selectedPropVisualIndex = -1;
                    selectedPetalExposureAreaIndex = -1;
                    GUI.FocusControl(null);
                }
            }
            EditorGUILayout.EndScrollView();

            using (new EditorGUI.DisabledScope(GetSelectedTemplate(database) == null))
            {
                if (GUILayout.Button("删除当前模板"))
                {
                    DeleteSelectedTemplate(database);
                }
            }
        }
    }

    private void DrawCanvasPanel(格子模板数据库 database)
    {
        using (new EditorGUILayout.VerticalScope("box", GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
        {
            格子模板数据库.格子模板条目 entry = GetSelectedTemplate(database);
            EditorGUILayout.LabelField("格子画布", EditorStyles.boldLabel);

            if (entry == null)
            {
                EditorGUILayout.HelpBox("先创建一个格子模板。", MessageType.Info);
                return;
            }

            DrawToolButtons(entry);
            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox(
                "左键点格切换可用格，左键拖拽连续涂格，右键拖拽连续擦除。当前工具决定点击时设置哪种点位。",
                MessageType.None);

            float gridWidth = HeaderSize + entry.width * (CellSize + CellGap) + CellGap;
            float gridHeight = HeaderSize + entry.height * (CellSize + CellGap) + CellGap;

            canvasScroll = EditorGUILayout.BeginScrollView(canvasScroll, GUILayout.ExpandHeight(true));
            DrawDoorEntranceButtons();
            EditorGUILayout.Space(4f);
            Rect canvasRect = GUILayoutUtility.GetRect(gridWidth, gridHeight, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));
            DrawGridCanvas(entry, canvasRect);
            HandleCanvasInput(entry, canvasRect);
            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawToolButtons(格子模板数据库.格子模板条目 entry)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            DrawToolButton(绘制工具.可用格, "可用格");
            DrawToolButton(绘制工具.敌人出生位, "敌人出生位");
            DrawToolButton(绘制工具.玩家默认出生点, "玩家默认出生点");
            DrawToolButton(绘制工具.玩家东门出生点, "玩家东门出生点");
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            DrawToolButton(绘制工具.玩家南门出生点, "玩家南门出生点");
            DrawToolButton(绘制工具.玩家西门出生点, "玩家西门出生点");
            DrawToolButton(绘制工具.玩家北门出生点, "玩家北门出生点");
        }

        DrawPropPlacementButtons(entry);
        DrawPetalExposureAreaButtons(entry);
    }

    private void DrawDoorEntranceButtons()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            DrawDoorEntranceButton(绘制工具.东门口, "东门口");
            DrawDoorEntranceButton(绘制工具.南门口, "南门口");
            DrawDoorEntranceButton(绘制工具.西门口, "西门口");
            DrawDoorEntranceButton(绘制工具.北门口, "北门口");
        }
    }

    private void DrawDoorEntranceButton(绘制工具 tool, string label)
    {
        bool selected = selectedPropVisualIndex < 0 && selectedPetalExposureAreaIndex < 0 && currentTool == tool;
        Color previousColor = GUI.backgroundColor;
        if (selected)
        {
            GUI.backgroundColor = new Color(0.28f, 0.78f, 0.62f, 1f);
        }

        if (GUILayout.Button(label, GUILayout.Width(72f), GUILayout.Height(26f)))
        {
            currentTool = tool;
            selectedPropVisualIndex = -1;
            selectedPetalExposureAreaIndex = -1;
            currentDragMode = 拖拽模式.无;
        }

        GUI.backgroundColor = previousColor;
    }

    private void DrawToolButton(绘制工具 tool, string label)
    {
        bool selected = selectedPropVisualIndex < 0 && selectedPetalExposureAreaIndex < 0 && currentTool == tool;
        Color previousColor = GUI.backgroundColor;
        if (selected)
        {
            GUI.backgroundColor = new Color(0.35f, 0.72f, 0.95f, 1f);
        }

        if (GUILayout.Button(label, GUILayout.Height(28f)))
        {
            currentTool = tool;
            selectedPropVisualIndex = -1;
            selectedPetalExposureAreaIndex = -1;
        }

        GUI.backgroundColor = previousColor;
    }

    private void DrawPropPlacementButtons(格子模板数据库.格子模板条目 entry)
    {
        if (entry == null || entry.propVisuals == null || entry.propVisuals.Count == 0)
        {
            return;
        }

        EditorGUILayout.Space(2f);
        EditorGUILayout.LabelField("物件画笔", EditorStyles.miniBoldLabel);
        int columns = 3;
        for (int i = 0; i < entry.propVisuals.Count; i += columns)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                for (int j = 0; j < columns && i + j < entry.propVisuals.Count; j++)
                {
                    int index = i + j;
                    格子模板数据库.PropVisualEntry prop = entry.propVisuals[index];
                    string label = prop != null && !string.IsNullOrWhiteSpace(prop.propName)
                        ? prop.propName.Trim()
                        : $"物件{index + 1}";

                    Color previousColor = GUI.backgroundColor;
                    if (selectedPropVisualIndex == index)
                    {
                        GUI.backgroundColor = new Color(0.85f, 0.48f, 0.18f, 1f);
                    }

                    if (GUILayout.Button(label, GUILayout.Height(26f)))
                    {
                        selectedPropVisualIndex = index;
                        selectedPetalExposureAreaIndex = -1;
                    }

                    GUI.backgroundColor = previousColor;
                }
            }
        }
    }

    private void DrawPetalExposureAreaButtons(格子模板数据库.格子模板条目 entry)
    {
        if (entry == null || entry.花瓣曝光区域列表 == null || entry.花瓣曝光区域列表.Count == 0)
        {
            return;
        }

        EditorGUILayout.Space(2f);
        EditorGUILayout.LabelField("花瓣曝光画笔", EditorStyles.miniBoldLabel);
        int columns = 3;
        for (int i = 0; i < entry.花瓣曝光区域列表.Count; i += columns)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                for (int j = 0; j < columns && i + j < entry.花瓣曝光区域列表.Count; j++)
                {
                    int index = i + j;
                    格子模板数据库.花瓣曝光区域Entry area = entry.花瓣曝光区域列表[index];
                    string label = ResolvePetalExposureAreaDisplayName(area, index);

                    Color previousColor = GUI.backgroundColor;
                    if (selectedPetalExposureAreaIndex == index)
                    {
                        GUI.backgroundColor = new Color(0.95f, 0.86f, 0.22f, 1f);
                    }

                    if (GUILayout.Button(label, GUILayout.Height(26f)))
                    {
                        selectedPetalExposureAreaIndex = index;
                        selectedPropVisualIndex = -1;
                    }

                    GUI.backgroundColor = previousColor;
                }
            }
        }
    }

    private void DrawGridCanvas(格子模板数据库.格子模板条目 entry, Rect canvasRect)
    {
        EditorGUI.DrawRect(canvasRect, new Color(0.12f, 0.12f, 0.12f));

        HashSet<Vector2Int> walkableCells = BuildCellSet(entry.walkableCells);

        for (int y = 0; y < entry.height; y++)
        {
            for (int x = 0; x < entry.width; x++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                Rect cellRect = ResolveCellRect(canvasRect, entry.height, cell);
                bool isWalkable = walkableCells.Contains(cell);

                EditorGUI.DrawRect(cellRect, isWalkable ? new Color(0.22f, 0.46f, 0.28f) : new Color(0.21f, 0.21f, 0.21f));
                Handles.color = new Color(0f, 0f, 0f, 0.35f);
                Handles.DrawSolidRectangleWithOutline(cellRect, Color.clear, new Color(0f, 0f, 0f, 0.25f));

                格子模板数据库.EnemySpawnSlot enemySlot = FindEnemySpawnSlot(entry, cell);
                DrawCellMarker(cellRect, enemySlot != null ? ResolveEnemySlotMarker(entry, enemySlot) : string.Empty, enemySlot != null, new Color(0.92f, 0.25f, 0.22f));
                DrawCellMarker(cellRect, "默", entry.hasDefaultPlayerSpawn && entry.defaultPlayerSpawnCell.ToVector2Int() == cell, new Color(0.22f, 0.58f, 0.95f));
                DrawCellMarker(cellRect, "东", entry.hasEastDoorPlayerSpawn && entry.eastDoorPlayerSpawnCell.ToVector2Int() == cell, new Color(0.95f, 0.68f, 0.18f));
                DrawCellMarker(cellRect, "南", entry.hasSouthDoorPlayerSpawn && entry.southDoorPlayerSpawnCell.ToVector2Int() == cell, new Color(0.90f, 0.45f, 0.16f));
                DrawCellMarker(cellRect, "西", entry.hasWestDoorPlayerSpawn && entry.westDoorPlayerSpawnCell.ToVector2Int() == cell, new Color(0.70f, 0.34f, 0.92f));
                DrawCellMarker(cellRect, "北", entry.hasNorthDoorPlayerSpawn && entry.northDoorPlayerSpawnCell.ToVector2Int() == cell, new Color(0.18f, 0.78f, 0.78f));
                DrawCellMarker(cellRect, "口", entry.hasEastDoorEntrance && entry.eastDoorEntranceCell.ToVector2Int() == cell, new Color(0.86f, 0.72f, 0.22f), 1);
                DrawCellMarker(cellRect, "口", entry.hasSouthDoorEntrance && entry.southDoorEntranceCell.ToVector2Int() == cell, new Color(0.86f, 0.45f, 0.22f), 1);
                DrawCellMarker(cellRect, "口", entry.hasWestDoorEntrance && entry.westDoorEntranceCell.ToVector2Int() == cell, new Color(0.58f, 0.36f, 0.86f), 1);
                DrawCellMarker(cellRect, "口", entry.hasNorthDoorEntrance && entry.northDoorEntranceCell.ToVector2Int() == cell, new Color(0.22f, 0.72f, 0.72f), 1);
                DrawCellMarker(cellRect, "锚", HasPropAnchorAtCell(entry, cell), new Color(0.85f, 0.48f, 0.18f), 1);
                DrawCellMarker(cellRect, "物", HasPropOccupiedCell(entry, cell), new Color(0.72f, 0.39f, 0.14f), 2);
                DrawCellMarker(cellRect, "墙", HasWallVisualAtCell(entry, cell), new Color(0.55f, 0.55f, 0.62f), 3);
            }
        }

        DrawPetalExposureAreas(entry, canvasRect);
        DrawHeaders(entry, canvasRect);
    }

    private void DrawPetalExposureAreas(格子模板数据库.格子模板条目 entry, Rect canvasRect)
    {
        if (entry == null || entry.花瓣曝光区域列表 == null)
        {
            return;
        }

        for (int i = 0; i < entry.花瓣曝光区域列表.Count; i++)
        {
            格子模板数据库.花瓣曝光区域Entry area = entry.花瓣曝光区域列表[i];
            if (area == null)
            {
                continue;
            }

            Rect areaRect = ResolvePetalExposureAreaRect(entry, canvasRect, area);
            Color fillColor = i == selectedPetalExposureAreaIndex
                ? new Color(1f, 0.92f, 0.18f, 0.34f)
                : new Color(1f, 0.92f, 0.18f, 0.18f);
            Color outlineColor = i == selectedPetalExposureAreaIndex
                ? new Color(1f, 0.96f, 0.42f, 1f)
                : new Color(1f, 0.86f, 0.22f, 0.75f);

            EditorGUI.DrawRect(areaRect, fillColor);
            Handles.DrawSolidRectangleWithOutline(areaRect, Color.clear, outlineColor);

            GUIStyle style = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            GUI.Label(areaRect, ResolvePetalExposureAreaDisplayName(area, i), style);
        }
    }

    private void DrawHeaders(格子模板数据库.格子模板条目 entry, Rect canvasRect)
    {
        GUIStyle headerStyle = new GUIStyle(EditorStyles.miniBoldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };

        for (int x = 0; x < entry.width; x++)
        {
            Rect headerRect = new Rect(
                canvasRect.x + HeaderSize + CellGap + x * (CellSize + CellGap),
                canvasRect.y,
                CellSize,
                HeaderSize - CellGap);
            GUI.Label(headerRect, x.ToString(), headerStyle);
        }

        for (int y = 0; y < entry.height; y++)
        {
            Rect headerRect = new Rect(
                canvasRect.x,
                canvasRect.y + HeaderSize + CellGap + y * (CellSize + CellGap),
                HeaderSize - CellGap,
                CellSize);
            GUI.Label(headerRect, (entry.height - 1 - y).ToString(), headerStyle);
        }
    }

    private void DrawCellMarker(Rect cellRect, string label, bool active, Color color, int markerSlot = 0)
    {
        if (!active)
        {
            return;
        }

        float badgeWidth = markerSlot == 0 ? 14f : 12f;
        float badgeHeight = 14f;
        float badgeX = cellRect.x + 3f;
        float badgeY = cellRect.y + 3f;
        if (markerSlot == 1)
        {
            badgeX = cellRect.xMax - badgeWidth - 3f;
        }
        else if (markerSlot == 2)
        {
            badgeY = cellRect.yMax - badgeHeight - 3f;
        }
        else if (markerSlot == 3)
        {
            badgeX = cellRect.xMax - badgeWidth - 3f;
            badgeY = cellRect.yMax - badgeHeight - 3f;
        }

        Rect badgeRect = new Rect(badgeX, badgeY, badgeWidth, badgeHeight);
        EditorGUI.DrawRect(badgeRect, color);
        GUIStyle style = new GUIStyle(EditorStyles.miniBoldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };
        GUI.Label(badgeRect, label, style);
    }

    private void HandleCanvasInput(格子模板数据库.格子模板条目 entry, Rect canvasRect)
    {
        Event currentEvent = Event.current;
        if (entry == null)
        {
            return;
        }

        bool isMouseEvent =
            currentEvent.type == EventType.MouseDown ||
            currentEvent.type == EventType.MouseDrag ||
            currentEvent.type == EventType.MouseUp;

        if (!isMouseEvent || !canvasRect.Contains(currentEvent.mousePosition))
        {
            return;
        }

        Vector2Int cell;
        if (!TryGetCellAtPosition(entry, canvasRect, currentEvent.mousePosition, out cell))
        {
            return;
        }

        if (IsPetalExposureAreaTool(entry))
        {
            if (currentEvent.button == 0)
            {
                if (currentEvent.type == EventType.MouseDown)
                {
                    Undo.RecordObject(EnsureDatabase(), "拉花瓣曝光范围");
                    currentDragMode = 拖拽模式.涂格;
                    exposureDragStartCell = cell;
                    ApplyPetalExposureDrag(entry, cell);
                    currentEvent.Use();
                }
                else if (currentEvent.type == EventType.MouseDrag && currentDragMode == 拖拽模式.涂格)
                {
                    ApplyPetalExposureDrag(entry, cell);
                    currentEvent.Use();
                }
            }

            return;
        }

        if (IsPropPlacementTool(entry))
        {
            if (currentEvent.button == 0)
            {
                if (currentEvent.type == EventType.MouseDown)
                {
                    Undo.RecordObject(EnsureDatabase(), "摆放格子物件");
                    currentDragMode = 拖拽模式.涂格;
                    lastPaintedCell = new Vector2Int(int.MinValue, int.MinValue);
                    BeginPropPlacementTool(entry, cell);
                    currentEvent.Use();
                }
                else if (currentEvent.type == EventType.MouseDrag && currentDragMode == 拖拽模式.涂格)
                {
                    AddPropOccupiedCell(entry, cell);
                    currentEvent.Use();
                }
            }
            else if (currentEvent.button == 1)
            {
                if (currentEvent.type == EventType.MouseDown)
                {
                    Undo.RecordObject(EnsureDatabase(), "擦除物件占格");
                    currentDragMode = 拖拽模式.擦除;
                    lastPaintedCell = new Vector2Int(int.MinValue, int.MinValue);
                    RemovePropOccupiedCell(entry, cell);
                    currentEvent.Use();
                }
                else if (currentEvent.type == EventType.MouseDrag && currentDragMode == 拖拽模式.擦除)
                {
                    RemovePropOccupiedCell(entry, cell);
                    currentEvent.Use();
                }
            }

            return;
        }

        if (currentEvent.button == 1)
        {
            if (currentEvent.type == EventType.MouseDown)
            {
                Undo.RecordObject(EnsureDatabase(), "擦除可用格");
                currentDragMode = 拖拽模式.擦除;
                ApplyWalkablePaint(entry, cell, false);
                currentEvent.Use();
            }
            else if (currentEvent.type == EventType.MouseDrag && currentDragMode == 拖拽模式.擦除)
            {
                ApplyWalkablePaint(entry, cell, false);
                currentEvent.Use();
            }

            return;
        }

        if (currentEvent.button != 0)
        {
            return;
        }

        if (currentTool == 绘制工具.可用格)
        {
            if (currentEvent.type == EventType.MouseDown)
            {
                bool shouldEnable = !ContainsCell(entry.walkableCells, cell);
                Undo.RecordObject(EnsureDatabase(), shouldEnable ? "绘制可用格" : "擦除可用格");
                currentDragMode = shouldEnable ? 拖拽模式.涂格 : 拖拽模式.擦除;
                ApplyWalkablePaint(entry, cell, shouldEnable);
                currentEvent.Use();
            }
            else if (currentEvent.type == EventType.MouseDrag)
            {
                if (currentDragMode == 拖拽模式.涂格)
                {
                    ApplyWalkablePaint(entry, cell, true);
                    currentEvent.Use();
                }
                else if (currentDragMode == 拖拽模式.擦除)
                {
                    ApplyWalkablePaint(entry, cell, false);
                    currentEvent.Use();
                }
            }

            return;
        }

        if (currentEvent.type == EventType.MouseDown)
        {
            Undo.RecordObject(EnsureDatabase(), "设置格子点位");
            ApplyPlacementTool(entry, cell);
            currentEvent.Use();
        }
    }

    private void ApplyWalkablePaint(格子模板数据库.格子模板条目 entry, Vector2Int cell, bool enabled)
    {
        if (lastPaintedCell == cell)
        {
            return;
        }

        lastPaintedCell = cell;
        if (enabled)
        {
            AddCell(entry.walkableCells, cell);
        }
        else
        {
            RemoveCell(entry.walkableCells, cell);
            RemoveEnemySpawnSlot(entry, cell);
            ClearSpawnIfMatches(ref entry.hasDefaultPlayerSpawn, ref entry.defaultPlayerSpawnCell, cell);
            ClearSpawnIfMatches(ref entry.hasEastDoorPlayerSpawn, ref entry.eastDoorPlayerSpawnCell, cell);
            ClearSpawnIfMatches(ref entry.hasSouthDoorPlayerSpawn, ref entry.southDoorPlayerSpawnCell, cell);
            ClearSpawnIfMatches(ref entry.hasWestDoorPlayerSpawn, ref entry.westDoorPlayerSpawnCell, cell);
            ClearSpawnIfMatches(ref entry.hasNorthDoorPlayerSpawn, ref entry.northDoorPlayerSpawnCell, cell);
            ClearSpawnIfMatches(ref entry.hasEastDoorEntrance, ref entry.eastDoorEntranceCell, cell);
            ClearSpawnIfMatches(ref entry.hasSouthDoorEntrance, ref entry.southDoorEntranceCell, cell);
            ClearSpawnIfMatches(ref entry.hasWestDoorEntrance, ref entry.westDoorEntranceCell, cell);
            ClearSpawnIfMatches(ref entry.hasNorthDoorEntrance, ref entry.northDoorEntranceCell, cell);
        }

        MarkDirtyAndRepaint();
    }

    private void ApplyPlacementTool(格子模板数据库.格子模板条目 entry, Vector2Int cell)
    {
        if (!IsDoorEntranceTool(currentTool))
        {
            AddCell(entry.walkableCells, cell);
        }

        switch (currentTool)
        {
            case 绘制工具.敌人出生位:
                ToggleEnemySpawnSlot(entry, cell);
                break;
            case 绘制工具.玩家默认出生点:
                SetSpawn(ref entry.hasDefaultPlayerSpawn, ref entry.defaultPlayerSpawnCell, cell);
                break;
            case 绘制工具.玩家东门出生点:
                SetSpawn(ref entry.hasEastDoorPlayerSpawn, ref entry.eastDoorPlayerSpawnCell, cell);
                break;
            case 绘制工具.玩家南门出生点:
                SetSpawn(ref entry.hasSouthDoorPlayerSpawn, ref entry.southDoorPlayerSpawnCell, cell);
                break;
            case 绘制工具.玩家西门出生点:
                SetSpawn(ref entry.hasWestDoorPlayerSpawn, ref entry.westDoorPlayerSpawnCell, cell);
                break;
            case 绘制工具.玩家北门出生点:
                SetSpawn(ref entry.hasNorthDoorPlayerSpawn, ref entry.northDoorPlayerSpawnCell, cell);
                break;
            case 绘制工具.东门口:
                SetSpawn(ref entry.hasEastDoorEntrance, ref entry.eastDoorEntranceCell, cell);
                break;
            case 绘制工具.南门口:
                SetSpawn(ref entry.hasSouthDoorEntrance, ref entry.southDoorEntranceCell, cell);
                break;
            case 绘制工具.西门口:
                SetSpawn(ref entry.hasWestDoorEntrance, ref entry.westDoorEntranceCell, cell);
                break;
            case 绘制工具.北门口:
                SetSpawn(ref entry.hasNorthDoorEntrance, ref entry.northDoorEntranceCell, cell);
                break;
        }

        MarkDirtyAndRepaint();
    }

    private static bool IsDoorEntranceTool(绘制工具 tool)
    {
        return tool == 绘制工具.东门口 ||
            tool == 绘制工具.南门口 ||
            tool == 绘制工具.西门口 ||
            tool == 绘制工具.北门口;
    }

    private bool IsPropPlacementTool(格子模板数据库.格子模板条目 entry)
    {
        return entry != null &&
            entry.propVisuals != null &&
            selectedPropVisualIndex >= 0 &&
            selectedPropVisualIndex < entry.propVisuals.Count &&
            entry.propVisuals[selectedPropVisualIndex] != null;
    }

    private bool IsPetalExposureAreaTool(格子模板数据库.格子模板条目 entry)
    {
        return entry != null &&
            entry.花瓣曝光区域列表 != null &&
            selectedPetalExposureAreaIndex >= 0 &&
            selectedPetalExposureAreaIndex < entry.花瓣曝光区域列表.Count &&
            entry.花瓣曝光区域列表[selectedPetalExposureAreaIndex] != null;
    }

    private void ApplyPetalExposureDrag(格子模板数据库.格子模板条目 entry, Vector2Int currentCell)
    {
        if (exposureDragStartCell.x == int.MinValue || !IsPetalExposureAreaTool(entry))
        {
            return;
        }

        格子模板数据库.花瓣曝光区域Entry area = entry.花瓣曝光区域列表[selectedPetalExposureAreaIndex];
        int minX = Mathf.Min(exposureDragStartCell.x, currentCell.x);
        int maxX = Mathf.Max(exposureDragStartCell.x, currentCell.x);
        int minY = Mathf.Min(exposureDragStartCell.y, currentCell.y);
        int maxY = Mathf.Max(exposureDragStartCell.y, currentCell.y);
        area.startCell = new 格子模板数据库.CellPosition(minX, minY);
        area.size = new Vector2Int(maxX - minX + 1, maxY - minY + 1);
        MarkDirtyAndRepaint();
    }

    private void BeginPropPlacementTool(格子模板数据库.格子模板条目 entry, Vector2Int cell)
    {
        格子模板数据库.PropVisualEntry prop = GetSelectedPropVisual(entry);
        if (prop == null)
        {
            return;
        }

        if (prop.blockedCells == null)
        {
            prop.blockedCells = new List<格子模板数据库.CellPosition>();
        }

        if (prop.anchorCell.ToVector2Int() != cell)
        {
            prop.anchorCell = 格子模板数据库.CellPosition.FromVector2Int(cell);
            prop.blockedCells.Clear();
        }

        AddPropOccupiedCell(entry, cell);
    }

    private void AddPropOccupiedCell(格子模板数据库.格子模板条目 entry, Vector2Int cell)
    {
        if (lastPaintedCell == cell)
        {
            return;
        }

        lastPaintedCell = cell;
        格子模板数据库.PropVisualEntry prop = GetSelectedPropVisual(entry);
        if (prop == null)
        {
            return;
        }

        if (prop.blockedCells == null)
        {
            prop.blockedCells = new List<格子模板数据库.CellPosition>();
        }

        AddCell(prop.blockedCells, cell);

        AddCell(entry.walkableCells, cell);
        MarkDirtyAndRepaint();
    }

    private void RemovePropOccupiedCell(格子模板数据库.格子模板条目 entry, Vector2Int cell)
    {
        if (lastPaintedCell == cell)
        {
            return;
        }

        lastPaintedCell = cell;
        格子模板数据库.PropVisualEntry prop = GetSelectedPropVisual(entry);
        if (prop == null || prop.anchorCell.ToVector2Int() == cell)
        {
            return;
        }

        if (prop.blockedCells == null)
        {
            prop.blockedCells = new List<格子模板数据库.CellPosition>();
        }

        RemoveCell(prop.blockedCells, cell);
        if (!ContainsCell(prop.blockedCells, prop.anchorCell.ToVector2Int()))
        {
            AddCell(prop.blockedCells, prop.anchorCell.ToVector2Int());
        }

        MarkDirtyAndRepaint();
    }

    private 格子模板数据库.PropVisualEntry GetSelectedPropVisual(格子模板数据库.格子模板条目 entry)
    {
        return IsPropPlacementTool(entry) ? entry.propVisuals[selectedPropVisualIndex] : null;
    }

    private void DrawDetailPanel(格子模板数据库 database, RoomEnemyPresetDatabase encounterDatabase)
    {
        using (new EditorGUILayout.VerticalScope("box", GUILayout.Width(RightPanelWidth), GUILayout.ExpandHeight(true)))
        {
            EditorGUILayout.LabelField("模板详情", EditorStyles.boldLabel);

            格子模板数据库.格子模板条目 entry = GetSelectedTemplate(database);
            if (entry == null)
            {
                EditorGUILayout.HelpBox("没有选中的模板。", MessageType.Info);
                return;
            }

            detailScroll = EditorGUILayout.BeginScrollView(detailScroll);
            EditorGUI.BeginChangeCheck();

            string oldTemplateId = entry.templateId;
            entry.templateId = NormalizeIdentifier(EditorGUILayout.TextField("模板ID", entry.templateId), oldTemplateId);
            entry.displayName = EditorGUILayout.TextField("模板名称", entry.displayName);

            int previousWidth = entry.width;
            int previousHeight = entry.height;
            entry.width = Mathf.Max(1, EditorGUILayout.IntField("画布宽度", entry.width));
            entry.height = Mathf.Max(1, EditorGUILayout.IntField("画布高度", entry.height));

            EditorGUILayout.Space(6f);
            DrawRoomVisualSettings(entry);

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("工具说明", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox($"当前工具：{ResolveCurrentToolLabel(entry)}", MessageType.None);

            EditorGUILayout.Space(6f);
            DrawEncounterPresetSelector(encounterDatabase);

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("敌人出生位", EditorStyles.boldLabel);
            DrawEnemySpawnSlotList(entry, encounterDatabase);

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("玩家出生点", EditorStyles.boldLabel);
            DrawSingleSpawnReadonly("玩家默认出生点", entry.hasDefaultPlayerSpawn, entry.defaultPlayerSpawnCell);
            DrawSingleSpawnReadonly("玩家东门出生点", entry.hasEastDoorPlayerSpawn, entry.eastDoorPlayerSpawnCell);
            DrawSingleSpawnReadonly("玩家南门出生点", entry.hasSouthDoorPlayerSpawn, entry.southDoorPlayerSpawnCell);
            DrawSingleSpawnReadonly("玩家西门出生点", entry.hasWestDoorPlayerSpawn, entry.westDoorPlayerSpawnCell);
            DrawSingleSpawnReadonly("玩家北门出生点", entry.hasNorthDoorPlayerSpawn, entry.northDoorPlayerSpawnCell);

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("门口", EditorStyles.boldLabel);
            DrawSingleSpawnReadonly("东门口", entry.hasEastDoorEntrance, entry.eastDoorEntranceCell);
            DrawSingleSpawnReadonly("南门口", entry.hasSouthDoorEntrance, entry.southDoorEntranceCell);
            DrawSingleSpawnReadonly("西门口", entry.hasWestDoorEntrance, entry.westDoorEntranceCell);
            DrawSingleSpawnReadonly("北门口", entry.hasNorthDoorEntrance, entry.northDoorEntranceCell);

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("可用格统计", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("可用格数量", entry.walkableCells.Count.ToString());
            EditorGUILayout.LabelField("敌人出生位数量", entry.enemySpawnSlots.Count.ToString());

            if (entry.width != previousWidth || entry.height != previousHeight)
            {
                ClampTemplateToBounds(entry);
            }

            if (!string.Equals(oldTemplateId, entry.templateId, StringComparison.Ordinal))
            {
                selectedTemplateId = entry.templateId;
            }

            if (EditorGUI.EndChangeCheck())
            {
                MarkDirtyAndRepaint();
            }

            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawEncounterPresetSelector(RoomEnemyPresetDatabase encounterDatabase)
    {
        List<string> presetIds = BuildEncounterPresetIds(encounterDatabase);
        List<string> presetLabels = BuildEncounterPresetLabels(encounterDatabase, presetIds);
        int selectedIndex = GetSelectedEncounterPresetIndex(presetIds);
        int newIndex = EditorGUILayout.Popup("预览遭遇战预设", selectedIndex, presetLabels.ToArray());
        selectedEncounterPresetId = newIndex >= 0 && newIndex < presetIds.Count ? presetIds[newIndex] : string.Empty;
    }

    private void DrawRoomVisualSettings(格子模板数据库.格子模板条目 entry)
    {
        格子模板数据库.EnsureValidEntry(entry);

        EditorGUILayout.LabelField("房间美术", EditorStyles.boldLabel);
        entry.defaultFloorPrefab = (GameObject)EditorGUILayout.ObjectField("整张地板Prefab", entry.defaultFloorPrefab, typeof(GameObject), false);
        entry.alignFloorToBattleCamera = EditorGUILayout.Toggle("地板平行战斗相机", entry.alignFloorToBattleCamera);
        entry.花瓣粒子预制体 = (GameObject)EditorGUILayout.ObjectField("花瓣粒子Prefab", entry.花瓣粒子预制体, typeof(GameObject), false);

        EditorGUILayout.Space(4f);
        DrawPetalExposureAreaList(entry);

        EditorGUILayout.Space(4f);
        DrawPropVisualList(entry);

        EditorGUILayout.Space(4f);
        DrawWallVisualList(entry);
    }

    private void DrawPetalExposureAreaList(格子模板数据库.格子模板条目 entry)
    {
        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("花瓣曝光区域", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("新增区域后，点中间画布上方的同名画笔，左键拖拽矩形范围。运行时该范围会沿世界Y轴向上形成曝光空间。", MessageType.None);
            if (GUILayout.Button("新增曝光区域"))
            {
                Undo.RecordObject(EnsureDatabase(), "新增花瓣曝光区域");
                if (entry.花瓣曝光区域列表 == null)
                {
                    entry.花瓣曝光区域列表 = new List<格子模板数据库.花瓣曝光区域Entry>();
                }

                int newIndex = entry.花瓣曝光区域列表.Count;
                selectedPetalExposureAreaIndex = newIndex;
                selectedPropVisualIndex = -1;
                entry.花瓣曝光区域列表.Add(new 格子模板数据库.花瓣曝光区域Entry
                {
                    areaName = $"曝光区域{newIndex + 1}",
                    startCell = new 格子模板数据库.CellPosition(0, 0),
                    size = Vector2Int.one
                });
                expandedPetalExposureAreaKeys.Add(GetPetalExposureAreaFoldoutKey(entry, newIndex));
                MarkDirtyAndRepaint();
            }

            if (entry.花瓣曝光区域列表 == null)
            {
                return;
            }

            for (int i = 0; i < entry.花瓣曝光区域列表.Count; i++)
            {
                格子模板数据库.花瓣曝光区域Entry area = entry.花瓣曝光区域列表[i];
                if (area == null)
                {
                    continue;
                }

                using (new EditorGUILayout.VerticalScope("box"))
                {
                    string foldoutKey = GetPetalExposureAreaFoldoutKey(entry, i);
                    bool isExpanded = expandedPetalExposureAreaKeys.Contains(foldoutKey);
                    string displayName = ResolvePetalExposureAreaDisplayName(area, i);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        bool nextExpanded = EditorGUILayout.Foldout(isExpanded, displayName, true);
                        if (nextExpanded)
                        {
                            expandedPetalExposureAreaKeys.Add(foldoutKey);
                        }
                        else
                        {
                            expandedPetalExposureAreaKeys.Remove(foldoutKey);
                        }

                        Color previousColor = GUI.backgroundColor;
                        if (selectedPetalExposureAreaIndex == i)
                        {
                            GUI.backgroundColor = new Color(0.95f, 0.86f, 0.22f, 1f);
                        }

                        if (GUILayout.Button("画", GUILayout.Width(40f)))
                        {
                            selectedPetalExposureAreaIndex = i;
                            selectedPropVisualIndex = -1;
                        }

                        GUI.backgroundColor = previousColor;

                        if (GUILayout.Button("删除", GUILayout.Width(56f)))
                        {
                            Undo.RecordObject(EnsureDatabase(), "删除花瓣曝光区域");
                            entry.花瓣曝光区域列表.RemoveAt(i);
                            if (selectedPetalExposureAreaIndex == i)
                            {
                                selectedPetalExposureAreaIndex = -1;
                            }
                            else if (selectedPetalExposureAreaIndex > i)
                            {
                                selectedPetalExposureAreaIndex--;
                            }
                            MarkDirtyAndRepaint();
                            return;
                        }
                    }

                    if (!expandedPetalExposureAreaKeys.Contains(foldoutKey))
                    {
                        continue;
                    }

                    area.areaName = EditorGUILayout.TextField("名称", area.areaName);
                    area.startCell = DrawCellPositionField("起点格子", area.startCell);
                    area.size = EditorGUILayout.Vector2IntField("尺寸", area.size);
                    Vector2Int start = area.startCell.ToVector2Int();
                    start.x = Mathf.Clamp(start.x, 0, entry.width - 1);
                    start.y = Mathf.Clamp(start.y, 0, entry.height - 1);
                    area.startCell = 格子模板数据库.CellPosition.FromVector2Int(start);
                    area.size.x = Mathf.Clamp(area.size.x, 1, entry.width - start.x);
                    area.size.y = Mathf.Clamp(area.size.y, 1, entry.height - start.y);
                }
            }
        }
    }

    private void DrawPropVisualList(格子模板数据库.格子模板条目 entry)
    {
        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("物件", EditorStyles.boldLabel);
            if (GUILayout.Button("新增物件"))
            {
                Undo.RecordObject(EnsureDatabase(), "新增格子物件");
                selectedPropVisualIndex = entry.propVisuals.Count;
                selectedPetalExposureAreaIndex = -1;
                entry.propVisuals.Add(new 格子模板数据库.PropVisualEntry
                {
                    propName = $"物件{entry.propVisuals.Count + 1}",
                    anchorCell = new 格子模板数据库.CellPosition(0, 0)
                });
                expandedPropVisualKeys.Add(GetPropVisualFoldoutKey(entry, selectedPropVisualIndex));
                MarkDirtyAndRepaint();
            }

            for (int i = 0; i < entry.propVisuals.Count; i++)
            {
                格子模板数据库.PropVisualEntry prop = entry.propVisuals[i];
                if (prop == null)
                {
                    continue;
                }

                using (new EditorGUILayout.VerticalScope("box"))
                {
                    string foldoutKey = GetPropVisualFoldoutKey(entry, i);
                    bool isExpanded = expandedPropVisualKeys.Contains(foldoutKey);
                    string displayName = ResolvePropVisualDisplayName(prop, i);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        bool nextExpanded = EditorGUILayout.Foldout(isExpanded, displayName, true);
                        if (nextExpanded)
                        {
                            expandedPropVisualKeys.Add(foldoutKey);
                        }
                        else
                        {
                            expandedPropVisualKeys.Remove(foldoutKey);
                        }

                        if (GUILayout.Button("删除", GUILayout.Width(56f)))
                        {
                            Undo.RecordObject(EnsureDatabase(), "删除格子物件");
                            entry.propVisuals.RemoveAt(i);
                            if (selectedPropVisualIndex == i)
                            {
                                selectedPropVisualIndex = -1;
                            }
                            else if (selectedPropVisualIndex > i)
                            {
                                selectedPropVisualIndex--;
                            }
                            MarkDirtyAndRepaint();
                            return;
                        }
                    }

                    if (!expandedPropVisualKeys.Contains(foldoutKey))
                    {
                        continue;
                    }

                    prop.propName = EditorGUILayout.TextField("名称", prop.propName);
                    prop.prefab = (GameObject)EditorGUILayout.ObjectField("Prefab", prop.prefab, typeof(GameObject), false);
                    prop.anchorCell = DrawCellPositionField("锚点格", prop.anchorCell);
                    prop.localOffset = EditorGUILayout.Vector3Field("美术偏移", prop.localOffset);
                    prop.alignToBattleCamera = EditorGUILayout.Toggle("平行战斗相机", prop.alignToBattleCamera);
                    prop.blocksMovement = EditorGUILayout.Toggle("阻挡移动", prop.blocksMovement);
                    DrawBlockedCellList(prop);
                }
            }
        }
    }

    private static string GetPropVisualFoldoutKey(格子模板数据库.格子模板条目 entry, int index)
    {
        string templateId = entry != null && !string.IsNullOrWhiteSpace(entry.templateId)
            ? entry.templateId.Trim()
            : "未命名模板";
        return $"{templateId}:prop:{index}";
    }

    private static string GetPetalExposureAreaFoldoutKey(格子模板数据库.格子模板条目 entry, int index)
    {
        string templateId = entry != null && !string.IsNullOrWhiteSpace(entry.templateId)
            ? entry.templateId.Trim()
            : "未命名模板";
        return $"{templateId}:petal-exposure:{index}";
    }

    private static string ResolvePropVisualDisplayName(格子模板数据库.PropVisualEntry prop, int index)
    {
        if (prop != null && !string.IsNullOrWhiteSpace(prop.propName))
        {
            return prop.propName.Trim();
        }

        return $"物件{index + 1}";
    }

    private static string ResolvePetalExposureAreaDisplayName(格子模板数据库.花瓣曝光区域Entry area, int index)
    {
        if (area != null && !string.IsNullOrWhiteSpace(area.areaName))
        {
            return area.areaName.Trim();
        }

        return $"曝光区域{index + 1}";
    }

    private void DrawBlockedCellList(格子模板数据库.PropVisualEntry prop)
    {
        if (prop.blockedCells == null)
        {
            prop.blockedCells = new List<格子模板数据库.CellPosition>();
        }

        EditorGUILayout.LabelField("占格", EditorStyles.miniBoldLabel);
        EditorGUILayout.HelpBox("左侧选中物件按钮后，左键第一个格子是锚点，拖动经过的格子会加入占格。右键可擦除非锚点占格。", MessageType.None);

        if (GUILayout.Button("新增占格"))
        {
            prop.blockedCells.Add(prop.anchorCell);
        }

        for (int i = 0; i < prop.blockedCells.Count; i++)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                prop.blockedCells[i] = DrawCellPositionField($"占格 {i + 1}", prop.blockedCells[i]);
                if (GUILayout.Button("删除", GUILayout.Width(56f)))
                {
                    prop.blockedCells.RemoveAt(i);
                    return;
                }
            }
        }
    }

    private void DrawWallVisualList(格子模板数据库.格子模板条目 entry)
    {
        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("墙/门", EditorStyles.boldLabel);
            if (GUILayout.Button("新增墙/门"))
            {
                Undo.RecordObject(EnsureDatabase(), "新增格子墙");
                int newWallIndex = entry.wallVisuals.Count;
                entry.wallVisuals.Add(new 格子模板数据库.WallVisualEntry
                {
                    wallName = $"墙{newWallIndex + 1}",
                    cell = new 格子模板数据库.CellPosition(0, 0),
                    side = 格子模板数据库.WallSide.North
                });
                expandedWallVisualKeys.Add(GetWallVisualFoldoutKey(entry, newWallIndex));
                MarkDirtyAndRepaint();
            }

            for (int i = 0; i < entry.wallVisuals.Count; i++)
            {
                格子模板数据库.WallVisualEntry wall = entry.wallVisuals[i];
                if (wall == null)
                {
                    continue;
                }

                using (new EditorGUILayout.VerticalScope("box"))
                {
                    string foldoutKey = GetWallVisualFoldoutKey(entry, i);
                    bool isExpanded = expandedWallVisualKeys.Contains(foldoutKey);
                    string displayName = ResolveWallVisualDisplayName(wall, i);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        bool nextExpanded = EditorGUILayout.Foldout(isExpanded, displayName, true);
                        if (nextExpanded)
                        {
                            expandedWallVisualKeys.Add(foldoutKey);
                        }
                        else
                        {
                            expandedWallVisualKeys.Remove(foldoutKey);
                        }

                        if (GUILayout.Button("删除", GUILayout.Width(56f)))
                        {
                            Undo.RecordObject(EnsureDatabase(), "删除格子墙");
                            entry.wallVisuals.RemoveAt(i);
                            MarkDirtyAndRepaint();
                            return;
                        }
                    }

                    if (!expandedWallVisualKeys.Contains(foldoutKey))
                    {
                        continue;
                    }

                    wall.wallName = EditorGUILayout.TextField("名称", wall.wallName);
                    wall.prefab = (GameObject)EditorGUILayout.ObjectField("Prefab", wall.prefab, typeof(GameObject), false);
                    wall.cell = DrawCellPositionField("挂在哪格", wall.cell);
                    wall.side = (格子模板数据库.WallSide)EditorGUILayout.EnumPopup("边", wall.side);
                    wall.localOffset = EditorGUILayout.Vector3Field("美术偏移", wall.localOffset);
                    wall.alignToBattleCamera = EditorGUILayout.Toggle("平行战斗相机", wall.alignToBattleCamera);
                }
            }
        }
    }

    private static string GetWallVisualFoldoutKey(格子模板数据库.格子模板条目 entry, int index)
    {
        string templateId = entry != null && !string.IsNullOrWhiteSpace(entry.templateId)
            ? entry.templateId.Trim()
            : "未命名模板";
        return $"{templateId}:wall:{index}";
    }

    private static string ResolveWallVisualDisplayName(格子模板数据库.WallVisualEntry wall, int index)
    {
        if (wall != null && !string.IsNullOrWhiteSpace(wall.wallName))
        {
            return wall.wallName.Trim();
        }

        return $"墙{index + 1}";
    }

    private static 格子模板数据库.CellPosition DrawCellPositionField(string label, 格子模板数据库.CellPosition cell)
    {
        Vector2Int value = cell.ToVector2Int();
        value = EditorGUILayout.Vector2IntField(label, value);
        return 格子模板数据库.CellPosition.FromVector2Int(value);
    }

    private void DrawEnemySpawnSlotList(格子模板数据库.格子模板条目 entry, RoomEnemyPresetDatabase encounterDatabase)
    {
        if (entry.enemySpawnSlots == null || entry.enemySpawnSlots.Count == 0)
        {
            EditorGUILayout.LabelField("未设置");
            return;
        }

        RoomEnemyPresetDatabase.RoomEnemyPresetEntry preset = ResolveSelectedEncounterPreset(encounterDatabase);
        string[] enemyOptions = BuildEncounterEnemyOptions(preset);

        for (int i = 0; i < entry.enemySpawnSlots.Count; i++)
        {
            格子模板数据库.EnemySpawnSlot slot = entry.enemySpawnSlots[i];
            if (slot == null)
            {
                continue;
            }

            using (new EditorGUILayout.VerticalScope("box"))
            {
                slot.slotName = EditorGUILayout.TextField("槽位名", slot.slotName);
                EditorGUILayout.LabelField("坐标", FormatCell(slot.cell.ToVector2Int()));

                if (preset == null || preset.enemies == null || preset.enemies.Count == 0)
                {
                    EditorGUILayout.LabelField("绑定敌人", slot.encounterEnemyIndex >= 0 ? $"第{slot.encounterEnemyIndex + 1}个敌人" : "未绑定");
                }
                else
                {
                    bool indexInRange = slot.encounterEnemyIndex >= 0 && slot.encounterEnemyIndex < preset.enemies.Count;
                    if (!indexInRange && slot.encounterEnemyIndex >= 0)
                    {
                        EditorGUILayout.HelpBox($"当前绑定索引 {slot.encounterEnemyIndex} 超出预设范围，请重新指定。", MessageType.Warning);
                    }

                    int selectedIndex = indexInRange ? slot.encounterEnemyIndex + 1 : 0;
                    EditorGUI.BeginChangeCheck();
                    int newIndex = EditorGUILayout.Popup("绑定敌人", selectedIndex, enemyOptions);
                    if (EditorGUI.EndChangeCheck())
                    {
                        slot.encounterEnemyIndex = newIndex - 1;
                    }
                }
            }
        }
    }

    private void DrawSingleSpawnReadonly(string label, bool hasSpawn, 格子模板数据库.CellPosition cell)
    {
        EditorGUILayout.LabelField(label, hasSpawn ? FormatCell(cell.ToVector2Int()) : "未设置");
    }

    private static string FormatCell(Vector2Int cell)
    {
        return $"({cell.x}, {cell.y})";
    }

    private void CreateTemplate(格子模板数据库 database)
    {
        Undo.RecordObject(database, "新建格子模板");
        string templateId = NormalizeIdentifier(newTemplateId, string.Empty);
        if (string.IsNullOrWhiteSpace(templateId))
        {
            return;
        }

        格子模板数据库.格子模板条目 entry = database.GetOrCreateEntry(templateId);
        entry.displayName = string.IsNullOrWhiteSpace(newTemplateName) ? templateId : newTemplateName.Trim();
        格子模板数据库.EnsureValidEntry(entry);
        selectedTemplateId = entry.templateId;
        selectedPropVisualIndex = -1;
        selectedPetalExposureAreaIndex = -1;
        newTemplateId = string.Empty;
        newTemplateName = string.Empty;
        MarkDirtyAndRepaint();
    }

    private void DeleteSelectedTemplate(格子模板数据库 database)
    {
        格子模板数据库.格子模板条目 entry = GetSelectedTemplate(database);
        if (entry == null)
        {
            return;
        }

        Undo.RecordObject(database, "删除格子模板");
        database.RemoveEntry(entry.templateId);
        selectedTemplateId = string.Empty;
        selectedPropVisualIndex = -1;
        selectedPetalExposureAreaIndex = -1;
        EnsureSelection(database);
        MarkDirtyAndRepaint();
    }

    private 格子模板数据库.格子模板条目 GetSelectedTemplate(格子模板数据库 database)
    {
        return database != null ? database.FindEntry(selectedTemplateId) : null;
    }

    private void EnsureSelection(格子模板数据库 database)
    {
        if (database == null)
        {
            selectedTemplateId = string.Empty;
            return;
        }

        if (GetSelectedTemplate(database) != null)
        {
            return;
        }

        List<格子模板数据库.格子模板条目> entries = database.Entries;
        for (int i = 0; i < entries.Count; i++)
        {
            格子模板数据库.格子模板条目 entry = entries[i];
            if (entry == null)
            {
                continue;
            }

            selectedTemplateId = entry.templateId;
            return;
        }

        selectedTemplateId = string.Empty;
    }

    private static 格子模板数据库 EnsureDatabase()
    {
        格子模板数据库 database = AssetDatabase.LoadAssetAtPath<格子模板数据库>(AssetPath);
        if (database != null)
        {
            return database;
        }

        EnsureResourceFolder();
        database = CreateInstance<格子模板数据库>();
        AssetDatabase.CreateAsset(database, AssetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return database;
    }

    private static RoomEnemyPresetDatabase EnsureEncounterDatabase()
    {
        return AssetDatabase.LoadAssetAtPath<RoomEnemyPresetDatabase>(ResourceFolder + "/RoomEnemyPresetDatabase.asset");
    }

    private static void EnsureResourceFolder()
    {
        if (!AssetDatabase.IsValidFolder(ResourceFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }
    }

    private static Rect ResolveCellRect(Rect canvasRect, int height, Vector2Int cell)
    {
        return new Rect(
            canvasRect.x + HeaderSize + CellGap + cell.x * (CellSize + CellGap),
            canvasRect.y + HeaderSize + CellGap + (height - 1 - cell.y) * (CellSize + CellGap),
            CellSize,
            CellSize);
    }

    private static Rect ResolvePetalExposureAreaRect(
        格子模板数据库.格子模板条目 entry,
        Rect canvasRect,
        格子模板数据库.花瓣曝光区域Entry area)
    {
        Vector2Int start = area.startCell.ToVector2Int();
        Vector2Int size = new Vector2Int(Mathf.Max(1, area.size.x), Mathf.Max(1, area.size.y));
        int minX = Mathf.Clamp(start.x, 0, entry.width - 1);
        int minY = Mathf.Clamp(start.y, 0, entry.height - 1);
        int maxX = Mathf.Clamp(start.x + size.x - 1, 0, entry.width - 1);
        int maxY = Mathf.Clamp(start.y + size.y - 1, 0, entry.height - 1);

        return new Rect(
            canvasRect.x + HeaderSize + CellGap + minX * (CellSize + CellGap),
            canvasRect.y + HeaderSize + CellGap + (entry.height - 1 - maxY) * (CellSize + CellGap),
            (maxX - minX + 1) * CellSize + (maxX - minX) * CellGap,
            (maxY - minY + 1) * CellSize + (maxY - minY) * CellGap);
    }

    private static bool TryGetCellAtPosition(
        格子模板数据库.格子模板条目 entry,
        Rect canvasRect,
        Vector2 mousePosition,
        out Vector2Int cell)
    {
        cell = default;

        Rect gridRect = new Rect(
            canvasRect.x + HeaderSize,
            canvasRect.y + HeaderSize,
            entry.width * (CellSize + CellGap) + CellGap,
            entry.height * (CellSize + CellGap) + CellGap);
        if (!gridRect.Contains(mousePosition))
        {
            return false;
        }

        float localX = mousePosition.x - gridRect.x - CellGap;
        float localY = mousePosition.y - gridRect.y - CellGap;
        if (localX < 0f || localY < 0f)
        {
            return false;
        }

        int column = Mathf.FloorToInt(localX / (CellSize + CellGap));
        int rowFromTop = Mathf.FloorToInt(localY / (CellSize + CellGap));
        if (column < 0 || column >= entry.width || rowFromTop < 0 || rowFromTop >= entry.height)
        {
            return false;
        }

        float xRemainder = localX % (CellSize + CellGap);
        float yRemainder = localY % (CellSize + CellGap);
        if (xRemainder > CellSize || yRemainder > CellSize)
        {
            return false;
        }

        cell = new Vector2Int(column, entry.height - 1 - rowFromTop);
        return true;
    }

    private static HashSet<Vector2Int> BuildCellSet(List<格子模板数据库.CellPosition> cells)
    {
        HashSet<Vector2Int> result = new HashSet<Vector2Int>();
        if (cells == null)
        {
            return result;
        }

        for (int i = 0; i < cells.Count; i++)
        {
            result.Add(cells[i].ToVector2Int());
        }

        return result;
    }

    private static 格子模板数据库.EnemySpawnSlot FindEnemySpawnSlot(格子模板数据库.格子模板条目 entry, Vector2Int target)
    {
        if (entry == null || entry.enemySpawnSlots == null)
        {
            return null;
        }

        for (int i = 0; i < entry.enemySpawnSlots.Count; i++)
        {
            格子模板数据库.EnemySpawnSlot slot = entry.enemySpawnSlots[i];
            if (slot != null && slot.cell.x == target.x && slot.cell.y == target.y)
            {
                return slot;
            }
        }

        return null;
    }

    private static bool HasPropAnchorAtCell(格子模板数据库.格子模板条目 entry, Vector2Int target)
    {
        if (entry == null || entry.propVisuals == null)
        {
            return false;
        }

        for (int i = 0; i < entry.propVisuals.Count; i++)
        {
            格子模板数据库.PropVisualEntry prop = entry.propVisuals[i];
            if (prop == null)
            {
                continue;
            }

            if (prop.anchorCell.ToVector2Int() == target)
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasPropOccupiedCell(格子模板数据库.格子模板条目 entry, Vector2Int target)
    {
        if (entry == null || entry.propVisuals == null)
        {
            return false;
        }

        for (int i = 0; i < entry.propVisuals.Count; i++)
        {
            格子模板数据库.PropVisualEntry prop = entry.propVisuals[i];
            if (prop == null || prop.anchorCell.ToVector2Int() == target)
            {
                continue;
            }

            if (ContainsCell(prop.blockedCells, target))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasWallVisualAtCell(格子模板数据库.格子模板条目 entry, Vector2Int target)
    {
        if (entry == null || entry.wallVisuals == null)
        {
            return false;
        }

        for (int i = 0; i < entry.wallVisuals.Count; i++)
        {
            格子模板数据库.WallVisualEntry wall = entry.wallVisuals[i];
            if (wall != null && wall.cell.ToVector2Int() == target)
            {
                return true;
            }
        }

        return false;
    }

    private static void ToggleEnemySpawnSlot(格子模板数据库.格子模板条目 entry, Vector2Int target)
    {
        格子模板数据库.EnemySpawnSlot existing = FindEnemySpawnSlot(entry, target);
        if (existing != null)
        {
            RemoveEnemySpawnSlot(entry, target);
            return;
        }

        if (entry.enemySpawnSlots == null)
        {
            entry.enemySpawnSlots = new List<格子模板数据库.EnemySpawnSlot>();
        }

        entry.enemySpawnSlots.Add(new 格子模板数据库.EnemySpawnSlot
        {
            slotName = $"敌人位{entry.enemySpawnSlots.Count + 1}",
            cell = 格子模板数据库.CellPosition.FromVector2Int(target),
            encounterEnemyIndex = -1
        });
    }

    private static void RemoveEnemySpawnSlot(格子模板数据库.格子模板条目 entry, Vector2Int target)
    {
        if (entry == null || entry.enemySpawnSlots == null)
        {
            return;
        }

        for (int i = entry.enemySpawnSlots.Count - 1; i >= 0; i--)
        {
            格子模板数据库.EnemySpawnSlot slot = entry.enemySpawnSlots[i];
            if (slot != null && slot.cell.x == target.x && slot.cell.y == target.y)
            {
                entry.enemySpawnSlots.RemoveAt(i);
            }
        }
    }

    private static bool ContainsCell(List<格子模板数据库.CellPosition> cells, Vector2Int target)
    {
        if (cells == null)
        {
            return false;
        }

        for (int i = 0; i < cells.Count; i++)
        {
            if (cells[i].x == target.x && cells[i].y == target.y)
            {
                return true;
            }
        }

        return false;
    }

    private static void AddCell(List<格子模板数据库.CellPosition> cells, Vector2Int target)
    {
        if (ContainsCell(cells, target))
        {
            return;
        }

        cells.Add(格子模板数据库.CellPosition.FromVector2Int(target));
    }

    private static void RemoveCell(List<格子模板数据库.CellPosition> cells, Vector2Int target)
    {
        if (cells == null)
        {
            return;
        }

        for (int i = cells.Count - 1; i >= 0; i--)
        {
            if (cells[i].x == target.x && cells[i].y == target.y)
            {
                cells.RemoveAt(i);
            }
        }
    }

    private static void ToggleCell(List<格子模板数据库.CellPosition> cells, Vector2Int target)
    {
        if (ContainsCell(cells, target))
        {
            RemoveCell(cells, target);
        }
        else
        {
            AddCell(cells, target);
        }
    }

    private static void SetSpawn(
        ref bool hasSpawn,
        ref 格子模板数据库.CellPosition spawnCell,
        Vector2Int target)
    {
        hasSpawn = true;
        spawnCell = 格子模板数据库.CellPosition.FromVector2Int(target);
    }

    private static void ClearSpawnIfMatches(
        ref bool hasSpawn,
        ref 格子模板数据库.CellPosition spawnCell,
        Vector2Int target)
    {
        if (!hasSpawn)
        {
            return;
        }

        if (spawnCell.x != target.x || spawnCell.y != target.y)
        {
            return;
        }

        hasSpawn = false;
        spawnCell = default;
    }

    private static bool IsCellInside(格子模板数据库.格子模板条目 entry, Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < entry.width && cell.y >= 0 && cell.y < entry.height;
    }

    private static void ClampTemplateToBounds(格子模板数据库.格子模板条目 entry)
    {
        ClampCellListToBounds(entry.walkableCells, entry);
        ClampEnemySpawnSlotsToBounds(entry);
        ClampPropVisualsToBounds(entry);
        ClampPetalExposureAreasToBounds(entry);
        ClampWallVisualsToBounds(entry);
        ClampSpawnToBounds(ref entry.hasDefaultPlayerSpawn, ref entry.defaultPlayerSpawnCell, entry);
        ClampSpawnToBounds(ref entry.hasEastDoorPlayerSpawn, ref entry.eastDoorPlayerSpawnCell, entry);
        ClampSpawnToBounds(ref entry.hasSouthDoorPlayerSpawn, ref entry.southDoorPlayerSpawnCell, entry);
        ClampSpawnToBounds(ref entry.hasWestDoorPlayerSpawn, ref entry.westDoorPlayerSpawnCell, entry);
        ClampSpawnToBounds(ref entry.hasNorthDoorPlayerSpawn, ref entry.northDoorPlayerSpawnCell, entry);
        ClampSpawnToBounds(ref entry.hasEastDoorEntrance, ref entry.eastDoorEntranceCell, entry);
        ClampSpawnToBounds(ref entry.hasSouthDoorEntrance, ref entry.southDoorEntranceCell, entry);
        ClampSpawnToBounds(ref entry.hasWestDoorEntrance, ref entry.westDoorEntranceCell, entry);
        ClampSpawnToBounds(ref entry.hasNorthDoorEntrance, ref entry.northDoorEntranceCell, entry);
    }

    private static void ClampCellListToBounds(
        List<格子模板数据库.CellPosition> cells,
        格子模板数据库.格子模板条目 entry)
    {
        if (cells == null)
        {
            return;
        }

        HashSet<Vector2Int> deduplicated = new HashSet<Vector2Int>();
        for (int i = cells.Count - 1; i >= 0; i--)
        {
            Vector2Int cell = cells[i].ToVector2Int();
            if (!IsCellInside(entry, cell) || !deduplicated.Add(cell))
            {
                cells.RemoveAt(i);
            }
        }
    }

    private static void ClampEnemySpawnSlotsToBounds(格子模板数据库.格子模板条目 entry)
    {
        if (entry == null || entry.enemySpawnSlots == null)
        {
            return;
        }

        HashSet<Vector2Int> deduplicated = new HashSet<Vector2Int>();
        for (int i = entry.enemySpawnSlots.Count - 1; i >= 0; i--)
        {
            格子模板数据库.EnemySpawnSlot slot = entry.enemySpawnSlots[i];
            if (slot == null)
            {
                entry.enemySpawnSlots.RemoveAt(i);
                continue;
            }

            Vector2Int cell = slot.cell.ToVector2Int();
            if (!IsCellInside(entry, cell) || !deduplicated.Add(cell))
            {
                entry.enemySpawnSlots.RemoveAt(i);
            }
        }
    }

    private static void ClampPropVisualsToBounds(格子模板数据库.格子模板条目 entry)
    {
        if (entry == null || entry.propVisuals == null)
        {
            return;
        }

        for (int i = entry.propVisuals.Count - 1; i >= 0; i--)
        {
            格子模板数据库.PropVisualEntry prop = entry.propVisuals[i];
            if (prop == null)
            {
                entry.propVisuals.RemoveAt(i);
                continue;
            }

            if (!IsCellInside(entry, prop.anchorCell.ToVector2Int()))
            {
                entry.propVisuals.RemoveAt(i);
                continue;
            }

            ClampCellListToBounds(prop.blockedCells, entry);
        }
    }

    private static void ClampPetalExposureAreasToBounds(格子模板数据库.格子模板条目 entry)
    {
        if (entry == null || entry.花瓣曝光区域列表 == null)
        {
            return;
        }

        for (int i = entry.花瓣曝光区域列表.Count - 1; i >= 0; i--)
        {
            格子模板数据库.花瓣曝光区域Entry area = entry.花瓣曝光区域列表[i];
            if (area == null)
            {
                entry.花瓣曝光区域列表.RemoveAt(i);
                continue;
            }

            Vector2Int start = area.startCell.ToVector2Int();
            if (!IsCellInside(entry, start))
            {
                entry.花瓣曝光区域列表.RemoveAt(i);
                continue;
            }

            area.size.x = Mathf.Clamp(area.size.x, 1, entry.width - start.x);
            area.size.y = Mathf.Clamp(area.size.y, 1, entry.height - start.y);
        }
    }

    private static void ClampWallVisualsToBounds(格子模板数据库.格子模板条目 entry)
    {
        if (entry == null || entry.wallVisuals == null)
        {
            return;
        }

        for (int i = entry.wallVisuals.Count - 1; i >= 0; i--)
        {
            格子模板数据库.WallVisualEntry wall = entry.wallVisuals[i];
            if (wall == null || !IsCellInside(entry, wall.cell.ToVector2Int()))
            {
                entry.wallVisuals.RemoveAt(i);
            }
        }
    }

    private static void ClampSpawnToBounds(
        ref bool hasSpawn,
        ref 格子模板数据库.CellPosition spawnCell,
        格子模板数据库.格子模板条目 entry)
    {
        if (!hasSpawn)
        {
            return;
        }

        if (IsCellInside(entry, spawnCell.ToVector2Int()))
        {
            return;
        }

        hasSpawn = false;
        spawnCell = default;
    }

    private void MarkDirtyAndRepaint()
    {
        格子模板数据库 database = EnsureDatabase();
        EditorUtility.SetDirty(database);
        Repaint();
    }

    private void EnsureEncounterSelection(RoomEnemyPresetDatabase encounterDatabase)
    {
        if (encounterDatabase == null || encounterDatabase.Entries == null || encounterDatabase.Entries.Count == 0)
        {
            selectedEncounterPresetId = string.Empty;
            return;
        }

        if (ResolveSelectedEncounterPreset(encounterDatabase) != null)
        {
            return;
        }

        for (int i = 0; i < encounterDatabase.Entries.Count; i++)
        {
            RoomEnemyPresetDatabase.RoomEnemyPresetEntry preset = encounterDatabase.Entries[i];
            if (preset != null && !string.IsNullOrWhiteSpace(preset.presetId))
            {
                selectedEncounterPresetId = preset.presetId.Trim();
                return;
            }
        }

        selectedEncounterPresetId = string.Empty;
    }

    private RoomEnemyPresetDatabase.RoomEnemyPresetEntry ResolveSelectedEncounterPreset(RoomEnemyPresetDatabase encounterDatabase)
    {
        if (encounterDatabase == null || string.IsNullOrWhiteSpace(selectedEncounterPresetId))
        {
            return null;
        }

        return encounterDatabase.FindEntry(selectedEncounterPresetId.Trim());
    }

    private static List<string> BuildEncounterPresetIds(RoomEnemyPresetDatabase encounterDatabase)
    {
        List<string> ids = new List<string> { string.Empty };
        if (encounterDatabase == null || encounterDatabase.Entries == null)
        {
            return ids;
        }

        for (int i = 0; i < encounterDatabase.Entries.Count; i++)
        {
            RoomEnemyPresetDatabase.RoomEnemyPresetEntry preset = encounterDatabase.Entries[i];
            if (preset == null || string.IsNullOrWhiteSpace(preset.presetId))
            {
                continue;
            }

            ids.Add(preset.presetId.Trim());
        }

        return ids;
    }

    private static List<string> BuildEncounterPresetLabels(RoomEnemyPresetDatabase encounterDatabase, List<string> presetIds)
    {
        List<string> labels = new List<string>();
        for (int i = 0; i < presetIds.Count; i++)
        {
            string presetId = presetIds[i];
            if (string.IsNullOrWhiteSpace(presetId))
            {
                labels.Add("未选择");
                continue;
            }

            RoomEnemyPresetDatabase.RoomEnemyPresetEntry preset = encounterDatabase != null ? encounterDatabase.FindEntry(presetId) : null;
            int enemyCount = preset != null && preset.enemies != null ? preset.enemies.Count : 0;
            labels.Add($"{presetId} ({enemyCount}个敌人)");
        }

        return labels;
    }

    private int GetSelectedEncounterPresetIndex(List<string> presetIds)
    {
        for (int i = 0; i < presetIds.Count; i++)
        {
            if (string.Equals(presetIds[i], selectedEncounterPresetId, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return 0;
    }

    private static string[] BuildEncounterEnemyOptions(RoomEnemyPresetDatabase.RoomEnemyPresetEntry preset)
    {
        if (preset == null || preset.enemies == null || preset.enemies.Count == 0)
        {
            return new[] { "未绑定" };
        }

        string[] options = new string[preset.enemies.Count + 1];
        options[0] = "未绑定";
        for (int i = 0; i < preset.enemies.Count; i++)
        {
            RoomEnemyPresetDatabase.PresetEnemyEntry enemy = preset.enemies[i];
            string enemyId = enemy != null && !string.IsNullOrWhiteSpace(enemy.enemyId) ? enemy.enemyId.Trim() : "未命名敌人";
            options[i + 1] = $"第{i + 1}个：{enemyId}";
        }

        return options;
    }

    private static string ResolveEnemySlotMarker(格子模板数据库.格子模板条目 entry, 格子模板数据库.EnemySpawnSlot slot)
    {
        if (entry == null || slot == null || entry.enemySpawnSlots == null)
        {
            return "敌";
        }

        int index = entry.enemySpawnSlots.IndexOf(slot);
        return index >= 0 ? (index + 1).ToString() : "敌";
    }

    private string ResolveCurrentToolLabel(格子模板数据库.格子模板条目 entry)
    {
        if (IsPropPlacementTool(entry))
        {
            格子模板数据库.PropVisualEntry prop = entry.propVisuals[selectedPropVisualIndex];
            string propName = prop != null && !string.IsNullOrWhiteSpace(prop.propName)
                ? prop.propName.Trim()
                : $"物件{selectedPropVisualIndex + 1}";
            return $"物件：{propName}";
        }

        if (IsPetalExposureAreaTool(entry))
        {
            格子模板数据库.花瓣曝光区域Entry area = entry.花瓣曝光区域列表[selectedPetalExposureAreaIndex];
            return $"花瓣曝光区域：{ResolvePetalExposureAreaDisplayName(area, selectedPetalExposureAreaIndex)}";
        }

        return ResolveToolLabel(currentTool);
    }

    private static string ResolveToolLabel(绘制工具 tool)
    {
        switch (tool)
        {
            case 绘制工具.可用格:
                return "可用格";
            case 绘制工具.敌人出生位:
                return "敌人出生位";
            case 绘制工具.玩家默认出生点:
                return "玩家默认出生点";
            case 绘制工具.玩家东门出生点:
                return "玩家东门出生点";
            case 绘制工具.玩家南门出生点:
                return "玩家南门出生点";
            case 绘制工具.玩家西门出生点:
                return "玩家西门出生点";
            case 绘制工具.玩家北门出生点:
                return "玩家北门出生点";
            case 绘制工具.东门口:
                return "东门口";
            case 绘制工具.南门口:
                return "南门口";
            case 绘制工具.西门口:
                return "西门口";
            case 绘制工具.北门口:
                return "北门口";
            default:
                return string.Empty;
        }
    }

    private static string NormalizeIdentifier(string rawValue, string fallbackValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return string.IsNullOrWhiteSpace(fallbackValue) ? string.Empty : fallbackValue.Trim();
        }

        string normalized = rawValue.Trim();
        normalized = normalized.Replace(' ', '_');
        normalized = normalized.Replace('\t', '_');
        while (normalized.Contains("__"))
        {
            normalized = normalized.Replace("__", "_");
        }

        return normalized;
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
}

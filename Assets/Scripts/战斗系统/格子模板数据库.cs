using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BattleGridTemplateDatabase", menuName = "战斗/格子模板库")]
public sealed class 格子模板数据库 : ScriptableObject
{
    public const string DefaultResourcePath = "BattleGridTemplateDatabase";

    [Serializable]
    public struct CellPosition
    {
        public int x;
        public int y;

        public CellPosition(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public Vector2Int ToVector2Int()
        {
            return new Vector2Int(x, y);
        }

        public static CellPosition FromVector2Int(Vector2Int cell)
        {
            return new CellPosition(cell.x, cell.y);
        }
    }

    [Serializable]
    public sealed class EnemySpawnSlot
    {
        public string slotName = string.Empty;
        public CellPosition cell;
        public int encounterEnemyIndex = -1;
    }

    public enum WallSide
    {
        East = 0,
        South = 1,
        West = 2,
        North = 3
    }

    [Serializable]
    public sealed class PropVisualEntry
    {
        public string propName = string.Empty;
        public GameObject prefab;
        public CellPosition anchorCell;
        public Vector3 localOffset = Vector3.zero;
        public bool alignToBattleCamera = true;
        public bool blocksMovement = true;
        public bool isTriggerable;
        public List<CellPosition> triggerCells = new List<CellPosition>();
        public List<CellPosition> blockedCells = new List<CellPosition>();
    }

    [Serializable]
    public sealed class WallVisualEntry
    {
        public string wallName = string.Empty;
        public GameObject prefab;
        public CellPosition cell;
        public WallSide side = WallSide.North;
        public Vector3 localOffset = Vector3.zero;
        public bool alignToBattleCamera = true;
    }

    [Serializable]
    public sealed class 花瓣曝光区域Entry
    {
        public string areaName = string.Empty;
        public CellPosition startCell;
        public Vector2Int size = Vector2Int.one;
    }

    [Serializable]
    public sealed class 格子模板条目
    {
        public string templateId = string.Empty;
        public string displayName = string.Empty;
        public int width = 20;
        public int height = 20;
        public List<CellPosition> walkableCells = new List<CellPosition>();
        public List<EnemySpawnSlot> enemySpawnSlots = new List<EnemySpawnSlot>();
        public GameObject defaultFloorPrefab;
        public bool alignFloorToBattleCamera = true;
        public GameObject 花瓣粒子预制体;
        public List<花瓣曝光区域Entry> 花瓣曝光区域列表 = new List<花瓣曝光区域Entry>();
        public List<PropVisualEntry> propVisuals = new List<PropVisualEntry>();
        public List<WallVisualEntry> wallVisuals = new List<WallVisualEntry>();
        public bool hasDefaultPlayerSpawn;
        public CellPosition defaultPlayerSpawnCell;
        public bool hasEastDoorPlayerSpawn;
        public CellPosition eastDoorPlayerSpawnCell;
        public bool hasSouthDoorPlayerSpawn;
        public CellPosition southDoorPlayerSpawnCell;
        public bool hasWestDoorPlayerSpawn;
        public CellPosition westDoorPlayerSpawnCell;
        public bool hasNorthDoorPlayerSpawn;
        public CellPosition northDoorPlayerSpawnCell;
        public bool hasEastDoorEntrance;
        public CellPosition eastDoorEntranceCell;
        public bool hasSouthDoorEntrance;
        public CellPosition southDoorEntranceCell;
        public bool hasWestDoorEntrance;
        public CellPosition westDoorEntranceCell;
        public bool hasNorthDoorEntrance;
        public CellPosition northDoorEntranceCell;
    }

    [SerializeField] private List<格子模板条目> entries = new List<格子模板条目>();

    public List<格子模板条目> Entries => entries;

    public 格子模板条目 FindEntry(string templateId)
    {
        if (string.IsNullOrWhiteSpace(templateId))
        {
            return null;
        }

        string resolvedId = templateId.Trim();
        for (int i = 0; i < entries.Count; i++)
        {
            格子模板条目 entry = entries[i];
            if (entry == null)
            {
                continue;
            }

            if (!string.Equals(entry.templateId, resolvedId, StringComparison.Ordinal))
            {
                continue;
            }

            EnsureValidEntry(entry);
            return entry;
        }

        return null;
    }

    public 格子模板条目 GetOrCreateEntry(string templateId)
    {
        if (string.IsNullOrWhiteSpace(templateId))
        {
            return null;
        }

        string resolvedId = templateId.Trim();
        格子模板条目 existing = FindEntry(resolvedId);
        if (existing != null)
        {
            return existing;
        }

        格子模板条目 created = new 格子模板条目
        {
            templateId = resolvedId,
            displayName = resolvedId
        };

        EnsureValidEntry(created);
        entries.Add(created);
        return created;
    }

    public bool RemoveEntry(string templateId)
    {
        if (string.IsNullOrWhiteSpace(templateId))
        {
            return false;
        }

        string resolvedId = templateId.Trim();
        for (int i = entries.Count - 1; i >= 0; i--)
        {
            格子模板条目 entry = entries[i];
            if (entry == null)
            {
                continue;
            }

            if (!string.Equals(entry.templateId, resolvedId, StringComparison.Ordinal))
            {
                continue;
            }

            entries.RemoveAt(i);
            return true;
        }

        return false;
    }

    public static void EnsureValidEntry(格子模板条目 entry)
    {
        if (entry == null)
        {
            return;
        }

        if (entry.walkableCells == null)
        {
            entry.walkableCells = new List<CellPosition>();
        }

        if (entry.enemySpawnSlots == null)
        {
            entry.enemySpawnSlots = new List<EnemySpawnSlot>();
        }

        if (entry.propVisuals == null)
        {
            entry.propVisuals = new List<PropVisualEntry>();
        }

        if (entry.花瓣曝光区域列表 == null)
        {
            entry.花瓣曝光区域列表 = new List<花瓣曝光区域Entry>();
        }

        if (entry.wallVisuals == null)
        {
            entry.wallVisuals = new List<WallVisualEntry>();
        }

        entry.width = Mathf.Max(1, entry.width);
        entry.height = Mathf.Max(1, entry.height);

        if (string.IsNullOrWhiteSpace(entry.displayName))
        {
            entry.displayName = entry.templateId;
        }

        for (int i = entry.enemySpawnSlots.Count - 1; i >= 0; i--)
        {
            EnemySpawnSlot slot = entry.enemySpawnSlots[i];
            if (slot == null)
            {
                entry.enemySpawnSlots.RemoveAt(i);
                continue;
            }

            if (string.IsNullOrWhiteSpace(slot.slotName))
            {
                slot.slotName = $"敌人位{i + 1}";
            }
        }

        for (int i = entry.propVisuals.Count - 1; i >= 0; i--)
        {
            PropVisualEntry prop = entry.propVisuals[i];
            if (prop == null)
            {
                entry.propVisuals.RemoveAt(i);
                continue;
            }

            if (prop.blockedCells == null)
            {
                prop.blockedCells = new List<CellPosition>();
            }

            if (prop.triggerCells == null)
            {
                prop.triggerCells = new List<CellPosition>();
            }

            if (string.IsNullOrWhiteSpace(prop.propName))
            {
                prop.propName = $"物件{i + 1}";
            }
        }

        for (int i = entry.花瓣曝光区域列表.Count - 1; i >= 0; i--)
        {
            花瓣曝光区域Entry area = entry.花瓣曝光区域列表[i];
            if (area == null)
            {
                entry.花瓣曝光区域列表.RemoveAt(i);
                continue;
            }

            if (string.IsNullOrWhiteSpace(area.areaName))
            {
                area.areaName = $"曝光区域{i + 1}";
            }

            area.size.x = Mathf.Max(1, area.size.x);
            area.size.y = Mathf.Max(1, area.size.y);
        }

        for (int i = entry.wallVisuals.Count - 1; i >= 0; i--)
        {
            WallVisualEntry wall = entry.wallVisuals[i];
            if (wall == null)
            {
                entry.wallVisuals.RemoveAt(i);
                continue;
            }

            if (string.IsNullOrWhiteSpace(wall.wallName))
            {
                wall.wallName = $"墙{i + 1}";
            }
        }
    }

    public static 格子模板数据库 LoadDefault()
    {
        return Resources.Load<格子模板数据库>(DefaultResourcePath);
    }
}

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
    public sealed class 格子模板条目
    {
        public string templateId = string.Empty;
        public string displayName = string.Empty;
        public int width = 20;
        public int height = 20;
        public List<CellPosition> walkableCells = new List<CellPosition>();
        public List<CellPosition> enemySpawnCells = new List<CellPosition>();
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

        if (entry.enemySpawnCells == null)
        {
            entry.enemySpawnCells = new List<CellPosition>();
        }

        entry.width = Mathf.Max(1, entry.width);
        entry.height = Mathf.Max(1, entry.height);

        if (string.IsNullOrWhiteSpace(entry.displayName))
        {
            entry.displayName = entry.templateId;
        }
    }

    public static 格子模板数据库 LoadDefault()
    {
        return Resources.Load<格子模板数据库>(DefaultResourcePath);
    }
}

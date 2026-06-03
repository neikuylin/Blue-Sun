using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MapTemplateDatabase", menuName = "战斗/地图模板库")]
public sealed class MapTemplateDatabase : ScriptableObject
{
    public const string DefaultResourcePath = "MapTemplateDatabase";

    public enum ConnectionDirection
    {
        East = 0,
        South = 1,
        West = 2,
        North = 3
    }

    [Serializable]
    public sealed class MapConnectionEntry
    {
        public string targetNodeId = string.Empty;
        public ConnectionDirection direction = ConnectionDirection.East;
    }

    [Serializable]
    public sealed class MapNodeEntry
    {
        public string nodeId = string.Empty;
        public string displayName = string.Empty;
        public Vector2 position = new Vector2(120f, 120f);
        public string roomTypeId = RoomTypeDatabase.EncounterBattleTypeId;
        public string encounterPresetId = string.Empty;
        public string battleGridTemplateId = string.Empty;
        public List<MapConnectionEntry> connections = new List<MapConnectionEntry>();
        public List<string> nextNodeIds = new List<string>();
    }

    [Serializable]
    public sealed class MapTemplateEntry
    {
        public string templateId = string.Empty;
        public string displayName = string.Empty;
        public int maxPartySize = 4;
        public List<MapNodeEntry> nodes = new List<MapNodeEntry>();
    }

    [SerializeField] private List<MapTemplateEntry> entries = new List<MapTemplateEntry>();

    public List<MapTemplateEntry> Entries => entries;

    public MapTemplateEntry FindEntry(string templateId)
    {
        if (string.IsNullOrWhiteSpace(templateId))
        {
            return null;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            MapTemplateEntry entry = entries[i];
            if (entry == null)
            {
                continue;
            }

            if (string.Equals(entry.templateId, templateId.Trim(), StringComparison.Ordinal))
            {
                EnsureValidTemplate(entry);
                return entry;
            }
        }

        return null;
    }

    public MapTemplateEntry GetOrCreateEntry(string templateId)
    {
        if (string.IsNullOrWhiteSpace(templateId))
        {
            return null;
        }

        string resolvedId = templateId.Trim();
        MapTemplateEntry existing = FindEntry(resolvedId);
        if (existing != null)
        {
            return existing;
        }

        MapTemplateEntry created = new MapTemplateEntry
        {
            templateId = resolvedId,
            displayName = resolvedId
        };
        EnsureValidTemplate(created);
        entries.Add(created);
        return created;
    }

    public bool RemoveEntry(string templateId)
    {
        if (string.IsNullOrWhiteSpace(templateId))
        {
            return false;
        }

        for (int i = entries.Count - 1; i >= 0; i--)
        {
            MapTemplateEntry entry = entries[i];
            if (entry == null)
            {
                continue;
            }

            if (!string.Equals(entry.templateId, templateId.Trim(), StringComparison.Ordinal))
            {
                continue;
            }

            entries.RemoveAt(i);
            return true;
        }

        return false;
    }

    public static void EnsureValidTemplate(MapTemplateEntry template)
    {
        if (template == null)
        {
            return;
        }

        if (template.nodes == null)
        {
            template.nodes = new List<MapNodeEntry>();
        }

        template.maxPartySize = Mathf.Max(1, template.maxPartySize);

        for (int i = 0; i < template.nodes.Count; i++)
        {
            EnsureValidNode(template.nodes[i]);
        }
    }

    public static void EnsureValidNode(MapNodeEntry node)
    {
        if (node == null)
        {
            return;
        }

        if (node.nextNodeIds == null)
        {
            node.nextNodeIds = new List<string>();
        }

        if (node.connections == null)
        {
            node.connections = new List<MapConnectionEntry>();
        }

        if (string.IsNullOrWhiteSpace(node.roomTypeId))
        {
            node.roomTypeId = RoomTypeDatabase.EncounterBattleTypeId;
        }

        if (string.IsNullOrWhiteSpace(node.displayName))
        {
            node.displayName = node.nodeId;
        }

        if (node.connections.Count == 0 && node.nextNodeIds.Count > 0)
        {
            for (int i = 0; i < node.nextNodeIds.Count; i++)
            {
                string targetNodeId = node.nextNodeIds[i];
                if (string.IsNullOrWhiteSpace(targetNodeId))
                {
                    continue;
                }

                node.connections.Add(new MapConnectionEntry
                {
                    targetNodeId = targetNodeId,
                    direction = ConnectionDirection.East
                });
            }
        }
    }

    public static MapTemplateDatabase LoadDefault()
    {
        return Resources.Load<MapTemplateDatabase>(DefaultResourcePath);
    }
}

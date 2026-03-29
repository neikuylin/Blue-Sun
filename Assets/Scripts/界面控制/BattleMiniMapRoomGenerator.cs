using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class BattleMiniMapRoomGenerator : MonoBehaviour
{
    private const string GeneratedNodePrefix = "__MiniMapRoom_";
    private const string DefaultTemplateId = "地牢1";
    private const string DefaultStartNodeId = "入口";

    [Header("Template")]
    [SerializeField] private MapTemplateDatabase mapTemplateDatabase;
    [SerializeField] private string templateId = DefaultTemplateId;
    [SerializeField] private string startNodeId = DefaultStartNodeId;

    [Header("Prefab")]
    [SerializeField] private GameObject roomPrefab;

    [Header("Placement")]
    [SerializeField] private Vector2 startAnchoredPosition = Vector2.zero;
    [SerializeField] private Vector2 directionStep = new Vector2(56f, 56f);

    [Header("Generation")]
    [SerializeField] private bool regenerateOnEnable = true;

    private bool isRegenerating;
#if UNITY_EDITOR
    private bool regenerateQueuedInEditor;
#endif

    public void RegenerateRooms()
    {
        if (isRegenerating)
        {
            return;
        }

        isRegenerating = true;
        try
        {
            ClearGeneratedRooms();

            MapTemplateDatabase.MapTemplateEntry template = ResolveTemplate();
            if (template == null || template.nodes == null || template.nodes.Count == 0 || roomPrefab == null)
            {
                return;
            }

            Dictionary<string, Vector2Int> nodeGridPositions = BuildNodeGridPositions(template);
            for (int i = 0; i < template.nodes.Count; i++)
            {
                MapTemplateDatabase.MapNodeEntry node = template.nodes[i];
                if (node == null || string.IsNullOrWhiteSpace(node.nodeId))
                {
                    continue;
                }

                GameObject instance = InstantiateRoomPrefab();
                if (instance == null)
                {
                    continue;
                }

                instance.name = GeneratedNodePrefix + node.nodeId;
                instance.transform.SetParent(transform, false);
                instance.transform.localScale = Vector3.one;

                RectTransform rectTransform = instance.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.anchoredPosition = ResolveAnchoredPosition(nodeGridPositions, node.nodeId);
                }

                ApplyNodeLabel(instance, node);
            }
        }
        finally
        {
            isRegenerating = false;
        }
    }

    private void OnEnable()
    {
        QueueRegenerate();
    }

    private void OnValidate()
    {
        QueueRegenerate();
    }

    private void QueueRegenerate()
    {
        if (!regenerateOnEnable)
        {
            return;
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            if (regenerateQueuedInEditor)
            {
                return;
            }

            regenerateQueuedInEditor = true;
            EditorApplication.delayCall += RegenerateRoomsInEditor;
            return;
        }
#endif

        RegenerateRooms();
    }

#if UNITY_EDITOR
    private void RegenerateRoomsInEditor()
    {
        EditorApplication.delayCall -= RegenerateRoomsInEditor;
        regenerateQueuedInEditor = false;

        if (this == null || gameObject == null)
        {
            return;
        }

        RegenerateRooms();
    }
#endif

    private void ClearGeneratedRooms()
    {
        List<GameObject> generatedChildren = new List<GameObject>();
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child == null || !child.name.StartsWith(GeneratedNodePrefix, StringComparison.Ordinal))
            {
                continue;
            }

            generatedChildren.Add(child.gameObject);
        }

        for (int i = 0; i < generatedChildren.Count; i++)
        {
            DestroyObject(generatedChildren[i]);
        }
    }

    private GameObject InstantiateRoomPrefab()
    {
        if (roomPrefab == null)
        {
            return null;
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            GameObject prefabInstance = PrefabUtility.InstantiatePrefab(roomPrefab, transform) as GameObject;
            if (prefabInstance != null)
            {
                return prefabInstance;
            }
        }
#endif

        return Instantiate(roomPrefab, transform, false);
    }

    private MapTemplateDatabase.MapTemplateEntry ResolveTemplate()
    {
        MapTemplateDatabase database = mapTemplateDatabase != null
            ? mapTemplateDatabase
            : MapTemplateDatabase.LoadDefault();
        return database != null ? database.FindEntry(templateId) : null;
    }

    private MapTemplateDatabase.MapNodeEntry FindStartNode(MapTemplateDatabase.MapTemplateEntry template)
    {
        if (template == null || template.nodes == null || template.nodes.Count == 0)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(startNodeId))
        {
            MapTemplateDatabase.MapNodeEntry configured = FindNode(template, startNodeId);
            if (configured != null)
            {
                return configured;
            }
        }

        MapTemplateDatabase.MapNodeEntry defaultEntry = FindNode(template, DefaultStartNodeId);
        if (defaultEntry != null)
        {
            return defaultEntry;
        }

        return template.nodes[0];
    }

    private Dictionary<string, Vector2Int> BuildNodeGridPositions(MapTemplateDatabase.MapTemplateEntry template)
    {
        Dictionary<string, Vector2Int> result = new Dictionary<string, Vector2Int>(StringComparer.Ordinal);
        MapTemplateDatabase.MapNodeEntry startNode = FindStartNode(template);
        if (startNode == null || string.IsNullOrWhiteSpace(startNode.nodeId))
        {
            return result;
        }

        Queue<MapTemplateDatabase.MapNodeEntry> queue = new Queue<MapTemplateDatabase.MapNodeEntry>();
        result[startNode.nodeId] = Vector2Int.zero;
        queue.Enqueue(startNode);

        while (queue.Count > 0)
        {
            MapTemplateDatabase.MapNodeEntry current = queue.Dequeue();
            if (current == null || string.IsNullOrWhiteSpace(current.nodeId))
            {
                continue;
            }

            MapTemplateDatabase.EnsureValidNode(current);

            Vector2Int currentPosition;
            if (!result.TryGetValue(current.nodeId, out currentPosition))
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

                if (result.ContainsKey(connection.targetNodeId))
                {
                    continue;
                }

                MapTemplateDatabase.MapNodeEntry target = FindNode(template, connection.targetNodeId);
                if (target == null)
                {
                    continue;
                }

                result[target.nodeId] = currentPosition + ResolveDirectionOffset(connection.direction);
                queue.Enqueue(target);
            }
        }

        int fallbackIndex = 1;
        for (int i = 0; i < template.nodes.Count; i++)
        {
            MapTemplateDatabase.MapNodeEntry node = template.nodes[i];
            if (node == null || string.IsNullOrWhiteSpace(node.nodeId) || result.ContainsKey(node.nodeId))
            {
                continue;
            }

            result[node.nodeId] = new Vector2Int(fallbackIndex, 0);
            fallbackIndex++;
        }

        return result;
    }

    private Vector2 ResolveAnchoredPosition(Dictionary<string, Vector2Int> nodeGridPositions, string nodeId)
    {
        Vector2Int logicalPosition;
        if (nodeGridPositions == null || !nodeGridPositions.TryGetValue(nodeId, out logicalPosition))
        {
            return startAnchoredPosition;
        }

        return new Vector2(
            startAnchoredPosition.x + logicalPosition.x * directionStep.x,
            startAnchoredPosition.y + logicalPosition.y * directionStep.y);
    }

    private static Vector2Int ResolveDirectionOffset(MapTemplateDatabase.ConnectionDirection direction)
    {
        switch (direction)
        {
            case MapTemplateDatabase.ConnectionDirection.North:
                return new Vector2Int(-1, 1);
            case MapTemplateDatabase.ConnectionDirection.South:
                return new Vector2Int(1, -1);
            case MapTemplateDatabase.ConnectionDirection.West:
                return new Vector2Int(-1, -1);
            case MapTemplateDatabase.ConnectionDirection.East:
                return new Vector2Int(1, 1);
            default:
                return Vector2Int.zero;
        }
    }

    private static MapTemplateDatabase.MapNodeEntry FindNode(MapTemplateDatabase.MapTemplateEntry template, string nodeId)
    {
        if (template == null || template.nodes == null || string.IsNullOrWhiteSpace(nodeId))
        {
            return null;
        }

        for (int i = 0; i < template.nodes.Count; i++)
        {
            MapTemplateDatabase.MapNodeEntry node = template.nodes[i];
            if (node == null || !string.Equals(node.nodeId, nodeId, StringComparison.Ordinal))
            {
                continue;
            }

            return node;
        }

        return null;
    }

    private static void ApplyNodeLabel(GameObject instance, MapTemplateDatabase.MapNodeEntry node)
    {
        if (instance == null || node == null)
        {
            return;
        }

        string label = string.IsNullOrWhiteSpace(node.displayName) ? node.nodeId : node.displayName;

        TMP_Text tmpText = instance.GetComponentInChildren<TMP_Text>(true);
        if (tmpText != null)
        {
            tmpText.text = label;
            return;
        }

        UnityEngine.UI.Text legacyText = instance.GetComponentInChildren<UnityEngine.UI.Text>(true);
        if (legacyText != null)
        {
            legacyText.text = label;
        }
    }

    private static void DestroyObject(GameObject target)
    {
        if (target == null)
        {
            return;
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            DestroyImmediate(target);
            return;
        }
#endif

        Destroy(target);
    }
}

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(DiamondLayoutGroup))]
public sealed class BattleMiniMapRoomGenerator : MonoBehaviour
{
    private const string GeneratedNodePrefix = "__MiniMapRoom_";

    [Header("Template")]
    [SerializeField] private MapTemplateDatabase mapTemplateDatabase;
    [SerializeField] private string templateId = "地牢1";

    [Header("Prefab")]
    [SerializeField] private GameObject roomPrefab;

    [Header("Generation")]
    [SerializeField] private bool regenerateOnEnable = true;

    private bool isRegenerating;

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

            MapTemplateDatabase database = mapTemplateDatabase != null
                ? mapTemplateDatabase
                : MapTemplateDatabase.LoadDefault();
            if (database == null || roomPrefab == null || string.IsNullOrWhiteSpace(templateId))
            {
                return;
            }

            MapTemplateDatabase.MapTemplateEntry template = database.FindEntry(templateId);
            if (template == null || template.nodes == null || template.nodes.Count == 0)
            {
                return;
            }

            List<GameObject> spawnedRooms = new List<GameObject>(template.nodes.Count);
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
                RectTransform rectTransform = instance.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.SetParent(transform, false);
                    rectTransform.localScale = Vector3.one;
                }
                else
                {
                    instance.transform.SetParent(transform, false);
                    instance.transform.localScale = Vector3.one;
                }

                ApplyNodeLabel(instance, node);
                spawnedRooms.Add(instance);
            }

            if (GetComponent<DiamondLayoutGroup>() != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
            }
        }
        finally
        {
            isRegenerating = false;
        }
    }

    private void OnEnable()
    {
        if (regenerateOnEnable)
        {
            RegenerateRooms();
        }
    }

    private void OnValidate()
    {
        if (!regenerateOnEnable)
        {
            return;
        }

        RegenerateRooms();
    }

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

        Text legacyText = instance.GetComponentInChildren<Text>(true);
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

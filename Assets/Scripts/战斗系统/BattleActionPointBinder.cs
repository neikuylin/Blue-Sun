using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class BattleActionPointBinder : MonoBehaviour
{
    private const string ActionPointPanelPath = "Canvas/下方栏位/角色操作栏/角色栏/行动力面板";

    private readonly List<GameObject> actionPointIndicators = new List<GameObject>();
    private BattleTurnSystem turnSystem;
    private int lastCurrentPoints = int.MinValue;
    private int lastMaxPoints = int.MinValue;
    private string lastUnitId = string.Empty;

    public void Initialize(BattleTurnSystem system)
    {
        turnSystem = system;
        CacheIndicators();
        Refresh(force: true);
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying || turnSystem == null)
        {
            return;
        }

        Refresh(force: false);
    }

    private void CacheIndicators()
    {
        if (actionPointIndicators.Count > 0)
        {
            return;
        }

        Transform panel = FindTransformByPath(ActionPointPanelPath);
        if (panel == null)
        {
            return;
        }

        List<Transform> pointSlots = new List<Transform>();
        for (int i = 0; i < panel.childCount; i++)
        {
            Transform child = panel.GetChild(i);
            if (child != null)
            {
                pointSlots.Add(child);
            }
        }

        pointSlots.Sort((left, right) => left.GetSiblingIndex().CompareTo(right.GetSiblingIndex()));
        for (int i = 0; i < pointSlots.Count; i++)
        {
            GameObject indicator = FindIndicatorObject(pointSlots[i]);
            if (indicator != null)
            {
                actionPointIndicators.Add(indicator);
            }
        }
    }

    private void Refresh(bool force)
    {
        CacheIndicators();
        if (actionPointIndicators.Count == 0)
        {
            return;
        }

        BattleUnit activeUnit = turnSystem.ActiveUnit;
        bool showPoints = activeUnit != null && activeUnit.IsAlive && activeUnit.isPlayerControlled;
        int currentPoints = showPoints ? Mathf.Max(0, activeUnit.currentActionPoints) : 0;
        int maxPoints = showPoints ? Mathf.Max(0, activeUnit.maxActionPoints) : 0;
        string unitId = showPoints ? activeUnit.characterId ?? string.Empty : string.Empty;
        if (!force &&
            currentPoints == lastCurrentPoints &&
            maxPoints == lastMaxPoints &&
            string.Equals(unitId, lastUnitId, StringComparison.Ordinal))
        {
            return;
        }

        lastCurrentPoints = currentPoints;
        lastMaxPoints = maxPoints;
        lastUnitId = unitId;

        int visibleCount = Mathf.Min(currentPoints, actionPointIndicators.Count);
        int usableCount = Mathf.Min(maxPoints, actionPointIndicators.Count);
        for (int i = 0; i < actionPointIndicators.Count; i++)
        {
            GameObject indicator = actionPointIndicators[i];
            if (indicator == null)
            {
                continue;
            }

            bool shouldShow = i < visibleCount && i < usableCount;
            if (indicator.activeSelf != shouldShow)
            {
                indicator.SetActive(shouldShow);
            }
        }
    }

    private static GameObject FindIndicatorObject(Transform pointSlot)
    {
        if (pointSlot == null)
        {
            return null;
        }

        for (int i = 0; i < pointSlot.childCount; i++)
        {
            Transform child = pointSlot.GetChild(i);
            if (child == null)
            {
                continue;
            }

            if (child.name.IndexOf("图片", StringComparison.OrdinalIgnoreCase) >= 0 ||
                child.name.IndexOf("行动点", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return child.gameObject;
            }
        }

        Image image = pointSlot.GetComponentInChildren<Image>(true);
        return image != null ? image.gameObject : null;
    }

    private static Transform FindTransformByPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        string[] segments = path.Split('/');
        if (segments.Length == 0)
        {
            return null;
        }

        GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
        Transform current = null;
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i] != null && string.Equals(roots[i].name, segments[0], StringComparison.Ordinal))
            {
                current = roots[i].transform;
                break;
            }
        }

        if (current == null)
        {
            return null;
        }

        for (int i = 1; i < segments.Length; i++)
        {
            current = FindChildByName(current, segments[i]);
            if (current == null)
            {
                return null;
            }
        }

        return current;
    }

    private static Transform FindChildByName(Transform parent, string childName)
    {
        if (parent == null)
        {
            return null;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child != null && string.Equals(child.name, childName, StringComparison.Ordinal))
            {
                return child;
            }
        }

        return null;
    }
}

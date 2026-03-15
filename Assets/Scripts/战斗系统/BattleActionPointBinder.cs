using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class BattleActionPointBinder : MonoBehaviour
{
    private const string ActionPointPanelPath = "Canvas/\u4e0b\u65b9\u680f\u4f4d/\u89d2\u8272\u64cd\u4f5c\u680f/\u89d2\u8272\u680f/\u884c\u52a8\u529b\u9762\u677f";

    private readonly List<GameObject> actionPointIndicators = new List<GameObject>();
    private BattleTurnSystem turnSystem;
    private int lastCurrentPoints = int.MinValue;
    private int lastMaxPoints = int.MinValue;
    private string lastUnitId = string.Empty;
    private Transform actionPointPanel;

    public void Initialize(BattleTurnSystem system)
    {
        turnSystem = system;
        actionPointPanel = ResolveActionPointPanel();
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

        Transform panel = actionPointPanel != null ? actionPointPanel : ResolveActionPointPanel();
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

    private static Transform ResolveActionPointPanel()
    {
        BattleSceneBindings bindings = BattleSceneBindings.FindInActiveScene();
        if (bindings != null && bindings.actionPointPanel != null)
        {
            return bindings.actionPointPanel;
        }

        return FindTransformByPath(ActionPointPanelPath);
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

            if (child.name.IndexOf("\u56fe", StringComparison.OrdinalIgnoreCase) >= 0 ||
                child.name.IndexOf("\u884c\u52a8\u70b9", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return child.gameObject;
            }
        }

        Image image = pointSlot.GetComponentInChildren<Image>(true);
        return image != null ? image.gameObject : null;
    }

    private static Transform FindTransformByPath(string path)
    {
        return SceneHierarchyPathUtility.FindInActiveScene(path);
    }
}

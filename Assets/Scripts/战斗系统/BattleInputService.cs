using System;
using UnityEngine;
using UnityEngine.EventSystems;

internal sealed class BattleInputService
{
    public void HandleCombatInput(
        BattleGrid grid,
        Camera battleCamera,
        BattleUnit activeUnit,
        bool isSkillModeActive,
        Action clearActiveSkillMode,
        Action refreshHighlights,
        Action clearLockedTargetUnit,
        Action<BattleUnit> setLockedTargetUnit,
        Action<BattleUnit, Vector2Int, BattleUnit> tryUseActiveSkill)
    {
        if (isSkillModeActive && Input.GetMouseButtonDown(1))
        {
            clearActiveSkillMode?.Invoke();
            refreshHighlights?.Invoke();
            return;
        }

        if (!isSkillModeActive && Input.GetMouseButtonDown(1))
        {
            clearLockedTargetUnit?.Invoke();
            return;
        }

        if (!Input.GetMouseButtonDown(0) || IsPointerBlockedByUi() || grid == null || battleCamera == null)
        {
            return;
        }

        Plane clickPlane = grid.GetInteractionPlane();
        Ray ray = battleCamera.ScreenPointToRay(Input.mousePosition);
        float enter;
        if (!clickPlane.Raycast(ray, out enter))
        {
            return;
        }

        Vector3 hitPoint = ray.GetPoint(enter);
        Vector2Int clickedCell = grid.WorldToCell(hitPoint);
        if (!grid.IsInside(clickedCell))
        {
            return;
        }

        BattleUnit target = grid.GetUnitAt(clickedCell);
        if (isSkillModeActive)
        {
            tryUseActiveSkill?.Invoke(activeUnit, clickedCell, target);
            return;
        }

        if (target != null && target.IsAlive)
        {
            setLockedTargetUnit?.Invoke(target);
        }
    }

    public void HandleExplorationInput(
        string activeExplorationActionId,
        BattleGrid grid,
        Camera battleCamera,
        BattleUnit activeUnit,
        Action<BattleUnit, Vector2Int> tryMoveFreely)
    {
        if (!string.Equals(activeExplorationActionId, BattleTurnSystem.ExplorationMoveSkillId, StringComparison.Ordinal))
        {
            return;
        }

        if (!Input.GetMouseButtonDown(0) || IsPointerBlockedByUi() || grid == null || battleCamera == null)
        {
            return;
        }

        Plane clickPlane = grid.GetInteractionPlane();
        Ray ray = battleCamera.ScreenPointToRay(Input.mousePosition);
        float enter;
        if (!clickPlane.Raycast(ray, out enter))
        {
            return;
        }

        Vector3 hitPoint = ray.GetPoint(enter);
        Vector2Int clickedCell = grid.WorldToCell(hitPoint);
        if (!grid.IsInside(clickedCell))
        {
            return;
        }

        BattleUnit target = grid.GetUnitAt(clickedCell);
        if (target != null && target != activeUnit && target.team != activeUnit.team)
        {
            return;
        }

        tryMoveFreely?.Invoke(activeUnit, clickedCell);
    }

    public static bool IsPointerBlockedByUi()
    {
        EventSystem eventSystem = EventSystem.current;
        return eventSystem != null && eventSystem.IsPointerOverGameObject();
    }
}

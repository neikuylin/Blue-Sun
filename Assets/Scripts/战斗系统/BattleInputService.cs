using System;
using UnityEngine;
using UnityEngine.UI;

internal sealed class BattleInputService
{
    private static readonly Image[] EmptyUiBlockerImages = new Image[0];
    private static Image[] cachedUiBlockerImages = EmptyUiBlockerImages;
    private static int cachedUiBlockerFrame = -1;

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
        Func<BattleUnit, Vector2Int, bool> tryMoveFreely)
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
        RefreshUiBlockerImageCacheIfNeeded();

        Vector2 pointerPosition = Input.mousePosition;
        for (int i = cachedUiBlockerImages.Length - 1; i >= 0; i--)
        {
            Image image = cachedUiBlockerImages[i];
            if (!IsVisibleUiBlockerImage(image))
            {
                continue;
            }

            Camera eventCamera = ResolveUiEventCamera(image);
            if (RectTransformUtility.RectangleContainsScreenPoint(image.rectTransform, pointerPosition, eventCamera))
            {
                return true;
            }
        }

        return false;
    }

    private static void RefreshUiBlockerImageCacheIfNeeded()
    {
        if (cachedUiBlockerFrame == Time.frameCount)
        {
            return;
        }

        cachedUiBlockerFrame = Time.frameCount;
        cachedUiBlockerImages = UnityEngine.Object.FindObjectsOfType<Image>(false) ?? EmptyUiBlockerImages;
    }

    private static bool IsVisibleUiBlockerImage(Image image)
    {
        return image != null &&
            image.enabled &&
            image.isActiveAndEnabled &&
            image.gameObject.activeInHierarchy &&
            image.color.a > 0.001f &&
            HasVisibleCanvasGroupAlpha(image.transform);
    }

    private static bool HasVisibleCanvasGroupAlpha(Transform transform)
    {
        Transform current = transform;
        while (current != null)
        {
            CanvasGroup canvasGroup = current.GetComponent<CanvasGroup>();
            if (canvasGroup != null && canvasGroup.alpha <= 0.001f)
            {
                return false;
            }

            current = current.parent;
        }

        return true;
    }

    private static Camera ResolveUiEventCamera(Image image)
    {
        Canvas canvas = image != null ? image.canvas : null;
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        return canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
    }
}

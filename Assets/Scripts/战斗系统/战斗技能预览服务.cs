using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

internal sealed class 战斗技能预览服务
{
    private RectTransform overlayCanvasRect;
    private RectTransform skillCostHintRect;
    private TMP_Text skillCostHintText;
    private BattleUnit hoveredSkillTarget;
    private readonly List<BattleUnit> hoveredSkillTargets = new List<BattleUnit>();

    public BattleUnit HoveredSkillTarget
    {
        get { return hoveredSkillTarget; }
    }

    public void 重置状态()
    {
        overlayCanvasRect = null;
        skillCostHintRect = null;
        skillCostHintText = null;
        hoveredSkillTarget = null;
        hoveredSkillTargets.Clear();
    }

    public void 设置行动点提示文本(TMP_Text hintText)
    {
        skillCostHintText = hintText;
        skillCostHintRect = hintText != null ? hintText.rectTransform : null;
        overlayCanvasRect = skillCostHintRect != null ? skillCostHintRect.GetComponentInParent<Canvas>()?.transform as RectTransform : null;

        if (skillCostHintText != null)
        {
            skillCostHintText.raycastTarget = false;
            skillCostHintText.text = string.Empty;
            skillCostHintText.gameObject.SetActive(false);
        }
    }

    public void 更新悬停目标(
        BattleGrid grid,
        Camera battleCamera,
        BattleUnit activeUnit,
        BattleSkillDatabase.SkillEntry activeSkill,
        bool isSkillModeActive,
        Func<bool> isPointerBlockedByUi,
        Func<BattleUnit, Vector2Int, BattleUnit, BattleSkillDatabase.SkillEntry, List<BattleUnit>> collectHoveredSkillTargets)
    {
        if (!isSkillModeActive || activeSkill == null || activeUnit == null)
        {
            清空悬停目标(grid);
            return;
        }

        if (grid == null || battleCamera == null || isPointerBlockedByUi == null || collectHoveredSkillTargets == null || isPointerBlockedByUi())
        {
            清空悬停目标(grid);
            return;
        }

        Plane clickPlane = grid.GetInteractionPlane();
        Ray ray = battleCamera.ScreenPointToRay(Input.mousePosition);
        float enter;
        if (!clickPlane.Raycast(ray, out enter))
        {
            清空悬停目标(grid);
            return;
        }

        Vector3 hitPoint = ray.GetPoint(enter);
        Vector2Int hoveredCell = grid.WorldToCell(hitPoint);
        if (!grid.IsInside(hoveredCell))
        {
            清空悬停目标(grid);
            return;
        }

        BattleUnit directTarget = grid.GetUnitAt(hoveredCell);
        List<BattleUnit> nextTargets = collectHoveredSkillTargets(activeUnit, hoveredCell, directTarget, activeSkill);
        if (nextTargets == null || nextTargets.Count == 0)
        {
            清空悬停目标(grid);
            return;
        }

        应用悬停目标(nextTargets, directTarget);
    }

    public void 更新悬停目标闪烁(
        BattleGrid grid,
        float currentTime,
        Color hoveredEnemyFlashColor,
        Color hoveredAllyFlashColor)
    {
        if (grid == null)
        {
            return;
        }

        if (hoveredSkillTargets.Count == 0)
        {
            grid.ClearHoveredFootprint();
            return;
        }

        float pulse = 0.5f + 0.5f * Mathf.Sin(currentTime * 10f);
        应用悬停目标覆盖(grid, pulse, hoveredEnemyFlashColor, hoveredAllyFlashColor);
        for (int i = 0; i < hoveredSkillTargets.Count; i++)
        {
            BattleUnit target = hoveredSkillTargets[i];
            if (target == null || !target.IsAlive)
            {
                continue;
            }

            Color targetFlashColor = 解析悬停闪烁颜色(target, hoveredEnemyFlashColor, hoveredAllyFlashColor);
            target.ApplyTint(targetFlashColor, Mathf.Lerp(0.2f, 0.75f, pulse));
        }
    }

    public void 清空悬停目标(BattleGrid grid)
    {
        for (int i = 0; i < hoveredSkillTargets.Count; i++)
        {
            BattleUnit target = hoveredSkillTargets[i];
            if (target != null)
            {
                target.ClearTint();
            }
        }

        hoveredSkillTargets.Clear();
        hoveredSkillTarget = null;
        grid?.ClearHoveredFootprint();
    }

    public void 更新行动点提示(
        bool isSkillModeActive,
        bool skillHoverValid,
        int skillHoverActionPointCost,
        BattleUnit activeUnit,
        Color insufficientColor,
        Color normalColor,
        Func<Transform> resolveOverlayCanvasTransform,
        Func<Transform, string, Transform> findChildByName)
    {
        if (!应该显示行动点提示(isSkillModeActive, skillHoverValid, skillHoverActionPointCost))
        {
            隐藏行动点提示();
            return;
        }

        TMP_Text hint = 确保行动点提示(resolveOverlayCanvasTransform, findChildByName, normalColor);
        RectTransform canvasRect = overlayCanvasRect;
        if (hint == null || canvasRect == null || skillCostHintRect == null)
        {
            return;
        }

        hint.text = "消耗行动点：" + skillHoverActionPointCost;
        hint.color = activeUnit != null && skillHoverActionPointCost > activeUnit.currentActionPoints
            ? insufficientColor
            : normalColor;
        hint.gameObject.SetActive(true);

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, Input.mousePosition, null, out localPoint);
        skillCostHintRect.anchoredPosition = localPoint + new Vector2(90f, -28f);
    }

    public void 隐藏行动点提示()
    {
        if (skillCostHintText != null)
        {
            skillCostHintText.gameObject.SetActive(false);
        }
    }

    private void 应用悬停目标(List<BattleUnit> nextTargets, BattleUnit directTarget)
    {
        for (int i = 0; i < hoveredSkillTargets.Count; i++)
        {
            BattleUnit previous = hoveredSkillTargets[i];
            if (previous != null && !nextTargets.Contains(previous))
            {
                previous.ClearTint();
            }
        }

        hoveredSkillTargets.Clear();
        hoveredSkillTargets.AddRange(nextTargets);
        hoveredSkillTarget = directTarget != null && nextTargets.Contains(directTarget)
            ? directTarget
            : nextTargets[0];
    }

    private void 应用悬停目标覆盖(
        BattleGrid grid,
        float pulse,
        Color hoveredEnemyFlashColor,
        Color hoveredAllyFlashColor)
    {
        HashSet<Vector2Int> hoveredCells = new HashSet<Vector2Int>();
        Color overlayColor = 解析悬停闪烁颜色(hoveredSkillTarget, hoveredEnemyFlashColor, hoveredAllyFlashColor);
        overlayColor.a = Mathf.Lerp(0.18f, overlayColor.a, pulse);

        for (int i = 0; i < hoveredSkillTargets.Count; i++)
        {
            BattleUnit target = hoveredSkillTargets[i];
            if (target == null || !target.IsAlive)
            {
                continue;
            }

            int radius = target.FootprintRadius;
            for (int y = target.currentCell.y - radius; y <= target.currentCell.y + radius; y++)
            {
                for (int x = target.currentCell.x - radius; x <= target.currentCell.x + radius; x++)
                {
                    Vector2Int cell = new Vector2Int(x, y);
                    if (grid.IsInside(cell))
                    {
                        hoveredCells.Add(cell);
                    }
                }
            }
        }

        grid.SetHoveredFootprint(hoveredCells, overlayColor);
    }

    private static Color 解析悬停闪烁颜色(BattleUnit target, Color hoveredEnemyFlashColor, Color hoveredAllyFlashColor)
    {
        if (target == null)
        {
            return hoveredEnemyFlashColor;
        }

        return target.team == BattleTeam.Player
            ? hoveredAllyFlashColor
            : hoveredEnemyFlashColor;
    }

    private static bool 应该显示行动点提示(bool isSkillModeActive, bool skillHoverValid, int skillHoverActionPointCost)
    {
        return isSkillModeActive && skillHoverValid && skillHoverActionPointCost > 0;
    }

    private TMP_Text 确保行动点提示(
        Func<Transform> resolveOverlayCanvasTransform,
        Func<Transform, string, Transform> findChildByName,
        Color normalColor)
    {
        if (skillCostHintText != null && overlayCanvasRect != null)
        {
            return skillCostHintText;
        }

        Transform canvasTransform = resolveOverlayCanvasTransform != null ? resolveOverlayCanvasTransform() : null;
        if (canvasTransform == null)
        {
            return null;
        }

        overlayCanvasRect = canvasTransform as RectTransform;
        if (overlayCanvasRect == null)
        {
            return null;
        }

        Transform existing = findChildByName != null ? findChildByName(canvasTransform, "SkillCostHint") : null;
        if (existing == null)
        {
            GameObject hintObject = new GameObject("SkillCostHint", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            existing = hintObject.transform;
            existing.SetParent(canvasTransform, false);
        }

        skillCostHintRect = existing as RectTransform;
        skillCostHintText = existing.GetComponent<TMP_Text>();
        if (skillCostHintRect == null || skillCostHintText == null)
        {
            return null;
        }

        skillCostHintRect.anchorMin = new Vector2(0.5f, 0.5f);
        skillCostHintRect.anchorMax = new Vector2(0.5f, 0.5f);
        skillCostHintRect.pivot = new Vector2(0f, 0.5f);
        skillCostHintRect.sizeDelta = new Vector2(260f, 44f);

        skillCostHintText.raycastTarget = false;
        skillCostHintText.fontSize = 28f;
        skillCostHintText.alignment = TextAlignmentOptions.Left;
        skillCostHintText.color = normalColor;
        skillCostHintText.text = string.Empty;
        skillCostHintText.gameObject.SetActive(false);
        return skillCostHintText;
    }
}

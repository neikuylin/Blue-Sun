using System;
using System.Collections.Generic;
using UnityEngine;

internal sealed class 战斗技能区域预览服务
{
    private BattleGrid grid;
    private Func<BattleSkillDatabase.SkillEntry, bool> usesContinuousCircularArea;
    private Func<BattleSkillDatabase.SkillEntry, bool> isCircularAxisAreaSkill;
    private Func<BattleSkillDatabase.SkillEntry, float> getContinuousAreaRadiusWorld;
    private Func<BattleUnit, Vector2Int, Vector3> resolveAxisDirectionWorld;
    private Func<BattleUnit, BattleSkillDatabase.SkillEntry, float> getAxisRangeWorld;
    private Func<BattleSkillDatabase.SkillEntry, float> getAxisWidthWorld;
    private Func<BattleUnit, Vector2Int, BattleSkillDatabase.SkillEntry, HashSet<Vector2Int>> collectAreaEffectCells;

    public void 初始化(
        BattleGrid battleGrid,
        Func<BattleSkillDatabase.SkillEntry, bool> continuousCircularAreaChecker,
        Func<BattleSkillDatabase.SkillEntry, bool> circularAxisAreaChecker,
        Func<BattleSkillDatabase.SkillEntry, float> continuousAreaRadiusResolver,
        Func<BattleUnit, Vector2Int, Vector3> axisDirectionResolver,
        Func<BattleUnit, BattleSkillDatabase.SkillEntry, float> axisRangeWorldResolver,
        Func<BattleSkillDatabase.SkillEntry, float> axisWidthWorldResolver,
        Func<BattleUnit, Vector2Int, BattleSkillDatabase.SkillEntry, HashSet<Vector2Int>> areaEffectCellsCollector)
    {
        grid = battleGrid;
        usesContinuousCircularArea = continuousCircularAreaChecker;
        isCircularAxisAreaSkill = circularAxisAreaChecker;
        getContinuousAreaRadiusWorld = continuousAreaRadiusResolver;
        resolveAxisDirectionWorld = axisDirectionResolver;
        getAxisRangeWorld = axisRangeWorldResolver;
        getAxisWidthWorld = axisWidthWorldResolver;
        collectAreaEffectCells = areaEffectCellsCollector;
    }

    public void 应用技能悬停预览(
        BattleUnit activeUnit,
        BattleSkillDatabase.SkillEntry activeSkill,
        Vector2Int skillHoverCell,
        bool hasSkillHoverPreview,
        bool skillHoverHasAnyVisibleCells,
        bool skillHoverValid,
        bool isMovementSkillActive,
        Color validColor,
        Color invalidColor)
    {
        if (grid == null || !hasSkillHoverPreview || !skillHoverHasAnyVisibleCells || activeUnit == null || activeSkill == null)
        {
            return;
        }

        if (usesContinuousCircularArea != null && usesContinuousCircularArea(activeSkill))
        {
            if (isMovementSkillActive)
            {
                Color movePreviewColor = skillHoverValid ? validColor : invalidColor;
                movePreviewColor.a = skillHoverValid ? 0.24f : 0.20f;
                grid.HighlightFootprintAt(skillHoverCell, activeUnit.footprintSize, movePreviewColor);
                return;
            }

            Color previewColor = skillHoverValid ? validColor : invalidColor;
            previewColor.a = skillHoverValid ? 0.18f : 0.16f;
            float radiusWorld = getContinuousAreaRadiusWorld != null
                ? getContinuousAreaRadiusWorld(activeSkill)
                : 0f;
            grid.HighlightCircleAt(skillHoverCell, radiusWorld, previewColor);
            grid.HighlightFootprintAt(skillHoverCell, activeUnit.footprintSize, previewColor);
            return;
        }

        if (isCircularAxisAreaSkill != null && isCircularAxisAreaSkill(activeSkill))
        {
            Color previewColor = skillHoverValid ? validColor : invalidColor;
            previewColor.a = skillHoverValid ? 0.18f : 0.16f;

            Vector3 origin = grid.GetWorldPosition(activeUnit.currentCell);
            Vector3 direction = resolveAxisDirectionWorld != null
                ? resolveAxisDirectionWorld(activeUnit, skillHoverCell)
                : Vector3.right;
            float rangeWorld = getAxisRangeWorld != null
                ? getAxisRangeWorld(activeUnit, activeSkill)
                : 0f;

            if (activeSkill.circularAxisAreaType == BattleSkillDatabase.CircularAxisAreaType.Fan)
            {
                grid.HighlightAxisFan(origin, direction, rangeWorld, activeSkill.axisAngle, previewColor);
            }
            else
            {
                float widthWorld = getAxisWidthWorld != null ? getAxisWidthWorld(activeSkill) : 0f;
                grid.HighlightAxisRay(origin, direction, rangeWorld, widthWorld, previewColor);
            }

            return;
        }

        HashSet<Vector2Int> previewCells = 收集可见区域效果格(activeUnit, skillHoverCell, activeSkill);
        if (previewCells == null || previewCells.Count == 0)
        {
            return;
        }

        if (skillHoverValid)
        {
            Color previewColor = validColor;
            previewColor.a = 0.18f;
            grid.HighlightCells(previewCells, previewColor);
            return;
        }

        Color partialColor = invalidColor;
        partialColor.a = 0.16f;
        grid.HighlightPartialCells(previewCells, activeUnit, partialColor);
    }

    public bool 是否存在可见技能预览格(
        BattleUnit activeUnit,
        BattleSkillDatabase.SkillEntry activeSkill,
        Vector2Int centerCell,
        Func<BattleSkillDatabase.SkillEntry, bool> shouldShowSkillAreaPreview)
    {
        if (grid == null || activeUnit == null || activeSkill == null || shouldShowSkillAreaPreview == null || !shouldShowSkillAreaPreview(activeSkill))
        {
            return false;
        }

        if ((usesContinuousCircularArea != null && usesContinuousCircularArea(activeSkill)) ||
            (isCircularAxisAreaSkill != null && isCircularAxisAreaSkill(activeSkill)))
        {
            return grid.IsInside(centerCell);
        }

        HashSet<Vector2Int> visibleCells = 收集可见区域效果格(activeUnit, centerCell, activeSkill);
        return visibleCells != null && visibleCells.Count > 0;
    }

    private HashSet<Vector2Int> 收集可见区域效果格(
        BattleUnit caster,
        Vector2Int centerCell,
        BattleSkillDatabase.SkillEntry skill)
    {
        HashSet<Vector2Int> cells = collectAreaEffectCells != null
            ? collectAreaEffectCells(caster, centerCell, skill)
            : null;
        if (cells == null || cells.Count == 0 || grid == null)
        {
            return cells ?? new HashSet<Vector2Int>();
        }

        HashSet<Vector2Int> visibleCells = new HashSet<Vector2Int>();
        foreach (Vector2Int cell in cells)
        {
            BattleUnit occupant = grid.GetUnitAt(cell);
            if (occupant != null && occupant != caster)
            {
                continue;
            }

            visibleCells.Add(cell);
        }

        return visibleCells;
    }
}

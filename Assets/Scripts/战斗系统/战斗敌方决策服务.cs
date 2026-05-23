using System;
using System.Collections.Generic;
using UnityEngine;

internal sealed class 战斗敌方决策服务
{
    private readonly 战斗可达格服务 可达格服务 = new 战斗可达格服务();

    public List<战斗敌方回合服务.技能选项> 构建技能选项(BattleUnit caster, string normalAttackSkillId, Func<string, BattleSkillDatabase.SkillEntry> resolveSkill)
    {
        List<战斗敌方回合服务.技能选项> result = new List<战斗敌方回合服务.技能选项>();
        if (caster == null)
        {
            return result;
        }

        HashSet<string> seenSkillIds = new HashSet<string>(StringComparer.Ordinal);
        CharacterSkillLoadoutDatabase.CharacterSkillEntry skillEntry =
            CharacterSkillRuntimeState.GetEntry(caster.characterId, createIfMissing: false);

        if (skillEntry != null && skillEntry.memorizedSkillIds != null)
        {
            CharacterSkillLoadoutDatabase.EnsureMemorizedSlotCapacity(skillEntry, skillEntry.memorizedSkillIds.Count);
            for (int i = 0; i < skillEntry.memorizedSkillIds.Count; i++)
            {
                尝试添加技能选项(
                    result,
                    seenSkillIds,
                    skillEntry.memorizedSkillIds[i],
                    string.Empty,
                    CharacterSkillLoadoutDatabase.GetMemorizedSkillWeightAt(skillEntry, i),
                    i,
                    resolveSkill);
            }
        }

        List<装备数值服务.授予技能条目> grantedSkills = InventoryShortcutRuntimeBinder.GetGrantedSkillEntriesForCharacter(caster.characterId);
        for (int i = 0; i < grantedSkills.Count; i++)
        {
            装备数值服务.授予技能条目 grantedSkill = grantedSkills[i];
            尝试添加技能选项(result, seenSkillIds, grantedSkill.技能ID, grantedSkill.来源物品ID, 0, 1000 + i, resolveSkill);
        }

        尝试添加技能选项(result, seenSkillIds, normalAttackSkillId, string.Empty, 0, int.MaxValue, resolveSkill);
        result.Sort(比较技能选项);
        return result;
    }

    public 战斗敌方回合服务.技能动作? 尝试查找技能动作(
        BattleUnit caster,
        IEnumerable<BattleUnit> units,
        List<战斗敌方回合服务.技能选项> skillChoices,
        BattleGrid grid,
        Func<BattleUnit, 战斗敌方回合服务.技能选项, bool> canEnemyUseSkill,
        Func<BattleUnit, BattleUnit, BattleSkillDatabase.SkillEntry, bool> isValidEnemySkillTarget,
        Func<BattleUnit, Vector2Int, BattleUnit, BattleSkillDatabase.SkillEntry, bool> canEnemyCastSkillAt)
    {
        if (caster == null || skillChoices == null)
        {
            return null;
        }

        for (int i = 0; i < skillChoices.Count; i++)
        {
            战斗敌方回合服务.技能选项 choice = skillChoices[i];
            if (canEnemyUseSkill == null || !canEnemyUseSkill(caster, choice))
            {
                continue;
            }

            战斗敌方回合服务.技能动作? action = 尝试为技能查找动作(
                caster,
                units,
                choice,
                grid,
                isValidEnemySkillTarget,
                canEnemyCastSkillAt);
            if (action.HasValue)
            {
                return action;
            }
        }

        return null;
    }

    public float? 尝试向技能范围移动(
        BattleUnit caster,
        IEnumerable<BattleUnit> units,
        List<战斗敌方回合服务.技能选项> skillChoices,
        BattleGrid grid,
        Func<string, BattleSkillDatabase.SkillEntry> resolveSkill,
        Func<BattleUnit, BattleSkillDatabase.SkillEntry, int> getMoveMaxRange,
        Func<BattleSkillDatabase.SkillEntry, int> getSkillManaCost,
        Func<BattleUnit, List<Vector2Int>, BattleSkillDatabase.SkillEntry, int> getMoveActionPointCost,
        Func<BattleSkillDatabase.SkillEntry, int> getSkillActionPointCost,
        Func<BattleUnit, 战斗敌方回合服务.技能选项, bool> canEnemyUseSkill,
        Func<BattleUnit, BattleUnit, BattleSkillDatabase.SkillEntry, bool> isValidEnemySkillTarget,
        Func<BattleUnit, Vector2Int, BattleUnit, BattleSkillDatabase.SkillEntry, bool> canEnemyCastSkillFromCell,
        Func<BattleUnit, BattleUnit> findClosestLivingOpponent,
        Func<BattleUnit, BattleUnit, int, Vector2Int> findBestStepToward,
        Func<BattleSkillDatabase.SkillEntry, BattleUnit, int> getSkillRange,
        Func<BattleUnit, Vector2Int, float?> tryMoveEnemyToCell,
        Action<BattleUnit, BattleUnit> faceTowardTarget)
    {
        if (caster == null || grid == null || skillChoices == null || skillChoices.Count == 0)
        {
            return null;
        }

        BattleSkillDatabase.SkillEntry moveSkill = resolveSkill != null ? resolveSkill(BattleSkillDatabase.MoveSkillId) : null;
        int maxMoveRange = getMoveMaxRange != null ? getMoveMaxRange(caster, moveSkill) : 0;
        int moveManaCost = getSkillManaCost != null ? getSkillManaCost(moveSkill) : 0;
        if (maxMoveRange <= 0 || !caster.CanSpendMana(moveManaCost))
        {
            return null;
        }

        List<战斗可达格服务.可达格> reachableCells = 可达格服务.收集可达格(
            grid,
            caster,
            maxMoveRange,
            ignoreAlliedOccupants: false);

        Vector2Int bestCell = caster.currentCell;
        BattleUnit bestTarget = null;
        int bestWeight = int.MinValue;
        int bestDistanceAfterMove = int.MaxValue;
        int bestPathLength = int.MaxValue;

        for (int skillIndex = 0; skillIndex < skillChoices.Count; skillIndex++)
        {
            战斗敌方回合服务.技能选项 choice = skillChoices[skillIndex];
            if (canEnemyUseSkill == null || !canEnemyUseSkill(caster, choice))
            {
                continue;
            }

            foreach (BattleUnit unit in units)
            {
                if (isValidEnemySkillTarget == null || !isValidEnemySkillTarget(caster, unit, choice.skill))
                {
                    continue;
                }

                for (int candidateIndex = 0; candidateIndex < reachableCells.Count; candidateIndex++)
                {
                    战斗可达格服务.可达格 reachableCell = reachableCells[candidateIndex];
                    if (reachableCell == null || reachableCell.path == null || reachableCell.path.Count <= 1)
                    {
                        continue;
                    }

                    int moveActionPointCost = getMoveActionPointCost != null ? getMoveActionPointCost(caster, reachableCell.path, moveSkill) : 0;
                    if (!caster.CanSpendActionPoints(moveActionPointCost))
                    {
                        continue;
                    }

                    if (caster.currentActionPoints - moveActionPointCost < (getSkillActionPointCost != null ? getSkillActionPointCost(choice.skill) : 0))
                    {
                        continue;
                    }

                    if (caster.currentMana - moveManaCost < (getSkillManaCost != null ? getSkillManaCost(choice.skill) : 0))
                    {
                        continue;
                    }

                    if (canEnemyCastSkillFromCell == null || !canEnemyCastSkillFromCell(caster, reachableCell.cell, unit, choice.skill))
                    {
                        continue;
                    }

                    int distanceAfterMove = grid.ManhattanDistance(reachableCell.cell, unit.currentCell);
                    if (choice.weight > bestWeight ||
                        (choice.weight == bestWeight && distanceAfterMove < bestDistanceAfterMove) ||
                        (choice.weight == bestWeight && distanceAfterMove == bestDistanceAfterMove && reachableCell.stepCount < bestPathLength))
                    {
                        bestWeight = choice.weight;
                        bestDistanceAfterMove = distanceAfterMove;
                        bestPathLength = reachableCell.stepCount;
                        bestCell = reachableCell.cell;
                        bestTarget = unit;
                    }
                }
            }
        }

        if (bestTarget == null)
        {
            BattleUnit closestTarget = findClosestLivingOpponent != null ? findClosestLivingOpponent(caster) : null;
            if (closestTarget == null)
            {
                return null;
            }

            bestCell = findBestStepToward != null
                ? findBestStepToward(caster, closestTarget, getSkillRange != null ? getSkillRange(skillChoices[0].skill, caster) : 0)
                : caster.currentCell;
            bestTarget = closestTarget;
            if (bestCell == caster.currentCell)
            {
                return null;
            }
        }

        float? moveDuration = tryMoveEnemyToCell != null
            ? tryMoveEnemyToCell(caster, bestCell)
            : null;
        if (moveDuration.HasValue && bestTarget != null)
        {
            faceTowardTarget?.Invoke(caster, bestTarget);
        }

        return moveDuration;
    }

    public static int 比较技能选项(战斗敌方回合服务.技能选项 left, 战斗敌方回合服务.技能选项 right)
    {
        if (ReferenceEquals(left, right))
        {
            return 0;
        }

        if (left == null)
        {
            return 1;
        }

        if (right == null)
        {
            return -1;
        }

        int weightCompare = right.weight.CompareTo(left.weight);
        if (weightCompare != 0)
        {
            return weightCompare;
        }

        return left.order.CompareTo(right.order);
    }

    private static void 尝试添加技能选项(
        List<战斗敌方回合服务.技能选项> choices,
        HashSet<string> seenSkillIds,
        string skillId,
        string skillSource,
        int weight,
        int order,
        Func<string, BattleSkillDatabase.SkillEntry> resolveSkill)
    {
        if (choices == null || seenSkillIds == null || string.IsNullOrWhiteSpace(skillId))
        {
            return;
        }

        BattleSkillDatabase.SkillEntry skill = resolveSkill != null ? resolveSkill(skillId) : null;
        if (skill == null)
        {
            return;
        }

        string resolvedSource = BattleSkillDatabase.ResolveSkillSource(skillSource, skill);
        string key = $"{skillId}|{resolvedSource}";
        if (!seenSkillIds.Add(key))
        {
            return;
        }

        choices.Add(new 战斗敌方回合服务.技能选项
        {
            skillId = skillId,
            skillSource = string.IsNullOrWhiteSpace(skillSource) || string.Equals(skillSource, BattleSkillDatabase.NoSkillSourceText, StringComparison.Ordinal)
                ? string.Empty
                : skillSource,
            weight = weight,
            order = order,
            skill = skill
        });
    }

    private static 战斗敌方回合服务.技能动作? 尝试为技能查找动作(
        BattleUnit caster,
        IEnumerable<BattleUnit> units,
        战斗敌方回合服务.技能选项 choice,
        BattleGrid grid,
        Func<BattleUnit, BattleUnit, BattleSkillDatabase.SkillEntry, bool> isValidEnemySkillTarget,
        Func<BattleUnit, Vector2Int, BattleUnit, BattleSkillDatabase.SkillEntry, bool> canEnemyCastSkillAt)
    {
        if (caster == null || choice == null || choice.skill == null || grid == null)
        {
            return null;
        }

        战斗敌方回合服务.技能动作 action = default;
        BattleUnit bestTarget = null;
        int bestDistance = int.MaxValue;
        foreach (BattleUnit unit in units)
        {
            if (isValidEnemySkillTarget == null || !isValidEnemySkillTarget(caster, unit, choice.skill))
            {
                continue;
            }

            Vector2Int targetCell = unit.currentCell;
            if (canEnemyCastSkillAt == null || !canEnemyCastSkillAt(caster, targetCell, unit, choice.skill))
            {
                continue;
            }

            int distance = grid.ManhattanDistance(caster.currentCell, unit.currentCell);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestTarget = unit;
                action = new 战斗敌方回合服务.技能动作
                {
                    choice = choice,
                    targetUnit = unit,
                    targetCell = targetCell
                };
            }
        }

        return bestTarget != null ? action : (战斗敌方回合服务.技能动作?)null;
    }
}

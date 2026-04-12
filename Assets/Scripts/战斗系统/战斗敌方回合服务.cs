using System.Collections;
using System.Collections.Generic;
using UnityEngine;

internal sealed class 战斗敌方回合服务
{
    internal sealed class 技能选项
    {
        public string skillId = string.Empty;
        public int weight;
        public int order;
        public BattleSkillDatabase.SkillEntry skill;
    }

    internal struct 技能动作
    {
        public 技能选项 choice;
        public BattleUnit targetUnit;
        public Vector2Int targetCell;
    }

    public IEnumerator 执行敌方回合(
        BattleUnit activeUnit,
        System.Func<BattleUnit, List<技能选项>> buildEnemySkillChoices,
        System.Func<BattleUnit, List<技能选项>, 技能动作?> tryFindEnemySkillAction,
        System.Func<BattleUnit, List<技能选项>, float?> tryMoveEnemyTowardSkillRange,
        System.Func<BattleUnit, 技能动作, IEnumerator> executeEnemySkillAction,
        System.Action refreshHighlights,
        System.Action endTurn)
    {
        yield return new WaitForSeconds(0.5f);

        while (activeUnit != null && activeUnit.IsAlive && activeUnit.currentActionPoints > 0)
        {
            List<技能选项> skillChoices = buildEnemySkillChoices != null
                ? buildEnemySkillChoices(activeUnit)
                : null;
            if (skillChoices == null || skillChoices.Count == 0)
            {
                break;
            }

            技能动作? action = tryFindEnemySkillAction != null
                ? tryFindEnemySkillAction(activeUnit, skillChoices)
                : null;
            if (action.HasValue)
            {
                if (executeEnemySkillAction != null)
                {
                    yield return executeEnemySkillAction(activeUnit, action.Value);
                }

                yield return new WaitForSeconds(0.1f);
                continue;
            }

            float? moveDuration = tryMoveEnemyTowardSkillRange != null
                ? tryMoveEnemyTowardSkillRange(activeUnit, skillChoices)
                : null;
            if (moveDuration.HasValue)
            {
                if (moveDuration.Value > 0f)
                {
                    yield return new WaitForSeconds(moveDuration.Value);
                }
                else
                {
                    yield return new WaitForSeconds(0.1f);
                }

                refreshHighlights?.Invoke();
                continue;
            }

            break;
        }

        endTurn?.Invoke();
    }
}

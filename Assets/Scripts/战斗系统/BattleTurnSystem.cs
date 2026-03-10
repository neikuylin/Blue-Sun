using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class BattleTurnSystem : MonoBehaviour
{
    private readonly List<BattleUnit> units = new List<BattleUnit>();

    private BattleGrid grid;
    private Camera battleCamera;
    private BattleUnit activeUnit;
    private int activeIndex = -1;
    private bool waitingForEnemyAction;
    private TMP_Text activeUnitIdText;

    public void Initialize(BattleGrid battleGrid, Camera mainCamera, IEnumerable<BattleUnit> battleUnits)
    {
        grid = battleGrid;
        battleCamera = mainCamera;
        activeUnitIdText = FindActiveUnitIdText();
        units.Clear();

        foreach (BattleUnit unit in battleUnits)
        {
            if (unit != null)
            {
                units.Add(unit);
            }
        }

        BeginNextTurn();
    }

    private void Update()
    {
        if (activeUnit == null || !activeUnit.IsAlive)
        {
            return;
        }

        if (activeUnit.team == BattleTeam.Player)
        {
            HandlePlayerInput();
            return;
        }

        if (!waitingForEnemyAction)
        {
            StartCoroutine(RunEnemyTurn());
        }
    }

    private void HandlePlayerInput()
    {
        if (!Input.GetMouseButtonDown(0))
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
        if (target != null && target.team != activeUnit.team)
        {
            TryAttack(activeUnit, target);
            return;
        }

        TryMove(activeUnit, clickedCell);
    }

    private IEnumerator RunEnemyTurn()
    {
        waitingForEnemyAction = true;
        yield return new WaitForSeconds(0.5f);

        BattleUnit target = FindClosestLivingOpponent(activeUnit);
        if (target == null)
        {
            waitingForEnemyAction = false;
            yield break;
        }

        if (grid.ManhattanDistance(activeUnit.currentCell, target.currentCell) <= activeUnit.attackRange)
        {
            ExecuteAttack(activeUnit, target);
            yield return new WaitForSeconds(0.35f);
            EndTurn();
            yield break;
        }

        Vector2Int destination = FindBestStepToward(activeUnit, target);
        if (destination != activeUnit.currentCell)
        {
            grid.MoveUnit(activeUnit, destination);
            activeUnit.FaceToward(target.transform.position);
        }

        yield return new WaitForSeconds(0.35f);

        if (target.IsAlive && grid.ManhattanDistance(activeUnit.currentCell, target.currentCell) <= activeUnit.attackRange)
        {
            ExecuteAttack(activeUnit, target);
            yield return new WaitForSeconds(0.35f);
        }

        EndTurn();
    }

    private void TryMove(BattleUnit unit, Vector2Int destination)
    {
        if (grid.ManhattanDistance(unit.currentCell, destination) > unit.moveRange)
        {
            return;
        }

        if (!grid.IsWalkable(unit, destination))
        {
            return;
        }

        grid.MoveUnit(unit, destination);
        EndTurn();
    }

    private void TryAttack(BattleUnit attacker, BattleUnit defender)
    {
        if (grid.ManhattanDistance(attacker.currentCell, defender.currentCell) > attacker.attackRange)
        {
            return;
        }

        ExecuteAttack(attacker, defender);
        EndTurn();
    }

    private void ExecuteAttack(BattleUnit attacker, BattleUnit defender)
    {
        attacker.FaceToward(defender.transform.position);
        defender.ApplyDamage(attacker.attackDamage);
        Debug.Log(attacker.unitName + " attacks " + defender.unitName + " for " + attacker.attackDamage + " damage.");

        if (!defender.IsAlive)
        {
            grid.RemoveUnit(defender);
            Debug.Log(defender.unitName + " is defeated.");
        }
    }

    private void EndTurn()
    {
        waitingForEnemyAction = false;
        BeginNextTurn();
    }

    private void BeginNextTurn()
    {
        CleanupDeadUnits();
        if (units.Count == 0)
        {
            activeUnit = null;
            return;
        }

        for (int i = 0; i < units.Count; i++)
        {
            activeIndex = (activeIndex + 1) % units.Count;
            BattleUnit candidate = units[activeIndex];
            if (candidate != null && candidate.IsAlive)
            {
                activeUnit = candidate;
                RefreshHighlights();
                RefreshActiveUnitUi();
                Debug.Log("Turn: " + activeUnit.unitName);
                return;
            }
        }

        activeUnit = null;
        RefreshActiveUnitUi();
    }

    private void RefreshHighlights()
    {
        grid.ResetHighlights();
        if (activeUnit == null)
        {
            return;
        }

        grid.HighlightReachable(activeUnit);
        grid.HighlightAttackTargets(activeUnit);
        grid.HighlightFootprint(activeUnit, new Color(1.00f, 0.90f, 0.20f, 0.60f));
    }

    private void CleanupDeadUnits()
    {
        units.RemoveAll(unit => unit == null || !unit.IsAlive);
    }

    private void RefreshActiveUnitUi()
    {
        if (activeUnitIdText == null)
        {
            activeUnitIdText = FindActiveUnitIdText();
        }

        if (activeUnitIdText == null)
        {
            return;
        }

        if (activeUnit == null)
        {
            activeUnitIdText.text = string.Empty;
            return;
        }

        activeUnitIdText.text = string.IsNullOrWhiteSpace(activeUnit.characterId) ? activeUnit.unitName : activeUnit.characterId;
    }

    private static TMP_Text FindActiveUnitIdText()
    {
        TMP_Text[] texts = Object.FindObjectsOfType<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];
            if (text != null && text.name == "ID")
            {
                return text;
            }
        }

        return null;
    }

    private BattleUnit FindClosestLivingOpponent(BattleUnit source)
    {
        BattleUnit best = null;
        int bestDistance = int.MaxValue;

        foreach (BattleUnit unit in units)
        {
            if (unit == null || !unit.IsAlive || unit.team == source.team)
            {
                continue;
            }

            int distance = grid.ManhattanDistance(source.currentCell, unit.currentCell);
            if (distance < bestDistance)
            {
                best = unit;
                bestDistance = distance;
            }
        }

        return best;
    }

    private Vector2Int FindBestStepToward(BattleUnit mover, BattleUnit target)
    {
        Vector2Int bestCell = mover.currentCell;
        int bestDistance = grid.ManhattanDistance(mover.currentCell, target.currentCell);

        for (int dx = -mover.moveRange; dx <= mover.moveRange; dx++)
        {
            for (int dy = -mover.moveRange; dy <= mover.moveRange; dy++)
            {
                Vector2Int candidate = mover.currentCell + new Vector2Int(dx, dy);
                if (grid.ManhattanDistance(mover.currentCell, candidate) > mover.moveRange)
                {
                    continue;
                }

                if (!grid.IsWalkable(mover, candidate))
                {
                    continue;
                }

                int candidateDistance = grid.ManhattanDistance(candidate, target.currentCell);
                if (candidateDistance < bestDistance)
                {
                    bestDistance = candidateDistance;
                    bestCell = candidate;
                }
            }
        }

        return bestCell;
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

internal sealed class 战斗探索移动服务
{
    private const int NormalFollowerStartDistance = 7;
    private const int NormalFollowerTargetDistance = 5;
    private const int ForcedFollowerTargetDistance = 1;
    private const int ForcedFollowerFallbackTargetDistance = 5;
    private const float NormalFollowerPollSeconds = 2f;
    private const float ForcedFollowerPollSeconds = 0.05f;

    private Coroutine explorationFollowerRoutine;
    private Coroutine explorationMoveAudioStopRoutine;
    private BattleAudioUtility.PlaybackHandle currentExplorationMoveAudioHandle;

    public void 停止全部(MonoBehaviour host)
    {
        停止跟随(host);
        停止移动音效(host);
    }

    public void 停止跟随(MonoBehaviour host)
    {
        if (host == null || explorationFollowerRoutine == null)
        {
            explorationFollowerRoutine = null;
            return;
        }

        host.StopCoroutine(explorationFollowerRoutine);
        explorationFollowerRoutine = null;
    }

    public void 停止移动音效(MonoBehaviour host)
    {
        if (host != null && explorationMoveAudioStopRoutine != null)
        {
            host.StopCoroutine(explorationMoveAudioStopRoutine);
        }

        explorationMoveAudioStopRoutine = null;
        停止追踪音频(currentExplorationMoveAudioHandle);
        currentExplorationMoveAudioHandle = null;
    }

    public bool 尝试自由移动(
        MonoBehaviour host,
        BattleUnit unit,
        Vector2Int destination,
        bool isExplorationMode,
        BattleGrid grid,
        IList<BattleUnit> units,
        Func<string, BattleUnit> findUnitByCharacterId,
        Func<string> resolveExplorationIdleStateName,
        Func<string> resolveExplorationMoveStateName,
        Func<bool> resolveExplorationMoveCompensateMotion,
        Func<AudioClip> resolveExplorationMoveSound,
        Func<GameObject> resolveExplorationMoveSoundPrefab,
        Camera battleCamera,
        Action refreshHighlights,
        bool forceFollowerFollow)
    {
        if (unit == null || grid == null)
        {
            return false;
        }

        Vector2Int resolvedDestination;
        if (!尝试解析探索移动核心格(unit, destination, grid, out resolvedDestination))
        {
            return false;
        }

        Vector2Int currentCell = unit.IsMoving ? grid.WorldToCell(unit.transform.position) : unit.currentCell;
        if (resolvedDestination == currentCell)
        {
            return false;
        }

        if (!unit.IsMoving)
        {
            List<Vector2Int> path = grid.FindPathIgnoringAllies(unit, resolvedDestination);
            if (path == null || path.Count <= 1)
            {
                return false;
            }
        }

        float originalMoveSpeed = unit.moveSpeed;
        unit.moveSpeed = Mathf.Max(0.01f, originalMoveSpeed * 0.5f);
        bool redirected = unit.IsMoving;
        float moveDuration = redirected
            ? grid.RedirectMovingUnitIgnoringAllies(unit, resolvedDestination)
            : grid.MoveUnitIgnoringAllies(unit, resolvedDestination);
        unit.moveSpeed = originalMoveSpeed;
        if (moveDuration <= 0f)
        {
            return false;
        }

        string idleStateName = resolveExplorationIdleStateName != null ? resolveExplorationIdleStateName() : string.Empty;
        播放探索移动音效(host, unit, moveDuration, resolveExplorationMoveSound, resolveExplorationMoveSoundPrefab, battleCamera);
        unit.PlayTimedAnimation(
            unit.GetMoveAnimationStateName(resolveExplorationMoveStateName != null ? resolveExplorationMoveStateName() : string.Empty),
            moveDuration,
            idleStateName,
            resolveExplorationMoveCompensateMotion != null && resolveExplorationMoveCompensateMotion());
        排队跟随移动(
            host,
            unit,
            isExplorationMode,
            grid,
            units,
            findUnitByCharacterId,
            resolveExplorationIdleStateName,
            resolveExplorationMoveStateName,
            resolveExplorationMoveCompensateMotion,
            refreshHighlights,
            forceFollowerFollow);
        refreshHighlights?.Invoke();
        return true;
    }

    public void 开始跟随移动(
        MonoBehaviour host,
        BattleUnit leaderUnit,
        bool isExplorationMode,
        BattleGrid grid,
        IList<BattleUnit> units,
        Func<string, BattleUnit> findUnitByCharacterId,
        Func<string> resolveExplorationIdleStateName,
        Func<string> resolveExplorationMoveStateName,
        Func<bool> resolveExplorationMoveCompensateMotion,
        Action refreshHighlights,
        bool forceFollowerFollow)
    {
        排队跟随移动(
            host,
            leaderUnit,
            isExplorationMode,
            grid,
            units,
            findUnitByCharacterId,
            resolveExplorationIdleStateName,
            resolveExplorationMoveStateName,
            resolveExplorationMoveCompensateMotion,
            refreshHighlights,
            forceFollowerFollow);
    }

    private static bool 尝试解析探索移动核心格(
        BattleUnit unit,
        Vector2Int clickedCell,
        BattleGrid grid,
        out Vector2Int resolvedDestination)
    {
        resolvedDestination = clickedCell;
        if (unit == null || grid == null)
        {
            return false;
        }

        Vector2Int currentCell = unit.IsMoving ? grid.WorldToCell(unit.transform.position) : unit.currentCell;
        if (是否在单位占地内(unit, currentCell, clickedCell))
        {
            resolvedDestination = currentCell;
            return true;
        }

        List<Vector2Int> exactPath;
        if (尝试评估探索移动核心格(unit, clickedCell, grid, out exactPath))
        {
            resolvedDestination = clickedCell;
            return true;
        }

        int radius = Mathf.Max(0, unit.FootprintRadius);
        bool found = false;
        int bestPathLength = int.MaxValue;
        int bestCoreDistance = int.MaxValue;

        for (int yOffset = -radius; yOffset <= radius; yOffset++)
        {
            for (int xOffset = -radius; xOffset <= radius; xOffset++)
            {
                Vector2Int candidate = new Vector2Int(clickedCell.x - xOffset, clickedCell.y - yOffset);
                if (candidate == clickedCell)
                {
                    continue;
                }

                List<Vector2Int> path;
                if (!尝试评估探索移动核心格(unit, candidate, grid, out path))
                {
                    continue;
                }

                int pathLength = path.Count - 1;
                int coreDistance = Mathf.Abs(candidate.x - clickedCell.x) + Mathf.Abs(candidate.y - clickedCell.y);
                if (found &&
                    (pathLength > bestPathLength ||
                     (pathLength == bestPathLength && coreDistance >= bestCoreDistance)))
                {
                    continue;
                }

                found = true;
                bestPathLength = pathLength;
                bestCoreDistance = coreDistance;
                resolvedDestination = candidate;
            }
        }

        return found;
    }

    private static bool 是否在单位占地内(BattleUnit unit, Vector2Int centerCell, Vector2Int targetCell)
    {
        int radius = unit != null ? Mathf.Max(0, unit.FootprintRadius) : 0;
        return targetCell.x >= centerCell.x - radius &&
            targetCell.x <= centerCell.x + radius &&
            targetCell.y >= centerCell.y - radius &&
            targetCell.y <= centerCell.y + radius;
    }

    private static bool 尝试评估探索移动核心格(
        BattleUnit unit,
        Vector2Int coreCell,
        BattleGrid grid,
        out List<Vector2Int> path)
    {
        path = null;
        if (unit == null || grid == null)
        {
            return false;
        }

        if (!grid.IsWalkableIgnoringAllies(unit, coreCell))
        {
            return false;
        }

        path = grid.FindPathIgnoringAllies(unit, coreCell);
        return path != null && path.Count > 1;
    }

    private void 排队跟随移动(
        MonoBehaviour host,
        BattleUnit leaderUnit,
        bool isExplorationMode,
        BattleGrid grid,
        IList<BattleUnit> units,
        Func<string, BattleUnit> findUnitByCharacterId,
        Func<string> resolveExplorationIdleStateName,
        Func<string> resolveExplorationMoveStateName,
        Func<bool> resolveExplorationMoveCompensateMotion,
        Action refreshHighlights,
        bool forceFollowerFollow)
    {
        if (host == null || !isExplorationMode || leaderUnit == null || !leaderUnit.IsAlive || !leaderUnit.isPlayerControlled)
        {
            return;
        }

        if (!string.Equals(leaderUnit.characterId, "玩家", StringComparison.Ordinal))
        {
            return;
        }

        if (explorationFollowerRoutine != null)
        {
            host.StopCoroutine(explorationFollowerRoutine);
            explorationFollowerRoutine = null;
        }

        explorationFollowerRoutine = host.StartCoroutine(执行跟随移动流程(
            leaderUnit,
            isExplorationMode,
            grid,
            units,
            findUnitByCharacterId,
            resolveExplorationIdleStateName,
            resolveExplorationMoveStateName,
            resolveExplorationMoveCompensateMotion,
            refreshHighlights,
            forceFollowerFollow));
    }

    private IEnumerator 执行跟随移动流程(
        BattleUnit leaderUnit,
        bool isExplorationMode,
        BattleGrid grid,
        IList<BattleUnit> units,
        Func<string, BattleUnit> findUnitByCharacterId,
        Func<string> resolveExplorationIdleStateName,
        Func<string> resolveExplorationMoveStateName,
        Func<bool> resolveExplorationMoveCompensateMotion,
        Action refreshHighlights,
        bool forceFollowerFollow)
    {
        WaitForSeconds idleDelay = new WaitForSeconds(forceFollowerFollow ? ForcedFollowerPollSeconds : NormalFollowerPollSeconds);
        int followerTargetDistance = forceFollowerFollow ? ForcedFollowerTargetDistance : NormalFollowerTargetDistance;
        int followerMaxTargetDistance = forceFollowerFollow ? ForcedFollowerFallbackTargetDistance : NormalFollowerTargetDistance;

        while (isExplorationMode && leaderUnit != null && leaderUnit.IsAlive)
        {
            bool issuedFollowerMove = false;
            bool hasPendingFollowerGap = false;
            float maxMoveDuration = 0f;
            Vector2Int leaderCell = leaderUnit.IsMoving ? grid.WorldToCell(leaderUnit.transform.position) : leaderUnit.currentCell;
            List<BattleUnit> followers = 获取跟随者顺序(leaderUnit, units, findUnitByCharacterId);
            HashSet<Vector2Int> reservedDestinations = new HashSet<Vector2Int>();
            for (int i = 0; i < followers.Count; i++)
            {
                BattleUnit follower = followers[i];
                if (follower == null || !follower.IsAlive)
                {
                    continue;
                }

                if (follower.IsMoving)
                {
                    hasPendingFollowerGap = true;
                    continue;
                }

                int followerDistance = grid.ManhattanDistance(follower.currentCell, leaderCell);
                if (!forceFollowerFollow && followerDistance <= NormalFollowerStartDistance)
                {
                    continue;
                }

                if (forceFollowerFollow && followerDistance <= followerTargetDistance)
                {
                    continue;
                }

                hasPendingFollowerGap = true;

                Vector2Int destination;
                if (!尝试查找跟随者目标格(follower, leaderCell, followerTargetDistance, followerMaxTargetDistance, reservedDestinations, grid, out destination))
                {
                    continue;
                }

                reservedDestinations.Add(destination);
                float moveDuration = 播放跟随者移动(
                    follower,
                    destination,
                    grid,
                    resolveExplorationIdleStateName,
                    resolveExplorationMoveStateName,
                    resolveExplorationMoveCompensateMotion);
                if (moveDuration > 0f)
                {
                    issuedFollowerMove = true;
                    maxMoveDuration = Mathf.Max(maxMoveDuration, moveDuration);
                }
            }

            if (issuedFollowerMove)
            {
                refreshHighlights?.Invoke();
                yield return forceFollowerFollow ? idleDelay : new WaitForSeconds(maxMoveDuration);
                continue;
            }

            if (!leaderUnit.IsMoving && !hasPendingFollowerGap && !issuedFollowerMove)
            {
                break;
            }

            yield return idleDelay;
        }

        explorationFollowerRoutine = null;
        refreshHighlights?.Invoke();
    }

    private static List<BattleUnit> 获取跟随者顺序(
        BattleUnit leaderUnit,
        IList<BattleUnit> units,
        Func<string, BattleUnit> findUnitByCharacterId)
    {
        List<BattleUnit> orderedFollowers = new List<BattleUnit>();
        HashSet<BattleUnit> added = new HashSet<BattleUnit>();
        IReadOnlyList<CharacterSelectionState.SlotSelection> slotSelections = CharacterSelectionState.SlotSelections;
        for (int i = 0; i < slotSelections.Count; i++)
        {
            CharacterSelectionState.SlotSelection slot = slotSelections[i];
            if (string.IsNullOrWhiteSpace(slot.characterId))
            {
                continue;
            }

            BattleUnit unit = findUnitByCharacterId != null ? findUnitByCharacterId(slot.characterId) : null;
            if (unit == null || unit == leaderUnit || !unit.IsAlive || !unit.isPlayerControlled || unit.team != BattleTeam.Player)
            {
                continue;
            }

            if (added.Add(unit))
            {
                orderedFollowers.Add(unit);
            }
        }

        if (units == null)
        {
            return orderedFollowers;
        }

        for (int i = 0; i < units.Count; i++)
        {
            BattleUnit unit = units[i];
            if (unit == null || unit == leaderUnit || !unit.IsAlive || !unit.isPlayerControlled || unit.team != BattleTeam.Player)
            {
                continue;
            }

            if (added.Add(unit))
            {
                orderedFollowers.Add(unit);
            }
        }

        return orderedFollowers;
    }

    private static bool 尝试查找跟随者目标格(
        BattleUnit follower,
        Vector2Int leaderCell,
        int targetDistance,
        int maxTargetDistance,
        HashSet<Vector2Int> reservedDestinations,
        BattleGrid grid,
        out Vector2Int destination)
    {
        destination = follower != null ? follower.currentCell : Vector2Int.zero;
        if (follower == null || grid == null)
        {
            return false;
        }

        int bestDistanceDelta = int.MaxValue;
        int bestPathLength = int.MaxValue;
        bool found = false;

        for (int y = 0; y < grid.height; y++)
        {
            for (int x = 0; x < grid.width; x++)
            {
                Vector2Int candidate = new Vector2Int(x, y);
                int leaderDistance = grid.ManhattanDistance(candidate, leaderCell);
                if (leaderDistance > maxTargetDistance)
                {
                    continue;
                }

                if (reservedDestinations != null && reservedDestinations.Contains(candidate))
                {
                    continue;
                }

                if (!grid.IsWalkableIgnoringAllies(follower, candidate))
                {
                    continue;
                }

                List<Vector2Int> path = grid.FindPathIgnoringAllies(follower, candidate);
                if (path == null || path.Count <= 1)
                {
                    continue;
                }

                int distanceDelta = Mathf.Abs(targetDistance - leaderDistance);
                int pathLength = path.Count - 1;
                if (distanceDelta > bestDistanceDelta)
                {
                    continue;
                }

                if (distanceDelta == bestDistanceDelta && pathLength >= bestPathLength)
                {
                    continue;
                }

                bestDistanceDelta = distanceDelta;
                bestPathLength = pathLength;
                destination = candidate;
                found = true;
            }
        }

        return found;
    }

    private static float 播放跟随者移动(
        BattleUnit unit,
        Vector2Int destination,
        BattleGrid grid,
        Func<string> resolveExplorationIdleStateName,
        Func<string> resolveExplorationMoveStateName,
        Func<bool> resolveExplorationMoveCompensateMotion)
    {
        if (unit == null || grid == null || destination == unit.currentCell)
        {
            return 0f;
        }

        float originalMoveSpeed = unit.moveSpeed;
        unit.moveSpeed = Mathf.Max(0.01f, originalMoveSpeed * 0.5f);
        float moveDuration = grid.MoveUnitIgnoringAllies(unit, destination);
        unit.moveSpeed = originalMoveSpeed;
        if (moveDuration <= 0f)
        {
            return 0f;
        }

        string idleStateName = resolveExplorationIdleStateName != null ? resolveExplorationIdleStateName() : string.Empty;
        unit.PlayTimedAnimation(
            unit.GetMoveAnimationStateName(resolveExplorationMoveStateName != null ? resolveExplorationMoveStateName() : string.Empty),
            moveDuration,
            idleStateName,
            resolveExplorationMoveCompensateMotion != null && resolveExplorationMoveCompensateMotion());
        return moveDuration;
    }

    private void 播放探索移动音效(
        MonoBehaviour host,
        BattleUnit unit,
        float duration,
        Func<AudioClip> resolveExplorationMoveSound,
        Func<GameObject> resolveExplorationMoveSoundPrefab,
        Camera battleCamera)
    {
        停止移动音效(host);
        currentExplorationMoveAudioHandle = BattleAudioUtility.StartTracked(
            resolveExplorationMoveSound != null ? resolveExplorationMoveSound() : null,
            resolveExplorationMoveSoundPrefab != null ? resolveExplorationMoveSoundPrefab() : null,
            unit,
            battleCamera);

        if (host == null || currentExplorationMoveAudioHandle == null || !currentExplorationMoveAudioHandle.IsValid)
        {
            return;
        }

        explorationMoveAudioStopRoutine = host.StartCoroutine(延迟停止探索移动音效(duration));
    }

    private IEnumerator 延迟停止探索移动音效(float delay)
    {
        yield return new WaitForSeconds(Mathf.Max(0.01f, delay));
        停止追踪音频(currentExplorationMoveAudioHandle);
        currentExplorationMoveAudioHandle = null;
        explorationMoveAudioStopRoutine = null;
    }

    private static void 停止追踪音频(BattleAudioUtility.PlaybackHandle handle)
    {
        if (handle == null)
        {
            return;
        }

        handle.Stop();
    }
}

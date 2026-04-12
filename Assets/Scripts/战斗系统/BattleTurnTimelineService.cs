using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

internal sealed class BattleTurnTimelineService
{
    private const string TimelineAnchorPath = "Canvas/上方栏位/回合时间轴";
    private const string TimelineDividerPath = "Canvas/上方栏位/时间轴分割线";

    private readonly MonoBehaviour host;
    private readonly List<GameObject> timelineInstances = new List<GameObject>();
    private readonly Dictionary<GameObject, BattleUnit> timelineInstanceUnits = new Dictionary<GameObject, BattleUnit>();
    private readonly Dictionary<GameObject, TimelineSlotKey> timelineInstanceKeys = new Dictionary<GameObject, TimelineSlotKey>();
    private readonly List<TimelineSlot> lastTimelineSlots = new List<TimelineSlot>();

    private Transform timelineAnchor;
    private Transform timelineDivider;
    private TurnTimelineButtonDatabase timelineDatabase;
    private Coroutine timelineAnimationRoutine;
    private BattleUnit timelineLeadUnit;

    public BattleTurnTimelineService(MonoBehaviour host)
    {
        this.host = host;
    }

    public void Initialize(BattleSceneBindings sceneBindings)
    {
        timelineAnchor = ResolveTimelineAnchor(sceneBindings);
        EnsureTimelineMask();
        timelineDatabase = TurnTimelineButtonDatabase.LoadDefault();
    }

    public void Refresh(
        BattleSceneBindings sceneBindings,
        List<List<BattleUnit>> timelineRounds,
        int currentRoundIndex,
        int absoluteRoundIndex,
        BattleTurnSystem owner)
    {
        if (owner.IsExplorationMode)
        {
            Clear();
            timelineLeadUnit = null;
            return;
        }

        if (timelineAnchor == null)
        {
            timelineAnchor = ResolveTimelineAnchor(sceneBindings);
            EnsureTimelineMask();
        }

        if (timelineDatabase == null)
        {
            timelineDatabase = TurnTimelineButtonDatabase.LoadDefault();
        }

        BattleUnit newLeadUnit = FindTimelineLeadUnit(timelineRounds);
        if (timelineAnchor == null || timelineDatabase == null || currentRoundIndex < 0)
        {
            Clear();
            timelineLeadUnit = null;
            return;
        }

        if (timelineInstances.Count > 0 &&
            Application.isPlaying &&
            TimelineNeedsAnimation(timelineRounds, absoluteRoundIndex, currentRoundIndex, newLeadUnit, owner))
        {
            if (timelineAnimationRoutine != null)
            {
                host.StopCoroutine(timelineAnimationRoutine);
            }

            timelineAnimationRoutine = host.StartCoroutine(AnimateTimelineReorderAndRebuild(
                timelineRounds,
                absoluteRoundIndex,
                currentRoundIndex,
                newLeadUnit,
                owner));
            return;
        }

        BuildTimelineImmediate(timelineRounds, absoluteRoundIndex, currentRoundIndex, owner);
        timelineLeadUnit = newLeadUnit;
    }

    public void SetVisible(BattleSceneBindings sceneBindings, bool visible)
    {
        if (timelineAnchor == null)
        {
            timelineAnchor = ResolveTimelineAnchor(sceneBindings);
        }

        if (timelineAnchor != null)
        {
            timelineAnchor.gameObject.SetActive(visible);
        }

        if (timelineDivider == null)
        {
            timelineDivider = SceneHierarchyPathUtility.FindInActiveScene(TimelineDividerPath);
        }

        if (timelineDivider != null)
        {
            timelineDivider.gameObject.SetActive(visible);
        }
    }

    public void Dispose()
    {
        Clear();
    }

    public void Clear()
    {
        if (timelineAnimationRoutine != null)
        {
            host.StopCoroutine(timelineAnimationRoutine);
            timelineAnimationRoutine = null;
        }

        for (int i = 0; i < timelineInstances.Count; i++)
        {
            GameObject instance = timelineInstances[i];
            if (instance != null)
            {
                Object.Destroy(instance);
            }
        }

        timelineInstances.Clear();
        timelineInstanceUnits.Clear();
        timelineInstanceKeys.Clear();
        lastTimelineSlots.Clear();
    }

    private void BuildTimelineImmediate(
        List<List<BattleUnit>> timelineRounds,
        int absoluteRoundIndex,
        int currentRoundIndex,
        BattleTurnSystem owner)
    {
        Clear();
        List<TimelineSlot> slots = BuildTimelineSlots(timelineRounds, absoluteRoundIndex, currentRoundIndex, owner);
        for (int i = 0; i < slots.Count; i++)
        {
            TimelineSlot slot = slots[i];
            if (slot.isSeparator)
            {
                CreateRoundSeparator(slot, owner);
                lastTimelineSlots.Add(slot);
                continue;
            }

            CreateTimelineUnitInstance(slot, owner);
            lastTimelineSlots.Add(slot);
        }
    }

    private GameObject CreateTimelineUnitInstance(TimelineSlot slot, BattleTurnSystem owner)
    {
        BattleUnit unit = slot.unit;
        GameObject prefab = timelineDatabase.FindButtonPrefab(unit.characterId);
        if (prefab == null)
        {
            return null;
        }

        GameObject instance = Object.Instantiate(prefab, timelineAnchor, false);
        instance.name = string.IsNullOrWhiteSpace(unit.characterId) ? prefab.name : unit.characterId + "_Timeline";

        TurnTimelineTeamTint teamTint = instance.GetComponent<TurnTimelineTeamTint>();
        if (teamTint == null)
        {
            teamTint = instance.AddComponent<TurnTimelineTeamTint>();
        }

        teamTint.Apply(ResolveTimelineColor(unit, slot.isActive, owner));

        RectTransform rect = instance.transform as RectTransform;
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(slot.x, 0f);
        }

        instance.transform.localScale = slot.isActive ? Vector3.one * owner.activeTimelineScale : Vector3.one;
        timelineInstances.Add(instance);
        timelineInstanceUnits[instance] = unit;
        timelineInstanceKeys[instance] = slot.key;
        return instance;
    }

    private IEnumerator AnimateTimelineReorderAndRebuild(
        List<List<BattleUnit>> timelineRounds,
        int absoluteRoundIndex,
        int currentRoundIndex,
        BattleUnit newLeadUnit,
        BattleTurnSystem owner)
    {
        List<TimelineSlot> desiredSlots = BuildTimelineSlots(timelineRounds, absoluteRoundIndex, currentRoundIndex, owner);
        List<GameObject> currentInstances = GetCurrentTimelineInstances();
        if (currentInstances.Count != lastTimelineSlots.Count)
        {
            BuildTimelineImmediate(timelineRounds, absoluteRoundIndex, currentRoundIndex, owner);
            timelineLeadUnit = newLeadUnit;
            timelineAnimationRoutine = null;
            yield break;
        }

        int[] matchedDesiredIndices = MatchTimelineSlotsByKey(lastTimelineSlots, desiredSlots);
        int earliestMatchedCurrentIndex = int.MaxValue;
        for (int i = 0; i < matchedDesiredIndices.Length; i++)
        {
            if (matchedDesiredIndices[i] >= 0)
            {
                earliestMatchedCurrentIndex = Mathf.Min(earliestMatchedCurrentIndex, i);
            }
        }

        List<RectTransform> animatedRects = new List<RectTransform>();
        List<Vector2> startPositions = new List<Vector2>();
        List<Vector2> targetPositions = new List<Vector2>();
        List<Vector3> startScales = new List<Vector3>();
        List<Vector3> targetScales = new List<Vector3>();

        for (int i = 0; i < currentInstances.Count; i++)
        {
            GameObject instance = currentInstances[i];
            RectTransform rect = instance.transform as RectTransform;
            if (rect == null)
            {
                continue;
            }

            TimelineSlot currentSlot = lastTimelineSlots[i];
            int matchedIndex = matchedDesiredIndices[i];
            bool stillInQueue = matchedIndex >= 0 && matchedIndex < desiredSlots.Count;
            animatedRects.Add(rect);
            startPositions.Add(rect.anchoredPosition);
            if (stillInQueue)
            {
                targetPositions.Add(new Vector2(desiredSlots[matchedIndex].x, 0f));
            }
            else if (i < earliestMatchedCurrentIndex)
            {
                float exitX = -(Mathf.Max(rect.rect.width, rect.sizeDelta.x, 100f) + 40f);
                targetPositions.Add(new Vector2(exitX, rect.anchoredPosition.y));
            }
            else
            {
                float exitY = -(Mathf.Max(rect.rect.height, rect.sizeDelta.y, 100f) + 40f);
                targetPositions.Add(new Vector2(rect.anchoredPosition.x, exitY));
            }

            startScales.Add(rect.localScale);
            if (currentSlot.isSeparator)
            {
                targetScales.Add(Vector3.one);
            }
            else
            {
                targetScales.Add(stillInQueue && desiredSlots[matchedIndex].isActive
                    ? Vector3.one * owner.activeTimelineScale
                    : Vector3.one);
            }
        }

        float duration = Mathf.Max(0.01f, owner.timelineShiftDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            for (int i = 0; i < animatedRects.Count; i++)
            {
                RectTransform rect = animatedRects[i];
                if (rect == null)
                {
                    continue;
                }

                rect.anchoredPosition = Vector2.Lerp(startPositions[i], targetPositions[i], t);
                rect.localScale = Vector3.Lerp(startScales[i], targetScales[i], t);
            }

            yield return null;
        }

        timelineAnimationRoutine = null;
        BuildTimelineImmediate(timelineRounds, absoluteRoundIndex, currentRoundIndex, owner);
        timelineLeadUnit = newLeadUnit;
    }

    private List<TimelineSlot> BuildTimelineSlots(
        List<List<BattleUnit>> timelineRounds,
        int absoluteRoundIndex,
        int currentRoundIndex,
        BattleTurnSystem owner)
    {
        List<TimelineSlot> slots = new List<TimelineSlot>();
        float cursorX = 0f;
        for (int roundIndex = 0; roundIndex < timelineRounds.Count; roundIndex++)
        {
            List<BattleUnit> round = timelineRounds[roundIndex];
            for (int i = 0; i < round.Count; i++)
            {
                BattleUnit unit = round[i];
                if (unit == null || !unit.IsAlive)
                {
                    continue;
                }

                GameObject prefab = timelineDatabase.FindButtonPrefab(unit.characterId);
                if (prefab == null)
                {
                    continue;
                }

                float width = ResolveTimelinePrefabWidth(prefab);
                bool isActive = roundIndex == 0 && i == 0;
                slots.Add(TimelineSlot.CreateUnit(unit, cursorX, isActive, absoluteRoundIndex + roundIndex));
                cursorX += width + owner.timelineSpacing + (isActive ? owner.activeTimelineExtraSpacing : 0f);
            }

            if (roundIndex < timelineRounds.Count - 1)
            {
                float separatorWidth = GetRoundSeparatorWidth(owner);
                if (owner.roundSeparatorSprite != null)
                {
                    slots.Add(TimelineSlot.CreateSeparator(cursorX + owner.roundSeparatorSpacing, absoluteRoundIndex + roundIndex + 1));
                }

                cursorX += separatorWidth;
            }
        }

        return slots;
    }

    private List<GameObject> GetCurrentTimelineInstances()
    {
        List<GameObject> result = new List<GameObject>();
        for (int i = 0; i < timelineInstances.Count; i++)
        {
            GameObject instance = timelineInstances[i];
            if (instance != null)
            {
                result.Add(instance);
            }
        }

        return result;
    }

    private void CreateRoundSeparator(TimelineSlot slot, BattleTurnSystem owner)
    {
        if (owner.roundSeparatorSprite == null || timelineAnchor == null)
        {
            return;
        }

        GameObject separatorObject = new GameObject("RoundSeparator", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        separatorObject.transform.SetParent(timelineAnchor, false);

        RectTransform rect = separatorObject.transform as RectTransform;
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = owner.roundSeparatorSize;
            rect.anchoredPosition = new Vector2(slot.x, 0f);
        }

        Image image = separatorObject.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = owner.roundSeparatorSprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        timelineInstances.Add(separatorObject);
        timelineInstanceKeys[separatorObject] = slot.key;
    }

    private bool TimelineNeedsAnimation(
        List<List<BattleUnit>> timelineRounds,
        int absoluteRoundIndex,
        int currentRoundIndex,
        BattleUnit newLeadUnit,
        BattleTurnSystem owner)
    {
        List<TimelineSlot> desiredSlots = BuildTimelineSlots(timelineRounds, absoluteRoundIndex, currentRoundIndex, owner);
        if (lastTimelineSlots.Count != desiredSlots.Count)
        {
            return true;
        }

        for (int i = 0; i < lastTimelineSlots.Count; i++)
        {
            if (!lastTimelineSlots[i].key.Equals(desiredSlots[i].key))
            {
                return true;
            }
        }

        return timelineLeadUnit != newLeadUnit;
    }

    private float GetRoundSeparatorWidth(BattleTurnSystem owner)
    {
        if (owner.roundSeparatorSprite == null)
        {
            return owner.roundSeparatorSpacing;
        }

        return owner.roundSeparatorSpacing + owner.roundSeparatorSize.x;
    }

    private static float ResolveTimelinePrefabWidth(GameObject prefab)
    {
        if (prefab == null)
        {
            return 100f;
        }

        RectTransform rect = prefab.transform as RectTransform;
        if (rect == null)
        {
            return 100f;
        }

        LayoutElement layoutElement = rect.GetComponent<LayoutElement>();
        if (layoutElement != null && layoutElement.preferredWidth > 0f)
        {
            return layoutElement.preferredWidth;
        }

        if (rect.sizeDelta.x > 0f)
        {
            return rect.sizeDelta.x;
        }

        return 100f;
    }

    private Color ResolveTimelineColor(BattleUnit unit, bool isActive, BattleTurnSystem owner)
    {
        if (unit == null)
        {
            return owner.playerTimelineColor;
        }

        if (unit.team == BattleTeam.Player)
        {
            return isActive ? owner.activePlayerTimelineColor : owner.playerTimelineColor;
        }

        return owner.enemyTimelineColor;
    }

    private void EnsureTimelineMask()
    {
        if (timelineAnchor != null && timelineAnchor.GetComponent<RectMask2D>() == null)
        {
            timelineAnchor.gameObject.AddComponent<RectMask2D>();
        }
    }

    private static BattleUnit FindTimelineLeadUnit(List<List<BattleUnit>> timelineRounds)
    {
        for (int roundIndex = 0; roundIndex < timelineRounds.Count; roundIndex++)
        {
            List<BattleUnit> round = timelineRounds[roundIndex];
            for (int i = 0; i < round.Count; i++)
            {
                BattleUnit unit = round[i];
                if (unit != null && unit.IsAlive)
                {
                    return unit;
                }
            }
        }

        return null;
    }

    private static Transform ResolveTimelineAnchor(BattleSceneBindings sceneBindings)
    {
        if (sceneBindings != null && sceneBindings.timelineAnchor != null)
        {
            return sceneBindings.timelineAnchor;
        }

        return SceneHierarchyPathUtility.FindInActiveScene(TimelineAnchorPath);
    }

    private struct TimelineSlot
    {
        public readonly BattleUnit unit;
        public readonly float x;
        public readonly bool isActive;
        public readonly bool isSeparator;
        public readonly TimelineSlotKey key;

        private TimelineSlot(BattleUnit unit, float x, bool isActive, bool isSeparator, TimelineSlotKey key)
        {
            this.unit = unit;
            this.x = x;
            this.isActive = isActive;
            this.isSeparator = isSeparator;
            this.key = key;
        }

        public static TimelineSlot CreateUnit(BattleUnit unit, float x, bool isActive, int absoluteRound)
        {
            return new TimelineSlot(unit, x, isActive, false, TimelineSlotKey.CreateUnit(absoluteRound, unit));
        }

        public static TimelineSlot CreateSeparator(float x, int absoluteRound)
        {
            return new TimelineSlot(null, x, false, true, TimelineSlotKey.CreateSeparator(absoluteRound));
        }
    }

    private struct TimelineSlotKey
    {
        public readonly int absoluteRound;
        public readonly BattleUnit unit;
        public readonly bool isSeparator;

        private TimelineSlotKey(int absoluteRound, BattleUnit unit, bool isSeparator)
        {
            this.absoluteRound = absoluteRound;
            this.unit = unit;
            this.isSeparator = isSeparator;
        }

        public static TimelineSlotKey CreateUnit(int absoluteRound, BattleUnit unit)
        {
            return new TimelineSlotKey(absoluteRound, unit, false);
        }

        public static TimelineSlotKey CreateSeparator(int absoluteRound)
        {
            return new TimelineSlotKey(absoluteRound, null, true);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is TimelineSlotKey))
            {
                return false;
            }

            TimelineSlotKey other = (TimelineSlotKey)obj;
            return absoluteRound == other.absoluteRound && unit == other.unit && isSeparator == other.isSeparator;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = absoluteRound * 397;
                hash ^= isSeparator ? 1 : 0;
                if (unit != null)
                {
                    hash ^= unit.GetHashCode();
                }

                return hash;
            }
        }
    }

    private static int[] MatchTimelineSlotsByKey(List<TimelineSlot> previousSlots, List<TimelineSlot> desiredSlots)
    {
        int[] matches = new int[previousSlots.Count];
        for (int i = 0; i < matches.Length; i++)
        {
            matches[i] = -1;
        }

        Dictionary<TimelineSlotKey, int> desiredIndices = new Dictionary<TimelineSlotKey, int>();
        for (int i = 0; i < desiredSlots.Count; i++)
        {
            desiredIndices[desiredSlots[i].key] = i;
        }

        for (int i = 0; i < previousSlots.Count; i++)
        {
            int desiredIndex;
            if (desiredIndices.TryGetValue(previousSlots[i].key, out desiredIndex))
            {
                matches[i] = desiredIndex;
            }
        }

        return matches;
    }
}

using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class BattlePartyPortraitBinder : MonoBehaviour
{
    private const float SecondaryPortraitScaleFactor = 0.55f;
    private const float SecondaryPortraitOffsetX = -4f;
    private const float SecondaryPortraitOffsetY = -5f;
    private const float ReorderDuration = 0.18f;
    private const string CurrentPortraitPath = "Canvas/\u4e0b\u65b9\u680f\u4f4d/\u89d2\u8272\u64cd\u4f5c\u680f/\u89d2\u8272\u680f/\u5f53\u524d\u89d2\u8272/\u5f53\u524d\u89d2\u8272\u56fe";
    private const string SecondPortraitPath = "Canvas/\u4e0b\u65b9\u680f\u4f4d/\u89d2\u8272\u64cd\u4f5c\u680f/\u89d2\u8272\u680f/\u7b2c\u4e8c\u89d2\u8272/\u7b2c\u4e8c\u89d2\u8272\u56fe";
    private const string ThirdPortraitPath = "Canvas/\u4e0b\u65b9\u680f\u4f4d/\u89d2\u8272\u64cd\u4f5c\u680f/\u89d2\u8272\u680f/\u7b2c\u4e09\u89d2\u8272/\u7b2c\u4e09\u89d2\u8272\u56fe";
    private const string FourthPortraitPath = "Canvas/\u4e0b\u65b9\u680f\u4f4d/\u89d2\u8272\u64cd\u4f5c\u680f/\u89d2\u8272\u680f/\u7b2c\u56db\u89d2\u8272/\u7b2c\u56db\u89d2\u8272\u56fe";

    private readonly List<Image> portraitSlots = new List<Image>(4);
    private readonly List<RectTransform> slotContainers = new List<RectTransform>(4);
    private readonly Dictionary<string, CharacterSelectionState.SlotSelection> portraitLookup = new Dictionary<string, CharacterSelectionState.SlotSelection>(StringComparer.Ordinal);
    private BattleTurnSystem turnSystem;
    private string lastSignature = string.Empty;
    private List<CharacterSelectionState.SlotSelection> currentDisplayedSelections = new List<CharacterSelectionState.SlotSelection>(4);
    private Coroutine reorderRoutine;

    public void Initialize(BattleTurnSystem system, IReadOnlyList<CharacterSelectionState.SlotSelection> selectedSlots)
    {
        turnSystem = system;
        RebuildLookup(selectedSlots);
        RefreshPortraits(force: true);
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying || turnSystem == null)
        {
            return;
        }

        RefreshPortraits(force: false);
    }

    private void RefreshPortraits(bool force)
    {
        CachePortraitSlots();
        List<CharacterSelectionState.SlotSelection> orderedSelections = GetOrderedPlayerSelections();
        string signature = BuildSignature(orderedSelections);
        if (!force && string.Equals(signature, lastSignature, StringComparison.Ordinal))
        {
            return;
        }

        lastSignature = signature;
        if (force || currentDisplayedSelections.Count == 0)
        {
            ApplySelectionsImmediate(orderedSelections);
            return;
        }

        if (reorderRoutine != null)
        {
            StopCoroutine(reorderRoutine);
            reorderRoutine = null;
            ApplySelectionsImmediate(currentDisplayedSelections);
        }

        reorderRoutine = StartCoroutine(AnimateReorder(orderedSelections));
    }

    private void CachePortraitSlots()
    {
        if (portraitSlots.Count > 0)
        {
            return;
        }

        portraitSlots.Add(FindImageByPath(CurrentPortraitPath));
        portraitSlots.Add(FindImageByPath(SecondPortraitPath));
        portraitSlots.Add(FindImageByPath(ThirdPortraitPath));
        portraitSlots.Add(FindImageByPath(FourthPortraitPath));

        for (int i = 0; i < portraitSlots.Count; i++)
        {
            Image portrait = portraitSlots[i];
            slotContainers.Add(portrait != null ? portrait.rectTransform.parent as RectTransform : null);
        }
    }

    private void RebuildLookup(IReadOnlyList<CharacterSelectionState.SlotSelection> selectedSlots)
    {
        portraitLookup.Clear();
        if (selectedSlots == null)
        {
            return;
        }

        for (int i = 0; i < selectedSlots.Count; i++)
        {
            CharacterSelectionState.SlotSelection selection = selectedSlots[i];
            if (string.IsNullOrWhiteSpace(selection.characterId))
            {
                continue;
            }

            portraitLookup[selection.characterId] = selection;
        }
    }

    private List<CharacterSelectionState.SlotSelection> GetOrderedPlayerSelections()
    {
        List<CharacterSelectionState.SlotSelection> result = new List<CharacterSelectionState.SlotSelection>(4);
        if (turnSystem == null)
        {
            return result;
        }

        IReadOnlyList<BattleUnit> timelineUnits = turnSystem.GetTimelineUnitsForUi();
        HashSet<string> seenIds = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < timelineUnits.Count && result.Count < 4; i++)
        {
            BattleUnit unit = timelineUnits[i];
            if (unit == null || !unit.IsAlive || unit.team != BattleTeam.Player || string.IsNullOrWhiteSpace(unit.characterId))
            {
                continue;
            }

            if (!seenIds.Add(unit.characterId))
            {
                continue;
            }

            CharacterSelectionState.SlotSelection selection;
            if (portraitLookup.TryGetValue(unit.characterId, out selection))
            {
                result.Add(selection);
            }
        }

        return result;
    }

    private static string BuildSignature(List<CharacterSelectionState.SlotSelection> selections)
    {
        if (selections == null || selections.Count == 0)
        {
            return string.Empty;
        }

        string[] ids = new string[selections.Count];
        for (int i = 0; i < selections.Count; i++)
        {
            ids[i] = selections[i].characterId ?? string.Empty;
        }

        return string.Join("|", ids);
    }

    private void ApplySelectionsImmediate(IReadOnlyList<CharacterSelectionState.SlotSelection> orderedSelections)
    {
        for (int i = 0; i < portraitSlots.Count; i++)
        {
            Image portraitSlot = portraitSlots[i];
            if (portraitSlot == null)
            {
                continue;
            }

            RectTransform slotContainer = i < slotContainers.Count ? slotContainers[i] : null;
            if (slotContainer != null && portraitSlot.rectTransform.parent != slotContainer)
            {
                portraitSlot.rectTransform.SetParent(slotContainer, false);
            }

            CharacterSelectionState.SlotSelection? selection = ResolveSlotSelection(orderedSelections, i);
            ApplySelectionToImage(portraitSlot, selection, i);
        }

        currentDisplayedSelections = new List<CharacterSelectionState.SlotSelection>(orderedSelections);
    }

    private IEnumerator AnimateReorder(List<CharacterSelectionState.SlotSelection> orderedSelections)
    {
        RectTransform commonParent = slotContainers.Count > 0 ? slotContainers[0].parent as RectTransform : null;
        if (commonParent == null)
        {
            ApplySelectionsImmediate(orderedSelections);
            yield break;
        }

        List<Image> oldImagesBySlot = new List<Image>(portraitSlots);
        List<Image> targetImagesBySlot = BuildTargetImageOrder(oldImagesBySlot, orderedSelections);
        List<Vector3> startPositions = new List<Vector3>(targetImagesBySlot.Count);
        List<Vector3> targetPositions = new List<Vector3>(targetImagesBySlot.Count);

        for (int i = 0; i < targetImagesBySlot.Count; i++)
        {
            Image movingImage = targetImagesBySlot[i];
            if (movingImage == null)
            {
                startPositions.Add(Vector3.zero);
                targetPositions.Add(Vector3.zero);
                continue;
            }

            RectTransform imageRect = movingImage.rectTransform;
            startPositions.Add(imageRect.position);
            imageRect.SetParent(commonParent, true);

            RectTransform destinationSlot = i < slotContainers.Count ? slotContainers[i] : null;
            targetPositions.Add(destinationSlot != null ? destinationSlot.position : imageRect.position);
        }

        float duration = Mathf.Max(0.01f, ReorderDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            for (int i = 0; i < targetImagesBySlot.Count; i++)
            {
                Image movingImage = targetImagesBySlot[i];
                if (movingImage == null)
                {
                    continue;
                }

                movingImage.rectTransform.position = Vector3.Lerp(startPositions[i], targetPositions[i], t);
            }

            yield return null;
        }

        portraitSlots.Clear();
        portraitSlots.AddRange(targetImagesBySlot);

        for (int i = 0; i < portraitSlots.Count; i++)
        {
            Image portraitSlot = portraitSlots[i];
            if (portraitSlot == null)
            {
                continue;
            }

            RectTransform slotContainer = i < slotContainers.Count ? slotContainers[i] : null;
            if (slotContainer != null)
            {
                portraitSlot.rectTransform.SetParent(slotContainer, false);
            }

            CharacterSelectionState.SlotSelection? selection = ResolveSlotSelection(orderedSelections, i);
            ApplySelectionToImage(portraitSlot, selection, i);
        }

        currentDisplayedSelections = new List<CharacterSelectionState.SlotSelection>(orderedSelections);
        reorderRoutine = null;
    }

    private List<Image> BuildTargetImageOrder(List<Image> oldImagesBySlot, List<CharacterSelectionState.SlotSelection> orderedSelections)
    {
        List<Image> result = new List<Image>(new Image[portraitSlots.Count]);
        bool[] usedOldSlots = new bool[oldImagesBySlot.Count];

        for (int oldIndex = 0; oldIndex < currentDisplayedSelections.Count && oldIndex < oldImagesBySlot.Count; oldIndex++)
        {
            CharacterSelectionState.SlotSelection oldSelection = currentDisplayedSelections[oldIndex];
            int newIndex = FindSelectionIndexByCharacterId(orderedSelections, oldSelection.characterId);
            if (newIndex < 0 || newIndex >= result.Count)
            {
                continue;
            }

            result[newIndex] = oldImagesBySlot[oldIndex];
            usedOldSlots[oldIndex] = true;
        }

        int fallbackOldIndex = 0;
        for (int newIndex = 0; newIndex < result.Count; newIndex++)
        {
            if (result[newIndex] != null)
            {
                continue;
            }

            while (fallbackOldIndex < usedOldSlots.Length && usedOldSlots[fallbackOldIndex])
            {
                fallbackOldIndex++;
            }

            if (fallbackOldIndex < oldImagesBySlot.Count)
            {
                result[newIndex] = oldImagesBySlot[fallbackOldIndex];
                usedOldSlots[fallbackOldIndex] = true;
            }
        }

        return result;
    }

    private static int FindSelectionIndexByCharacterId(List<CharacterSelectionState.SlotSelection> selections, string characterId)
    {
        if (selections == null || string.IsNullOrWhiteSpace(characterId))
        {
            return -1;
        }

        for (int i = 0; i < selections.Count; i++)
        {
            if (string.Equals(selections[i].characterId, characterId, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }

    private static CharacterSelectionState.SlotSelection? ResolveSlotSelection(IReadOnlyList<CharacterSelectionState.SlotSelection> selectedSlots, int index)
    {
        if (selectedSlots == null || index < 0 || index >= selectedSlots.Count)
        {
            return null;
        }

        return selectedSlots[index];
    }

    private static Image FindImageByPath(string path)
    {
        Transform target = FindTransformByPath(path);
        return target != null ? target.GetComponent<Image>() : null;
    }

    private static void ApplySelectionToImage(Image portraitSlot, CharacterSelectionState.SlotSelection? selection, int slotIndex)
    {
        if (portraitSlot == null)
        {
            return;
        }

        Sprite portrait = selection.HasValue ? selection.Value.portraitSprite : null;
        portraitSlot.sprite = portrait;
        portraitSlot.color = portrait != null ? Color.white : new Color(1f, 1f, 1f, 0f);
        portraitSlot.preserveAspect = true;
        if (selection.HasValue)
        {
            ApplyPortraitLayout(portraitSlot.rectTransform, selection.Value.portraitLayout, slotIndex);
        }
    }

    private static void ApplyPortraitLayout(RectTransform target, CharacterSelectionState.PortraitLayout layout, int slotIndex)
    {
        if (target == null)
        {
            return;
        }

        target.anchorMin = layout.anchorMin;
        target.anchorMax = layout.anchorMax;
        target.pivot = layout.pivot;
        Vector2 anchoredPosition = layout.anchoredPosition;
        if (slotIndex > 0)
        {
            anchoredPosition.x += SecondaryPortraitOffsetX;
            anchoredPosition.y += SecondaryPortraitOffsetY;
        }

        target.anchoredPosition = anchoredPosition;
        target.sizeDelta = slotIndex > 0 ? layout.sizeDelta * SecondaryPortraitScaleFactor : layout.sizeDelta;
        target.localScale = layout.localScale;
    }

    private static Transform FindTransformByPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        string[] segments = path.Split('/');
        if (segments.Length == 0)
        {
            return null;
        }

        GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
        Transform current = null;
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i] != null && string.Equals(roots[i].name, segments[0], StringComparison.Ordinal))
            {
                current = roots[i].transform;
                break;
            }
        }

        if (current == null)
        {
            return null;
        }

        for (int i = 1; i < segments.Length; i++)
        {
            current = FindChildByName(current, segments[i]);
            if (current == null)
            {
                return null;
            }
        }

        return current;
    }

    private static Transform FindChildByName(Transform parent, string childName)
    {
        if (parent == null)
        {
            return null;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child != null && string.Equals(child.name, childName, StringComparison.Ordinal))
            {
                return child;
            }
        }

        return null;
    }
}

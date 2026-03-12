using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class BattlePartyPortraitBinder : MonoBehaviour
{
    private const float SecondaryPortraitScaleFactor = 0.55f;
    private const float SecondaryPortraitOffsetX = -4f;
    private const float SecondaryPortraitOffsetY = -5f;
    private const string CurrentPortraitPath = "Canvas/\u4e0b\u65b9\u680f\u4f4d/\u89d2\u8272\u64cd\u4f5c\u680f/\u89d2\u8272\u680f/\u5f53\u524d\u89d2\u8272/\u5f53\u524d\u89d2\u8272\u56fe";
    private const string SecondPortraitPath = "Canvas/\u4e0b\u65b9\u680f\u4f4d/\u89d2\u8272\u64cd\u4f5c\u680f/\u89d2\u8272\u680f/\u7b2c\u4e8c\u89d2\u8272/\u7b2c\u4e8c\u89d2\u8272\u56fe";
    private const string ThirdPortraitPath = "Canvas/\u4e0b\u65b9\u680f\u4f4d/\u89d2\u8272\u64cd\u4f5c\u680f/\u89d2\u8272\u680f/\u7b2c\u4e09\u89d2\u8272/\u7b2c\u4e09\u89d2\u8272\u56fe";
    private const string FourthPortraitPath = "Canvas/\u4e0b\u65b9\u680f\u4f4d/\u89d2\u8272\u64cd\u4f5c\u680f/\u89d2\u8272\u680f/\u7b2c\u56db\u89d2\u8272/\u7b2c\u56db\u89d2\u8272\u56fe";

    private readonly List<Image> portraitSlots = new List<Image>(4);
    private readonly Dictionary<string, CharacterSelectionState.SlotSelection> portraitLookup = new Dictionary<string, CharacterSelectionState.SlotSelection>(StringComparer.Ordinal);
    private BattleTurnSystem turnSystem;
    private string lastSignature = string.Empty;

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

        for (int i = 0; i < portraitSlots.Count; i++)
        {
            Image portraitSlot = portraitSlots[i];
            if (portraitSlot == null)
            {
                continue;
            }

            CharacterSelectionState.SlotSelection? selection = ResolveSlotSelection(orderedSelections, i);
            Sprite portrait = selection.HasValue ? selection.Value.portraitSprite : null;

            portraitSlot.sprite = portrait;
            portraitSlot.color = portrait != null ? Color.white : new Color(1f, 1f, 1f, 0f);
            portraitSlot.preserveAspect = true;
            if (selection.HasValue)
            {
                ApplyPortraitLayout(portraitSlot.rectTransform, selection.Value.portraitLayout, i);
            }
        }
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

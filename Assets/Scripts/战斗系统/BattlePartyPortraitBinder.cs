using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class BattlePartyPortraitBinder : MonoBehaviour
{
    private const float SecondaryPortraitScaleFactor = 0.55f;
    private const float SecondaryPortraitOffsetX = -4f;
    private const float SecondaryPortraitOffsetY = -5f;
    private const float ReorderDuration = 0.18f;
    private const float ActiveSlotScale = 1.08f;
    private const float InactiveSlotScale = 0.92f;
    private const string CurrentPortraitPath = "Canvas/\u4e0b\u65b9\u680f\u4f4d/\u89d2\u8272\u64cd\u4f5c\u680f/\u89d2\u8272\u680f/\u5f53\u524d\u89d2\u8272/\u5f53\u524d\u89d2\u8272\u56fe";
    private const string SecondPortraitPath = "Canvas/\u4e0b\u65b9\u680f\u4f4d/\u89d2\u8272\u64cd\u4f5c\u680f/\u89d2\u8272\u680f/\u7b2c\u4e8c\u89d2\u8272/\u7b2c\u4e8c\u89d2\u8272\u56fe";
    private const string ThirdPortraitPath = "Canvas/\u4e0b\u65b9\u680f\u4f4d/\u89d2\u8272\u64cd\u4f5c\u680f/\u89d2\u8272\u680f/\u7b2c\u4e09\u89d2\u8272/\u7b2c\u4e09\u89d2\u8272\u56fe";
    private const string FourthPortraitPath = "Canvas/\u4e0b\u65b9\u680f\u4f4d/\u89d2\u8272\u64cd\u4f5c\u680f/\u89d2\u8272\u680f/\u7b2c\u56db\u89d2\u8272/\u7b2c\u56db\u89d2\u8272\u56fe";

    private readonly List<Image> portraitSlots = new List<Image>(4);
    private readonly List<RectTransform> slotContainers = new List<RectTransform>(4);
    private readonly List<Button> portraitButtons = new List<Button>(4);
    private readonly List<UnityAction> portraitButtonActions = new List<UnityAction>(4);
    private readonly List<RectLayout> slotTemplateLayouts = new List<RectLayout>(4);
    private readonly List<int> slotTemplateSiblingIndices = new List<int>(4);
    private readonly Dictionary<string, CharacterSelectionState.SlotSelection> portraitLookup = new Dictionary<string, CharacterSelectionState.SlotSelection>(StringComparer.Ordinal);
    private BattleTurnSystem turnSystem;
    private string lastSignature = string.Empty;
    private List<CharacterSelectionState.SlotSelection> currentDisplayedSelections = new List<CharacterSelectionState.SlotSelection>(4);
    private Coroutine reorderRoutine;
    private BattleSceneBindings battleBindings;
    private RectTransform equipmentPanel;
    private bool equipmentPanelVisible;

    private struct RectLayout
    {
        public Vector2 anchorMin;
        public Vector2 anchorMax;
        public Vector2 pivot;
        public Vector2 anchoredPosition;
        public Vector2 sizeDelta;
        public Vector3 localScale;
    }

    public void Initialize(BattleTurnSystem system, IReadOnlyList<CharacterSelectionState.SlotSelection> selectedSlots)
    {
        turnSystem = system;
        battleBindings = BattleSceneBindings.FindInActiveScene();
        equipmentPanel = battleBindings != null ? battleBindings.equipmentContainer : null;
        RebuildLookup(selectedSlots);
        RefreshPortraits(force: true);
        CachePortraitButtons();
        HookPortraitButtons();
        SetEquipmentPanelVisible(false);
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

        portraitSlots.Add(ResolvePortraitSlot(0));
        portraitSlots.Add(ResolvePortraitSlot(1));
        portraitSlots.Add(ResolvePortraitSlot(2));
        portraitSlots.Add(ResolvePortraitSlot(3));

        for (int i = 0; i < portraitSlots.Count; i++)
        {
            Image portrait = portraitSlots[i];
            RectTransform container = portrait != null ? portrait.rectTransform.parent as RectTransform : null;
            slotContainers.Add(container);
            slotTemplateLayouts.Add(ReadLayout(container));
            slotTemplateSiblingIndices.Add(container != null ? container.GetSiblingIndex() : i);
        }
    }

    private void CachePortraitButtons()
    {
        if (portraitButtons.Count > 0)
        {
            return;
        }

        CachePortraitSlots();
        for (int i = 0; i < portraitSlots.Count; i++)
        {
            Image portrait = portraitSlots[i];
            Button button = portrait != null
                ? portrait.GetComponent<Button>() ?? portrait.GetComponentInParent<Button>()
                : null;
            portraitButtons.Add(button);
            portraitButtonActions.Add(null);
        }
    }

    private void HookPortraitButtons()
    {
        CachePortraitButtons();
        for (int i = 0; i < portraitButtons.Count; i++)
        {
            Button button = portraitButtons[i];
            if (button == null)
            {
                continue;
            }

            int capturedIndex = i;
            UnityAction existingAction = i < portraitButtonActions.Count ? portraitButtonActions[i] : null;
            if (existingAction != null)
            {
                button.onClick.RemoveListener(existingAction);
            }

            UnityAction action = () => OnPortraitButtonClicked(capturedIndex);
            if (i < portraitButtonActions.Count)
            {
                portraitButtonActions[i] = action;
            }
            else
            {
                portraitButtonActions.Add(action);
            }

            button.onClick.AddListener(action);
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

            ApplySlotContainerLayout(slotContainer, i);

            CharacterSelectionState.SlotSelection? selection = ResolveSlotSelection(orderedSelections, i);
            ApplySelectionToImage(portraitSlot, selection, i);
        }

        currentDisplayedSelections = new List<CharacterSelectionState.SlotSelection>(orderedSelections);
    }

    private IEnumerator AnimateReorder(List<CharacterSelectionState.SlotSelection> orderedSelections)
    {
        List<RectTransform> oldContainersBySlot = new List<RectTransform>(slotContainers);
        List<Image> oldImagesBySlot = new List<Image>(portraitSlots);
        List<RectTransform> targetContainersBySlot = BuildTargetContainerOrder(oldContainersBySlot, orderedSelections);
        List<Image> targetImagesBySlot = BuildTargetImageOrder(oldImagesBySlot, orderedSelections);
        if (targetContainersBySlot.Count == 0 || targetImagesBySlot.Count == 0)
        {
            ApplySelectionsImmediate(orderedSelections);
            yield break;
        }

        List<RectLayout> startContainerLayouts = new List<RectLayout>(targetContainersBySlot.Count);
        List<RectLayout> targetContainerLayouts = new List<RectLayout>(targetContainersBySlot.Count);
        List<RectLayout> startImageLayouts = new List<RectLayout>(targetImagesBySlot.Count);
        List<RectLayout> targetImageLayouts = new List<RectLayout>(targetImagesBySlot.Count);

        for (int i = 0; i < targetContainersBySlot.Count; i++)
        {
            RectTransform movingContainer = targetContainersBySlot[i];
            Image movingImage = targetImagesBySlot[i];
            if (movingContainer == null || movingImage == null)
            {
                startContainerLayouts.Add(default);
                targetContainerLayouts.Add(default);
                startImageLayouts.Add(default);
                targetImageLayouts.Add(default);
                continue;
            }

            startContainerLayouts.Add(ReadLayout(movingContainer));
            targetContainerLayouts.Add(BuildSlotContainerLayout(i));

            startImageLayouts.Add(ReadLayout(movingImage.rectTransform));
            CharacterSelectionState.SlotSelection? selection = ResolveSlotSelection(orderedSelections, i);
            targetImageLayouts.Add(selection.HasValue
                ? BuildPortraitImageLayout(selection.Value.portraitLayout, i)
                : ReadLayout(movingImage.rectTransform));
        }

        float duration = Mathf.Max(0.01f, ReorderDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            for (int i = 0; i < targetContainersBySlot.Count; i++)
            {
                RectTransform movingContainer = targetContainersBySlot[i];
                Image movingImage = targetImagesBySlot[i];
                if (movingContainer == null || movingImage == null)
                {
                    continue;
                }

                ApplyLerpedLayout(movingContainer, startContainerLayouts[i], targetContainerLayouts[i], t);
                ApplyLerpedLayout(movingImage.rectTransform, startImageLayouts[i], targetImageLayouts[i], t);
            }

            yield return null;
        }

        slotContainers.Clear();
        slotContainers.AddRange(targetContainersBySlot);
        portraitSlots.Clear();
        portraitSlots.AddRange(targetImagesBySlot);

        for (int i = 0; i < slotContainers.Count; i++)
        {
            RectTransform container = slotContainers[i];
            if (container != null)
            {
                ApplySlotContainerLayout(container, i);
            }
        }

        for (int i = 0; i < portraitSlots.Count; i++)
        {
            Image portraitSlot = portraitSlots[i];
            if (portraitSlot == null)
            {
                continue;
            }

            CharacterSelectionState.SlotSelection? selection = ResolveSlotSelection(orderedSelections, i);
            ApplySelectionToImage(portraitSlot, selection, i);
        }

        currentDisplayedSelections = new List<CharacterSelectionState.SlotSelection>(orderedSelections);
        reorderRoutine = null;
    }

    private List<RectTransform> BuildTargetContainerOrder(List<RectTransform> oldContainersBySlot, List<CharacterSelectionState.SlotSelection> orderedSelections)
    {
        List<RectTransform> result = new List<RectTransform>(new RectTransform[slotContainers.Count]);
        bool[] usedOldSlots = new bool[oldContainersBySlot.Count];

        for (int oldIndex = 0; oldIndex < currentDisplayedSelections.Count && oldIndex < oldContainersBySlot.Count; oldIndex++)
        {
            CharacterSelectionState.SlotSelection oldSelection = currentDisplayedSelections[oldIndex];
            int newIndex = FindSelectionIndexByCharacterId(orderedSelections, oldSelection.characterId);
            if (newIndex < 0 || newIndex >= result.Count)
            {
                continue;
            }

            result[newIndex] = oldContainersBySlot[oldIndex];
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

            if (fallbackOldIndex < oldContainersBySlot.Count)
            {
                result[newIndex] = oldContainersBySlot[fallbackOldIndex];
                usedOldSlots[fallbackOldIndex] = true;
            }
        }

        return result;
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

    private void OnPortraitButtonClicked(int slotIndex)
    {
        CharacterSelectionState.SlotSelection? selection = ResolveSlotSelection(currentDisplayedSelections, slotIndex);
        if (!selection.HasValue || string.IsNullOrWhiteSpace(selection.Value.characterId))
        {
            SetEquipmentPanelVisible(false);
            InventoryShortcutRuntimeBinder.ClearDisplayedEquipmentCharacter();
            return;
        }

        string characterId = selection.Value.characterId;
        bool isSameCharacter = string.Equals(
            InventoryShortcutRuntimeBinder.CurrentEquipmentCharacterId,
            characterId,
            StringComparison.Ordinal);

        if (equipmentPanelVisible && isSameCharacter)
        {
            SetEquipmentPanelVisible(false);
            InventoryShortcutRuntimeBinder.ClearDisplayedEquipmentCharacter();
            return;
        }

        InventoryShortcutRuntimeBinder.SetDisplayedEquipmentCharacter(characterId);
        SetEquipmentPanelVisible(true);
    }

    private void SetEquipmentPanelVisible(bool visible)
    {
        equipmentPanelVisible = visible;
        if (equipmentPanel != null && equipmentPanel.gameObject.activeSelf != visible)
        {
            equipmentPanel.gameObject.SetActive(visible);
        }
    }

    private static Image FindImageByPath(string path)
    {
        Transform target = FindTransformByPath(path);
        return target != null ? target.GetComponent<Image>() : null;
    }

    private Image ResolvePortraitSlot(int index)
    {
        if (battleBindings != null)
        {
            if (index == 0 && battleBindings.currentPortrait != null)
            {
                return battleBindings.currentPortrait;
            }

            if (index == 1 && battleBindings.secondPortrait != null)
            {
                return battleBindings.secondPortrait;
            }

            if (index == 2 && battleBindings.thirdPortrait != null)
            {
                return battleBindings.thirdPortrait;
            }

            if (index == 3 && battleBindings.fourthPortrait != null)
            {
                return battleBindings.fourthPortrait;
            }
        }

        if (index == 0)
        {
            return FindImageByPath(CurrentPortraitPath);
        }

        if (index == 1)
        {
            return FindImageByPath(SecondPortraitPath);
        }

        if (index == 2)
        {
            return FindImageByPath(ThirdPortraitPath);
        }

        return FindImageByPath(FourthPortraitPath);
    }

    private static RectLayout ReadLayout(RectTransform target)
    {
        return new RectLayout
        {
            anchorMin = target != null ? target.anchorMin : new Vector2(0.5f, 0.5f),
            anchorMax = target != null ? target.anchorMax : new Vector2(0.5f, 0.5f),
            pivot = target != null ? target.pivot : new Vector2(0.5f, 0.5f),
            anchoredPosition = target != null ? target.anchoredPosition : Vector2.zero,
            sizeDelta = target != null ? target.sizeDelta : Vector2.zero,
            localScale = target != null ? target.localScale : Vector3.one
        };
    }

    private RectLayout BuildSlotContainerLayout(int slotIndex)
    {
        RectLayout result;
        if (slotIndex >= 0 && slotIndex < slotTemplateLayouts.Count)
        {
            result = slotTemplateLayouts[slotIndex];
        }
        else
        {
            result = new RectLayout
            {
                anchorMin = new Vector2(0.5f, 0.5f),
                anchorMax = new Vector2(0.5f, 0.5f),
                pivot = new Vector2(0.5f, 0.5f),
                anchoredPosition = Vector2.zero,
                sizeDelta = Vector2.zero,
                localScale = Vector3.one
            };
        }

        float slotScale = slotIndex == 0 ? ActiveSlotScale : InactiveSlotScale;
        result.localScale = new Vector3(slotScale, slotScale, 1f);
        return result;
    }

    private static RectLayout BuildPortraitImageLayout(CharacterSelectionState.PortraitLayout layout, int slotIndex)
    {
        RectLayout result = new RectLayout
        {
            anchorMin = layout.anchorMin,
            anchorMax = layout.anchorMax,
            pivot = layout.pivot,
            anchoredPosition = layout.anchoredPosition,
            sizeDelta = slotIndex > 0 ? layout.sizeDelta * SecondaryPortraitScaleFactor : layout.sizeDelta,
            localScale = layout.localScale
        };

        if (slotIndex > 0)
        {
            result.anchoredPosition.x += SecondaryPortraitOffsetX;
            result.anchoredPosition.y += SecondaryPortraitOffsetY;
        }

        return result;
    }

    private static void ApplyLayout(RectTransform target, RectLayout layout)
    {
        if (target == null)
        {
            return;
        }

        target.anchorMin = layout.anchorMin;
        target.anchorMax = layout.anchorMax;
        target.pivot = layout.pivot;
        target.anchoredPosition = layout.anchoredPosition;
        target.sizeDelta = layout.sizeDelta;
        target.localScale = layout.localScale;
    }

    private static void ApplyLerpedLayout(RectTransform target, RectLayout from, RectLayout to, float t)
    {
        if (target == null)
        {
            return;
        }

        target.anchorMin = Vector2.Lerp(from.anchorMin, to.anchorMin, t);
        target.anchorMax = Vector2.Lerp(from.anchorMax, to.anchorMax, t);
        target.pivot = Vector2.Lerp(from.pivot, to.pivot, t);
        target.anchoredPosition = Vector2.Lerp(from.anchoredPosition, to.anchoredPosition, t);
        target.sizeDelta = Vector2.Lerp(from.sizeDelta, to.sizeDelta, t);
        target.localScale = Vector3.Lerp(from.localScale, to.localScale, t);
    }

    private void ApplySlotContainerLayout(RectTransform target, int slotIndex)
    {
        if (target == null)
        {
            return;
        }

        ApplyLayout(target, BuildSlotContainerLayout(slotIndex));
        if (slotIndex >= 0 && slotIndex < slotTemplateSiblingIndices.Count)
        {
            target.SetSiblingIndex(slotTemplateSiblingIndices[slotIndex]);
        }
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
        return SceneHierarchyPathUtility.FindInActiveScene(path);
    }
}

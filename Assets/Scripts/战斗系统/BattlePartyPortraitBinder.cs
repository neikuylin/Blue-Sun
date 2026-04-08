using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
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
    private const string EquipmentPanelPath = "Canvas/\u5f39\u7a97/\u5de6\u8fb9\u680f\u4f4d";

    private readonly List<Image> portraitSlots = new List<Image>(4);
    private readonly List<RectTransform> slotContainers = new List<RectTransform>(4);
    private readonly List<TMP_Text> healthTexts = new List<TMP_Text>(4);
    private readonly List<Button> portraitButtons = new List<Button>(4);
    private readonly List<UnityAction> portraitButtonActions = new List<UnityAction>(4);
    private readonly List<string> portraitButtonCharacterIds = new List<string>(4);
    private readonly List<RectLayout> slotTemplateLayouts = new List<RectLayout>(4);
    private readonly List<int> slotTemplateSiblingIndices = new List<int>(4);
    private readonly Dictionary<string, CharacterSelectionState.SlotSelection> portraitLookup = new Dictionary<string, CharacterSelectionState.SlotSelection>(StringComparer.Ordinal);
    private static BattlePartyPortraitBinder instance;
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
        instance = this;
        turnSystem = system;
        battleBindings = BattleSceneBindings.FindInActiveScene();
        equipmentPanel = ResolveEquipmentPanel();
        RebuildLookup(selectedSlots);
        CachePortraitButtons();
        RefreshPortraits(force: true);
        HookPortraitButtons();
        InventoryShortcutRuntimeBinder.ClearDisplayedEquipmentCharacter();
        SetEquipmentPanelVisible(false);
    }

    public static void CloseEquipmentPanel()
    {
        if (instance == null)
        {
            return;
        }

        instance.SetEquipmentPanelVisible(false);
        InventoryShortcutRuntimeBinder.ClearDisplayedEquipmentCharacter();
        界面ID列表.清空当前ID();
    }

    public static string GetDisplayedCharacterIdAtSlot(int slotIndex)
    {
        if (instance == null || slotIndex < 0 || slotIndex >= instance.portraitButtonCharacterIds.Count)
        {
            return string.Empty;
        }

        return instance.portraitButtonCharacterIds[slotIndex] ?? string.Empty;
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying || turnSystem == null)
        {
            return;
        }

        if (equipmentPanelVisible && equipmentPanel != null && !equipmentPanel.gameObject.activeInHierarchy)
        {
            equipmentPanelVisible = false;
            InventoryShortcutRuntimeBinder.ClearDisplayedEquipmentCharacter();
            界面ID列表.清空当前ID();
        }

        RefreshPortraits(force: false);
        RefreshHealthTexts();
        SyncEquipmentPanelVisibility();
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

        RebuildHealthTextCache();
    }

    private void CachePortraitButtons()
    {
        for (int i = 0; i < portraitButtons.Count && i < portraitButtonActions.Count; i++)
        {
            Button existingButton = portraitButtons[i];
            UnityAction existingAction = portraitButtonActions[i];
            if (existingButton != null && existingAction != null)
            {
                existingButton.onClick.RemoveListener(existingAction);
            }
        }

        CachePortraitSlots();
        portraitButtons.Clear();
        portraitButtonActions.Clear();
        for (int i = 0; i < portraitSlots.Count; i++)
        {
            Image portrait = portraitSlots[i];
            Button button = portrait != null
                ? portrait.GetComponent<Button>() ?? portrait.GetComponentInParent<Button>()
                : null;
            portraitButtons.Add(button);
            portraitButtonActions.Add(null);
            if (portraitButtonCharacterIds.Count <= i)
            {
                portraitButtonCharacterIds.Add(string.Empty);
            }
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

        if (turnSystem.IsExplorationMode)
        {
            IReadOnlyList<CharacterSelectionState.SlotSelection> slotSelections = CharacterSelectionState.SlotSelections;
            for (int i = 0; i < slotSelections.Count && result.Count < 4; i++)
            {
                CharacterSelectionState.SlotSelection selection = slotSelections[i];
                if (string.IsNullOrWhiteSpace(selection.characterId))
                {
                    continue;
                }

                BattleUnit unit = turnSystem.FindUnitByCharacterId(selection.characterId);
                if (unit == null || !unit.IsAlive || unit.team != BattleTeam.Player)
                {
                    continue;
                }

                result.Add(selection);
            }

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
            if (i < portraitButtonCharacterIds.Count)
            {
                portraitButtonCharacterIds[i] = selection.HasValue ? selection.Value.characterId ?? string.Empty : string.Empty;
            }
        }

        currentDisplayedSelections = new List<CharacterSelectionState.SlotSelection>(orderedSelections);
        HookPortraitButtons();
        RebuildHealthTextCache();
        RefreshHealthTexts();
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
            if (i < portraitButtonCharacterIds.Count)
            {
                portraitButtonCharacterIds[i] = selection.HasValue ? selection.Value.characterId ?? string.Empty : string.Empty;
            }
        }

        currentDisplayedSelections = new List<CharacterSelectionState.SlotSelection>(orderedSelections);
        HookPortraitButtons();
        RebuildHealthTextCache();
        RefreshHealthTexts();
        reorderRoutine = null;
    }

    private void RefreshHealthTexts()
    {
        CachePortraitSlots();
        for (int i = 0; i < healthTexts.Count; i++)
        {
            TMP_Text healthText = healthTexts[i];
            if (healthText == null)
            {
                continue;
            }

            string characterId = i < portraitButtonCharacterIds.Count
                ? portraitButtonCharacterIds[i]
                : string.Empty;
            BattleUnit unit = FindPlayerUnitByCharacterId(characterId);
            healthText.text = unit != null && unit.IsAlive
                ? unit.currentHealth + "/" + unit.maxHealth
                : string.Empty;
        }
    }

    private void RebuildHealthTextCache()
    {
        healthTexts.Clear();
        for (int i = 0; i < slotContainers.Count; i++)
        {
            healthTexts.Add(ResolveHealthText(slotContainers[i]));
        }
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
        string characterId = slotIndex >= 0 && slotIndex < portraitButtonCharacterIds.Count
            ? portraitButtonCharacterIds[slotIndex]
            : string.Empty;
        if (string.IsNullOrWhiteSpace(characterId))
        {
            SetEquipmentPanelVisible(false);
            InventoryShortcutRuntimeBinder.ClearDisplayedEquipmentCharacter();
            界面ID列表.清空当前ID();
            return;
        }
        bool isSameCharacter = string.Equals(
            界面ID列表.当前ID,
            characterId,
            StringComparison.Ordinal);

        if (equipmentPanelVisible && isSameCharacter)
        {
            SetEquipmentPanelVisible(false);
            InventoryShortcutRuntimeBinder.ClearDisplayedEquipmentCharacter();
            界面ID列表.清空当前ID();
            return;
        }

        界面ID列表.设置当前ID(characterId);
        SetEquipmentPanelVisible(true);
    }

    private void SetEquipmentPanelVisible(bool visible)
    {
        equipmentPanelVisible = visible;
        if (!visible)
        {
            InventoryShortcutRuntimeBinder.ClearDisplayedEquipmentCharacter();
        }

        SyncEquipmentPanelVisibility();
    }

    private RectTransform ResolveEquipmentPanel()
    {
        if (battleBindings != null && battleBindings.equipmentContainer != null)
        {
            return battleBindings.equipmentContainer;
        }

        Transform target = FindTransformByPath(EquipmentPanelPath);
        return target as RectTransform;
    }

    private void SyncEquipmentPanelVisibility()
    {
        if (equipmentPanel == null)
        {
            equipmentPanel = ResolveEquipmentPanel();
        }

        if (equipmentPanel != null && equipmentPanel.gameObject.activeSelf != equipmentPanelVisible)
        {
            equipmentPanel.gameObject.SetActive(equipmentPanelVisible);
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

    private static TMP_Text ResolveHealthText(RectTransform container)
    {
        if (container == null)
        {
            return null;
        }

        TMP_Text[] texts = container.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];
            if (text == null)
            {
                continue;
            }

            if (text.name.Contains("\u751f\u547d\u503c", StringComparison.Ordinal))
            {
                return text;
            }
        }

        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];
            if (text == null)
            {
                continue;
            }

            return text;
        }

        return null;
    }

    private BattleUnit FindPlayerUnitByCharacterId(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId))
        {
            return null;
        }

        BattleUnit[] units = FindObjectsOfType<BattleUnit>(true);
        BattleUnit fallback = null;
        for (int i = 0; i < units.Length; i++)
        {
            BattleUnit unit = units[i];
            if (unit == null || unit.team != BattleTeam.Player || string.IsNullOrWhiteSpace(unit.characterId))
            {
                continue;
            }

            if (string.Equals(unit.characterId, characterId, StringComparison.Ordinal))
            {
                if (unit.gameObject.activeInHierarchy && unit.IsAlive)
                {
                    return unit;
                }

                if (fallback == null)
                {
                    fallback = unit;
                }
            }
        }

        return fallback;
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

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}

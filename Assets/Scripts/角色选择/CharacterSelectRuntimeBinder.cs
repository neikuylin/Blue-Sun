using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CharacterSelectRuntimeBinder : MonoBehaviour
{
    private static readonly Color AvailableEntryColor = Color.white;
    private static readonly Color OccupiedEntryColor = new Color32(100, 100, 100, 255);
    private const string OptionalTeammateEventPrefix = "可选队友：";

    [SerializeField] private string playerCharacterId = "玩家";

    private readonly List<CharacterSlotView> slots = new List<CharacterSlotView>();
    private readonly List<CharacterSelectEntry> entries = new List<CharacterSelectEntry>();
    private readonly List<Action> unbindActions = new List<Action>();
    private readonly CharacterSelectModalPresenter modalPresenter = new CharacterSelectModalPresenter();

    private CharacterSlotView currentSlot;
    private bool modalSelectionActive;
    private GameObject currentCharacterPanel;
    private EventDatabase eventDatabase;
    private string lastCurrentId = string.Empty;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindObjectOfType<CharacterSelectRuntimeBinder>() != null)
        {
            return;
        }

        GameObject go = new GameObject("CharacterSelectRuntimeBinder");
        DontDestroyOnLoad(go);
        go.AddComponent<CharacterSelectRuntimeBinder>();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        OpenCharacterSelect.PanelOpened += OnCharacterPanelOpened;
        OpenCharacterSelect.PanelClosed += OnCharacterPanelClosed;
        BindScene();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        OpenCharacterSelect.PanelOpened -= OnCharacterPanelOpened;
        OpenCharacterSelect.PanelClosed -= OnCharacterPanelClosed;
        ExitModalSelection(false);
        modalPresenter.Dispose();
        UnbindAll();
    }

    private void Update()
    {
        string currentId = 界面ID列表.当前ID ?? string.Empty;
        if (!string.Equals(lastCurrentId, currentId, StringComparison.Ordinal))
        {
            lastCurrentId = currentId;
            RefreshDisplayByActiveToggle();
        }

        if (modalSelectionActive)
        {
            if (currentCharacterPanel == null || !currentCharacterPanel.activeInHierarchy)
            {
                ExitModalSelection(false);
            }

            return;
        }

        HandleRightClickClearSlot();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BindScene();
    }

    private void BindScene()
    {
        ExitModalSelection(false);
        UnbindAll();
        eventDatabase = EventDatabase.LoadDefault();
        CollectComponents();

        if (slots.Count == 0 && entries.Count == 0)
        {
            currentSlot = null;
            return;
        }

        BindSlotListeners();
        BindEntryListeners();

        if (slots.Count > 0)
        {
            SetCurrentSlot(slots[0]);
        }

        lastCurrentId = 界面ID列表.当前ID ?? string.Empty;
        RefreshEntryAvailabilityVisuals();
        RefreshDisplayByActiveToggle();
        SyncSelectionState();
    }

    private void CollectComponents()
    {
        slots.Clear();
        entries.Clear();
        currentSlot = null;

        CharacterSlotView[] foundSlots = FindObjectsOfType<CharacterSlotView>(true);
        for (int i = 0; i < foundSlots.Length; i++)
        {
            CharacterSlotView slot = foundSlots[i];
            if (slot == null || (slot.portraitImage == null && !slot.isMainSlot))
            {
                continue;
            }

            slots.Add(slot);
        }

        CharacterSelectEntry[] foundEntries = FindObjectsOfType<CharacterSelectEntry>(true);
        for (int i = 0; i < foundEntries.Length; i++)
        {
            CharacterSelectEntry entry = foundEntries[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.characterId))
            {
                continue;
            }

            entries.Add(entry);
        }
    }

    private void BindSlotListeners()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            CharacterSlotView slot = slots[i];
            CharacterSlotView capturedSlot = slot;

            for (int b = 0; b < slot.selectButtons.Count; b++)
            {
                Button btn = slot.selectButtons[b];
                if (btn == null)
                {
                    continue;
                }

                UnityAction onClick = delegate { SetCurrentSlot(capturedSlot); };
                btn.onClick.AddListener(onClick);
                unbindActions.Add(delegate { if (btn != null) btn.onClick.RemoveListener(onClick); });
            }

            for (int t = 0; t < slot.selectToggles.Count; t++)
            {
                Toggle toggle = slot.selectToggles[t];
                if (toggle == null)
                {
                    continue;
                }

                UnityAction<bool> onChanged = delegate (bool isOn)
                {
                    if (!isOn)
                    {
                        return;
                    }

                    SetCurrentSlot(capturedSlot);
                    RefreshDisplayByActiveToggle();
                };

                toggle.onValueChanged.AddListener(onChanged);
                unbindActions.Add(delegate { if (toggle != null) toggle.onValueChanged.RemoveListener(onChanged); });
            }
        }
    }

    private void BindEntryListeners()
    {
        for (int i = 0; i < entries.Count; i++)
        {
            CharacterSelectEntry entry = entries[i];
            string capturedId = entry.characterId;
            Button btn = entry.selectButton;

            if (btn == null)
            {
                continue;
            }

            UnityAction onClick = delegate { TryAssignCurrentSlot(capturedId); };
            btn.onClick.AddListener(onClick);
            unbindActions.Add(delegate { if (btn != null) btn.onClick.RemoveListener(onClick); });
        }
    }

    private void UnbindAll()
    {
        for (int i = 0; i < unbindActions.Count; i++)
        {
            unbindActions[i]?.Invoke();
        }

        unbindActions.Clear();
    }

    private void SetCurrentSlot(CharacterSlotView slot)
    {
        currentSlot = slot;
        SyncSelectionState();
    }

    private void OnCharacterPanelClosed(OpenCharacterSelect opener)
    {
        if (!modalSelectionActive)
        {
            return;
        }

        if (opener == null || opener.characterPanel == currentCharacterPanel)
        {
            ExitModalSelection(false);
        }
    }

    private void OnCharacterPanelOpened(OpenCharacterSelect opener)
    {
        if (opener == null || opener.characterPanel == null)
        {
            return;
        }

        currentCharacterPanel = opener.characterPanel;

        CharacterSlotView owner = null;
        Button openerButton = opener.GetComponent<Button>();
        if (openerButton != null)
        {
            owner = FindSlotByButton(openerButton);
        }

        if (owner == null)
        {
            owner = FindSlotByTransform(opener.transform);
        }

        if (owner != null)
        {
            SetCurrentSlot(owner);
        }

        EnterModalSelection(opener);
    }

    private CharacterSlotView FindSlotByButton(Button button)
    {
        if (button == null)
        {
            return null;
        }

        for (int i = 0; i < slots.Count; i++)
        {
            CharacterSlotView slot = slots[i];
            for (int j = 0; j < slot.selectButtons.Count; j++)
            {
                if (slot.selectButtons[j] == button)
                {
                    return slot;
                }
            }
        }

        return null;
    }

    private CharacterSlotView FindSlotByTransform(Transform tr)
    {
        if (tr == null)
        {
            return null;
        }

        for (int i = 0; i < slots.Count; i++)
        {
            CharacterSlotView slot = slots[i];
            if (slot == null)
            {
                continue;
            }

            if (tr == slot.transform || tr.IsChildOf(slot.transform))
            {
                return slot;
            }
        }

        return null;
    }

    private void HandleRightClickClearSlot()
    {
        if (!Input.GetMouseButtonDown(1) || EventSystem.current == null)
        {
            return;
        }

        PointerEventData pointer = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, hits);

        for (int i = 0; i < hits.Count; i++)
        {
            GameObject hitObject = hits[i].gameObject;
            if (hitObject == null)
            {
                continue;
            }

            CharacterSlotView slot = FindSlotByTransform(hitObject.transform);
            if (slot == null || slot.isMainSlot)
            {
                continue;
            }

            bool hasSelection = !string.IsNullOrEmpty(slot.selectedCharacterId) ||
                                (slot.portraitImage != null && slot.portraitImage.sprite != null);
            if (!hasSelection)
            {
                return;
            }

            ResetSlotToInitialState(slot);
            RefreshDisplayByActiveToggle();
            return;
        }
    }

    private void EnterModalSelection(OpenCharacterSelect opener)
    {
        if (opener == null || opener.characterPanel == null)
        {
            return;
        }

        ExitModalSelection(false);
        modalSelectionActive = true;
        currentCharacterPanel = opener.characterPanel;
        modalPresenter.Enter(opener, currentSlot);
    }

    private void ExitModalSelection(bool closePanel)
    {
        OpenCharacterSelect opener = closePanel ? FindOpenCharacterSelectByPanel(currentCharacterPanel) : null;
        modalPresenter.Exit(closePanel, currentCharacterPanel, opener);

        if (closePanel && currentCharacterPanel != null)
        {
            currentCharacterPanel = null;
        }

        modalSelectionActive = false;
        currentCharacterPanel = null;
    }

    private OpenCharacterSelect FindOpenCharacterSelectByPanel(GameObject panel)
    {
        if (panel == null)
        {
            return null;
        }

        OpenCharacterSelect[] openers = FindObjectsOfType<OpenCharacterSelect>(true);
        for (int i = 0; i < openers.Length; i++)
        {
            if (openers[i] != null && openers[i].characterPanel == panel)
            {
                return openers[i];
            }
        }

        return null;
    }
    private void TryAssignCurrentSlot(string characterId)
    {
        if (currentSlot == null || currentSlot.isMainSlot || currentSlot.portraitImage == null)
        {
            return;
        }

        CharacterSelectEntry entry = FindEntry(characterId);
        if (entry == null || entry.portraitSource == null || entry.portraitSource.sprite == null)
        {
            return;
        }

        CharacterSlotView occupiedSlot = FindOtherSlotByCharacter(characterId, currentSlot);
        if (occupiedSlot != null)
        {
            ResetSlotToInitialState(occupiedSlot);
        }

        currentSlot.selectedCharacterId = characterId;
        currentSlot.portraitImage.sprite = entry.portraitSource.sprite;
        ApplyPortraitLayout(currentSlot.portraitImage, entry.portraitSource.rectTransform);
        currentSlot.portraitImage.color = Color.white;
        currentSlot.portraitImage.preserveAspect = true;
        currentSlot.portraitImage.raycastTarget = false;
        currentSlot.portraitImage.gameObject.SetActive(true);

        if (currentSlot.unselectedObject != null)
        {
            currentSlot.unselectedObject.SetActive(false);
        }

        RefreshEntryAvailabilityVisuals();
        RefreshDisplayByActiveToggle();
        SyncSelectionState();
        ExitModalSelection(true);
    }

    private static void ApplyPortraitLayout(Image target, RectTransform source)
    {
        if (target == null || source == null)
        {
            return;
        }

        RectTransform rt = target.rectTransform;
        rt.anchorMin = source.anchorMin;
        rt.anchorMax = source.anchorMax;
        rt.pivot = source.pivot;
        rt.anchoredPosition = source.anchoredPosition;
        rt.sizeDelta = source.sizeDelta;
        rt.localScale = source.localScale;
    }

    private void RefreshDisplayByActiveToggle()
    {
        RefreshEntryAvailabilityVisuals();
        SyncToggleFromCurrentId();

        CharacterSlotView activeSlot = FindToggleOnSlot();
        if (activeSlot == null)
        {
            SyncSelectionState();
            return;
        }

        SyncSelectionState();
    }

    private void SyncToggleFromCurrentId()
    {
        string currentCharacterId = 界面ID列表.当前ID;
        if (string.IsNullOrWhiteSpace(currentCharacterId))
        {
            return;
        }

        CharacterSlotView matchedSlot = null;
        Toggle matchedToggle = null;

        for (int i = 0; i < slots.Count; i++)
        {
            CharacterSlotView slot = slots[i];
            if (slot == null)
            {
                continue;
            }

            string slotCharacterId = ResolveCharacterIdForSlot(slot);
            if (!string.Equals(slotCharacterId, currentCharacterId, StringComparison.Ordinal))
            {
                continue;
            }

            for (int t = 0; t < slot.selectToggles.Count; t++)
            {
                Toggle toggle = slot.selectToggles[t];
                if (toggle == null)
                {
                    continue;
                }

                matchedSlot = slot;
                matchedToggle = toggle;
                break;
            }

            if (matchedToggle != null)
            {
                break;
            }
        }

        if (matchedSlot == null || matchedToggle == null || matchedToggle.isOn)
        {
            return;
        }

        matchedToggle.SetIsOnWithoutNotify(true);
        currentSlot = matchedSlot;
    }

    private CharacterSlotView FindToggleOnSlot()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            CharacterSlotView slot = slots[i];
            for (int t = 0; t < slot.selectToggles.Count; t++)
            {
                Toggle toggle = slot.selectToggles[t];
                if (toggle != null && toggle.isOn)
                {
                    return slot;
                }
            }
        }

        return currentSlot;
    }

    private string ResolveCharacterIdForSlot(CharacterSlotView slot)
    {
        if (!string.IsNullOrEmpty(slot.selectedCharacterId))
        {
            return slot.selectedCharacterId;
        }

        if (!string.IsNullOrEmpty(slot.slotCharacterId))
        {
            return slot.slotCharacterId;
        }

        if (slot.isMainSlot)
        {
            return playerCharacterId;
        }

        return string.Empty;
    }

    private CharacterSlotView FindOtherSlotByCharacter(string characterId, CharacterSlotView targetSlot)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            CharacterSlotView slot = slots[i];
            if (slot == targetSlot)
            {
                continue;
            }

            if (string.Equals(slot.selectedCharacterId, characterId, StringComparison.Ordinal))
            {
                return slot;
            }
        }

        return null;
    }

    private void RefreshEntryAvailabilityVisuals()
    {
        for (int i = 0; i < entries.Count; i++)
        {
            CharacterSelectEntry entry = entries[i];
            if (entry == null)
            {
                continue;
            }

            ApplyEntryRectAnchor(entry);

            bool isVisible = IsCharacterEntryVisible(entry.characterId);
            if (entry.gameObject.activeSelf != isVisible)
            {
                entry.gameObject.SetActive(isVisible);
            }

            if (!isVisible)
            {
                continue;
            }

            bool occupiedByAnySlot = IsCharacterUsedInAnySlot(entry.characterId);
            ApplyEntryDisplayColor(entry, occupiedByAnySlot ? OccupiedEntryColor : AvailableEntryColor);
        }
    }

    private bool IsCharacterUsedInAnySlot(string characterId)
    {
        if (string.IsNullOrEmpty(characterId))
        {
            return false;
        }

        for (int i = 0; i < slots.Count; i++)
        {
            CharacterSlotView slot = slots[i];
            if (slot == null)
            {
                continue;
            }

            string resolvedId = ResolveCharacterIdForSlot(slot);
            if (string.Equals(resolvedId, characterId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static void ApplyEntryDisplayColor(CharacterSelectEntry entry, Color color)
    {
        if (entry == null)
        {
            return;
        }

        if (entry.selectButton != null)
        {
            Image[] childImages = entry.selectButton.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < childImages.Length; i++)
            {
                Image image = childImages[i];
                if (image == null)
                {
                    continue;
                }

                if (image.gameObject == entry.selectButton.gameObject)
                {
                    continue;
                }

                image.color = color;
            }
        }
    }

    private static void ApplyEntryRectAnchor(CharacterSelectEntry entry)
    {
        if (entry == null)
        {
            return;
        }

        RectTransform rectTransform = entry.transform as RectTransform;
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchorMin = new Vector2(0f, 0.5f);
        rectTransform.anchorMax = new Vector2(0f, 0.5f);
        rectTransform.pivot = new Vector2(0f, 0.5f);
    }

    private static void ResetSlotToInitialState(CharacterSlotView slot)
    {
        if (slot == null)
        {
            return;
        }

        slot.selectedCharacterId = string.Empty;

        if (slot.portraitImage != null)
        {
            slot.portraitImage.sprite = null;
            slot.portraitImage.gameObject.SetActive(false);
        }

        if (slot.unselectedObject != null)
        {
            slot.unselectedObject.SetActive(true);
        }
    }

    private void SyncSelectionState()
    {
        if (slots.Count == 0)
        {
            return;
        }

        CharacterSelectionState.UpdateSelections(slots, FindToggleOnSlot());
    }

    private CharacterSelectEntry FindEntry(string characterId)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            CharacterSelectEntry entry = entries[i];
            if (entry != null &&
                string.Equals(entry.characterId, characterId, StringComparison.Ordinal))
            {
                return entry;
            }
        }

        return null;
    }

    private bool IsCharacterEntryVisible(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId))
        {
            return false;
        }

        if (eventDatabase == null)
        {
            return true;
        }

        string eventId = OptionalTeammateEventPrefix + characterId;
        EventDatabase.EventEntry entry = eventDatabase.FindEntry(eventId);
        return entry == null || EventRuntimeState.IsEnabled(entry);
    }
}



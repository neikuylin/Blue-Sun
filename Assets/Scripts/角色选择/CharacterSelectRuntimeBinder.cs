using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CharacterSelectRuntimeBinder : MonoBehaviour
{
    [SerializeField] private string playerCharacterId = "玩家";

    private readonly List<CharacterSlotView> slots = new List<CharacterSlotView>();
    private readonly List<CharacterSelectEntry> entries = new List<CharacterSelectEntry>();
    private readonly List<Action> unbindActions = new List<Action>();
    private readonly List<ModalCanvasPatch> modalCanvasPatches = new List<ModalCanvasPatch>();

    private CharacterSlotView currentSlot;
    private bool modalSelectionActive;
    private GameObject modalOverlay;
    private GameObject currentCharacterPanel;

    private struct ModalCanvasPatch
    {
        public Transform target;
        public Canvas canvas;
        public bool canvasAdded;
        public bool previousOverrideSorting;
        public int previousSortingOrder;
        public GraphicRaycaster raycaster;
        public bool raycasterAdded;
    }

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
        UnbindAll();
    }

    private void Update()
    {
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
        CollectComponents();
        BindSlotListeners();
        BindEntryListeners();

        if (slots.Count > 0)
        {
            SetCurrentSlot(slots[0]);
        }

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

        EnsureModalOverlay(opener.characterPanel);
        ElevateAllowedForModal(opener);
    }

    private void EnsureModalOverlay(GameObject panel)
    {
        if (panel == null)
        {
            return;
        }

        Canvas rootCanvas = panel.GetComponentInParent<Canvas>();
        if (rootCanvas == null)
        {
            return;
        }

        if (modalOverlay == null)
        {
            modalOverlay = new GameObject("CharacterSelectModalOverlay", typeof(RectTransform), typeof(Image));
        }

        RectTransform rt = modalOverlay.GetComponent<RectTransform>();
        rt.SetParent(rootCanvas.transform, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image image = modalOverlay.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.5f);
        image.raycastTarget = true;

        modalOverlay.SetActive(true);
        modalOverlay.transform.SetAsLastSibling();
    }

    private void ElevateAllowedForModal(OpenCharacterSelect opener)
    {
        ClearModalCanvasPatches();

        if (modalOverlay == null)
        {
            return;
        }

        ElevateTransformWithCanvas(opener.characterPanel.transform, 5001);

        if (currentSlot != null)
        {
            ElevateTransformWithCanvas(currentSlot.transform, 5002);

            for (int i = 0; i < currentSlot.selectButtons.Count; i++)
            {
                if (currentSlot.selectButtons[i] != null)
                {
                    ElevateTransformWithCanvas(currentSlot.selectButtons[i].transform, 5003);
                }
            }
        }

        ElevateTransformWithCanvas(opener.transform, 5004);
    }

    private void ElevateTransformWithCanvas(Transform tr, int sortingOrder)
    {
        if (tr == null)
        {
            return;
        }

        for (int i = 0; i < modalCanvasPatches.Count; i++)
        {
            if (modalCanvasPatches[i].target == tr)
            {
                ModalCanvasPatch existing = modalCanvasPatches[i];
                if (existing.canvas != null)
                {
                    existing.canvas.overrideSorting = true;
                    existing.canvas.sortingOrder = Mathf.Max(existing.canvas.sortingOrder, sortingOrder);
                }

                modalCanvasPatches[i] = existing;
                return;
            }
        }

        Canvas canvas = tr.GetComponent<Canvas>();
        bool canvasAdded = false;
        if (canvas == null)
        {
            canvas = tr.gameObject.AddComponent<Canvas>();
            canvasAdded = true;
        }

        GraphicRaycaster raycaster = tr.GetComponent<GraphicRaycaster>();
        bool raycasterAdded = false;
        if (raycaster == null)
        {
            raycaster = tr.gameObject.AddComponent<GraphicRaycaster>();
            raycasterAdded = true;
        }

        ModalCanvasPatch patch = new ModalCanvasPatch
        {
            target = tr,
            canvas = canvas,
            canvasAdded = canvasAdded,
            previousOverrideSorting = canvas.overrideSorting,
            previousSortingOrder = canvas.sortingOrder,
            raycaster = raycaster,
            raycasterAdded = raycasterAdded
        };

        canvas.overrideSorting = true;
        canvas.sortingOrder = sortingOrder;
        modalCanvasPatches.Add(patch);
    }

    private void ExitModalSelection(bool closePanel)
    {
        if (closePanel && currentCharacterPanel != null)
        {
            currentCharacterPanel.SetActive(false);
            OpenCharacterSelect opener = FindOpenCharacterSelectByPanel(currentCharacterPanel);
            if (opener != null)
            {
                opener.MarkClosedFromOutside();
            }
        }

        ClearModalCanvasPatches();

        if (modalOverlay != null)
        {
            modalOverlay.SetActive(false);
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

    private void ClearModalCanvasPatches()
    {
        for (int i = modalCanvasPatches.Count - 1; i >= 0; i--)
        {
            ModalCanvasPatch patch = modalCanvasPatches[i];
            if (patch.target == null)
            {
                continue;
            }

            if (patch.raycasterAdded && patch.raycaster != null)
            {
                Destroy(patch.raycaster);
            }

            if (patch.canvas != null)
            {
                if (patch.canvasAdded)
                {
                    Destroy(patch.canvas);
                }
                else
                {
                    patch.canvas.overrideSorting = patch.previousOverrideSorting;
                    patch.canvas.sortingOrder = patch.previousSortingOrder;
                }
            }
        }

        modalCanvasPatches.Clear();
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
        CharacterSlotView activeSlot = FindToggleOnSlot();
        if (activeSlot == null)
        {
            ShowBackgroundPortrait(string.Empty);
            SyncSelectionState();
            return;
        }

        UpdateBackgroundPortraitForSlot(activeSlot);
        SyncSelectionState();
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

    private void UpdateBackgroundPortraitForSlot(CharacterSlotView slot)
    {
        if (slot == null)
        {
            ShowBackgroundPortrait(string.Empty);
            return;
        }

        string characterId = ResolveCharacterIdForSlot(slot);
        ShowBackgroundPortrait(characterId);
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

    private void ShowBackgroundPortrait(string characterId)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            CharacterSelectEntry entry = entries[i];
            if (entry == null)
            {
                continue;
            }

            bool shouldShow = !string.IsNullOrEmpty(characterId) && string.Equals(entry.characterId, characterId, StringComparison.Ordinal);
            for (int j = 0; j < entry.backgroundPortraits.Count; j++)
            {
                GameObject go = entry.backgroundPortraits[j];
                if (go != null)
                {
                    go.SetActive(shouldShow);
                }
            }
        }
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
        CharacterSelectionState.UpdateSelections(slots, FindToggleOnSlot());
    }

    private CharacterSelectEntry FindEntry(string characterId)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            CharacterSelectEntry entry = entries[i];
            if (entry != null && string.Equals(entry.characterId, characterId, StringComparison.Ordinal))
            {
                return entry;
            }
        }

        return null;
    }
}



using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class SkillLoadoutRuntimeBinder : MonoBehaviour
{
    private const float SkillTooltipDelaySeconds = 0.5f;
    private const float DragIconSize = 100f;
    private const string OverlayIconName = "\u6280\u80fd\u56fe\u6848";
    private const string GrantedCornerMarkerName = "__GrantedSkillCornerMarker";
    private const string GrantedSlotNamePrefix = "__GrantedSkillSlot_";
    private const string MemorizedSlotNamePrefix = "__MemorizedSkillSlot_";
    private const string DefaultCharacterId = "\u73a9\u5bb6";

    private static readonly Color DisabledSkillColor = SkillUsabilityUtility.DisabledSkillColor;
    private static readonly Color EnabledSkillColor = SkillUsabilityUtility.EnabledSkillColor;

    private enum SlotSurface
    {
        Loadout,
        Warehouse
    }

    private sealed class SkillSlotWidget
    {
        public RectTransform root;
        public Image skillIcon;
        public Image grantedCornerMarker;
        public string skillId;
        public bool isGranted;
        public int slotIndex = -1;
        public SlotSurface surface;
        public SkillSlotRelay relay;
    }

    private sealed class SkillSlotRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerClickHandler
    {
        private SkillLoadoutRuntimeBinder owner;
        private SlotSurface surface;
        private int index;

        public void Configure(SkillLoadoutRuntimeBinder binder, SlotSurface slotSurface, int slotIndex)
        {
            owner = binder;
            surface = slotSurface;
            index = slotIndex;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            owner?.HandleSkillPointerEnter(surface, index);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            owner?.HandleSkillPointerExit(surface, index, eventData);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            owner?.HandleBeginDrag(surface, index, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            owner?.HandleDrag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            owner?.HandleEndDrag();
        }

        public void OnDrop(PointerEventData eventData)
        {
            owner?.HandleDrop(surface, index);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            owner?.HandlePointerClick(surface, index, eventData);
        }
    }

    private struct DragRef
    {
        public SlotSurface surface;
        public int index;
    }

    private static SkillLoadoutRuntimeBinder instance;

    private readonly List<SkillSlotWidget> journeySkillSlots = new List<SkillSlotWidget>();
    private readonly List<SkillSlotWidget> warehouseSkillSlots = new List<SkillSlotWidget>();
    private BattleSkillDatabase skillDatabase;
    private string currentCharacterId = string.Empty;
    private RectTransform journeySkillContainer;
    private SkillBarBinding skillBarBinding;
    private SkillWarehouseBinding skillWarehouseBinding;
    private RectTransform warehouseContainer;
    private Canvas dragCanvas;
    private RectTransform dragIconRoot;
    private Image dragIconImage;
    private bool isDragging;
    private Coroutine deferredRefreshCoroutine;
    private DragRef dragSource;
    private SkillSlotWidget dragSourceWidget;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
        {
            return;
        }

        GameObject go = new GameObject(nameof(SkillLoadoutRuntimeBinder));
        DontDestroyOnLoad(go);
        instance = go.AddComponent<SkillLoadoutRuntimeBinder>();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        界面刷新中心.全部界面刷新 += OnGlobalRefreshRequested;
        界面刷新中心.当前角色切换刷新 += OnCurrentCharacterRefreshRequested;
        界面刷新中心.技能装配变更 += OnSkillLoadoutChanged;
        界面刷新中心.装备变更 += OnEquipmentChanged;
        BindScene();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        界面刷新中心.全部界面刷新 -= OnGlobalRefreshRequested;
        界面刷新中心.当前角色切换刷新 -= OnCurrentCharacterRefreshRequested;
        界面刷新中心.技能装配变更 -= OnSkillLoadoutChanged;
        界面刷新中心.装备变更 -= OnEquipmentChanged;
        if (deferredRefreshCoroutine != null)
        {
            StopCoroutine(deferredRefreshCoroutine);
            deferredRefreshCoroutine = null;
        }
        HandleEndDrag();
        journeySkillSlots.Clear();
        warehouseSkillSlots.Clear();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BindScene();
    }

    private void BindScene()
    {
        skillDatabase = BattleSkillDatabase.LoadDefault();
        skillBarBinding = SkillBarBinding.FindBindingInActiveScene();
        skillWarehouseBinding = SkillWarehouseBinding.FindBindingInActiveScene();
        journeySkillContainer = ResolveJourneySkillContainer();
        warehouseContainer = ResolveWarehouseContainer();
        CollectJourneySkillSlots();
        CollectWarehouseSkillSlots();
        currentCharacterId = ResolveCharacterId(界面ID列表.当前ID);
        if (deferredRefreshCoroutine != null)
        {
            StopCoroutine(deferredRefreshCoroutine);
        }
        deferredRefreshCoroutine = StartCoroutine(DeferredRefreshAfterBinding());
    }

    private IEnumerator DeferredRefreshAfterBinding()
    {
        yield return null;
        deferredRefreshCoroutine = null;
        currentCharacterId = ResolveCharacterId(界面ID列表.当前ID);
        RefreshAll();
    }

    private void OnGlobalRefreshRequested()
    {
        currentCharacterId = ResolveCharacterId(界面ID列表.当前ID);
        RefreshAll();
    }

    private void OnCurrentCharacterRefreshRequested(string characterId)
    {
        currentCharacterId = ResolveCharacterId(界面ID列表.当前ID);
        RefreshAll();
    }

    private void OnSkillLoadoutChanged(string characterId)
    {
        currentCharacterId = ResolveCharacterId(界面ID列表.当前ID);
        RefreshAll();
    }

    private void OnEquipmentChanged(string characterId)
    {
        currentCharacterId = ResolveCharacterId(界面ID列表.当前ID);
        RefreshAll();
    }

    private void RefreshAll()
    {
        RefreshJourneySkillSlots();
        RefreshWarehouseSkillSlots();
    }

    private void CollectJourneySkillSlots()
    {
        journeySkillSlots.Clear();
        RectTransform container = journeySkillContainer;
        if (container == null)
        {
            return;
        }
        for (int i = 0; i < container.childCount; i++)
        {
            RectTransform child = container.GetChild(i) as RectTransform;
            if (child == null)
            {
                continue;
            }

            Image overlay = EnsureOverlayIcon(child);
            if (overlay == null)
            {
                continue;
            }

            journeySkillSlots.Add(new SkillSlotWidget
            {
                root = child,
                skillIcon = overlay,
                grantedCornerMarker = EnsureGrantedCornerMarker(child),
                surface = SlotSurface.Loadout
            });
        }
    }

    private void CollectWarehouseSkillSlots()
    {
        warehouseSkillSlots.Clear();
        warehouseContainer = ResolveWarehouseContainer();
        if (warehouseContainer == null)
        {
            return;
        }
        for (int i = 0; i < warehouseContainer.childCount; i++)
        {
            RectTransform child = warehouseContainer.GetChild(i) as RectTransform;
            if (child == null)
            {
                continue;
            }

            Image overlay = EnsureOverlayIcon(child);
            if (overlay == null)
            {
                continue;
            }

            warehouseSkillSlots.Add(new SkillSlotWidget
            {
                root = child,
                skillIcon = overlay,
                surface = SlotSurface.Warehouse
            });
        }
    }

    private void RefreshJourneySkillSlots()
    {
        CharacterSkillLoadoutDatabase.CharacterSkillEntry entry = ResolveLoadoutEntry(currentCharacterId);
        List<string> grantedSkillIds = CharacterSkillListUtility.BuildGrantedSkillIds(currentCharacterId);
        int grantedSkillCount = grantedSkillIds.Count;
        int memorizedSlotCapacity = ResolveVisibleSkillMemorySlotCount(currentCharacterId);
        int visibleSlotCount = grantedSkillCount + memorizedSlotCapacity;
        EnsureJourneySkillSlotCapacity(grantedSkillCount, memorizedSlotCapacity);
        CollectJourneySkillSlots();
        if (journeySkillSlots.Count == 0)
        {
            return;
        }

        for (int i = 0; i < journeySkillSlots.Count; i++)
        {
            SkillSlotWidget widget = journeySkillSlots[i];
            bool shouldDisplay = i < visibleSlotCount;
            if (widget.root != null && widget.root.gameObject.activeSelf != shouldDisplay)
            {
                widget.root.gameObject.SetActive(shouldDisplay);
            }

            bool isGrantedSlot = shouldDisplay && i < grantedSkillCount;
            int memorizedIndex = i - grantedSkillCount;
            string skillId = string.Empty;
            if (isGrantedSlot)
            {
                skillId = i < grantedSkillIds.Count ? grantedSkillIds[i] : string.Empty;
            }
            else if (shouldDisplay && memorizedIndex >= 0 && entry != null && entry.memorizedSkillIds != null && memorizedIndex < entry.memorizedSkillIds.Count)
            {
                skillId = entry.memorizedSkillIds[memorizedIndex];
            }

            widget.skillId = skillId;
            widget.isGranted = isGrantedSlot;
            widget.slotIndex = isGrantedSlot ? -1 : memorizedIndex;

            EnsureRelay(widget, SlotSurface.Loadout, i);
            RefreshSlotVisual(widget, shouldDisplay, skillId, isGrantedSlot);
            RefreshGrantedCornerMarker(widget);
        }
    }

    private void RefreshWarehouseSkillSlots()
    {
        CharacterSkillLoadoutDatabase.CharacterSkillEntry entry = ResolveLoadoutEntry(currentCharacterId);
        int warehouseCount = entry != null && entry.warehouseSkillIds != null
            ? entry.warehouseSkillIds.Count
            : 0;
        int visibleSlotCount = warehouseCount;

        EnsureWarehouseSlotCapacity(visibleSlotCount);
        CollectWarehouseSkillSlots();

        for (int i = 0; i < warehouseSkillSlots.Count; i++)
        {
            SkillSlotWidget widget = warehouseSkillSlots[i];
            bool shouldDisplay = i < visibleSlotCount;
            if (widget.root != null && widget.root.gameObject.activeSelf != shouldDisplay)
            {
                widget.root.gameObject.SetActive(shouldDisplay);
            }

            string skillId = i < warehouseCount && entry != null && entry.warehouseSkillIds != null
                ? entry.warehouseSkillIds[i]
                : string.Empty;
            widget.skillId = skillId;
            widget.isGranted = false;
            widget.slotIndex = i;

            EnsureRelay(widget, SlotSurface.Warehouse, i);
            RefreshSlotVisual(widget, shouldDisplay, skillId, false);
        }
    }

    private void RefreshSlotVisual(SkillSlotWidget widget, bool shouldDisplay, string skillId, bool isGranted)
    {
        if (widget == null || widget.skillIcon == null)
        {
            return;
        }

        Sprite icon = shouldDisplay ? ResolveSkillIcon(skillId) : null;
        bool isUsable = SkillUsabilityUtility.IsSkillUsable(skillDatabase, currentCharacterId, skillId);
        widget.skillIcon.sprite = icon;
        widget.skillIcon.enabled = shouldDisplay && icon != null;
        widget.skillIcon.gameObject.SetActive(shouldDisplay && icon != null);
        widget.skillIcon.raycastTarget = false;
        widget.skillIcon.color = ResolveSkillDisplayColor(isGranted, isUsable);
    }

    private void HandleSkillPointerEnter(SlotSurface surface, int index)
    {
        SkillSlotWidget widget = ResolveWidget(surface, index);
        if (widget == null || string.IsNullOrWhiteSpace(widget.skillId))
        {
            HoverTooltipController.Cancel(HoverTooltipController.HoverCategory.Skill, SkillTooltipRuntime.Hide);
            return;
        }

        BattleSkillDatabase.SkillEntry entry = skillDatabase != null ? skillDatabase.FindEntry(widget.skillId) : null;
        if (entry == null ||
            (entry.group != BattleSkillDatabase.SkillGroup.CombatArt &&
             entry.group != BattleSkillDatabase.SkillGroup.Spell))
        {
            HoverTooltipController.Cancel(HoverTooltipController.HoverCategory.Skill, SkillTooltipRuntime.Hide);
            return;
        }

        float attackPower = InventoryShortcutRuntimeBinder.GetCharacterWeaponAttackPower(currentCharacterId);
        float multiplier = Mathf.Max(0f, entry.damageMultiplier);
        SkillTooltipRuntime.Snapshot snapshot = new SkillTooltipRuntime.Snapshot
        {
            skillId = widget.skillId,
            displayName = widget.skillId,
            description = entry.description ?? string.Empty,
            ownerCharacterId = currentCharacterId ?? string.Empty,
            hitRate = ResolveDisplayedSkillHitRate(currentCharacterId, entry),
            damage = Mathf.Max(0, Mathf.RoundToInt(attackPower * multiplier)),
            icon = entry.icon,
            isEmpty = false
        };

        HoverTooltipController.BeginHover(
            HoverTooltipController.HoverCategory.Skill,
            widget.root,
            SkillTooltipDelaySeconds,
            () => SkillTooltipRuntime.Show(snapshot),
            SkillTooltipRuntime.Hide);
    }

    private void HandleSkillPointerExit(SlotSurface surface, int index, PointerEventData eventData)
    {
        SkillSlotWidget widget = ResolveWidget(surface, index);
        if (widget == null || widget.root == null)
        {
            HoverTooltipController.Cancel(HoverTooltipController.HoverCategory.Skill, SkillTooltipRuntime.Hide);
            return;
        }

        HoverTooltipController.EndHover(HoverTooltipController.HoverCategory.Skill, widget.root, eventData);
    }

    private void HandleBeginDrag(SlotSurface surface, int index, PointerEventData eventData)
    {
        if (isDragging)
        {
            return;
        }

        SkillSlotWidget widget = ResolveWidget(surface, index);
        if (!CanDragWidget(widget))
        {
            return;
        }

        EnsureDragVisual(widget.root);
        if (dragIconRoot == null || dragIconImage == null)
        {
            return;
        }

        dragIconRoot.sizeDelta = new Vector2(DragIconSize, DragIconSize);
        dragIconImage.sprite = widget.skillIcon != null ? widget.skillIcon.sprite : ResolveSkillIcon(widget.skillId);
        dragIconImage.color = new Color(1f, 1f, 1f, 0.9f);
        dragIconImage.enabled = dragIconImage.sprite != null;
        dragIconImage.rectTransform.sizeDelta = new Vector2(DragIconSize, DragIconSize);
        dragIconRoot.gameObject.SetActive(true);
        UpdateDragVisualPosition(eventData);

        dragSource = new DragRef { surface = surface, index = index };
        dragSourceWidget = widget;
        SetWidgetDraggingVisible(widget, false);
        isDragging = true;
    }

    private void HandleDrag(PointerEventData eventData)
    {
        if (isDragging)
        {
            UpdateDragVisualPosition(eventData);
        }
    }

    private void HandleDrop(SlotSurface surface, int index)
    {
        if (!isDragging)
        {
            return;
        }

        SkillSlotWidget sourceWidget = ResolveWidget(dragSource.surface, dragSource.index);
        SkillSlotWidget targetWidget = ResolveWidget(surface, index);
        if (sourceWidget == null || targetWidget == null)
        {
            return;
        }

        if (!TrySwapSkillSlots(sourceWidget, targetWidget))
        {
            return;
        }

        界面刷新中心.请求技能装配变更(currentCharacterId);
        ItemSoundUtility.PlaySkillMove();
    }

    private void HandlePointerClick(SlotSurface surface, int index, PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Right || isDragging)
        {
            return;
        }

        SkillSlotWidget widget = ResolveWidget(surface, index);
        if (!TryHandleRightClickMove(widget))
        {
            return;
        }

        eventData.Use();
        界面刷新中心.请求技能装配变更(currentCharacterId);
        ItemSoundUtility.PlaySkillMove();
    }

    private bool TryHandleRightClickMove(SkillSlotWidget widget)
    {
        if (!CanReorderWidget(widget))
        {
            return false;
        }

        CharacterSkillLoadoutDatabase.CharacterSkillEntry entry = ResolveLoadoutEntry(currentCharacterId);
        if (entry == null)
        {
            return false;
        }

        return widget.surface == SlotSurface.Warehouse
            ? TryMoveWarehouseToMemorized(entry, widget.slotIndex)
            : TryMoveMemorizedToWarehouse(entry, widget.slotIndex);
    }

    private bool TrySwapSkillSlots(SkillSlotWidget sourceWidget, SkillSlotWidget targetWidget)
    {
        if (sourceWidget == null || targetWidget == null)
        {
            return false;
        }

        CharacterSkillLoadoutDatabase.CharacterSkillEntry entry = ResolveLoadoutEntry(currentCharacterId);
        if (entry == null)
        {
            return false;
        }

        if (sourceWidget.surface == SlotSurface.Loadout && targetWidget.surface == SlotSurface.Loadout)
        {
            return TrySwapWithinMemorized(entry, sourceWidget.slotIndex, targetWidget.slotIndex);
        }

        if (sourceWidget.surface == SlotSurface.Warehouse && targetWidget.surface == SlotSurface.Warehouse)
        {
            return TrySwapWithinWarehouse(entry, sourceWidget.slotIndex, targetWidget.slotIndex);
        }

        if (sourceWidget.surface == SlotSurface.Warehouse)
        {
            return TryMoveWarehouseIntoMemorizedSlot(entry, sourceWidget.slotIndex, targetWidget.slotIndex);
        }

        return TryMoveMemorizedIntoWarehouseSlot(entry, sourceWidget.slotIndex, targetWidget.slotIndex);
    }

    private static int FindFirstEmptyMemorizedSlotIndex(CharacterSkillLoadoutDatabase.CharacterSkillEntry entry)
    {
        if (entry == null || entry.memorizedSkillIds == null)
        {
            return -1;
        }

        int visibleSlotCount = ResolveVisibleSkillMemorySlotCount(entry.characterId);
        for (int i = 0; i < visibleSlotCount; i++)
        {
            if (i >= entry.memorizedSkillIds.Count || string.IsNullOrWhiteSpace(entry.memorizedSkillIds[i]))
            {
                return i;
            }
        }

        return -1;
    }

    private static int FindFirstEmptyWarehouseSlotIndex(CharacterSkillLoadoutDatabase.CharacterSkillEntry entry)
    {
        if (entry == null || entry.warehouseSkillIds == null)
        {
            return -1;
        }

        for (int i = 0; i < entry.warehouseSkillIds.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(entry.warehouseSkillIds[i]))
            {
                return i;
            }
        }

        return -1;
    }

    private static bool TryMoveWarehouseToMemorized(CharacterSkillLoadoutDatabase.CharacterSkillEntry entry, int warehouseIndex)
    {
        if (entry == null || warehouseIndex < 0 || entry.warehouseSkillIds == null || warehouseIndex >= entry.warehouseSkillIds.Count)
        {
            return false;
        }

        int memorizedIndex = FindFirstEmptyMemorizedSlotIndex(entry);
        if (memorizedIndex < 0)
        {
            return false;
        }

        CharacterSkillLoadoutDatabase.EnsureWarehouseSlotCapacity(entry, warehouseIndex + 1);
        CharacterSkillLoadoutDatabase.EnsureMemorizedSlotMinSize(entry, memorizedIndex + 1);
        string movedSkillId = entry.warehouseSkillIds[warehouseIndex];
        if (string.IsNullOrWhiteSpace(movedSkillId))
        {
            return false;
        }

        int movedWeight = CharacterSkillLoadoutDatabase.GetWarehouseSkillWeightAt(entry, warehouseIndex);
        entry.memorizedSkillIds[memorizedIndex] = movedSkillId;
        entry.memorizedSkillWeights[memorizedIndex] = movedWeight;
        entry.warehouseSkillIds[warehouseIndex] = string.Empty;
        if (entry.warehouseSkillWeights != null && warehouseIndex < entry.warehouseSkillWeights.Count)
        {
            entry.warehouseSkillWeights[warehouseIndex] = 0;
        }
        return true;
    }

    private static bool TryMoveMemorizedToWarehouse(CharacterSkillLoadoutDatabase.CharacterSkillEntry entry, int memorizedIndex)
    {
        if (entry == null || memorizedIndex < 0 || entry.memorizedSkillIds == null || memorizedIndex >= entry.memorizedSkillIds.Count)
        {
            return false;
        }

        string movedSkillId = entry.memorizedSkillIds[memorizedIndex];
        if (string.IsNullOrWhiteSpace(movedSkillId))
        {
            return false;
        }

        int warehouseIndex = FindFirstEmptyWarehouseSlotIndex(entry);
        if (warehouseIndex < 0)
        {
            return false;
        }

        CharacterSkillLoadoutDatabase.EnsureMemorizedSlotMinSize(entry, memorizedIndex + 1);
        entry.memorizedSkillIds[memorizedIndex] = string.Empty;
        int movedWeight = CharacterSkillLoadoutDatabase.GetMemorizedSkillWeightAt(entry, memorizedIndex);
        entry.warehouseSkillIds[warehouseIndex] = movedSkillId;
        if (entry.warehouseSkillWeights != null && warehouseIndex < entry.warehouseSkillWeights.Count)
        {
            entry.warehouseSkillWeights[warehouseIndex] = movedWeight;
        }
        if (entry.memorizedSkillWeights != null && memorizedIndex < entry.memorizedSkillWeights.Count)
        {
            entry.memorizedSkillWeights[memorizedIndex] = 0;
        }
        return true;
    }

    private static bool TrySwapWithinMemorized(CharacterSkillLoadoutDatabase.CharacterSkillEntry entry, int sourceIndex, int targetIndex)
    {
        if (entry == null || entry.memorizedSkillIds == null || sourceIndex < 0 || targetIndex < 0)
        {
            return false;
        }

        CharacterSkillLoadoutDatabase.EnsureMemorizedSlotMinSize(entry, Mathf.Max(sourceIndex, targetIndex) + 1);
        if (sourceIndex >= entry.memorizedSkillIds.Count || targetIndex >= entry.memorizedSkillIds.Count)
        {
            return false;
        }

        string tempSkillId = entry.memorizedSkillIds[sourceIndex];
        entry.memorizedSkillIds[sourceIndex] = entry.memorizedSkillIds[targetIndex];
        entry.memorizedSkillIds[targetIndex] = tempSkillId;

        if (entry.memorizedSkillWeights != null &&
            sourceIndex < entry.memorizedSkillWeights.Count &&
            targetIndex < entry.memorizedSkillWeights.Count)
        {
            int tempWeight = entry.memorizedSkillWeights[sourceIndex];
            entry.memorizedSkillWeights[sourceIndex] = entry.memorizedSkillWeights[targetIndex];
            entry.memorizedSkillWeights[targetIndex] = tempWeight;
        }
        return true;
    }

    private static bool TrySwapWithinWarehouse(CharacterSkillLoadoutDatabase.CharacterSkillEntry entry, int sourceIndex, int targetIndex)
    {
        if (entry == null || sourceIndex < 0 || targetIndex < 0)
        {
            return false;
        }

        CharacterSkillLoadoutDatabase.EnsureWarehouseSlotCapacity(entry, Mathf.Max(sourceIndex, targetIndex) + 1);
        if (entry.warehouseSkillIds == null || sourceIndex >= entry.warehouseSkillIds.Count || targetIndex >= entry.warehouseSkillIds.Count)
        {
            return false;
        }

        string tempSkillId = entry.warehouseSkillIds[sourceIndex];
        entry.warehouseSkillIds[sourceIndex] = entry.warehouseSkillIds[targetIndex];
        entry.warehouseSkillIds[targetIndex] = tempSkillId;

        if (entry.warehouseSkillWeights != null &&
            sourceIndex < entry.warehouseSkillWeights.Count &&
            targetIndex < entry.warehouseSkillWeights.Count)
        {
            int tempWeight = entry.warehouseSkillWeights[sourceIndex];
            entry.warehouseSkillWeights[sourceIndex] = entry.warehouseSkillWeights[targetIndex];
            entry.warehouseSkillWeights[targetIndex] = tempWeight;
        }
        return true;
    }

    private static bool TryMoveWarehouseIntoMemorizedSlot(CharacterSkillLoadoutDatabase.CharacterSkillEntry entry, int warehouseIndex, int memorizedIndex)
    {
        if (entry == null || warehouseIndex < 0 || memorizedIndex < 0)
        {
            return false;
        }

        CharacterSkillLoadoutDatabase.EnsureWarehouseSlotCapacity(entry, warehouseIndex + 1);
        CharacterSkillLoadoutDatabase.EnsureMemorizedSlotMinSize(entry, memorizedIndex + 1);
        if (entry.warehouseSkillIds == null ||
            entry.memorizedSkillIds == null ||
            warehouseIndex >= entry.warehouseSkillIds.Count ||
            memorizedIndex >= entry.memorizedSkillIds.Count)
        {
            return false;
        }

        string warehouseSkillId = entry.warehouseSkillIds[warehouseIndex];
        if (string.IsNullOrWhiteSpace(warehouseSkillId) ||
            !string.IsNullOrWhiteSpace(entry.memorizedSkillIds[memorizedIndex]))
        {
            return false;
        }

        int warehouseWeight = CharacterSkillLoadoutDatabase.GetWarehouseSkillWeightAt(entry, warehouseIndex);
        entry.memorizedSkillIds[memorizedIndex] = warehouseSkillId;
        entry.warehouseSkillIds[warehouseIndex] = string.Empty;
        if (entry.memorizedSkillWeights != null && memorizedIndex < entry.memorizedSkillWeights.Count)
        {
            entry.memorizedSkillWeights[memorizedIndex] = warehouseWeight;
        }
        if (entry.warehouseSkillWeights != null && warehouseIndex < entry.warehouseSkillWeights.Count)
        {
            entry.warehouseSkillWeights[warehouseIndex] = 0;
        }
        return true;
    }

    private static bool TryMoveMemorizedIntoWarehouseSlot(CharacterSkillLoadoutDatabase.CharacterSkillEntry entry, int memorizedIndex, int warehouseIndex)
    {
        if (entry == null || warehouseIndex < 0 || memorizedIndex < 0)
        {
            return false;
        }

        CharacterSkillLoadoutDatabase.EnsureWarehouseSlotCapacity(entry, warehouseIndex + 1);
        CharacterSkillLoadoutDatabase.EnsureMemorizedSlotMinSize(entry, memorizedIndex + 1);
        if (entry.warehouseSkillIds == null ||
            entry.memorizedSkillIds == null ||
            warehouseIndex >= entry.warehouseSkillIds.Count ||
            memorizedIndex >= entry.memorizedSkillIds.Count)
        {
            return false;
        }

        string memorizedSkillId = entry.memorizedSkillIds[memorizedIndex];
        if (string.IsNullOrWhiteSpace(memorizedSkillId) ||
            !string.IsNullOrWhiteSpace(entry.warehouseSkillIds[warehouseIndex]))
        {
            return false;
        }

        int memorizedWeight = CharacterSkillLoadoutDatabase.GetMemorizedSkillWeightAt(entry, memorizedIndex);
        entry.warehouseSkillIds[warehouseIndex] = memorizedSkillId;
        entry.memorizedSkillIds[memorizedIndex] = string.Empty;
        if (entry.warehouseSkillWeights != null && warehouseIndex < entry.warehouseSkillWeights.Count)
        {
            entry.warehouseSkillWeights[warehouseIndex] = memorizedWeight;
        }
        if (entry.memorizedSkillWeights != null && memorizedIndex < entry.memorizedSkillWeights.Count)
        {
            entry.memorizedSkillWeights[memorizedIndex] = 0;
        }

        return true;
    }

    private void HandleEndDrag()
    {
        isDragging = false;
        if (dragIconRoot != null)
        {
            dragIconRoot.gameObject.SetActive(false);
        }

        if (dragIconImage != null)
        {
            dragIconImage.sprite = null;
            dragIconImage.enabled = false;
        }

        SetWidgetDraggingVisible(dragSourceWidget, true);
        dragSourceWidget = null;
    }

    private bool CanDragWidget(SkillSlotWidget widget)
    {
        return CanReorderWidget(widget);
    }

    private static bool CanReorderWidget(SkillSlotWidget widget)
    {
        // 鎶€鑳借兘涓嶈兘鍦ㄦ垬鏂椾腑浣跨敤锛屽拰鑳戒笉鑳藉湪鍚▼鐣岄潰閲屾暣鐞嗕綅缃槸涓ゅ洖浜嬨€?
        return widget != null &&
               !string.IsNullOrWhiteSpace(widget.skillId) &&
               !widget.isGranted &&
               widget.slotIndex >= 0;
    }

    private SkillSlotWidget ResolveWidget(SlotSurface surface, int index)
    {
        List<SkillSlotWidget> list = surface == SlotSurface.Loadout ? journeySkillSlots : warehouseSkillSlots;
        return index >= 0 && index < list.Count ? list[index] : null;
    }

    private CharacterSkillLoadoutDatabase.CharacterSkillEntry ResolveLoadoutEntry(string characterId)
    {
        CharacterSkillLoadoutDatabase database = CharacterSkillLoadoutDatabase.LoadDefault();
        if (database == null)
        {
            return null;
        }

        string resolvedCharacterId = ResolveCharacterId(characterId);
        CharacterSkillLoadoutDatabase.CharacterSkillEntry entry = database.GetOrCreateEntry(resolvedCharacterId);
        CharacterSkillLoadoutDatabase.PrepareEntryForRuntime(entry, ResolveVisibleSkillMemorySlotCount(resolvedCharacterId));
        return entry;
    }

    private void EnsureWarehouseSlotCapacity(int requiredCount)
    {
        if (warehouseContainer == null)
        {
            return;
        }

        RectTransform template = skillWarehouseBinding != null ? skillWarehouseBinding.ResolveWarehouseSlotTemplate() : null;
        if (template == null)
        {
            return;
        }

        while (warehouseContainer.childCount < requiredCount)
        {
            RectTransform clone = Instantiate(template, warehouseContainer, false);
            clone.name = template.name;
            clone.gameObject.SetActive(true);
        }
    }

    private void EnsureJourneySkillSlotCapacity(int grantedCount, int memorizedCount)
    {
        if (journeySkillContainer == null)
        {
            return;
        }

        RectTransform memorizedTemplate = skillBarBinding != null ? skillBarBinding.ResolveSkillSlotTemplate() : null;
        RectTransform grantedTemplate = skillBarBinding != null ? skillBarBinding.ResolveGrantedSkillSlotTemplate() : null;
        if (memorizedTemplate == null || grantedTemplate == null)
        {
            return;
        }

        int requiredCount = grantedCount + memorizedCount;
        bool requiresRebuild = journeySkillContainer.childCount != requiredCount;
        if (!requiresRebuild)
        {
            for (int i = 0; i < grantedCount; i++)
            {
                Transform child = journeySkillContainer.GetChild(i);
                if (child == null || !child.name.StartsWith(GrantedSlotNamePrefix, StringComparison.Ordinal))
                {
                    requiresRebuild = true;
                    break;
                }
            }

            if (!requiresRebuild)
            {
                for (int i = 0; i < memorizedCount; i++)
                {
                    int childIndex = grantedCount + i;
                    Transform child = journeySkillContainer.GetChild(childIndex);
                    if (child == null || !child.name.StartsWith(MemorizedSlotNamePrefix, StringComparison.Ordinal))
                    {
                        requiresRebuild = true;
                        break;
                    }
                }
            }
        }

        if (!requiresRebuild)
        {
            return;
        }

        for (int i = journeySkillContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = journeySkillContainer.GetChild(i);
            if (child == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }

        for (int i = 0; i < grantedCount; i++)
        {
            RectTransform clone = Instantiate(grantedTemplate, journeySkillContainer, false);
            clone.name = GrantedSlotNamePrefix + i;
            clone.gameObject.SetActive(true);
            clone.SetSiblingIndex(i);
        }

        for (int i = 0; i < memorizedCount; i++)
        {
            RectTransform clone = Instantiate(memorizedTemplate, journeySkillContainer, false);
            clone.name = MemorizedSlotNamePrefix + i;
            clone.gameObject.SetActive(true);
            clone.SetSiblingIndex(grantedCount + i);
        }
    }

    private RectTransform ResolveJourneySkillContainer()
    {
        return skillBarBinding != null ? skillBarBinding.ResolveSkillSlotContainer() : null;
    }

    private RectTransform ResolveWarehouseContainer()
    {
        return skillWarehouseBinding != null ? skillWarehouseBinding.ResolveWarehouseContainer() : null;
    }

    private void EnsureDragVisual(RectTransform fromRoot)
    {
        if (dragIconRoot != null && dragIconImage != null)
        {
            return;
        }

        dragCanvas = fromRoot != null ? fromRoot.GetComponentInParent<Canvas>() : null;
        if (dragCanvas == null)
        {
            return;
        }

        GameObject go = new GameObject("SkillDragIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        go.transform.SetParent(dragCanvas.transform, false);

        dragIconRoot = go.GetComponent<RectTransform>();
        dragIconRoot.anchorMin = new Vector2(0.5f, 0.5f);
        dragIconRoot.anchorMax = new Vector2(0.5f, 0.5f);
        dragIconRoot.pivot = new Vector2(0.5f, 0.5f);

        dragIconImage = go.GetComponent<Image>();
        dragIconImage.raycastTarget = false;
        dragIconImage.preserveAspect = true;

        CanvasGroup canvasGroup = go.GetComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        go.SetActive(false);
    }

    private void UpdateDragVisualPosition(PointerEventData eventData)
    {
        if (dragCanvas == null || dragIconRoot == null || eventData == null)
        {
            return;
        }

        Camera uiCamera = dragCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : dragCanvas.worldCamera;
        RectTransform canvasRect = dragCanvas.transform as RectTransform;
        if (canvasRect == null)
        {
            return;
        }

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, uiCamera, out Vector2 localPos))
        {
            dragIconRoot.anchoredPosition = localPos;
        }
    }

    private static void SetWidgetDraggingVisible(SkillSlotWidget widget, bool visible)
    {
        if (widget == null)
        {
            return;
        }

        if (widget.skillIcon != null)
        {
            bool shouldShow = visible && widget.skillIcon.sprite != null;
            widget.skillIcon.enabled = shouldShow;
            widget.skillIcon.gameObject.SetActive(shouldShow);
        }

        if (widget.grantedCornerMarker != null)
        {
            widget.grantedCornerMarker.gameObject.SetActive(visible && widget.grantedCornerMarker.enabled);
        }
    }

    private void EnsureRelay(SkillSlotWidget widget, SlotSurface surface, int index)
    {
        if (widget == null || widget.root == null || instance == null)
        {
            return;
        }

        if (widget.relay == null)
        {
            widget.relay = widget.root.GetComponent<SkillSlotRelay>();
            if (widget.relay == null)
            {
                widget.relay = widget.root.gameObject.AddComponent<SkillSlotRelay>();
            }
        }

        widget.relay.Configure(instance, surface, index);
    }

    private static Image EnsureOverlayIcon(RectTransform slotRoot)
    {
        Transform existing = FindChildByName(slotRoot, OverlayIconName);
        if (existing == null)
        {
            GameObject iconObject = new GameObject(OverlayIconName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            existing = iconObject.transform;
            existing.SetParent(slotRoot, false);
        }

        RectTransform rect = existing as RectTransform;
        Image image = existing != null ? existing.GetComponent<Image>() : null;
        if (rect == null || image == null)
        {
            return null;
        }

        if (existing.parent != slotRoot)
        {
            existing.SetParent(slotRoot, false);
        }

        image.raycastTarget = false;
        image.preserveAspect = true;
        return image;
    }

    private Image EnsureGrantedCornerMarker(RectTransform slotRoot)
    {
        if (slotRoot == null)
        {
            return null;
        }

        Transform existing = FindChildByName(slotRoot, GrantedCornerMarkerName);
        if (existing == null)
        {
            GameObject markerObject = new GameObject(GrantedCornerMarkerName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            existing = markerObject.transform;
            existing.SetParent(slotRoot, false);
        }

        RectTransform rect = existing as RectTransform;
        Image image = existing != null ? existing.GetComponent<Image>() : null;
        if (rect == null || image == null)
        {
            return null;
        }

        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.localScale = Vector3.one;
        image.raycastTarget = false;
        existing.SetAsLastSibling();
        return image;
    }

    private string ResolveCharacterId(string characterId)
    {
        return string.IsNullOrWhiteSpace(characterId) ? DefaultCharacterId : characterId;
    }

    private static int ResolveVisibleSkillMemorySlotCount(string characterId)
    {
        CharacterStatDatabase statDatabase = CharacterStatDatabase.LoadDefault();
        CharacterStatDatabase.StatEntry statEntry =
            statDatabase != null ? statDatabase.FindEntry(string.IsNullOrWhiteSpace(characterId) ? DefaultCharacterId : characterId) : null;

        return statEntry != null
            ? statEntry.ResolveSkillMemorySlots()
            : CharacterStatDatabase.StatEntry.BaseSkillMemorySlots;
    }

    private static int CountGrantedSkills(List<CharacterSkillListUtility.DisplaySkillEntry> displayEntries)
    {
        if (displayEntries == null)
        {
            return 0;
        }

        int count = 0;
        for (int i = 0; i < displayEntries.Count; i++)
        {
            if (displayEntries[i].IsGranted)
            {
                count++;
            }
        }

        return count;
    }

    private static Color ResolveSkillDisplayColor(bool isGranted, bool isUsable)
    {
        if (!isUsable)
        {
            return DisabledSkillColor;
        }

        return EnabledSkillColor;
    }

    private static int ResolveDisplayedSkillHitRate(string characterId, BattleSkillDatabase.SkillEntry skill)
    {
        CharacterStatDatabase statDatabase = CharacterStatDatabase.LoadDefault();
        CharacterStatDatabase.StatEntry statEntry =
            statDatabase != null ? statDatabase.FindEntry(string.IsNullOrWhiteSpace(characterId) ? DefaultCharacterId : characterId) : null;
        int baseHitRate = statEntry != null ? statEntry.ResolveHitRate() : 100;
        return Mathf.Max(0, baseHitRate + (skill != null ? skill.ResolveHitRateModifier() : 0));
    }

    private void RefreshGrantedCornerMarker(SkillSlotWidget widget)
    {
        if (widget == null || widget.grantedCornerMarker == null)
        {
            return;
        }

        Sprite cornerSprite = skillBarBinding != null ? skillBarBinding.GrantedMarkerSprite : null;
        bool shouldShow = widget.isGranted && cornerSprite != null && !string.IsNullOrWhiteSpace(widget.skillId);

        widget.grantedCornerMarker.sprite = cornerSprite;
        widget.grantedCornerMarker.enabled = shouldShow;
        widget.grantedCornerMarker.gameObject.SetActive(shouldShow);

        RectTransform markerRect = widget.grantedCornerMarker.rectTransform;
        if (markerRect == null)
        {
            return;
        }

        Vector2 anchoredPosition = skillBarBinding != null
            ? skillBarBinding.GrantedMarkerPosition
            : new Vector2(-6f, -6f);

        markerRect.sizeDelta = cornerSprite != null ? cornerSprite.rect.size : Vector2.zero;
        markerRect.anchoredPosition = anchoredPosition;
    }

    private Sprite ResolveSkillIcon(string skillId)
    {
        if (string.IsNullOrWhiteSpace(skillId))
        {
            return null;
        }

        if (skillDatabase == null)
        {
            skillDatabase = BattleSkillDatabase.LoadDefault();
        }

        BattleSkillDatabase.SkillEntry entry = skillDatabase != null ? skillDatabase.FindEntry(skillId) : null;
        return entry != null ? entry.icon : null;
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

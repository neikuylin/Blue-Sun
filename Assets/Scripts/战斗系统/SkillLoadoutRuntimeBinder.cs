using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class SkillLoadoutRuntimeBinder : MonoBehaviour
{
    private const float SkillTooltipDelaySeconds = 0.5f;
    private const string OverlayIconName = "\u6280\u80fd\u56fe\u6848";
    private const string GrantedCornerMarkerName = "__GrantedSkillCornerMarker";
    private const string DefaultCharacterId = "\u73a9\u5bb6";

    private static readonly string[] JourneySkillContainerChain =
    {
        "\u89d2\u8272\u9875\u9762",
        "\u6280\u80fd\u680f\u4f4d",
        "\u6280\u80fd\u683c\u5b50\u533a\u57df"
    };

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

    private sealed class SkillSlotRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
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
    private int lastEquipmentSkillRevision = -1;
    private RectTransform journeySkillContainer;
    private JourneySkillGridBinding journeySkillGridBinding;
    private JourneySkillWarehouseBinding warehouseBinding;
    private RectTransform warehouseContainer;
    private Canvas dragCanvas;
    private RectTransform dragIconRoot;
    private Image dragIconImage;
    private bool isDragging;
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
        BindScene();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        HandleEndDrag();
        journeySkillSlots.Clear();
        warehouseSkillSlots.Clear();
    }

    private void Update()
    {
        string targetCharacterId = ResolveCharacterId(CharacterSelectionState.ActiveCharacterId);
        int equipmentSkillRevision = InventoryShortcutRuntimeBinder.EquipmentSkillRevision;
        if (string.Equals(currentCharacterId, targetCharacterId, StringComparison.Ordinal) &&
            lastEquipmentSkillRevision == equipmentSkillRevision)
        {
            return;
        }

        currentCharacterId = targetCharacterId;
        lastEquipmentSkillRevision = equipmentSkillRevision;
        RefreshAll();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BindScene();
    }

    private void BindScene()
    {
        skillDatabase = BattleSkillDatabase.LoadDefault();
        journeySkillGridBinding = JourneySkillGridBinding.FindBindingInActiveScene();
        warehouseBinding = JourneySkillWarehouseBinding.FindBindingInActiveScene();
        journeySkillContainer = ResolveJourneySkillContainer();
        warehouseContainer = ResolveWarehouseContainer();
        CollectJourneySkillSlots();
        CollectWarehouseSkillSlots();
        currentCharacterId = ResolveCharacterId(CharacterSelectionState.ActiveCharacterId);
        lastEquipmentSkillRevision = InventoryShortcutRuntimeBinder.EquipmentSkillRevision;
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
        RectTransform container = journeySkillContainer != null ? journeySkillContainer : ResolveJourneySkillContainer();
        if (container == null)
        {
            return;
        }

        EnsureGridLayout(container);
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
        warehouseContainer = warehouseContainer != null ? warehouseContainer : ResolveWarehouseContainer();
        if (warehouseContainer == null)
        {
            return;
        }

        EnsureGridLayout(warehouseContainer);
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
        if (journeySkillSlots.Count == 0)
        {
            return;
        }

        List<CharacterSkillListUtility.DisplaySkillEntry> displayEntries = CharacterSkillListUtility.BuildDisplaySkillEntries(currentCharacterId);
        int memorySlotCount = ResolveVisibleSkillMemorySlotCount(currentCharacterId);
        int grantedSkillCount = CountGrantedSkills(displayEntries);
        int visibleSlotCount = grantedSkillCount + memorySlotCount;

        for (int i = 0; i < journeySkillSlots.Count; i++)
        {
            SkillSlotWidget widget = journeySkillSlots[i];
            bool shouldDisplay = i < visibleSlotCount;
            if (widget.root != null && widget.root.gameObject.activeSelf != shouldDisplay)
            {
                widget.root.gameObject.SetActive(shouldDisplay);
            }

            CharacterSkillListUtility.DisplaySkillEntry displayEntry =
                shouldDisplay && i < displayEntries.Count ? displayEntries[i] : default;
            string skillId = shouldDisplay && i < displayEntries.Count ? displayEntry.SkillId : string.Empty;
            widget.skillId = skillId;
            widget.isGranted = shouldDisplay && i < displayEntries.Count && displayEntry.IsGranted;
            widget.slotIndex = widget.isGranted ? -1 : i - grantedSkillCount;

            EnsureRelay(widget, SlotSurface.Loadout, i);
            RefreshSlotVisual(widget, shouldDisplay, skillId, widget.isGranted);
            RefreshGrantedCornerMarker(widget);
        }
    }

    private void RefreshWarehouseSkillSlots()
    {
        CharacterSkillLoadoutDatabase.CharacterSkillEntry entry = ResolveLoadoutEntry(currentCharacterId);
        int memorySlotCount = ResolveVisibleSkillMemorySlotCount(currentCharacterId);
        int warehouseCount = entry != null && entry.skillIds != null
            ? Mathf.Max(0, entry.skillIds.Count - memorySlotCount)
            : 0;
        int visibleSlotCount = Mathf.Max(1, Mathf.Max(warehouseCount, warehouseContainer != null ? warehouseContainer.childCount : 0));

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

            string skillId = i < warehouseCount && entry != null && entry.skillIds != null
                ? entry.skillIds[memorySlotCount + i]
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
        if (entry == null || entry.group != BattleSkillDatabase.SkillGroup.CombatArt)
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

        dragIconRoot.sizeDelta = widget.root.rect.size;
        dragIconImage.sprite = widget.skillIcon != null ? widget.skillIcon.sprite : ResolveSkillIcon(widget.skillId);
        dragIconImage.color = new Color(1f, 1f, 1f, 0.9f);
        dragIconImage.enabled = dragIconImage.sprite != null;
        dragIconImage.SetNativeSize();
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

        RefreshAll();
    }

    private bool TrySwapSkillSlots(SkillSlotWidget sourceWidget, SkillSlotWidget targetWidget)
    {
        CharacterSkillLoadoutDatabase.CharacterSkillEntry entry = ResolveLoadoutEntry(currentCharacterId);
        if (entry == null || entry.skillIds == null)
        {
            return false;
        }

        int memorySlotCount = ResolveVisibleSkillMemorySlotCount(currentCharacterId);
        int sourceDataIndex = ResolveDataIndex(sourceWidget, memorySlotCount);
        int targetDataIndex = ResolveDataIndex(targetWidget, memorySlotCount);
        if (sourceDataIndex < 0 || targetDataIndex < 0 || sourceDataIndex >= entry.skillIds.Count)
        {
            return false;
        }

        EnsureSkillDataCapacity(entry, Mathf.Max(sourceDataIndex, targetDataIndex) + 1);

        string tempSkillId = entry.skillIds[sourceDataIndex];
        entry.skillIds[sourceDataIndex] = entry.skillIds[targetDataIndex];
        entry.skillIds[targetDataIndex] = tempSkillId;

        if (sourceDataIndex < entry.skillWeights.Count && targetDataIndex < entry.skillWeights.Count)
        {
            int tempWeight = entry.skillWeights[sourceDataIndex];
            entry.skillWeights[sourceDataIndex] = entry.skillWeights[targetDataIndex];
            entry.skillWeights[targetDataIndex] = tempWeight;
        }

        return true;
    }

    private static int ResolveDataIndex(SkillSlotWidget widget, int memorySlotCount)
    {
        if (widget == null || widget.slotIndex < 0)
        {
            return -1;
        }

        return widget.surface == SlotSurface.Loadout
            ? widget.slotIndex
            : memorySlotCount + widget.slotIndex;
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
        int memorySlotCount = ResolveVisibleSkillMemorySlotCount(resolvedCharacterId);
        CharacterSkillLoadoutDatabase.EnsureSlotDataSize(entry, Mathf.Max(memorySlotCount, entry.skillIds != null ? entry.skillIds.Count : 0));
        return entry;
    }

    private void EnsureWarehouseSlotCapacity(int requiredCount)
    {
        if (warehouseContainer == null)
        {
            return;
        }

        RectTransform template = warehouseBinding != null ? warehouseBinding.ResolveWarehouseSlotTemplate() : null;
        if (template == null && warehouseContainer.childCount > 0)
        {
            template = warehouseContainer.GetChild(0) as RectTransform;
        }

        if (template == null && requiredCount > 0)
        {
            template = CreateFallbackWarehouseSlot(warehouseContainer);
        }

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

    private static RectTransform CreateFallbackWarehouseSlot(RectTransform parent)
    {
        if (parent == null)
        {
            return null;
        }

        GameObject rootObject = new GameObject("技能仓库格子", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        rootObject.transform.SetParent(parent, false);

        RectTransform root = rootObject.GetComponent<RectTransform>();
        root.sizeDelta = new Vector2(96f, 96f);

        Image background = rootObject.GetComponent<Image>();
        background.color = Color.white;
        background.raycastTarget = true;

        GameObject iconObject = new GameObject(OverlayIconName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.SetParent(root, false);
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.sizeDelta = new Vector2(72f, 72f);

        Image icon = iconObject.GetComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        icon.enabled = false;
        return root;
    }

    private static void EnsureSkillDataCapacity(CharacterSkillLoadoutDatabase.CharacterSkillEntry entry, int requiredCount)
    {
        if (entry == null)
        {
            return;
        }

        if (entry.skillIds == null)
        {
            entry.skillIds = new List<string>();
        }

        if (entry.skillWeights == null)
        {
            entry.skillWeights = new List<int>();
        }

        while (entry.skillIds.Count < requiredCount)
        {
            entry.skillIds.Add(string.Empty);
        }

        while (entry.skillWeights.Count < requiredCount)
        {
            entry.skillWeights.Add(0);
        }
    }

    private RectTransform ResolveJourneySkillContainer()
    {
        RectTransform explicitContainer = warehouseBinding != null ? warehouseBinding.ResolveSkillSlotContainer() : null;
        if (explicitContainer != null)
        {
            return explicitContainer;
        }

        RectTransform boundContainer = JourneySkillGridBinding.FindInActiveScene();
        if (boundContainer != null)
        {
            return boundContainer;
        }

        return FindJourneySkillContainer();
    }

    private RectTransform ResolveWarehouseContainer()
    {
        return warehouseBinding != null ? warehouseBinding.ResolveWarehouseContainer() : null;
    }

    private static RectTransform FindJourneySkillContainer()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid())
        {
            return null;
        }

        GameObject[] roots = activeScene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            RectTransform found = FindContainerRecursive(roots[i] != null ? roots[i].transform : null, 0);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static RectTransform FindContainerRecursive(Transform current, int matchedDepth)
    {
        if (current == null)
        {
            return null;
        }

        if (string.Equals(current.name, JourneySkillContainerChain[matchedDepth], StringComparison.Ordinal))
        {
            if (matchedDepth == JourneySkillContainerChain.Length - 1)
            {
                return current as RectTransform;
            }

            for (int i = 0; i < current.childCount; i++)
            {
                RectTransform nested = FindContainerRecursive(current.GetChild(i), matchedDepth + 1);
                if (nested != null)
                {
                    return nested;
                }
            }
        }

        for (int i = 0; i < current.childCount; i++)
        {
            RectTransform nested = FindContainerRecursive(current.GetChild(i), matchedDepth);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }

    private static void EnsureGridLayout(RectTransform container)
    {
        if (container == null)
        {
            return;
        }

        GridLayoutGroup grid = container.GetComponent<GridLayoutGroup>();
        if (grid == null)
        {
            grid = container.gameObject.AddComponent<GridLayoutGroup>();
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            int childCount = Mathf.Max(1, container.childCount);
            grid.constraintCount = Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(childCount)));
        }
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

    private void RefreshGrantedCornerMarker(SkillSlotWidget widget)
    {
        if (widget == null || widget.grantedCornerMarker == null)
        {
            return;
        }

        Sprite cornerSprite = journeySkillGridBinding != null ? journeySkillGridBinding.grantedSkillCornerSprite : null;
        bool shouldShow = widget.isGranted && cornerSprite != null && !string.IsNullOrWhiteSpace(widget.skillId);

        widget.grantedCornerMarker.sprite = cornerSprite;
        widget.grantedCornerMarker.enabled = shouldShow;
        widget.grantedCornerMarker.gameObject.SetActive(shouldShow);

        RectTransform markerRect = widget.grantedCornerMarker.rectTransform;
        if (markerRect == null)
        {
            return;
        }

        Vector2 anchoredPosition = journeySkillGridBinding != null
            ? journeySkillGridBinding.grantedSkillCornerAnchoredPosition
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

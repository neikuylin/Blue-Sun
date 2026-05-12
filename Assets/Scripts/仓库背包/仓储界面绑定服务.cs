using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

internal sealed class 仓储界面绑定服务
{
    internal sealed class Context
    {
        public InventoryShortcutRuntimeBinder Owner;
        public int FixedStorageSlotCount;
        public List<InventoryShortcutRuntimeBinder.ItemSlotData> WarehouseData;
        public List<InventoryShortcutRuntimeBinder.ItemSlotData> BackpackData;
        public List<InventoryShortcutRuntimeBinder.ItemSlotData> ChestData;
        public List<InventoryShortcutRuntimeBinder.SlotWidget> WarehouseSlots;
        public List<InventoryShortcutRuntimeBinder.SlotWidget> BackpackSlots;
        public List<InventoryShortcutRuntimeBinder.SlotWidget> ChestSlots;
        public List<List<InventoryShortcutRuntimeBinder.SlotWidget>> ExtraBackpackSlots;
        public List<InventoryShortcutRuntimeBinder.SlotWidget> EquipmentSlots;
        public List<List<InventoryShortcutRuntimeBinder.SlotWidget>> ExtraEquipmentSlots;
        public InventoryShortcutRuntimeBinder.CategoryFilterBinding WarehouseFilter;
        public InventoryShortcutRuntimeBinder.CategoryFilterBinding BackpackFilter;
        public List<Action> CategoryFilterUnbindActions;
        public Action<RectTransform> CacheBackpackLayout;
        public Action RefreshWarehouseFilteredView;
        public Action RefreshBackpackFilteredView;
    }

    public void CollectStorageSlots(Context context)
    {
        context.WarehouseSlots.Clear();
        context.BackpackSlots.Clear();
        context.ChestSlots.Clear();
        context.ExtraBackpackSlots.Clear();

        物品格子区域绑定[] bindings = UnityEngine.Object.FindObjectsOfType<物品格子区域绑定>(true);
        for (int i = 0; i < bindings.Length; i++)
        {
            物品格子区域绑定 binding = bindings[i];
            if (binding == null)
            {
                continue;
            }

            RectTransform container = EnsureBoundSlotContainer(binding);
            if (container == null)
            {
                continue;
            }

            int targetCount = Mathf.Max(context.FixedStorageSlotCount, ResolveDataCount(context, binding.数据来源));
            EnsureBoundSlots(binding, container, targetCount);

            if (binding.数据来源 == 物品格子区域绑定.数据来源类型.仓库)
            {
                if (context.WarehouseSlots.Count == 0)
                {
                    CollectSlotsFromContainer(container, context.WarehouseSlots);
                    ApplyStorageRightClickTarget(context.WarehouseSlots, MapRightClickTarget(binding.右键拖拽目标));
                }

                continue;
            }

            if (binding.数据来源 == 物品格子区域绑定.数据来源类型.宝箱)
            {
                if (context.ChestSlots.Count == 0)
                {
                    CollectSlotsFromContainer(container, context.ChestSlots);
                    ApplyStorageRightClickTarget(context.ChestSlots, MapRightClickTarget(binding.右键拖拽目标));
                }

                continue;
            }

            if (context.BackpackSlots.Count == 0)
            {
                CollectSlotsFromContainer(container, context.BackpackSlots);
                ApplyStorageRightClickTarget(context.BackpackSlots, MapRightClickTarget(binding.右键拖拽目标));
                context.CacheBackpackLayout?.Invoke(container);
                continue;
            }

            List<InventoryShortcutRuntimeBinder.SlotWidget> extraGroup = new List<InventoryShortcutRuntimeBinder.SlotWidget>();
            CollectSlotsFromContainer(container, extraGroup);
            if (extraGroup.Count > 0)
            {
                ApplyStorageRightClickTarget(extraGroup, MapRightClickTarget(binding.右键拖拽目标));
                context.ExtraBackpackSlots.Add(extraGroup);
            }
        }
    }

    public void CollectEquipmentSlots(Context context)
    {
        context.EquipmentSlots.Clear();
        context.ExtraEquipmentSlots.Clear();

        装备面板绑定[] panelBindings = UnityEngine.Object.FindObjectsOfType<装备面板绑定>(true);
        for (int i = 0; i < panelBindings.Length; i++)
        {
            装备面板绑定 binding = panelBindings[i];
            if (binding == null)
            {
                continue;
            }

            List<InventoryShortcutRuntimeBinder.SlotWidget> collected = new List<InventoryShortcutRuntimeBinder.SlotWidget>();
            CollectEquipmentSlotsFromBinding(binding, collected);
            if (collected.Count == 0)
            {
                continue;
            }

            ApplyStorageRightClickTarget(collected, MapEquipmentReturnTarget(binding.ReturnTarget));
            if (context.EquipmentSlots.Count == 0)
            {
                context.EquipmentSlots.AddRange(collected);
                continue;
            }

            context.ExtraEquipmentSlots.Add(collected);
        }
    }

    public void BindDragRelays(Context context)
    {
        BindDragRelaysForList(context, context.WarehouseSlots, InventoryShortcutRuntimeBinder.SlotKind.Warehouse, InventoryShortcutRuntimeBinder.SlotSurface.Warehouse);
        BindDragRelaysForList(context, context.BackpackSlots, InventoryShortcutRuntimeBinder.SlotKind.Backpack, InventoryShortcutRuntimeBinder.SlotSurface.WarehouseBackpack);
        BindDragRelaysForList(context, context.ChestSlots, InventoryShortcutRuntimeBinder.SlotKind.Chest, InventoryShortcutRuntimeBinder.SlotSurface.WarehouseBackpack);
        for (int i = 0; i < context.ExtraBackpackSlots.Count; i++)
        {
            BindDragRelaysForList(context, context.ExtraBackpackSlots[i], InventoryShortcutRuntimeBinder.SlotKind.Backpack, InventoryShortcutRuntimeBinder.SlotSurface.WarehouseBackpack);
        }

        BindDragRelaysForList(context, context.EquipmentSlots, InventoryShortcutRuntimeBinder.SlotKind.Equipment, InventoryShortcutRuntimeBinder.SlotSurface.Equipment);
        for (int i = 0; i < context.ExtraEquipmentSlots.Count; i++)
        {
            BindDragRelaysForList(context, context.ExtraEquipmentSlots[i], InventoryShortcutRuntimeBinder.SlotKind.Equipment, InventoryShortcutRuntimeBinder.SlotSurface.Equipment);
        }
    }

    public void BindCategoryFilters(Context context)
    {
        BindCategoryFilterForPanel(
            FindCategoryFilterPanel("仓库面板"),
            context.WarehouseFilter,
            context.RefreshWarehouseFilteredView,
            context.CategoryFilterUnbindActions);
        BindCategoryFilterForPanel(
            FindCategoryFilterPanel("背包面板"),
            context.BackpackFilter,
            context.RefreshBackpackFilteredView,
            context.CategoryFilterUnbindActions);
    }

    private static InventoryShortcutRuntimeBinder.StorageRightClickTarget MapRightClickTarget(物品格子区域绑定.右键拖拽目标类型 target)
    {
        switch (target)
        {
            case 物品格子区域绑定.右键拖拽目标类型.背包:
                return InventoryShortcutRuntimeBinder.StorageRightClickTarget.Backpack;
            case 物品格子区域绑定.右键拖拽目标类型.目标ID装备栏:
                return InventoryShortcutRuntimeBinder.StorageRightClickTarget.TargetIdEquipment;
            case 物品格子区域绑定.右键拖拽目标类型.宝箱:
                return InventoryShortcutRuntimeBinder.StorageRightClickTarget.Chest;
            case 物品格子区域绑定.右键拖拽目标类型.仓库:
            default:
                return InventoryShortcutRuntimeBinder.StorageRightClickTarget.Warehouse;
        }
    }

    private static int ResolveDataCount(Context context, 物品格子区域绑定.数据来源类型 sourceType)
    {
        switch (sourceType)
        {
            case 物品格子区域绑定.数据来源类型.仓库:
                return context.WarehouseData != null ? context.WarehouseData.Count : 0;
            case 物品格子区域绑定.数据来源类型.宝箱:
                return context.ChestData != null ? context.ChestData.Count : 0;
            case 物品格子区域绑定.数据来源类型.背包:
            default:
                return context.BackpackData != null ? context.BackpackData.Count : 0;
        }
    }

    private static void ApplyStorageRightClickTarget(List<InventoryShortcutRuntimeBinder.SlotWidget> slots, InventoryShortcutRuntimeBinder.StorageRightClickTarget target)
    {
        if (slots == null)
        {
            return;
        }

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] == null)
            {
                continue;
            }

            slots[i].rightClickTarget = target;
        }
    }

    private static InventoryShortcutRuntimeBinder.StorageRightClickTarget MapEquipmentReturnTarget(装备面板绑定.回流目标类型 target)
    {
        return target == 装备面板绑定.回流目标类型.仓库
            ? InventoryShortcutRuntimeBinder.StorageRightClickTarget.Warehouse
            : InventoryShortcutRuntimeBinder.StorageRightClickTarget.Backpack;
    }

    private static RectTransform EnsureBoundSlotContainer(物品格子区域绑定 binding)
    {
        return binding != null ? binding.已绑定格子容器 : null;
    }

    private static void EnsureBoundSlots(物品格子区域绑定 binding, RectTransform container, int desiredCount)
    {
        if (binding == null || container == null)
        {
            return;
        }

        int normalizedCount = Mathf.Max(0, desiredCount);
        if (normalizedCount <= 0)
        {
            return;
        }

        GameObject template = ResolveBoundSlotTemplate(binding, container);
        if (template == null)
        {
            return;
        }

        int currentCount = CountRecognizableSlots(container);
        if (currentCount == normalizedCount)
        {
            return;
        }

        for (int i = container.childCount - 1; i >= 0; i--)
        {
            UnityEngine.Object.Destroy(container.GetChild(i).gameObject);
        }

        for (int i = 0; i < normalizedCount; i++)
        {
            GameObject go = UnityEngine.Object.Instantiate(template, container, false);
            go.name = "格子 (" + (i + 1) + ")";
            SetActiveRecursively(go, true);
            if (go.transform is RectTransform rt)
            {
                rt.localScale = Vector3.one;
            }
        }
    }

    private static GameObject ResolveBoundSlotTemplate(物品格子区域绑定 binding, RectTransform container)
    {
        if (binding != null && binding.格子模板 != null)
        {
            return binding.格子模板;
        }

        if (container == null)
        {
            return null;
        }

        for (int i = 0; i < container.childCount; i++)
        {
            Transform child = container.GetChild(i);
            if (child == null)
            {
                continue;
            }

            bool looksLikeSlot = child.name.Contains("格子");
            bool hasButton = child.GetComponent<Button>() != null;
            if (looksLikeSlot || hasButton)
            {
                return child.gameObject;
            }
        }

        return null;
    }

    private static int CountRecognizableSlots(RectTransform container)
    {
        if (container == null)
        {
            return 0;
        }

        int count = 0;
        for (int i = 0; i < container.childCount; i++)
        {
            Transform child = container.GetChild(i);
            if (child == null)
            {
                continue;
            }

            if (child.name.Contains("格子") || child.GetComponent<Button>() != null)
            {
                count++;
            }
        }

        return count;
    }

    private static void SetActiveRecursively(GameObject target, bool active)
    {
        if (target == null)
        {
            return;
        }

        target.SetActive(active);
        Transform root = target.transform;
        for (int i = 0; i < root.childCount; i++)
        {
            SetActiveRecursively(root.GetChild(i).gameObject, active);
        }
    }

    private static void CollectSlotsFromContainer(Transform container, List<InventoryShortcutRuntimeBinder.SlotWidget> target)
    {
        target.Clear();
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

            bool looksLikeSlot = child.name.Contains("格子");
            Button button = child.GetComponent<Button>();
            if (!looksLikeSlot && button == null)
            {
                continue;
            }

            RectTransform backgroundAnchor = FindNamedRectTransform(child, "物品底背景");
            RectTransform iconAnchor = FindNamedRectTransform(child, "物品图标");
            Image icon = ResolveSlotDisplayImage(child, iconAnchor);
            if (icon == null)
            {
                continue;
            }

            target.Add(new InventoryShortcutRuntimeBinder.SlotWidget
            {
                root = child,
                button = button,
                backgroundAnchor = backgroundAnchor,
                iconAnchor = iconAnchor,
                icon = icon,
                iconIsRoot = icon.transform == child,
                iconOriginalColor = icon.color,
                iconOriginalSprite = icon.sprite
            });
        }
    }

    private static void CollectEquipmentSlotsFromBinding(装备面板绑定 binding, List<InventoryShortcutRuntimeBinder.SlotWidget> target)
    {
        target.Clear();
        if (binding == null)
        {
            return;
        }

        AddEquipmentSlotWidget(target, binding.HelmetSlot, ItemDatabase.EquipmentSlotType.Helmet);
        AddEquipmentSlotWidget(target, binding.ArmorSlot, ItemDatabase.EquipmentSlotType.Armor);
        AddEquipmentSlotWidget(target, binding.GlovesSlot, ItemDatabase.EquipmentSlotType.Gloves);
        AddEquipmentSlotWidget(target, binding.ShoesSlot, ItemDatabase.EquipmentSlotType.Shoes);
        AddEquipmentSlotWidget(target, binding.LegArmorSlot, ItemDatabase.EquipmentSlotType.LegArmor);
        AddEquipmentSlotWidget(target, binding.AccessorySlot, ItemDatabase.EquipmentSlotType.Accessory);
        AddEquipmentSlotWidget(target, binding.MainHandSlot, ItemDatabase.EquipmentSlotType.MainHand);
        AddEquipmentSlotWidget(target, binding.OffHandSlot, ItemDatabase.EquipmentSlotType.OffHand);
    }

    private static void AddEquipmentSlotWidget(List<InventoryShortcutRuntimeBinder.SlotWidget> target, RectTransform slotRoot, ItemDatabase.EquipmentSlotType slotType)
    {
        if (target == null || slotRoot == null)
        {
            return;
        }

        Button button = slotRoot.GetComponent<Button>();
        RectTransform backgroundAnchor = FindNamedRectTransform(slotRoot, "物品底背景");
        RectTransform iconAnchor = FindNamedRectTransform(slotRoot, "物品图标");
        Image icon = ResolveSlotDisplayImage(slotRoot, iconAnchor) ?? FindEquipmentIconImage(slotRoot);
        if (icon == null)
        {
            return;
        }

        target.Add(new InventoryShortcutRuntimeBinder.SlotWidget
        {
            root = slotRoot,
            button = button,
            backgroundAnchor = backgroundAnchor,
            iconAnchor = iconAnchor,
            icon = icon,
            iconIsRoot = icon.transform == slotRoot,
            iconOriginalColor = icon.color,
            iconOriginalSprite = icon.sprite,
            equipmentSlotType = slotType
        });
    }

    private static Image FindBestIconImage(RectTransform slotRoot)
    {
        Image[] images = slotRoot.GetComponentsInChildren<Image>(true);
        Image rootImage = slotRoot.GetComponent<Image>();

        for (int i = 0; i < images.Length; i++)
        {
            Image img = images[i];
            if (img == null)
            {
                continue;
            }

            string name = img.gameObject.name;
            if (name.Contains("图标") || name.Contains("Icon") || name.Contains("icon"))
            {
                return img;
            }
        }

        for (int i = 0; i < images.Length; i++)
        {
            Image img = images[i];
            if (img != null && img != rootImage)
            {
                return img;
            }
        }

        return rootImage;
    }

    private static Image FindEquipmentIconImage(RectTransform slotRoot)
    {
        Transform explicitIcon = FindChildByName(slotRoot, "物品图标") ?? FindDescendantByName(slotRoot, "物品图标");
        if (explicitIcon != null)
        {
            Image explicitImage = explicitIcon.GetComponent<Image>();
            if (explicitImage != null)
            {
                return explicitImage;
            }
        }

        return FindBestIconImage(slotRoot);
    }

    private static Image ResolveSlotDisplayImage(RectTransform slotRoot, RectTransform iconAnchor)
    {
        if (iconAnchor != null)
        {
            Image anchorImage = iconAnchor.GetComponent<Image>();
            if (anchorImage != null)
            {
                return anchorImage;
            }
        }

        Image rootImage = slotRoot != null ? slotRoot.GetComponent<Image>() : null;
        if (rootImage != null)
        {
            return rootImage;
        }

        return slotRoot != null ? FindBestIconImage(slotRoot) : null;
    }

    private static RectTransform FindNamedRectTransform(RectTransform root, string childName)
    {
        Transform target = FindChildByName(root, childName) ?? FindDescendantByName(root, childName);
        return target as RectTransform;
    }

    private static void BindDragRelaysForList(
        Context context,
        List<InventoryShortcutRuntimeBinder.SlotWidget> slots,
        InventoryShortcutRuntimeBinder.SlotKind kind,
        InventoryShortcutRuntimeBinder.SlotSurface surface)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            RectTransform root = slots[i].root;
            if (root == null)
            {
                continue;
            }

            InventoryShortcutRuntimeBinder.SlotDragRelay relay = root.GetComponent<InventoryShortcutRuntimeBinder.SlotDragRelay>();
            if (relay == null)
            {
                relay = root.gameObject.AddComponent<InventoryShortcutRuntimeBinder.SlotDragRelay>();
            }

            relay.Configure(context.Owner, kind, surface, i);
        }
    }

    private static Transform FindCategoryFilterPanel(string panelName)
    {
        if (string.IsNullOrWhiteSpace(panelName))
        {
            return null;
        }

        物品格子区域绑定.数据来源类型 sourceType = string.Equals(panelName, "仓库面板", StringComparison.Ordinal)
            ? 物品格子区域绑定.数据来源类型.仓库
            : 物品格子区域绑定.数据来源类型.背包;

        物品格子区域绑定[] bindings = UnityEngine.Object.FindObjectsOfType<物品格子区域绑定>(true);
        for (int i = 0; i < bindings.Length; i++)
        {
            物品格子区域绑定 binding = bindings[i];
            if (binding == null || binding.数据来源 != sourceType)
            {
                continue;
            }

            Transform panelRoot = FindAncestorByName(binding.已绑定格子容器, panelName);
            if (panelRoot != null)
            {
                return panelRoot;
            }
        }

        return null;
    }

    private static void BindCategoryFilterForPanel(
        Transform panelRoot,
        InventoryShortcutRuntimeBinder.CategoryFilterBinding binding,
        Action refreshAction,
        List<Action> unbindActions)
    {
        binding.panelRoot = panelRoot;
        binding.toggles.Clear();
        binding.selectedCategories.Clear();

        if (panelRoot == null || refreshAction == null)
        {
            return;
        }

        BindCategoryToggle(panelRoot, binding, "装备", refreshAction, unbindActions);
        BindCategoryToggle(panelRoot, binding, "消耗品", refreshAction, unbindActions);
        BindCategoryToggle(panelRoot, binding, "材料", refreshAction, unbindActions);
        BindCategoryToggle(panelRoot, binding, "补给", refreshAction, unbindActions);

        RebuildSelectedCategories(binding);
    }

    private static void BindCategoryToggle(
        Transform panelRoot,
        InventoryShortcutRuntimeBinder.CategoryFilterBinding binding,
        string categoryName,
        Action refreshAction,
        List<Action> unbindActions)
    {
        Toggle toggle = FindCategoryToggle(panelRoot, categoryName);
        if (toggle == null)
        {
            return;
        }

        if (!binding.toggles.Contains(toggle))
        {
            binding.toggles.Add(toggle);
        }

        UnityEngine.Events.UnityAction<bool> onChanged = _ =>
        {
            RebuildSelectedCategories(binding);
            refreshAction();
        };

        toggle.onValueChanged.AddListener(onChanged);
        unbindActions.Add(() =>
        {
            if (toggle != null)
            {
                toggle.onValueChanged.RemoveListener(onChanged);
            }
        });
    }

    private static Toggle FindCategoryToggle(Transform panelRoot, string categoryName)
    {
        if (panelRoot == null || string.IsNullOrWhiteSpace(categoryName))
        {
            return null;
        }

        Transform header = FindChildByName(panelRoot, "头部区域") ?? FindDescendantByName(panelRoot, "头部区域") ?? panelRoot;
        Transform namedTransform = FindChildByName(header, categoryName) ?? FindDescendantByName(header, categoryName);
        if (namedTransform != null)
        {
            Toggle directToggle = namedTransform.GetComponent<Toggle>();
            if (directToggle != null)
            {
                return directToggle;
            }

            Toggle childToggle = namedTransform.GetComponentInChildren<Toggle>(true);
            if (childToggle != null)
            {
                return childToggle;
            }
        }

        Toggle[] toggles = header.GetComponentsInChildren<Toggle>(true);
        for (int i = 0; i < toggles.Length; i++)
        {
            Toggle toggle = toggles[i];
            if (toggle == null)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(toggle.name) && toggle.name.IndexOf(categoryName, StringComparison.Ordinal) >= 0)
            {
                return toggle;
            }

            if (GetToggleCategoryLabel(toggle).IndexOf(categoryName, StringComparison.Ordinal) >= 0)
            {
                return toggle;
            }
        }

        return null;
    }

    private static Transform FindAncestorByName(Transform child, string ancestorName)
    {
        if (child == null || string.IsNullOrWhiteSpace(ancestorName))
        {
            return null;
        }

        Transform current = child;
        while (current != null)
        {
            if (string.Equals(current.name, ancestorName, StringComparison.Ordinal))
            {
                return current;
            }

            current = current.parent;
        }

        return null;
    }

    private static void RebuildSelectedCategories(InventoryShortcutRuntimeBinder.CategoryFilterBinding binding)
    {
        if (binding == null)
        {
            return;
        }

        binding.selectedCategories.Clear();
        for (int i = 0; i < binding.toggles.Count; i++)
        {
            Toggle toggle = binding.toggles[i];
            if (toggle == null || !toggle.isOn)
            {
                continue;
            }

            if (TryResolveCategoryFromLabel(GetToggleCategoryLabel(toggle), out ItemDatabase.ItemCategory category))
            {
                binding.selectedCategories.Add(category);
            }
        }
    }

    private static string GetToggleCategoryLabel(Toggle toggle)
    {
        if (toggle == null)
        {
            return string.Empty;
        }

        TMP_Text tmpText = toggle.GetComponentInChildren<TMP_Text>(true);
        if (tmpText != null && !string.IsNullOrWhiteSpace(tmpText.text))
        {
            return tmpText.text;
        }

        Text text = toggle.GetComponentInChildren<Text>(true);
        if (text != null && !string.IsNullOrWhiteSpace(text.text))
        {
            return text.text;
        }

        return toggle.name ?? string.Empty;
    }

    private static bool TryResolveCategoryFromLabel(string label, out ItemDatabase.ItemCategory category)
    {
        category = ItemDatabase.ItemCategory.Consumable;
        if (string.IsNullOrWhiteSpace(label))
        {
            return false;
        }

        if (label.IndexOf("装备", StringComparison.Ordinal) >= 0)
        {
            category = ItemDatabase.ItemCategory.Equipment;
            return true;
        }

        if (label.IndexOf("消耗品", StringComparison.Ordinal) >= 0)
        {
            category = ItemDatabase.ItemCategory.Consumable;
            return true;
        }

        if (label.IndexOf("材料", StringComparison.Ordinal) >= 0)
        {
            category = ItemDatabase.ItemCategory.Material;
            return true;
        }

        if (label.IndexOf("补给", StringComparison.Ordinal) >= 0)
        {
            category = ItemDatabase.ItemCategory.Supply;
            return true;
        }

        return false;
    }

    private static Transform FindChildByName(Transform parent, string childName)
    {
        if (parent == null || string.IsNullOrEmpty(childName))
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

    private static Transform FindDescendantByName(Transform parent, string targetName)
    {
        if (parent == null || string.IsNullOrWhiteSpace(targetName))
        {
            return null;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child == null)
            {
                continue;
            }

            if (string.Equals(child.name, targetName, StringComparison.Ordinal))
            {
                return child;
            }

            Transform nested = FindDescendantByName(child, targetName);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }
}

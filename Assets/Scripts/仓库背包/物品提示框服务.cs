using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

internal sealed class 物品提示框服务
{
    internal sealed class State
    {
        public RectTransform itemTooltipRoot;
        public RectTransform itemTooltipDetailRoot;
        public RectTransform itemTooltipLowerBackgroundRoot;
        public RectTransform itemTooltipTextContentRoot;
        public RectTransform itemTooltipExpandHintRoot;
        public TMP_Text itemTooltipLowerContentText;
        public Image itemTooltipDetailBackgroundImage;
        public Image itemTooltipItemIconImage;
        public TMP_Text itemTooltipItemNameText;
        public TMP_Text itemTooltipQualityText;
        public TMP_Text itemTooltipWeaponCategoryText;
        public TMP_Text itemTooltipOwnerText;
        public TMP_Text itemTooltipAttackPowerText;
        public TMP_Text itemTooltipFixedDamageText;
        public TMP_Text itemTooltipAttributeMultiplierText;
        public TMP_Text itemTooltipDescriptionText;
        public TMP_Text itemTooltipGrantedSkillsText;
        public RectTransform itemTooltipGrantedSkillsIconRoot;
        public 武器详情视图 itemTooltipView;
        public InventoryShortcutRuntimeBinder.SlotRef hoveredTooltipSlot;
        public float pendingTooltipShownAt;
        public GameObject runtimeTooltipRootInstance;
        public GameObject runtimeTooltipSourcePrefab;
    }

    internal sealed class Context
    {
        public Func<ItemDatabase.WeaponCategory, GameObject> GetWeaponTooltipPrefab;
        public Func<ItemDatabase.ItemQuality, GameObject> GetQualityBackgroundPrefab;
        public Func<Transform> FindTooltipParent;
        public Func<Transform, string, Transform> FindChildByName;
        public Func<Transform, string, Transform> FindDescendantByName;
        public Func<string, BattleSkillDatabase.SkillEntry> FindSkillEntry;
        public Func<ItemDatabase.ItemEntry, string> ResolveItemDisplayName;
        public Func<ItemDatabase.ItemQuality, string> GetItemQualityDisplayName;
        public Func<ItemDatabase.WeaponCategory, string> GetWeaponCategoryDisplayName;
        public Func<ItemDatabase.ItemEntry, InventoryShortcutRuntimeBinder.SlotRef, string> GetTooltipOwnerDisplayText;
        public Func<ItemDatabase.ItemEntry, InventoryShortcutRuntimeBinder.SlotRef, TMP_Text, string> GetTooltipAttackPowerText;
        public Func<ItemDatabase.ItemEntry, string> GetFixedDamageDisplayText;
        public Func<ItemDatabase.ItemEntry, string> GetAttributeMultiplierDisplayText;
        public Func<ItemDatabase.ItemEntry, string> BuildTooltipLowerContentText;
        public Func<ItemDatabase.ItemEntry, Sprite> ResolveTooltipItemIconSprite;
        public Func<ItemDatabase.ItemEntry, Image, Vector2> ResolveTooltipItemIconSize;
        public Func<Material> EnsureItemTooltipIconFadeMaterial;
        public Action CancelHover;
        public Func<bool> ShouldShowLowerBackground;
        public Vector3 ItemTooltipScale;
        public Vector3 ItemTooltipIconScale;
    }

    public void CacheTooltip(State state, Context context, ItemDatabase.WeaponCategory weaponCategory, bool resetTooltipState)
    {
        state.itemTooltipRoot = null;
        state.itemTooltipView = null;
        EnsureTooltipRootsFromDatabase(state, context, weaponCategory);
        ResolveTooltipView(state);
        if (resetTooltipState)
        {
            HideTooltip(state, context);
        }
    }

    public void ShowTooltip(
        State state,
        Context context,
        InventoryShortcutRuntimeBinder.SlotWidget widget,
        InventoryShortcutRuntimeBinder.SlotRef slot,
        ItemDatabase.ItemEntry entry)
    {
        if (entry == null)
        {
            return;
        }

        CacheTooltip(state, context, entry.weaponCategory, false);
        if (widget == null || widget.root == null || state.itemTooltipRoot == null)
        {
            return;
        }

        if (state.itemTooltipView == null)
        {
            Debug.LogWarning("武器提示框显示失败：单_双手武器详情 prefab 根节点缺少“武器详情视图”组件。");
            return;
        }

        state.hoveredTooltipSlot = slot;
        state.itemTooltipView.刷新(BuildSnapshot(state, context, slot, entry));
        state.itemTooltipRoot.localScale = context.ItemTooltipScale;
        PositionTooltip(state.itemTooltipRoot);
        state.itemTooltipRoot.gameObject.SetActive(true);
        state.itemTooltipRoot.SetAsLastSibling();
        UpdateTooltipLowerBackgroundState(state, context);
    }

    public void HideTooltip(State state, Context context)
    {
        state.hoveredTooltipSlot = default;
        state.pendingTooltipShownAt = 0f;
        state.itemTooltipView?.清空运行时内容();
        context.CancelHover?.Invoke();
        if (state.itemTooltipRoot != null)
        {
            state.itemTooltipRoot.gameObject.SetActive(false);
        }
    }

    public void UpdateTooltipLowerBackgroundState(State state, Context context)
    {
        bool shouldShow = state.itemTooltipRoot != null &&
            state.itemTooltipRoot.gameObject.activeSelf &&
            context.ShouldShowLowerBackground != null &&
            context.ShouldShowLowerBackground();
        state.itemTooltipView?.设置下背景显示(shouldShow);
    }

    private static 武器详情视图.Snapshot BuildSnapshot(
        State state,
        Context context,
        InventoryShortcutRuntimeBinder.SlotRef slot,
        ItemDatabase.ItemEntry entry)
    {
        return new 武器详情视图.Snapshot
        {
            背景Sprite = ResolveQualityBackgroundSprite(context, entry.quality),
            物品图标Sprite = context.ResolveTooltipItemIconSprite != null ? context.ResolveTooltipItemIconSprite(entry) : null,
            物品图标尺寸 = context.ResolveTooltipItemIconSize != null ? context.ResolveTooltipItemIconSize(entry, state.itemTooltipItemIconImage) : Vector2.zero,
            物品图标缩放 = context.ItemTooltipIconScale,
            物品图标材质 = context.EnsureItemTooltipIconFadeMaterial != null ? context.EnsureItemTooltipIconFadeMaterial() : null,
            物品名字 = context.ResolveItemDisplayName != null ? context.ResolveItemDisplayName(entry) : string.Empty,
            品质 = context.GetItemQualityDisplayName != null ? context.GetItemQualityDisplayName(entry.quality) : string.Empty,
            武器分类 = context.GetWeaponCategoryDisplayName != null ? context.GetWeaponCategoryDisplayName(entry.weaponCategory) : string.Empty,
            装备者 = context.GetTooltipOwnerDisplayText != null ? context.GetTooltipOwnerDisplayText(entry, slot) : string.Empty,
            攻击力 = context.GetTooltipAttackPowerText != null ? context.GetTooltipAttackPowerText(entry, slot, state.itemTooltipAttackPowerText) : string.Empty,
            固定伤害 = context.GetFixedDamageDisplayText != null ? context.GetFixedDamageDisplayText(entry) : string.Empty,
            属性加成 = context.GetAttributeMultiplierDisplayText != null ? context.GetAttributeMultiplierDisplayText(entry) : string.Empty,
            文本介绍 = entry.description ?? string.Empty,
            下文本内容 = context.BuildTooltipLowerContentText != null ? context.BuildTooltipLowerContentText(entry) : string.Empty,
            附带技能图标Sprites = BuildGrantedSkillIconSprites(context, entry)
        };
    }

    private static Sprite ResolveQualityBackgroundSprite(Context context, ItemDatabase.ItemQuality quality)
    {
        GameObject backgroundPrefab = context.GetQualityBackgroundPrefab != null ? context.GetQualityBackgroundPrefab(quality) : null;
        if (backgroundPrefab == null)
        {
            return null;
        }

        Image image = backgroundPrefab.GetComponent<Image>() ?? backgroundPrefab.GetComponentInChildren<Image>(true);
        return image != null ? image.sprite : null;
    }

    private static IReadOnlyList<Sprite> BuildGrantedSkillIconSprites(Context context, ItemDatabase.ItemEntry entry)
    {
        List<Sprite> icons = new List<Sprite>();
        if (entry == null || entry.grantedSkillIds == null || entry.grantedSkillIds.Count == 0)
        {
            return icons;
        }

        for (int i = 0; i < entry.grantedSkillIds.Count; i++)
        {
            string skillId = entry.grantedSkillIds[i];
            if (string.IsNullOrWhiteSpace(skillId))
            {
                continue;
            }

            BattleSkillDatabase.SkillEntry skillEntry = context.FindSkillEntry != null ? context.FindSkillEntry(skillId) : null;
            if (skillEntry != null && skillEntry.icon != null)
            {
                icons.Add(skillEntry.icon);
            }
        }

        return icons;
    }

    private static void PositionTooltip(RectTransform tooltip)
    {
        if (tooltip == null)
        {
            return;
        }

        RectTransform parentRect = tooltip.parent as RectTransform;
        Canvas canvas = tooltip.GetComponentInParent<Canvas>();
        Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        if (parentRect != null &&
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, Input.mousePosition, uiCamera, out Vector2 localPoint))
        {
            Vector2 pivotOffset = new Vector2(
                tooltip.rect.width * tooltip.pivot.x,
                tooltip.rect.height * tooltip.pivot.y);
            tooltip.anchoredPosition = localPoint + pivotOffset;
        }
    }

    private void EnsureTooltipRootsFromDatabase(State state, Context context, ItemDatabase.WeaponCategory weaponCategory)
    {
        Transform parent = context.FindTooltipParent != null ? context.FindTooltipParent() : null;
        if (parent == null)
        {
            return;
        }

        GameObject tooltipPrefab = context.GetWeaponTooltipPrefab != null ? context.GetWeaponTooltipPrefab(weaponCategory) : null;
        state.runtimeTooltipRootInstance = EnsureTooltipInstance(
            state.runtimeTooltipRootInstance,
            ref state.runtimeTooltipSourcePrefab,
            tooltipPrefab,
            parent,
            "物品内容");
        state.itemTooltipRoot = state.runtimeTooltipRootInstance != null ? state.runtimeTooltipRootInstance.transform as RectTransform : null;
    }

    private static GameObject EnsureTooltipInstance(GameObject currentInstance, ref GameObject sourcePrefab, GameObject prefab, Transform parent, string runtimeName)
    {
        if (currentInstance != null && sourcePrefab == prefab)
        {
            return currentInstance;
        }

        if (currentInstance != null)
        {
            UnityEngine.Object.Destroy(currentInstance);
            currentInstance = null;
        }

        if (prefab == null || parent == null)
        {
            sourcePrefab = prefab;
            return null;
        }

        GameObject instance = UnityEngine.Object.Instantiate(prefab, parent, false);
        instance.name = runtimeName;
        instance.SetActive(false);
        sourcePrefab = prefab;
        return instance;
    }

    private static void ResolveTooltipView(State state)
    {
        if (state.itemTooltipRoot == null)
        {
            ClearCachedViewReferences(state);
            return;
        }

        state.itemTooltipView = state.itemTooltipRoot.GetComponent<武器详情视图>();
        if (state.itemTooltipView == null)
        {
            ClearCachedViewReferences(state);
            Debug.LogWarning("武器提示框绑定失败：单_双手武器详情 prefab 根节点缺少“武器详情视图”组件。");
            return;
        }

        state.itemTooltipDetailRoot = state.itemTooltipView.根节点;
        state.itemTooltipLowerBackgroundRoot = state.itemTooltipView.下背景节点;
        state.itemTooltipTextContentRoot = state.itemTooltipView.文本内容节点;
        state.itemTooltipExpandHintRoot = state.itemTooltipView.展开提示节点;
        state.itemTooltipLowerContentText = state.itemTooltipView.下文本内容组件;
        state.itemTooltipDetailBackgroundImage = state.itemTooltipView.背景图组件;
        state.itemTooltipItemIconImage = state.itemTooltipView.物品图标组件;
        state.itemTooltipItemNameText = state.itemTooltipView.物品名字文本组件;
        state.itemTooltipQualityText = state.itemTooltipView.品质文本组件;
        state.itemTooltipWeaponCategoryText = state.itemTooltipView.武器分类文本组件;
        state.itemTooltipOwnerText = state.itemTooltipView.装备者文本组件;
        state.itemTooltipAttackPowerText = state.itemTooltipView.攻击力文本组件;
        state.itemTooltipFixedDamageText = state.itemTooltipView.固定伤害文本组件;
        state.itemTooltipAttributeMultiplierText = state.itemTooltipView.属性加成文本组件;
        state.itemTooltipDescriptionText = state.itemTooltipView.文本介绍文本组件;
        state.itemTooltipGrantedSkillsText = state.itemTooltipView.附带技能文本组件;
        state.itemTooltipGrantedSkillsIconRoot = state.itemTooltipView.附带技能图标区域节点;
    }

    private static void ClearCachedViewReferences(State state)
    {
        state.itemTooltipDetailRoot = null;
        state.itemTooltipLowerBackgroundRoot = null;
        state.itemTooltipTextContentRoot = null;
        state.itemTooltipExpandHintRoot = null;
        state.itemTooltipLowerContentText = null;
        state.itemTooltipDetailBackgroundImage = null;
        state.itemTooltipItemIconImage = null;
        state.itemTooltipItemNameText = null;
        state.itemTooltipQualityText = null;
        state.itemTooltipWeaponCategoryText = null;
        state.itemTooltipOwnerText = null;
        state.itemTooltipAttackPowerText = null;
        state.itemTooltipFixedDamageText = null;
        state.itemTooltipAttributeMultiplierText = null;
        state.itemTooltipDescriptionText = null;
        state.itemTooltipGrantedSkillsText = null;
        state.itemTooltipGrantedSkillsIconRoot = null;
    }
}

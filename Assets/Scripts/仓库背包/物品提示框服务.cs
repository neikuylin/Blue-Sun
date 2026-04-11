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
        public readonly List<GameObject> itemTooltipGrantedSkillIcons = new List<GameObject>();
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
        public Func<string, TMP_Text> FindTooltipTextByName;
        public Func<string, Transform> FindTransformByPath;
        public Func<string, BattleSkillDatabase.SkillEntry> FindSkillEntry;
        public Func<ItemDatabase.ItemEntry, string> ResolveItemDisplayName;
        public Func<ItemDatabase.ItemQuality, string> GetItemQualityDisplayName;
        public Func<ItemDatabase.WeaponCategory, string> GetWeaponCategoryDisplayName;
        public Func<ItemDatabase.ItemEntry, InventoryShortcutRuntimeBinder.SlotRef, string> GetTooltipOwnerDisplayText;
        public Action<ItemDatabase.ItemEntry, InventoryShortcutRuntimeBinder.SlotRef, TMP_Text> SetTooltipAttackPowerText;
        public Func<ItemDatabase.ItemEntry, string> GetFixedDamageDisplayText;
        public Func<ItemDatabase.ItemEntry, string> GetAttributeMultiplierDisplayText;
        public Func<ItemDatabase.ItemEntry, string> BuildTooltipLowerContentText;
        public Func<ItemDatabase.ItemEntry, Sprite> ResolveTooltipItemIconSprite;
        public Func<ItemDatabase.ItemEntry, Vector2> ResolveTooltipItemIconSize;
        public Func<Material> EnsureItemTooltipIconFadeMaterial;
        public Action CancelHover;
        public Func<bool> ShouldShowLowerBackground;
        public Func<Canvas> FindAnyCanvas;
        public Vector3 ItemTooltipScale;
        public Vector3 ItemTooltipIconScale;
    }

    public void CacheTooltip(State state, Context context, ItemDatabase.WeaponCategory weaponCategory, bool resetTooltipState)
    {
        state.itemTooltipRoot = null;
        EnsureTooltipRootsFromDatabase(state, context, weaponCategory);
        state.itemTooltipDetailRoot = ResolveItemTooltipDetailRoot(state, context, state.itemTooltipRoot, weaponCategory);
        state.itemTooltipLowerBackgroundRoot =
            context.FindChildByName(state.itemTooltipDetailRoot, "下背景") as RectTransform ??
            context.FindDescendantByName(state.itemTooltipDetailRoot, "下背景") as RectTransform;
        Transform backgroundRoot = context.FindChildByName(state.itemTooltipDetailRoot, "背景") ?? context.FindDescendantByName(state.itemTooltipDetailRoot, "背景");
        state.itemTooltipDetailBackgroundImage = backgroundRoot != null ? backgroundRoot.GetComponent<Image>() : null;
        Transform iconRoot = context.FindChildByName(state.itemTooltipDetailRoot, "物品图标") ?? context.FindDescendantByName(state.itemTooltipDetailRoot, "物品图标");
        state.itemTooltipItemIconImage = iconRoot != null ? iconRoot.GetComponent<Image>() : null;
        Transform textContentRoot = context.FindChildByName(state.itemTooltipDetailRoot, "文本内容") ?? context.FindDescendantByName(state.itemTooltipDetailRoot, "文本内容");
        state.itemTooltipTextContentRoot = textContentRoot as RectTransform;
        state.itemTooltipExpandHintRoot = textContentRoot != null
            ? (context.FindChildByName(textContentRoot, "展开提示") ?? context.FindDescendantByName(textContentRoot, "展开提示")) as RectTransform
            : null;
        state.itemTooltipLowerContentText = FindTooltipText(context, state.itemTooltipLowerBackgroundRoot, "下文本内容");
        state.itemTooltipItemNameText = FindTooltipText(context, textContentRoot, "物品名字");
        state.itemTooltipQualityText = FindTooltipText(context, textContentRoot, "品质");
        state.itemTooltipWeaponCategoryText = FindTooltipText(context, textContentRoot, "武器分类");
        state.itemTooltipOwnerText = FindTooltipText(context, textContentRoot, "装备者");
        state.itemTooltipAttackPowerText = FindTooltipText(context, textContentRoot, "攻击力");
        state.itemTooltipFixedDamageText = FindTooltipText(context, textContentRoot, "固定伤害");
        state.itemTooltipAttributeMultiplierText = FindTooltipText(context, textContentRoot, "属性加成");
        state.itemTooltipDescriptionText = FindTooltipText(context, textContentRoot, "文本介绍");
        state.itemTooltipGrantedSkillsText = FindTooltipText(context, textContentRoot, "附带技能");
        Transform grantedSkillRoot = state.itemTooltipGrantedSkillsText != null
            ? (context.FindChildByName(state.itemTooltipGrantedSkillsText.transform, "技能区域") ?? context.FindDescendantByName(state.itemTooltipGrantedSkillsText.transform, "技能区域"))
            : null;
        state.itemTooltipGrantedSkillsIconRoot = grantedSkillRoot as RectTransform;
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

        state.hoveredTooltipSlot = slot;
        SetTooltipItemIcon(state, context, entry);
        SetTooltipText(state.itemTooltipItemNameText, context.ResolveItemDisplayName(entry));
        SetTooltipText(state.itemTooltipQualityText, context.GetItemQualityDisplayName(entry.quality));
        SetTooltipText(state.itemTooltipWeaponCategoryText, context.GetWeaponCategoryDisplayName(entry.weaponCategory));
        SetTooltipText(state.itemTooltipOwnerText, context.GetTooltipOwnerDisplayText(entry, slot));
        context.SetTooltipAttackPowerText?.Invoke(entry, slot, state.itemTooltipAttackPowerText);
        SetTooltipText(state.itemTooltipFixedDamageText, context.GetFixedDamageDisplayText(entry));
        SetTooltipText(state.itemTooltipAttributeMultiplierText, context.GetAttributeMultiplierDisplayText(entry));
        SetTooltipText(state.itemTooltipDescriptionText, entry.description ?? string.Empty);
        SetTooltipText(state.itemTooltipGrantedSkillsText, "附带技能：");
        SetTooltipLowerContentText(state, context, entry);
        RebuildTooltipGrantedSkillIcons(state, context, entry);
        RefreshTooltipQualityBackground(state, context, entry.quality);
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
        ClearTooltipGrantedSkillIcons(state);
        context.CancelHover?.Invoke();
        SetTooltipLowerBackgroundVisible(state, false);
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
        SetTooltipLowerBackgroundVisible(state, shouldShow);
    }

    private void SetTooltipLowerBackgroundVisible(State state, bool visible)
    {
        if (state.itemTooltipLowerBackgroundRoot == null)
        {
            SetTooltipExpandHintVisible(state, !visible);
            return;
        }

        if (state.itemTooltipLowerBackgroundRoot.gameObject.activeSelf == visible)
        {
            SetTooltipExpandHintVisible(state, !visible);
            return;
        }

        state.itemTooltipLowerBackgroundRoot.gameObject.SetActive(visible);
        SetTooltipExpandHintVisible(state, !visible);
    }

    private void SetTooltipExpandHintVisible(State state, bool visible)
    {
        if (state.itemTooltipExpandHintRoot == null)
        {
            return;
        }

        if (state.itemTooltipExpandHintRoot.gameObject.activeSelf == visible)
        {
            return;
        }

        state.itemTooltipExpandHintRoot.gameObject.SetActive(visible);
    }

    private void RefreshTooltipQualityBackground(State state, Context context, ItemDatabase.ItemQuality quality)
    {
        GameObject backgroundPrefab = context.GetQualityBackgroundPrefab != null ? context.GetQualityBackgroundPrefab(quality) : null;
        if (backgroundPrefab == null)
        {
            if (state.itemTooltipDetailBackgroundImage != null)
            {
                state.itemTooltipDetailBackgroundImage.sprite = null;
                state.itemTooltipDetailBackgroundImage.enabled = false;
            }

            return;
        }

        Sprite targetSprite = null;
        Image image = backgroundPrefab.GetComponent<Image>() ?? backgroundPrefab.GetComponentInChildren<Image>(true);
        if (image != null)
        {
            targetSprite = image.sprite;
        }

        if (state.itemTooltipDetailBackgroundImage != null)
        {
            state.itemTooltipDetailBackgroundImage.sprite = targetSprite;
            state.itemTooltipDetailBackgroundImage.enabled = targetSprite != null;
        }
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

    private static void SetTooltipText(TMP_Text text, string value)
    {
        if (text != null)
        {
            text.text = value ?? string.Empty;
        }
    }

    private static void SetTooltipLowerContentText(State state, Context context, ItemDatabase.ItemEntry entry)
    {
        if (state.itemTooltipLowerContentText == null)
        {
            return;
        }

        string value = context.BuildTooltipLowerContentText != null ? context.BuildTooltipLowerContentText(entry) : string.Empty;
        bool hasValue = !string.IsNullOrWhiteSpace(value);
        state.itemTooltipLowerContentText.gameObject.SetActive(hasValue);
        state.itemTooltipLowerContentText.text = hasValue ? value : string.Empty;
    }

    private void SetTooltipItemIcon(State state, Context context, ItemDatabase.ItemEntry entry)
    {
        if (state.itemTooltipItemIconImage == null)
        {
            return;
        }

        Sprite iconSprite = context.ResolveTooltipItemIconSprite != null ? context.ResolveTooltipItemIconSprite(entry) : null;
        Vector2 iconSize = context.ResolveTooltipItemIconSize != null ? context.ResolveTooltipItemIconSize(entry) : state.itemTooltipItemIconImage.rectTransform.sizeDelta;
        state.itemTooltipItemIconImage.sprite = iconSprite;
        state.itemTooltipItemIconImage.preserveAspect = true;
        state.itemTooltipItemIconImage.rectTransform.sizeDelta = iconSize;
        state.itemTooltipItemIconImage.rectTransform.localScale = context.ItemTooltipIconScale;
        state.itemTooltipItemIconImage.material = context.EnsureItemTooltipIconFadeMaterial != null ? context.EnsureItemTooltipIconFadeMaterial() : null;
        state.itemTooltipItemIconImage.enabled = iconSprite != null;
    }

    private void RebuildTooltipGrantedSkillIcons(State state, Context context, ItemDatabase.ItemEntry entry)
    {
        ClearTooltipGrantedSkillIcons(state);
        if (entry == null || entry.grantedSkillIds == null || entry.grantedSkillIds.Count == 0 || state.itemTooltipGrantedSkillsText == null || state.itemTooltipGrantedSkillsIconRoot == null)
        {
            return;
        }

        int createdCount = 0;
        for (int i = 0; i < entry.grantedSkillIds.Count; i++)
        {
            string skillId = entry.grantedSkillIds[i];
            if (string.IsNullOrWhiteSpace(skillId))
            {
                continue;
            }

            BattleSkillDatabase.SkillEntry skillEntry = context.FindSkillEntry != null ? context.FindSkillEntry(skillId) : null;
            if (skillEntry == null || skillEntry.icon == null)
            {
                continue;
            }

            GameObject go = new GameObject($"附带技能图标_{createdCount}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(state.itemTooltipGrantedSkillsIconRoot, false);
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = new Vector2(30f, 30f);
            rect.anchoredPosition = new Vector2(createdCount * 34f, 0f);

            Image image = go.GetComponent<Image>();
            image.sprite = skillEntry.icon;
            image.preserveAspect = true;
            image.raycastTarget = false;

            state.itemTooltipGrantedSkillIcons.Add(go);
            createdCount++;
        }
    }

    private static void ClearTooltipGrantedSkillIcons(State state)
    {
        for (int i = 0; i < state.itemTooltipGrantedSkillIcons.Count; i++)
        {
            GameObject go = state.itemTooltipGrantedSkillIcons[i];
            if (go == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(go);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        state.itemTooltipGrantedSkillIcons.Clear();
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

    private static RectTransform ResolveItemTooltipDetailRoot(State state, Context context, RectTransform tooltipRoot, ItemDatabase.WeaponCategory weaponCategory)
    {
        if (tooltipRoot == null)
        {
            return null;
        }

        string[] detailRootNames = GetTooltipDetailRootNames(weaponCategory);
        for (int i = 0; i < detailRootNames.Length; i++)
        {
            string rootName = detailRootNames[i];
            if (string.IsNullOrWhiteSpace(rootName))
            {
                continue;
            }

            RectTransform detailRoot =
                context.FindChildByName(tooltipRoot, rootName) as RectTransform ??
                context.FindDescendantByName(tooltipRoot, rootName) as RectTransform;
            if (detailRoot != null)
            {
                return detailRoot;
            }
        }

        return tooltipRoot;
    }

    private static string[] GetTooltipDetailRootNames(ItemDatabase.WeaponCategory weaponCategory)
    {
        switch (weaponCategory)
        {
            case ItemDatabase.WeaponCategory.OneHanded:
                return new[] { "单手武器详情", "武器详情", "单_双手武器详情", "单/双手武器详情" };
            case ItemDatabase.WeaponCategory.TwoHanded:
                return new[] { "双手武器详情", "武器详情", "单_双手武器详情", "单/双手武器详情" };
            case ItemDatabase.WeaponCategory.Bow:
                return new[] { "弓箭详情", "武器详情", "单_双手武器详情", "单/双手武器详情" };
            default:
                return new[] { "武器详情", "单_双手武器详情", "单/双手武器详情" };
        }
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

    private static TMP_Text FindTooltipText(Context context, Transform root, string childName)
    {
        Transform target = context.FindChildByName(root, childName) ?? context.FindDescendantByName(root, childName);
        return target != null ? target.GetComponent<TMP_Text>() : null;
    }
}

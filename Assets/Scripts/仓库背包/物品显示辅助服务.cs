using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

internal static class 物品显示辅助服务
{
    private const string ItemTooltipIconFadeShaderName = "UI/BottomFadeImage";
    private static Material itemTooltipIconFadeMaterial;

    private sealed class AttackPowerSegment
    {
        public string attributeId = string.Empty;
        public float amount;
        public string colorHex = "#FFFFFF";
    }

    public static Sprite 解析提示框物品图标(
        ItemDatabase.ItemEntry entry,
        Func<Transform, string, Transform> findChildByName,
        Func<Transform, string, Transform> findDescendantByName,
        string itemIconName)
    {
        if (entry == null || entry.prefab == null)
        {
            return null;
        }

        Transform iconRoot = findChildByName(entry.prefab.transform, itemIconName) ?? findDescendantByName(entry.prefab.transform, itemIconName);
        Image iconImage = iconRoot != null ? iconRoot.GetComponent<Image>() : null;
        return iconImage != null ? iconImage.sprite : null;
    }

    public static Vector2 解析提示框物品图标尺寸(
        ItemDatabase.ItemEntry entry,
        Image tooltipItemIconImage,
        Func<Transform, string, Transform> findChildByName,
        Func<Transform, string, Transform> findDescendantByName,
        string itemIconName)
    {
        if (tooltipItemIconImage == null)
        {
            return Vector2.zero;
        }

        Vector2 iconSize = tooltipItemIconImage.rectTransform.sizeDelta;
        if (entry == null || entry.prefab == null)
        {
            return iconSize;
        }

        Transform iconRoot = findChildByName(entry.prefab.transform, itemIconName) ?? findDescendantByName(entry.prefab.transform, itemIconName);
        Image iconImage = iconRoot != null ? iconRoot.GetComponent<Image>() : null;
        if (iconImage == null || iconImage.rectTransform == null)
        {
            return iconSize;
        }

        return iconImage.rectTransform.sizeDelta;
    }

    public static string 获取提示框装备者显示文本(
        ItemDatabase.ItemEntry entry,
        InventoryShortcutRuntimeBinder.SlotRef slot,
        Func<string> resolveTooltipEquipmentOwnerCharacterId)
    {
        if (!装备数值服务.是攻击力武器条目(entry) || slot.kind != InventoryShortcutRuntimeBinder.SlotKind.Equipment)
        {
            return string.Empty;
        }

        string ownerCharacterId = resolveTooltipEquipmentOwnerCharacterId != null ? resolveTooltipEquipmentOwnerCharacterId() : string.Empty;
        if (string.IsNullOrWhiteSpace(ownerCharacterId))
        {
            return "装备者：\n无";
        }

        CharacterStatDatabase statDatabase = CharacterStatDatabase.LoadDefault();
        CharacterStatDatabase.StatEntry statEntry = statDatabase != null ? statDatabase.FindEntry(ownerCharacterId) : null;
        return statEntry != null ? $"装备者：\n{ownerCharacterId}" : "装备者：\n无";
    }

    public static void 设置提示框攻击力文本(
        ItemDatabase.ItemEntry entry,
        InventoryShortcutRuntimeBinder.SlotRef slot,
        TMP_Text attackPowerText,
        Func<string> resolveTooltipEquipmentOwnerCharacterId)
    {
        if (attackPowerText == null)
        {
            return;
        }

        string ownerCharacterId = resolveTooltipEquipmentOwnerCharacterId != null ? resolveTooltipEquipmentOwnerCharacterId() : string.Empty;
        string value = 获取攻击力显示文本(entry, slot, ownerCharacterId, attackPowerText, out List<AttackPowerSegment> _);
        bool hasValue = !string.IsNullOrEmpty(value);
        attackPowerText.gameObject.SetActive(hasValue);
        attackPowerText.text = value ?? string.Empty;
    }

    public static TMP_Text 确保提示框攻击力文本(TMP_Text current, RectTransform textContentRoot, TMP_Text fixedDamageText, TMP_Text attributeMultiplierText, TMP_Text descriptionText)
    {
        if (current != null)
        {
            return current;
        }

        if (textContentRoot == null)
        {
            return null;
        }

        TMP_Text template = fixedDamageText ?? attributeMultiplierText ?? descriptionText;
        if (template == null)
        {
            return null;
        }

        GameObject attackPowerObject = UnityEngine.Object.Instantiate(template.gameObject, textContentRoot, false);
        attackPowerObject.name = "攻击力";

        TMP_Text attackPowerText = attackPowerObject.GetComponent<TMP_Text>();
        RectTransform attackPowerRect = attackPowerObject.transform as RectTransform;
        RectTransform templateRect = template.rectTransform;
        if (attackPowerText == null || attackPowerRect == null || templateRect == null)
        {
            return attackPowerText;
        }

        attackPowerRect.anchorMin = templateRect.anchorMin;
        attackPowerRect.anchorMax = templateRect.anchorMax;
        attackPowerRect.pivot = templateRect.pivot;
        attackPowerRect.sizeDelta = templateRect.sizeDelta;
        attackPowerRect.localScale = templateRect.localScale;
        attackPowerRect.anchoredPosition = templateRect.anchoredPosition + new Vector2(0f, -36f);
        attackPowerText.fontStyle |= FontStyles.Bold;
        attackPowerText.text = string.Empty;
        attackPowerText.gameObject.SetActive(false);
        return attackPowerText;
    }

    public static string 获取攻击力显示文本(
        ItemDatabase.ItemEntry entry,
        InventoryShortcutRuntimeBinder.SlotRef slot,
        string ownerCharacterId,
        TMP_Text attackPowerText,
        out List<object> dummySegments)
    {
        dummySegments = null;
        string result = 获取攻击力显示文本(entry, slot, ownerCharacterId, attackPowerText, out List<AttackPowerSegment> segments);
        if (segments != null)
        {
            dummySegments = new List<object>(segments.Count);
            for (int i = 0; i < segments.Count; i++)
            {
                dummySegments.Add(segments[i]);
            }
        }

        return result;
    }

    private static string 获取攻击力显示文本(
        ItemDatabase.ItemEntry entry,
        InventoryShortcutRuntimeBinder.SlotRef slot,
        string ownerCharacterId,
        TMP_Text attackPowerText,
        out List<AttackPowerSegment> segments)
    {
        segments = null;
        if (!装备数值服务.是攻击力武器条目(entry) || slot.kind != InventoryShortcutRuntimeBinder.SlotKind.Equipment)
        {
            return string.Empty;
        }

        return 构建攻击力显示文本(entry, ownerCharacterId, attackPowerText, out segments);
    }

    public static string 构建提示框下方面板文本(ItemDatabase.ItemEntry entry)
    {
        if (entry == null)
        {
            return string.Empty;
        }

        WeaponDetailLowerTextDatabase database = WeaponDetailLowerTextDatabase.LoadDefault();
        List<string> lines = new List<string>();

        if (entry.criticalChanceBonus > 0)
        {
            string line = 格式化武器下方面板文本(database != null ? database.criticalChanceFormat : string.Empty, entry.criticalChanceBonus.ToString());
            if (!string.IsNullOrWhiteSpace(line))
            {
                lines.Add(line);
            }
        }

        if (entry.criticalDamageBonus > 0)
        {
            string line = 格式化武器下方面板文本(database != null ? database.criticalDamageFormat : string.Empty, entry.criticalDamageBonus.ToString());
            if (!string.IsNullOrWhiteSpace(line))
            {
                lines.Add(line);
            }
        }

        return lines.Count > 0 ? string.Join("\n", lines) : string.Empty;
    }

    public static string 获取角色攻击力显示文本(ItemDatabase.ItemEntry entry, string ownerCharacterId)
    {
        return 构建攻击力显示文本(entry, ownerCharacterId, null, out _);
    }

    private static string 构建攻击力显示文本(ItemDatabase.ItemEntry entry, string ownerCharacterId, TMP_Text attackPowerText, out List<AttackPowerSegment> segments)
    {
        segments = null;
        if (!装备数值服务.是攻击力武器条目(entry))
        {
            return string.Empty;
        }

        if (string.IsNullOrWhiteSpace(ownerCharacterId))
        {
            return "攻击力：无";
        }

        CharacterStatDatabase statDatabase = CharacterStatDatabase.LoadDefault();
        CharacterStatDatabase.StatEntry statEntry = statDatabase != null ? statDatabase.FindEntry(ownerCharacterId) : null;
        if (statEntry == null)
        {
            return "攻击力：无";
        }

        segments = 构建攻击力分段(entry, statEntry);
        if (segments == null)
        {
            return "<color=#E6C229>攻击力：伤害分布未配置</color>";
        }

        if (segments.Count == 0)
        {
            float attackPower = 装备数值服务.计算武器攻击力(entry, statEntry);
            return $"攻击力：{attackPower:0.##}";
        }

        TMP_SpriteAsset activeSpriteAsset = 解析攻击力精灵资源();
        List<string> parts = new List<string>();
        for (int i = 0; i < segments.Count; i++)
        {
            AttackPowerSegment segment = segments[i];
            string segmentText = $"<color={segment.colorHex}>{格式化提示框攻击力数值(segment.amount)}</color>";

            string spriteName = 获取攻击力精灵名(segment.attributeId);
            if (activeSpriteAsset != null && !string.IsNullOrWhiteSpace(spriteName))
            {
                segmentText += $"<sprite name=\"{spriteName}\">";
            }

            parts.Add(segmentText);
        }

        if (attackPowerText != null)
        {
            attackPowerText.spriteAsset = activeSpriteAsset;
        }

        return $"攻击力：{string.Join("<color=#808080>+</color>", parts)}";
    }

    private static string 格式化武器下方面板文本(string format, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        if (string.IsNullOrEmpty(format))
        {
            return value;
        }

        return format.Contains("x")
            ? format.Replace("x", value)
            : format + value;
    }

    private static List<AttackPowerSegment> 构建攻击力分段(ItemDatabase.ItemEntry entry, CharacterStatDatabase.StatEntry statEntry)
    {
        List<AttackPowerSegment> segments = new List<AttackPowerSegment>();
        if (!装备数值服务.是攻击力武器条目(entry) || statEntry == null)
        {
            return segments;
        }

        float attackPower = 装备数值服务.计算武器攻击力(entry, statEntry);
        if (attackPower <= 0f)
        {
            return segments;
        }

        ItemDatabase.WeaponDamageDistribution distribution = entry.weaponDamageDistribution;
        if (distribution == null || distribution.physical < 0 || distribution.fire < 0 ||
            distribution.corruption < 0 || distribution.cold < 0 || distribution.Total != 100)
        {
            Debug.LogWarning($"[物品数据警告] 武器提示读取到未配置伤害分布：{entry.itemId}");
            return null;
        }

        int total = Mathf.Max(1, distribution.Total);
        尝试添加攻击力分段(segments, "物理", distribution.physical, total, attackPower, "#FFFFFF");
        尝试添加攻击力分段(segments, "火焰", distribution.fire, total, attackPower, "#FF8A00");
        尝试添加攻击力分段(segments, "腐败", distribution.corruption, total, attackPower, "#33CC66");
        尝试添加攻击力分段(segments, "寒冷", distribution.cold, total, attackPower, "#4DA6FF");
        return segments;
    }

    private static void 尝试添加攻击力分段(List<AttackPowerSegment> segments, string attributeId, int distributionValue, int distributionTotal, float attackPower, string colorHex)
    {
        if (segments == null || distributionValue <= 0 || distributionTotal <= 0 || attackPower <= 0f)
        {
            return;
        }

        float amount = attackPower * distributionValue / distributionTotal;
        if (amount <= 0f)
        {
            return;
        }

        segments.Add(new AttackPowerSegment
        {
            attributeId = attributeId,
            amount = amount,
            colorHex = colorHex
        });
    }

    private static string 格式化提示框攻击力数值(float value)
    {
        if (Mathf.Approximately(value, Mathf.Round(value)))
        {
            return Mathf.RoundToInt(value).ToString();
        }

        return value.ToString("0.#");
    }

    private static string 获取攻击力精灵名(string attributeId)
    {
        switch (attributeId)
        {
            case "物理":
                return "物理伤害";
            case "火焰":
                return "火焰伤害";
            case "腐败":
                return "腐败伤害";
            case "寒冷":
                return "寒冷伤害";
            default:
                return string.Empty;
        }
    }

    private static TMP_SpriteAsset 解析攻击力精灵资源()
    {
        AttackPowerTextSpriteDatabase database = AttackPowerTextSpriteDatabase.LoadDefault();
        return database != null ? database.spriteAsset : null;
    }

    public static Material 确保提示框图标渐隐材质()
    {
        if (itemTooltipIconFadeMaterial != null)
        {
            return itemTooltipIconFadeMaterial;
        }

        Shader shader = Shader.Find(ItemTooltipIconFadeShaderName);
        if (shader == null)
        {
            return null;
        }

        itemTooltipIconFadeMaterial = new Material(shader)
        {
            name = "ItemTooltipIconBottomFade"
        };
        itemTooltipIconFadeMaterial.hideFlags = HideFlags.HideAndDontSave;
        itemTooltipIconFadeMaterial.SetFloat("_FadeHeight", 0.2f);
        itemTooltipIconFadeMaterial.SetFloat("_FadePower", 3f);
        return itemTooltipIconFadeMaterial;
    }

    public static string 解析物品显示名(ItemDatabase.ItemEntry entry)
    {
        if (entry == null)
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(entry.displayName))
        {
            return entry.displayName;
        }

        if (!string.IsNullOrWhiteSpace(entry.itemId) && entry.itemId.StartsWith("itm_", StringComparison.Ordinal))
        {
            return entry.itemId.Substring(4);
        }

        return entry.itemId ?? string.Empty;
    }

    public static string 获取固定伤害显示文本(ItemDatabase.ItemEntry entry)
    {
        float value = entry != null ? entry.fixedDamage : 0f;
        return $"固定伤害：{value:0.##}";
    }

    public static string 获取属性倍率显示文本(ItemDatabase.ItemEntry entry)
    {
        if (entry == null || entry.weaponAttributeMultipliers == null || entry.weaponAttributeMultipliers.Count == 0)
        {
            return string.Empty;
        }

        List<string> parts = new List<string>();
        for (int i = 0; i < entry.weaponAttributeMultipliers.Count; i++)
        {
            ItemDatabase.WeaponAttributeMultiplierEntry multiplier = entry.weaponAttributeMultipliers[i];
            if (multiplier == null || multiplier.attributeType == ItemDatabase.WeaponAttributeType.None)
            {
                continue;
            }

            parts.Add($"{获取武器属性类型显示名(multiplier.attributeType)}{获取属性倍率等级(multiplier.multiplier)}");
        }

        return parts.Count == 0 ? string.Empty : string.Join(" ", parts);
    }

    public static string 获取物品品质显示名(ItemDatabase.ItemQuality quality)
    {
        switch (quality)
        {
            case ItemDatabase.ItemQuality.Excellent:
                return "优秀";
            case ItemDatabase.ItemQuality.Epic:
                return "史诗";
            case ItemDatabase.ItemQuality.Blessed:
                return "赐福";
            default:
                return "普通";
        }
    }

    public static string 获取武器类别显示名(ItemDatabase.WeaponCategory weaponCategory)
    {
        switch (weaponCategory)
        {
            case ItemDatabase.WeaponCategory.OneHanded:
                return "单手武器";
            case ItemDatabase.WeaponCategory.TwoHanded:
                return "双手武器";
            case ItemDatabase.WeaponCategory.Bow:
                return "弓箭";
            case ItemDatabase.WeaponCategory.Staff:
                return "法杖";
            default:
                return weaponCategory == ItemDatabase.WeaponCategory.None ? string.Empty : weaponCategory.ToString();
        }
    }

    private static string 获取武器属性类型显示名(ItemDatabase.WeaponAttributeType attributeType)
    {
        switch (attributeType)
        {
            case ItemDatabase.WeaponAttributeType.Strength:
                return "力量";
            case ItemDatabase.WeaponAttributeType.Agility:
                return "敏捷";
            case ItemDatabase.WeaponAttributeType.Intelligence:
                return "智力";
            default:
                return string.Empty;
        }
    }

    private static string 获取属性倍率等级(float multiplier)
    {
        if (multiplier < 0.5f)
        {
            return "E";
        }

        if (multiplier < 1f)
        {
            return "D";
        }

        if (multiplier < 1.5f)
        {
            return "C";
        }

        if (multiplier < 2f)
        {
            return "B";
        }

        if (multiplier < 2.5f)
        {
            return "A";
        }

        return "S";
    }

    public static Sprite 解析显示图标(
        InventoryShortcutRuntimeBinder.ItemSlotData data,
        Func<string, ItemDatabase.ItemEntry> resolveItemEntry,
        Func<Transform, string, Transform> findChildByName,
        Func<Transform, string, Transform> findDescendantByName)
    {
        if (data.icon != null)
        {
            return data.icon;
        }

        GameObject prefab = 解析物品预制体(data.itemId, resolveItemEntry);
        if (prefab == null)
        {
            return null;
        }

        Image image = 解析显示图片(prefab.transform, findChildByName, findDescendantByName);
        return image != null ? image.sprite : null;
    }

    public static Sprite 解析预制体显示图标(
        GameObject prefab,
        Func<Transform, string, Transform> findChildByName,
        Func<Transform, string, Transform> findDescendantByName)
    {
        if (prefab == null)
        {
            return null;
        }

        Image image = 解析显示图片(prefab.transform, findChildByName, findDescendantByName);
        return image != null ? image.sprite : null;
    }

    private static GameObject 解析物品预制体(string itemId, Func<string, ItemDatabase.ItemEntry> resolveItemEntry)
    {
        ItemDatabase.ItemEntry entry = resolveItemEntry != null ? resolveItemEntry(itemId) : null;
        return entry != null ? entry.prefab : null;
    }

    public static GameObject 解析品质背景预制体(
        Dictionary<string, GameObject> qualityBackgroundPrefabCache,
        ItemDatabase.ItemEntry entry)
    {
        if (qualityBackgroundPrefabCache == null || entry == null)
        {
            return null;
        }

        string cacheKey = 构建品质背景缓存键(entry.quality, 应使用一乘二品质背景(entry));
        return qualityBackgroundPrefabCache.TryGetValue(cacheKey, out GameObject prefab) ? prefab : null;
    }

    public static Sprite 解析运行时图标(InventoryShortcutRuntimeBinder.SlotWidget widget)
    {
        if (widget == null)
        {
            return null;
        }

        if (widget.runtimeIconVisual != null)
        {
            Image image = widget.runtimeIconVisual.GetComponent<Image>();
            if (image != null)
            {
                return image.sprite;
            }
        }

        return widget.icon != null ? widget.icon.sprite : null;
    }

    public static void 缓存品质背景预制体(Dictionary<string, GameObject> qualityBackgroundPrefabCache)
    {
        if (qualityBackgroundPrefabCache == null)
        {
            return;
        }

        qualityBackgroundPrefabCache.Clear();
        ItemQualityBackgroundDatabase database = ItemQualityBackgroundDatabase.LoadDefault();
        if (database == null)
        {
            return;
        }

        缓存品质背景预制体(database, qualityBackgroundPrefabCache, ItemDatabase.ItemQuality.Common);
        缓存品质背景预制体(database, qualityBackgroundPrefabCache, ItemDatabase.ItemQuality.Excellent);
        缓存品质背景预制体(database, qualityBackgroundPrefabCache, ItemDatabase.ItemQuality.Epic);
        缓存品质背景预制体(database, qualityBackgroundPrefabCache, ItemDatabase.ItemQuality.Blessed);
    }

    private static void 缓存品质背景预制体(ItemQualityBackgroundDatabase database, Dictionary<string, GameObject> cache, ItemDatabase.ItemQuality quality)
    {
        if (database == null || cache == null)
        {
            return;
        }

        缓存品质背景预制体变体(database, cache, quality, false);
        缓存品质背景预制体变体(database, cache, quality, true);
    }

    private static void 缓存品质背景预制体变体(ItemQualityBackgroundDatabase database, Dictionary<string, GameObject> cache, ItemDatabase.ItemQuality quality, bool useOneByTwo)
    {
        GameObject prefab = database.GetPrefab(quality, useOneByTwo);
        if (prefab == null)
        {
            return;
        }

        cache[构建品质背景缓存键(quality, useOneByTwo)] = prefab;
    }

    private static bool 应使用一乘二品质背景(ItemDatabase.ItemEntry entry)
    {
        return entry != null &&
            entry.category == ItemDatabase.ItemCategory.Equipment &&
            (entry.weaponCategory == ItemDatabase.WeaponCategory.Bow ||
             entry.weaponCategory == ItemDatabase.WeaponCategory.TwoHanded ||
             entry.weaponCategory == ItemDatabase.WeaponCategory.Staff);
    }

    private static string 构建品质背景缓存键(ItemDatabase.ItemQuality quality, bool useOneByTwo)
    {
        return quality + (useOneByTwo ? "_1x2" : "_1x1");
    }

    private static Image 解析显示图片(
        Transform root,
        Func<Transform, string, Transform> findChildByName,
        Func<Transform, string, Transform> findDescendantByName)
    {
        if (root == null)
        {
            return null;
        }

        Transform picture = findChildByName(root, "图片") ?? findDescendantByName(root, "图片");
        if (picture != null)
        {
            Image pictureImage = picture.GetComponent<Image>();
            if (pictureImage != null && pictureImage.sprite != null)
            {
                return pictureImage;
            }
        }

        Image[] images = root.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] != null && images[i].sprite != null)
            {
                return images[i];
            }
        }

        return null;
    }

    public static ItemDatabase.ItemEntry 解析物品条目(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return null;
        }

        ItemDatabase database = ItemDatabase.LoadDefault();
        return database != null ? database.FindEntry(itemId) : null;
    }

    public static bool 应显示物品(InventoryShortcutRuntimeBinder.CategoryFilterBinding binding, InventoryShortcutRuntimeBinder.ItemSlotData data, Func<string, ItemDatabase.ItemEntry> resolveItemEntry)
    {
        if (data.IsEmpty || binding == null || binding.selectedCategories.Count == 0)
        {
            return true;
        }

        ItemDatabase.ItemEntry entry = resolveItemEntry != null ? resolveItemEntry(data.itemId) : null;
        return entry != null && binding.selectedCategories.Contains(entry.category);
    }
}
